namespace BattleFight
{
    /// <summary>
    /// 演出を出す場面。EffectLibrary で場面ごとにプレハブを決めると、いつもの演出(コードで作る仮の演出)の代わりにそれを出す。
    /// 番号はデータに保存されるので、足すときは最後に足す(途中に入れない・消さない)。
    /// </summary>
    public enum EffectCue
    {
        // ---------- プレイヤーの攻撃 ----------
        /// <summary>攻撃が当たった火花</summary>
        HitSpark = 0,
        /// <summary>武器ごとの当たりの形(剣の斬撃の線・大槌の土煙・杖の魔力の輪・籠手の衝撃)</summary>
        HitImpact = 1,
        /// <summary>敵のアーマーが砕けた</summary>
        ArmorBreak = 2,
        /// <summary>敵を倒した(砕け散る。刻片が飛んでくるのはそのまま)</summary>
        Defeat = 3,
        /// <summary>振ったときの斬撃の弧</summary>
        SwingArc = 4,
        /// <summary>広い範囲の技の、地面の輪</summary>
        AreaAttack = 5,
        /// <summary>ビーム(大きさはビームの太さ。向きは撃った向き)</summary>
        Beam = 6,
        /// <summary>プレイヤーの弾の爆発</summary>
        ProjectileBurst = 7,
        /// <summary>居合のカウンターが決まった</summary>
        Counter = 8,

        // ---------- プレイヤーの動き ----------
        /// <summary>移動スキル(回避)の出だしの土煙</summary>
        DodgeStart = 20,
        /// <summary>ブリンクで消えた場所 / 現れた場所</summary>
        BlinkOut = 21,
        BlinkIn = 22,
        /// <summary>ジャンプの踏み切り</summary>
        Jump = 23,
        /// <summary>着地(大きさは落ちた速さで変わる)</summary>
        Land = 24,
        /// <summary>武器の持ち替え</summary>
        WeaponSwap = 25,
        /// <summary>チェンジアタック / チェンジドライブ</summary>
        ChangeAttack = 26,
        ChangeDrive = 27,

        // ---------- 守り ----------
        /// <summary>ガードで受けた</summary>
        Guard = 40,
        /// <summary>パリィを決めた</summary>
        Parry = 41,
        /// <summary>ジャスト回避(見切り)を決めた</summary>
        JustDodge = 42,
        /// <summary>プレイヤーが攻撃を受けた</summary>
        PlayerHurt = 43,

        // ---------- 敵 ----------
        /// <summary>敵の攻撃の直前のきらめき(パリィ・見切りの合図)</summary>
        AttackGlint = 60,
        /// <summary>敵の近接攻撃が地面に当たった</summary>
        EnemyAttackLand = 61,
        /// <summary>敵が瞬間移動で消えた・現れた</summary>
        EnemyTeleport = 62,
        /// <summary>敵が仲間を呼んだ</summary>
        EnemySummon = 63,
        /// <summary>ボスが第二形態になった</summary>
        EnemyPhase2 = 64,
        /// <summary>写し身が型を切り替えた</summary>
        EnemySkillSwap = 65,
        /// <summary>敵の弾の爆発</summary>
        EnemyProjectileBurst = 66,

        // ---------- 探索 ----------
        /// <summary>スキルを拾った</summary>
        SkillPickup = 80,
        /// <summary>壊せる壁を壊した</summary>
        WallBreak = 81,
    }
}
