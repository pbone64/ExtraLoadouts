using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace ExtraLoadouts
{
    public class ExtraLoadoutsSystem : ModSystem
    {
        public override void AddRecipeGroups()
        {
            RecipeGroup anyGoldBarGroup = new RecipeGroup(() => Language.GetTextValue("Mods.ExtraLoadouts.RecipeGroup.AnyGoldBar"), ItemID.GoldBar, ItemID.PlatinumBar);
            RecipeGroup.RegisterGroup("ExtraLoadouts:AnyGoldBar", anyGoldBarGroup);

            RecipeGroup anyDemoniteBarGroup = new RecipeGroup(() => Language.GetTextValue("Mods.ExtraLoadouts.RecipeGroup.AnyDemoniteBar"), ItemID.DemoniteBar, ItemID.CrimtaneBar);
            RecipeGroup.RegisterGroup("ExtraLoadouts:AnyDemoniteBar", anyDemoniteBarGroup);

            RecipeGroup anyShadowScaleGroup = new RecipeGroup(() => Language.GetTextValue("Mods.ExtraLoadouts.RecipeGroup.AnyShadowScale"), ItemID.ShadowScale, ItemID.TissueSample);
            RecipeGroup.RegisterGroup("ExtraLoadouts:AnyShadowScale", anyShadowScaleGroup);

            RecipeGroup anySoulGroup = new RecipeGroup(() => Language.GetTextValue("Mods.ExtraLoadouts.RecipeGroup.AnySoul"), ItemID.SoulofNight, ItemID.SoulofLight, ItemID.SoulofFlight);
            RecipeGroup.RegisterGroup("ExtraLoadouts:AnySoul", anySoulGroup);
        }
    }
}