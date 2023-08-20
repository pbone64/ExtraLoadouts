using ExtraLoadouts.GUI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using MonoMod.Cil;
using System;
using Microsoft.Xna.Framework.Graphics;

namespace ExtraLoadouts.Patches {
    public sealed class DrawLoadoutButtonsPatch : BasePatch {
        public override void Patch(Mod mod) {
            On_Main.DrawLoadoutButtons += On_Main_DrawLoadoutButtons;
            IL_Main.DrawLoadoutButtons += IL_Main_DrawLoadoutButtons;
        }

        private void IL_Main_DrawLoadoutButtons(ILContext il) {
            ILCursor c = new(il);

            if (!c.TryGotoNext(MoveType.Before,
                    opcode => opcode.MatchCall(typeof(Utils), nameof(Utils.Frame))
            )) {
                throw new Exception("Failed while patching DrawLoadoutButtons: could not match call");
            }

            c.Remove();
            c.EmitDelegate<Func<Texture2D, int, int, int, int, int, int, Rectangle>>((tex, _, _, vanillaFrameX, i, _, _) => {
                Player player = Main.LocalPlayer;
                LoadoutPlayer modPlayer = player.GetModPlayer<LoadoutPlayer>();

                if (modPlayer.CurrentExLoadoutIndex < 0) {
                    // ExLoadoutIndex is negative: we're on a vanilla loadout
                    return tex.Frame(3, 3, vanillaFrameX, i);
                } else {
                    // We're on a modded loadout. Draw all vanilla buttons as unselected even if they technically aren't
                    return tex.Frame(3, 3, 0, i);
                }
            });

            /*if (!c.TryGotoNext(MoveType.Before,
                opcode => opcode.MatchCallvirt<Player>(nameof(Player.TrySwitchingLoadout))
            )) {
                throw new Exception("Failed while patching DrawLoadoutButtons: could not match callvirt");
            }*/
        }

        private void On_Main_DrawLoadoutButtons(On_Main.orig_DrawLoadoutButtons orig, int inventoryTop, bool demonHeartSlotAvailable, bool masterModeSlotAvailable) {
            orig(inventoryTop, demonHeartSlotAvailable, masterModeSlotAvailable);

            ExLoadoutButtons.DrawLoadoutButtons(inventoryTop, demonHeartSlotAvailable, masterModeSlotAvailable);
        }
    }
}
