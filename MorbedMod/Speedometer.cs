using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MelonLoader;
using HarmonyLib;
using UnityEngine;

public enum ChapterTimerState {
    NOT_STARTED,
    STARTED,
    LEVEL_FINISHED,
}

namespace MorbedMod {
    // TODO: maybe rename this class bc it's now the MorbedClass, and properly compartmentalize its components

    public class Speedometer : MelonMod {
        public bool is_dashing = false;
        public Vector3 total_velocity = Vector3.zero;

        public float lateral_velocity_magnitude = 0f;
        public float vertical_velocity = 0f;

        public Vector3 pos = Vector3.zero;
        public Vector3 dir = Vector3.zero;
        public float facing_direction = 0f;
        public float facing_angle = 0f;

        public static long chapter_time_sum = 0;
        public bool chapter_start_hooked = false;
        public static ChapterTimerState chapter_timer_state = ChapterTimerState.NOT_STARTED;

        // Preferences

        public static MelonPreferences_Category speedometer_config;
        public static MelonPreferences_Entry<Color> text_color;
        public static MelonPreferences_Entry<Color> text_color_dashing;
        public static MelonPreferences_Entry<Color> text_color_fast;
        public static MelonPreferences_Entry<Color> text_color_slow;

        public static MelonPreferences_Entry<int> x_offset;
        public static MelonPreferences_Entry<int> y_offset;
        public static MelonPreferences_Entry<int> font_size;
        public static MelonPreferences_Entry<bool> verbose_display;

        public static MelonPreferences_Category misc_speedrun_config;

        public static MelonPreferences_Entry<int> level_rush_seed;

        public static MelonPreferences_Entry<bool> chapter_time_display;
        public static MelonPreferences_Entry<Color> chapter_text_color;
        public static MelonPreferences_Entry<int> chapter_x_offset;
        public static MelonPreferences_Entry<int> chapter_y_offset;
        public static MelonPreferences_Entry<int> chapter_font_size;

        public override void OnApplicationStart() {
            speedometer_config = MelonPreferences.CreateCategory("Speedometer Config");
            verbose_display = speedometer_config.CreateEntry("Verbose Info", false);

            text_color = speedometer_config.CreateEntry("Text color (Default)", Color.yellow);
            text_color_dashing = speedometer_config.CreateEntry("Text color (Dashing)", Color.blue);
            text_color_fast = speedometer_config.CreateEntry("Text color (Fast)", Color.green);
            text_color_slow = speedometer_config.CreateEntry("Text color (Slow)", Color.red);

            x_offset = speedometer_config.CreateEntry("X Offset", 30);
            y_offset = speedometer_config.CreateEntry("Y Offset", 30);
            font_size = speedometer_config.CreateEntry("Font Size", 20);

            misc_speedrun_config = MelonPreferences.CreateCategory("Misc. MorbedMod config");
            level_rush_seed = misc_speedrun_config.CreateEntry("Rush Seed (random if negative)", -1);
            chapter_time_display = misc_speedrun_config.CreateEntry("Chapter Rush timer display", false);

            chapter_x_offset = misc_speedrun_config.CreateEntry("X Offset", 200);
            chapter_y_offset = misc_speedrun_config.CreateEntry("Y Offset", 30);
            // TODO: rewrite styles handling :woozy:
            // chapter_font_size = misc_speedrun_config.CreateEntry("Font Size", 20);
            chapter_text_color = misc_speedrun_config.CreateEntry("Text color (Default)", Color.yellow);

            if (chapter_time_display.Value) {
                ApplyChapterHooks();
            }
        }

        public void ApplyChapterHooks() {
            if (!chapter_start_hooked) {
                this.HarmonyInstance.PatchAll(typeof(ChapterTimerHooks_Patch));
                chapter_start_hooked = true;
            }
        }

        public class ChapterTimerHooks_Patch {
            // initial level start
            [HarmonyPatch(typeof(Game), "PlayLevel", new Type[] { typeof(string), typeof(bool), typeof(Action) })]
            [HarmonyPostfix]
            public static void HookLevelStart_FromArchive(bool fromArchive) {
                if (fromArchive) {
                    chapter_time_sum = 0;
                    chapter_timer_state = ChapterTimerState.STARTED;
                }
            }

            // level restart passes the actual LevelData obj
            // just restart the timer state.
            [HarmonyPatch(typeof(Game), "PlayLevel", new Type[] { typeof(LevelData), typeof(bool), typeof(bool) })]
            [HarmonyPostfix]
            public static void HookLevelRestart_FromArchive() {
                chapter_timer_state = ChapterTimerState.STARTED;
            }

            [HarmonyPatch(typeof(Game), "PlayNextArchiveLevel")]
            [HarmonyPostfix]
            public static void onNextLevel() {
                chapter_timer_state = ChapterTimerState.STARTED;
            }

            [HarmonyPatch(typeof(Game), "WinRoutine")]
            [HarmonyPatch(typeof(MechController), "Die")]
            [HarmonyPrefix] // these need to prefix bc PlayNext handles the Started state from within this call
            public static void OnLevelFinishDieRestartEtc() {
                chapter_time_sum += Singleton<Game>.Instance.GetCurrentLevelTimerMicroseconds();
                chapter_timer_state = ChapterTimerState.LEVEL_FINISHED;
            }
        }

        public override void OnPreferencesSaved() {
            GameDataManager.powerPrefs.seedForLevelRushLevelOrder_NegativeValuesMeansRandomizeSeed = level_rush_seed.Value;
            if (chapter_start_hooked && !chapter_time_display.Value) {
                this.HarmonyInstance.UnpatchSelf();
                chapter_start_hooked = false;
            } else if (!chapter_start_hooked && chapter_time_display.Value) {
                ApplyChapterHooks();
            }
        }

        public override void OnLateUpdate() {
            if (RM.mechController && RM.mechController.GetIsAlive()) {
                this.pos = RM.playerPosition;
                this.dir = RM.mechController.playerCamera.PlayerCam.transform.forward;
                this.facing_direction = RM.drifter.mouseLookX.RotationX;
                this.facing_angle = RM.drifter.mouseLookY.RotationY;

                Vector3 normal_velocity = RM.drifter.Velocity;
                Vector3 move_velocity = RM.drifter.MovementVelocity;

                this.total_velocity = normal_velocity + move_velocity;

                this.is_dashing = RM.drifter.GetIsDashing();

                this.vertical_velocity = this.total_velocity.y;

                this.lateral_velocity_magnitude = Mathf.Sqrt(
                    Mathf.Pow(total_velocity.x, 2f) + Mathf.Pow(total_velocity.z, 2f)
                );
            }
        }

        public string Vec3ToString(Vector3 v) {
            return v.x.ToString("N2") + ", " + v.y.ToString("N2") + ", " + v.z.ToString("N2");
        }

        public static GUIStyle SpeedometerStyle() {
            GUIStyle style = new GUIStyle();

            style.fixedHeight = font_size.Value;
            style.fontSize = font_size.Value;

            return style;
        }

        public void DrawText(int x_offset, int y_offset, string s, Color c) {
            GUIStyle style = SpeedometerStyle();
            style.normal.textColor = c;

            GUIStyle outline_style = SpeedometerStyle();
            outline_style.normal.textColor = Color.black;
            int outline_strength = 2;

            Rect r = new Rect(x_offset, y_offset, 120, 30);

            for (int i = -outline_strength; i <= outline_strength; ++i) {
                GUI.Label(new Rect(r.x - outline_strength, r.y + i, r.width, r.height), s, outline_style);
                GUI.Label(new Rect(r.x + outline_strength, r.y + i, r.width, r.height), s, outline_style);
            }
            for (int i = -outline_strength + 1; i <= outline_strength - 1; ++i) {
                GUI.Label(new Rect(r.x + i, r.y - outline_strength, r.width, r.height), s, outline_style);
                GUI.Label(new Rect(r.x + i, r.y + outline_strength, r.width, r.height), s, outline_style);
            }
            GUI.Label(r, s, style);
        }

        public override void OnGUI() {
            if (chapter_time_display.Value) {
                long total_chapter_time = chapter_time_sum;
                if (chapter_timer_state == ChapterTimerState.STARTED) {
                    total_chapter_time += Singleton<Game>.Instance.GetCurrentLevelTimerMicroseconds();
                }
                DrawText(chapter_x_offset.Value, chapter_y_offset.Value, Game.GetTimerFormattedMillisecond(total_chapter_time), chapter_text_color.Value);
            }

            if (RM.mechController && RM.mechController.GetIsAlive()) {
                int local_y_offset = y_offset.Value;

                GUIStyle style = SpeedometerStyle();
                // draw position
                if (verbose_display.Value) {
                    DrawText(x_offset.Value, local_y_offset, Vec3ToString(this.pos), text_color.Value);
                    local_y_offset += font_size.Value + 2;

                    //facing angle and vector direction
                    DrawText(x_offset.Value, local_y_offset,
                        this.facing_direction.ToString("N2") + ", " + this.facing_angle.ToString("N2") 
                        + " | " +  Vec3ToString(this.dir), text_color.Value);
                    local_y_offset += font_size.Value + 2;

                    // velocity vector
                    DrawText(x_offset.Value, local_y_offset, "Velocity: " + Vec3ToString(this.total_velocity), text_color.Value);
                    local_y_offset += font_size.Value + 2;
                }

                // draw Velocity magnitudes
                Color color = text_color.Value;
                if (this.is_dashing) {
                    color = text_color_dashing.Value;
                } else if (lateral_velocity_magnitude > 18.75) {
                    color = text_color_fast.Value;
                } else if (lateral_velocity_magnitude < 18.7) { // prevent rounding flickers
                    color = text_color_slow.Value;
                }
                DrawText(x_offset.Value, local_y_offset,
                    "Lateral: " + this.lateral_velocity_magnitude.ToString("N2"),
                    color
                );
                local_y_offset += font_size.Value + 2;

                if (this.is_dashing) {
                    color = text_color_dashing.Value;
                } else if (vertical_velocity > 0.1) {
                    color = text_color_fast.Value;
                } else if (vertical_velocity < 0.1) {
                    color = text_color_slow.Value;
                }

                DrawText(x_offset.Value, local_y_offset,
                    "y: " + this.vertical_velocity.ToString("N2"),
                    color
                );
                local_y_offset += font_size.Value + 2;

            }
        }
    }
}
