using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ExtraLoadouts.GUI {
    public class ExLoadoutButtons : ILoadable {
        public enum LoadoutStatus {
            Display,
            DisplayWithWarning,
            Hide
        }

        public const int LoadoutButtons_HoriFrames = 4;
        public const int LoadoutButtons_VertiFrames = ExtraLoadoutsMod.EXTRA_LOADOUTS;
        public static Asset<Texture2D> LoadoutButtonsTexture = null;

        void ILoadable.Load(Mod mod) {
            if (!Main.dedServ) {
                Main.QueueMainThreadAction(
                    () => LoadoutButtonsTexture = mod.Assets.Request<Texture2D>("GUI/ExLoadoutButtons")
                );
            }
        }

        public static void DrawLoadoutButtons(int inventoryTop, bool demonHeartSlotAvailable, bool masterModeSlotAvailable) {
            Player player = Main.LocalPlayer;
            LoadoutPlayer modPlayer = player.GetModPlayer<LoadoutPlayer>();

            int locationScale = 10
                - (demonHeartSlotAvailable ? 1 : 0)
                - (masterModeSlotAvailable ? 1 : 0);

            // Don't ask me about this math. It's shamfully ripped off from Vanilla.
            int x = Main.screenWidth - 58 + 14;
            int y = inventoryTop;
            int w = 4;
            int h = (int)((float)(inventoryTop - 2f) + (float)(locationScale * 56f) * Main.inventoryScale);
            Rectangle rectangle = new(x, y, w, h);

            int buttonHeight = 32;
            int buttonSpacing = 4;

            LoadoutStatus[] loadoutStatus = GetLoadoutStatuses();
            Rectangle[] buttonHitboxes = new Rectangle[ExtraLoadoutsMod.EXTRA_LOADOUTS];

            Rectangle warningFrame = LoadoutButtonsTexture.Frame(
                LoadoutButtons_HoriFrames, LoadoutButtons_VertiFrames,
                3, 0
            );

            for (int i = 0; i < ExtraLoadoutsMod.EXTRA_LOADOUTS; i++) {
                if (loadoutStatus[i] == LoadoutStatus.Hide) {
                    continue;
                }

                GetLoadoutFrames(i, out var inactiveFrame, out var activeFrame, out var outlineFrame);
                buttonHitboxes[i] =
                    new(
                        rectangle.X + rectangle.Width,
                        rectangle.Y + (buttonHeight + buttonSpacing) * (ExtraLoadoutsMod.VANILLA_LOADOUTS + i),
                        32,
                        32
                    );

                bool hovered = false;
                if (buttonHitboxes[i].Contains(Main.MouseScreen.ToPoint())) {
                    hovered = true;
                    player.mouseInterface = true;
                    
                    if (!Main.mouseText) {
                        string text = Language.GetTextValue("Mods.ExtraLoadouts.GUI.Loadout" + (ExtraLoadoutsMod.VANILLA_LOADOUTS + 1 + i));
                        if (loadoutStatus[i] == LoadoutStatus.DisplayWithWarning) {
                            text += "\n";
                            text += Language.GetTextValue("Mods.ExtraLoadouts.GUI.DisabledButOccupied");
                        }

                        Main.instance.MouseText(text);
                    }

                    if (Main.mouseLeft && Main.mouseLeftRelease) {
                        modPlayer.TrySwitchToExLoadout(i);
                    }
                }

                Vector2 pos = buttonHitboxes[i].Center.ToVector2();
                // All frames are the same size
                Vector2 origin = activeFrame.Size() / 2f;

                // Draw main button
                Main.spriteBatch.Draw(
                    LoadoutButtonsTexture.Value,
                    pos,
                    modPlayer.CurrentExLoadoutIndex == i ? activeFrame : inactiveFrame,
                    Color.White,
                    0f,
                    origin,
                    1f,
                    SpriteEffects.None,
                    0f
                );

                // Draw outline if hovered
                if (hovered) {
                    // Draw main button
                    Main.spriteBatch.Draw(
                        LoadoutButtonsTexture.Value,
                        pos,
                        outlineFrame,
                        Main.OurFavoriteColor,
                        0f,
                        origin,
                        1f,
                        SpriteEffects.None,
                        0f
                    );
                }

                // If the loadout is disabled in the config but still has items, draw an ! over it
                if (loadoutStatus[i] == LoadoutStatus.DisplayWithWarning) {
                    Main.spriteBatch.Draw(
                        LoadoutButtonsTexture.Value,
                        pos,
                        warningFrame,
                        Color.White,
                        0f,
                        origin,
                        1f,
                        SpriteEffects.None,
                        0f
                    );
                }
            }
        }

        public static LoadoutStatus[] GetLoadoutStatuses() {
            Player player = Main.LocalPlayer;
            LoadoutPlayer modPlayer = player.GetModPlayer<LoadoutPlayer>();

            LoadoutStatus[] statuses = new LoadoutStatus[ExtraLoadoutsMod.EXTRA_LOADOUTS];

            for (int i = 0; i < ExtraLoadoutsMod.EXTRA_LOADOUTS; i++) {
                EquipmentLoadout loadout = modPlayer.ExLoadouts[i];

                if (i < ModContent.GetInstance<LoadoutsConfig>().ExtraLoadouts) {
                    // It's enabled; all good!
                    statuses[i] = LoadoutStatus.Display;
                } else if (modPlayer.CurrentExLoadoutIndex == i) {
                    bool occupied = player.armor.Any(item => !item.IsLikelyNone()) || player.dye.Any(item => !item.IsLikelyNone());
                    statuses[i] = occupied ? LoadoutStatus.DisplayWithWarning : LoadoutStatus.Display;
                } else {
                    // If it contains items, keep it visible (with a warning) so people can empty it
                    bool occupied = loadout.Armor.Any(item => !item.IsLikelyNone()) || loadout.Dye.Any(item => !item.IsLikelyNone());
                    statuses[i] = occupied ? LoadoutStatus.DisplayWithWarning : LoadoutStatus.Hide;
                }
            }

            return statuses;
        }

        public static void GetLoadoutFrames(int loadout, out Rectangle inactive, out Rectangle active, out Rectangle outline) {
            inactive = LoadoutButtonsTexture.Frame(
                LoadoutButtons_HoriFrames, LoadoutButtons_VertiFrames,
                0, loadout
            );

            active = LoadoutButtonsTexture.Frame(
                LoadoutButtons_HoriFrames, LoadoutButtons_VertiFrames,
                1, loadout
            );

            outline = LoadoutButtonsTexture.Frame(
                LoadoutButtons_HoriFrames, LoadoutButtons_VertiFrames,
                2, loadout
            );
        }

        void ILoadable.Unload() {
            Main.QueueMainThreadAction(() => LoadoutButtonsTexture?.Dispose());
        }
    }
}
