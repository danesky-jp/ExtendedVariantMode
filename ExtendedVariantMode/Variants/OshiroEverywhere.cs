﻿using Celeste;
using Celeste.Mod;
using ExtendedVariants.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections;

namespace ExtendedVariants.Variants {
    public class OshiroEverywhere : AbstractExtendedVariant {
        public override int GetDefaultValue() {
            return 0;
        }

        public override int GetValue() {
            return Settings.OshiroEverywhere ? 1 : 0;
        }

        public override void SetValue(int value) {
            Settings.OshiroEverywhere = (value != 0);
        }

        public override void Load() {
            On.Celeste.Level.LoadLevel += modLoadLevel;
            On.Celeste.Level.TransitionRoutine += modTransitionRoutine;
            IL.Celeste.AngryOshiro.Update += modAngryOshiroUpdate;
        }

        public override void Unload() {
            On.Celeste.Level.LoadLevel -= modLoadLevel;
            On.Celeste.Level.TransitionRoutine -= modTransitionRoutine;
            IL.Celeste.AngryOshiro.Update -= modAngryOshiroUpdate;
        }
        
        private void modLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);

            if (playerIntro != Player.IntroTypes.Transition) {
                addOshiroToLevel(self);
            }
        }
        
        private IEnumerator modTransitionRoutine(On.Celeste.Level.orig_TransitionRoutine orig, Level self, LevelData next, Vector2 direction) {
            // just make sure the whole transition routine is over
            IEnumerator origEnum = orig(self, next, direction);
            while (origEnum.MoveNext()) {
                yield return origEnum.Current;
            }

            addOshiroToLevel(self);

            yield break;
        }

        private void addOshiroToLevel(Level level) {
            // if no Oshiro is present...
            if(Settings.OshiroEverywhere && level.Tracker.CountEntities<AngryOshiro>() == 0) {
                // this replicates the behavior of Oshiro Trigger in vanilla Celeste
                Vector2 position = new Vector2(level.Bounds.Left - 32, level.Bounds.Top + level.Bounds.Height / 2);
                level.Add(new AutoDestroyingAngryOshiro(position, false));

                level.Entities.UpdateLists();
            }
        }

        private void modAngryOshiroUpdate(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // we want to add ourselves in here: entity != null && !entity.Dead && base.CenterX < entity.CenterX + 4f
            // this is the condition used to apply slowdown
            // we use entity.Dead since this is the only time it is used in the method
            // (ps: I saw there was a StopControllingTime method, but it makes Oshiro stop controlling anxiety as well.)
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("get_Dead"))) {
                // we're pointing at a branch instruction that checks that our thing is false, since this is !entity.Dead. Store it and step past it.
                Instruction branchInstruction = cursor.Next;
                cursor.Index++;

                Logger.Log("ExtendedVariantsModule", $"Adding condition for time control at {cursor.Index} in CIL code for AngryOshiro.Update");

                // inject !isOshiroSlowdownDisabled
                cursor.EmitDelegate<Func<bool>>(isOshiroSlowdownDisabled);
                cursor.Emit(branchInstruction.OpCode, branchInstruction.Operand);
            }
        }

        private bool isOshiroSlowdownDisabled() {
            return Settings.DisableOshiroSlowdown;
        }
    }
}