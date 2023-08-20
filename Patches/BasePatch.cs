using Terraria.ModLoader;

namespace ExtraLoadouts.Patches {
    public abstract class BasePatch : ILoadable {
        public abstract void Patch(Mod mod);
        public virtual void Unpatch() { }

        void ILoadable.Load(Mod mod) { Patch(mod);  }
        void ILoadable.Unload() { Unpatch(); }
    }
}
