using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// 3スロットのスキルを管理し、戦闘中のリアルタイム切り替えを受け付ける。
    /// ・ラック方式: スロットごとに最大3候補を順送り
    /// ・プリセット方式: 3スロットをまとめたプリセット(最大4つ)を一括で切り替え
    /// </summary>
    public class SkillSlotController : MonoBehaviour
    {
        public const int PresetCount = 4;

        [Header("切り替え方式")]
        [SerializeField, FormerlySerializedAs("mode")] SwapMode mMode = SwapMode.Rack;

        [Header("ラック方式")]
        [SerializeField, FormerlySerializedAs("attackARack")] SkillData[] mAttackARack = new SkillData[SkillRack.Capacity];
        [SerializeField, FormerlySerializedAs("attackBRack")] SkillData[] mAttackBRack = new SkillData[SkillRack.Capacity];
        [SerializeField, FormerlySerializedAs("movementRack")] SkillData[] mMovementRack = new SkillData[SkillRack.Capacity];

        [Header("プリセット方式")]
        [SerializeField, FormerlySerializedAs("defaultPresets")] SkillPreset[] mDefaultPresets = new SkillPreset[PresetCount];
        [SerializeField, Tooltip("編成画面で選べるスキルと、保存データの読み込みに使う"), FormerlySerializedAs("database")] SkillDatabase mDatabase;
        [SerializeField, Tooltip("編成をブラウザ/PCに保存して、次回も使う"), FormerlySerializedAs("persistLoadout")] bool mPersistLoadout = true;

        [Header("武器種")]
        [SerializeField, FormerlySerializedAs("weaponDatabase")] WeaponTypeData[] mWeaponDatabase;

        SkillRack[] mRacks;
        readonly List<SkillPreset> mPresets = new List<SkillPreset>();
        int mPresetIndex;

        // 探索モード: 集めたスキルだけを使える。編成はアリーナとは別に保存する
        bool mCollecting;
        SkillPreset[] mCollectionPresets;
        string mStorageKey = LoadoutStorage.PrefsKey;

        /// <summary>スロットのスキルが変わった(切り替え・編成の変更)</summary>
        public event Action<SlotType> SlotChanged;
        public event Action PresetChanged;
        public event Action ModeChanged;

        public SwapMode Mode => mMode;
        public int PresetIndex => mPresetIndex;
        public IReadOnlyList<SkillPreset> Presets => mPresets;
        public SkillPreset CurrentPreset => mPresets.Count > 0 ? mPresets[mPresetIndex] : null;
        public SkillDatabase Database => mDatabase;
        public IReadOnlyList<WeaponTypeData> Weapons => mWeaponDatabase;
        public WeaponBonus Bonus { get; private set; }

        /// <summary>マスタリー中ならその武器種のフィニッシャー、それ以外は null</summary>
        public SkillData MasteryFinisher
        {
            get
            {
                if (Bonus.Kind != WeaponBonusKind.Mastery) return null;
                var aData = GetWeaponData(Bonus.Weapon);
                return aData != null ? aData.MasteryFinisher : null;
            }
        }

        void Awake()
        {
            mRacks = new[]
            {
                new SkillRack(mAttackARack),
                new SkillRack(mAttackBRack),
                new SkillRack(mMovementRack),
            };

            ResetPresetsToDefault();
            if (mPersistLoadout && LoadoutStorage.TryLoad(mDatabase, out var aSaved))
            {
                mMode = aSaved.Mode;
                for (int aI = 0; aI < mPresets.Count && aI < aSaved.Presets.Count; aI++) mPresets[aI] = aSaved.Presets[aI];
                mPresetIndex = Mathf.Clamp(aSaved.PresetIndex, 0, mPresets.Count - 1);
            }
            RecomputeBonus();
        }

        void ResetPresetsToDefault()
        {
            var aSources = mCollecting ? mCollectionPresets : mDefaultPresets;
            mPresets.Clear();
            for (int aI = 0; aI < PresetCount; aI++)
            {
                var aSource = aSources != null && aI < aSources.Length ? aSources[aI] : null;
                mPresets.Add(aSource != null ? aSource.Clone() : new SkillPreset { Name = $"プリセット{aI + 1}" });
            }
            mPresetIndex = 0;
        }

        /// <summary>探索モードで、集めたスキルだけを使えるようにしているか</summary>
        public bool IsCollecting => mCollecting;

        /// <summary>編成で選べるスキルか(探索モードでは手に入れたものだけ)</summary>
        public bool IsAvailable(SkillData iSkill) => iSkill != null && (!mCollecting || ExplorationSave.IsUnlocked(iSkill));

        /// <summary>
        /// 探索モードに入る。プリセット方式に固定し、集めたスキルだけで組んだ編成を読み込む。
        /// 手に入れていないスキルが保存データに残っていたら外す。
        /// </summary>
        public void BeginCollection(SkillPreset[] iStartingPresets)
        {
            mCollecting = true;
            mCollectionPresets = iStartingPresets;
            mStorageKey = LoadoutStorage.ExplorationPrefsKey;
            mMode = SwapMode.Preset;

            ResetPresetsToDefault();
            if (mPersistLoadout && LoadoutStorage.TryLoad(mStorageKey, mDatabase, out var aSaved))
            {
                for (int aI = 0; aI < mPresets.Count && aI < aSaved.Presets.Count; aI++) mPresets[aI] = aSaved.Presets[aI];
                mPresetIndex = Mathf.Clamp(aSaved.PresetIndex, 0, mPresets.Count - 1);
            }
            foreach (var aPreset in mPresets)
            {
                for (int aS = 0; aS < 3; aS++)
                {
                    if (!IsAvailable(aPreset.Get((SlotType)aS))) aPreset.Set((SlotType)aS, null);
                }
            }
            if (mPresets[mPresetIndex].IsEmpty) mPresetIndex = FirstNonEmptyPreset();

            RecomputeBonus();
            for (int aI = 0; aI < 3; aI++) SlotChanged?.Invoke((SlotType)aI);
            ModeChanged?.Invoke();
            PresetChanged?.Invoke();
        }

        /// <summary>
        /// 新しく手に入れたスキルを、そのスロットが空いている最初のプリセットに入れる(すぐ使えるように)。
        /// 入れたプリセットの番号を返す。空きがなければ -1。
        /// </summary>
        public int EquipNewSkill(SkillData iSkill)
        {
            if (iSkill == null) return -1;
            for (int aI = 0; aI < mPresets.Count; aI++)
            {
                if (mPresets[aI].Get(iSkill.Slot) != null) continue;
                AssignPresetSkill(aI, iSkill.Slot, iSkill);
                SaveLoadout();
                return aI;
            }
            return -1;
        }

        int FirstNonEmptyPreset()
        {
            for (int aI = 0; aI < mPresets.Count; aI++)
            {
                if (!mPresets[aI].IsEmpty) return aI;
            }
            return 0;
        }

        public SkillRack GetRack(SlotType iSlot) => mRacks[(int)iSlot];

        public SkillData GetCurrent(SlotType iSlot)
        {
            if (mMode == SwapMode.Rack) return mRacks[(int)iSlot].Current;
            var aPreset = CurrentPreset;
            return aPreset != null ? aPreset.Get(iSlot) : null;
        }

        /// <summary>ラック方式: スロットを次の候補へ切り替える</summary>
        public bool Cycle(SlotType iSlot)
        {
            if (mMode != SwapMode.Rack || !mRacks[(int)iSlot].Cycle()) return false;
            RecomputeBonus();
            SlotChanged?.Invoke(iSlot);
            return true;
        }

        /// <summary>プリセット方式: プリセットを一括で切り替える。中身が変わったスロットだけ SlotChanged を出す。</summary>
        public bool SelectPreset(int iIndex)
        {
            if (mMode != SwapMode.Preset || iIndex < 0 || iIndex >= mPresets.Count || iIndex == mPresetIndex) return false;
            // 空のプリセットに切り替えると何もできなくなるので、切り替えない
            if (mPresets[iIndex].IsEmpty) return false;

            var aBefore = new[] { GetCurrent(SlotType.AttackA), GetCurrent(SlotType.AttackB), GetCurrent(SlotType.Movement) };
            mPresetIndex = iIndex;
            RecomputeBonus();
            for (int aI = 0; aI < 3; aI++)
            {
                if (GetCurrent((SlotType)aI) != aBefore[aI]) SlotChanged?.Invoke((SlotType)aI);
            }
            PresetChanged?.Invoke();
            return true;
        }

        public void SetMode(SwapMode iNewMode)
        {
            // 探索モードはプリセット方式だけ(ラックの候補は集めたスキルと関係なく決まっているため)
            if (mMode == iNewMode || mCollecting) return;
            mMode = iNewMode;
            RecomputeBonus();
            for (int aI = 0; aI < 3; aI++) SlotChanged?.Invoke((SlotType)aI);
            ModeChanged?.Invoke();
        }

        /// <summary>編成画面から: プリセットのスロットにスキルを入れる</summary>
        public bool AssignPresetSkill(int iIndex, SlotType iSlot, SkillData iSkill)
        {
            if (iIndex < 0 || iIndex >= mPresets.Count || (iSkill != null && !IsAvailable(iSkill)) || !mPresets[iIndex].Set(iSlot, iSkill)) return false;
            if (iIndex == mPresetIndex)
            {
                RecomputeBonus();
                if (mMode == SwapMode.Preset) SlotChanged?.Invoke(iSlot);
            }
            return true;
        }

        public void RenamePreset(int iIndex, string iNewName)
        {
            if (iIndex >= 0 && iIndex < mPresets.Count) mPresets[iIndex].Name = iNewName;
        }

        public void RestoreDefaultPresets()
        {
            ResetPresetsToDefault();
            RecomputeBonus();
            for (int aI = 0; aI < 3; aI++) SlotChanged?.Invoke((SlotType)aI);
            PresetChanged?.Invoke();
        }

        public void SaveLoadout()
        {
            if (mPersistLoadout) LoadoutStorage.Save(mStorageKey, mMode, mPresetIndex, mPresets);
        }

        public WeaponTypeData GetWeaponData(WeaponType iWeapon)
        {
            if (mWeaponDatabase == null) return null;
            foreach (var aData in mWeaponDatabase)
            {
                if (aData != null && aData.Weapon == iWeapon) return aData;
            }
            return null;
        }

        void RecomputeBonus()
        {
            var aA = GetCurrent(SlotType.AttackA);
            var aB = GetCurrent(SlotType.AttackB);
            var aM = GetCurrent(SlotType.Movement);
            Bonus = aA != null && aB != null && aM != null
                ? WeaponBonusResolver.Resolve(aA.Weapon, aB.Weapon, aM.Weapon)
                : WeaponBonus.None;
        }
    }
}
