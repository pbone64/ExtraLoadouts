using Microsoft.Xna.Framework.Input;
using Terraria.ModLoader;

namespace ExtraLoadouts {
    public sealed class LoadoutKeybinds : ModSystem {
        public ModKeybind[] ExLoadoutKeybinds;

        public override void Load() {
            ExLoadoutKeybinds = new ModKeybind[ExtraLoadoutsMod.EXTRA_LOADOUTS];
            for (int i = 0; i < ExLoadoutKeybinds.Length; i++) {
                ExLoadoutKeybinds[i] = KeybindLoader.RegisterKeybind(Mod, "SwapToLoadout" + ExtraLoadoutsMod.LoadoutNumber(i, true), Keys.None);
            }
        }
    }
}
