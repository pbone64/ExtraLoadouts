using System;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExtraLoadouts.Items;

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

    public enum CanTakeEffectStatus {
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
        Item.rare = ItemRarityID.Gray;
        Item.value = Item.sellPrice(0);
        Item.accessory = true;
    }
}
