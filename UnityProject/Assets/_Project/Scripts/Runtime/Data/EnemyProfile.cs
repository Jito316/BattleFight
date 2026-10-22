using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    [Serializable]
    public class EnemyAttack
    {
        [FormerlySerializedAs("name")] public string Name = "攻撃";
        [Tooltip("この距離以内で攻撃を始める")]
        [FormerlySerializedAs("range")] public float Range = 2f;
        [FormerlySerializedAs("windup")] public float Windup = 0.7f;
        [FormerlySerializedAs("active")] public float Active = 0.12f;
        [FormerlySerializedAs("recovery")] public float Recovery = 0.9f;
        [FormerlySerializedAs("damage")] public float Damage = 10f;
        [Tooltip("敵のスケールが掛かる")]
        [FormerlySerializedAs("radius")] public float Radius = 1.2f;
        [Tooltip("敵のスケールが掛かる")]
        [FormerlySerializedAs("offset")] public Vector3 Offset = new Vector3(0f, 1f, 1.2f);
        [FormerlySerializedAs("knockback")] public float Knockback = 5f;
        [Tooltip("攻撃判定中に前進する距離")]
        [FormerlySerializedAs("lunge")] public float Lunge;
        [Tooltip("予備動作で跳び、着地時に攻撃する")]
        [FormerlySerializedAs("jumpSlam")] public bool JumpSlam;
        [FormerlySerializedAs("weight")] public float Weight = 1f;

        [Header("遠距離(弾を撃つ)")]
        [FormerlySerializedAs("ranged")] public bool Ranged;
        [Min(1)] [FormerlySerializedAs("projectileCount")] public int ProjectileCount = 1;
        [FormerlySerializedAs("projectileSpread")] public float ProjectileSpread;
        [FormerlySerializedAs("projectileSpeed")] public float ProjectileSpeed = 16f;
        [FormerlySerializedAs("projectileRadius")] public float ProjectileRadius = 0.35f;

        [Header("特殊")]
        [Tooltip("プレイヤーの足元に予告を出し、少し遅れて範囲攻撃を落とす")]
        [FormerlySerializedAs("targetedStrike")] public bool TargetedStrike;
        [FormerlySerializedAs("strikeDelay")] public float StrikeDelay = 0.8f;
        [Tooltip("攻撃すると自分も倒れる(爆弾兵)")]
        [FormerlySerializedAs("selfDestruct")] public bool SelfDestruct;
        [Tooltip("手下を呼ぶ")]
        [FormerlySerializedAs("summon")] public EnemyProfile Summon;
        [Min(0)] [FormerlySerializedAs("summonCount")] public int SummonCount;
        [Tooltip("予備動作の途中で消え、プレイヤーの背後に現れてから攻撃する")]
        [FormerlySerializedAs("teleportBehind")] public bool TeleportBehind;
        [Tooltip("同じ攻撃を続けて出す回数(三連斬なら 2)")]
        [Min(0)] [FormerlySerializedAs("repeat")] public int Repeat;
        [Tooltip("続けて出すときの予備動作(秒)")]
        [FormerlySerializedAs("repeatWindup")] public float RepeatWindup = 0.3f;

        [Tooltip("プレイヤーのスキルから作った攻撃なら、その元のスキル(予備動作中に名前を出す)")]
        [FormerlySerializedAs("sourceSkill")] public SkillData SourceSkill;
    }

    /// <summary>敵が使うプレイヤーのスキルの「型」。武器種ごとに持ち、戦闘中に切り替える(プレイヤーのプリセットと同じ考え方)</summary>
    [Serializable]
    public class EnemySkillSet
    {
        [FormerlySerializedAs("name")] public string Name = "型";
        [FormerlySerializedAs("weapon")] public WeaponType Weapon;
        [Tooltip("武器の色(手に持つ武器と、切り替えたときの演出)")]
        [FormerlySerializedAs("color")] public Color Color = Color.white;
        [FormerlySerializedAs("skills")] public SkillData[] Skills = new SkillData[0];
        [Tooltip("この型のときの弱点。空なら敵の弱点のまま")]
        [FormerlySerializedAs("weaknesses")] public WeaponType[] Weaknesses = new WeaponType[0];
    }

    [CreateAssetMenu(menuName = "BattleFight/Enemy Profile", fileName = "Enemy_")]
    public class EnemyProfile : ScriptableObject
    {
        [FormerlySerializedAs("displayName")] public string DisplayName = "敵";
        [FormerlySerializedAs("isBoss")] public bool IsBoss;
        [Tooltip("引き寄せで動かせない(自分が飛びつく)")]
        [FormerlySerializedAs("heavy")] public bool Heavy;

        [Header("弱点")]
        [Tooltip("この武器種の技で攻撃されると、ダメージとひるみが増える(頭上に表示される)")]
        [FormerlySerializedAs("weaknesses")] public WeaponType[] Weaknesses = new WeaponType[0];

        public bool IsWeakTo(WeaponType iWeapon)
        {
            if (Weaknesses == null) return false;
            foreach (var aWeapon in Weaknesses)
            {
                if (aWeapon == iWeapon) return true;
            }
            return false;
        }

        [Header("体力・アーマー")]
        [FormerlySerializedAs("maxHealth")] public float MaxHealth = 100f;
        [FormerlySerializedAs("maxArmor")] public float MaxArmor;
        [FormerlySerializedAs("armorRegenDelay")] public float ArmorRegenDelay = 6f;
        [Tooltip("この値以上のひるみ値でひるむ")]
        [FormerlySerializedAs("staggerThreshold")] public float StaggerThreshold = 5f;
        [FormerlySerializedAs("staggerDuration")] public float StaggerDuration = 0.45f;
        [FormerlySerializedAs("armorBreakStagger")] public float ArmorBreakStagger = 1.4f;

        [Header("移動")]
        [FormerlySerializedAs("moveSpeed")] public float MoveSpeed = 3.5f;
        [FormerlySerializedAs("turnSpeed")] public float TurnSpeed = 360f;
        [Tooltip("この距離より近づかれると下がる(射手など)。0 なら下がらない")]
        [FormerlySerializedAs("preferredDistance")] public float PreferredDistance;
        [Tooltip("空を飛ぶ(重力を受けず、地面からこの高さに浮く)")]
        [FormerlySerializedAs("flying")] public bool Flying;
        [FormerlySerializedAs("hoverHeight")] public float HoverHeight = 3.5f;

        [Header("攻撃")]
        [FormerlySerializedAs("attackCooldown")] public float AttackCooldown = 1.2f;
        [FormerlySerializedAs("attacks")] public List<EnemyAttack> Attacks = new List<EnemyAttack> { new EnemyAttack() };

        [Header("プレイヤーのスキル")]
        [Tooltip("プレイヤーと同じスキルで攻めてくる(攻撃に足される)。2つ以上あると、戦闘中に型を切り替える")]
        [FormerlySerializedAs("skillSets")] public List<EnemySkillSet> SkillSets = new List<EnemySkillSet>();
        [Tooltip("この秒数ごとに型を切り替える。0 なら切り替えない")]
        [FormerlySerializedAs("skillSwapInterval")] public float SkillSwapInterval = 7f;
        [Tooltip("スキルの威力に掛ける倍率(プレイヤーの技のままだと、敵が使うには強すぎる)")]
        [FormerlySerializedAs("skillDamageScale")] public float SkillDamageScale = 0.5f;
        [Tooltip("スキルの予備動作に掛ける倍率(見てから避けられるように長くする)")]
        [FormerlySerializedAs("skillWindupScale")] public float SkillWindupScale = 2.2f;

        [Header("第二形態(ボス)")]
        [Tooltip("体力がこの割合を下回ると第二形態になる。0 なら第二形態なし")]
        [Range(0f, 1f)] [FormerlySerializedAs("phase2HealthRatio")] public float Phase2HealthRatio;
        [FormerlySerializedAs("phase2SpeedMultiplier")] public float Phase2SpeedMultiplier = 1.3f;
        [Tooltip("攻撃の間隔に掛かる(小さいほど攻撃が多い)")]
        [FormerlySerializedAs("phase2CooldownMultiplier")] public float Phase2CooldownMultiplier = 0.6f;
        [Tooltip("第二形態で増える攻撃")]
        [FormerlySerializedAs("phase2Attacks")] public List<EnemyAttack> Phase2Attacks = new List<EnemyAttack>();
        [FormerlySerializedAs("phase2Message")] public string Phase2Message = "本気になった!";

        [Header("エリート(実行時に EliteMaker が複製に入れる)")]
        [FormerlySerializedAs("elite")] public EliteAffix Elite;
        [Tooltip("エリートの元の敵(分裂したときの分身を作るのに使う)")]
        [FormerlySerializedAs("eliteSource")] public EnemyProfile EliteSource;

        [Header("見た目・報酬")]
        [FormerlySerializedAs("bodyShape")] public PrimitiveType BodyShape = PrimitiveType.Capsule;
        [FormerlySerializedAs("scale")] public float Scale = 1f;
        [FormerlySerializedAs("color")] public Color Color = new Color(0.45f, 0.5f, 0.6f);
        [FormerlySerializedAs("telegraphColor")] public Color TelegraphColor = new Color(1f, 0.3f, 0.1f);
        [Tooltip("倒したときの刻片(基本値に対する倍率)")]
        [FormerlySerializedAs("shardValue")] public float ShardValue = 1f;
    }
}
