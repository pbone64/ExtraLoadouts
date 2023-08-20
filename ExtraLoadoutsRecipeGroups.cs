using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace ExtraLoadouts {
    public class ExtraLoadoutsRecipeGroups : ModSystem {
        public const string AnyGoldBar = "ExtraLoadouts:AnyGoldBar";
        public const string AnyDemoniteBar = "ExtraLoadouts:AnyDemoniteBar";
        public const string AnyShadowScale = "ExtraLoadouts:AnyShadowScale";
        public const string AnySoul = "ExtraLoadouts:AnySoul";

        public override void AddRecipeGroups() {
            RecipeGroup anyGoldBarGroup = new(() => Language.GetTextValue("Mods.ExtraLoadouts.RecipeGroup.AnyGoldBar"), ItemID.GoldBar, ItemID.PlatinumBar);
            RecipeGroup.RegisterGroup(AnyGoldBar, anyGoldBarGroup);

            RecipeGroup anyDemoniteBarGroup = new(() => Language.GetTextValue("Mods.ExtraLoadouts.RecipeGroup.AnyDemoniteBar"), ItemID.DemoniteBar, ItemID.CrimtaneBar);
            RecipeGroup.RegisterGroup(AnyDemoniteBar, anyDemoniteBarGroup);

            RecipeGroup anyShadowScaleGroup = new(() => Language.GetTextValue("Mods.ExtraLoadouts.RecipeGroup.AnyShadowScale"), ItemID.ShadowScale, ItemID.TissueSample);
            RecipeGroup.RegisterGroup(AnyShadowScale, anyShadowScaleGroup);

            RecipeGroup anySoulGroup = new(() => Language.GetTextValue("Mods.ExtraLoadouts.RecipeGroup.AnySoul"), ItemID.SoulofNight, ItemID.SoulofLight, ItemID.SoulofFlight);
            RecipeGroup.RegisterGroup(AnySoul, anySoulGroup);
        }
    }
}