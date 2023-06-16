using System;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ExtraLoadouts {
    internal sealed class LoadoutsConfig : ModConfig {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Slider]
        [Range(0, ExtraLoadoutsMod.MAX_EXTRA_LOADOUTS)]
        [DefaultValue(ExtraLoadoutsMod.MAX_EXTRA_LOADOUTS)]
        public int ExtraLoadouts;
    }
}
