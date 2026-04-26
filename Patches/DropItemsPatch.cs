using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;

namespace ExtraLoadouts.Patches;

public sealed class DropItemsPatch : BasePatch {
    public override void Patch(Mod mod) {
        IL_Player.DropItems += IL_Player_DropItems;
    }

    private void IL_Player_DropItems(ILContext il) {
        var c = new ILCursor(il);

        if (!c.TryGotoNext(MoveType.Before, instr => instr.MatchCall<Player>(nameof(Player.DropItems_End)))) {
            throw new Exception("while patching Player::DropItems: could not match (call Player::Dropitems_End)");
        }

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate(LoadoutPlayer.DropItems);
    }
}
