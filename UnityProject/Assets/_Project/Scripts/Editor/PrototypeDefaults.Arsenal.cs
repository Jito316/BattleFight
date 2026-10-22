using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// プリセット方式を試すための追加スキル。杖(魔法)・銃(SF)・拳を加え、既存の武器種にも候補を足す。
    /// 数値はたたき台。調整は生成後のアセットで行う。
    /// </summary>
    static partial class PrototypeDefaults
    {
        static void ProjectileSettings(SkillData iSkill, float iSpeed, float iLifetime, float iRadius, int iCount = 1, float iSpread = 0f,
            float iExplosion = 0f, float iHoming = 0f, bool iPierce = false)
        {
            iSkill.ProjectileSpeed = iSpeed;
            iSkill.ProjectileLifetime = iLifetime;
            iSkill.ProjectileRadius = iRadius;
            iSkill.ProjectileCount = iCount;
            iSkill.ProjectileSpread = iSpread;
            iSkill.ExplosionRadius = iExplosion;
            iSkill.Homing = iHoming;
            iSkill.ProjectilePierce = iPierce;
        }

        /// <summary>弾やブーストなど、近接の攻撃判定を使わない段の判定位置</summary>
        static readonly Vector3 mNone = Vector3.zero;

        // ---------- 剣(追加) ----------

        public static void SwordThrust(SkillData iSkill)
        {
            Setup(iSkill, "疾風突き", "踏み込みながらの2段突き。リーチが長く、離れた敵にも届く。", SlotType.AttackA, WeaponType.Sword,
                SkillBehavior.Standard,
                Step(8, 4, 16, 12, 18, 6, 22, new Vector3(0, 1, 1.8f), 1.1f, 1.6f, 3f),
                Step(10, 5, 22, 18, 30, 10, 30, new Vector3(0, 1, 2.0f), 1.2f, 2.0f, 6f, iHitstop: 0.06f));
        }

        public static void SwordWave(SkillData iSkill)
        {
            Setup(iSkill, "飛燕斬", "斬撃を飛ばす。敵を貫通する。", SlotType.AttackB, WeaponType.Sword, SkillBehavior.Projectile,
                Step(10, 2, 22, 16, 20, 5, 30, mNone, 0f, 0.3f, 3f));
            ProjectileSettings(iSkill, 30f, 0.8f, 0.7f, iPierce: true);
        }

        public static void SwordPhantom(SkillData iSkill)
        {
            Setup(iSkill, "残影", "入力方向へ一瞬で移動する。無敵がある。", SlotType.Movement, WeaponType.Sword, SkillBehavior.Blink,
                Step(0, 2, 10, 0, 0, 0, 0, mNone, 0f, 0f, iCancel: 4));
            iSkill.DashDistance = 6f;
            iSkill.InvulnerableFrames = 12;
            iSkill.AirGravityScale = 0f;
        }

        // ---------- 大槌(追加) ----------

        public static void HammerSpin(SkillData iSkill)
        {
            Setup(iSkill, "旋風槌", "大槌を振り回して周囲を3回薙ぎ払う。", SlotType.AttackA, WeaponType.Hammer, SkillBehavior.Standard,
                Step(12, 8, 10, 12, 25, 20, 24, new Vector3(0, 1, 0.3f), 2.4f, 0.3f, 4f, iHitstop: 0.05f),
                Step(6, 8, 10, 12, 25, 20, 24, new Vector3(0, 1, 0.3f), 2.4f, 0.3f, 4f, iHitstop: 0.05f),
                Step(8, 10, 26, 18, 45, 30, 32, new Vector3(0, 1, 0.3f), 2.6f, 0.3f, 7f, iHitstop: 0.08f));
        }

        public static void HammerMeteor(SkillData iSkill)
        {
            Setup(iSkill, "隕鉄投げ", "重い塊を投げつけ、当たると爆発する。アーマーを大きく削る。", SlotType.AttackB, WeaponType.Hammer,
                SkillBehavior.Projectile,
                Step(18, 2, 28, 30, 60, 50, 45, mNone, 0f, 0f, 8f, iLaunch: 6f, iHitstop: 0.1f));
            ProjectileSettings(iSkill, 16f, 1.2f, 0.6f, iExplosion: 3f);
        }

        public static void HammerCharge(SkillData iSkill)
        {
            Setup(iSkill, "突撃", "大槌を構えて長い距離を突進し、ぶつかった敵を吹き飛ばす。", SlotType.Movement, WeaponType.Hammer,
                SkillBehavior.DashStrike,
                Step(4, 16, 16, 14, 40, 25, 22, new Vector3(0, 1, 0.8f), 1.3f, 8f, 9f, iCancel: 8));
            iSkill.UsableInAir = false;
            iSkill.InvulnerableFrames = 6;
        }

        // ---------- 鎖(追加) ----------

        public static void ChainRing(SkillData iSkill)
        {
            Setup(iSkill, "鎖斬輪", "鎖を大きく回して、周囲を広く2回斬る。", SlotType.AttackA, WeaponType.Chain, SkillBehavior.Standard,
                Step(9, 6, 12, 8, 14, 4, 20, new Vector3(0, 1, 0), 3.4f, 0.2f, 2f),
                Step(10, 8, 24, 14, 28, 8, 28, new Vector3(0, 1, 0), 3.4f, 0.2f, 5f, iHitstop: 0.06f));
        }

        public static void ChainBind(SkillData iSkill)
        {
            Setup(iSkill, "縛鎖", "敵を追う鎖を飛ばす。威力は低いが、大きくひるませる。", SlotType.AttackB, WeaponType.Chain,
                SkillBehavior.Projectile,
                Step(6, 2, 20, 8, 80, 10, 32, mNone, 0f, 0f, 0f));
            ProjectileSettings(iSkill, 40f, 0.5f, 0.5f, iHoming: 360f);
        }

        // ---------- 杖(魔法) ----------

        public static void StaffBolt(SkillData iSkill)
        {
            Setup(iSkill, "魔弾", "ロックオン対象を追う魔力の弾を3連射する。", SlotType.AttackA, WeaponType.Staff, SkillBehavior.Projectile,
                Step(6, 2, 10, 7, 10, 2, 16, mNone, 0f, 0f, 1f),
                Step(6, 2, 10, 7, 10, 2, 16, mNone, 0f, 0f, 1f),
                Step(8, 2, 20, 12, 20, 4, 22, mNone, 0f, 0f, 3f));
            ProjectileSettings(iSkill, 32f, 1f, 0.3f, iHoming: 200f);
        }

        public static void StaffMelee(SkillData iSkill)
        {
            Setup(iSkill, "杖術", "杖で打つ3段の近接攻撃。最後の一撃で打ち上げる。", SlotType.AttackA, WeaponType.Staff, SkillBehavior.Standard,
                Step(7, 4, 14, 8, 12, 5, 18, new Vector3(0, 1, 1.5f), 1.4f),
                Step(7, 4, 14, 8, 12, 5, 18, new Vector3(0, 1, 1.5f), 1.4f),
                Step(10, 6, 22, 14, 28, 8, 26, new Vector3(0, 1, 1.5f), 1.5f, 0.6f, 2f, iLaunch: 9f, iHitstop: 0.06f));
        }

        public static void StaffFireball(SkillData iSkill)
        {
            Setup(iSkill, "火球", "ゆっくり飛ぶ火の玉。当たると爆発して周りも巻き込む。", SlotType.AttackB, WeaponType.Staff,
                SkillBehavior.Projectile,
                Step(16, 2, 26, 26, 40, 20, 40, mNone, 0f, 0f, 6f, iHitstop: 0.08f));
            ProjectileSettings(iSkill, 18f, 1.4f, 0.5f, iExplosion: 3.2f);
        }

        public static void StaffThunder(SkillData iSkill)
        {
            Setup(iSkill, "落雷", "ロックオン対象(いなければ前方)の足元に、少し遅れて雷を落とす。", SlotType.AttackB, WeaponType.Staff,
                SkillBehavior.TargetedStrike,
                Step(14, 2, 24, 30, 50, 25, 45, mNone, 0f, 0f, 2f, iLaunch: 8f, iHitstop: 0.1f));
            iSkill.ExplosionRadius = 2.6f;
            iSkill.StrikeDelay = 0.35f;
            iSkill.StrikeDistance = 9f;
        }

        public static void StaffFrost(SkillData iSkill)
        {
            Setup(iSkill, "氷結陣", "足元から冷気を広げる。威力は控えめだが、周りの敵を長くひるませる。", SlotType.AttackB, WeaponType.Staff,
                SkillBehavior.Standard,
                Step(20, 6, 28, 18, 90, 15, 40, new Vector3(0, 0.5f, 0), 4.5f, 0f, 1f, iHitstop: 0.08f));
        }

        public static void StaffBlink(SkillData iSkill)
        {
            Setup(iSkill, "ブリンク", "入力方向へ長い距離を瞬間移動する。", SlotType.Movement, WeaponType.Staff, SkillBehavior.Blink,
                Step(0, 2, 8, 0, 0, 0, 0, mNone, 0f, 0f, iCancel: 3));
            iSkill.DashDistance = 8f;
            iSkill.InvulnerableFrames = 10;
            iSkill.AirGravityScale = 0f;
        }

        public static void StaffLevitate(SkillData iSkill)
        {
            Setup(iSkill, "浮遊", "ふわりと浮き上がる。落ちるのが遅くなり、空中から魔法を撃ちやすい。", SlotType.Movement, WeaponType.Staff,
                SkillBehavior.Boost,
                Step(2, 2, 14, 0, 0, 0, 0, mNone, 0f, 3f, iCancel: 4));
            iSkill.JumpVelocity = 8f;
            iSkill.AirGravityScale = 0.25f;
        }

        public static void StaffFinisher(SkillData iSkill)
        {
            Setup(iSkill, "メテオ", "杖マスタリーのフィニッシャー。狙った場所に隕石を3つ落とす。", SlotType.AttackA, WeaponType.Staff,
                SkillBehavior.TargetedStrike,
                Step(10, 2, 8, 30, 60, 40, 40, mNone, 0f, 0f, 4f, iLaunch: 6f),
                Step(0, 2, 8, 30, 60, 40, 40, mNone, 0f, 0f, 4f, iLaunch: 6f),
                Step(0, 2, 30, 50, 100, 60, 60, mNone, 0f, 0f, 8f, iLaunch: 10f, iHitstop: 0.12f));
            iSkill.AutoChain = true;
            iSkill.ExplosionRadius = 4f;
            iSkill.StrikeDelay = 0.5f;
            iSkill.StrikeDistance = 9f;
            iSkill.InvulnerableFrames = 40;
        }

        // ---------- 銃(SF) ----------

        public static void GunRapid(SkillData iSkill)
        {
            Setup(iSkill, "ラピッドショット", "高速の弾を4連射する。1発は軽いが隙が小さい。", SlotType.AttackA, WeaponType.Gun, SkillBehavior.Projectile,
                Step(4, 1, 6, 5, 6, 2, 12, mNone, 0f, 0f, 0.5f, iHitstop: 0.02f),
                Step(4, 1, 6, 5, 6, 2, 12, mNone, 0f, 0f, 0.5f, iHitstop: 0.02f),
                Step(4, 1, 6, 5, 6, 2, 12, mNone, 0f, 0f, 0.5f, iHitstop: 0.02f),
                Step(6, 1, 16, 8, 14, 3, 16, mNone, 0f, 0f, 2f, iHitstop: 0.03f));
            ProjectileSettings(iSkill, 55f, 0.6f, 0.2f);
        }

        public static void GunShotgun(SkillData iSkill)
        {
            Setup(iSkill, "ショットガン", "扇状に散弾を撃つ。近いほど多く当たる。", SlotType.AttackA, WeaponType.Gun, SkillBehavior.Projectile,
                Step(8, 1, 20, 6, 10, 4, 12, mNone, 0f, 0f, 3f, iHitstop: 0.04f),
                Step(10, 1, 26, 7, 12, 5, 14, mNone, 0f, 0f, 6f, iHitstop: 0.05f));
            ProjectileSettings(iSkill, 40f, 0.3f, 0.3f, iCount: 7, iSpread: 30f);
        }

        public static void GunLaser(SkillData iSkill)
        {
            Setup(iSkill, "レーザー", "前方へ貫通するレーザーを照射し続ける。", SlotType.AttackB, WeaponType.Gun, SkillBehavior.Beam,
                Step(14, 4, 0, 6, 10, 5, 12, mNone, 0f, 0f, 0.5f, iHitstop: 0.02f),
                Step(0, 4, 0, 6, 10, 5, 12, mNone, 0f, 0f, 0.5f, iHitstop: 0.02f),
                Step(0, 4, 0, 6, 10, 5, 12, mNone, 0f, 0f, 0.5f, iHitstop: 0.02f),
                Step(0, 6, 24, 12, 30, 10, 20, mNone, 0f, 0f, 5f, iHitstop: 0.05f));
            iSkill.AutoChain = true;
            iSkill.BeamLength = 18f;
            iSkill.BeamRadius = 0.45f;
        }

        public static void GunGrenade(SkillData iSkill)
        {
            Setup(iSkill, "グレネード", "爆発する弾を撃ち出す。当たった周りの敵を打ち上げる。", SlotType.AttackB, WeaponType.Gun,
                SkillBehavior.Projectile,
                Step(12, 2, 22, 24, 45, 25, 38, mNone, 0f, 0f, 4f, iLaunch: 10f, iHitstop: 0.08f));
            ProjectileSettings(iSkill, 14f, 0.9f, 0.35f, iExplosion: 3.5f);
        }

        public static void GunJet(SkillData iSkill)
        {
            Setup(iSkill, "ジェットブースト", "背中のジェットで斜め上へ加速する。空中でも使える。", SlotType.Movement, WeaponType.Gun, SkillBehavior.Boost,
                Step(2, 10, 10, 0, 0, 0, 0, mNone, 0f, 7f, iCancel: 4));
            iSkill.JumpVelocity = 6f;
            iSkill.AirGravityScale = 0.1f;
            iSkill.InvulnerableFrames = 8;
        }

        public static void GunSlide(SkillData iSkill)
        {
            Setup(iSkill, "スライド", "低い姿勢で滑り込む。長い無敵で攻撃をくぐり抜ける。", SlotType.Movement, WeaponType.Gun, SkillBehavior.DashStrike,
                Step(2, 14, 10, 4, 20, 2, 12, new Vector3(0, 0.6f, 0.5f), 1.0f, 7f, 2f, iCancel: 4));
            iSkill.UsableInAir = false;
            iSkill.InvulnerableFrames = 14;
        }

        public static void GunFinisher(SkillData iSkill)
        {
            Setup(iSkill, "オーバードライブ", "銃マスタリーのフィニッシャー。全方位へ弾幕を3回撃つ。", SlotType.AttackA, WeaponType.Gun,
                SkillBehavior.Projectile,
                Step(10, 1, 6, 10, 20, 8, 25, mNone, 0f, 0f, 3f),
                Step(0, 1, 6, 10, 20, 8, 25, mNone, 0f, 0f, 3f),
                Step(0, 1, 30, 16, 50, 15, 40, mNone, 0f, 0f, 6f, iHitstop: 0.08f));
            iSkill.AutoChain = true;
            iSkill.InvulnerableFrames = 50;
            ProjectileSettings(iSkill, 35f, 0.8f, 0.35f, iCount: 12, iSpread: 360f, iPierce: true);
        }

        // ---------- 拳 ----------

        public static void GauntletRush(SkillData iSkill)
        {
            Setup(iSkill, "連打", "素早い5連打。最後の一撃で大きく押し込む。", SlotType.AttackA, WeaponType.Gauntlet, SkillBehavior.Standard,
                Step(4, 3, 8, 5, 8, 3, 12, new Vector3(0, 1, 1.1f), 1.1f, 0.4f, 1f, iHitstop: 0.02f, iCancel: 4),
                Step(4, 3, 8, 5, 8, 3, 12, new Vector3(0, 1, 1.1f), 1.1f, 0.4f, 1f, iHitstop: 0.02f, iCancel: 4),
                Step(4, 3, 8, 5, 8, 3, 12, new Vector3(0, 1, 1.1f), 1.1f, 0.4f, 1f, iHitstop: 0.02f, iCancel: 4),
                Step(4, 3, 8, 5, 8, 3, 12, new Vector3(0, 1, 1.1f), 1.1f, 0.4f, 1f, iHitstop: 0.02f, iCancel: 4),
                Step(8, 4, 20, 12, 30, 8, 22, new Vector3(0, 1, 1.2f), 1.3f, 0.8f, 6f, iHitstop: 0.07f));
        }

        public static void GauntletUppercut(SkillData iSkill)
        {
            Setup(iSkill, "昇龍拳", "飛び上がりながらのアッパー。敵と一緒に空中へ上がり、空中コンボにつなげる。", SlotType.AttackB,
                WeaponType.Gauntlet, SkillBehavior.Boost,
                Step(8, 6, 26, 20, 50, 20, 36, new Vector3(0, 1.2f, 1.1f), 1.3f, 0.8f, 1f, iLaunch: 14f, iHitstop: 0.09f));
            iSkill.JumpVelocity = 12f;
            iSkill.AirGravityScale = 0.3f;
        }

        public static void GauntletRocket(SkillData iSkill)
        {
            Setup(iSkill, "ロケットパンチ", "拳を撃ち出す。重く、ひるみとアーマー削りに優れる。", SlotType.AttackB, WeaponType.Gauntlet,
                SkillBehavior.Projectile,
                Step(12, 2, 24, 22, 55, 35, 34, mNone, 0f, 0f, 10f, iHitstop: 0.08f));
            ProjectileSettings(iSkill, 34f, 0.7f, 0.6f);
        }

        public static void GauntletDash(SkillData iSkill)
        {
            Setup(iSkill, "ロケットダッシュ", "拳を突き出したまま急加速する。ぶつかった敵を吹き飛ばす。", SlotType.Movement, WeaponType.Gauntlet,
                SkillBehavior.DashStrike,
                Step(3, 12, 12, 10, 35, 15, 20, new Vector3(0, 1, 0.8f), 1.2f, 7f, 8f, iCancel: 5));
            iSkill.InvulnerableFrames = 8;
            iSkill.AirGravityScale = 0f;
        }

        public static void GauntletFinisher(SkillData iSkill)
        {
            var aSteps = new SkillStep[8];
            for (int aIndex = 0; aIndex < 7; aIndex++) aSteps[aIndex] = Step(aIndex == 0 ? 6 : 1, 2, 1, 6, 15, 6, 10, new Vector3(0, 1, 1.2f), 1.6f, 0.1f, 0.5f,
                iHitstop: 0.02f);
            aSteps[7] = Step(6, 4, 30, 30, 100, 40, 50, new Vector3(0, 1, 1.3f), 1.8f, 0.5f, 12f, iLaunch: 8f, iHitstop: 0.14f);
            Setup(iSkill, "百裂拳", "拳マスタリーのフィニッシャー。目にも止まらぬ連打を叩き込む。", SlotType.AttackA, WeaponType.Gauntlet,
                SkillBehavior.Standard, aSteps);
            iSkill.AutoChain = true;
            iSkill.InvulnerableFrames = 60;
        }

        // ---------- 武器種(追加) ----------

        public static void Staff(WeaponTypeData iWeapon, SkillData iFinisher)
        {
            iWeapon.Weapon = WeaponType.Staff;
            iWeapon.DisplayName = "杖";
            iWeapon.Color = new Color(0.3f, 1f, 0.75f);
            iWeapon.HeadShape = PrimitiveType.Sphere;
            iWeapon.HeadScale = new Vector3(0.24f, 0.24f, 0.24f);
            iWeapon.HeadOffset = new Vector3(0f, 0f, 1.5f);
            iWeapon.HandleLength = 1.4f;
            iWeapon.MasteryFinisher = iFinisher;
            iWeapon.SynergyStaggerBonus = 0.1f;
        }

        public static void Gun(WeaponTypeData iWeapon, SkillData iFinisher)
        {
            iWeapon.Weapon = WeaponType.Gun;
            iWeapon.DisplayName = "銃";
            iWeapon.Color = new Color(1f, 0.9f, 0.25f);
            iWeapon.HeadShape = PrimitiveType.Cube;
            iWeapon.HeadScale = new Vector3(0.12f, 0.18f, 0.55f);
            iWeapon.HeadOffset = new Vector3(0f, 0.05f, 0.3f);
            iWeapon.HandleLength = 0f;
            iWeapon.MasteryFinisher = iFinisher;
            iWeapon.SynergyStaggerBonus = 0.1f;
        }

        public static void Gauntlet(WeaponTypeData iWeapon, SkillData iFinisher)
        {
            iWeapon.Weapon = WeaponType.Gauntlet;
            iWeapon.DisplayName = "拳";
            iWeapon.Color = new Color(1f, 0.35f, 0.45f);
            iWeapon.HeadShape = PrimitiveType.Cube;
            iWeapon.HeadScale = new Vector3(0.28f, 0.28f, 0.32f);
            iWeapon.HeadOffset = new Vector3(0f, 0f, 0.12f);
            iWeapon.HandleLength = 0f;
            iWeapon.MasteryFinisher = iFinisher;
            iWeapon.SynergyStaggerBonus = 0.15f;
        }
    }
}
