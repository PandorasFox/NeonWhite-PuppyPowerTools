using System;
using System.Reflection;

using MelonLoader;
using HarmonyLib;
using UnityEngine;
using I2.Loc;

namespace Puppy {
    public class PuppyPowertools : MelonMod {
        Speedometer speedometer = null;
        ChapterTimer chapter_timer = null;
        VfxToggles vfx_toggles = null;
        CardCustomizations custom_cards = null;

        PuppyPowertools() {
            this.speedometer = new Speedometer();
            this.chapter_timer = new ChapterTimer();
            this.vfx_toggles = new VfxToggles();
            this.custom_cards = new CardCustomizations();
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

            public float coyote_time = 0f;
            public float swap_time = 0f;

            public static MelonPreferences_Category speedometer_config;
            public static MelonPreferences_Entry<bool> speedometer_enabled;
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
                speedometer_enabled = speedometer_config.CreateEntry("Speedometer Enabled", true);
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
                if (speedometer_enabled.Value && RM.mechController && RM.mechController.GetIsAlive()) {
                    this.pos = RM.playerPosition;
                    this.dir = RM.mechController.playerCamera.PlayerCam.transform.forward;
                    this.facing_direction = RM.drifter.mouseLookX.RotationX;
                    this.facing_angle = RM.drifter.mouseLookY.RotationY;

                    Vector3 normal_velocity = RM.drifter.Velocity;
                    Vector3 move_velocity = RM.drifter.MovementVelocity;

                    //this.total_velocity = normal_velocity + move_velocity;
                    this.total_velocity = RM.drifter.Motor.BaseVelocity;

                    this.is_dashing = RM.drifter.GetIsDashing();

                    this.vertical_velocity = this.total_velocity.y;

                    this.lateral_velocity_magnitude = Mathf.Sqrt(
                        Mathf.Pow(total_velocity.x, 2f) + Mathf.Pow(total_velocity.z, 2f)
                    );

                    // TODO temp coyote time/swap time shit
                    Type white = typeof(FirstPersonDrifter); // yeehaw cus it controls the guns. get it?
                    FieldInfo coyote_jump_field = white.GetField("jumpForgivenessTimer", BindingFlags.NonPublic | BindingFlags.Instance);
                    this.coyote_time = (float)coyote_jump_field.GetValue(RM.drifter);

                    Type yeehaw = typeof(MechController); // yeehaw cus it controls the guns. get it?
                    FieldInfo reload_timer_field = yeehaw.GetField("weaponReloadTimer", BindingFlags.NonPublic | BindingFlags.Instance);
                    this.swap_time = (float)reload_timer_field.GetValue(RM.mechController);

                }
            }
            public override void OnGUI() {
                if (speedometer_enabled.Value && RM.mechController && RM.mechController.GetIsAlive()) {
                    int size = font_size.Value;
                    int local_y_offset = y_offset.Value;

                    GUIStyle style = TextStyle(size);
                    // draw position
                    if (verbose_display.Value) {
                        DrawText(x_offset.Value, local_y_offset, Vec3ToString(this.pos), size, text_color.Value);
                        local_y_offset += font_size.Value + 2;

                        // TODO: maybe split facing angle/direction into a 'nicer' representation + separate toggle?
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

                    // TODO NOTE ETC temp reload timer/coyote time value logging

                    if (verbose_display.Value) {
                        if (swap_time < 0) swap_time = 0;
                        DrawText(x_offset.Value, local_y_offset, "Swap Timer: " + this.swap_time.ToString(), size, text_color.Value);
                        local_y_offset += font_size.Value + 2;

                        if (coyote_time < 0) coyote_time = 0;
                        Color c = Color.red;
                        if (coyote_time > 0) c = Color.green;

                        DrawText(x_offset.Value, local_y_offset, "Coyote Time: " + this.coyote_time.ToString(), size, c);
                        local_y_offset += font_size.Value + 2;
                    }
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
            private HarmonyLib.Harmony harmony_instance = new HarmonyLib.Harmony("chapter_timer");

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
                    this.harmony_instance.UnpatchSelf();
                    chapter_start_hooked = false;
                } else if (!chapter_start_hooked && chapter_time_display.Value) {
                    ApplyChapterHooks();
                }
            }

            public void ApplyChapterHooks() {
                if (!chapter_start_hooked) {
                    this.harmony_instance.PatchAll(typeof(ChapterTimerHooks_Patch));
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

        public class VfxToggles : MelonMod {
            public static MelonPreferences_Category VfxSettings;
            public static MelonPreferences_Entry<bool> sun_disabled;
            public static MelonPreferences_Entry<bool> reflections_disabled;
            public static MelonPreferences_Entry<bool> bloom_disabled;
            public static MelonPreferences_Entry<bool> fireball_disabled;
            public static MelonPreferences_Entry<bool> stomp_splashbang_disabled;
            public static MelonPreferences_Entry<bool> stomp_splash_disabled;
            public static MelonPreferences_Entry<bool> crt_effect_disabled;

            private HarmonyLib.Harmony harmony_instance = new HarmonyLib.Harmony("sunkiller");

            public override void OnApplicationStart() {
                VfxSettings = MelonPreferences.CreateCategory("VFX Toggles");
                sun_disabled = VfxSettings.CreateEntry("Disable sun [requires restart to re-enable :3]", false);
                bloom_disabled = VfxSettings.CreateEntry("Disable bloom", false);
                reflections_disabled = VfxSettings.CreateEntry("Disable reflection flares", false);
                fireball_disabled = VfxSettings.CreateEntry("Disable fireball screen effect", false);
                stomp_splashbang_disabled = VfxSettings.CreateEntry("Disable stomp splashbang", false);
                crt_effect_disabled = VfxSettings.CreateEntry("Disable CRT effect", false);

                // i cannot be fucked to make these dynamically unloadable given that most people will turn this shit off and never think about them again
                harmony_instance.PatchAll(typeof(FuckTheSun_Patch));
                harmony_instance.PatchAll(typeof(AntiStompFlashbang_Patch));
                harmony_instance.PatchAll(typeof(CRTTogglePatch));
            }

            public override void OnPreferencesSaved() {
                FuckTheSun();
            }

            public static void FuckTheSun() {
                if (!sun_disabled.Value && !reflections_disabled.Value && !bloom_disabled.Value) return;

                Beautify.Universal.Beautify[] beautify_shit = UnityEngine.Object.FindObjectsOfType<Beautify.Universal.Beautify>();
                for (int i = 0; i < beautify_shit.Length; ++i) {
                    if (sun_disabled.Value) beautify_shit[i].sunFlaresIntensity = new UnityEngine.Rendering.ClampedFloatParameter(0f, 0f, 0f, true);
                    if (bloom_disabled.Value) beautify_shit[i].bloomIntensity = new UnityEngine.Rendering.ClampedFloatParameter(0f, 0f, 0f, true);
                    if (reflections_disabled.Value) beautify_shit[i].anamorphicFlaresIntensity = new UnityEngine.Rendering.ClampedFloatParameter(0f, 0f, 0f, true);
                }
            }

            // TODO FullscreenImageData for the perlin noise waves?

            public class CRTTogglePatch {
                // TODO ???
                [HarmonyPatch(typeof(CRTRendererFeature.CRTEffectPass), "Execute")]
                [HarmonyPrefix]
                public static bool ToCRTOrNotToCRT() {
                    return !crt_effect_disabled.Value;
                }
            }

            public class AntiStompFlashbang_Patch {
                [HarmonyPatch(typeof(ScannerEffect), "OnStomp")]
                [HarmonyPostfix]
                public static void AntiFlashbang(ScannerEffect __instance) {
                    if (!stomp_splashbang_disabled.Value) return;
                    // grab __instance._currentProfile by reflection
                    Type Scanner = typeof(ScannerEffect);
                    FieldInfo profileField = Scanner.GetField("_currentProfile", BindingFlags.NonPublic | BindingFlags.Instance);
                    ScannerEffectProfile currentProfile = (ScannerEffectProfile) profileField.GetValue(__instance);
                    // turn off the stomp particle immediately!
                    currentProfile.particles.Stop();
                }
            }

            public class FuckTheSun_Patch {
                // initial level start
                [HarmonyPatch(typeof(Game), "PlayLevel", new Type[] { typeof(string), typeof(bool), typeof(Action) })]
                [HarmonyPostfix]
                public static void HookLevelStart_FromArchive(bool fromArchive) {
                    FuckTheSun();
                }

                [HarmonyPatch(typeof(Game), "PlayNextArchiveLevel")]
                [HarmonyPostfix]
                public static void onNextLevel() {
                    FuckTheSun();
                }

                [HarmonyPatch(typeof(LevelRush), "PlayCurrentLevelRushMission")]
                [HarmonyPostfix]
                public static void onNextLevelRush() {
                    FuckTheSun();
                }
            }

            public override void OnUpdate() {
                if (fireball_disabled.Value && RM.mechController) {
                    RM.mechController.fireballParticles.Stop();
                    RM.mechController.fireballTrailParticles.Stop();
                }
            }
        }

        public class CardCustomizations : MelonMod {
            public static MelonPreferences_Category CardSettings;
            public static MelonPreferences_Entry<bool> EnableCardCustomizations;
            /*
            public static MelonPreferences_Entry<Color> KatanaColor;
            public static MelonPreferences_Entry<Color> FistsColor;
            public static MelonPreferences_Entry<Color> ElevateColor;
            */
            public static MelonPreferences_Entry<String> ElevateText;
            //public static MelonPreferences_Entry<Color> PurifyColor;
            public static MelonPreferences_Entry<String> PurifyText;
            //public static MelonPreferences_Entry<Color> GodspeedColor;
            public static MelonPreferences_Entry<String> GodspeedText;
            //public static MelonPreferences_Entry<Color> StompColor;
            public static MelonPreferences_Entry<String> StompText;
            //public static MelonPreferences_Entry<Color> FireballColor;
            public static MelonPreferences_Entry<String> FireballText;
            //public static MelonPreferences_Entry<Color> DominionColor;
            public static MelonPreferences_Entry<String> DominionText;
            //public static MelonPreferences_Entry<Color> BoofColor;
            public static MelonPreferences_Entry<String> BoofText;
            //public static MelonPreferences_Entry<Color> AmmoColor;
            public static MelonPreferences_Entry<String> AmmoText;
            //public static MelonPreferences_Entry<Color> HealthColor;
            public static MelonPreferences_Entry<String> HealthText;

            public bool patched = false;
            private HarmonyLib.Harmony harmony_instance = new HarmonyLib.Harmony("custom_cards");

            public override void OnApplicationStart() {
                CardSettings = MelonPreferences.CreateCategory("Card Customizations");
                EnableCardCustomizations = CardSettings.CreateEntry<bool>("Enable card customizations [changes require level restart]", false);
                //KatanaColor = CardSettings.CreateEntry<Color>("Katana Color", new Color(0.631373f, 0.631373f, 0.631373f, 1));
                //FistsColor = CardSettings.CreateEntry<Color>("Fists Color", new Color(0.631373f, 0.631373f, 0.631373f, 1));
                //ElevateColor = CardSettings.CreateEntry<Color>("Elevate Color", new Color(0.985294f, 0.853788f, 0.304282f, 1));
                ElevateText = CardSettings.CreateEntry<String>("Elevate Text", "Elevate");
                //PurifyColor = CardSettings.CreateEntry<Color>("Purify Color", new Color(0.666667f, 0.47451f, 0.901961f, 1));
                PurifyText = CardSettings.CreateEntry<String>("Purify Text", "Purify");
                //GodspeedColor = CardSettings.CreateEntry<Color>("Godspeed Color", new Color(0.47451f, 0.592157f, 0.964706f, 1));
                GodspeedText = CardSettings.CreateEntry<String>("Godspeed Text", "Godspeed");
                //StompColor = CardSettings.CreateEntry<Color>("Stomp Color", new Color(0.160784f, 0.666667f, 0.137255f, 1));
                StompText = CardSettings.CreateEntry<String>("Stomp Text", "Stomp");
                //FireballColor = CardSettings.CreateEntry<Color>("Fireball Color", new Color(0.85098f, 0.317647f, 0.392157f, 1));
                FireballText = CardSettings.CreateEntry<String>("Fireball Text", "Fireball");
                //DominionColor = CardSettings.CreateEntry<Color>("Dominion Color", new Color(0.164706f, 0.803922f, 0.827451f, 1));
                DominionText = CardSettings.CreateEntry<String>("Dominion Text", "Dominion");
                //BoofColor = CardSettings.CreateEntry<Color>("Boof Color", new Color(1, 1, 1, 1));
                BoofText = CardSettings.CreateEntry<String>("Boof Text", "Book of Life");
                //AmmoColor = CardSettings.CreateEntry<Color>("Ammo Color", new Color(1, 1, 1, 1));
                AmmoText = CardSettings.CreateEntry<String>("Ammo Text", "Ammo");
                //HealthColor = CardSettings.CreateEntry<Color>("Health Color", new Color(0.933962f, 0.436143f, 0.714336f, 1));
                HealthText = CardSettings.CreateEntry<String>("Health Text", "Health");

                if (EnableCardCustomizations.Value) {
                    ApplyPatches();
                }
            }

            public void ApplyPatches() {
                if (!patched) {
                    this.harmony_instance.PatchAll(typeof(CardCustomizations_Patch));
                    patched = true;
                }
            }

            public void RemovePatches() {
                if (patched) {
                    this.harmony_instance.UnpatchSelf();
                    patched = false;
                }
            }

            public override void OnPreferencesSaved() {
                if (EnableCardCustomizations.Value) {
                    ApplyPatches();
                } else {
                    RemovePatches();
                }
            }

            public class CardCustomizations_Patch {
                [HarmonyPatch(typeof(LocalizationManager), "GetTranslation")]
                [HarmonyPostfix]
                public static void OverrideCardText(string Term, ref string __result) {
                   switch (Term) {
                        case "Interface/DISCARD_ELEVATE": {
                                __result = ElevateText.Value;
                                break;
                            }
                        case "Interface/DISCARD_PURIFY": {
                                __result = PurifyText.Value;
                                break;
                            }
                        case "Interface/DISCARD_GODSPEED": {
                                __result = GodspeedText.Value;
                                break;
                            }
                        case "Interface/DISCARD_STOMP": {
                                __result = StompText.Value;
                                break;
                            }
                        case "Interface/DISCARD_FIREBALL": {
                                __result = FireballText.Value;
                                break;
                            }
                        case "Interface/DISCARD_DOMINION": {
                                __result = DominionText.Value;
                                break;
                            }
                        case "Interface/DISCARD_BOOKOFLIFE": {
                                __result = BoofText.Value;
                                break;
                            }
                        case "Interface/DISCARD_HEALTH": {
                                __result = HealthText.Value;
                                break;
                            }
                        case "Interface/DISCARD_AMMO": {
                                __result = AmmoText.Value;
                                break;
                            }
                    }
                }

                /*
                public static Color? GetNewColor(string cardID) {
                    switch (cardID) {
                        case "FISTS": {
                                return FistsColor.Value;
                            }
                        case "KATANA": {
                                return KatanaColor.Value;
                            }
                        case "KATANA_MIRACLE": {
                                return KatanaColor.Value;
                            }
                        case "PISTOL": {
                                return ElevateColor.Value;
                            }
                        case "MACHINEGUN": {
                                return PurifyColor.Value;
                            }
                        case "RIFLE": {
                                return GodspeedColor.Value;
                            }
                        case "UZI": {
                                return StompColor.Value;
                            }
                        case "SHOTGUN": {
                                return FireballColor.Value;
                            }
                        case "ROCKETLAUNCHER": {
                                return DominionColor.Value;
                            }
                        case "RAPTURE": {
                                return BoofColor.Value;
                            }
                        case "AMMO": {
                                return AmmoColor.Value;
                            }
                        case "HEALTH": {
                                return HealthColor.Value;
                            }
                    };
                    return null;
                }

                [HarmonyPatch(typeof(CardPickupSpawner), "SpawnCard", new Type[] { })]
                [HarmonyPrefix]
                public static void OverrideCardColor(ref CardPickupSpawner __instance) {
                    Color? c = GetNewColor(__instance.card.cardID);
                    if (c.HasValue) {
                        __instance.card.cardColor = c.Value;
                    }
                }
                */
            }
        }
        public static MelonPreferences_Category poweruserprefs;
        public static MelonPreferences_Entry<int> level_rush_seed;


        // TODO: break these into their own module eventually
        public static MelonPreferences_Category misc_tweaks_config;
        public static MelonPreferences_Entry<bool> disable_start_mission;

        // "Start Mission" button is first button in Location_Portal for MenuScreenLocation
        // need to hook creation? or figure out its click action func....

        public class IDidNotWantToClickThat {
            [HarmonyPatch(typeof(MenuScreenLocation), "CreateActionButton")]
            [HarmonyPrefix]
            public static bool Smile(HubAction hubAction) {
                if (hubAction.ID == "PORTAL_CONTINUE_MISSION") {
                    MelonLogger.Msg("interfering with load of IDPORTAL_CONTINUE_MISSION");
                    return !disable_start_mission.Value;
                }
                return true;
            }
        }

        public override void OnApplicationStart() {
            poweruserprefs = MelonPreferences.CreateCategory("PowerPrefs adjustments");
            level_rush_seed = poweruserprefs.CreateEntry("Level Rush Seed (negative is random)", -1);

            misc_tweaks_config = MelonPreferences.CreateCategory("Misc tweaks");
            disable_start_mission = misc_tweaks_config.CreateEntry("disable Start Mission button in job archive", false);

            HarmonyInstance.PatchAll(typeof(IDidNotWantToClickThat));

            this.speedometer.OnApplicationStart();
            this.chapter_timer.OnApplicationStart();
            this.vfx_toggles.OnApplicationStart();
            this.custom_cards.OnApplicationStart();
        }

        public override void OnPreferencesSaved() {
            GameDataManager.powerPrefs.seedForLevelRushLevelOrder_NegativeValuesMeansRandomizeSeed = level_rush_seed.Value;
            this.chapter_timer.OnPreferencesSaved();
            this.vfx_toggles.OnPreferencesSaved();
            this.custom_cards.OnPreferencesSaved();
        }

        public override void OnLateUpdate() {
            this.speedometer.OnLateUpdate();
        }

        public override void OnUpdate() {
            this.vfx_toggles.OnUpdate();
        }

        public override void OnGUI() {
            this.speedometer.OnGUI();
            this.chapter_timer.OnGUI();
        }
    }
}
