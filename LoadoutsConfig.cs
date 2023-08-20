using System;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ExtraLoadouts {
    internal sealed class LoadoutsConfig : ModConfig {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Slider]
        [Range(0, ExtraLoadoutsMod.EXTRA_LOADOUTS)]
        [DefaultValue(ExtraLoadoutsMod.EXTRA_LOADOUTS)]
        public int ExtraLoadouts;

        public enum DollsMaterial
        {
            FallenStar,
            GoldBar, // or platinum
            DemoniteBar, // or crimtane
            ShadowScale, // or tissue sample
            BeeWax,
            Bone,
            Souls,
            HallowedBar,
            ChlorophyteBar,
        }


        [DefaultValue(DollsMaterial.Bone)]
        [DrawTicks]
        [Slider]
        public DollsMaterial DollsSecondaryMaterial;
    }
}
