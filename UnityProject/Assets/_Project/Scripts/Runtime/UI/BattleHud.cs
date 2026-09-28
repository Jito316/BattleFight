using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleFight
{
    /// <summary>
    /// 試作用の HUD(IMGUI)。スロットとラック、武器種ボーナス、スタイルランク(常時表示)、体力などを描く。
    /// 1920x1080 基準の座標を画面の高さに合わせて拡大縮小する。
    /// </summary>
    public class BattleHud : MonoBehaviour
    {
        const float RefHeight = 1080f;

        static readonly string[] SlotLabels = { "攻撃A", "攻撃B", "移動" };
        static readonly string[] SlotKeys = { "←/1", "→/2", "↓/3", "↑/4" };
        static readonly Color[] RankColors =
        {
            new Color(0.6f, 0.6f, 0.6f),
            new Color(0.4f, 0.7f, 1f),
            new Color(0.4f, 1f, 0.5f),
            new Color(1f, 0.9f, 0.3f),
            new Color(1f, 0.6f, 0.2f),
            new Color(1f, 0.3f, 0.3f),
            new Color(1f, 0.3f, 1f),
        };

        [SerializeField] SkillSlotController slots;
        [SerializeField] SkillExecutor executor;
        [SerializeField] StyleRankSystem style;
        [SerializeField] Damageable player;
        [SerializeField] LockOnSystem lockOn;
        [SerializeField] ArenaDirector director;
        [SerializeField] Camera view;
        [SerializeField, Tooltip("WebGL ではOSのフォントを使えないため、日本語を含むフォントを指定する")] Font font;

        GUIStyle labelStyle;
        float scale;
        float virtualWidth;
        bool showHelp = true;
        int lastRank;
        float rankPulse;
        readonly float[] swappedAt = { -99f, -99f, -99f };
        readonly StringBuilder builder = new StringBuilder();

        EnemyController phaseEnemy;
        float phaseAnnouncedAt = -99f;

        void OnEnable()
        {
            slots.SlotChanged += OnSlotChanged;
            EnemyController.PhaseChanged += OnPhaseChanged;
        }

        void OnDisable()
        {
            slots.SlotChanged -= OnSlotChanged;
            EnemyController.PhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged(EnemyController enemy)
        {
            phaseEnemy = enemy;
            phaseAnnouncedAt = Time.unscaledTime;
        }
        void OnSlotChanged(SlotType slot) => swappedAt[(int)slot] = Time.unscaledTime;

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) showHelp = !showHelp;

            int rank = style.Meter.RankIndex;
            if (rank > lastRank) rankPulse = 1f;
            lastRank = rank;
            rankPulse = Mathf.MoveTowards(rankPulse, 0f, Time.unscaledDeltaTime * 3f);
        }

        void OnGUI()
        {
            if (LoadoutEditorUI.IsOpen) return;
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = false };
                if (font != null) labelStyle.font = font;
            }
            scale = Screen.height / RefHeight;
            virtualWidth = Screen.width / scale;

            // 文字は 1080p 基準の決まったサイズで描き、画面全体を拡大縮小する。
            // 文字サイズを画面や演出で変え続けると、動的フォントのテクスチャがあふれて
            // 再構築が無限に続き、WebGL でスタックオーバーフローになる。
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (director.State == ArenaDirector.GameState.StageSelect)
            {
                GUI.matrix = previousMatrix;
                return;
            }

            DrawWorldOverlays();
            DrawHealth();
            DrawWave();
            DrawBoss();
            DrawStyleRank();
            DrawSlots();
            DrawHelp();
            DrawCenterMessage();

            GUI.matrix = previousMatrix;
        }

        // ---------- 画面上部 ----------

        void DrawHealth()
        {
            Text(new Rect(40, 30, 400, 30), "HP", 22, Color.white);
            Bar(new Rect(80, 36, 400, 22), player.Health / player.MaxHealth, new Color(0.3f, 0.9f, 0.4f));
            Text(new Rect(40, 64, 440, 30), $"刻片 {director.Shards}", 22, new Color(0.8f, 0.9f, 1f));
            string mode = slots.Mode == SwapMode.Preset ? "プリセット方式" : "ラック方式";
            Text(new Rect(40, 92, 440, 30), $"切り替え: {mode}   <color=#AAAAAA>[P] 編成</color>", 18, new Color(1f, 1f, 1f, 0.8f),
                TextAnchor.UpperLeft, FontStyle.Normal);
        }

        void DrawWave()
        {
            var stage = director.CurrentStage;
            if (stage == null) return;
            string text;
            if (stage.kind == StageKind.Training) text = stage.displayName;
            else if (director.WaveNumber <= 0) text = stage.displayName;
            else if (stage.kind == StageKind.Endless) text = $"{stage.displayName}   {director.WaveLabel} {director.WaveNumber}";
            else text = $"{stage.displayName}   {director.WaveLabel}  {director.WaveNumber} / {director.WaveCount}";
            Text(new Rect(0, 24, virtualWidth, 40), text, 28, Color.white, TextAnchor.UpperCenter);
        }

        void DrawBoss()
        {
            var boss = director.Boss;
            if (boss == null) return;
            float width = 800f;
            float x = (virtualWidth - width) * 0.5f;
            var damageable = boss.Damageable;
            string weakness = WeaknessText(boss.Profile);
            string phase = boss.IsPhase2 ? "  <color=#FF6666>第二形態</color>" : "";
            Text(new Rect(x, 64, width, 30), boss.Profile.displayName + phase + (weakness.Length > 0 ? $"   <size=20>弱点 {weakness}</size>" : ""), 24,
                new Color(1f, 0.5f, 0.5f), TextAnchor.UpperCenter);
            Bar(new Rect(x, 96, width, 18), damageable.Health / damageable.MaxHealth, new Color(0.9f, 0.2f, 0.2f));
            if (damageable.MaxArmor > 0f)
            {
                Bar(new Rect(x, 118, width, 8), damageable.Armor / damageable.MaxArmor, new Color(1f, 0.7f, 0.2f));
            }
        }

        // ---------- スタイルランク(右側) ----------

        void DrawStyleRank()
        {
            var meter = style.Meter;
            float right = virtualWidth - 50f;
            float width = 320f;
            var color = RankColors[Mathf.Min(meter.RankIndex, RankColors.Length - 1)];

            Text(new Rect(right - width, 150, width, 30), "STYLE", 22, new Color(1f, 1f, 1f, 0.7f), TextAnchor.UpperRight);
            // ランクアップの演出は文字サイズではなく拡大表示で行う(文字サイズは固定)
            var matrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(Vector2.one * (1f + 0.35f * rankPulse), new Vector2(right, 240f) * scale);
            Text(new Rect(right - width, 170, width, 150), meter.RankName, 110, color, TextAnchor.UpperRight);
            GUI.matrix = matrix;
            Bar(new Rect(right - width, 320, width, 10), meter.RankProgress, color);

            float y = 345f;
            for (int i = style.Announcements.Count - 1; i >= 0; i--)
            {
                var announcement = style.Announcements[i];
                float age = Time.unscaledTime - announcement.time;
                var c = announcement.color;
                c.a = Mathf.Clamp01(1f - age / StyleRankSystem.AnnouncementLifetime) * 1.5f;
                Text(new Rect(right - width - 100, y, width + 100, 40), announcement.text, 30, c, TextAnchor.UpperRight);
                y += 38f;
            }

            DrawChain(right, width);

            Text(new Rect(right - width - 100, 690, width + 100, 30), executor.DebugLabel, 18, new Color(1f, 1f, 1f, 0.6f),
                TextAnchor.UpperRight, FontStyle.Normal);
        }

        /// <summary>チェンジチェイン(切り替え → ヒットで上がり、ダメージが増える)</summary>
        void DrawChain(float right, float width)
        {
            var chain = executor.SwapChain;
            var color = new Color(0.55f, 1f, 0.9f);
            if (chain.Chain <= 0)
            {
                Text(new Rect(right - width, 580, width, 30), "CHAIN  切り替え → ヒットでつながる", 16, new Color(1f, 1f, 1f, 0.45f),
                    TextAnchor.UpperRight, FontStyle.Normal);
                return;
            }
            int bonus = Mathf.RoundToInt((chain.DamageMultiplier - 1f) * 100f);
            string max = chain.Chain >= chain.MaxChain ? "  MAX" : "";
            Text(new Rect(right - width, 575, width, 50), $"CHAIN ×{chain.Chain}{max}", 38, color, TextAnchor.UpperRight);
            Text(new Rect(right - width, 622, width, 26), $"ダメージ +{bonus}%", 20, color, TextAnchor.UpperRight);
            Bar(new Rect(right - width, 652, width, 6), chain.Remaining(Time.time), color);
        }

        string WeaknessText(EnemyProfile profile)
        {
            if (profile == null || profile.weaknesses == null || profile.weaknesses.Length == 0) return "";
            builder.Clear();
            foreach (var weapon in profile.weaknesses)
            {
                var data = slots.GetWeaponData(weapon);
                if (data == null) continue;
                if (builder.Length > 0) builder.Append(' ');
                builder.Append($"<color=#{ColorUtility.ToHtmlStringRGB(data.color)}>{data.displayName}</color>");
            }
            return builder.ToString();
        }

        // ---------- スロット(下部) ----------

        void DrawSlots()
        {
            const float width = 330f;
            const float height = 128f;
            const float gap = 18f;
            float total = width * 3f + gap * 2f;
            float x0 = (virtualWidth - total) * 0.5f;
            float y = RefHeight - height - 30f;

            DrawBonusLine(x0, y - 78f, total);
            DrawChangeAttack(x0 + total - 260f, y - 124f);

            if (executor.SwapStrikeReady)
            {
                var strikeColor = new Color(1f, 0.9f, 0.2f);
                Text(new Rect(x0, y - 124f, total, 36), "SWAP STRIKE READY", 26, strikeColor, TextAnchor.UpperCenter);
                Bar(new Rect(x0 + total * 0.5f - 150f, y - 88f, 300f, 6f), executor.SwapStrikeRemaining, strikeColor);
            }

            for (int i = 0; i < 3; i++)
            {
                DrawSlot((SlotType)i, new Rect(x0 + (width + gap) * i, y, width, height));
            }

            if (slots.Mode == SwapMode.Preset)
            {
                // 左側に置く余白がなければ、スロットの上に横一列で並べる
                bool roomOnLeft = x0 - 300f >= 20f;
                if (roomOnLeft) DrawPresetBar(x0 - 300f, RefHeight - 30f, false);
                else DrawPresetBar(x0, y - 176f, true);
            }
        }

        /// <summary>プリセット方式: 4つのプリセットと切り替えキー。今切り替えるとキャンセルになるものは点滅させる。</summary>
        void DrawPresetBar(float x, float anchorY, bool horizontal)
        {
            const float width = 250f;
            const float height = 40f;
            int count = slots.Presets.Count;
            for (int i = 0; i < count; i++)
            {
                var rect = horizontal
                    ? new Rect(x + i * (width + 8f), anchorY, width, height)
                    : new Rect(x, anchorY - (count - i) * (height + 6f), width, height);
                var preset = slots.Presets[i];
                bool current = i == slots.PresetIndex;
                var a = preset.attackA;
                var data = a != null ? slots.GetWeaponData(a.weapon) : null;
                var color = data != null ? data.color : Color.white;

                Box(rect, new Color(0f, 0f, 0f, current ? 0.75f : 0.45f));
                Box(new Rect(rect.x, rect.y, 6, rect.height), color);
                if (current) Outline(rect, color, 3f);
                else if (executor.CanPresetCancel(i))
                {
                    float blink = Mathf.PingPong(Time.unscaledTime * 8f, 1f);
                    Outline(rect, Color.Lerp(new Color(0.4f, 0.9f, 1f), Color.white, blink), 3f);
                }
                Text(new Rect(rect.x + 14, rect.y + 7, rect.width - 20, rect.height), $"<color=#AAAAAA>[{SlotKeys[i]}]</color>  {preset.name}",
                    20, current ? Color.white : new Color(1f, 1f, 1f, 0.7f));
            }
        }

        void DrawSlot(SlotType slot, Rect rect)
        {
            var rack = slots.GetRack(slot);
            var current = ShownSkill(slot);
            var weaponData = current != null ? slots.GetWeaponData(current.weapon) : null;
            var weaponColor = weaponData != null ? weaponData.color : Color.white;
            bool active = executor.Current != null && executor.Current == current && !executor.IsFinisherActive;
            bool rackMode = slots.Mode == SwapMode.Rack;
            bool cancelReady = rackMode && executor.CanSwapCancelNow(slot);
            int i = (int)slot;
            float sinceSwap = Time.unscaledTime - swappedAt[i];

            // 切り替えた直後は枠が外へ広がって光る
            if (sinceSwap < 0.3f)
            {
                float t = sinceSwap / 0.3f;
                float grow = 14f * t;
                var glow = weaponColor;
                glow.a = 1f - t;
                Outline(new Rect(rect.x - grow, rect.y - grow, rect.width + grow * 2f, rect.height + grow * 2f), glow, 4f);
            }

            Box(rect, new Color(0f, 0f, 0f, active ? 0.75f : 0.5f));
            if (cancelReady)
            {
                // 今切り替えるとスワップキャンセルになる
                float blink = Mathf.PingPong(Time.unscaledTime * 8f, 1f);
                Outline(rect, Color.Lerp(new Color(0.4f, 0.9f, 1f), Color.white, blink), 4f);
            }
            else if (active)
            {
                Outline(rect, weaponColor, 3f);
            }
            Box(new Rect(rect.x, rect.y, 8, rect.height), weaponColor);

            string keyHint = rackMode ? $"   <color=#AAAAAA>[{SlotKeys[i]}]</color>" : "";
            Text(new Rect(rect.x + 20, rect.y + 8, rect.width - 30, 26), $"{SlotLabels[i]}{keyHint}", 20, Color.white);
            if (cancelReady)
            {
                Text(new Rect(rect.x + 20, rect.y + 8, rect.width - 34, 26), "今なら切替でキャンセル", 18, new Color(0.4f, 0.9f, 1f),
                    TextAnchor.UpperRight);
            }

            // 切り替えた直後は、新しいスキル名をパネルの上に出す
            if (sinceSwap < 0.8f && current != null)
            {
                var labelColor = weaponColor;
                labelColor.a = Mathf.Clamp01(1f - (sinceSwap - 0.4f) / 0.4f);
                float rise = 10f * Mathf.Clamp01(sinceSwap / 0.2f);
                Text(new Rect(rect.x, rect.y - 26 - rise, rect.width, 34), $"⇒ {current.displayName}", 26, labelColor,
                    TextAnchor.UpperCenter);
            }

            if (current == null) return;
            Text(new Rect(rect.x + 20, rect.y + 36, rect.width - 30, 44), current.displayName, 34, weaponColor);
            if (weaponData != null)
            {
                Text(new Rect(rect.x + 20, rect.y + 44, rect.width - 34, 30), weaponData.displayName, 20,
                    new Color(1f, 1f, 1f, 0.7f), TextAnchor.UpperRight);
            }

            if (!rackMode)
            {
                // プリセット方式: 次に切り替わる候補の代わりに、技の種類を出す
                Text(new Rect(rect.x + 20, rect.y + 88, rect.width - 30, 30), DescribeShort(current), 18,
                    new Color(1f, 1f, 1f, 0.6f), TextAnchor.UpperLeft, FontStyle.Normal);
                return;
            }

            // ラックの中身。現在の候補を強調し、次の候補に矢印を付ける
            builder.Clear();
            for (int j = 0; j < rack.Count; j++)
            {
                var skill = rack.Skills[j];
                var data = slots.GetWeaponData(skill.weapon);
                string hex = ColorUtility.ToHtmlStringRGB(data != null ? data.color : Color.white);
                if (j > 0) builder.Append("  ");
                if (j == rack.CurrentIndex) builder.Append($"<color=#{hex}><b>■{skill.displayName}</b></color>");
                else if (skill == rack.Next) builder.Append($"<color=#{hex}>▶{skill.displayName}</color>");
                else builder.Append($"<color=#888888>{skill.displayName}</color>");
            }
            Text(new Rect(rect.x + 20, rect.y + 88, rect.width - 30, 30), builder.ToString(), 18, Color.white, TextAnchor.UpperLeft,
                FontStyle.Normal);
        }

        /// <summary>
        /// スロットのパネルに出すスキル。実際に使われるもの(プリセット方式ではプリセットの中身)を出す。
        /// 以前はラックの候補を出していたため、プリセットを変えても表示が変わらなかった。
        /// </summary>
        public SkillData ShownSkill(SlotType slot) => slots.GetCurrent(slot);

        /// <summary>チェンジアタック(切り替えた瞬間の周囲攻撃)の準備</summary>
        void DrawChangeAttack(float x, float y)
        {
            bool ready = executor.ChangeAttackReady;
            var color = ready ? new Color(0.55f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.5f);
            Text(new Rect(x, y, 260, 30), ready ? "CHANGE ATTACK 準備OK" : "CHANGE ATTACK", 18, color, TextAnchor.UpperRight);
            Bar(new Rect(x + 60, y + 28, 200, 5), executor.ChangeAttackCharge, color);
        }

        static string DescribeShort(SkillData skill)
        {
            if (skill == null) return "";
            string description = skill.description ?? "";
            int end = description.IndexOf('。');
            if (end > 0) description = description.Substring(0, end);
            return description.Length > 18 ? description.Substring(0, 18) + "…" : description;
        }

        void DrawBonusLine(float x, float y, float width)
        {
            var bonus = slots.Bonus;
            var data = slots.GetWeaponData(bonus.Weapon);
            string weaponName = data != null ? data.displayName : bonus.Weapon.ToString();
            string text;
            Color color;
            switch (bonus.Kind)
            {
                case WeaponBonusKind.Mastery:
                    var finisher = slots.MasteryFinisher;
                    text = $"マスタリー({weaponName})   [R1 / F] フィニッシャー「{(finisher != null ? finisher.displayName : "-")}」";
                    color = data != null ? data.color : Color.white;
                    break;
                case WeaponBonusKind.Synergy:
                    text = $"シナジー({weaponName})   ひるみ +{Mathf.RoundToInt((data != null ? data.synergyStaggerBonus : 0f) * 100f)}%";
                    color = data != null ? Color.Lerp(data.color, Color.white, 0.3f) : Color.white;
                    break;
                case WeaponBonusKind.Arsenal:
                    text = $"アーセナル   スタイル獲得 x{style.Config.arsenalMultiplier:0.##}";
                    color = new Color(0.9f, 0.9f, 1f);
                    break;
                default:
                    return;
            }
            Text(new Rect(x, y, width, 36), text, 24, color, TextAnchor.UpperCenter);
        }

        // ---------- ワールド上の表示 ----------

        void DrawWorldOverlays()
        {
            if (view == null) return;

            foreach (var enemy in EnemyController.Active)
            {
                if (enemy.IsDead || enemy.Profile.isBoss) continue;
                if (!WorldToGui(enemy.transform.position + Vector3.up * (enemy.Height + 0.4f), out var point)) continue;

                var damageable = enemy.Damageable;
                var rect = new Rect(point.x - 40, point.y, 80, 7);
                Bar(rect, damageable.Health / damageable.MaxHealth, new Color(0.9f, 0.25f, 0.25f));
                if (damageable.MaxArmor > 0f)
                {
                    Bar(new Rect(rect.x, rect.y + 9, rect.width, 5), damageable.Armor / damageable.MaxArmor, new Color(1f, 0.7f, 0.2f));
                }
                // 弱点の武器種(今の攻撃Aの武器が弱点なら強調する)
                string weakness = WeaknessText(enemy.Profile);
                if (weakness.Length > 0)
                {
                    var attackA = slots.GetCurrent(SlotType.AttackA);
                    bool weakNow = attackA != null && enemy.Profile.IsWeakTo(attackA.weapon);
                    Text(new Rect(point.x - 70, point.y - 24, 140, 24), (weakNow ? "<b>弱点</b> " : "弱 ") + weakness, weakNow ? 17 : 15,
                        weakNow ? new Color(1f, 0.6f, 0.2f) : new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperCenter);
                }
                if (enemy.IsTelegraphing)
                {
                    Text(new Rect(point.x - 40, point.y - 40, 80, 40), "!", 34, enemy.Profile.telegraphColor, TextAnchor.UpperCenter);
                }
            }

            // ワイヤーで飛ぶ先のポイント
            var grapple = executor.GrapplePreview;
            if (grapple != null && WorldToGui(grapple.transform.position, out var grapplePoint))
            {
                float pulse = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * 10f);
                var matrix = GUI.matrix;
                GUIUtility.ScaleAroundPivot(Vector2.one * pulse, grapplePoint * scale);
                Text(new Rect(grapplePoint.x - 40, grapplePoint.y - 26, 80, 52), "◎", 44, new Color(0.8f, 0.55f, 1f),
                    TextAnchor.MiddleCenter);
                GUI.matrix = matrix;
                Text(new Rect(grapplePoint.x - 80, grapplePoint.y + 18, 160, 30), "Shift / ○", 16, new Color(0.9f, 0.8f, 1f),
                    TextAnchor.UpperCenter, FontStyle.Normal);
            }

            var target = lockOn.Target;
            if (target != null && WorldToGui(target.CenterPoint, out var lockPoint))
            {
                Text(new Rect(lockPoint.x - 40, lockPoint.y - 22, 80, 44), "◇", 40, new Color(1f, 0.3f, 0.3f), TextAnchor.MiddleCenter);
            }

            if (CombatFeedback.Instance != null)
            {
                foreach (var number in CombatFeedback.Instance.DamageNumbers)
                {
                    float age = Time.unscaledTime - number.time;
                    if (!WorldToGui(number.position + Vector3.up * age * 1.2f, out var p)) continue;
                    var c = number.color;
                    c.a = 1f - age / CombatFeedback.DamageNumberLifetime;
                    Text(new Rect(p.x - 60, p.y - 20, 120, 40), number.text, 26, c, TextAnchor.MiddleCenter);
                }
            }
        }

        bool WorldToGui(Vector3 world, out Vector2 point)
        {
            Vector3 screen = view.WorldToScreenPoint(world);
            point = new Vector2(screen.x / scale, (Screen.height - screen.y) / scale);
            return screen.z > 0f;
        }

        // ---------- ヘルプ・メッセージ ----------

        void DrawHelp()
        {
            const float x = 40f;
            const float y = 120f;
            if (!showHelp)
            {
                Text(new Rect(x, y, 400, 30), "[F1] 操作説明", 18, new Color(1f, 1f, 1f, 0.6f), TextAnchor.UpperLeft, FontStyle.Normal);
                return;
            }

            const string help =
                "<b>操作(キーボード / ゲームパッド)</b>\n" +
                "移動            WASD / 左スティック\n" +
                "カメラ          マウス / 右スティック\n" +
                "攻撃A           左クリック・J / □\n" +
                "攻撃B           右クリック・K / △(居合は長押しで溜め)\n" +
                "移動スキル      Shift・L / ○\n" +
                "ジャンプ        Space / ×\n" +
                "ロックオン      Tab・中クリック / L1\n" +
                "切り替え        1〜4  ・  十字キー\n" +
                "  ラック方式: 1/2/3 で各スロット  プリセット方式: 1〜4 で一括\n" +
                "フィニッシャー  F / R1(マスタリー時)\n" +
                "スキル編成      P / Select(切り替え方式もここで)\n" +
                "もう一度 / 選択に戻る  R / T\n" +
                "\n" +
                "<b>コツ</b>\n" +
                "・技の硬直中にそのスロットを切り替えると硬直をキャンセル\n" +
                "・切り替えた直後の一撃は強化(SWAP STRIKE)\n" +
                "・同じ技の連発はスタイルが伸びない\n" +
                "・敵の頭上の「弱」の武器で攻撃すると 1.5倍\n" +
                "・切り替え → ヒットでCHAIN(最大×5、ダメージ +50%)\n" +
                "・切り替えた瞬間、新しい武器で周りを攻撃(3秒ごと)\n" +
                "・ワイヤーは視界内で一番近い ◎ へ飛ぶ(ぶら下がり中に\n" +
                "   ワイヤーで次へ / Space でジャンプ / 攻撃で空中攻撃)\n" +
                "・[F1] でこの説明を閉じる";

            Box(new Rect(x - 10, y - 8, 600, 650), new Color(0f, 0f, 0f, 0.45f));
            Text(new Rect(x, y, 580, 640), help, 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal);
        }

        void DrawCenterMessage()
        {
            string message = null;
            Color color = Color.white;
            switch (director.State)
            {
                case ArenaDirector.GameState.Starting:
                    message = "READY";
                    break;
                case ArenaDirector.GameState.Cleared:
                    string best = style.Config.rankNames[Mathf.Min(style.HighestRank, style.Config.rankNames.Length - 1)];
                    string record = director.NewRecord ? "   <color=#FFE066>NEW RECORD!</color>" : "";
                    message = $"STAGE CLEAR\n<size=30>タイム {director.ElapsedTime:0.0}秒   最高ランク {best}   刻片 {director.Shards}{record}</size>";
                    color = new Color(1f, 0.9f, 0.4f);
                    break;
                case ArenaDirector.GameState.GameOver:
                    bool endless = director.CurrentStage != null && director.CurrentStage.kind == StageKind.Endless;
                    string reached = endless ? $"到達 WAVE {director.WaveNumber}{(director.NewRecord ? "   <color=#FFE066>NEW RECORD!</color>" : "")}" : "";
                    message = $"GAME OVER\n<size=30>{reached}</size>";
                    color = new Color(1f, 0.35f, 0.35f);
                    break;
            }
            // ボスの第二形態の告知(2秒)
            float sincePhase = Time.unscaledTime - phaseAnnouncedAt;
            if (message == null && phaseEnemy != null && sincePhase < 2f)
            {
                var c = phaseEnemy.Profile.telegraphColor;
                c.a = Mathf.Clamp01((2f - sincePhase) / 0.5f);
                Text(new Rect(0, RefHeight * 0.3f, virtualWidth, 200),
                    $"{phaseEnemy.Profile.displayName}  第二形態\n<size=30>{phaseEnemy.Profile.phase2Message}</size>", 64, c, TextAnchor.UpperCenter);
            }

            if (message == null) return;
            Text(new Rect(0, RefHeight * 0.28f, virtualWidth, 300), message, 72, color, TextAnchor.UpperCenter);

            if (director.State == ArenaDirector.GameState.Cleared || director.State == ArenaDirector.GameState.GameOver)
            {
                DrawResultButtons(RefHeight * 0.28f + 190f);
            }
        }

        void DrawResultButtons(float y)
        {
            bool next = director.State == ArenaDirector.GameState.Cleared && director.HasNextStage;
            int count = next ? 3 : 2;
            const float width = 260f;
            const float gap = 20f;
            float x = (virtualWidth - (width * count + gap * (count - 1))) * 0.5f;

            if (next)
            {
                if (ResultButton(new Rect(x, y, width, 56), "次のステージ [N]", new Color(0.2f, 0.55f, 0.9f))) director.NextStage();
                x += width + gap;
            }
            if (ResultButton(new Rect(x, y, width, 56), "もう一度 [R]", new Color(0.3f, 0.45f, 0.3f))) director.Retry();
            x += width + gap;
            if (ResultButton(new Rect(x, y, width, 56), "ステージ選択 [T]", new Color(0.3f, 0.3f, 0.36f))) director.BackToStageSelect();
        }

        bool ResultButton(Rect rect, string text, Color color)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            Box(rect, hover ? Color.Lerp(color, Color.white, 0.2f) : color);
            Text(rect, text, 24, Color.white, TextAnchor.MiddleCenter);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        // ---------- 描画の補助 ----------

        // GUI.matrix で拡大縮小しているので、座標は 1080p 基準のまま使う
        static Rect Scaled(Rect r) => r;

        void Text(Rect rect, string text, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft,
            FontStyle fontStyle = FontStyle.Bold)
        {
            labelStyle.fontSize = size;
            labelStyle.alignment = anchor;
            labelStyle.fontStyle = fontStyle;

            var scaled = Scaled(rect);
            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, color.a * 0.8f);
            GUI.Label(new Rect(scaled.x + 2, scaled.y + 2, scaled.width, scaled.height), text, labelStyle);
            GUI.color = color;
            GUI.Label(scaled, text, labelStyle);
            GUI.color = previous;
        }

        void Box(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(Scaled(rect), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        void Outline(Rect rect, Color color, float thickness)
        {
            Box(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Box(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Box(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Box(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        void Bar(Rect rect, float fill, Color color)
        {
            Box(rect, new Color(0f, 0f, 0f, 0.6f));
            Box(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill), rect.height), color);
        }
    }
}
