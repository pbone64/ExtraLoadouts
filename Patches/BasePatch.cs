using log4net;
using Terraria.ModLoader;

namespace ExtraLoadouts.Patches {
    public abstract class BasePatch : ILoadable {
        public Mod Mod { get; private set; }

        protected ILog Logger => Mod.Logger;
        
        public abstract void Patch(Mod mod);
        public virtual void Unpatch() { }

        void ILoadable.Load(Mod mod) {
            Mod = mod;
            Patch(mod);
        }
        
        void ILoadable.Unload() { Unpatch(); }
    }
}
