using ExtraLoadouts.Items;
using MonoMod.Cil;
using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExtraLoadouts.Patches {
    // Necessary to allow multiple Loadout Voodoo Dolls to be equipped at once

    public sealed class AccCheckPatch : BasePatch {
        private delegate bool ItemSlot_AccCheck_ForPlayer(Player player, Item[] itemCollection, Item item, int slot);

        private ILHook hook;
        
        public override void Patch(Mod mod) {
            // As of writing this method, IL_ItemSlot has not been updated and does not contain an event for AccCheck_ForPlayer
            // I love tModLoader
            MethodInfo itemSlot_AccCheck_ForPlayer =
                typeof(ItemSlot).GetMethod("AccCheck_ForPlayer", BindingFlags.NonPublic | BindingFlags.Static);
            hook = new ILHook(itemSlot_AccCheck_ForPlayer, IL_ItemSlot_AccCheck_ForPlayer);
            // HookEndpointManager.Modify<ItemSlot_AccCheck_ForPlayer>(itemSlot_AccCheck_ForPlayer, IL_ItemSlot_AccCheck_ForPlayer);
        }

        private void IL_ItemSlot_AccCheck_ForPlayer(ILContext il) {
            foreach (var instr in il.Body.Instructions) {
                Logger.Info(instr);
            }

            ILCursor c = new(il);
            int patchCount = 0;
            MethodInfo itemIsTheSameAs = typeof(Item).GetMethod("IsTheSameAs", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo selfIsTheSameAsReplacement = typeof(AccCheckPatch).GetMethod("IsTheSameAsReplacement", BindingFlags.NonPublic | BindingFlags.Static);

            while (c.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt(typeof(Item), "IsTheSameAs"))) {
                c.Remove();
                c.EmitCall(selfIsTheSameAsReplacement);
                patchCount++;
            }
            

            if (patchCount == 0) {
                throw new Exception("Found no IsTheSameAs calls in ItemSlot.AccCheck_ForPlayer.");
            } else {
                Logger.Info("Patched {patchCount} occurrences of IsTheSameAs in ItemSlot.AccCheck_ForPlayer.");
            }

            // if (!c.TryGotoNext(MoveType.Before,
            //     opcode => opcode.MatchCallvirt<Item>("IsTheSameAs")
            // )) {
            //     throw new Exception("Failed while patching AccCheck_ForLocalPlayer: couldn't match callvirt #1");
            // }
            //
            // c.Remove();
            // c.EmitDelegate<Func<Item, Item, bool>>((item0, item1) => {
            //     if (item0.ModItem is LoadoutVoodooDoll) {
            //         return false;
            //     }
            //
            //     // The same logic as Item.IsTheSameAs but not internal
            //     return item0.netID == item1.netID && item0.type == item1.type;
            // });
            //
            // if (!c.TryGotoNext(MoveType.Before,
            //     opcode => opcode.MatchCallvirt<Item>("IsTheSameAs")
            // )) {
            //     throw new Exception("Failed while patching AccCheck_ForLocalPlayer: couldn't match callvirt #2");
            // }
            //
            // c.Remove();
            // c.EmitDelegate<Func<Item, Item, bool>>((item0, item1) => {
            //     if (item0.ModItem is LoadoutVoodooDoll) {
            //         return false;
            //     }
            //
            //     // The same logic as Item.IsTheSameAs but not internal
            //     return item0.netID == item1.netID && item0.type == item1.type;
            // });
        }

        private static bool IsTheSameAsReplacement(Item self, Item compareItem) {
                if (self.ModItem is LoadoutVoodooDoll) {
                    return false;
                }

                // The same logic as Item.IsTheSameAs but not internal
                return self.netID == compareItem.netID && self.type == compareItem.type;
            }
    }
}
