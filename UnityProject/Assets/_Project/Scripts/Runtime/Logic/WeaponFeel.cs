namespace BattleFight
{
    /// <summary>当たったときの見た目の種類(武器ごと)</summary>
    public enum ImpactStyle
    {
        /// <summary>剣: 鋭い斬撃の線</summary>
        Slash,
        /// <summary>大槌: 足元に土煙の輪、大きな揺れ</summary>
        Smash,
        /// <summary>鎖: しなる打撃</summary>
        Lash,
        /// <summary>杖: 魔力のはじけ</summary>
        Arcane,
        /// <summary>銃: 小さな着弾。撃つたびに反動</summary>
        Shot,
        /// <summary>籠手: 短く重い打撃</summary>
        Punch,
    }

    /// <summary>
    /// 武器ごとの手ごたえ。ヒットストップ・画面の揺れ・敵ののけぞり・火花の量を、武器らしく変える。
    /// どれも「1 = ふつう」の倍率。
    /// </summary>
    public readonly struct WeaponFeel
    {
        public readonly float HitstopScale;
        public readonly float ShakeScale;
        public readonly float ReactionScale;
        public readonly int ExtraSparks;
        public readonly float SparkSpeed;
        /// <summary>撃った・振ったときに、プレイヤーが後ろへ押される強さ(銃の反動)</summary>
        public readonly float Recoil;
        /// <summary>近接で当てたときに、プレイヤーが前へ踏み込む強さ</summary>
        public readonly float Lunge;
        /// <summary>敵が押し飛ばされる強さの倍率</summary>
        public readonly float KnockbackScale;
        public readonly ImpactStyle Style;

        public WeaponFeel(float iHitstopScale, float iShakeScale, float iReactionScale, int iExtraSparks, float iSparkSpeed, float iRecoil,
            float iLunge, float iKnockbackScale, ImpactStyle iStyle)
        {
            HitstopScale = iHitstopScale;
            ShakeScale = iShakeScale;
            ReactionScale = iReactionScale;
            ExtraSparks = iExtraSparks;
            SparkSpeed = iSparkSpeed;
            Recoil = iRecoil;
            Lunge = iLunge;
            KnockbackScale = iKnockbackScale;
            Style = iStyle;
        }

        /// <summary>ヒットストップの上限(秒)。重い武器でも、これより長くは止めない</summary>
        public const float MaxHitstop = 0.2f;

        public static WeaponFeel For(WeaponType iWeapon) => iWeapon switch
        {
            WeaponType.Sword => new WeaponFeel(0.8f, 0.8f, 0.8f, 0, 9f, 0f, 0.8f, 1f, ImpactStyle.Slash),
            WeaponType.Hammer => new WeaponFeel(1.8f, 2f, 1.6f, 4, 6f, 0f, 0.3f, 1.5f, ImpactStyle.Smash),
            WeaponType.Chain => new WeaponFeel(1f, 1f, 1f, 1, 8f, 0f, 0.4f, 0.9f, ImpactStyle.Lash),
            WeaponType.Staff => new WeaponFeel(0.9f, 0.9f, 1f, 2, 7f, 0f, 0.4f, 1f, ImpactStyle.Arcane),
            WeaponType.Gun => new WeaponFeel(0.5f, 0.6f, 0.6f, 0, 10f, 1.6f, 0f, 0.6f, ImpactStyle.Shot),
            WeaponType.Gauntlet => new WeaponFeel(0.7f, 1.3f, 1.1f, 1, 8f, 0f, 1.4f, 1.2f, ImpactStyle.Punch),
            _ => new WeaponFeel(1f, 1f, 1f, 0, 7f, 0f, 0.5f, 1f, ImpactStyle.Slash),
        };

        /// <summary>この武器で当てたときのヒットストップ(秒)</summary>
        public float Hitstop(float iBase, bool iArmorBroken) =>
            UnityEngine.Mathf.Min(iBase * HitstopScale * (iArmorBroken ? 2f : 1f), MaxHitstop);
    }
}
