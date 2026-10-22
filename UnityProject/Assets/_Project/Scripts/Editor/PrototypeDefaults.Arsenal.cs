using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// プリセット方式を試すための追加スキル。杖(魔法)・銃(SF)・拳を加え、既存の武器種にも候補を足す。
    /// 数値はたたき台。調整は生成後のアセットで行う。
    /// </summary>
    static partial class PrototypeDefaults
    {
        static void ProjectileSettings(SkillData iS, float iSpeed, float iLifetime, float iRadius, int iCount = 1, float iSpread = 0f,
            float iExplosion = 0f, float iHoming = 0f, bool iPierce = false)
        {
            iS.ProjectileSpeed = iSpeed;
            iS.ProjectileLifetime = iLifetime;
            iS.ProjectileRadius = iRadius;
            iS.ProjectileCount = iCount;
            iS.ProjectileSpread = iSpread;
            iS.ExplosionRadius = iExplosion;
            iS.Homing = iHoming;
            iS.ProjectilePierce = iPierce;
        }

        /// <summary>弾やブーストなど、近接の攻撃判定を使わない段の判定位置</summary>
        static readonly Vector3 mNone = Vector3.zero;

        // ---------- 剣(追加) ----------

        public static void SwordThrust(SkillData iS)
        {
            Setup(iS, "疾風突き", "踏み込みながらの2段突き。リーチが長く、離れた敵にも届く。", SlotType.AttackA, WeaponType.Sword,
                SkillBehavior.Standard,
                Step(8, 4, 16, 12, 18, 6, 22, new Vector3(0, 1, 1.8f), 1.1f, 1.6f, 3f),
                Step(10, 5, 22, 18, 30, 10, 30, new Vector3(0, 1, 2.0f), 1.2f, 2.0f, 6f, iHitstop: 0.06f));
        }

        public static void SwordWave(SkillData iS)
        {
            Setup(iS, "飛燕斬", "斬撃を飛ばす。敵を貫通する。", SlotType.AttackB, WeaponType.Sword, SkillBehavior.Projectile,
                Step(10, 2, 22, 16, 20, 5, 30, mNone, 0f, 0.3f, 3f));
            ProjectileSettings(iS, 30f, 0.8f, 0.7f, iPierce: true);
        }

        public static void SwordPhantom(SkillData iS)
        {
            Setup(iS, "残影", "入力方向へ一瞬で移動する。無敵がある。", SlotType.Movement, WeaponType.Sword, SkillBehavior.Blink,
                Step(0, 2, 10, 0, 0, 0, 0, mNone, 0f, 0f, iCancel: 4));
            iS.DashDistance = 6f;
            iS.InvulnerableFrames = 12;
            iS.AirGravityScale = 0f;
        }

        // ---------- 大槌(追加) ----------

        public static void HammerSpin(SkillData iS)
        {
            Setup(iS, "旋風槌", "大槌を振り回して周囲を3回薙ぎ払う。", SlotType.AttackA, WeaponType.Hammer, SkillBehavior.Standard,
                Step(12, 8, 10, 12, 25, 20, 24, new Vector3(0, 1, 0.3f), 2.4f, 0.3f, 4f, iHitstop: 0.05f),
                Step(6, 8, 10, 12, 25, 20, 24, new Vector3(0, 1, 0.3f), 2.4f, 0.3f, 4f, iHitstop: 0.05f),
                Step(8, 10, 26, 18, 45, 30, 32, new Vector3(0, 1, 0.3f), 2.6f, 0.3f, 7f, iHitstop: 0.08f));
        }

        public static void HammerMeteor(SkillData iS)
        {
            Setup(iS, "隕鉄投げ", "重い塊を投げつけ、当たると爆発する。アーマーを大きく削る。", SlotType.AttackB, WeaponType.Hammer,
                SkillBehavior.Projectile,
                Step(18, 2, 28, 30, 60, 50, 45, mNone, 0f, 0f, 8f, iLaunch: 6f, iHitstop: 0.1f));
            ProjectileSettings(iS, 16f, 1.2f, 0.6f, iExplosion: 3f);
        }

        public static void HammerCharge(SkillData iS)
        {
            Setup(iS, "突撃", "大槌を構えて長い距離を突進し、ぶつかった敵を吹き飛ばす。", SlotType.Movement, WeaponType.Hammer,
                SkillBehavior.DashStrike,
                Step(4, 16, 16, 14, 40, 25, 22, new Vector3(0, 1, 0.8f), 1.3f, 8f, 9f, iCancel: 8));
            iS.UsableInAir = false;
            iS.InvulnerableFrames = 6;
        }

        // ---------- 鎖(追加) ----------

        public static void ChainRing(SkillData iS)
        {
            Setup(iS, "鎖斬輪", "鎖を大きく回して、周囲を広く2回斬る。", SlotType.AttackA, WeaponType.Chain, SkillBehavior.Standard,
                Step(9, 6, 12, 8, 14, 4, 20, new Vector3(0, 1, 0), 3.4f, 0.2f, 2f),
                Step(10, 8, 24, 14, 28, 8, 28, new Vector3(0, 1, 0), 3.4f, 0.2f, 5f, iHitstop: 0.06f));
        }

        public static void ChainBind(SkillData iS)
        {
            Setup(iS, "縛鎖", "敵を追う鎖を飛ばす。威力は低いが、大きくひるませる。", SlotType.AttackB, WeaponType.Chain,
                SkillBehavior.Projectile,
                Step(6, 2, 20, 8, 80, 10, 32, mNone, 0f, 0f, 0f));
            ProjectileSettings(iS, 40f, 0.5f, 0.5f, iHoming: 360f);
        }

        // ---------- 杖(魔法) ----------

        public static void StaffBolt(SkillData iS)
        {
            Setup(iS, "魔弾", "ロックオン対象を追う魔力の弾を3連射する。", SlotType.AttackA, WeaponType.Staff, SkillBehavior.Projectile,
                Step(6, 2, 10, 7, 10, 2, 16, mNone, 0f, 0f, 1f),
                Step(6, 2, 10, 7, 10, 2, 16, mNone, 0f, 0f, 1f),
                Step(8, 2, 20, 12, 20, 4, 22, mNone, 0f, 0f, 3f));
            ProjectileSettings(iS, 32f, 1f, 0.3f, iHoming: 200f);
        }

        public static void StaffMelee(SkillData iS)
        {
            Setup(iS, "杖術", "杖で打つ3段の近接攻撃。最後の一撃で打ち上げる。", SlotType.AttackA, WeaponType.Staff, SkillBehavior.Standard,
                Step(7, 4, 14, 8, 12, 5, 18, new Vector3(0, 1, 1.5f), 1.4f),
                Step(7, 4, 14, 8, 12, 5, 18, new Vector3(0, 1, 1.5f), 1.4f),
                Step(10, 6, 22, 14, 28, 8, 26, new Vector3(0, 1, 1.5f), 1.5f, 0.6f, 2f, iLaunch: 9f, iHitstop: 0.06f));
        }

        public static void StaffFireball(SkillData iS)
        {
            Setup(iS, "火球", "ゆっくり飛ぶ火の玉。当たると爆発して周りも巻き込む。", SlotType.AttackB, WeaponType.Staff,
                SkillBehavior.Projectile,
                Step(16, 2, 26, 26, 40, 20, 40, mNone, 0f, 0f, 6f, iHitstop: 0.08f));
            ProjectileSettings(iS, 18f, 1.4f, 0.5f, iExplosion: 3.2f);
        }

        public static void StaffThunder(SkillData iS)
        {
            Setup(iS, "落雷", "ロックオン対象(いなければ前方)の足元に、少し遅れて雷を落とす。", SlotType.AttackB, WeaponType.Staff,
                SkillBehavior.TargetedStrike,
                Step(14, 2, 24, 30, 50, 25, 45, mNone, 0f, 0f, 2f, iLaunch: 8f, iHitstop: 0.1f));
            iS.ExplosionRadius = 2.6f;
            iS.StrikeDelay = 0.35f;
            iS.StrikeDistance = 9f;
        }

        public static void StaffFrost(SkillData iS)
        {
            Setup(iS, "氷結陣", "足元から冷気を広げる。威力は控えめだが、周りの敵を長くひるませる。", SlotType.AttackB, WeaponType.Staff,
                SkillBehavior.Standard,
                Step(20, 6, 28, 18, 90, 15, 40, new Vector3(0, 0.5f, 0), 4.5f, 0f, 1f, iHitstop: 0.08f));
        }

        public static void StaffBlink(SkillData iS)
        {
            Setup(iS, "ブリンク", "入力方向へ長い距離を瞬間移動する。", SlotType.Movement, WeaponType.Staff, SkillBehavior.Blink,
                Step(0, 2, 8, 0, 0, 0, 0, mNone, 0f, 0f, iCancel: 3));
            iS.DashDistance = 8f;
            iS.InvulnerableFrames = 10;
            iS.AirGravityScale = 0f;
        }

        public static void StaffLevitate(SkillData iS)
        {
            Setup(iS, "浮遊", "ふわりと浮き上がる。落ちるのが遅くなり、空中から魔法を撃ちやすい。", SlotType.Movement, WeaponType.Staff,
                SkillBehavior.Boost,
                Step(2, 2, 14, 0, 0, 0, 0, mNone, 0f, 3f, iCancel: 4));
            iS.JumpVelocity = 8f;
            iS.AirGravityScale = 0.25f;
        }

        public static void StaffFinisher(SkillData iS)
        {
            Setup(iS, "メテオ", "杖マスタリーのフィニッシャー。狙った場所に隕石を3つ落とす。", SlotType.AttackA, WeaponType.Staff,
                SkillBehavior.TargetedStrike,
                Step(10, 2, 8, 30, 60, 40, 40, mNone, 0f, 0f, 4f, iLaunch: 6f),
                Step(0, 2, 8, 30, 60, 40, 40, mNone, 0f, 0f, 4f, iLaunch: 6f),
                Step(0, 2, 30, 50, 100, 60, 60, mNone, 0f, 0f, 8f, iLaunch: 10f, iHitstop: 0.12f));
            iS.AutoChain = true;
            iS.ExplosionRadius = 4f;
            iS.StrikeDelay = 0.5f;
            iS.StrikeDistance = 9f;
            iS.InvulnerableFrames = 40;
        }

        // ---------- 銃(SF) ----------

        public static void GunRapid(SkillData iS)
        {
            Setup(iS, "ラピッドショット", "高速の弾を4連射する。1発は軽いが隙が小さい。", SlotType.AttackA, WeaponType.Gun, SkillBehavior.Projectile,
                Step(4, 1, 6, 5, 6, 2, 12, mNone, 0f, 0f, 0.5f, iHitstop: 0.02f),
                Step(4, 1, 6, 5, 6, 2, 12, mNone, 0f, 0f, 0.5f, iHitstop: 0.02f),
                Step(4, 1, 6, 5, 6, 2, 12, mNone, 0f, 0f, 0.5f, iHitstop: 0.02f),
                Step(6, 1, 16, 8, 14, 3, 16, mNone, 0f, 0f, 2f, iHitstop: 0.03f));
            ProjectileSettings(iS, 55f, 0.6f, 0.2f);
        }

        public static void GunShotgun(SkillData iS)
        {
            Setup(iS, "ショットガン", "扇状に散弾を撃つ。近いほど多く当たる。", SlotType.AttackA, WeaponType.Gun, SkillBehavior.Projectile,
                Step(8, 1, 20, 6, 10, 4, 12, mNone, 0f, 0f, 3f, iHitstop: 0.04f),
                Step(10, 1, 26, 7, 12, 5, 14, mNone, 0f, 0f, 6f, iHitstop: 0.05f));
            ProjectileSettings(iS, 40f, 0.3f, 0.3f, iCount: 7, iSpread: 30f);
        }

        public static void GunLaser(SkillData iS)
        {
            Setup(iS, "レーザー", "前方へ貫通するレーザーを照射し続ける。", SlotType.AttackB, WeaponType.Gun, SkillBehavior.Beam,
                Step(14, 4, 0, 6, 10, 5, 12, mNone, 0f, 0f, 0.5f, iHitstop: 0.02f),
                Step(0, 4, 0, 6, 10, 5, 12, mNone, 0f, 0f, 0.5f, iHitstop: 0.02f),
                Step(0, 4, 0, 6, 10, 5, 12, mNone, 0f, 0f, 0.5f, iHitstop: 0.02f),
                Step(0, 6, 24, 12, 30, 10, 20, mNone, 0f, 0f, 5f, iHitstop: 0.05f));
            iS.AutoChain = true;
            iS.BeamLength = 18f;
            iS.BeamRadius = 0.45f;
        }

        public static void GunGrenade(SkillData iS)
        {
            Setup(iS, "グレネード", "爆発する弾を撃ち出す。当たった周りの敵を打ち上げる。", SlotType.AttackB, WeaponType.Gun,
                SkillBehavior.Projectile,
                Step(12, 2, 22, 24, 45, 25, 38, mNone, 0f, 0f, 4f, iLaunch: 10f, iHitstop: 0.08f));
            ProjectileSettings(iS, 14f, 0.9f, 0.35f, iExplosion: 3.5f);
        }

        public static void GunJet(SkillData iS)
        {
            Setup(iS, "ジェットブースト", "背中のジェットで斜め上へ加速する。空中でも使える。", SlotType.Movement, WeaponType.Gun, SkillBehavior.Boost,
                Step(2, 10, 10, 0, 0, 0, 0, mNone, 0f, 7f, iCancel: 4));
            iS.JumpVelocity = 6f;
            iS.AirGravityScale = 0.1f;
            iS.InvulnerableFrames = 8;
        }

        public static void GunSlide(SkillData iS)
        {
            Setup(iS, "スライド", "低い姿勢で滑り込む。長い無敵で攻撃をくぐり抜ける。", SlotType.Movement, WeaponType.Gun, SkillBehavior.DashStrike,
                Step(2, 14, 10, 4, 20, 2, 12, new Vector3(0, 0.6f, 0.5f), 1.0f, 7f, 2f, iCancel: 4));
            iS.UsableInAir = false;
            iS.InvulnerableFrames = 14;
        }

        public static void GunFinisher(SkillData iS)
        {
            Setup(iS, "オーバードライブ", "銃マスタリーのフィニッシャー。全方位へ弾幕を3回撃つ。", SlotType.AttackA, WeaponType.Gun,
                SkillBehavior.Projectile,
                Step(10, 1, 6, 10, 20, 8, 25, mNone, 0f, 0f, 3f),
                Step(0, 1, 6, 10, 20, 8, 25, mNone, 0f, 0f, 3f),
                Step(0, 1, 30, 16, 50, 15, 40, mNone, 0f, 0f, 6f, iHitstop: 0.08f));
            iS.AutoChain = true;
            iS.InvulnerableFrames = 50;
            ProjectileSettings(iS, 35f, 0.8f, 0.35f, iCount: 12, iSpread: 360f, iPierce: true);
        }

        // ---------- 拳 ----------

        public static void GauntletRush(SkillData iS)
        {
            Setup(iS, "連打", "素早い5連打。最後の一撃で大きく押し込む。", SlotType.AttackA, WeaponType.Gauntlet, SkillBehavior.Standard,
                Step(4, 3, 8, 5, 8, 3, 12, new Vector3(0, 1, 1.1f), 1.1f, 0.4f, 1f, iHitstop: 0.02f, iCancel: 4),
                Step(4, 3, 8, 5, 8, 3, 12, new Vector3(0, 1, 1.1f), 1.1f, 0.4f, 1f, iHitstop: 0.02f, iCancel: 4),
                Step(4, 3, 8, 5, 8, 3, 12, new Vector3(0, 1, 1.1f), 1.1f, 0.4f, 1f, iHitstop: 0.02f, iCancel: 4),
                Step(4, 3, 8, 5, 8, 3, 12, new Vector3(0, 1, 1.1f), 1.1f, 0.4f, 1f, iHitstop: 0.02f, iCancel: 4),
                Step(8, 4, 20, 12, 30, 8, 22, new Vector3(0, 1, 1.2f), 1.3f, 0.8f, 6f, iHitstop: 0.07f));
        }

        public static void GauntletUppercut(SkillData iS)
        {
            Setup(iS, "昇龍拳", "飛び上がりながらのアッパー。敵と一緒に空中へ上がり、空中コンボにつなげる。", SlotType.AttackB,
                WeaponType.Gauntlet, SkillBehavior.Boost,
                Step(8, 6, 26, 20, 50, 20, 36, new Vector3(0, 1.2f, 1.1f), 1.3f, 0.8f, 1f, iLaunch: 14f, iHitstop: 0.09f));
            iS.JumpVelocity = 12f;
            iS.AirGravityScale = 0.3f;
        }

        public static void GauntletRocket(SkillData iS)
        {
            Setup(iS, "ロケットパンチ", "拳を撃ち出す。重く、ひるみとアーマー削りに優れる。", SlotType.AttackB, WeaponType.Gauntlet,
                SkillBehavior.Projectile,
                Step(12, 2, 24, 22, 55, 35, 34, mNone, 0f, 0f, 10f, iHitstop: 0.08f));
            ProjectileSettings(iS, 34f, 0.7f, 0.6f);
        }

        public static void GauntletDash(SkillData iS)
        {
            Setup(iS, "ロケットダッシュ", "拳を突き出したまま急加速する。ぶつかった敵を吹き飛ばす。", SlotType.Movement, WeaponType.Gauntlet,
                SkillBehavior.DashStrike,
                Step(3, 12, 12, 10, 35, 15, 20, new Vector3(0, 1, 0.8f), 1.2f, 7f, 8f, iCancel: 5));
            iS.InvulnerableFrames = 8;
            iS.AirGravityScale = 0f;
        }

        public static void GauntletFinisher(SkillData iS)
        {
            var aSteps = new SkillStep[8];
            for (int aI = 0; aI < 7; aI++) aSteps[aI] = Step(aI == 0 ? 6 : 1, 2, 1, 6, 15, 6, 10, new Vector3(0, 1, 1.2f), 1.6f, 0.1f, 0.5f,
                iHitstop: 0.02f);
            aSteps[7] = Step(6, 4, 30, 30, 100, 40, 50, new Vector3(0, 1, 1.3f), 1.8f, 0.5f, 12f, iLaunch: 8f, iHitstop: 0.14f);
            Setup(iS, "百裂拳", "拳マスタリーのフィニッシャー。目にも止まらぬ連打を叩き込む。", SlotType.AttackA, WeaponType.Gauntlet,
                SkillBehavior.Standard, aSteps);
            iS.AutoChain = true;
            iS.InvulnerableFrames = 60;
        }

        // ---------- 武器種(追加) ----------

        public static void Staff(WeaponTypeData iW, SkillData iFinisher)
        {
            iW.Weapon = WeaponType.Staff;
            iW.DisplayName = "杖";
            iW.Color = new Color(0.3f, 1f, 0.75f);
            iW.HeadShape = PrimitiveType.Sphere;
            iW.HeadScale = new Vector3(0.24f, 0.24f, 0.24f);
            iW.HeadOffset = new Vector3(0f, 0f, 1.5f);
            iW.HandleLength = 1.4f;
            iW.MasteryFinisher = iFinisher;
            iW.SynergyStaggerBonus = 0.1f;
        }

        public static void Gun(WeaponTypeData iW, SkillData iFinisher)
        {
            iW.Weapon = WeaponType.Gun;
            iW.DisplayName = "銃";
            iW.Color = new Color(1f, 0.9f, 0.25f);
            iW.HeadShape = PrimitiveType.Cube;
            iW.HeadScale = new Vector3(0.12f, 0.18f, 0.55f);
            iW.HeadOffset = new Vector3(0f, 0.05f, 0.3f);
            iW.HandleLength = 0f;
            iW.MasteryFinisher = iFinisher;
            iW.SynergyStaggerBonus = 0.1f;
        }

        public static void Gauntlet(WeaponTypeData iW, SkillData iFinisher)
        {
            iW.Weapon = WeaponType.Gauntlet;
            iW.DisplayName = "拳";
            iW.Color = new Color(1f, 0.35f, 0.45f);
            iW.HeadShape = PrimitiveType.Cube;
            iW.HeadScale = new Vector3(0.28f, 0.28f, 0.32f);
            iW.HeadOffset = new Vector3(0f, 0f, 0.12f);
            iW.HandleLength = 0f;
            iW.MasteryFinisher = iFinisher;
            iW.SynergyStaggerBonus = 0.15f;
        }
    }
}
