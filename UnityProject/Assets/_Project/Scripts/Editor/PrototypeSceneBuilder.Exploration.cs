using System.Collections.Generic;
using UnityEngine;

namespace BattleFight.EditorTools
{
    /// <summary>
    /// 探索マップ「古城」を作る(メトロイドヴァニア)。部屋はすべて箱で組む。
    ///
    ///                       [闘技の間(ボス)]
    ///                             |
    ///                         [塔の麓]  祭壇
    ///                             ‖ 谷(ワイヤー)
    ///  [崩れた大広間]         [高台の庭](高さ3m)
    ///        | ひび割れた壁        ‖ 高い段差(ハンマージャンプ)
    ///  [入口の広間] 祭壇 ============┘
    ///        |
    ///     [兵舎] ─ ひび割れた壁 ─ [隠し部屋]
    ///
    /// 進み方: 兵舎で「重撃」→ 北の壁を壊して大広間で「ハンマージャンプ」→ 高台の庭で「ワイヤー」→ 谷を渡ってボス。
    /// 座標はマップの原点からの相対位置(x が東、z が北)。
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        static readonly Vector3 mExplorationOrigin = new Vector3(200f, 0f, 0f);
        const float mWallHeight = 6f;
        const float mLedgeHeight = 3f;

        class ExplorationSkills
        {
            public SkillData SwordCombo;
            public SkillData SwordIai;
            public SkillData SwordStep;
            public SkillData HammerCombo;
            public SkillData HammerSpin;
            public SkillData HammerQuake;
            public SkillData HammerMeteor;
            public SkillData HammerJump;
            public SkillData ChainCombo;
            public SkillData ChainPull;
            public SkillData ChainWire;
            public SkillData StaffBolt;
            public SkillData StaffBlink;
            public SkillData StaffFireball;
            public SkillData StaffThunder;
        }

        class ExplorationEnemies
        {
            public EnemyProfile Grunt;
            public EnemyProfile Armored;
            public EnemyProfile Shooter;
            public EnemyProfile Dasher;
            public EnemyProfile Flyer;
            public EnemyProfile Boss;
        }

        class ExplorationMaterials
        {
            public Material Floor;
            public Material Wall;
            public Material Raised;
            public Material Cracked;
            public Material Crack;
            public Material Altar;
            public Material Crystal;
            public Material Orb;
            public Material Barrier;
            public Material Grapple;
        }

        static ExplorationRegion BuildExploration(ExplorationSkills iSkill, ExplorationEnemies iEnemy)
        {
            var aRoot = new GameObject("Exploration").transform;
            aRoot.position = mExplorationOrigin;
            var aM = new ExplorationMaterials
            {
                Floor = Mat("ExploreFloor", new Color(0.3f, 0.3f, 0.34f)),
                Wall = Mat("ExploreWall", new Color(0.24f, 0.22f, 0.26f)),
                Raised = Mat("ExploreRaised", new Color(0.36f, 0.38f, 0.33f)),
                Cracked = Mat("CrackedWall", new Color(0.52f, 0.4f, 0.3f)),
                Crack = Mat("Crack", new Color(0.12f, 0.08f, 0.06f)),
                Altar = Mat("Altar", new Color(0.55f, 0.55f, 0.6f)),
                Crystal = Mat("AltarCrystal", new Color(0.45f, 0.8f, 1f)),
                Orb = Mat("SkillOrb", new Color(1f, 0.9f, 0.5f)),
                Barrier = Mat("Barrier", new Color(0.6f, 0.15f, 0.2f)),
                Grapple = Mat("GrapplePoint", new Color(0.75f, 0.5f, 1f)),
            };

            // ---------- 入口の広間(x -12..12, z -12..12, 床の高さ 0) ----------
            Floor(aRoot, "Hall_Floor", -12, 12, -12, 12, 0f, aM.Floor);
            WallX(aRoot, "Hall_Wall_S", -12, 12, -12, 0f, aM.Wall);
            WallX(aRoot, "Hall_Wall_N", -12, 12, 12, 0f, aM.Wall, (-3, 3));
            WallZ(aRoot, "Hall_Wall_E", -12, 12, 12, 0f, aM.Wall, (-3, 3));
            WallZ(aRoot, "Hall_Wall_W", -12, 12, -12, 0f, aM.Wall, (-4, 4));
            Pillar(aRoot, "Hall_Pillar_1", new Vector3(-7, 0, 7), aM.Wall);
            Pillar(aRoot, "Hall_Pillar_2", new Vector3(7, 0, 7), aM.Wall);
            var aSpawn = new GameObject("PlayerSpawn").transform;
            aSpawn.SetParent(aRoot, false);
            aSpawn.localPosition = new Vector3(0f, 0f, -5f);
            // 祭壇は通り道をふさがないよう、広間の隅に置く
            Altar(aRoot, "cp_hall", new Vector3(-7f, 0f, -7f), aM);

            // ---------- 東の回廊 → 兵舎(x 20..44, z -10..10) ----------
            Floor(aRoot, "EastHall_Floor", 12, 20, -3, 3, 0f, aM.Floor);
            WallX(aRoot, "EastHall_Wall_S", 12, 20, -3, 0f, aM.Wall);
            WallX(aRoot, "EastHall_Wall_N", 12, 20, 3, 0f, aM.Wall);

            Floor(aRoot, "Barracks_Floor", 20, 44, -10, 10, 0f, aM.Floor);
            WallZ(aRoot, "Barracks_Wall_W", -10, 10, 20, 0f, aM.Wall, (-3, 3));
            WallZ(aRoot, "Barracks_Wall_E", -10, 10, 44, 0f, aM.Wall);
            WallX(aRoot, "Barracks_Wall_N", 20, 44, 10, 0f, aM.Wall);
            WallX(aRoot, "Barracks_Wall_S", 20, 44, -10, 0f, aM.Wall, (29, 35));
            Pickup(aRoot, "pickup_hammer_combo", new Vector3(40f, 0f, 0f), "ひび割れた壁を壊せる(大槌の技で叩く)", iSkill.HammerCombo);
            Zone(aRoot, "zone_barracks", new Vector3(32f, 0f, 0f), new Vector3(22f, 8f, 18f),
                (iEnemy.Grunt, new Vector3(28f, 0f, 5f)), (iEnemy.Grunt, new Vector3(36f, 0f, -4f)), (iEnemy.Grunt, new Vector3(38f, 0f, 5f)));

            // 兵舎の南: ひび割れた壁の奥の隠し部屋(x 28..36, z -18..-10)
            CrackedWall(aRoot, "wall_barracks_secret", new Vector3(32f, 0f, -10f), 6f, true, aM);
            Floor(aRoot, "Secret_Floor", 28, 36, -18, -10, 0f, aM.Floor);
            WallX(aRoot, "Secret_Wall_S", 28, 36, -18, 0f, aM.Wall);
            WallZ(aRoot, "Secret_Wall_W", -18, -10, 28, 0f, aM.Wall);
            WallZ(aRoot, "Secret_Wall_E", -18, -10, 36, 0f, aM.Wall);
            Pickup(aRoot, "pickup_hammer_spin", new Vector3(32f, 0f, -15f), "隠し部屋を見つけた", iSkill.HammerSpin);

            // ---------- 北の回廊(ひび割れた壁)→ 崩れた大広間(x -11..11, z 20..44) ----------
            Floor(aRoot, "NorthHall_Floor", -3, 3, 12, 20, 0f, aM.Floor);
            WallZ(aRoot, "NorthHall_Wall_W", 12, 20, -3, 0f, aM.Wall);
            WallZ(aRoot, "NorthHall_Wall_E", 12, 20, 3, 0f, aM.Wall);
            CrackedWall(aRoot, "wall_hall_north", new Vector3(0f, 0f, 14f), 6f, true, aM);

            Floor(aRoot, "GreatHall_Floor", -11, 11, 20, 44, 0f, aM.Floor);
            WallX(aRoot, "GreatHall_Wall_S", -11, 11, 20, 0f, aM.Wall, (-3, 3));
            WallX(aRoot, "GreatHall_Wall_N", -11, 11, 44, 0f, aM.Wall);
            WallZ(aRoot, "GreatHall_Wall_W", 20, 44, -11, 0f, aM.Wall);
            WallZ(aRoot, "GreatHall_Wall_E", 20, 44, 11, 0f, aM.Wall);
            Pickup(aRoot, "pickup_hammer_jump", new Vector3(6f, 0f, 40f), "高い段差に跳び乗れる(移動スキル)", iSkill.HammerJump);
            Pickup(aRoot, "pickup_hammer_quake", new Vector3(-6f, 0f, 40f), "", iSkill.HammerQuake);
            Zone(aRoot, "zone_great_hall", new Vector3(0f, 0f, 32f), new Vector3(20f, 8f, 22f),
                (iEnemy.Armored, new Vector3(-4f, 0f, 34f)), (iEnemy.Grunt, new Vector3(4f, 0f, 30f)), (iEnemy.Grunt, new Vector3(4f, 0f, 38f)));
            // 大広間の中の高台(ハンマージャンプで乗る)
            Block(aRoot, "GreatHall_Ledge", new Vector3(-10f, -1f, 22f), new Vector3(-4f, mLedgeHeight, 28f), aM.Raised);
            Marker(aRoot, "GreatHall_Ledge_Label", new Vector3(-7f, mLedgeHeight, 28.5f), "高い段差", new Color(0.8f, 1f, 0.7f), 0.8f);
            Pickup(aRoot, "pickup_hammer_meteor", new Vector3(-7f, mLedgeHeight, 25f), "高所に隠されていた", iSkill.HammerMeteor);

            // ---------- 高台の庭(x -36..-12, z -12..12, 床の高さ 3) ----------
            Block(aRoot, "Garden_Floor", new Vector3(-36f, -1f, -12f), new Vector3(-12f, mLedgeHeight, 12f), aM.Raised);
            Marker(aRoot, "Garden_Ledge_Label", new Vector3(-12.6f, 0f, 0f), "高い段差", new Color(0.8f, 1f, 0.7f), 3.6f);
            WallX(aRoot, "Garden_Wall_S", -36, -12, -12, mLedgeHeight, aM.Wall);
            WallX(aRoot, "Garden_Wall_N", -36, -12, 12, mLedgeHeight, aM.Wall, (-30, -18));
            WallZ(aRoot, "Garden_Wall_W", -12, 12, -36, mLedgeHeight, aM.Wall);
            Pickup(aRoot, "pickup_chain_wire", new Vector3(-32f, mLedgeHeight, -8f), "◎ のポイントへ飛べる。谷を渡れる(移動スキル)", iSkill.ChainWire);
            Pickup(aRoot, "pickup_chain_pull", new Vector3(-32f, mLedgeHeight, 8f), "", iSkill.ChainPull);
            Pickup(aRoot, "pickup_chain_combo", new Vector3(-16f, mLedgeHeight, -9f), "", iSkill.ChainCombo);
            Zone(aRoot, "zone_garden", new Vector3(-24f, mLedgeHeight, 0f), new Vector3(22f, 8f, 22f),
                (iEnemy.Shooter, new Vector3(-30f, mLedgeHeight, 6f)), (iEnemy.Shooter, new Vector3(-30f, mLedgeHeight, -6f)),
                (iEnemy.Dasher, new Vector3(-18f, mLedgeHeight, 0f)));
            Safe(aRoot, "safe_garden_edge", new Vector3(-24f, mLedgeHeight, 9f));

            // ---------- 谷(x -30..-18, z 12..30。床がない)。ワイヤーのポイントで渡る ----------
            Block(aRoot, "Chasm_Wall_W", new Vector3(-31f, -8f, 12f), new Vector3(-30f, mLedgeHeight + mWallHeight, 30f), aM.Wall);
            Block(aRoot, "Chasm_Wall_E", new Vector3(-18f, -8f, 12f), new Vector3(-17f, mLedgeHeight + mWallHeight, 30f), aM.Wall);
            Marker(aRoot, "Chasm_Label", new Vector3(-24f, mLedgeHeight, 12.5f), "谷  ◎ にワイヤーで渡れそうだ", new Color(0.85f, 0.7f, 1f), 1.2f);
            Vector3[] aGrapplePoints = { new Vector3(-24f, 8f, 15f), new Vector3(-24f, 9f, 22f), new Vector3(-24f, 8.5f, 31.5f) };
            for (int aI = 0; aI < aGrapplePoints.Length; aI++)
            {
                var aPoint = Part(PrimitiveType.Sphere, $"Chasm_GrapplePoint_{aI + 1}", aRoot, aGrapplePoints[aI], Vector3.one * 0.8f, aM.Grapple);
                aPoint.gameObject.AddComponent<GrapplePoint>();
            }

            // ---------- 塔の麓(x -34..-14, z 30..50, 床の高さ 3) ----------
            Floor(aRoot, "Tower_Floor", -34, -14, 30, 50, mLedgeHeight, aM.Floor);
            WallX(aRoot, "Tower_Wall_S", -34, -14, 30, mLedgeHeight, aM.Wall, (-30, -18));
            WallX(aRoot, "Tower_Wall_N", -34, -14, 50, mLedgeHeight, aM.Wall, (-27, -21));
            WallZ(aRoot, "Tower_Wall_W", 30, 50, -34, mLedgeHeight, aM.Wall);
            WallZ(aRoot, "Tower_Wall_E", 30, 50, -14, mLedgeHeight, aM.Wall);
            Altar(aRoot, "cp_tower", new Vector3(-24f, mLedgeHeight, 38f), aM);
            Safe(aRoot, "safe_tower_edge", new Vector3(-24f, mLedgeHeight, 33f));
            Pickup(aRoot, "pickup_staff_bolt", new Vector3(-30f, mLedgeHeight, 46f), "", iSkill.StaffBolt);
            Pickup(aRoot, "pickup_staff_blink", new Vector3(-18f, mLedgeHeight, 46f), "", iSkill.StaffBlink);
            Zone(aRoot, "zone_tower", new Vector3(-24f, mLedgeHeight, 42f), new Vector3(18f, 8f, 14f),
                (iEnemy.Flyer, new Vector3(-28f, mLedgeHeight, 46f)), (iEnemy.Dasher, new Vector3(-20f, mLedgeHeight, 46f)));

            // ---------- 闘技の間への道 → 闘技の間(x -38..-10, z 56..84) ----------
            Floor(aRoot, "BossRoad_Floor", -27, -21, 50, 56, mLedgeHeight, aM.Floor);
            WallZ(aRoot, "BossRoad_Wall_W", 50, 56, -27, mLedgeHeight, aM.Wall);
            WallZ(aRoot, "BossRoad_Wall_E", 50, 56, -21, mLedgeHeight, aM.Wall);
            Marker(aRoot, "BossRoad_Label", new Vector3(-24f, mLedgeHeight, 53f), "闘技の間", new Color(1f, 0.6f, 0.55f), 3.5f);

            Floor(aRoot, "Arena_Floor", -38, -10, 56, 84, mLedgeHeight, aM.Floor);
            WallX(aRoot, "Arena_Wall_S", -38, -10, 56, mLedgeHeight, aM.Wall, (-27, -21));
            WallX(aRoot, "Arena_Wall_N", -38, -10, 84, mLedgeHeight, aM.Wall);
            WallZ(aRoot, "Arena_Wall_W", 56, 84, -38, mLedgeHeight, aM.Wall);
            WallZ(aRoot, "Arena_Wall_E", 56, 84, -10, mLedgeHeight, aM.Wall);
            // ボスと戦っている間は入口を閉じる
            var aBarrier = Block(aRoot, "Arena_Barrier", new Vector3(-27f, mLedgeHeight, 55.5f), new Vector3(-21f, mLedgeHeight + mWallHeight, 56.5f), aM.Barrier);
            var aBossZoneSize = new Vector3(26f, 8f, 26f);
            var aBossZone = Zone(aRoot, "zone_boss", new Vector3(-24f, mLedgeHeight, 71f), aBossZoneSize);
            // 床の高さ(ゾーンの中心から半分下)の、部屋の奥に出す
            aBossZone.Setup("zone_boss", aBossZoneSize,
                new List<EncounterZone.Spawn> { new EncounterZone.Spawn { Profile = iEnemy.Boss, Offset = new Vector3(0f, -aBossZoneSize.y * 0.5f, 6f) } },
                new[] { iSkill.StaffFireball, iSkill.StaffThunder }, true, new[] { aBarrier.gameObject });

            // ---------- マップ全体 ----------
            var aRegion = aRoot.gameObject.AddComponent<ExplorationRegion>();
            aRegion.Setup("古城", aSpawn,
                new[] { iSkill.SwordCombo, iSkill.SwordIai, iSkill.SwordStep },
                new[] { new SkillPreset("剣士", iSkill.SwordCombo, iSkill.SwordIai, iSkill.SwordStep) },
                -8f,
                new List<ExplorationRegion.Room>
                {
                    Room("入口の広間", 0, 0, 24, 24, 0f),
                    Room("東の回廊", 16, 0, 8, 6, 0f),
                    Room("兵舎", 32, 0, 24, 20, 0f),
                    Room("隠し部屋", 32, -14, 8, 8, 0f),
                    Room("北の回廊", 0, 16, 6, 8, 0f),
                    Room("崩れた大広間", 0, 32, 22, 24, 0f),
                    Room("高台の庭", -24, 0, 24, 24, mLedgeHeight),
                    Room("谷", -24, 21, 12, 18, mLedgeHeight),
                    Room("塔の麓", -24, 40, 20, 20, mLedgeHeight),
                    Room("闘技の間への道", -24, 53, 6, 6, mLedgeHeight),
                    Room("闘技の間", -24, 70, 28, 28, mLedgeHeight),
                });

            // 探索のステージを選ぶまでは出さない
            aRoot.gameObject.SetActive(false);
            return aRegion;
        }

        // ---------- 部品 ----------

        static ExplorationRegion.Room Room(string iName, float iX, float iZ, float iWidth, float iDepth, float iFloor) => new ExplorationRegion.Room
        {
            Name = iName,
            // 床の少し下から、壁の上までを部屋とみなす
            Center = new Vector3(iX, iFloor + 3f, iZ),
            Size = new Vector3(iWidth, 16f, iDepth),
        };

        /// <summary>最小と最大の角で指定する箱(地形。当たり判定あり)</summary>
        static Transform Block(Transform iRoot, string iName, Vector3 iMin, Vector3 iMax, Material iMaterial) =>
            Part(PrimitiveType.Cube, iName, iRoot, (iMin + iMax) * 0.5f, iMax - iMin, iMaterial, true);

        /// <summary>厚さ1mの床。上面が floorTop になる</summary>
        static void Floor(Transform iRoot, string iName, float iX0, float iX1, float iZ0, float iZ1, float iFloorTop, Material iMaterial) =>
            Block(iRoot, iName, new Vector3(iX0, iFloorTop - 1f, iZ0), new Vector3(iX1, iFloorTop, iZ1), iMaterial);

        /// <summary>z が一定の壁(東西に伸びる)。gaps は x の範囲で、そこだけ開ける</summary>
        static void WallX(Transform iRoot, string iName, float iX0, float iX1, float iZ, float iFloorTop, Material iMaterial,
            params (float from, float to)[] iGaps)
        {
            int aIndex = 0;
            foreach (var (aFrom, aTo) in Segments(iX0, iX1, iGaps))
            {
                Block(iRoot, $"{iName}_{++aIndex}", new Vector3(aFrom, iFloorTop, iZ - 0.5f), new Vector3(aTo, iFloorTop + mWallHeight, iZ + 0.5f), iMaterial);
            }
        }

        /// <summary>x が一定の壁(南北に伸びる)。gaps は z の範囲</summary>
        static void WallZ(Transform iRoot, string iName, float iZ0, float iZ1, float iX, float iFloorTop, Material iMaterial,
            params (float from, float to)[] iGaps)
        {
            int aIndex = 0;
            foreach (var (aFrom, aTo) in Segments(iZ0, iZ1, iGaps))
            {
                Block(iRoot, $"{iName}_{++aIndex}", new Vector3(iX - 0.5f, iFloorTop, aFrom), new Vector3(iX + 0.5f, iFloorTop + mWallHeight, aTo), iMaterial);
            }
        }

        static IEnumerable<(float, float)> Segments(float iStart, float iEnd, (float from, float to)[] iGaps)
        {
            var aSorted = new List<(float from, float to)>(iGaps);
            aSorted.Sort((iA, iB) => iA.from.CompareTo(iB.from));
            float aCursor = iStart;
            foreach (var (aFrom, aTo) in aSorted)
            {
                if (aFrom > aCursor) yield return (aCursor, aFrom);
                aCursor = Mathf.Max(aCursor, aTo);
            }
            if (iEnd > aCursor) yield return (aCursor, iEnd);
        }

        static void Pillar(Transform iRoot, string iName, Vector3 iBasePosition, Material iMaterial) =>
            Block(iRoot, iName, iBasePosition + new Vector3(-1f, 0f, -1f), iBasePosition + new Vector3(1f, mWallHeight, 1f), iMaterial);

        static void Marker(Transform iRoot, string iName, Vector3 iPosition, string iText, Color iColor, float iHeight)
        {
            var aMarker = new GameObject(iName);
            aMarker.transform.SetParent(iRoot, false);
            aMarker.transform.localPosition = iPosition;
            WorldLabel.Attach(aMarker, iText, iColor, iHeight);
        }

        /// <summary>ひび割れた壁。center は床の中央。横幅 width、厚さ 1m、高さは壁と同じ</summary>
        static void CrackedWall(Transform iRoot, string iId, Vector3 iCenter, float iWidth, bool iAlongX, ExplorationMaterials iM)
        {
            var aSize = iAlongX ? new Vector3(iWidth, mWallHeight, 1f) : new Vector3(1f, mWallHeight, iWidth);
            var aWall = Part(PrimitiveType.Cube, iId, iRoot, iCenter + Vector3.up * (mWallHeight * 0.5f), aSize, iM.Cracked, true);
            // ひびの模様(見た目だけ。子なので壁と一緒に消える)
            Vector3[] aCracks = { new Vector3(-0.2f, 0.1f, 0f), new Vector3(0.15f, -0.15f, 0f), new Vector3(0.05f, 0.3f, 0f) };
            float[] aAngles = { 35f, -50f, 80f };
            for (int aI = 0; aI < aCracks.Length; aI++)
            {
                var aCrack = Part(PrimitiveType.Cube, $"Crack_{aI + 1}", aWall, aCracks[aI], new Vector3(0.45f, 0.03f, 1.06f), iM.Crack);
                aCrack.localRotation = Quaternion.Euler(0f, 0f, aAngles[aI]);
                if (!iAlongX) aCrack.localRotation = Quaternion.Euler(0f, 90f, 0f) * aCrack.localRotation;
            }
            aWall.gameObject.AddComponent<BreakableWall>().Setup(iId, WeaponType.Hammer);
            WorldLabel.Attach(aWall.gameObject, "ひび割れた壁", new Color(1f, 0.8f, 0.6f), 0.3f);
        }

        static void Pickup(Transform iRoot, string iId, Vector3 iFloorPosition, string iHint, params SkillData[] iSkills)
        {
            var aPickup = new GameObject(iId).transform;
            aPickup.SetParent(iRoot, false);
            aPickup.localPosition = iFloorPosition + Vector3.up * 1f;
            var aOrb = Part(PrimitiveType.Sphere, "Orb", aPickup, new Vector3(0f, 0.2f, 0f), Vector3.one * 0.7f,
                Mat("SkillOrb", new Color(1f, 0.9f, 0.5f)));
            Part(PrimitiveType.Cylinder, "Base", aPickup, new Vector3(0f, -0.95f, 0f), new Vector3(1.2f, 0.05f, 1.2f),
                Mat("Altar", new Color(0.55f, 0.55f, 0.6f)));
            aPickup.gameObject.AddComponent<SkillPickup>().Setup(iId, iSkills, iHint, aOrb);
            WorldLabel.Attach(aPickup.gameObject, "スキル", new Color(1f, 0.9f, 0.5f), 1.1f);
        }

        static void Altar(Transform iRoot, string iId, Vector3 iFloorPosition, ExplorationMaterials iM)
        {
            var aAltar = new GameObject(iId).transform;
            aAltar.SetParent(iRoot, false);
            aAltar.localPosition = iFloorPosition;
            // 復帰位置は祭壇の南(forward の逆)に出したいので、南を向ける
            aAltar.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Part(PrimitiveType.Cylinder, "Pedestal", aAltar, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 0.5f, 1.4f), iM.Altar, true);
            var aCrystal = Part(PrimitiveType.Cube, "Crystal", aAltar, new Vector3(0f, 1.6f, 0f), new Vector3(0.5f, 0.8f, 0.5f), iM.Crystal);
            aCrystal.localRotation = Quaternion.Euler(45f, 0f, 45f);
            aAltar.gameObject.AddComponent<Checkpoint>().Setup(iId, aCrystal.GetComponent<Renderer>());
            WorldLabel.Attach(aAltar.gameObject, "祭壇", new Color(0.6f, 0.9f, 1f), 2.6f);
        }

        static void Safe(Transform iRoot, string iName, Vector3 iFloorPosition)
        {
            var aPoint = new GameObject(iName);
            aPoint.transform.SetParent(iRoot, false);
            aPoint.transform.localPosition = iFloorPosition;
            aPoint.AddComponent<SafePoint>();
        }

        static EncounterZone Zone(Transform iRoot, string iId, Vector3 iFloorCenter, Vector3 iSize,
            params (EnemyProfile profile, Vector3 position)[] iEnemies)
        {
            var aZone = new GameObject(iId).transform;
            aZone.SetParent(iRoot, false);
            aZone.localPosition = iFloorCenter + Vector3.up * (iSize.y * 0.5f);
            var aSpawns = new List<EncounterZone.Spawn>();
            foreach (var (aProfile, aPosition) in iEnemies)
            {
                aSpawns.Add(new EncounterZone.Spawn { Profile = aProfile, Offset = aPosition - aZone.localPosition });
            }
            var aComponent = aZone.gameObject.AddComponent<EncounterZone>();
            aComponent.Setup(iId, iSize, aSpawns, new SkillData[0], false, new GameObject[0]);
            return aComponent;
        }
    }
}
