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
        static readonly Vector3 ExplorationOrigin = new Vector3(200f, 0f, 0f);
        const float WallHeight = 6f;
        const float LedgeHeight = 3f;

        class ExplorationSkills
        {
            public SkillData swordCombo, swordIai, swordStep;
            public SkillData hammerCombo, hammerSpin, hammerQuake, hammerMeteor, hammerJump;
            public SkillData chainCombo, chainPull, chainWire;
            public SkillData staffBolt, staffBlink, staffFireball, staffThunder;
        }

        class ExplorationEnemies
        {
            public EnemyProfile grunt, armored, shooter, dasher, flyer, boss;
        }

        class ExplorationMaterials
        {
            public Material floor, wall, raised, cracked, crack, altar, crystal, orb, barrier, grapple;
        }

        static ExplorationRegion BuildExploration(ExplorationSkills skill, ExplorationEnemies enemy)
        {
            var root = new GameObject("Exploration").transform;
            root.position = ExplorationOrigin;
            var m = new ExplorationMaterials
            {
                floor = Mat("ExploreFloor", new Color(0.3f, 0.3f, 0.34f)),
                wall = Mat("ExploreWall", new Color(0.24f, 0.22f, 0.26f)),
                raised = Mat("ExploreRaised", new Color(0.36f, 0.38f, 0.33f)),
                cracked = Mat("CrackedWall", new Color(0.52f, 0.4f, 0.3f)),
                crack = Mat("Crack", new Color(0.12f, 0.08f, 0.06f)),
                altar = Mat("Altar", new Color(0.55f, 0.55f, 0.6f)),
                crystal = Mat("AltarCrystal", new Color(0.45f, 0.8f, 1f)),
                orb = Mat("SkillOrb", new Color(1f, 0.9f, 0.5f)),
                barrier = Mat("Barrier", new Color(0.6f, 0.15f, 0.2f)),
                grapple = Mat("GrapplePoint", new Color(0.75f, 0.5f, 1f)),
            };

            // ---------- 入口の広間(x -12..12, z -12..12, 床の高さ 0) ----------
            Floor(root, "Hall_Floor", -12, 12, -12, 12, 0f, m.floor);
            WallX(root, "Hall_Wall_S", -12, 12, -12, 0f, m.wall);
            WallX(root, "Hall_Wall_N", -12, 12, 12, 0f, m.wall, (-3, 3));
            WallZ(root, "Hall_Wall_E", -12, 12, 12, 0f, m.wall, (-3, 3));
            WallZ(root, "Hall_Wall_W", -12, 12, -12, 0f, m.wall, (-4, 4));
            Pillar(root, "Hall_Pillar_1", new Vector3(-7, 0, 7), m.wall);
            Pillar(root, "Hall_Pillar_2", new Vector3(7, 0, 7), m.wall);
            var spawn = new GameObject("PlayerSpawn").transform;
            spawn.SetParent(root, false);
            spawn.localPosition = new Vector3(0f, 0f, -5f);
            // 祭壇は通り道をふさがないよう、広間の隅に置く
            Altar(root, "cp_hall", new Vector3(-7f, 0f, -7f), m);

            // ---------- 東の回廊 → 兵舎(x 20..44, z -10..10) ----------
            Floor(root, "EastHall_Floor", 12, 20, -3, 3, 0f, m.floor);
            WallX(root, "EastHall_Wall_S", 12, 20, -3, 0f, m.wall);
            WallX(root, "EastHall_Wall_N", 12, 20, 3, 0f, m.wall);

            Floor(root, "Barracks_Floor", 20, 44, -10, 10, 0f, m.floor);
            WallZ(root, "Barracks_Wall_W", -10, 10, 20, 0f, m.wall, (-3, 3));
            WallZ(root, "Barracks_Wall_E", -10, 10, 44, 0f, m.wall);
            WallX(root, "Barracks_Wall_N", 20, 44, 10, 0f, m.wall);
            WallX(root, "Barracks_Wall_S", 20, 44, -10, 0f, m.wall, (29, 35));
            Pickup(root, "pickup_hammer_combo", new Vector3(40f, 0f, 0f), "ひび割れた壁を壊せる(大槌の技で叩く)", skill.hammerCombo);
            Zone(root, "zone_barracks", new Vector3(32f, 0f, 0f), new Vector3(22f, 8f, 18f),
                (enemy.grunt, new Vector3(28f, 0f, 5f)), (enemy.grunt, new Vector3(36f, 0f, -4f)), (enemy.grunt, new Vector3(38f, 0f, 5f)));

            // 兵舎の南: ひび割れた壁の奥の隠し部屋(x 28..36, z -18..-10)
            CrackedWall(root, "wall_barracks_secret", new Vector3(32f, 0f, -10f), 6f, true, m);
            Floor(root, "Secret_Floor", 28, 36, -18, -10, 0f, m.floor);
            WallX(root, "Secret_Wall_S", 28, 36, -18, 0f, m.wall);
            WallZ(root, "Secret_Wall_W", -18, -10, 28, 0f, m.wall);
            WallZ(root, "Secret_Wall_E", -18, -10, 36, 0f, m.wall);
            Pickup(root, "pickup_hammer_spin", new Vector3(32f, 0f, -15f), "隠し部屋を見つけた", skill.hammerSpin);

            // ---------- 北の回廊(ひび割れた壁)→ 崩れた大広間(x -11..11, z 20..44) ----------
            Floor(root, "NorthHall_Floor", -3, 3, 12, 20, 0f, m.floor);
            WallZ(root, "NorthHall_Wall_W", 12, 20, -3, 0f, m.wall);
            WallZ(root, "NorthHall_Wall_E", 12, 20, 3, 0f, m.wall);
            CrackedWall(root, "wall_hall_north", new Vector3(0f, 0f, 14f), 6f, true, m);

            Floor(root, "GreatHall_Floor", -11, 11, 20, 44, 0f, m.floor);
            WallX(root, "GreatHall_Wall_S", -11, 11, 20, 0f, m.wall, (-3, 3));
            WallX(root, "GreatHall_Wall_N", -11, 11, 44, 0f, m.wall);
            WallZ(root, "GreatHall_Wall_W", 20, 44, -11, 0f, m.wall);
            WallZ(root, "GreatHall_Wall_E", 20, 44, 11, 0f, m.wall);
            Pickup(root, "pickup_hammer_jump", new Vector3(6f, 0f, 40f), "高い段差に跳び乗れる(移動スキル)", skill.hammerJump);
            Pickup(root, "pickup_hammer_quake", new Vector3(-6f, 0f, 40f), "", skill.hammerQuake);
            Zone(root, "zone_great_hall", new Vector3(0f, 0f, 32f), new Vector3(20f, 8f, 22f),
                (enemy.armored, new Vector3(-4f, 0f, 34f)), (enemy.grunt, new Vector3(4f, 0f, 30f)), (enemy.grunt, new Vector3(4f, 0f, 38f)));
            // 大広間の中の高台(ハンマージャンプで乗る)
            Block(root, "GreatHall_Ledge", new Vector3(-10f, -1f, 22f), new Vector3(-4f, LedgeHeight, 28f), m.raised);
            Marker(root, "GreatHall_Ledge_Label", new Vector3(-7f, LedgeHeight, 28.5f), "高い段差", new Color(0.8f, 1f, 0.7f), 0.8f);
            Pickup(root, "pickup_hammer_meteor", new Vector3(-7f, LedgeHeight, 25f), "高所に隠されていた", skill.hammerMeteor);

            // ---------- 高台の庭(x -36..-12, z -12..12, 床の高さ 3) ----------
            Block(root, "Garden_Floor", new Vector3(-36f, -1f, -12f), new Vector3(-12f, LedgeHeight, 12f), m.raised);
            Marker(root, "Garden_Ledge_Label", new Vector3(-12.6f, 0f, 0f), "高い段差", new Color(0.8f, 1f, 0.7f), 3.6f);
            WallX(root, "Garden_Wall_S", -36, -12, -12, LedgeHeight, m.wall);
            WallX(root, "Garden_Wall_N", -36, -12, 12, LedgeHeight, m.wall, (-30, -18));
            WallZ(root, "Garden_Wall_W", -12, 12, -36, LedgeHeight, m.wall);
            Pickup(root, "pickup_chain_wire", new Vector3(-32f, LedgeHeight, -8f), "◎ のポイントへ飛べる。谷を渡れる(移動スキル)", skill.chainWire);
            Pickup(root, "pickup_chain_pull", new Vector3(-32f, LedgeHeight, 8f), "", skill.chainPull);
            Pickup(root, "pickup_chain_combo", new Vector3(-16f, LedgeHeight, -9f), "", skill.chainCombo);
            Zone(root, "zone_garden", new Vector3(-24f, LedgeHeight, 0f), new Vector3(22f, 8f, 22f),
                (enemy.shooter, new Vector3(-30f, LedgeHeight, 6f)), (enemy.shooter, new Vector3(-30f, LedgeHeight, -6f)),
                (enemy.dasher, new Vector3(-18f, LedgeHeight, 0f)));
            Safe(root, "safe_garden_edge", new Vector3(-24f, LedgeHeight, 9f));

            // ---------- 谷(x -30..-18, z 12..30。床がない)。ワイヤーのポイントで渡る ----------
            Block(root, "Chasm_Wall_W", new Vector3(-31f, -8f, 12f), new Vector3(-30f, LedgeHeight + WallHeight, 30f), m.wall);
            Block(root, "Chasm_Wall_E", new Vector3(-18f, -8f, 12f), new Vector3(-17f, LedgeHeight + WallHeight, 30f), m.wall);
            Marker(root, "Chasm_Label", new Vector3(-24f, LedgeHeight, 12.5f), "谷  ◎ にワイヤーで渡れそうだ", new Color(0.85f, 0.7f, 1f), 1.2f);
            Vector3[] grapplePoints = { new Vector3(-24f, 8f, 15f), new Vector3(-24f, 9f, 22f), new Vector3(-24f, 8.5f, 31.5f) };
            for (int i = 0; i < grapplePoints.Length; i++)
            {
                var point = Part(PrimitiveType.Sphere, $"Chasm_GrapplePoint_{i + 1}", root, grapplePoints[i], Vector3.one * 0.8f, m.grapple);
                point.gameObject.AddComponent<GrapplePoint>();
            }

            // ---------- 塔の麓(x -34..-14, z 30..50, 床の高さ 3) ----------
            Floor(root, "Tower_Floor", -34, -14, 30, 50, LedgeHeight, m.floor);
            WallX(root, "Tower_Wall_S", -34, -14, 30, LedgeHeight, m.wall, (-30, -18));
            WallX(root, "Tower_Wall_N", -34, -14, 50, LedgeHeight, m.wall, (-27, -21));
            WallZ(root, "Tower_Wall_W", 30, 50, -34, LedgeHeight, m.wall);
            WallZ(root, "Tower_Wall_E", 30, 50, -14, LedgeHeight, m.wall);
            Altar(root, "cp_tower", new Vector3(-24f, LedgeHeight, 38f), m);
            Safe(root, "safe_tower_edge", new Vector3(-24f, LedgeHeight, 33f));
            Pickup(root, "pickup_staff_bolt", new Vector3(-30f, LedgeHeight, 46f), "", skill.staffBolt);
            Pickup(root, "pickup_staff_blink", new Vector3(-18f, LedgeHeight, 46f), "", skill.staffBlink);
            Zone(root, "zone_tower", new Vector3(-24f, LedgeHeight, 42f), new Vector3(18f, 8f, 14f),
                (enemy.flyer, new Vector3(-28f, LedgeHeight, 46f)), (enemy.dasher, new Vector3(-20f, LedgeHeight, 46f)));

            // ---------- 闘技の間への道 → 闘技の間(x -38..-10, z 56..84) ----------
            Floor(root, "BossRoad_Floor", -27, -21, 50, 56, LedgeHeight, m.floor);
            WallZ(root, "BossRoad_Wall_W", 50, 56, -27, LedgeHeight, m.wall);
            WallZ(root, "BossRoad_Wall_E", 50, 56, -21, LedgeHeight, m.wall);
            Marker(root, "BossRoad_Label", new Vector3(-24f, LedgeHeight, 53f), "闘技の間", new Color(1f, 0.6f, 0.55f), 3.5f);

            Floor(root, "Arena_Floor", -38, -10, 56, 84, LedgeHeight, m.floor);
            WallX(root, "Arena_Wall_S", -38, -10, 56, LedgeHeight, m.wall, (-27, -21));
            WallX(root, "Arena_Wall_N", -38, -10, 84, LedgeHeight, m.wall);
            WallZ(root, "Arena_Wall_W", 56, 84, -38, LedgeHeight, m.wall);
            WallZ(root, "Arena_Wall_E", 56, 84, -10, LedgeHeight, m.wall);
            // ボスと戦っている間は入口を閉じる
            var barrier = Block(root, "Arena_Barrier", new Vector3(-27f, LedgeHeight, 55.5f), new Vector3(-21f, LedgeHeight + WallHeight, 56.5f), m.barrier);
            var bossZoneSize = new Vector3(26f, 8f, 26f);
            var bossZone = Zone(root, "zone_boss", new Vector3(-24f, LedgeHeight, 71f), bossZoneSize);
            // 床の高さ(ゾーンの中心から半分下)の、部屋の奥に出す
            bossZone.Setup("zone_boss", bossZoneSize,
                new List<EncounterZone.Spawn> { new EncounterZone.Spawn { profile = enemy.boss, offset = new Vector3(0f, -bossZoneSize.y * 0.5f, 6f) } },
                new[] { skill.staffFireball, skill.staffThunder }, true, new[] { barrier.gameObject });

            // ---------- マップ全体 ----------
            var region = root.gameObject.AddComponent<ExplorationRegion>();
            region.Setup("古城", spawn,
                new[] { skill.swordCombo, skill.swordIai, skill.swordStep },
                new[] { new SkillPreset("剣士", skill.swordCombo, skill.swordIai, skill.swordStep) },
                -8f,
                new List<ExplorationRegion.Room>
                {
                    Room("入口の広間", 0, 0, 24, 24, 0f),
                    Room("東の回廊", 16, 0, 8, 6, 0f),
                    Room("兵舎", 32, 0, 24, 20, 0f),
                    Room("隠し部屋", 32, -14, 8, 8, 0f),
                    Room("北の回廊", 0, 16, 6, 8, 0f),
                    Room("崩れた大広間", 0, 32, 22, 24, 0f),
                    Room("高台の庭", -24, 0, 24, 24, LedgeHeight),
                    Room("谷", -24, 21, 12, 18, LedgeHeight),
                    Room("塔の麓", -24, 40, 20, 20, LedgeHeight),
                    Room("闘技の間への道", -24, 53, 6, 6, LedgeHeight),
                    Room("闘技の間", -24, 70, 28, 28, LedgeHeight),
                });

            // 探索のステージを選ぶまでは出さない
            root.gameObject.SetActive(false);
            return region;
        }

        // ---------- 部品 ----------

        static ExplorationRegion.Room Room(string name, float x, float z, float width, float depth, float floor) => new ExplorationRegion.Room
        {
            name = name,
            // 床の少し下から、壁の上までを部屋とみなす
            center = new Vector3(x, floor + 3f, z),
            size = new Vector3(width, 16f, depth),
        };

        /// <summary>最小と最大の角で指定する箱(地形。当たり判定あり)</summary>
        static Transform Block(Transform root, string name, Vector3 min, Vector3 max, Material material) =>
            Part(PrimitiveType.Cube, name, root, (min + max) * 0.5f, max - min, material, true);

        /// <summary>厚さ1mの床。上面が floorTop になる</summary>
        static void Floor(Transform root, string name, float x0, float x1, float z0, float z1, float floorTop, Material material) =>
            Block(root, name, new Vector3(x0, floorTop - 1f, z0), new Vector3(x1, floorTop, z1), material);

        /// <summary>z が一定の壁(東西に伸びる)。gaps は x の範囲で、そこだけ開ける</summary>
        static void WallX(Transform root, string name, float x0, float x1, float z, float floorTop, Material material,
            params (float from, float to)[] gaps)
        {
            int index = 0;
            foreach (var (from, to) in Segments(x0, x1, gaps))
            {
                Block(root, $"{name}_{++index}", new Vector3(from, floorTop, z - 0.5f), new Vector3(to, floorTop + WallHeight, z + 0.5f), material);
            }
        }

        /// <summary>x が一定の壁(南北に伸びる)。gaps は z の範囲</summary>
        static void WallZ(Transform root, string name, float z0, float z1, float x, float floorTop, Material material,
            params (float from, float to)[] gaps)
        {
            int index = 0;
            foreach (var (from, to) in Segments(z0, z1, gaps))
            {
                Block(root, $"{name}_{++index}", new Vector3(x - 0.5f, floorTop, from), new Vector3(x + 0.5f, floorTop + WallHeight, to), material);
            }
        }

        static IEnumerable<(float, float)> Segments(float start, float end, (float from, float to)[] gaps)
        {
            var sorted = new List<(float from, float to)>(gaps);
            sorted.Sort((a, b) => a.from.CompareTo(b.from));
            float cursor = start;
            foreach (var (from, to) in sorted)
            {
                if (from > cursor) yield return (cursor, from);
                cursor = Mathf.Max(cursor, to);
            }
            if (end > cursor) yield return (cursor, end);
        }

        static void Pillar(Transform root, string name, Vector3 basePosition, Material material) =>
            Block(root, name, basePosition + new Vector3(-1f, 0f, -1f), basePosition + new Vector3(1f, WallHeight, 1f), material);

        static void Marker(Transform root, string name, Vector3 position, string text, Color color, float height)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(root, false);
            marker.transform.localPosition = position;
            WorldLabel.Attach(marker, text, color, height);
        }

        /// <summary>ひび割れた壁。center は床の中央。横幅 width、厚さ 1m、高さは壁と同じ</summary>
        static void CrackedWall(Transform root, string id, Vector3 center, float width, bool alongX, ExplorationMaterials m)
        {
            var size = alongX ? new Vector3(width, WallHeight, 1f) : new Vector3(1f, WallHeight, width);
            var wall = Part(PrimitiveType.Cube, id, root, center + Vector3.up * (WallHeight * 0.5f), size, m.cracked, true);
            // ひびの模様(見た目だけ。子なので壁と一緒に消える)
            Vector3[] cracks = { new Vector3(-0.2f, 0.1f, 0f), new Vector3(0.15f, -0.15f, 0f), new Vector3(0.05f, 0.3f, 0f) };
            float[] angles = { 35f, -50f, 80f };
            for (int i = 0; i < cracks.Length; i++)
            {
                var crack = Part(PrimitiveType.Cube, $"Crack_{i + 1}", wall, cracks[i], new Vector3(0.45f, 0.03f, 1.06f), m.crack);
                crack.localRotation = Quaternion.Euler(0f, 0f, angles[i]);
                if (!alongX) crack.localRotation = Quaternion.Euler(0f, 90f, 0f) * crack.localRotation;
            }
            wall.gameObject.AddComponent<BreakableWall>().Setup(id, WeaponType.Hammer);
            WorldLabel.Attach(wall.gameObject, "ひび割れた壁", new Color(1f, 0.8f, 0.6f), 0.3f);
        }

        static void Pickup(Transform root, string id, Vector3 floorPosition, string hint, params SkillData[] skills)
        {
            var pickup = new GameObject(id).transform;
            pickup.SetParent(root, false);
            pickup.localPosition = floorPosition + Vector3.up * 1f;
            var orb = Part(PrimitiveType.Sphere, "Orb", pickup, new Vector3(0f, 0.2f, 0f), Vector3.one * 0.7f,
                Mat("SkillOrb", new Color(1f, 0.9f, 0.5f)));
            Part(PrimitiveType.Cylinder, "Base", pickup, new Vector3(0f, -0.95f, 0f), new Vector3(1.2f, 0.05f, 1.2f),
                Mat("Altar", new Color(0.55f, 0.55f, 0.6f)));
            pickup.gameObject.AddComponent<SkillPickup>().Setup(id, skills, hint, orb);
            WorldLabel.Attach(pickup.gameObject, "スキル", new Color(1f, 0.9f, 0.5f), 1.1f);
        }

        static void Altar(Transform root, string id, Vector3 floorPosition, ExplorationMaterials m)
        {
            var altar = new GameObject(id).transform;
            altar.SetParent(root, false);
            altar.localPosition = floorPosition;
            // 復帰位置は祭壇の南(forward の逆)に出したいので、南を向ける
            altar.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Part(PrimitiveType.Cylinder, "Pedestal", altar, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 0.5f, 1.4f), m.altar, true);
            var crystal = Part(PrimitiveType.Cube, "Crystal", altar, new Vector3(0f, 1.6f, 0f), new Vector3(0.5f, 0.8f, 0.5f), m.crystal);
            crystal.localRotation = Quaternion.Euler(45f, 0f, 45f);
            altar.gameObject.AddComponent<Checkpoint>().Setup(id, crystal.GetComponent<Renderer>());
            WorldLabel.Attach(altar.gameObject, "祭壇", new Color(0.6f, 0.9f, 1f), 2.6f);
        }

        static void Safe(Transform root, string name, Vector3 floorPosition)
        {
            var point = new GameObject(name);
            point.transform.SetParent(root, false);
            point.transform.localPosition = floorPosition;
            point.AddComponent<SafePoint>();
        }

        static EncounterZone Zone(Transform root, string id, Vector3 floorCenter, Vector3 size,
            params (EnemyProfile profile, Vector3 position)[] enemies)
        {
            var zone = new GameObject(id).transform;
            zone.SetParent(root, false);
            zone.localPosition = floorCenter + Vector3.up * (size.y * 0.5f);
            var spawns = new List<EncounterZone.Spawn>();
            foreach (var (profile, position) in enemies)
            {
                spawns.Add(new EncounterZone.Spawn { profile = profile, offset = position - zone.localPosition });
            }
            var component = zone.gameObject.AddComponent<EncounterZone>();
            component.Setup(id, size, spawns, new SkillData[0], false, new GameObject[0]);
            return component;
        }
    }
}
