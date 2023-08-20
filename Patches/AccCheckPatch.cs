using ExtraLoadouts.Items;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExtraLoadouts.Patches {
    public sealed class AccCheckPatch : BasePatch {
        public override void Patch(Mod mod) {
            IL_ItemSlot.AccCheck_ForLocalPlayer += IL_ItemSlot_AccCheck_ForLocalPlayer;
        }

        private void IL_ItemSlot_AccCheck_ForLocalPlayer(ILContext il) {
            ILCursor c = new(il);

            if (!c.TryGotoNext(MoveType.Before,
                opcode => opcode.MatchCallvirt<Item>("IsTheSameAs")
            )) {
                throw new Exception("Failed while patching AccCheck_ForLocalPlayer: couldn't match callvirt #1");
            }

            c.Remove();
            c.EmitDelegate<Func<Item, Item, bool>>((item0, item1) => {
                if (item0.ModItem is LoadoutVoodooDoll) {
                    return false;
                }

                // The same logic as Item.IsTheSameAs but not internal
                return item0.netID == item1.netID && item0.type == item1.type;
            });

            if (!c.TryGotoNext(MoveType.Before,
                opcode => opcode.MatchCallvirt<Item>("IsTheSameAs")
            )) {
                throw new Exception("Failed while patching AccCheck_ForLocalPlayer: couldn't match callvirt #2");
            }

            c.Remove();
            c.EmitDelegate<Func<Item, Item, bool>>((item0, item1) => {
                if (item0.ModItem is LoadoutVoodooDoll) {
                    return false;
                }

                // The same logic as Item.IsTheSameAs but not internal
                return item0.netID == item1.netID && item0.type == item1.type;
            });
        }
    }
}
