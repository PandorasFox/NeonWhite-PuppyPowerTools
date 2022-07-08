using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Guirao.UltimateTextDamage;

public class DamageDebugLogging_Patch {
    [HarmonyPatch(typeof(Breakable), "OnHit")]
    [HarmonyPrefix]
    public static void BreakableOnHitLogging(Breakable __instance, int damage, BaseDamageable.DamageSource damageSource) {
        if (__instance._damageableType == Breakable.DamageableType.Crystal /*&& damage == 5*/ ) {
            //MelonLogger.Msg(__instance._damageableType.ToString() + " took " + damage.ToString() + " dmg from source " + damageSource.ToString());

            Type baseDamageable = typeof(BaseDamageable);
            FieldInfo breakable = baseDamageable.GetField("_breakablePlatform", BindingFlags.NonPublic | BindingFlags.Instance);

            BaseDamageable attached_damageable = (BaseDamageable)breakable.GetValue(__instance);

            if (attached_damageable != null) {
                MelonLogger.Msg("crystal's 'breakablePlatform' is of type " + attached_damageable._damageableType.ToString() + " and has " + attached_damageable.maxHealth.ToString() + " max hp");
            }
        }
    }

    static int platform_pair_id = 0; // todo reset this on playerDie

    [HarmonyPatch(typeof(Game), "WinRoutine")]
    [HarmonyPatch(typeof(MechController), "Die")]
    [HarmonyPrefix] // these need to prefix bc PlayNext handles the Started state from within this call
    public static void OnLevelFinishDieRestartEtc() {
        platform_pair_id = 0;
    }

    [HarmonyPatch(typeof(BaseDamageable), "FindBreakablePlatform")]
    [HarmonyPostfix]
    public static void BreakablePlatformSetupLogging(BaseDamageable __instance, ref BaseDamageable __result) {
        if (__result != null) {
            MelonLogger.Msg("Parent " + __instance.GetDamageableType().ToString() + " found underlying breakable of type " + __result.GetDamageableType().ToString() + " during its setup. zamn!");
            UltimateTextDamageManager.Instance.Add("child " + platform_pair_id.ToString(), __instance.transform.position, "default");
            UltimateTextDamageManager.Instance.Add("platform " + platform_pair_id.ToString(), __result.transform.position, "default");
            platform_pair_id++;
        }
    }
}

namespace Puppy {
    public class PuppyPowertools : MelonMod {
        Speedometer speedometer = null;
        ChapterTimer chapter_timer = null;

        PuppyPowertools() {
            this.speedometer = new Speedometer();
            this.chapter_timer = new ChapterTimer();
        }

        // shared GUI helper funcs

        public static GUIStyle TextStyle(int size) {
            GUIStyle style = new GUIStyle();

            style.fixedHeight = size;
            style.fontSize = size;

            return style;
        }
        public static void DrawText(int x_offset, int y_offset, string s, int size, Color c) {
            GUIStyle style = TextStyle(size);
            style.normal.textColor = c;

            GUIStyle outline_style = TextStyle(size);
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

        public static string Vec3ToString(Vector3 v) {
            return v.x.ToString("N2") + ", " + v.y.ToString("N2") + ", " + v.z.ToString("N2");
        }

        // TODO: move the speedometer, chapterTimer, and shared GUI funcs into their own proper files for readability :)

        public class Speedometer : MelonMod {
            public bool is_dashing = false;
            public Vector3 total_velocity = Vector3.zero;

            public float lateral_velocity_magnitude = 0f;
            public float vertical_velocity = 0f;

            public Vector3 pos = Vector3.zero;
            public Vector3 dir = Vector3.zero;
            public float facing_direction = 0f;
            public float facing_angle = 0f;

            public static MelonPreferences_Category speedometer_config;
            public static MelonPreferences_Entry<Color> text_color;
            public static MelonPreferences_Entry<Color> text_color_dashing;
            public static MelonPreferences_Entry<Color> text_color_fast;
            public static MelonPreferences_Entry<Color> text_color_slow;

            public static MelonPreferences_Entry<int> x_offset;
            public static MelonPreferences_Entry<int> y_offset;
            public static MelonPreferences_Entry<int> font_size;
            public static MelonPreferences_Entry<bool> verbose_display;
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
            public override void OnGUI() {
                if (RM.mechController && RM.mechController.GetIsAlive()) {
                    int size = font_size.Value;
                    int local_y_offset = y_offset.Value;

                    GUIStyle style = TextStyle(size);
                    // draw position
                    if (verbose_display.Value) {
                        DrawText(x_offset.Value, local_y_offset, Vec3ToString(this.pos), size, text_color.Value);
                        local_y_offset += font_size.Value + 2;

                        //facing angle and vector direction
                        DrawText(x_offset.Value, local_y_offset,
                            this.facing_direction.ToString("N2") + ", " + this.facing_angle.ToString("N2")
                            + " | " + Vec3ToString(this.dir), size, text_color.Value);
                        local_y_offset += font_size.Value + 2;

                        // velocity vector
                        DrawText(x_offset.Value, local_y_offset, "Velocity: " + Vec3ToString(this.total_velocity), size, text_color.Value);
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
                        size, color
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
                        size, color
                    );
                    local_y_offset += font_size.Value + 2;

                }
            }
        }

        public class ChapterTimer : MelonMod {
            public enum ChapterTimerState {
                NOT_STARTED,
                STARTED,
                LEVEL_FINISHED,
            }

            public static long chapter_time_sum = 0;
            public static ChapterTimerState chapter_timer_state = ChapterTimerState.NOT_STARTED;
            public bool chapter_start_hooked = false;

            public static MelonPreferences_Category chapter_timer_config;
            public static MelonPreferences_Entry<bool> chapter_time_display;
            public static MelonPreferences_Entry<Color> chapter_text_color;
            public static MelonPreferences_Entry<int> chapter_x_offset;
            public static MelonPreferences_Entry<int> chapter_y_offset;
            public static MelonPreferences_Entry<int> chapter_font_size;

            public override void OnGUI() {
                if (chapter_time_display.Value) {
                    long total_chapter_time = chapter_time_sum;
                    if (chapter_timer_state == ChapterTimerState.STARTED) {
                        total_chapter_time += Singleton<Game>.Instance.GetCurrentLevelTimerMicroseconds();
                    }
                    DrawText(chapter_x_offset.Value, chapter_y_offset.Value, Game.GetTimerFormattedMillisecond(total_chapter_time), chapter_font_size.Value, chapter_text_color.Value);
                }
            }

            public override void OnApplicationStart() {
                chapter_timer_config = MelonPreferences.CreateCategory("Chapter Timer config");
                chapter_time_display = chapter_timer_config.CreateEntry("Chapter timer display enabled", false);

                chapter_x_offset = chapter_timer_config.CreateEntry("X Offset", 200);
                chapter_y_offset = chapter_timer_config.CreateEntry("Y Offset", 30);

                chapter_font_size = chapter_timer_config.CreateEntry("Font Size", 20);
                chapter_text_color = chapter_timer_config.CreateEntry("Text color (Default)", Color.yellow);

                if (chapter_time_display.Value) {
                    ApplyChapterHooks();
                }
            }
            public override void OnPreferencesSaved() {
                if (chapter_start_hooked && !chapter_time_display.Value) {
                    this.HarmonyInstance.UnpatchSelf();
                    chapter_start_hooked = false;
                } else if (!chapter_start_hooked && chapter_time_display.Value) {
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
        }

        public static MelonPreferences_Category poweruserprefs;
        public static MelonPreferences_Entry<int> level_rush_seed;

        public override void OnApplicationStart() {
            poweruserprefs = MelonPreferences.CreateCategory("PowerPrefs adjustments");
            level_rush_seed = poweruserprefs.CreateEntry("Level Rush Seed (negative is random)", -1);
            //this.HarmonyInstance.PatchAll(typeof(DamageDebugLogging_Patch));

            this.speedometer.OnApplicationStart();
            this.chapter_timer.OnApplicationStart();
        }

        public override void OnPreferencesSaved() {
            GameDataManager.powerPrefs.seedForLevelRushLevelOrder_NegativeValuesMeansRandomizeSeed = level_rush_seed.Value;
            this.chapter_timer.OnPreferencesSaved();
        }

        public override void OnLateUpdate() {
            this.speedometer.OnLateUpdate();
        }

        public override void OnGUI() {
            this.speedometer.OnGUI();
            this.chapter_timer.OnGUI();
        }
    }
}
