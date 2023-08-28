using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.Utilities;

using static ExtraLoadouts.LoadoutsConfig;

namespace ExtraLoadouts.Items {
    [Autoload(false)]
    public sealed class LoadoutVoodooDoll : ModItem {
        public sealed class LoadoutVoodooDollLoader : ILoadable {
            void ILoadable.Load(Mod mod) {
                for (int i = 0; i < ExtraLoadoutsMod.VANILLA_LOADOUTS; i++) {
                    mod.AddContent(NewDoll(false, i));
                }

                for (int i = 0; i < ExtraLoadoutsMod.EXTRA_LOADOUTS; i++) {
                    mod.AddContent(NewDoll(true, i));
                }
            }

            public static LoadoutVoodooDoll NewDoll(bool extra, int index) {
                return new() {
                    Extra = extra,
                    Index = index
                };
            }

            void ILoadable.Unload() { }
        }

        private enum CanTakeEffectStatus {
            CanBeEquipped,
            CantCopyCurrentLoadout,
            CantCopyLoadoutDoll,
            AlreadyEquippedOnCurrentLoadout,
            ModItemCantBeEquipped,
            IsLikelyNone,
        }

        public bool Extra;
        public int Index;
        public Guid Guid;

        public int LoadoutNumber => (Extra ? 4 : 1) + Index;

        public override string Name => "LoadoutVoodooDoll" + LoadoutNumber;

        public override void SetStaticDefaults() {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults() {
            Item.width = 32;
            Item.height = 28;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(0);
            Item.accessory = true;
        }

        public override ModItem NewInstance(Item entity) {
            LoadoutVoodooDoll doll = base.NewInstance(entity) as LoadoutVoodooDoll;
            doll.Extra = Extra;
            doll.Index = Index;
            doll.Guid = Guid.NewGuid();
            return doll;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            Item itemToCopy = GetItemToCopy(player, out int slot);

            if (itemToCopy is null || CanTakeEffect(player, itemToCopy, slot) != CanTakeEffectStatus.CanBeEquipped) {
                return;
            }

            player.GrantPrefixBenefits(itemToCopy);
            player.GrantArmorBenefits(itemToCopy);
            player.ApplyEquipFunctional(itemToCopy, hideVisual);
        }

        public override bool? PrefixChance(int pre, UnifiedRandom rand) {
            // This should stop us from getting any prefixes
            return false;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded) {
            return !IsPlayerCurrentlyOnMyLoadout(player);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            int index = tooltips.FindIndex(line => line.Name == "Tooltip2");
            if (index != -1) {
                tooltips.Insert(index + 1, new TooltipLine(Mod, "CurrentlyCopying",
                    string.Format(
                        Language.GetTextValue("Mods.ExtraLoadouts.CommonItemTooltips.LoadoutVoodooDollCurrentlyCopying"),
                        GetCopiedItemText())));
            }
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            Item itemToCopy = GetItemToCopy(Main.LocalPlayer, out int slot);
            if (itemToCopy is null || CanTakeEffect(Main.LocalPlayer, itemToCopy, slot) != CanTakeEffectStatus.CanBeEquipped) {
                return;
            }

            ItemSlot.DrawItemIcon(itemToCopy, 10, spriteBatch, position, Main.inventoryScale, 32f, Color.White * (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 1.5f);
        }

        public override ModItem Clone(Item newEntity) {
            // Fun fact: Recipe stores an instance of it's output item and clones it!
            // To give each crafted doll a unique guid, we need to return a new
            // instance of the doll instead of relying on NewInstance running.
            // In practice this _probably shouldn't_ cause any issues because
            // Guid is the only field re-assigned in NewInstance and it
            // changing _shouldn't_ break anything. Enjoy the bandaid fix!!!!!
            return NewInstance(newEntity);
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe()
                .AddIngredient(ItemID.Silk, 3)
                .AddTile(TileID.DemonAltar);

            DollsMaterial material = ModContent.GetInstance<LoadoutsConfig>().DollsSecondaryMaterial;

            switch (material) {
                case DollsMaterial.FallenStar:
                    recipe.AddIngredient(ItemID.FallenStar, 2);
                    break;

                case DollsMaterial.BeeWax:
                    recipe.AddIngredient(ItemID.BeeWax, 2);
                    break;

                case DollsMaterial.Bone:
                    recipe.AddIngredient(ItemID.Bone, 2);
                    break;

                case DollsMaterial.HallowedBar:
                    recipe.AddIngredient(ItemID.HallowedBar, 2);
                    break;

                case DollsMaterial.ChlorophyteBar:
                    recipe.AddIngredient(ItemID.ChlorophyteBar, 2);
                    break;

                case DollsMaterial.AnyGoldBar:
                    recipe.AddRecipeGroup(ExtraLoadoutsRecipeGroups.AnyGoldBar, 2);
                    break;
                case DollsMaterial.AnyDemoniteBar:
                    recipe.AddRecipeGroup(ExtraLoadoutsRecipeGroups.AnyDemoniteBar, 2);
                    break;
                case DollsMaterial.AnyShadowScale:
                    recipe.AddRecipeGroup(ExtraLoadoutsRecipeGroups.AnyShadowScale, 2);
                    break;
                case DollsMaterial.AnySoul:
                    recipe.AddRecipeGroup(ExtraLoadoutsRecipeGroups.AnySoul, 2);
                    break;
            }

            if (Extra) {
                recipe.AddCondition(Language.GetText("Mods.ExtraLoadouts.RecipeConditions.ExtraLoadoutVoodooDoll" + (Index + 1)),
                    () => Index < ModContent.GetInstance<LoadoutsConfig>().ExtraLoadouts);
            }

            recipe.Register();
        }

        private string GetCopiedItemText() {
            string text = Language.GetTextValue("Mods.ExtraLoadouts.Misc.LoadoutVoodooDollCopyingNothing");

            Item itemToCopy = GetItemToCopy(Main.LocalPlayer, out int slot);
            if (itemToCopy is null || CanTakeEffect(Main.LocalPlayer, itemToCopy, slot) != CanTakeEffectStatus.CanBeEquipped) {
                return text;
            }

            text = $"{itemToCopy.AffixName()} [i:{itemToCopy.type}]";

            return text;
        }

        private CanTakeEffectStatus CanTakeEffect(Player player, Item itemToCopy, int slot) {
            return
                itemToCopy.IsLikelyNone() ? CanTakeEffectStatus.IsLikelyNone :
                IsPlayerCurrentlyOnMyLoadout(player) ? CanTakeEffectStatus.CantCopyCurrentLoadout :
                itemToCopy.ModItem is LoadoutVoodooDoll ? CanTakeEffectStatus.CantCopyLoadoutDoll :
                !ItemLoader.CanEquipAccessory(itemToCopy, slot, itemToCopy.ModItem is not null) ? CanTakeEffectStatus.ModItemCantBeEquipped :
                !player.armor.Any(item => item.type != itemToCopy.type) ? CanTakeEffectStatus.AlreadyEquippedOnCurrentLoadout :
                CanTakeEffectStatus.CanBeEquipped;
        }

        private Item GetItemToCopy(Player player, out int slot) {
            EquipmentLoadout targetLoadout = GetLoadoutToCopyFrom(player);
            slot = Array.FindIndex(player.armor, item => item.ModItem is LoadoutVoodooDoll doll && doll.Guid == Guid);

            if (slot != -1) {
                return targetLoadout.Armor[slot];
            }

            return null;
        }

        private EquipmentLoadout GetLoadoutToCopyFrom(Player player) {
            LoadoutPlayer modPlayer = player.GetModPlayer<LoadoutPlayer>();
            return (Extra ? modPlayer.ExLoadouts : player.Loadouts)[Index];
        }

        private bool IsPlayerCurrentlyOnMyLoadout(Player player) {
            return Extra ?
                player.GetModPlayer<LoadoutPlayer>().CurrentExLoadoutIndex == Index :
                player.GetModPlayer<LoadoutPlayer>().CurrentExLoadoutIndex == -1 && player.CurrentLoadoutIndex == Index;
        }
    }
}
