using ExtraLoadouts.Items;
using Terraria;
using Terraria.ModLoader;

namespace ExtraLoadouts.Patches {
    public sealed class UpdateVisibleAccessoryPatch : BasePatch {
        public override void Patch(Mod mod) {
            On_Player.ItemIsVisuallyIncompatible += On_Player_ItemIsVisuallyIncompatible;
            On_Player.UpdateVisibleAccessory += On_Player_UpdateVisibleAccessory;
        }

        private bool On_Player_ItemIsVisuallyIncompatible(On_Player.orig_ItemIsVisuallyIncompatible orig, Player self, Item item) {
            if (item.ModItem is LoadoutVoodooDoll doll) {
                return doll.DoAThingWithClonedItem(self,
                    itemToCopy => orig(self, itemToCopy),
                    () => orig(self, item)
                );
            }

            return orig(self, item);
        }

        private void On_Player_UpdateVisibleAccessory(On_Player.orig_UpdateVisibleAccessory orig, Player self, int itemSlot, Item item, bool modded) {
            if (item.ModItem is LoadoutVoodooDoll doll) {
                doll.DoAThingWithClonedItem(self,
                    itemToCopy => orig(self, itemSlot, itemToCopy, modded),
                    () => orig(self, itemSlot, item, modded)
                );
                return;
            }

            orig(self, itemSlot, item, modded);
        }
    }
}
