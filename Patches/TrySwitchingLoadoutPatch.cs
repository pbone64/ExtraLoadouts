using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;

namespace ExtraLoadouts.Patches {
    public sealed class TrySwitchingLoadoutPatch : BasePatch {
        public override void Patch(Mod mod) {
            IL_Player.TrySwitchingLoadout += IL_Player_TrySwitchingLoadout;
        }

        private void IL_Player_TrySwitchingLoadout(ILContext il) {
            ILCursor c = new(il);

            // In essence, this section makes Terraria switch to it's current loadout
            // if a modded loadout is currently set. This is done because we keep the vanilla loadout index
            // set at whatever it was before being switched to a modded because vanilla doesn't have a defined number
            // for invalid loadouts
            // todo lol that comment sucks

            if (!c.TryGotoNext(MoveType.After,
                    opcode => opcode.MatchLdarg(1)
            )) {
                throw new Exception("Failed while patching TrySwitchingLayout: could not match ldarg.1");
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, Player, int>>((loadoutIndex, player) => {
                // If we're on a modded loadout...
                if (player.GetModPlayer<LoadoutPlayer>().CurrentExLoadoutIndex >= 0) {
                    // 22 will never equal the current loadout index,
                    // so the clause that checks whether we're trying to switch
                    // to the current loadout will never evaluate to true
                    return 22;
                }

                // Else return whatever we had before
                return loadoutIndex;
            });

            /* ---------------------------------------- */

            if (!c.TryGotoNext(MoveType.Before,
                    opcode => opcode.MatchCallvirt<EquipmentLoadout>(nameof(EquipmentLoadout.Swap))
            )) {
                throw new Exception("Failed while patching TrySwitchingLayout: could not match callvirt");
            }

            c.Remove();

            c.EmitDelegate<Action<EquipmentLoadout, Player>>((loadout, player) => {
                LoadoutPlayer modPlayer = player.GetModPlayer<LoadoutPlayer>();

                // If we're on a modded loadout (Modded -> Vanilla)...
                if (modPlayer.CurrentExLoadoutIndex >= 0) {
                    // ...Swap out the current modded one
                    modPlayer.ClearExForVanilla();
                } else {
                    loadout.Swap(player);
                }
            });
        }
    }
}
