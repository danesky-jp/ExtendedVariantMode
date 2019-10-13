﻿using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using Monocle;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using Celeste.Mod.UI;
using Celeste.Mod;
using ExtendedVariants.UI;
using Celeste;
using TextMenuExt = ExtendedVariants.UI.TextMenuExt;
using ExtendedVariants.Entities;

namespace ExtendedVariants.Module {
    public class ExtendedVariantsModule : EverestModule {

        public static ExtendedVariantsModule Instance;

        public override Type SettingsType => typeof(ExtendedVariantsSettings);
        public override Type SessionType => typeof(ExtendedVariantsSession);

        public static ExtendedVariantsSettings Settings => (ExtendedVariantsSettings)Instance._Settings;
        public static ExtendedVariantsSession Session => (ExtendedVariantsSession)Instance._Session;

        public static Dictionary<Variant, int> OverridenVariantsInRoom = new Dictionary<Variant, int>();
        public static Dictionary<Variant, int> OldVariantsInRoom = new Dictionary<Variant, int>();
        public static Dictionary<Variant, int> VariantValuesBeforeOverride = new Dictionary<Variant, int>();

        public static TextMenu.Option<bool> MasterSwitchOption;
        public static TextMenu.Option<int> GravityOption;
        public static TextMenu.Option<int> FallSpeedOption;
        public static TextMenu.Option<int> JumpHeightOption;
        public static TextMenu.Option<int> SpeedXOption;
        public static TextMenu.Option<int> StaminaOption;
        public static TextMenu.Option<int> DashSpeedOption;
        public static TextMenu.Option<int> DashCountOption;
        public static TextMenu.Option<int> FrictionOption;
        public static TextMenu.Option<bool> DisableWallJumpingOption;
        public static TextMenu.Option<int> JumpCountOption;
        public static TextMenu.Option<bool> RefillJumpsOnDashRefillOption;
        public static TextMenu.Option<bool> UpsideDownOption;
        public static TextMenu.Option<int> HyperdashSpeedOption;
        public static TextMenu.Option<int> WallBouncingSpeedOption;
        public static TextMenu.Option<int> DashLengthOption;
        public static TextMenu.Option<bool> ForceDuckOnGroundOption;
        public static TextMenu.Option<bool> InvertDashesOption;
        public static TextMenu.Option<bool> DisableNeutralJumpingOption;
        public static TextMenu.Option<bool> ChangeVariantsRandomlyOption;
        public static TextMenu.Option<bool> BadelineChasersEverywhereOption;
        public static TextMenu.Option<int> ChaserCountOption;
        public static TextMenu.Option<bool> AffectExistingChasersOption;
        public static TextMenu.Option<int> RegularHiccupsOption;
        public static TextMenu.Option<int> RoomLightingOption;
        public static TextMenu.Option<bool> OshiroEverywhereOption;
        public static TextMenu.Option<int> WindEverywhereOption;
        public static TextMenu.Option<bool> SnowballsEverywhereOption;
        public static TextMenu.Option<int> SnowballDelayOption;
        public static TextMenu.Option<int> AddSeekersOption;
        public static TextMenu.Option<int> BadelineLagOption;
        public static TextMenu.Item ResetToDefaultOption;
        public static TextMenu.Item RandomizerOptions;

        public static VariantRandomizer Randomizer;

        public ExtendedVariantsModule() {
            Instance = this;
            Randomizer = new VariantRandomizer();
        }

        // ================ Options menu handling ================
        
        /// <summary>
        /// List of options shown for multipliers.
        /// </summary>
        public static int[] MultiplierScale = new int[] {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
            25, 30, 35, 40, 45, 50, 60, 70, 80, 90, 100, 250, 500, 1000
        };

        /// <summary>
        /// Formats a multiplier (with no decimal point if not required).
        /// </summary>
        private static Func<int, string> multiplierFormatter = option => {
            option = MultiplierScale[option];
            if (option % 10 == 0) {
                return $"{option / 10f:n0}x";
            }
            return $"{option / 10f:n1}x";
        };

        /// <summary>
        /// Finds out the index of a multiplier in the multiplierScale table.
        /// If it is not present, will return the previous option.
        /// (For example, 18x will return the index for 10x.)
        /// </summary>
        /// <param name="option">The multiplier</param>
        /// <returns>The index of the multiplier in the multiplierScale table</returns>
        private static int indexFromMultiplier(int option) {
            for (int index = 0; index < MultiplierScale.Length - 1; index++) {
                if (MultiplierScale[index + 1] > option) {
                    return index;
                }
            }

            return MultiplierScale.Length - 1;
        }

        public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
            base.CreateModMenuSection(menu, inGame, snapshot);

            // create every option
            GravityOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_GRAVITY"),
                multiplierFormatter, 0, MultiplierScale.Length - 1, indexFromMultiplier(Settings.Gravity), indexFromMultiplier(10)).Change(i => Settings.Gravity = MultiplierScale[i]);
            FallSpeedOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_FALLSPEED"),
                multiplierFormatter, 0, MultiplierScale.Length - 1, indexFromMultiplier(Settings.FallSpeed), indexFromMultiplier(10)).Change(i => Settings.FallSpeed = MultiplierScale[i]);
            JumpHeightOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_JUMPHEIGHT"),
                multiplierFormatter, 0, MultiplierScale.Length - 1, indexFromMultiplier(Settings.JumpHeight), indexFromMultiplier(10)).Change(i => Settings.JumpHeight = MultiplierScale[i]);
            SpeedXOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_SPEEDX"),
                multiplierFormatter, 0, MultiplierScale.Length - 1, indexFromMultiplier(Settings.SpeedX), indexFromMultiplier(10)).Change(i => Settings.SpeedX = MultiplierScale[i]);
            StaminaOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_STAMINA"),
                i => $"{i * 10}", 0, 50, Settings.Stamina, 11).Change(i => Settings.Stamina = i);
            DashSpeedOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_DASHSPEED"),
                multiplierFormatter, 0, MultiplierScale.Length - 1, indexFromMultiplier(Settings.DashSpeed), indexFromMultiplier(10)).Change(i => Settings.DashSpeed = MultiplierScale[i]);
            DashCountOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_DASHCOUNT"), i => {
                if (i == -1) {
                    return Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_DEFAULT");
                }
                return i.ToString();
            }, -1, 5, Settings.DashCount, 0).Change(i => Settings.DashCount = i);
            FrictionOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_FRICTION"),
                i => {
                    switch (i) {
                        case -1: return "0x";
                        case 0: return "0.05x";
                        default: return multiplierFormatter(i);
                    }
                }, -1, MultiplierScale.Length - 1, Settings.Friction == -1 ? -1 : indexFromMultiplier(Settings.Friction), indexFromMultiplier(10) + 1)
                .Change(i => Settings.Friction = (i == -1 ? -1 : MultiplierScale[i]));
            DisableWallJumpingOption = new TextMenuExt.OnOff(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_DISABLEWALLJUMPING"), Settings.DisableWallJumping, false)
                .Change(b => Settings.DisableWallJumping = b);
            JumpCountOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_JUMPCOUNT"),
                i => {
                    if (i == 6) {
                        return Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_INFINITE");
                    }
                    return i.ToString();
                }, 0, 6, Settings.JumpCount, 1).Change(i => {
                    Settings.JumpCount = i;
                    refreshOptionMenuEnabledStatus();
                });
            RefillJumpsOnDashRefillOption = new TextMenuExt.OnOff(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_REFILLJUMPSONDASHREFILL"), Settings.RefillJumpsOnDashRefill, false)
                .Change(b => Settings.RefillJumpsOnDashRefill = b);
            UpsideDownOption = new TextMenuExt.OnOff(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_UPSIDEDOWN"), Settings.UpsideDown, false)
                .Change(b => Settings.UpsideDown = b);
            HyperdashSpeedOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_HYPERDASHSPEED"),
                multiplierFormatter, 0, MultiplierScale.Length - 1, indexFromMultiplier(Settings.HyperdashSpeed), indexFromMultiplier(10)).Change(i => Settings.HyperdashSpeed = MultiplierScale[i]);
            WallBouncingSpeedOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_WALLBOUNCINGSPEED"),
                multiplierFormatter, 0, MultiplierScale.Length - 1, indexFromMultiplier(Settings.WallBouncingSpeed), indexFromMultiplier(10)).Change(i => Settings.WallBouncingSpeed = MultiplierScale[i]);
            DashLengthOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_DASHLENGTH"),
                multiplierFormatter, 0, MultiplierScale.Length - 1, indexFromMultiplier(Settings.DashLength), indexFromMultiplier(10)).Change(i => Settings.DashLength = MultiplierScale[i]);
            ForceDuckOnGroundOption = new TextMenuExt.OnOff(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_FORCEDUCKONGROUND"), Settings.ForceDuckOnGround, false)
                .Change(b => Settings.ForceDuckOnGround = b);
            InvertDashesOption = new TextMenuExt.OnOff(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_INVERTDASHES"), Settings.InvertDashes, false)
                .Change(b => Settings.InvertDashes = b);
            DisableNeutralJumpingOption = new TextMenuExt.OnOff(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_DISABLENEUTRALJUMPING"), Settings.DisableNeutralJumping, false)
                .Change(b => Settings.DisableNeutralJumping = b);
            BadelineChasersEverywhereOption = new TextMenuExt.OnOff(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_BADELINECHASERSEVERYWHERE"), Settings.BadelineChasersEverywhere, false)
                .Change(b => Settings.BadelineChasersEverywhere = b);
            AffectExistingChasersOption = new TextMenuExt.OnOff(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_AFFECTEXISTINGCHASERS"), Settings.AffectExistingChasers, false)
                .Change(b => Settings.AffectExistingChasers = b);
            ChaserCountOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_CHASERCOUNT"),
                i => i.ToString(), 1, 10, Settings.ChaserCount, 0).Change(i => Settings.ChaserCount = i);
            ChangeVariantsRandomlyOption = new TextMenuExt.OnOff(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_CHANGEVARIANTSRANDOMLY"), Settings.ChangeVariantsRandomly, false)
                .Change(b => {
                    Settings.ChangeVariantsRandomly = b;
                    refreshOptionMenuEnabledStatus();
                });
            RegularHiccupsOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_REGULARHICCUPS"),
                i => i == 0 ? Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_DISABLED") : multiplierFormatter(i).Replace("x", "s"), 
                0, MultiplierScale.Length - 1, indexFromMultiplier(Settings.RegularHiccups), 0).Change(i => {
                    Settings.RegularHiccups = MultiplierScale[i];
                    regularHiccupTimer = Settings.RegularHiccups / 10f;
                });
            RoomLightingOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_ROOMLIGHTING"),
                i => {
                    if (i == -1) return Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_DEFAULT");
                    return $"{i * 10}%";
                }, -1, 10, Settings.RoomLighting, 0).Change(i => {
                    Settings.RoomLighting = i;
                    if(Engine.Scene.GetType() == typeof(Level)) {
                        // currently in level, change lighting right away
                        Level lvl = (Engine.Scene as Level);
                        lvl.Lighting.Alpha = (i == -1 ? lvl.BaseLightingAlpha + lvl.Session.LightingAlphaAdd : 1 - (i / 10f));
                    }
                });
            OshiroEverywhereOption = new TextMenuExt.OnOff(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_OSHIROEVERYWHERE"), Settings.OshiroEverywhere, false)
                .Change(b => Settings.OshiroEverywhere = b);
            WindEverywhereOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_WINDEVERYWHERE"),
                i => {
                    if (i == 0) return Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_DISABLED");
                    if (i == availableWindPatterns.Length + 1) return Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_WINDEVERYWHERE_RANDOM");
                    return Dialog.Clean($"MODOPTIONS_EXTENDEDVARIANTS_WINDEVERYWHERE_{availableWindPatterns[i - 1].ToString()}");
                },
                0, availableWindPatterns.Length + 1, Settings.WindEverywhere, 0)
                .Change(i => Settings.WindEverywhere = i);
            SnowballsEverywhereOption = new TextMenuExt.OnOff(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_SNOWBALLSEVERYWHERE"), Settings.SnowballsEverywhere, false)
                .Change(b => Settings.SnowballsEverywhere = b);
            SnowballDelayOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_SNOWBALLDELAY"),
                i => multiplierFormatter(i).Replace("x", "s"), 0, MultiplierScale.Length - 1, indexFromMultiplier(Settings.SnowballDelay), 8)
                .Change(i => Settings.SnowballDelay = MultiplierScale[i]);
            AddSeekersOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_ADDSEEKERS"),
                i => i.ToString(), 0, 5, indexFromMultiplier(Settings.AddSeekers), 0).Change(i => Settings.AddSeekers = i);
            BadelineLagOption = new TextMenuExt.Slider(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_BADELINELAG"),
                i => i == 0 ? Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_DEFAULT") : multiplierFormatter(i).Replace("x", "s"),
                0, MultiplierScale.Length - 1, indexFromMultiplier(Settings.BadelineLag), 0).Change(i => Settings.BadelineLag = MultiplierScale[i]);

            // create the "master switch" option with specific enable/disable handling.
            MasterSwitchOption = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_MASTERSWITCH"), Settings.MasterSwitch)
                .Change(v => {
                    Settings.MasterSwitch = v;
                    if (!v) {
                        // We are disabling extended variants: reset values to their defaults.
                        resetToDefaultSettings();
                        refreshOptionMenuValues();
                    }

                    refreshOptionMenuEnabledStatus();
                });

            // Add a button to easily revert to default values
            ResetToDefaultOption = new TextMenu.Button(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_RESETTODEFAULT")).Pressed(() => {
                resetToDefaultSettings();
                refreshOptionMenuValues();
                refreshOptionMenuEnabledStatus();
            });

            if(inGame) {
                Level level = Engine.Scene as Level;

                // this is how it works in-game
                RandomizerOptions = new TextMenu.Button(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_RANDOMIZER")).Pressed(() => {
                    // close the Mod Options menu
                    menu.RemoveSelf();

                    // create our menu and prepare it
                    TextMenu randomizerMenu = OuiRandomizerOptions.BuildMenu();

                    randomizerMenu.OnESC = randomizerMenu.OnCancel = () => {
                        // close the randomizer options
                        Audio.Play(SFX.ui_main_button_back);
                        randomizerMenu.Close();

                        // and open the Mod Options menu back (this should work, right? we only removed it from the scene earlier, but it still exists and is intact)
                        // "what could possibly go wrong?" ~ famous last words
                        level.Add(menu);
                    };

                    randomizerMenu.OnPause = () => {
                        // we're unpausing, so close that menu, and save the mod settings because the Mod Options menu won't do that for us
                        Audio.Play(SFX.ui_main_button_back);
                        randomizerMenu.CloseAndRun(Everest.SaveSettings(), () => {
                            level.Paused = false;
                            Engine.FreezeTimer = 0.15f;
                        });
                    };

                    // finally, add the menu to the scene
                    level.Add(randomizerMenu);
                });
            } else {
                // this is how it works in the main menu: way more simply than the in-game mess.
                RandomizerOptions = new TextMenu.Button(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_RANDOMIZER")).Pressed(() => {
                    OuiModOptions.Instance.Overworld.Goto<OuiRandomizerOptions>();
                });
            }

            refreshOptionMenuEnabledStatus();

            menu.Add(MasterSwitchOption);
            menu.Add(ResetToDefaultOption);

            addHeading(menu, "VERTICALSPEED");
            menu.Add(GravityOption);
            menu.Add(FallSpeedOption);

            addHeading(menu, "JUMPING");
            menu.Add(JumpHeightOption);
            menu.Add(WallBouncingSpeedOption);
            menu.Add(DisableWallJumpingOption);
            menu.Add(JumpCountOption);
            menu.Add(RefillJumpsOnDashRefillOption);

            addHeading(menu, "DASHING");
            menu.Add(DashSpeedOption);
            menu.Add(DashLengthOption);
            menu.Add(HyperdashSpeedOption);
            menu.Add(DashCountOption);

            addHeading(menu, "MOVING");
            menu.Add(SpeedXOption);
            menu.Add(FrictionOption);

            addHeading(menu, "CHASERS");
            menu.Add(BadelineChasersEverywhereOption);
            menu.Add(ChaserCountOption);
            menu.Add(AffectExistingChasersOption);
            menu.Add(BadelineLagOption);

            addHeading(menu, "EVERYWHERE");
            menu.Add(OshiroEverywhereOption);
            menu.Add(WindEverywhereOption);
            menu.Add(SnowballsEverywhereOption);
            menu.Add(SnowballDelayOption);
            menu.Add(AddSeekersOption);

            addHeading(menu, "OTHER");
            menu.Add(StaminaOption);
            menu.Add(UpsideDownOption);
            menu.Add(DisableNeutralJumpingOption);
            menu.Add(RegularHiccupsOption);
            menu.Add(RoomLightingOption);

            addHeading(menu, "TROLL");
            menu.Add(ForceDuckOnGroundOption);
            menu.Add(InvertDashesOption);

            addHeading(menu, "RANDOMIZER");
            menu.Add(ChangeVariantsRandomlyOption);
            menu.Add(RandomizerOptions);
        }

        private static void addHeading(TextMenu menu, String headingNameResource) {
            menu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_HEADING") + " - " + Dialog.Clean("MODOPTIONS_EXTENDEDVARIANTS_HEADING_" + headingNameResource)));
        }

        private static void resetToDefaultSettings() {
            if(Settings.RoomLighting != -1 && Engine.Scene.GetType() == typeof(Level)) {
                // currently in level, change lighting right away
                Level lvl = (Engine.Scene as Level);
                lvl.Lighting.Alpha = lvl.BaseLightingAlpha + lvl.Session.LightingAlphaAdd;
            }

            Settings.Gravity = 10;
            Settings.FallSpeed = 10;
            Settings.JumpHeight = 10;
            Settings.SpeedX = 10;
            Settings.Stamina = 11;
            Settings.DashSpeed = 10;
            Settings.DashCount = -1;
            Settings.Friction = 10;
            Settings.DisableWallJumping = false;
            Settings.JumpCount = 1;
            Settings.RefillJumpsOnDashRefill = false;
            Settings.UpsideDown = false;
            Settings.HyperdashSpeed = 10;
            Settings.WallBouncingSpeed = 10;
            Settings.DashLength = 10;
            Settings.ForceDuckOnGround = false;
            Settings.InvertDashes = false;
            Settings.DisableNeutralJumping = false;
            Settings.BadelineChasersEverywhere = false;
            Settings.ChaserCount = 1;
            Settings.AffectExistingChasers = false;
            Settings.ChangeVariantsRandomly = false;
            Settings.RegularHiccups = 0;
            Settings.RoomLighting = -1;
            Settings.OshiroEverywhere = false;
            Settings.WindEverywhere = 0;
            Settings.SnowballsEverywhere = false;
            Settings.SnowballDelay = 8;
            Settings.AddSeekers = 0;
            Settings.BadelineLag = 0;
        }

        private static void refreshOptionMenuValues() {
            setValue(GravityOption, 0, indexFromMultiplier(Settings.Gravity));
            setValue(FallSpeedOption, 0, indexFromMultiplier(Settings.FallSpeed));
            setValue(JumpHeightOption, 0, indexFromMultiplier(Settings.JumpHeight));
            setValue(SpeedXOption, 0, indexFromMultiplier(Settings.SpeedX));
            setValue(StaminaOption, 0, Settings.Stamina);
            setValue(DashSpeedOption, 0, indexFromMultiplier(Settings.DashSpeed));
            setValue(DashCountOption, -1, Settings.DashCount);
            setValue(FrictionOption, -1, Settings.Friction == -1 ? -1 : indexFromMultiplier(Settings.Friction));
            setValue(DisableWallJumpingOption, Settings.DisableWallJumping);
            setValue(JumpCountOption, 0, Settings.JumpCount);
            setValue(RefillJumpsOnDashRefillOption, Settings.RefillJumpsOnDashRefill);
            setValue(UpsideDownOption, Settings.UpsideDown);
            setValue(HyperdashSpeedOption, 0, indexFromMultiplier(Settings.HyperdashSpeed));
            setValue(WallBouncingSpeedOption, 0, indexFromMultiplier(Settings.WallBouncingSpeed));
            setValue(DashLengthOption, 0, indexFromMultiplier(Settings.DashLength));
            setValue(ForceDuckOnGroundOption, Settings.ForceDuckOnGround);
            setValue(InvertDashesOption, Settings.InvertDashes);
            setValue(DisableNeutralJumpingOption, Settings.DisableNeutralJumping);
            setValue(BadelineChasersEverywhereOption, Settings.BadelineChasersEverywhere);
            setValue(ChaserCountOption, 1, Settings.ChaserCount);
            setValue(AffectExistingChasersOption, Settings.AffectExistingChasers);
            setValue(ChangeVariantsRandomlyOption, Settings.ChangeVariantsRandomly);
            setValue(RegularHiccupsOption, 0, indexFromMultiplier(Settings.RegularHiccups));
            setValue(RoomLightingOption, -1, Settings.RoomLighting);
            setValue(OshiroEverywhereOption, Settings.OshiroEverywhere);
            setValue(WindEverywhereOption, 0, Settings.WindEverywhere);
            setValue(SnowballsEverywhereOption, Settings.SnowballsEverywhere);
            setValue(SnowballDelayOption, 0, Settings.SnowballDelay);
            setValue(AddSeekersOption, 0, Settings.AddSeekers);
            setValue(BadelineLagOption, 0, Settings.BadelineLag);
        }

        private static void refreshOptionMenuEnabledStatus() {
            GravityOption.Disabled = !Settings.MasterSwitch;
            FallSpeedOption.Disabled = !Settings.MasterSwitch;
            JumpHeightOption.Disabled = !Settings.MasterSwitch;
            SpeedXOption.Disabled = !Settings.MasterSwitch;
            StaminaOption.Disabled = !Settings.MasterSwitch;
            DashCountOption.Disabled = !Settings.MasterSwitch;
            DashSpeedOption.Disabled = !Settings.MasterSwitch;
            FrictionOption.Disabled = !Settings.MasterSwitch;
            DisableWallJumpingOption.Disabled = !Settings.MasterSwitch;
            JumpCountOption.Disabled = !Settings.MasterSwitch;
            RefillJumpsOnDashRefillOption.Disabled = !Settings.MasterSwitch || Settings.JumpCount < 2;
            ResetToDefaultOption.Disabled = !Settings.MasterSwitch;
            UpsideDownOption.Disabled = !Settings.MasterSwitch;
            HyperdashSpeedOption.Disabled = !Settings.MasterSwitch;
            WallBouncingSpeedOption.Disabled = !Settings.MasterSwitch;
            DashLengthOption.Disabled = !Settings.MasterSwitch;
            ForceDuckOnGroundOption.Disabled = !Settings.MasterSwitch;
            InvertDashesOption.Disabled = !Settings.MasterSwitch;
            DisableNeutralJumpingOption.Disabled = !Settings.MasterSwitch;
            BadelineChasersEverywhereOption.Disabled = !Settings.MasterSwitch;
            ChaserCountOption.Disabled = !Settings.MasterSwitch;
            AffectExistingChasersOption.Disabled = !Settings.MasterSwitch;
            ChangeVariantsRandomlyOption.Disabled = !Settings.MasterSwitch;
            RegularHiccupsOption.Disabled = !Settings.MasterSwitch;
            RoomLightingOption.Disabled = !Settings.MasterSwitch;
            OshiroEverywhereOption.Disabled = !Settings.MasterSwitch;
            WindEverywhereOption.Disabled = !Settings.MasterSwitch;
            SnowballsEverywhereOption.Disabled = !Settings.MasterSwitch;
            SnowballDelayOption.Disabled = !Settings.MasterSwitch;
            AddSeekersOption.Disabled = !Settings.MasterSwitch;
            BadelineLagOption.Disabled = !Settings.MasterSwitch;
            RandomizerOptions.Disabled = !Settings.MasterSwitch || !Settings.ChangeVariantsRandomly;
        }

        private static void setValue(TextMenu.Option<int> option, int min, int newValue) {
            newValue -= min;

            if(newValue != option.Index) {
                // replicate the vanilla behaviour
                option.PreviousIndex = option.Index;
                option.Index = newValue;
                option.ValueWiggler.Start();
            }
        }

        private static void setValue(TextMenu.Option<bool> option, bool newValue) {
            if (newValue != (option.Index == 1)) {
                // replicate the vanilla behaviour
                option.PreviousIndex = option.Index;
                option.Index = newValue ? 1 : 0;
                option.ValueWiggler.Start();
            }
        }

        // ================ Module loading ================

        public override void Load() {
            // mod methods here
            IL.Celeste.Player.NormalBegin += ModNormalBegin;
            IL.Celeste.Player.NormalUpdate += ModNormalUpdate;
            IL.Celeste.Player.ClimbUpdate += ModClimbUpdate;
            On.Celeste.Player.RefillStamina += ModRefillStamina;
            IL.Celeste.Player.SwimBegin += ModSwimBegin;
            IL.Celeste.Player.DreamDashBegin += ModDreamDashBegin;
            On.Celeste.Player.Update += ModUpdate;
            IL.Celeste.Player.ctor += ModPlayerConstructor;
            On.Celeste.SummitGem.SmashRoutine += ModSummitGemSmash;
            IL.Celeste.Player.UpdateSprite += ModUpdateSprite;
            On.Celeste.Player.RefillDash += ModRefillDash;
            IL.Celeste.Player.UseRefill += ModUseRefill;
            On.Celeste.Player.UseRefill += ModOnUseRefill;
            On.Celeste.Player.Added += ModAdded;
            IL.Celeste.Player.CallDashEvents += ModCallDashEvents;
            IL.Celeste.Player.UpdateHair += ModUpdateHair;
            IL.Celeste.Player.Jump += ModJump;
            IL.Celeste.Player.SuperJump += ModSuperJump;
            IL.Celeste.Player.SuperWallJump += ModSuperWallJump;
            IL.Celeste.Player.WallJump += ModWallJump;
            On.Celeste.Player.WallJump += ModOnWallJump;
            On.Celeste.Player.WallJumpCheck += ModWallJumpCheck;
            On.Celeste.AreaComplete.VersionNumberAndVariants += ModVersionNumberAndVariants;
            Everest.Events.Level.OnLoadEntity += new Everest.Events.Level.LoadEntityHandler(OnLoadEntity);
            Everest.Events.Player.OnSpawn += OnPlayerSpawn;
            Everest.Events.Level.OnTransitionTo += OnLevelTransitionTo;
            Everest.Events.Level.OnEnter += OnLevelEnter;
            Everest.Events.Level.OnExit += OnLevelExit;
            IL.Celeste.ChangeRespawnTrigger.OnEnter += ModRespawnTrigger;
            IL.Celeste.Level.Render += ModLevelRender;
            IL.Celeste.Player.DashBegin += ModDashBegin;
            On.Celeste.Player.DashCoroutine += ModDashCoroutine;
            IL.Celeste.Player.DashUpdate += ModDashUpdate;
            On.Celeste.Level.EndPauseEffects += ModEndPauseEffects;

            IL.Celeste.BadelineOldsite.ctor_Vector2_int += ModBadelineOldsiteConstructor;
            IL.Celeste.BadelineOldsite.Added += ModBadelineOldsiteAdded;
            IL.Celeste.BadelineOldsite.CanChangeMusic += ModBadelineOldsiteCanChangeMusic;
            On.Celeste.BadelineOldsite.IsChaseEnd += ModBadelineOldsiteIsChaseEnd;
            On.Celeste.Level.LoadLevel += ModLoadLevel;
            On.Celeste.Level.TransitionRoutine += ModTransitionRoutine;
            IL.Celeste.Player.UpdateChaserStates += ModUpdateChaserStates;

            On.Celeste.LightFadeTrigger.OnStay += ModOnLightFadeTriggerStay;

            IL.Celeste.Snowball.Update += ModSnowballUpdateIL;
            IL.Celeste.Pathfinder.ctor += ModPathfinderConstructor;

            On.Celeste.BadelineBoost.BoostRoutine += WrapBadelineBoostRoutine;

            // if master switch is disabled, ensure all values are the default ones. (variants are disabled even if the yml file has been edited.)
            if (!Settings.MasterSwitch) {
                resetToDefaultSettings();
            }
        }

        public override void Unload() {
            // unmod methods here
            IL.Celeste.Player.NormalBegin -= ModNormalBegin;
            IL.Celeste.Player.NormalUpdate -= ModNormalUpdate;
            IL.Celeste.Player.ClimbUpdate -= ModClimbUpdate;
            On.Celeste.Player.RefillStamina -= ModRefillStamina;
            IL.Celeste.Player.SwimBegin -= ModSwimBegin;
            IL.Celeste.Player.DreamDashBegin -= ModDreamDashBegin;
            On.Celeste.Player.Update -= ModUpdate;
            IL.Celeste.Player.ctor -= ModPlayerConstructor;
            On.Celeste.SummitGem.SmashRoutine -= ModSummitGemSmash;
            IL.Celeste.Player.UpdateSprite -= ModUpdateSprite;
            On.Celeste.Player.RefillDash -= ModRefillDash;
            IL.Celeste.Player.UseRefill -= ModUseRefill;
            On.Celeste.Player.UseRefill -= ModOnUseRefill;
            On.Celeste.Player.Added -= ModAdded;
            IL.Celeste.Player.CallDashEvents -= ModCallDashEvents;
            IL.Celeste.Player.UpdateHair -= ModUpdateHair;
            IL.Celeste.Player.Jump -= ModJump;
            IL.Celeste.Player.SuperJump -= ModSuperJump;
            IL.Celeste.Player.SuperWallJump -= ModSuperWallJump;
            IL.Celeste.Player.WallJump -= ModWallJump;
            On.Celeste.Player.WallJump -= ModOnWallJump;
            On.Celeste.Player.WallJumpCheck -= ModWallJumpCheck;
            On.Celeste.AreaComplete.VersionNumberAndVariants -= ModVersionNumberAndVariants;
            Everest.Events.Level.OnLoadEntity -= new Everest.Events.Level.LoadEntityHandler(OnLoadEntity);
            Everest.Events.Player.OnSpawn -= OnPlayerSpawn;
            Everest.Events.Level.OnTransitionTo -= OnLevelTransitionTo;
            Everest.Events.Level.OnEnter -= OnLevelEnter;
            Everest.Events.Level.OnExit -= OnLevelExit;
            IL.Celeste.ChangeRespawnTrigger.OnEnter -= ModRespawnTrigger;
            IL.Celeste.Level.Render -= ModLevelRender;
            IL.Celeste.Player.DashBegin -= ModDashBegin;
            On.Celeste.Player.DashCoroutine -= ModDashCoroutine;
            IL.Celeste.Player.DashUpdate -= ModDashUpdate;
            On.Celeste.Level.EndPauseEffects -= ModEndPauseEffects;

            IL.Celeste.BadelineOldsite.ctor_Vector2_int -= ModBadelineOldsiteConstructor;
            IL.Celeste.BadelineOldsite.Added -= ModBadelineOldsiteAdded;
            IL.Celeste.BadelineOldsite.CanChangeMusic -= ModBadelineOldsiteCanChangeMusic;
            On.Celeste.BadelineOldsite.IsChaseEnd -= ModBadelineOldsiteIsChaseEnd;
            On.Celeste.Level.LoadLevel -= ModLoadLevel;
            On.Celeste.Level.TransitionRoutine -= ModTransitionRoutine;
            IL.Celeste.Player.UpdateChaserStates -= ModUpdateChaserStates;

            On.Celeste.LightFadeTrigger.OnStay -= ModOnLightFadeTriggerStay;

            IL.Celeste.Snowball.Update -= ModSnowballUpdateIL;
            IL.Celeste.Pathfinder.ctor -= ModPathfinderConstructor;

            On.Celeste.BadelineBoost.BoostRoutine -= WrapBadelineBoostRoutine;
        }

        private void ModEndPauseEffects(On.Celeste.Level.orig_EndPauseEffects orig, Level self) {
            orig(self);

            // rewrite the file in case the player changed anything in the pause menu
            Randomizer.SpitOutEnabledVariantsInFile();
        }

        // ================ Extended Variant Trigger handling ================

        /// <summary>
        /// Restore extended variants values when entering a saved level.
        /// </summary>
        /// <param name="session">unused</param>
        /// <param name="fromSaveData">true if loaded from save data, false otherwise</param>
        public void OnLevelEnter(Session session, bool fromSaveData) {
            // be sure to load the settings in the randomizer first
            Randomizer.UpdateCountersFromSettings();

            Randomizer.SpitOutEnabledVariantsInFile();

            foreach (Variant v in Session.VariantsEnabledViaTrigger.Keys) {
                Logger.Log("ExtendedVariantsModule/OnLevelEnter", $"Loading save: restoring {v} to {Session.VariantsEnabledViaTrigger[v]}");
                int oldValue = ExtendedVariantTrigger.SetVariantValue(v, Session.VariantsEnabledViaTrigger[v]);
                VariantValuesBeforeOverride[v] = oldValue;
            }
        }

        /// <summary>
        /// Handles ExtendedVariantTrigger constructing when loading a level.
        /// </summary>
        /// <param name="level">The level being loaded</param>
        /// <param name="levelData">unused</param>
        /// <param name="offset">offset passed to the trigger</param>
        /// <param name="entityData">the entity parameters</param>
        /// <returns>true if the trigger was loaded, false otherwise</returns>
        public bool OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            if(entityData.Name == "ExtendedVariantTrigger") {
                level.Add(new ExtendedVariantTrigger(entityData, offset));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handle respawn (reset variants that were set in the room).
        /// </summary>
        /// <param name="obj">unused</param>
        public void OnPlayerSpawn(Player obj) {
            if (OldVariantsInRoom.Count != 0) {
                // reset all variants that got set in the room
                foreach (Variant v in OldVariantsInRoom.Keys) {
                    Logger.Log("ExtendedVariantsModule/OnPlayerSpawn", $"Died in room: resetting {v} to {OldVariantsInRoom[v]}");
                    ExtendedVariantTrigger.SetVariantValue(v, OldVariantsInRoom[v]);
                }

                // clear values
                Logger.Log("ExtendedVariantsModule/OnPlayerSpawn", "Room state reset");
                OldVariantsInRoom.Clear();
                OverridenVariantsInRoom.Clear();
            }
        }

        /// <summary>
        /// Handle screen transitions (make variants set within the room permanent).
        /// </summary>
        /// <param name="level">unused</param>
        /// <param name="next">unused</param>
        /// <param name="direction">unused</param>
        public void OnLevelTransitionTo(Level level, LevelData next, Vector2 direction) {
            CommitVariantChanges();
        }

        /// <summary>
        /// Edits the OnEnter method in ChangeRespawnTrigger, so that the variants set are made permanent when the respawn point is changed.
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModRespawnTrigger(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // simply jump into the "if" controlling whether the respawn should be changed or not
            // (yet again, this is brtrue.s in XNA and brfalse.s in FNA. Thanks compiler.)
            if (cursor.TryGotoNext(MoveType.After, instr => (instr.OpCode == OpCodes.Brtrue_S || instr.OpCode == OpCodes.Brfalse_S))) {
                // and call our method in there
                Logger.Log("ExtendedVariantsModule", $"Inserting call to CommitVariantChanges at index {cursor.Index} in CIL code for OnEnter in ChangeRespawnTrigger");
                cursor.EmitDelegate<Action>(CommitVariantChanges);
            }
        }

        /// <summary>
        /// Make the changes in variant settings permanent (even if the player dies).
        /// </summary>
        public static void CommitVariantChanges() {
            if (OverridenVariantsInRoom.Count != 0) {
                // "commit" variants set in the room to save slot
                foreach (Variant v in OverridenVariantsInRoom.Keys) {
                    Logger.Log("ExtendedVariantsModule/CommitVariantChanges", $"Committing variant change {v} to {OverridenVariantsInRoom[v]}");
                    Session.VariantsEnabledViaTrigger[v] = OverridenVariantsInRoom[v];
                }

                // clear values
                Logger.Log("ExtendedVariantsModule/CommitVariantChanges", "Room state reset");
                OldVariantsInRoom.Clear();
                OverridenVariantsInRoom.Clear();
            }
        }

        public void OnLevelExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
            Randomizer.ClearFile();

            if (VariantValuesBeforeOverride.Count != 0) {
                // reset all variants that got set during the session
                foreach (Variant v in VariantValuesBeforeOverride.Keys) {
                    Logger.Log("ExtendedVariantsModule/OnLevelExit", $"Ending session: resetting {v} to {VariantValuesBeforeOverride[v]}");
                    ExtendedVariantTrigger.SetVariantValue(v, VariantValuesBeforeOverride[v]);
                }
            }

            // exiting level: clear state
            Logger.Log("ExtendedVariantsModule/OnLevelExit", "Room and session state reset");
            OverridenVariantsInRoom.Clear();
            OldVariantsInRoom.Clear();
            VariantValuesBeforeOverride.Clear();

            // reset the flags about stuff generated by EVM
            snowBackdropAddedByEVM = false;
            extendedPathfinder = false;
            BadelineBoosting = false;
        }

        // ================ Stamp on Chapter Complete screen ================

        /// <summary>
        /// Wraps the VersionNumberAndVariants in the base game in order to add the Variant Mode logo if Extended Variants are enabled.
        /// </summary>
        public static void ModVersionNumberAndVariants(On.Celeste.AreaComplete.orig_VersionNumberAndVariants orig, string version, float ease, float alpha) {
            if(Settings.MasterSwitch) {
                // The "if" conditioning the display of the Variant Mode logo is in an "orig_" method, we can't access it with IL.Celeste.
                // The best we can do is turn on Variant Mode, run the method then restore its original value.
                bool oldVariantModeValue = SaveData.Instance.VariantMode;
                SaveData.Instance.VariantMode = true;

                orig.Invoke(version, ease, alpha);

                SaveData.Instance.VariantMode = oldVariantModeValue;
            } else {
                // Extended Variants are disabled so just keep the original behaviour
                orig.Invoke(version, ease, alpha);
            }
        }
        
        // ================ Gravity handling ================

        /// <summary>
        /// Edits the NormalUpdate method in Player (handling the player state when not doing anything like climbing etc.)
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModNormalUpdate(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // find out where the constant 900 (downward acceleration) is loaded into the stack
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 900f)) {
                Logger.Log("ExtendedVariantsModule", $"Applying gravity to constant at {cursor.Index} in CIL code for NormalUpdate");

                // add two instructions to multiply those constants with the "gravity factor"
                cursor.EmitDelegate<Func<float>>(DetermineGravityFactor);
                cursor.Emit(OpCodes.Mul);
            }

            // chain every other NormalUpdate usage
            ModNormalUpdateFallSpeed(il);
            ModNormalUpdateSpeedX(il);
            ModNormalUpdateFriction(il);
            ModNormalUpdateJumpCount(il);
            ModNormalUpdateForceDuckOnGround(il);
        }

        /// <summary>
        /// Returns the currently configured gravity factor.
        /// </summary>
        /// <returns>The gravity factor (1 = default gravity)</returns>
        public static float DetermineGravityFactor() {
            return Settings.GravityFactor;
        }

        // ================ Fall speed handling ================

        /// <summary>
        /// Edits the NormalBegin method in Player, so that ma fall speed is applied right when entering the "normal" state.
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        private void ModNormalBegin(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // go wherever the maxFall variable is initialized to 160 (... I mean, that's a one-line method, but maxFall is private so...)
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 160f)) {
                Logger.Log("ExtendedVariantsModule", $"Applying max fall speed factor to constant at {cursor.Index} in CIL code for NormalBegin");

                // add two instructions to multiply those constants with the "fall speed factor"
                cursor.EmitDelegate<Func<float>>(DetermineFallSpeedFactor);
                cursor.Emit(OpCodes.Mul);
            }
        }

        /// <summary>
        /// Edits the NormalUpdate method in Player (handling the player state when not doing anything like climbing etc.)
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModNormalUpdateFallSpeed(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // we will edit 2 constants here:
            // * 160 = max falling speed
            // * 240 = max falling speed when holding Down

            // find out where those constants are loaded into the stack
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && ((float)instr.Operand == 160f || (float)instr.Operand == 240f))) {
                Logger.Log("ExtendedVariantsModule", $"Applying max fall speed factor to constant at {cursor.Index} in CIL code for NormalUpdate");

                // add two instructions to multiply those constants with the "fall speed factor"
                cursor.EmitDelegate<Func<float>>(DetermineFallSpeedFactor);
                cursor.Emit(OpCodes.Mul);
            }

            cursor.Index = 0;

            // go back to the first 240f, then to the next "if" implying MoveY
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 240f)
                && cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldsfld && ((FieldReference)instr.Operand).Name.Contains("MoveY"))
                && cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Bne_Un || instr.OpCode == OpCodes.Brfalse)) {
                Logger.Log("ExtendedVariantsModule", $"Injecting code to fix animation with 0 fall speed at {cursor.Index} in CIL code for NormalUpdate");

                // save the target of this branch
                object label = cursor.Prev.Operand;

                // the goal here is to add another condition to the if: FallSpeedFactor should not be zero
                // so that the game does not try computing the animation (doing a nice division by 0 by the way)
                cursor.EmitDelegate<Func<float>>(DetermineFallSpeedFactor);
                cursor.Emit(OpCodes.Ldc_R4, 0f);
                cursor.Emit(OpCodes.Beq, label); // we jump (= skip the "if") if DetermineFallSpeedFactor is equal to 0.
            }
        }
        
        /// <summary>
        /// Edits the UpdateSprite method in Player (updating the player animation.)
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModUpdateSprite(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // the goal is to multiply 160 (max falling speed) with the fall speed factor to fix the falling animation
            // let's search for all 160 occurrences in the IL code
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 160f)) {
                Logger.Log("ExtendedVariantsModule", $"Applying fall speed and gravity to constant at {cursor.Index} in CIL code for UpdateSprite to fix animation");

                // add two instructions to multiply those constants with a mix between fall speed and gravity
                cursor.EmitDelegate<Func<float>>(MixFallSpeedAndGravity);
                cursor.Emit(OpCodes.Mul);
                // also remove 0.1 to prevent an animation glitch caused by rounding (I guess?) on very low fall speeds
                cursor.Emit(OpCodes.Ldc_R4, 0.1f);
                cursor.Emit(OpCodes.Sub);
            }

            // chain every other UpdateSprite usage
            ModUpdateSpriteFriction(il);
        }

        /// <summary>
        /// Returns the currently configured fall speed factor.
        /// </summary>
        /// <returns>The fall speed factor (1 = default fall speed)</returns>
        public static float DetermineFallSpeedFactor() {
            return Settings.FallSpeedFactor;
        }

        public static float MixFallSpeedAndGravity() {
            return Math.Min(Settings.FallSpeedFactor, Settings.GravityFactor);
        }

        // ================ X speed handling ================

        /// <summary>
        /// Edits the NormalUpdate method in Player (handling the player state when not doing anything like climbing etc.)
        /// to handle the X speed part.
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModNormalUpdateSpeedX(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // we use 90 as an anchor (an "if" before the instruction we want to mod loads 90 in the stack)
            // then we jump to the next usage of V_6 to get the reference to it (no idea how to build it otherwise)
            // (actually, this is V_31 in the FNA version)
            if (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 90f)
                && cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Stloc_S
                    && (((VariableDefinition)instr.Operand).Index == 6 || ((VariableDefinition)instr.Operand).Index == 31))) {

                VariableDefinition variable = (VariableDefinition) cursor.Next.Operand;

                // we jump before the next ldflda, which is between the "if (this.level.InSpace)" and the next one
                if (cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Ldflda)) {
                    Logger.Log("ExtendedVariantsModule", $"Applying X speed modding to variable {variable.ToString()} at {cursor.Index} in CIL code for NormalUpdate");

                    // pop ldarg.0
                    cursor.Emit(OpCodes.Pop);

                    // modify variable 6 to apply X factor
                    cursor.Emit(OpCodes.Ldloc_S, variable);
                    cursor.EmitDelegate<Func<float>>(DetermineSpeedXFactor);
                    cursor.Emit(OpCodes.Mul);
                    cursor.Emit(OpCodes.Stloc_S, variable);

                    // execute ldarg.0 again
                    cursor.Emit(OpCodes.Ldarg_0);
                }
            }
        }

        /// <summary>
        /// Edits the SuperJump method in Player (called when super/hyperdashing.)
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModSuperJump(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // we want to multiply 260f (speed given by a superdash) with the X speed factor
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 260f)) {
                Logger.Log("ExtendedVariantsModule", $"Applying X speed to constant at {cursor.Index} in CIL code for SuperJump");
                cursor.EmitDelegate<Func<float>>(DetermineSpeedXFactor);
                cursor.Emit(OpCodes.Mul);
                cursor.EmitDelegate<Func<float>>(DetermineHyperdashSpeedFactor);
                cursor.Emit(OpCodes.Mul);
            }

            // chain every other SuperJump usage
            ModSuperJumpHeight(il);
        }
        
        /// <summary>
        /// Edits the SuperJump method in Player (called when super/hyperdashing on a wall.)
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModSuperWallJump(ILContext il)  {
            ILCursor cursor = new ILCursor(il);

            // we want to multiply 170f (X speed given by a superdash) with the X speed factor
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 170f)) {
                Logger.Log("ExtendedVariantsModule", $"Applying X speed to constant at {cursor.Index} in CIL code for SuperWallJump");
                cursor.EmitDelegate<Func<float>>(DetermineSpeedXFactor);
                cursor.Emit(OpCodes.Mul);
            }

            // chain every other SuperWallJump usage
            ModSuperWallJumpHeight(il);
        }

        /// <summary>
        /// Edits the WallJump method in Player (called when walljumping, obviously.)
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModWallJump(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // we want to multiply 130f (X speed given by a walljump) with the X speed factor
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 130f)) {
                Logger.Log("ExtendedVariantsModule", $"Applying X speed to constant at {cursor.Index} in CIL code for WallJump");
                cursor.EmitDelegate<Func<float>>(DetermineSpeedXFactor);
                cursor.Emit(OpCodes.Mul);
            }

            // chain every other WallJump usage
            ModWallJumpHeight(il);
            ModWallJumpNeutralJumping(il);
        }

        /// <summary>
        /// Returns the currently configured X speed factor.
        /// </summary>
        /// <returns>The speed factor (1 = default speed)</returns>
        public static float DetermineSpeedXFactor() {
            return Settings.SpeedXFactor;
        }

        // ================ Stamina handling ================

        /// <summary>
        /// Edits the ClimbUpdate method in Player (handling the player state when climbing).
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModClimbUpdate(ILContext il) {
            patchOutStamina(il);
        }

        /// <summary>
        /// Edits the SwimBegin method in Player (handling the player state when starting to swim).
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModSwimBegin(ILContext il) {
            patchOutStamina(il);
        }

        /// <summary>
        /// Edits the DreamDashBegin method in Player (handling the player state when entering a dream dash block).
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModDreamDashBegin(ILContext il) {
            patchOutStamina(il);
        }

        /// <summary>
        /// Edits the constructor of Player.
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModPlayerConstructor(ILContext il) {
            patchOutStamina(il);
        }

        /// <summary>
        /// Mods the SmashRoutine in SummitGem.
        /// </summary>
        /// <param name="orig">The original method</param>
        /// <param name="self">The SummitGem instance</param>
        /// <param name="player">The player</param>
        /// <param name="level">(unused)</param>
        /// <returns></returns>
        private IEnumerator ModSummitGemSmash(On.Celeste.SummitGem.orig_SmashRoutine orig, SummitGem self, Player player, Level level) {
            IEnumerator coroutine = orig.Invoke(self, player, level);

            // get the first value, this includes the code setting stamina back to 110f
            coroutine.MoveNext();
            yield return coroutine.Current;

            player.Stamina = DetermineBaseStamina();

            // leave the rest of the coroutine intact
            while (coroutine.MoveNext()) {
                yield return coroutine.Current;
            }
            yield break;
        }

        /// <summary>
        /// Wraps the Update method in the base game (used to refresh the player state).
        /// </summary>
        /// <param name="orig">The original Update method</param>
        /// <param name="self">The Player instance</param>
        public void ModUpdate(On.Celeste.Player.orig_Update orig, Player self) {
            // notify the randomizer a frame happened, so that it can trigger timed events like vanillafying
            Randomizer.OnUpdate();

            // since we cannot patch IL in orig_Update, we will wrap it and try to guess if the stamina was reset
            // this is **certainly** the case if the stamina changed and is now 110
            float staminaBeforeCall = self.Stamina;
            orig.Invoke(self);
            if (self.Stamina == 110f && staminaBeforeCall != 110f) {
                // reset it to the value we chose instead of 110
                self.Stamina = DetermineBaseStamina();
            }

            // chain the other functions of Update()
            ModUpdateRegularHiccups(self);
        }

        /// <summary>
        /// Replaces the default 110 stamina value with the one defined in the settings.
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        private static void patchOutStamina(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            // now, patch everything stamina-related (every instance of 110)
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 110f)) {
                Logger.Log("ExtendedVariantsModule", $"Patching stamina at index {cursor.Index} in CIL code");

                // pop the 110 and call our method instead
                cursor.Emit(OpCodes.Pop);
                cursor.EmitDelegate<Func<float>>(DetermineBaseStamina);
            }
        }

        /// <summary>
        /// Replaces the RefillStamina in the base game.
        /// </summary>
        /// <param name="orig">The original RefillStamina method</param>
        /// <param name="self">The Player instance</param>
        public static void ModRefillStamina(On.Celeste.Player.orig_RefillStamina orig, Player self) {
            // invoking the original method is not really useful, but another mod may try to hook it, so don't break it if the Stamina variant is disabled
            orig.Invoke(self);

            if (Settings.Stamina != 11) {
                self.Stamina = DetermineBaseStamina();
            }
        }

        /// <summary>
        /// Returns the max stamina.
        /// </summary>
        /// <returns>The max stamina (default 110)</returns>
        public static float DetermineBaseStamina() {
            return Settings.Stamina * 10f;
        }

        // ================ Dash speed handling ================

        /// <summary>
        /// Edits the CallDashEvents method in Player (called multiple times when the player dashes).
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModCallDashEvents(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // enter the if in the method (the "if" checks if dash events were already called) and inject ourselves in there
            // (those are actually brtrue in the XNA version and brfalse in the FNA version. Seriously?)
            if (cursor.TryGotoNext(MoveType.After, instr => (instr.OpCode == OpCodes.Brtrue || instr.OpCode == OpCodes.Brfalse))) {
                Logger.Log("ExtendedVariantsModule", $"Adding code to mod dash speed at index {cursor.Index} in CIL code for CallDashEvents");

                // just add a call to ModifyDashSpeed (arg 0 = this)
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Player>>(ModifyDashSpeed);
            }
        }

        /// <summary>
        /// Modifies the dash speed of the player.
        /// </summary>
        /// <param name="self">A reference to the player</param>
        public static void ModifyDashSpeed(Player self) {
            self.Speed *= Settings.DashSpeedFactor;

            // chain call to this
            ModifyDashSpeedInvertDashes(self);
        }

        // ================ Dash count handling ================

        /// <summary>
        /// Replaces the RefillDash in the base game.
        /// </summary>
        /// <param name="orig">The original RefillDash method</param>
        /// <param name="self">The Player instance</param>
        public static bool ModRefillDash(On.Celeste.Player.orig_RefillDash orig, Player self) {
            if (Settings.RefillJumpsOnDashRefill && Settings.JumpCount >= 2) {
                RefillJumpBuffer(1f);
            }

            if (Settings.DashCount == -1) {
                return orig.Invoke(self);
            } else if(self.Dashes < Settings.DashCount) {
                self.Dashes = Settings.DashCount;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Edits the UseRefill method in Player (called when the player gets a refill, obviously.)
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModUseRefill(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // we want to insert ourselves just before the first stloc.0
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Stloc_0)) {
                Logger.Log("ExtendedVariantsModule", $"Modding dash count given by refills at {cursor.Index} in CIL code for UseRefill");

                // call our method just before storing the result from get_MaxDashes in local variable 0
                cursor.EmitDelegate<Func<int, int>>(DetermineDashCount);
            }
        }

        /// <summary>
        /// Wraps the UseRefill method, so that it returns true when crystals refill jumps.
        /// </summary>
        /// <param name="orig">The original method</param>
        /// <param name="self">The Player entity</param>
        /// <param name="twoDashes">unused</param>
        /// <returns>true if the original method returned true OR the refill also refilled dashes, false otherwise</returns>
        private bool ModOnUseRefill(On.Celeste.Player.orig_UseRefill orig, Player self, bool twoDashes) {
            int jumpBufferBefore = jumpBuffer;

            bool origResult = orig(self, twoDashes);
            if(Settings.RefillJumpsOnDashRefill && jumpBuffer > jumpBufferBefore) {
                // break the crystal because it refilled jumps
                return true;
            }
            return origResult;
        }

        /// <summary>
        /// Edits the UpdateHair method in Player (mainly computing the hair color).
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModUpdateHair(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // the goal here is to turn "this.Dashes == 2" checks into "this.Dashes >= 2" to make it look less weird
            // and be more consistent with the behaviour of the "Infinite Dashes" variant.
            // (without this patch, with > 2 dashes, Madeline's hair is red, then turns pink, then red again before becoming blue)
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_I4_2 && (instr.Next.OpCode == OpCodes.Bne_Un_S || instr.Next.OpCode == OpCodes.Ceq))) {
                Logger.Log("ExtendedVariantsModule", $"Fixing hair color when having more than 2 dashes by modding a check at {cursor.Index} in CIL code for UpdateHair");

                if (cursor.Next.OpCode == OpCodes.Bne_Un_S) {
                    // XNA version: this is a branch
                    // small trap: the instruction in CIL code actually says "jump if **not** equal to 2". So we set it to "jump if lower than 2" instead
                    cursor.Next.OpCode = OpCodes.Blt_Un_S;
                } else {
                    // FNA version: this is a boolean FOLLOWED by a branch
                    // we're turning this boolean from "Dashes == 2" to "Dashes > 1"
                    cursor.Prev.OpCode = OpCodes.Ldc_I4_1;
                    cursor.Next.OpCode = OpCodes.Cgt;
                }
            }
        }

        /// <summary>
        /// Wraps the Added method in the base game (used to initialize the player state).
        /// </summary>
        /// <param name="orig">The original Added method</param>
        /// <param name="self">The Player instance</param>
        /// <param name="scene">Argument of the original method (passed as is)</param>
        public static void ModAdded(On.Celeste.Player.orig_Added orig, Player self, Scene scene) {
            orig.Invoke(self, scene);
            self.Dashes = DetermineDashCount(self.Dashes);
        }

        /// <summary>
        /// Returns the dash count.
        /// </summary>
        /// <param name="defaultValue">The default value (= Player.MaxDashes)</param>
        /// <returns>The dash count</returns>
        public static int DetermineDashCount(int defaultValue) {
            if(Settings.RefillJumpsOnDashRefill && Settings.JumpCount >= 2) {
                RefillJumpBuffer(1f);
            }

            if (Settings.DashCount == -1) {
                return defaultValue;
            }

            return Settings.DashCount;
        }

        // ================ Ground friction handling ================

        /// <summary>
        /// Edits the NormalUpdate method in Player (handling the player state when not doing anything like climbing etc.) to apply ground friction.
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModNormalUpdateFriction(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // jump to the 500 in "this.Speed.X = Calc.Approach(this.Speed.X, 0f, 500f * Engine.DeltaTime);"
            if (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 500f)) {
                Logger.Log("ExtendedVariantsModule", $"Applying friction to constant at {cursor.Index} (ducking stop speed on ground) in CIL code for NormalUpdate");

                cursor.EmitDelegate<Func<float>>(DetermineFrictionFactor);
                cursor.Emit(OpCodes.Mul);
            }

            // jump to "float num = this.onGround ? 1f : 0.65f;" by jumping to 0.65 then 1 (the numbers are swapped in the IL code)
            if (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 0.65f)
                && cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 1f)) {

                Logger.Log("ExtendedVariantsModule", $"Applying friction to constant at {cursor.Index} (friction factor on ground) in CIL code for NormalUpdate");

                // 1 is the acceleration when on the ground. Apply the friction factor to it.
                cursor.EmitDelegate<Func<float>>(DetermineFrictionFactor);
                cursor.Emit(OpCodes.Mul);
            }
        }

        /// <summary>
        /// Edits the UpdateSprite method in Player (updating the player animation) to fix the animations when using modded friction.
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModUpdateSpriteFriction(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // we're jumping to this line: "if (Math.Abs(this.Speed.X) <= 25f && this.moveX == 0)"
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 25f)) {
                Logger.Log("ExtendedVariantsModule", $"Modding constant at {cursor.Index} in CIL code for UpdateSprite to fix animation with friction");

                // call our method which will essentially replace the 25 with whatever value we want
                cursor.Emit(OpCodes.Pop);
                cursor.EmitDelegate<Func<float>>(GetIdleAnimationThreshold);
            }
        }

        /// <summary>
        /// Compute the idle animation threshold (when the player lets go every button, Madeline will use the walking animation until
        /// her X speed gets below this value. Under this value, she will use her idle animation.)
        /// </summary>
        /// <returns>The idle animation threshold (minimum 25, gets higher as the friction factor is lower)</returns>
        public static float GetIdleAnimationThreshold() {
            if(Settings.FrictionFactor >= 1f) {
                // keep the default value
                return 25f;
            }

            // shift the "stand still" threshold towards max walking speed, which is 90f
            // for example, it will give 83.5 when friction factor is 0.1, Madeline will appear to slip standing still.
            return 25f + (90f * Settings.SpeedXFactor - 25f) * (1 - Settings.FrictionFactor);
        }

        /// <summary>
        /// Returns the currently configured friction factor.
        /// </summary>
        /// <returns>The friction factor (1 = default friction)</returns>
        public static float DetermineFrictionFactor() {
            return Settings.FrictionFactor;
        }

        // ================ Jump height handling ================

        /// <summary>
        /// Edits the Jump method in Player (called when jumping, simply.)
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModJump(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // the speed applied to jumping is simply -105f (negative = up). Let's multiply this with our jump height factor.
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == -105f)) {
                Logger.Log("ExtendedVariantsModule", $"Modding constant at {cursor.Index} in CIL code for Jump to make jump height editable");

                // add two instructions to multiply -105f with the "jump height factor"
                cursor.EmitDelegate<Func<float>>(DetermineJumpHeightFactor);
                cursor.Emit(OpCodes.Mul);
            }

            // chain every other UpdateSprite usage
            ModUpdateSpriteFriction(il);
        }

        /// <summary>
        /// Edits the SuperJump method in Player (called when super/hyperdashing.)
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModSuperJumpHeight(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // we want to multiply -105f (height given by a superdash) with the jump height factor
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == -105f)) {
                Logger.Log("ExtendedVariantsModule", $"Applying jump height to constant at {cursor.Index} in CIL code for SuperJump");
                cursor.EmitDelegate<Func<float>>(DetermineJumpHeightFactor);
                cursor.Emit(OpCodes.Mul);
            }
        }

        /// <summary>
        /// Edits the WallJump method in Player (called when walljumping, obviously.)
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModWallJumpHeight(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // we want to multiply -105f (height given by a superdash) with the jump height factor
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == -105f)) {
                Logger.Log("ExtendedVariantsModule", $"Applying jump height to constant at {cursor.Index} in CIL code for WallJump");
                cursor.EmitDelegate<Func<float>>(DetermineJumpHeightFactor);
                cursor.Emit(OpCodes.Mul);
            }
        }

        /// <summary>
        /// Edits the SuperWallJump method in Player (called when super/hyperdashing on a wall.)
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModSuperWallJumpHeight(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // we want to multiply -160f (height given by a superdash) with the jump height factor
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == -160f)) {
                Logger.Log("ExtendedVariantsModule", $"Applying jump height to constant at {cursor.Index} in CIL code for SuperWallJump");
                cursor.EmitDelegate<Func<float>>(DetermineJumpHeightFactor);
                cursor.Emit(OpCodes.Mul);
                cursor.EmitDelegate<Func<float>>(DetermineWallBouncingSpeedFactor);
                cursor.Emit(OpCodes.Mul);
            }
        }

        /// <summary>
        /// Returns the currently configured jump height factor.
        /// </summary>
        /// <returns>The jump height factor (1 = default jump height)</returns>
        public static float DetermineJumpHeightFactor() {
            return Settings.JumpHeightFactor;
        }

        // ================ Disable walljumping handling ================

        /// <summary>
        /// Detour the WallJump method in order to disable it if we want.
        /// </summary>
        /// <param name="orig">the original method</param>
        /// <param name="self">the player</param>
        /// <param name="dir">the wall jump direction</param>
        private void ModOnWallJump(On.Celeste.Player.orig_WallJump orig, Player self, int dir) {
            if (!Settings.DisableWallJumping) {
                orig(self, dir);
            }
        }

        /// <summary>
        /// Mods the WallJumpCheck method, checking if a walljump is possible.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self">the player</param>
        /// <param name="dir">the direction</param>
        /// <returns>true if walljumping is possible, false otherwise</returns>
        private bool ModWallJumpCheck(On.Celeste.Player.orig_WallJumpCheck orig, Player self, int dir) {
            if(Settings.DisableWallJumping) {
                return false;
            }
            return orig(self, dir);
        }

        // ================ Jump count handling ================

        private static int jumpBuffer = 0;

        /// <summary>
        /// Edits the NormalUpdate method in Player (handling the player state when not doing anything like climbing etc.) to apply jump count.
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModNormalUpdateJumpCount(ILContext il) {
            patchJumpGraceTimer(il);
        }

        private static void patchJumpGraceTimer(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            MethodReference wallJumpCheck = seekReferenceToMethod(il, "WallJumpCheck");

            // jump to whenever jumpGraceTimer is retrieved
            if (wallJumpCheck != null && cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldfld && ((FieldReference)instr.Operand).Name.Contains("jumpGraceTimer"))) {
                Logger.Log("ExtendedVariantsModule", $"Patching jump count in at {cursor.Index} in CIL code");

                // store a reference to it
                FieldReference refToJumpGraceTimer = ((FieldReference)cursor.Prev.Operand);

                // call this.WallJumpCheck(1)
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldc_I4_1);
                cursor.Emit(OpCodes.Callvirt, wallJumpCheck);

                // call this.WallJumpCheck(-1)
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldc_I4_M1);
                cursor.Emit(OpCodes.Callvirt, wallJumpCheck);

                // replace the jumpGraceTimer with the modded value
                cursor.EmitDelegate<Func<float, bool, bool, float>>(CanJump);

                // go back to the beginning of the method
                cursor.Index = 0;
                // and add a call to RefillJumpBuffer so that we can reset the jumpBuffer even if we cannot access jumpGraceTimer (being private)
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, refToJumpGraceTimer);
                cursor.EmitDelegate<Action<float>>(RefillJumpBuffer);
            }
        }

        public static void RefillJumpBuffer(float jumpGraceTimer) {
            // JumpCount - 1 because the first jump is from vanilla Celeste
            if (jumpGraceTimer > 0f) jumpBuffer = Settings.JumpCount - 1;
        }

        /// <summary>
        /// Detour the WallJump method in order to disable it if we want.
        /// </summary>
        public static float CanJump(float initialJumpGraceTimer, bool canWallJumpRight, bool canWallJumpLeft) {
            if(Settings.JumpCount == 0) {
                // we disabled jumping, so let's pretend the grace timer has run out
                return 0f;
            }
            if(canWallJumpLeft || canWallJumpRight) {
                // no matter what, don't touch vanilla behavior if a wall jump is possible
                // because inserting extra jumps would kill wall jumping
                return initialJumpGraceTimer;
            }
            if(Settings.JumpCount == 6) {
                // infinite jumping, yay
                return 1f;
            }
            if(Settings.JumpCount == 1 || initialJumpGraceTimer > 0f || jumpBuffer <= 0) {
                // return the default value because we don't want to change anything 
                // (we are disabled, our jump buffer ran out, or vanilla Celeste allows jumping anyway)
                return initialJumpGraceTimer;
            }
            // consume an Extended Variant Jump(TM)
            jumpBuffer--;
            return 1f;
        }

        /// <summary>
        /// Seeks any reference to a named method (callvirt) in IL code.
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        /// <param name="methodName">name of the method</param>
        /// <returns>A reference to the method</returns>
        private static MethodReference seekReferenceToMethod(ILContext il, string methodName) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Callvirt && ((MethodReference)instr.Operand).Name.Contains(methodName))) {
                return (MethodReference)cursor.Next.Operand;
            }
            return null;
        }


        // ================ Upside down handling ================

        /// <summary>
        /// Edits the Render method in Level (handling the whole level rendering).
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModLevelRender(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // jump right where Mirror Mode is handled
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Ldfld && ((FieldReference)instr.Operand).Name.Contains("MirrorMode"))) {
                // move back 2 steps (we are between Instance and MirrorMode in "SaveData.Instance.MirrorMode" and we want to be before that)
                cursor.Index -= 2;

                VariableDefinition positionVector = seekReferenceTo(il, cursor.Index, 5);
                VariableDefinition paddingVector = seekReferenceTo(il, cursor.Index, 9);

                if(positionVector == null || paddingVector == null) {
                    positionVector = seekReferenceTo(il, cursor.Index, 10);
                    paddingVector = seekReferenceTo(il, cursor.Index, 14);
                }

                if(positionVector != null && paddingVector != null) {
                    // insert our delegates to do about the same thing as vanilla Celeste at about the same time
                    Logger.Log("ExtendedVariantsModule", $"Adding upside down delegate call at {cursor.Index} in CIL code for LevelRender");

                    cursor.Emit(OpCodes.Ldloca_S, paddingVector);
                    cursor.Emit(OpCodes.Ldloca_S, positionVector);
                    cursor.EmitDelegate<TwoRefVectorParameters>(ApplyUpsideDownEffect);
                }
            }

            // move forward a bit to get after the MirrorMode loading
            cursor.Index += 3;

            // jump to the next MirrorMode usage again
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Ldfld && ((FieldReference)instr.Operand).Name.Contains("MirrorMode"))) {
                // jump back 2 steps
                cursor.Index -= 2;

                Logger.Log("ExtendedVariantsModule", $"Adding upside down delegate call at {cursor.Index} in CIL code for LevelRender for sprite effects");

                // erase "SaveData.Instance.Assists.MirrorMode ? SpriteEffects.FlipHorizontally : SpriteEffects.None"
                // that's 3 instructions to load MirrorMode, and 4 assigning either 1 or 0 to it
                cursor.RemoveRange(7);

                // and replace it with a delegate call
                cursor.EmitDelegate<Func<SpriteEffects>>(ApplyUpsideDownEffectToSprites);
            }
        }

        /// <summary>
        /// Seeks any reference to a numbered variable in IL code.
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        /// <param name="variableIndex">Index of the variable</param>
        /// <returns>A reference to the variable</returns>
        private static VariableDefinition seekReferenceTo(ILContext il, int startingPoint, int variableIndex) {
            ILCursor cursor = new ILCursor(il);
            cursor.Index = startingPoint;
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Ldloc_S && ((VariableDefinition)instr.Operand).Index == variableIndex)) {
                return (VariableDefinition)cursor.Next.Operand;
            }
            return null;
        }

        public delegate void TwoRefVectorParameters(ref Vector2 one, ref Vector2 two);
        
        public static void ApplyUpsideDownEffect(ref Vector2 paddingVector, ref Vector2 positionVector) {
            Input.Aim.InvertedY = (Input.MoveY.Inverted = Settings.UpsideDown);

            if(Settings.UpsideDown) {
                paddingVector.Y = -paddingVector.Y;
                positionVector.Y = 90f - (positionVector.Y - 90f);
            }
        }

        public static SpriteEffects ApplyUpsideDownEffectToSprites() {
            SpriteEffects effects = SpriteEffects.None;
            if (Settings.UpsideDown) effects |= SpriteEffects.FlipVertically;
            if (SaveData.Instance.Assists.MirrorMode) effects |= SpriteEffects.FlipHorizontally;
            return effects;
        }

        // ================ Hyperdash speed handling ================

        /// <summary>
        /// Returns the current hyperdash speed factor.
        /// </summary>
        /// <returns>The hyperdash speed factor (1 = default hyperdash speed)</returns>
        public static float DetermineHyperdashSpeedFactor() {
            return Settings.HyperdashSpeedFactor;
        }


        // ================ Wallbouncing speed handling ================

        /// <summary>
        /// Returns the current wallbounce speed factor.
        /// </summary>
        /// <returns>The wallbounce speed factor (1 = default wallbounce speed)</returns>
        public static float DetermineWallBouncingSpeedFactor()
        {
            return Settings.WallBouncingSpeedFactor;
        }

        // ================ Dash length handling ================

        private static float lastDashDuration = 0f;

        /// <summary>
        /// Edits the DashBegin method in Player (called when the player dashes).
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModDashBegin(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // jump where 0.3 is loaded (0.3 is the dash timer)
            if (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 0.3f)) {
                Logger.Log("ExtendedVariantsModule", $"Applying dash length to constant at {cursor.Index} in CIL code for DashBegin");

                cursor.EmitDelegate<Func<float>>(DetermineDashLengthFactor);
                cursor.Emit(OpCodes.Mul);
            }
        }

        /// <summary>
        /// Returns the current dash length factor.
        /// </summary>
        /// <returns>The dash length factor (1 = default dash length)</returns>
        public static float DetermineDashLengthFactor() {
            return Settings.DashLengthFactor;
        }


        private IEnumerator ModDashCoroutine(On.Celeste.Player.orig_DashCoroutine orig, Player self) {
            // let's try and intercept whenever the DashCoroutine sends out 0.3f or 0.15f
            // because we should mod that
            IEnumerator coroutine = orig.Invoke(self);
            while(coroutine.MoveNext()) {
                object o = coroutine.Current;
                if(o != null && o.GetType() == typeof(float)) {
                    float f = (float)o;
                    if (f == 0.15f || f == 0.3f) {
                        f *= Settings.DashLengthFactor;
                        lastDashDuration = f;
                    }
                    yield return f;
                } else {
                    yield return o;
                }
            }

            yield break;
        }

        /// <summary>
        /// Edits the DashUpdate method in Player (called while the player is dashing).
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModDashUpdate(ILContext il) {
            FieldReference dashTrailCounter = seekReferenceToVariable(il, "dashTrailCounter");

            if (dashTrailCounter != null) {
                ILCursor cursor = new ILCursor(il);

                Logger.Log("ExtendedVariantsModule", $"Patching dashTrailCounter to fix animation with long dashes at {cursor.Index} in CIL code for DashUpdate");

                // add a delegate call to modify dashTrailCounter (private variable set in DashCoroutine we can't mod with IL)
                // so that we add more trails if the dash is made longer than usual
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, dashTrailCounter);
                cursor.EmitDelegate<Func<int, int>>(ModDashTrailCounter);
                cursor.Emit(OpCodes.Stfld, dashTrailCounter);
            }

            // chain call to other patches
            patchJumpGraceTimer(il);
        }

        private static int ModDashTrailCounter(int dashTrailCounter) {
            if (Settings.DashLengthFactor != 1 && lastDashDuration != 0f) {
                float bakLastDashDuration = lastDashDuration;
                lastDashDuration = 0f;
                return (int)Math.Round(bakLastDashDuration * 10 * Settings.DashLengthFactor) - 1;
            }
            return dashTrailCounter;
        }

        /// <summary>
        /// Seeks any reference to a named variable in IL code.
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        /// <param name="variableName">name of the variable</param>
        /// <returns>A reference to the variable</returns>
        private static FieldReference seekReferenceToVariable(ILContext il, String variableName) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Ldfld && ((FieldReference)instr.Operand).Name.Contains(variableName))) {
                return (FieldReference)cursor.Next.Operand;
            }
            return null;
        }

        // ================ Force duck on ground handling ================

        /// <summary>
        /// Edits the NormalUpdate method in Player (handling the player state when not doing anything like climbing etc.)
        /// to handle the "force duck on ground" variant.
        /// </summary>
        /// <param name="il">Object allowing CIL patching</param>
        public static void ModNormalUpdateForceDuckOnGround(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // jump to "if(this.Ducking)" => that's a brfalse
            // (or, in the FNA version, "bool ducking = this.Ducking;" => that's a stloc.s)
            if (cursor.TryGotoNext(MoveType.After, 
                instr => instr.OpCode == OpCodes.Callvirt && ((MethodReference)instr.Operand).Name.Contains("get_Ducking"),
                instr => (instr.OpCode == OpCodes.Brfalse || instr.OpCode == OpCodes.Stloc_S))
                // in the XNA version, we get after the brfalse. In the FNA version, we have a ldloc.s and a brfalse after the cursor
                && (cursor.Prev.OpCode == OpCodes.Brfalse || cursor.Next.Next.OpCode == OpCodes.Brfalse)) {

                if(cursor.Prev.OpCode == OpCodes.Stloc_S) {
                    // get after the brfalse in order to line up with the XNA version
                    cursor.Index += 2;
                }

                Logger.Log("ExtendedVariantsModule", $"Inserting condition to enforce Force Duck On Ground at {cursor.Index} in CIL code for NormalUpdate");

                ILLabel target = (ILLabel)cursor.Prev.Operand;

                // basically, this turns the if into "if(this.Ducking && !Settings.ForceDuckOnGround)": this prevents unducking
                cursor.EmitDelegate<Func<bool>>(ForceDuckOnGroundEnabled);
                cursor.Emit(OpCodes.Brtrue, target);

                // jump to the "else" to modify this one too
                cursor.GotoLabel(target);

                // set ourselves just before the condition we want to mod
                if(cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Ldsfld && ((FieldReference)instr.Operand).Name.Contains("MoveY"))) {
                    ILCursor cursorAfterCondition = cursor.Clone();

                    if (cursorAfterCondition.TryGotoNext(MoveType.After, instr => (instr.OpCode == OpCodes.Bne_Un || instr.OpCode == OpCodes.Bne_Un_S))) {
                        Logger.Log("ExtendedVariantsModule", $"Inserting condition to enforce Force Duck On Ground at {cursor.Index} in CIL code for NormalUpdate");

                        // so this is basically "if (this.onGround && (Settings.ForceDuckOnGround || Input.MoveY == 1) && this.Speed.Y >= 0f)"
                        // by telling IL "if Settings.ForceDuckOnGround is true, jump over the Input.MoveY check"
                        cursor.EmitDelegate<Func<bool>>(ForceDuckOnGroundEnabled);
                        cursor.Emit(OpCodes.Brtrue, cursorAfterCondition.Next);
                    }
                }
            }
        }

        private static bool ForceDuckOnGroundEnabled() => Settings.ForceDuckOnGround;

        // ================ Invert Dashes handling ================

        /// <summary>
        /// Inverts the dash direction of the player.
        /// </summary>
        /// <param name="self">A reference to the player</param>
        public static void ModifyDashSpeedInvertDashes(Player self) {
            if (Settings.InvertDashes) {
                self.Speed *= -1;
                self.DashDir *= -1;
            }
        }


        // ================ Disable Neutral Jumping handling ================

        private static void ModWallJumpNeutralJumping(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // jump to the first MoveX usage (this.MoveX => ldarg.0 then ldfld MoveX basically)
            if(cursor.TryGotoNext(MoveType.Before,
                instr => instr.OpCode == OpCodes.Ldarg_0,
                instr => instr.OpCode == OpCodes.Ldfld && ((FieldReference)instr.Operand).Name.Contains("moveX"))) {

                // sneak between the ldarg.0 and the ldfld (the ldarg.0 is the target to a jump instruction, so we should put ourselves after that.)
                cursor.Index++;

                ILCursor cursorAfterBranch = cursor.Clone();
                if(cursorAfterBranch.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Brfalse_S)) {

                    Logger.Log("ExtendedVariantsModule", $"Inserting condition to enforce Disable Neutral Jumping at {cursor.Index} in CIL code for WallJump");

                    // pop the ldarg.0
                    cursor.Emit(OpCodes.Pop);

                    // before the MoveX check, check if neutral jumping is enabled: if it is not, skip the MoveX check
                    cursor.EmitDelegate<Func<bool>>(NeutralJumpingEnabled);
                    cursor.Emit(OpCodes.Brfalse_S, cursorAfterBranch.Next);

                    // push the ldarg.0 again
                    cursor.Emit(OpCodes.Ldarg_0);
                }
            }
        }

        /// <summary>
        /// Indicates if neutral jumping is enabled.
        /// </summary>
        public static bool NeutralJumpingEnabled() {
            return !Settings.DisableNeutralJumping;
        }


        // ================ Badeline Chasers Everywhere handling ================


        private void ModBadelineOldsiteConstructor(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // go everywhere where the 1.55 second delay is defined
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 1.55f)) {
                Logger.Log("ExtendedVariantsModule", $"Modding Badeline lag at {cursor.Index} in CIL code for BadelineOldsite constructor");

                // and substitute it with our own value
                cursor.Emit(OpCodes.Pop);
                cursor.EmitDelegate<Func<float>>(DetermineBadelineLag);
            }
        }

        private float DetermineBadelineLag() {
            return shouldIgnoreCustomDelaySettings() || Settings.BadelineLag == 0 ? 1.55f : Settings.BadelineLag / 10f;
        }

        private static bool shouldIgnoreCustomDelaySettings() {
            if (Engine.Scene.GetType() == typeof(Level)) {
                Player player = (Engine.Scene as Level).Tracker.GetEntity<Player>();
                // those states are "Intro Walk", "Intro Jump" and "Intro Wake Up". Getting killed during such an intro is annoying but can also **crash the game**
                if (player != null && (player.StateMachine.State == 12 || player.StateMachine.State == 13 || player.StateMachine.State == 15)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Wraps the LoadLevel method in order to add Badeline chasers when needed.
        /// </summary>
        /// <param name="orig">The base method</param>
        /// <param name="self">The level entity</param>
        /// <param name="playerIntro">The type of player intro</param>
        /// <param name="isFromLoader">unused</param>
        private void ModLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);

            // this method takes care of every situation except transitions, we let this one to TransitionRoutine
            if (Settings.BadelineChasersEverywhere && playerIntro != Player.IntroTypes.Transition) {
                // set this to avoid the player being instakilled during the level intro animation
                Player player = self.Tracker.GetEntity<Player>();
                if (player != null) player.JustRespawned = true;
            }

            if((Settings.BadelineChasersEverywhere || Settings.AffectExistingChasers) && playerIntro != Player.IntroTypes.Transition) {
                injectBadelineChasers(self);
            }

            // chain calls
            ModLoadLevelLighting(self, playerIntro);
            addSeekersToLevel(self);
            if (playerIntro != Player.IntroTypes.Transition) {
                addOshiroToLevel(self);
                applyWind(self);
                addSnowballToLevel(self);
            }
        }

        /// <summary>
        /// Wraps the TransitionRoutine in Level, in order to add Badeline chasers when needed.
        /// This is not done in LoadLevel, since this one will wait for the transition to be done, so that the entities from the previous screen are unloaded.
        /// </summary>
        /// <param name="orig">The base method</param>
        /// <param name="self">The level entity</param>
        /// <param name="next">unused</param>
        /// <param name="direction">unused</param>
        /// <returns></returns>
        private IEnumerator ModTransitionRoutine(On.Celeste.Level.orig_TransitionRoutine orig, Level self, LevelData next, Vector2 direction) {
            // first, notify the randomizer we're changing rooms so that the variants can be modified
            Randomizer.OnRoomChange();

            // just make sure the whole transition routine is over
            IEnumerator origEnum = orig(self, next, direction);
            while (origEnum.MoveNext()) {
                yield return origEnum.Current;
            }

            // then decide whether to add Badeline or not
            injectBadelineChasers(self);

            // chain other calls
            unmodBaseLightingAlpha(self);
            addOshiroToLevel(self);
            applyWind(self);
            addSnowballToLevel(self);

            yield break;
        }

        /// <summary>
        /// Mods the Added method in BadelineOldsite, to make it not kill chasers on screens they are not supposed to be.
        /// </summary>
        /// <param name="il">Object allowing IL modding</param>
        private void ModBadelineOldsiteAdded(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // go right after the equality check that compares the level set name with "Celeste"
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Call && ((MethodReference)instr.Operand).Name.Contains("op_Equality"))) {
                Logger.Log("ExtendedVariantsModule", $"Modding vanilla level check at index {cursor.Index} in the Added method from BadelineOldsite");

                // mod the result of that check to prevent the chasers we will spawn from... committing suicide
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate<Func<bool, Scene, bool>>(ModVanillaBehaviorCheckForChasers);
            }
        }

        /// <summary>
        /// Mods the CanChangeMusic method in BadelineOldsite, so that forcibly added chasers do not change the level music.
        /// </summary>
        /// <param name="il">Object allowing IL modding</param>
        private void ModBadelineOldsiteCanChangeMusic(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // go right after the equality check that compares the level set name with "Celeste"
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Call && ((MethodReference)instr.Operand).Name.Contains("op_Equality"))) {
                Logger.Log("ExtendedVariantsModule", $"Modding vanilla level check at index {cursor.Index} in the CanChangeMusic method from BadelineOldsite");

                // mod the result of that check to always use modded value, even in vanilla levels
                cursor.EmitDelegate<Func<bool, bool>>(ModVanillaBehaviorCheckForMusic);
            }
        }

        private void injectBadelineChasers(Level level) {
            bool hasChasersInBaseLevel = level.Tracker.CountEntities<BadelineOldsite>() != 0;

            if (Settings.BadelineChasersEverywhere) {
                Player player = level.Tracker.GetEntity<Player>();

                // check if the base level already has chasers
                if (player != null && !hasChasersInBaseLevel) {
                    // add a Badeline chaser where the player is, and tell it not to change the music to the chase music
                    for (int i = 0; i < Settings.ChaserCount; i++) {
                        level.Add(new AutoDestroyingBadelineOldsite(generateBadelineEntityData(level, i), player.Position, i));
                    }

                    level.Entities.UpdateLists();
                }
            }

            // plz disregard the settings and don't touch the chasers if in Badeline Intro cutscene
            // because the chaser triggers the cutscene, so having 10 chasers triggers 10 instances of the cutscene at the same time (a)
            if(Settings.AffectExistingChasers && hasChasersInBaseLevel && notInBadelineIntroCutscene(level)) {
                List<Entity> chasers = level.Tracker.GetEntities<BadelineOldsite>();
                if (chasers.Count > Settings.ChaserCount) {
                    // for example, if there are 6 chasers and we want 3, we will ask chasers 4-6 to commit suicide
                    for(int i = chasers.Count - 1; i >= Settings.ChaserCount; i--) {
                        chasers[i].RemoveSelf();
                    }
                } else if(chasers.Count < Settings.ChaserCount) {
                    // for example, if we have 2 chasers and we want 6, we will duplicate both chasers twice
                    for(int i = chasers.Count; i < Settings.ChaserCount; i++) {
                        int baseChaser = i % chasers.Count;
                        level.Add(new AutoDestroyingBadelineOldsite(generateBadelineEntityData(level, i), chasers[baseChaser].Position, i));
                    }
                }

                level.Entities.UpdateLists();
            }
        }

        private bool notInBadelineIntroCutscene(Level level) {
            return (level.Session.Area.GetSID() != "Celeste/2-OldSite" || level.Session.Level != "3" || level.Session.Area.Mode != AreaMode.Normal);
        }

        /// <summary>
        /// Generates a new EntityData instance, linked to the level given, an ID which will be the same if and only if generated
        /// in the same room with the same entityNumber, and an empty map of attributes.
        /// </summary>
        /// <param name="level">The level the entity belongs to</param>
        /// <param name="entityNumber">An entity number, between 0 and 19 inclusive</param>
        /// <returns>A fresh EntityData linked to the level, and with an ID</returns>
        private EntityData generateBasicEntityData(Level level, int entityNumber) {
            EntityData entityData = new EntityData();

            // we hash the current level name, so we will get a hopefully-unique "room hash" for each room in the level
            // the resulting hash should be between 0 and 49_999_999 inclusive
            int roomHash = Math.Abs(level.Session.Level.GetHashCode()) % 50_000_000;

            // generate an ID, minimum 1_000_000_000 (to minimize chances of conflicting with existing entities)
            // and maximum 1_999_999_999 inclusive (1_000_000_000 + 49_999_999 * 20 + 19) => max value for int32 is 2_147_483_647
            // => if the same entity (same entityNumber) is generated in the same room, it will have the same ID, like any other entity would
            entityData.ID = 1_000_000_000 + roomHash * 20 + entityNumber;

            entityData.Level = level.Session.LevelData;
            entityData.Values = new Dictionary<string, object>();

            return entityData;
        }

        private EntityData generateBadelineEntityData(Level level, int badelineNumber) {
            EntityData entityData = generateBasicEntityData(level, badelineNumber);
            entityData.Values["canChangeMusic"] = false;
            return entityData;
        }

        private bool ModVanillaBehaviorCheckForMusic(bool shouldUseVanilla) {
            // we can use the "flag-based behavior" on all A-sides
            if (Engine.Scene.GetType() == typeof(Level) && (Engine.Scene as Level).Session.Area.Mode == AreaMode.Normal) {
                return false;
            }
            // fall back to standard Everest behavior everywhere else: vanilla will not trigger chase music, and Everest will be flag-based
            return shouldUseVanilla;
        }

        private bool ModVanillaBehaviorCheckForChasers(bool shouldUseVanilla, Scene scene) {
            Session session = (scene as Level).Session;

            if (Settings.BadelineChasersEverywhere && 
                // don't use vanilla behaviour when that would lead the chasers to commit suicide
                (!session.GetLevelFlag("3") || session.GetLevelFlag("11") || 
                // don't use vanilla behaviour when that would trigger the Badeline intro cutscene, except (of course) on Old Site
                (session.Area.GetSID() != "Celeste/2-OldSite" && session.Level == "3" && session.Area.Mode == AreaMode.Normal))) {
                return false;
            }
            return shouldUseVanilla;
        }

        private bool ModBadelineOldsiteIsChaseEnd(On.Celeste.BadelineOldsite.orig_IsChaseEnd orig, BadelineOldsite self, bool value) {
            Session session = self.SceneAs<Level>().Session;
            if (session.Area.GetLevelSet() == "Celeste" && session.Area.GetSID() != "Celeste/2-OldSite") {
                // there is no chase end outside Old Site in the vanilla game.
                return false;
            }
            return orig(self, value);
        }

        /// <summary>
        /// Mods the UpdateChaserStates to tell it to save a bit more history of chaser states, so that we can spawn more chasers.
        /// </summary>
        /// <param name="il">Object allowing IL modding</param>
        private void ModUpdateChaserStates(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // go where the "4" is
            while (cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 4f)) {
                Logger.Log("ExtendedVariantsModule", $"Modding constant at {cursor.Index} in the UpdateChaserStates method to allow more chasers to spawn");

                // and replace it with a "5.5" in order to support up to 10 chasers
                cursor.Next.Operand = 5.5f;
            }
        }

        // ================ Oshiro Everywhere handling ================

        private void addOshiroToLevel(Level level) {
            // if no Oshiro is present...
            if(Settings.OshiroEverywhere && level.Tracker.CountEntities<AngryOshiro>() == 0) {
                // this replicates the behavior of Oshiro Trigger in vanilla Celeste
                Vector2 position = new Vector2(level.Bounds.Left - 32, level.Bounds.Top + level.Bounds.Height / 2);
                level.Add(new AutoDestroyingAngryOshiro(position, false));

                level.Entities.UpdateLists();
            }
        }

        // ================ Wind Everywhere handling ================

        private Random randomGenerator = new Random();

        private bool snowBackdropAddedByEVM = false;

        private readonly WindController.Patterns[] availableWindPatterns = new WindController.Patterns[] {
            WindController.Patterns.Left, WindController.Patterns.Right, WindController.Patterns.LeftStrong, WindController.Patterns.RightStrong, WindController.Patterns.RightCrazy,
                WindController.Patterns.LeftOnOff, WindController.Patterns.RightOnOff, WindController.Patterns.Alternating, WindController.Patterns.LeftOnOffFast,
                WindController.Patterns.RightOnOffFast, WindController.Patterns.Down, WindController.Patterns.Up
        };

        private void applyWind(Level level) {
            if (Settings.WindEverywhere != 0) {
                if(level.Foreground.Backdrops.Find(backdrop => backdrop.GetType() == typeof(WindSnowFG)) == null) {
                    // add the styleground / backdrop used in Golden Ridge to make wind actually visible
                    level.Foreground.Backdrops.Add(new WindSnowFG());
                    snowBackdropAddedByEVM = true;
                }

                // also switch the audio ambience so that wind can actually be heard too
                // (that's done by switching to the ch4 audio ambience. yep)
                Audio.SetAmbience("event:/env/amb/04_main", true);

                WindController.Patterns selectedPattern;
                if (Settings.WindEverywhere == availableWindPatterns.Length + 1) {
                    // pick up random wind
                    selectedPattern = availableWindPatterns[randomGenerator.Next(availableWindPatterns.Length)];
                } else {
                    // pick up the chosen wind pattern
                    selectedPattern = availableWindPatterns[Settings.WindEverywhere - 1];
                }

                // and apply it; this is basically what Wind Trigger does
                WindController windController = level.Entities.FindFirst<WindController>();
                if (windController == null) {
                    windController = new WindController(selectedPattern);
                    windController.SetStartPattern();
                    level.Add(windController);
                    level.Entities.UpdateLists();
                } else {
                    windController.SetPattern(selectedPattern);
                }
            } else if (snowBackdropAddedByEVM) {
                // remove the backdrop
                level.Foreground.Backdrops.RemoveAll(backdrop => backdrop.GetType() == typeof(WindSnowFG));
                snowBackdropAddedByEVM = false;
            }
        }

        // ================ Snowballs Everywhere handling ================

        private void addSnowballToLevel(Level level) {
            // do pretty much the same thing as the so-called "Wind Attack Trigger" from vanilla
            if (Settings.SnowballsEverywhere && level.Entities.FindFirst<Snowball>() == null) {
                Snowball snowball = new AutoDestroyingSnowball();
                level.Add(snowball);
                level.Entities.UpdateLists();

                // send the snowball off-screen, so that the player gets 0.8 seconds before the first snowball
                snowball.X = level.Camera.Left - 60f;
            }
        }

        private void ModSnowballUpdateIL(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // go everywhere where the 0.8 second delay is defined
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldc_R4 && (float)instr.Operand == 0.8f)) {
                Logger.Log("ExtendedVariantsModule", $"Modding delay between snowballs at {cursor.Index} in CIL code for Update in Snowball");

                // and substitute it with our own value
                cursor.Emit(OpCodes.Pop);
                cursor.EmitDelegate<Func<float>>(DetermineDelayBetweenSnowballs);
            }
        }

        private float DetermineDelayBetweenSnowballs() {
            if(shouldIgnoreCustomDelaySettings()) {
                return 0.8f;
            }
            return Settings.SnowballDelay / 10f;
        }

        // ================ Add Seekers handling ================

        private bool extendedPathfinder = false;

        private void addSeekersToLevel(Level level) {
            Player player = level.Tracker.GetEntity<Player>();
                
            if(player != null && Settings.AddSeekers != 0) {
                // first of all, ensure that the size is within the limits of the pathfinder
                if (level.Bounds.Width / 8 > 659 || level.Bounds.Height / 8 > 407) {
                    Logger.Log(LogLevel.Warn, "ExtendedVariantMode", $"Not spawning seekers since room exceeds max size of 659x407 tiles. ({level.Bounds.Width / 8}x{level.Bounds.Height / 8})");
                    return;
                }

                // ensure the pathfinder is the right one (i.e. the extended one)
                if (!extendedPathfinder) {
                    extendedPathfinder = true;
                    level.Pathfinder = new Pathfinder(level);
                }

                for(int seekerCount = 0; seekerCount < Settings.AddSeekers; seekerCount++) {
                    for (int i = 0; i < 100; i++) {
                        // roll a seeker position in the room
                        int x = randomGenerator.Next(level.Bounds.Width) + level.Bounds.X;
                        int y = randomGenerator.Next(level.Bounds.Height) + level.Bounds.Y;

                        // should be at least 100 pixels from the player
                        double playerDistance = Math.Sqrt(Math.Pow(MathHelper.Distance(x, player.X), 2) + Math.Pow(MathHelper.Distance(y, player.Y), 2));

                        // also check if we are not spawning in a wall, that would be a shame
                        Rectangle collideRectangle = new Rectangle(x - 8, y - 8, 16, 16);
                        if (playerDistance > 100 && !level.CollideCheck<Solid>(collideRectangle) && !level.CollideCheck<Seeker>(collideRectangle)) {
                            // build a Seeker with a proper EntityID to make Speedrun Tool happy (this is useless in vanilla Celeste but the constructor call is intercepted by Speedrun Tool)
                            EntityData seekerData = generateBasicEntityData(level, 10 + seekerCount);
                            seekerData.Position = new Vector2(x, y);
                            Seeker seeker = new AutoDestroyingSeeker(seekerData, Vector2.Zero);
                            level.Add(seeker);
                            break;
                        }
                    }
                }

                level.Entities.UpdateLists();
            } else if(Settings.AddSeekers == 0 && extendedPathfinder) {
                extendedPathfinder = false;
                level.Pathfinder = new Pathfinder(level);
            }
        }

        private void ModPathfinderConstructor(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // go everywhere where the 0.8 second delay is defined
            if (cursor.TryGotoNext(MoveType.After,
                instr => instr.OpCode == OpCodes.Ldc_I4 && (int)instr.Operand == 200,
                instr => instr.OpCode == OpCodes.Ldc_I4 && (int)instr.Operand == 200)) {

                Logger.Log("ExtendedVariantsModule", $"Modding size of pathfinder array at {cursor.Index} in CIL code for the Pathfinder constructor");

                // we will resize the pathfinder (provided that the seekers everywhere variant is enabled) to fit all rooms in vanilla Celeste
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate<Func<Pathfinder, int>>(DeterminePathfinderWidth);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate<Func<Pathfinder, int>>(DeterminePathfinderHeight);
            }
        }

        private int DeterminePathfinderWidth(Pathfinder self) {
            if(extendedPathfinder) {
                return 659;
            }
            return 200;
        }

        private int DeterminePathfinderHeight(Pathfinder self) {
            if (extendedPathfinder) {
                return 407;
            }
            return 200;
        }

        public static bool BadelineBoosting = false;

        private IEnumerator WrapBadelineBoostRoutine(On.Celeste.BadelineBoost.orig_BoostRoutine orig, BadelineBoost self, Player player) {
            BadelineBoosting = true;
            IEnumerator coroutine = orig(self, player);
            while (coroutine.MoveNext()) {
                yield return coroutine.Current;
            }
            BadelineBoosting = false;

            // apply the dash refill rules here (this does not call RefillDash)
            if(Settings.DashCount != -1) {
                // an alarm set off in the coroutine will add one more dash in 0.15 seconds
                player.Dashes = Settings.DashCount - 1;
            }
            if (Settings.RefillJumpsOnDashRefill && Settings.JumpCount >= 2) {
                RefillJumpBuffer(1f);
            }

            yield break;
        }

        public static bool ShouldEntitiesAutoDestroy(Player player) {
            return player != null && (player.StateMachine.State == 10 || player.StateMachine.State == 11) && !BadelineBoosting;
        }

        // ================ Regular Hiccups handling ================

        private float regularHiccupTimer = 0f;

        private void ModUpdateRegularHiccups(Player self) {
            if(Settings.RegularHiccups != 0 && !SaveData.Instance.Assists.Hiccups) {
                regularHiccupTimer -= Engine.DeltaTime;

                if (regularHiccupTimer > Settings.RegularHiccups / 10f) {
                    regularHiccupTimer = Settings.RegularHiccups / 10f;
                }
                if (regularHiccupTimer <= 0) {
                    regularHiccupTimer = Settings.RegularHiccups / 10f;
                    self.HiccupJump();
                }
            }
        }

        // ================ Room Lighting handling ================

        private float initialBaseLightingAlpha = -1f;

        /// <summary>
        /// Mods the lighting of a new room being loaded.
        /// </summary>
        /// <param name="self">The level we are in</param>
        /// <param name="introType">How the player enters the level</param>
        private void ModLoadLevelLighting(Level self, Player.IntroTypes introType) {
            if (Settings.RoomLighting != -1) {
                float lightingTarget = 1 - Settings.RoomLighting / 10f;
                if (introType == Player.IntroTypes.Transition) {
                    // we force the game into thinking this is not a dark room, so that it uses the BaseLightingAlpha + LightingAlphaAdd formula
                    // (this change is not permanent, exiting and re-entering will reset this flag)
                    self.DarkRoom = false;

                    // we mod BaseLightingAlpha temporarily so that it adds up with LightingAlphaAdd to the right value: the transition routine will smoothly switch to it
                    // (we are sure BaseLightingAlpha will not move for the entire session: it's only set when the map is loaded)
                    initialBaseLightingAlpha = self.BaseLightingAlpha;
                    self.BaseLightingAlpha = lightingTarget - self.Session.LightingAlphaAdd;
                } else {
                    // just set the initial value
                    self.Lighting.Alpha = lightingTarget;
                }
            }
        }

        /// <summary>
        /// Resets the BaseLightingAlpha to its initial value (if modified by ModLoadLevelLighting).
        /// </summary>
        /// <param name="self">The level</param>
        private void unmodBaseLightingAlpha(Level self) {
            if (initialBaseLightingAlpha != -1f) {
                self.BaseLightingAlpha = initialBaseLightingAlpha;
                initialBaseLightingAlpha = -1f;
            }
        }

        /// <summary>
        /// Locks the lighting to the value set by the user even when they hit a Light Fade Trigger.
        /// (The Light Fade Trigger will still change the LightingAlphaAdd, so it will be effective immediately if the variant is disabled.)
        /// </summary>
        /// <param name="orig">The original method</param>
        /// <param name="self">The trigger itself</param>
        /// <param name="player">The player hitting the trigger</param>
        private void ModOnLightFadeTriggerStay(On.Celeste.LightFadeTrigger.orig_OnStay orig, LightFadeTrigger self, Player player) {
            orig(self, player);

            if (Settings.RoomLighting != -1) {
                // be sure to lock the lighting alpha to the value set by the player
                float lightingTarget = 1 - Settings.RoomLighting / 10f;
                (self.Scene as Level).Lighting.Alpha = lightingTarget;
            }
        }
    }
}