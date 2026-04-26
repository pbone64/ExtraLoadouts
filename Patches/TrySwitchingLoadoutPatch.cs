using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;

namespace ExtraLoadouts.Patches;

public sealed class TrySwitchingLoadoutPatch : BasePatch {
    public override void Patch(Mod mod) {
        IL_Player.TrySwitchingLoadout += IL_Player_TrySwitchingLoadout;
    }

    // This is a really lazy way of tracking these variables between two deleagetes, but I don't have a better idea
    private static bool WasOldLoadoutModded = false;
    private static int WasOldLoadoutIndex = -1;

    private void IL_Player_TrySwitchingLoadout(ILContext il) {
        ILCursor c = new(il);

        // Match the preliminary clause that checks whether we are trying to switch to the same loadout that is currently selected.
        // As we do not dummy-out the previous Player.CurrentLoadoutIndex value when we switch to an extra loadout, this is necessary
        //  to allow us to switch to an extra loadout from a Vanilla loadout and then switch back to the same Vanilla loadout.
        if (!c.TryGotoNext(MoveType.After,
                opcode => opcode.MatchLdarg(1)
        )) {
            throw new Exception("Failed while patching TrySwitchingLayout: could not match ldarg.1");
        }

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<int, Player, int>>((loadoutIndex, player) => {
            var currentExtraLoadoutIndex = player.GetModPlayer<LoadoutPlayer>().CurrentExtraLoadoutIndex;

            // If we are, before swapping, on a modded loadout...
            if (currentExtraLoadoutIndex >= 0) {
                WasOldLoadoutModded = true;
                WasOldLoadoutIndex = currentExtraLoadoutIndex;

                // return a dummy value to signal to the Vanilla code that we are indeed on a different loadout
                // This is necessary as we do not dummy-out Player.CurrentLoadoutIndex when we switch to an extra loadout;
                //  we leave it at whatever value it was before
                return 22;
            }

            // If Vanilla, return whatever we had before
            WasOldLoadoutModded = false;
            WasOldLoadoutIndex = player.CurrentLoadoutIndex;
            return loadoutIndex;
        });

        // Match the first swap (responsible for clearing the current loadout's items before the new loadout is swapped in)
        if (!c.TryGotoNext(MoveType.Before,
                opcode => opcode.MatchCallvirt<EquipmentLoadout>(nameof(EquipmentLoadout.Swap))
        )) {
            throw new Exception("Failed while patching TrySwitchingLayout: could not match (callvirt EquipmentLoadout.Swap)");
        }

        // First, emit a call to PreSwapLoadout callbacks
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_1);
        c.EmitDelegate<Action<Player, int>>((player, newLoadoutIndex) => {
            var modPlayer = player.GetModPlayer<LoadoutPlayer>();
            bool modded = modPlayer.CurrentExtraLoadoutIndex >= 0;
            var index = modded ? modPlayer.CurrentExtraLoadoutIndex : player.CurrentLoadoutIndex;

            ModContent.GetInstance<ExtraLoadoutsMod>().InvokePreSwapLoadoutsCallback(player, modded, index, false, newLoadoutIndex);
        });

        c.Remove();

        c.EmitDelegate<Action<EquipmentLoadout, Player>>((loadout, player) => {
            LoadoutPlayer modPlayer = player.GetModPlayer<LoadoutPlayer>();

            // If we're on a modded loadout (Modded -> Vanilla)...
            if (modPlayer.CurrentExtraLoadoutIndex >= 0) {
                // ...Swap out the current modded one
                modPlayer.ClearExForVanilla();
            } else {
                loadout.Swap(player);
            }
        });

        // Emit a call to PostSwapLoadout callbacks at the end of the if-block, after PlayerLoader.OnEquipmentLoadoutSwitched is called
        if (!c.TryGotoNext(MoveType.After,
                opcode => opcode.MatchCall(typeof(PlayerLoader), nameof(PlayerLoader.OnEquipmentLoadoutSwitched))
        )) {
            throw new Exception("Failed while patching TrySwitchingLayout: could not match (call PlayerLoader::OnEquipmentLoadoutSwitched)");
        }

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_1);
        c.EmitDelegate<Action<Player, int>>((player, newLoadoutIndex) => {
            ModContent.GetInstance<ExtraLoadoutsMod>().InvokePostSwapLoadoutsCallback(player, WasOldLoadoutModded, WasOldLoadoutIndex, false, newLoadoutIndex);
        });
    }
}
