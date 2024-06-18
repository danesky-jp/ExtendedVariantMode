﻿using Celeste;
using Celeste.Mod;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using static ExtendedVariants.Module.ExtendedVariantsModule;

namespace ExtendedVariants.Variants {
    public class PreserveWallbounceSpeed : AbstractExtendedVariant {
        public override object ConvertLegacyVariantValue(int value) {
            return value != 0;
        }

        public override object GetDefaultVariantValue() {
            return false;
        }

        public override Type GetVariantType() {
            return typeof(bool);
        }

        public override void Load() {
            IL.Celeste.Player.SuperWallJump += Player_SuperWallJump;
        }

        public override void Unload() {
            IL.Celeste.Player.SuperWallJump -= Player_SuperWallJump;
        }

        private void Player_SuperWallJump(ILContext il) {
            ILCursor cursor = new(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(-160f))) {
                Logger.Log(LogLevel.Error, "ExtendedVariantMode/PreserveWallbounceSpeed",
                    $"Could not find [ldc.r4 -160] in {il.Method.FullName}!");
                return;
            }

            Logger.Log(LogLevel.Error, "ExtendedVariantMode/PreserveWallbounceSpeed",
                $"Modifying wallbounce speed in {il.Method.FullName} @ {cursor.Instrs[cursor.Index]}");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(WallbounceSpeedModifier);
        }

        // if the variant is disabled, return the original speed (normally -160f)
        // else, return player.Speed.Y, with a min cap of -160f
        private float WallbounceSpeedModifier(float wallbounceSpeed, Player player) {
            return GetVariantValue<bool>(Variant.PreserveWallbounceSpeed)
                ? Math.Min(wallbounceSpeed, player.Speed.Y)
                : wallbounceSpeed;
        }
    }
}
