using ExtraLoadouts.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;
using static Terraria.ModLoader.Default.ModAccessorySlotPlayer;

namespace ExtraLoadouts;

public sealed partial class LoadoutPlayer : ModPlayer, IPostDeserializePlayer {
    public const string DataVersionKey = "version";
    public const int DataVersion = 1;
    public const string DataTagKey = "data";

    public override void SaveData(TagCompound tag) {
        IO_1.SaveData(tag, this);
    }

    public override void LoadData(TagCompound tag) {
        DidWeLoadData = true;

        // If DataVersion does not exist, then this save file was made before the versioned format was introduced
        if (!tag.TryGet(DataVersionKey, out int savedDataVersion)) {
            IO_Legacy.LoadData(tag, this);
            return;
        }

        tag.TryGet(DataTagKey, out TagCompound data);
        if (tag.Count > 0 && data == null) {
            Mod.Logger.Error($"While loading player data for \"{Player.name}\": tag contains {tag.Count} entries but no data entry (saved data version: {savedDataVersion} / current version: {DataVersion})");
            return;
        }

        switch (savedDataVersion) {
            case 1:
                IO_1.LoadData(data, this);
                break;
            default:
                Mod.Logger.Error($"While loading player data for \"{Player.name}\": unsupported version {savedDataVersion}");
                break;
        }
    }

    void IPostDeserializePlayer.PostDeserializePlayer() {
        // If we didn't load mod data, we have not yet had a chance to resize arrays based to the correct number of slots as loaded in ModAccessorySlotPlayer::LoadData
        if (!DidWeLoadData) {
            foreach (var loadout in ExtraLoadouts) {
                loadout.ResetAndSizeAccessoryArrays(Player);
            }
        }
    }

    private static class IO_1 {
        public const string CurrentLoadoutKey = "currentExtraLoadoutIndex";
        public const string ExtraLoadoutsKey = "extraLoadouts";

        public static void SaveData(TagCompound tag, LoadoutPlayer forPlayer) {
            tag.Add(DataVersionKey, DataVersion);

            TagCompound data = new();

            if (forPlayer.CurrentExtraLoadoutIndex != -1) {
                data.Add(CurrentLoadoutKey, forPlayer.CurrentExtraLoadoutIndex);
            }

            var loadoutsList = forPlayer.ExtraLoadouts.Select(loadout => LoadoutIO.Serialize(loadout, forPlayer.Player)).ToList();

            data.Add(ExtraLoadoutsKey, loadoutsList);

            tag.Add(DataTagKey, data);
        }

        public static void LoadData(TagCompound data, LoadoutPlayer forPlayer) {
            foreach (var loadout in forPlayer.ExtraLoadouts) {
                loadout.ResetAndSizeAccessoryArrays(forPlayer.Player);
            }

            if (data.TryGet(CurrentLoadoutKey, out int currentExLoadoutIndex)) {
                forPlayer.CurrentExtraLoadoutIndex = currentExLoadoutIndex;
            }

            var savedLoadouts = data.GetList<TagCompound>(ExtraLoadoutsKey).Select(tag => LoadoutIO.Deserialize(tag, forPlayer.Player)).ToArray();
            var expectedLoadouts = forPlayer.ExtraLoadouts;

            // less loadouts serialized than expected: this is a normal state and probably indicates that more loadouts have been added to the mod since this file was saved
            if (savedLoadouts.Length < expectedLoadouts.Length) {
                // copy over all that are saved
                Array.Copy(savedLoadouts, expectedLoadouts, savedLoadouts.Length);
            } else {
                // equal or more loadouts serialized than expected: copy over as many as we expect, and drop remaining items if there are "overflowing" serialized loadouts
                Array.Copy(savedLoadouts, expectedLoadouts, expectedLoadouts.Length);

                // Having more loadouts serialized than expected is a very weird case, but we handle it just in case so that players don't loose their items
                if (savedLoadouts.Length > forPlayer.ExtraLoadouts.Length) {
                    // remember overflow loadouts for later so that their items can be given back on world load
                    forPlayer.OverflowExtraLoadouts.AddRange(savedLoadouts[expectedLoadouts.Length..]);
                }
            }
        }

        public static class LoadoutIO {
            public const string SerializerVersionTagKey = "serializerVersion";
            public const int SerializerVersion = 1;

            public const string DataTagKey = "data";
            public const string VanillaLoadoutTagKey = "vanilla";
            public const string ModLoaderLoadoutTagKey = "modLoader";

            public const string VanillaArmorKey = "armor";
            public const string VanillaDyeKey = "dye";
            public const string VanillaHideKey = "hide";

            public const string ModLoaderSlotsListKey = "slots";
            public const string ModLoaderAccessoryKey = "accessory";
            public const string ModLoaderSocialKey = "social";
            public const string ModLoaderDyeKey = "dye";
            public const string ModLoaderHideKey = "hide";

            public static TagCompound Serialize(ExtraEquipmentLoadout value, Player forPlayer) {
                var vanillaTag = new TagCompound {
                    { VanillaArmorKey, value.Vanilla.Armor.Select(ItemIO.Save).ToList() },
                    { VanillaDyeKey, value.Vanilla.Dye.Select(ItemIO.Save).ToList() },
                    { VanillaHideKey, value.Vanilla.Hide.ToList() }
                };

                var modLoaderTag = new TagCompound();
                var slotsList = new TagCompound();

                var modLoaderPlayer = forPlayer.GetModPlayer<ModAccessorySlotPlayer>();

                // ModAccessorySlotPlayer::slots holds unloaded slot names and an index for them, but the items array does not necessarily contain them
                // Use the AccessorySlotLoader methods because they happen to work. idk, man
                var loader = LoaderManager.Get<AccessorySlotLoader>();

                // The default modloader slots [TODO]
                for (int i = 0; i < modLoaderPlayer.SlotCount; i++) {
                    var slot = loader.Get(i, forPlayer);
                    var unloaded = slot is UnloadedAccessorySlot;

                    // UnloadedAccessorySlots returned by AccessorySlotLoader::Get() have a placeholder name, so we look up their real full name from ModAccessorySlotPlayer::slots
                    var fullName = unloaded switch {
                        false => slot.FullName,
                        true => modLoaderPlayer.slots.First(s => s.Value == i).Key // This should always succeed
                    };

                    var accessory = value.ModLoader.ExAccessorySlot[i];
                    var social = value.ModLoader.ExAccessorySlot[i + modLoaderPlayer.SlotCount];
                    var dye = value.ModLoader.ExDyesAccessory[i];
                    var hide = value.ModLoader.ExHideAccessory[i];

                    // Don't bother saving if there's nothing
                    if (accessory.IsLikelyNone() && social.IsLikelyNone() && dye.IsLikelyNone() && !hide) {
                        continue;
                    }

                    slotsList.Add(fullName, new TagCompound {
                        { ModLoaderAccessoryKey, accessory },
                        { ModLoaderSocialKey, social },
                        { ModLoaderDyeKey, dye },
                        { ModLoaderHideKey, hide },
                    });
                }

                modLoaderTag.Add(ModLoaderSlotsListKey, slotsList);

                return new() {
                    { SerializerVersionTagKey, SerializerVersion },
                    { DataTagKey, new TagCompound() {
                        { VanillaLoadoutTagKey, vanillaTag },
                        { ModLoaderLoadoutTagKey, modLoaderTag }
                    } }
                };
            }

            public static ExtraEquipmentLoadout Deserialize(TagCompound tag, Player forPlayer) {
                int savedVersion = tag.GetInt(SerializerVersionTagKey);
                TagCompound data = tag.Get<TagCompound>(DataTagKey);

                if (data == null) {
                    return null;
                }

                switch (savedVersion) {
                    case 1:
                        return Deserialize_1(data, forPlayer).loadout;
                    default:
                        ModContent.GetInstance<ExtraLoadoutsMod>().Logger.Warn($"While deserialize ExEquipmentLoadout: unsupported version {savedVersion}");
                        return null;
                }
            }

            private static (ExtraEquipmentLoadout loadout, IList<Item> leftoverItems) Deserialize_1(TagCompound data, Player forPlayer) {
                var extraLoadout = new ExtraEquipmentLoadout(forPlayer);
                Deserialize_1_Vanilla(data.Get<TagCompound>(VanillaLoadoutTagKey), extraLoadout.Vanilla);
                var leftoverItems = Deserialize_1_ModLoader(data.Get<TagCompound>(ModLoaderLoadoutTagKey), forPlayer, extraLoadout.ModLoader);

                return (extraLoadout, leftoverItems);
            }

            private static void Deserialize_1_Vanilla(TagCompound vanillaTag, EquipmentLoadout loadout) {
                var armor = vanillaTag.GetList<TagCompound>(VanillaArmorKey).Select(ItemIO.Load).ToArray();
                var dye = vanillaTag.GetList<TagCompound>(VanillaDyeKey).Select(ItemIO.Load).ToArray();
                var hide = vanillaTag.GetList<bool>(VanillaHideKey).ToArray();

                // There is no reason for the saved arrays to be bigger than the vanilla equipment loadout arrays, so we directly copy them over
                Array.Copy(armor, loadout.Armor, loadout.Armor.Length);
                Array.Copy(dye, loadout.Dye, loadout.Dye.Length);
                Array.Copy(hide, loadout.Hide, loadout.Hide.Length);
            }

            private static IList<Item> Deserialize_1_ModLoader(TagCompound modLoaderTag, Player forPlayer, ExEquipmentLoadout loadout) {
                var modPlayer = forPlayer.GetModPlayer<ModAccessorySlotPlayer>();
                loadout.ResetAndSizeAccessoryArrays(modPlayer.SlotCount);

                var leftoverItems = new List<Item>();
                var slotsList = modLoaderTag.Get<TagCompound>(ModLoaderSlotsListKey);
                foreach ((var fullName, var tagObj) in slotsList) {
                    var tag = tagObj as TagCompound;

                    // These tags may be empty, but ItemIO.Load on an empty tag returns an empty item (perfect for us)
                    var accessory = ItemIO.Load(tag.Get<TagCompound>(ModLoaderAccessoryKey));
                    var social = ItemIO.Load(tag.Get<TagCompound>(ModLoaderSocialKey));
                    var dye = ItemIO.Load(tag.Get<TagCompound>(ModLoaderDyeKey));
                    var hide = tag.Get<bool>(ModLoaderHideKey);

                    if (ModContent.TryFind<ModAccessorySlot>(fullName, out var slot)) { // Try to look up loaded slots with ModContent instead of searching ModAccessorySlotPlayer.slots right away, to account for legacy names

                        loadout.ExAccessorySlot[slot.Type] = accessory;
                        loadout.ExAccessorySlot[slot.Type + modPlayer.SlotCount] = social;
                        loadout.ExDyesAccessory[slot.Type] = dye;
                        loadout.ExHideAccessory[slot.Type] = hide;
                    } else if (modPlayer.slots.TryGetValue(fullName, out var index)) { // Unloaded slots are tracked in ModAccessorySlotPlayer.slots, so we get their index from there
                        loadout.ExAccessorySlot[index] = accessory;
                        loadout.ExAccessorySlot[index + modPlayer.SlotCount] = social;
                        loadout.ExDyesAccessory[index] = dye;
                        loadout.ExHideAccessory[index] = hide;
                    } else {
                        // The slot is not currently loaded and the tML system that keeps track of unloaded slots with saved data doesn't recognize it,
                        // so we're screwed. We don't expect this to happen, but we handle this case anyways so that players don't loose their items
                        ModContent.GetInstance<ExtraLoadoutsMod>().Logger.Error($"While deserializing ExtraEquipmentLoadout: saved slot with full name \"{fullName}\" is neither loaded nor a known unloaded slot");
                        leftoverItems.AddRange([accessory, social, dye]);
                    }
                }

                return leftoverItems;
            }
        }
    }

    // Historic IO, very messy
    private static class IO_Legacy {
        public const string ExtraLoadoutsListKey = "ExLoadouts";
        public const string CurrentExtraLoadoutIndexKey = "CurrentExLoadoutIndex";

        [Obsolete("Historic SaveData methods are kept around for reference and should not be called")]
        public static void SaveData(TagCompound tag, LoadoutPlayer forPlayer) {
            if (forPlayer.CurrentExtraLoadoutIndex != -1) {
                tag.Add(CurrentExtraLoadoutIndexKey, forPlayer.CurrentExtraLoadoutIndex);
            }

            for (int i = 0; i < forPlayer.ExtraLoadouts.Length; i++) {
                tag.Add(ExtraLoadoutsListKey + i, forPlayer.ExtraLoadouts[i]);
            }
        }

        public static void LoadData(TagCompound data, LoadoutPlayer forPlayer) {
            if (data.TryGet(CurrentExtraLoadoutIndexKey, out int currentExLoadoutIndex)) {
                forPlayer.CurrentExtraLoadoutIndex = currentExLoadoutIndex;
            }

            for (int i = 0; i < forPlayer.ExtraLoadouts.Length; i++) {
                if (data.TryGet(ExtraLoadoutsListKey + i, out TagCompound loadoutTag)) {
                    forPlayer.ExtraLoadouts[i] = new(forPlayer.Player, EquipmentLoadoutSerializer.Deserialize(loadoutTag));
                }
            }

            foreach (var extraLoadout in forPlayer.ExtraLoadouts) {
                if (extraLoadout.ModLoader.ExAccessorySlot.Length <= 0) {
                    ModContent.GetInstance<ExtraLoadoutsMod>().Logger.Fatal("what the fuck");
                }
            }
        }

        private static class EquipmentLoadoutSerializer {
            public const string SerializerVersionKey = "SerializerVersion";
            public const int SerializerVersion = 2;

            public const string ArmorKey = "Armor";
            public const string DyeKey = "Dye";
            public const string HideKey = "Hide";

            public const string ArrayLengthKey = "Length";
            public const string ArrayItemTagPrefix = "Item";
            public const string ItemDataKey = "Item";


            public static TagCompound Serialize(EquipmentLoadout value) {
                TagCompound tag = new();

                TagCompound armorTag = new();
                SerializeItemArray(armorTag, value.Armor);
                tag.Add(ArmorKey, armorTag);

                TagCompound dyeTag = new();
                SerializeItemArray(dyeTag, value.Dye);
                tag.Add(DyeKey, dyeTag);

                tag.Add(HideKey, value.Hide);

                return tag;
            }

            private static void SerializeItemArray(TagCompound tag, Item[] items) {
                tag.Add(ArrayLengthKey, items.Length);
                for (int i = 0; i < items.Length; i++) {
                    tag.Add(ArrayItemTagPrefix + i, SerializeItem(items[i]));
                }
            }

            private static TagCompound SerializeItem(Item item) {
                return new() {
                    { ItemDataKey, ItemIO.Save(item) },
                    { SerializerVersionKey, SerializerVersion },
                };
            }

            public static EquipmentLoadout Deserialize(TagCompound tag) {
                EquipmentLoadout loadout = new();

                TagCompound armorTag = tag.Get<TagCompound>(ArmorKey);
                loadout.Armor = DeserializeItemArray(armorTag);

                TagCompound dyeTag = tag.Get<TagCompound>(DyeKey);
                loadout.Dye = DeserializeItemArray(dyeTag);

                bool[] hide = tag.Get<bool[]>(HideKey);
                loadout.Hide = hide;

                return loadout;
            }

            private static Item[] DeserializeItemArray(TagCompound tag) {
                int length = tag.GetInt(ArrayLengthKey);

                Item[] items = new Item[length];

                for (int i = 0; i < items.Length; i++) {
                    TagCompound itemTag = tag.Get<TagCompound>(ArrayItemTagPrefix + i);
                    int version = 1;
                    if (itemTag.ContainsKey(SerializerVersionKey)) {
                        version = itemTag.GetInt(SerializerVersionKey);
                    }

                    switch (version) {
                        case 1:
                            items[i] = DeserializeItem_1(itemTag);
                            break;
                        case 2:
                            items[i] = DeserializeItem_2(itemTag);
                            break;
                    }

                }

                return items;
            }

            public const string LegacyItemNetIDKey = "netID";
            public const string LegacyItemStackKey = "stack";
            public const string LegacyItemPrefixKey = "prefix";
            public const string LegacyItemModDataKey = "ModData";

            private static Item DeserializeItem_1(TagCompound tag) {
                int netId = tag.GetInt(LegacyItemNetIDKey);
                int stack = tag.GetInt(LegacyItemStackKey);
                int prefix = tag.GetInt(LegacyItemPrefixKey);

                Item item = new(netId, stack, prefix);

                if (tag.TryGet(LegacyItemModDataKey, out TagCompound modData)) {
                    item.ModItem?.LoadData(modData);
                }

                return item;
            }

            private static Item DeserializeItem_2(TagCompound tag) {
                return ItemIO.Load(tag.Get<TagCompound>(ItemDataKey));
            }
        }
    }
}
