using ExtraLoadouts.Items;
using Terraria;
using Terraria.ModLoader;

namespace ExtraLoadouts.Patches {
    public sealed class UpdateItemDyePatch : BasePatch {
        public override void Patch(Mod mod) {
            On_Player.UpdateItemDye += On_Player_UpdateItemDye;
        }

        private void On_Player_UpdateItemDye(On_Player.orig_UpdateItemDye orig, Player self, bool isNotInVanitySlot, bool isSetToHidden, Item armorItem, Item dyeItem) {
            if (armorItem.ModItem is LoadoutVoodooDoll doll) {
                doll.DoAThingWithClonedItem(self,
                    itemToCopy => orig(self, isNotInVanitySlot, isSetToHidden, itemToCopy, dyeItem),
                    () => orig(self, isNotInVanitySlot, isSetToHidden, armorItem, dyeItem));
                return;
            }

            orig(self, isNotInVanitySlot, isSetToHidden, armorItem, dyeItem);
        }
    }
}
