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
