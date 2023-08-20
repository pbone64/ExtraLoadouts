using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ExtraLoadouts.Patches {
    public sealed class LoadoutColorPatch : BasePatch {
        public override void Patch(Mod mod) {
            On_ItemSlot.TryGetSlotColor += On_ItemSlot_TryGetSlotColor;
        }

        private bool On_ItemSlot_TryGetSlotColor(On_ItemSlot.orig_TryGetSlotColor orig, int loadoutIndex, int context, out Color color) {
            Player player = Main.LocalPlayer;
            LoadoutPlayer modPlayer = player.GetModPlayer<LoadoutPlayer>();

            if (modPlayer.CurrentExLoadoutIndex < 0) {
                // ExLoadoutIndex is negative: we're on a vanilla loadout
                return orig(loadoutIndex, context, out color);
            }

            color = default;

            int slotColorIndex = -1;
            switch (context) {
                case 8:
                case 10:
                    slotColorIndex = 0;
                    break;

                case 9:
                case 11:
                    slotColorIndex = 1;
                    break;

                case 12:
                    slotColorIndex = 2;
                    break;
            }

            if (slotColorIndex == -1) {
                return false;
            }

            color = ExtraLoadoutsMod.ExLoadoutColors[modPlayer.CurrentExLoadoutIndex, slotColorIndex];
            return true;
        }
    }
}
