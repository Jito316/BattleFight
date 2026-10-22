using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace BattleFight
{
    /// <summary>
    /// スキル編成画面(UI Toolkit)。P / Select で開閉し、開いている間はゲームを止める。
    /// ・切り替え方式(ラック / プリセット)の選択
    /// ・4つのプリセットの中身(攻撃A / 攻撃B / 移動)をマウスで選ぶ
    /// 閉じると編成を保存する(WebGL ではブラウザに保存)。レイアウトは LoadoutEditor.uxml。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class LoadoutEditorUI : MonoBehaviour
    {
        static readonly string[] mSlotLabels = { "攻撃A", "攻撃B", "移動" };
        static readonly string[] mListNames = { "list-a", "list-b", "list-m" };
        static readonly Dictionary<SkillBehavior, string> mBehaviorLabels = new Dictionary<SkillBehavior, string>
        {
            { SkillBehavior.Standard, "近接" },
            { SkillBehavior.DashStrike, "突進" },
            { SkillBehavior.ChargeCounter, "溜め・カウンター" },
            { SkillBehavior.HammerJump, "跳躍" },
            { SkillBehavior.Grapple, "ワイヤー" },
            { SkillBehavior.Pull, "引き寄せ" },
            { SkillBehavior.Projectile, "射撃" },
            { SkillBehavior.Beam, "ビーム" },
            { SkillBehavior.TargetedStrike, "遠隔の範囲攻撃" },
            { SkillBehavior.Blink, "瞬間移動" },
            { SkillBehavior.Boost, "ブースト" },
        };

        [SerializeField, FormerlySerializedAs("slots")] SkillSlotController mSlots;
        [SerializeField, FormerlySerializedAs("input")] PlayerInputReader mInput;

        public static bool IsOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsOpen = false;

        VisualElement mRoot;
        VisualElement mEditor;
        VisualElement mMode;
        VisualElement mDetailStats;
        Label mCollectInfo;
        Label mNotice;
        Label mNameCaption;
        Label mDetailEmpty;
        Label mDetailName;
        Label mDetailMeta;
        Label mDetailDesc;
        Label mDetailBonus;
        Label mFooter;
        Button mModeRack;
        Button mModePreset;
        Button mClose;
        Button mReset;
        TextField mPresetName;
        readonly ScrollView[] mLists = new ScrollView[3];
        readonly List<(VisualElement card, Label number, Label title, Label inUse, Label[] skills, Label bonus)> mPresetCards =
            new List<(VisualElement, Label, Label, Label, Label[], Label)>();
        readonly List<(VisualElement row, SkillData skill, SlotType slot)> mSkillRows = new List<(VisualElement, SkillData, SlotType)>();

        int mEditIndex;
        SkillData mDetailSkill;
        bool mListsDirty = true;

        void OnEnable()
        {
            mRoot = GetComponent<UIDocument>().rootVisualElement;
            mEditor = mRoot.Q("editor");
            mMode = mRoot.Q("mode");
            mCollectInfo = mRoot.Q<Label>("collect-info");
            mNotice = mRoot.Q<Label>("notice");
            mNameCaption = mRoot.Q<Label>("name-caption");
            mPresetName = mRoot.Q<TextField>("preset-name");
            mDetailEmpty = mRoot.Q<Label>("detail-empty");
            mDetailName = mRoot.Q<Label>("detail-name");
            mDetailMeta = mRoot.Q<Label>("detail-meta");
            mDetailDesc = mRoot.Q<Label>("detail-desc");
            mDetailStats = mRoot.Q("detail-stats");
            mDetailBonus = mRoot.Q<Label>("detail-bonus");
            mFooter = mRoot.Q<Label>("footer");
            mModeRack = mRoot.Q<Button>("mode-rack");
            mModePreset = mRoot.Q<Button>("mode-preset");
            mClose = mRoot.Q<Button>("close");
            mReset = mRoot.Q<Button>("reset");
            for (int aI = 0; aI < 3; aI++) mLists[aI] = mRoot.Q<ScrollView>(mListNames[aI]);

            mPresetCards.Clear();
            for (int aI = 0; aI < SkillSlotController.PresetCount; aI++)
            {
                var aInstance = mRoot.Q($"preset-card-{aI + 1}");
                var aCard = aInstance.Q("card");
                int aIndex = aI;
                aCard.RegisterCallback<ClickEvent>(_ => SelectPreset(aIndex));
                mPresetCards.Add((aCard, aInstance.Q<Label>("number"), aInstance.Q<Label>("title"), aInstance.Q<Label>("in-use"),
                    new[] { aInstance.Q<Label>("a"), aInstance.Q<Label>("b"), aInstance.Q<Label>("m") }, aInstance.Q<Label>("bonus")));
            }

            mModeRack.clicked += () => { mSlots.SetMode(SwapMode.Rack); Refresh(); };
            mModePreset.clicked += () => { mSlots.SetMode(SwapMode.Preset); Refresh(); };
            mClose.clicked += Close;
            mReset.clicked += () => { mSlots.RestoreDefaultPresets(); Refresh(); };
            mPresetName.RegisterValueChangedCallback(iE =>
            {
                if (IsOpen && iE.newValue != mSlots.Presets[mEditIndex].Name)
                {
                    mSlots.RenamePreset(mEditIndex, iE.newValue);
                    RefreshPresets();
                }
            });

            mEditor.style.display = IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnDisable()
        {
            if (IsOpen) Close();
        }

        void Update()
        {
            // 名前を入力している間は、P や Esc を文字として扱う(閉じない)
            bool aTyping = IsOpen && mRoot.focusController?.focusedElement is TextField;
            bool aToggle = mInput.Consume(PlayerAction.OpenLoadout) && !aTyping;
            bool aEscape = IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (aEscape && aTyping)
            {
                mPresetName.Blur();
                return;
            }
            if (aToggle || aEscape)
            {
                if (IsOpen) Close();
                else Open();
            }
        }

        public void Open()
        {
            IsOpen = true;
            mEditIndex = mSlots.PresetIndex;
            mDetailSkill = null;
            mListsDirty = true;
            mEditor.style.display = DisplayStyle.Flex;
            Refresh();
            GamePause.Set(true);
            ThirdPersonCamera.SetCursorLocked(false);
        }

        public void Close()
        {
            IsOpen = false;
            if (mEditor != null) mEditor.style.display = DisplayStyle.None;
            mPresetName?.Blur();
            mSlots.SaveLoadout();
            GamePause.Set(false);
            mInput.ClearBuffer();
#if !UNITY_WEBGL || UNITY_EDITOR
            ThirdPersonCamera.SetCursorLocked(true);
#endif
        }

        void SelectPreset(int iIndex)
        {
            mEditIndex = iIndex;
            mPresetName.Blur();
            Refresh();
        }

        // ---------- 反映 ----------

        void Refresh()
        {
            RefreshHeader();
            RefreshPresets();
            if (mListsDirty) BuildLists();
            RefreshLists();
            RefreshDetail();
        }

        void RefreshHeader()
        {
            bool aCollecting = mSlots.IsCollecting;
            mMode.style.display = aCollecting ? DisplayStyle.None : DisplayStyle.Flex;
            mCollectInfo.style.display = aCollecting ? DisplayStyle.Flex : DisplayStyle.None;
            if (aCollecting)
            {
                // 探索: 切り替え方式はプリセットだけ。代わりに集めたスキルの数を出す
                int aTotal = ExplorationRegion.Active != null ? ExplorationRegion.Active.TotalSkills : ExplorationSave.TotalSkills;
                mCollectInfo.text = $"探索中: 集めたスキルだけで組めます   <color=#FFE08A>{ExplorationSave.UnlockedCount} / {aTotal}</color>";
            }
            bool aPreset = mSlots.Mode == SwapMode.Preset;
            mModePreset.EnableInClassList("bf-button--selected", aPreset);
            mModeRack.EnableInClassList("bf-button--selected", !aPreset);
            mNotice.style.display = !aPreset && !aCollecting ? DisplayStyle.Flex : DisplayStyle.None;
            mClose.text = "閉じる [P]";
            mFooter.text = "クリックでスキルを入れる  /  戦闘中は 1〜4キー(十字キー)でプリセットを一括切り替え  /  閉じると自動で保存";
        }

        void RefreshPresets()
        {
            for (int aI = 0; aI < mPresetCards.Count && aI < mSlots.Presets.Count; aI++)
            {
                var (aCard, aNumber, aTitle, aInUse, aSkills, aBonus) = mPresetCards[aI];
                var aPreset = mSlots.Presets[aI];
                aCard.EnableInClassList("preset-card--editing", aI == mEditIndex);
                aInUse.style.display = mSlots.Mode == SwapMode.Preset && aI == mSlots.PresetIndex ? DisplayStyle.Flex : DisplayStyle.None;
                aNumber.text = (aI + 1).ToString();
                aTitle.text = aPreset.Name;
                var aA = aPreset.AttackA;
                aCard.style.borderLeftColor = aA != null ? WeaponColor(aA) : new Color(1f, 1f, 1f, 0.2f);
                for (int aS = 0; aS < 3; aS++)
                {
                    var aSkill = aPreset.Get((SlotType)aS);
                    string aName = aSkill != null ? $"<color=#{Hex(WeaponColor(aSkill))}>{aSkill.DisplayName}</color>" : "<color=#666666>(なし)</color>";
                    aSkills[aS].text = $"<color=#8A93A3>{mSlotLabels[aS]}</color>  {aName}";
                }
                aBonus.text = BonusText(aPreset.Bonus);
            }
            mNameCaption.text = $"プリセット{mEditIndex + 1} の名前";
            mPresetName.SetValueWithoutNotify(mSlots.Presets[mEditIndex].Name);
        }

        /// <summary>スキルの一覧を作る(武器種ごとにまとめる)。手に入れていないスキルは「？？？」</summary>
        void BuildLists()
        {
            mListsDirty = false;
            mSkillRows.Clear();
            for (int aS = 0; aS < 3; aS++)
            {
                var aSlot = (SlotType)aS;
                var aList = mLists[aS];
                aList.Clear();
                foreach (var aWeapon in mSlots.Weapons)
                {
                    if (aWeapon == null) continue;
                    var aSkills = new List<SkillData>();
                    foreach (var aSkill in mSlots.Database.ForSlot(aSlot))
                    {
                        if (aSkill.Weapon == aWeapon.Weapon) aSkills.Add(aSkill);
                    }
                    if (aSkills.Count == 0) continue;

                    var aGroup = Classed(new VisualElement(), "skill-group");
                    var aDot = Classed(new VisualElement(), "skill-group__dot");
                    aDot.style.backgroundColor = aWeapon.Color;
                    aGroup.Add(aDot);
                    var aGroupName = Classed(new Label(aWeapon.DisplayName), "skill-group__name");
                    aGroupName.style.color = aWeapon.Color;
                    aGroup.Add(aGroupName);
                    aList.Add(aGroup);

                    foreach (var aSkill in aSkills)
                    {
                        var aRow = Classed(new VisualElement(), "skill-row");
                        bool aAvailable = mSlots.IsAvailable(aSkill);
                        aRow.EnableInClassList("skill-row--locked", !aAvailable);
                        aRow.Add(Classed(new Label(aAvailable ? aSkill.DisplayName : "？？？"), "skill-row__name"));
                        aRow.Add(Classed(new Label("使用中"), "skill-row__check"));
                        if (aAvailable)
                        {
                            var aCaptured = aSkill;
                            aRow.RegisterCallback<PointerEnterEvent>(_ => { mDetailSkill = aCaptured; RefreshDetail(); });
                            aRow.RegisterCallback<ClickEvent>(_ =>
                            {
                                mSlots.AssignPresetSkill(mEditIndex, aSlot, aCaptured);
                                mDetailSkill = aCaptured;
                                Refresh();
                            });
                        }
                        aList.Add(aRow);
                        mSkillRows.Add((aRow, aSkill, aSlot));
                    }
                }
            }
        }

        void RefreshLists()
        {
            var aPreset = mSlots.Presets[mEditIndex];
            foreach (var (aRow, aSkill, aSlot) in mSkillRows)
            {
                bool aSelected = aPreset.Get(aSlot) == aSkill;
                aRow.EnableInClassList("skill-row--selected", aSelected);
                aRow.Q<Label>(className: "skill-row__check").style.display = aSelected ? DisplayStyle.Flex : DisplayStyle.None;
                if (aSelected) aRow.style.borderLeftColor = WeaponColor(aSkill);
            }
        }

        void RefreshDetail()
        {
            var aPreset = mSlots.Presets[mEditIndex];
            mDetailBonus.text = BonusText(aPreset.Bonus);

            var aSkill = mDetailSkill;
            bool aHas = aSkill != null;
            mDetailEmpty.style.display = aHas ? DisplayStyle.None : DisplayStyle.Flex;
            mDetailName.style.display = aHas ? DisplayStyle.Flex : DisplayStyle.None;
            mDetailMeta.style.display = aHas ? DisplayStyle.Flex : DisplayStyle.None;
            mDetailDesc.style.display = aHas ? DisplayStyle.Flex : DisplayStyle.None;
            mDetailStats.Clear();
            if (!aHas) return;

            var aWeaponData = mSlots.GetWeaponData(aSkill.Weapon);
            mDetailName.text = aSkill.DisplayName;
            mDetailName.style.color = aWeaponData != null ? aWeaponData.Color : Color.white;
            string aBehavior = mBehaviorLabels.TryGetValue(aSkill.Behavior, out var aB) ? aB : aSkill.Behavior.ToString();
            mDetailMeta.text = $"{(aWeaponData != null ? aWeaponData.DisplayName : "")}  /  {mSlotLabels[(int)aSkill.Slot]}  /  {aBehavior}";
            mDetailDesc.text = aSkill.Description;
            foreach (var aLine in StatLines(aSkill)) mDetailStats.Add(Classed(new Label(aLine), "detail__stat"));
        }

        static IEnumerable<string> StatLines(SkillData iSkill)
        {
            var aFirst = iSkill.GetStep(0);
            int aTotalFrames = 0;
            float aTotalDamage = 0f;
            foreach (var aStep in iSkill.Steps)
            {
                aTotalFrames += aStep.StartupFrames + aStep.ActiveFrames + aStep.RecoveryFrames;
                aTotalDamage += aStep.Damage * (iSkill.Behavior == SkillBehavior.Projectile ? Mathf.Max(1, iSkill.ProjectileCount) : 1);
            }
            yield return $"段数 {iSkill.StepCount}   発生 {aFirst.StartupFrames}F   全体 {aTotalFrames}F";
            yield return $"威力(合計) {aTotalDamage:0}   ひるみ {aFirst.Stagger:0}   アーマー削り {aFirst.ArmorBreak:0}";
            if (iSkill.InvulnerableFrames > 0) yield return $"無敵 {iSkill.InvulnerableFrames}F";
            if (!iSkill.UsableInAir) yield return "地上でのみ使える";
            if (iSkill.Behavior == SkillBehavior.ChargeCounter) yield return "長押しで溜め。溜め中に攻撃を受けるとカウンター";
            if (iSkill.ExplosionRadius > 0f) yield return $"爆発半径 {iSkill.ExplosionRadius:0.#}m";
            if (iSkill.ProjectileCount > 1 && iSkill.Behavior == SkillBehavior.Projectile) yield return $"弾数 {iSkill.ProjectileCount}";
        }

        string BonusText(WeaponBonus iBonus)
        {
            var aData = mSlots.GetWeaponData(iBonus.Weapon);
            string aWeaponName = aData != null ? aData.DisplayName : iBonus.Weapon.ToString();
            switch (iBonus.Kind)
            {
                case WeaponBonusKind.Mastery:
                    return $"マスタリー({aWeaponName})  フィニッシャー「{(aData != null && aData.MasteryFinisher != null ? aData.MasteryFinisher.DisplayName : "-")}」";
                case WeaponBonusKind.Synergy:
                    return $"シナジー({aWeaponName})  ひるみ +{Mathf.RoundToInt((aData != null ? aData.SynergyStaggerBonus : 0f) * 100f)}%";
                case WeaponBonusKind.Arsenal:
                    return "アーセナル  スタイル獲得アップ";
                default:
                    return "(スロットが空いています)";
            }
        }

        Color WeaponColor(SkillData iSkill)
        {
            var aData = mSlots.GetWeaponData(iSkill.Weapon);
            return aData != null ? aData.Color : Color.white;
        }

        static string Hex(Color iColor) => ColorUtility.ToHtmlStringRGB(iColor);

        static T Classed<T>(T iElement, string iClassName) where T : VisualElement
        {
            iElement.AddToClassList(iClassName);
            return iElement;
        }
    }
}
