namespace BattleFight
{
    public enum SlotType
    {
        AttackA = 0,
        AttackB = 1,
        Movement = 2,
    }

    public enum WeaponType
    {
        Sword = 0,
        Hammer = 1,
        Chain = 2,
        Staff = 3,
        Gun = 4,
        Gauntlet = 5,
    }

    /// <summary>戦闘中の切り替え方式</summary>
    public enum SwapMode
    {
        /// <summary>スロットごとに3候補を順送り</summary>
        Rack,
        /// <summary>3スロットをまとめたプリセットを一括で切り替え</summary>
        Preset,
    }

    /// <summary>スキルの挙動。Standard 以外は SkillExecutor 側に専用の処理がある。</summary>
    public enum SkillBehavior
    {
        /// <summary>段ごとに 開始 → 攻撃判定 → 硬直 を進める通常の技</summary>
        Standard,
        /// <summary>入力方向へ移動しながら攻撃判定を出す(ステップ斬り)</summary>
        DashStrike,
        /// <summary>ボタンを押している間溜め、溜め中に被弾するとカウンター(居合)</summary>
        ChargeCounter,
        /// <summary>1段目で跳び、着地時に2段目を出す(ハンマージャンプ)</summary>
        HammerJump,
        /// <summary>ロックオン対象かグラップルポイントへ飛ぶ(ワイヤー)</summary>
        Grapple,
        /// <summary>軽い敵は引き寄せ、重い敵には自分が飛びつく(引き寄せ)</summary>
        Pull,
        /// <summary>攻撃判定の代わりに弾を撃つ(魔弾・銃・飛ぶ斬撃など)</summary>
        Projectile,
        /// <summary>攻撃判定が前方へ伸びる直線になる(レーザー)</summary>
        Beam,
        /// <summary>ロックオン対象(なければ前方)の足元に、少し遅れて範囲攻撃を落とす(落雷など)</summary>
        TargetedStrike,
        /// <summary>入力方向へ瞬間移動してから段を進める(ブリンク)</summary>
        Blink,
        /// <summary>上向きの速度を得てから段を進める。空中でも使える(ジェットブースト)</summary>
        Boost,
    }

    public enum Team
    {
        Player,
        Enemy,
    }

    public static class FrameTime
    {
        public const float Fps = 60f;

        public static float ToSeconds(int frames) => frames / Fps;
    }
}
