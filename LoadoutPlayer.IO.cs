using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ExtraLoadouts;

public sealed partial class LoadoutPlayer : ModPlayer {
    public const string DataVersionKey = "version";
    public const int DataVersion = 1;
    public const string DataTagKey = "data";

    public override void SaveData(TagCompound tag) {
        IO_1.SaveData(tag, this);
    }

    public override void LoadData(TagCompound tag) {
        string name = Player.name;

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

    private static class IO_1 {
        private const string CurrentLoadoutKey = "currentExtraLoadoutIndex";
        private const string ExtraLoadoutsKey = "extraLoadouts";

        public static void SaveData(TagCompound tag, LoadoutPlayer forPlayer) {
            tag.Add(DataVersionKey, DataVersion);

            TagCompound data = new();

            if (forPlayer.CurrentExtraLoadoutIndex != -1) {
                data.Add(CurrentLoadoutKey, forPlayer.CurrentExtraLoadoutIndex);
            }

            var loadoutsList = forPlayer.ExtraLoadouts.Select(loadout => ExtraEquipmentLoadout.IO.Serialize(loadout, forPlayer.Player)).ToList();

            data.Add(ExtraLoadoutsKey, loadoutsList);

            tag.Add(DataTagKey, data);
        }

        public static void LoadData(TagCompound data, LoadoutPlayer forPlayer) {
            forPlayer.DidWeLoadData = 1;

            if (data.TryGet(CurrentLoadoutKey, out int currentExLoadoutIndex)) {
                forPlayer.CurrentExtraLoadoutIndex = currentExLoadoutIndex;
            }

            var savedLoadouts = data.GetList<TagCompound>(ExtraLoadoutsKey).Select(tag => ExtraEquipmentLoadout.IO.Deserialize(tag, forPlayer.Player)).ToArray();
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
            forPlayer.DidWeLoadData = 0;

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
            public const int SerializerVersion = 2;

            public static TagCompound Serialize(EquipmentLoadout value) {
                TagCompound tag = new();

                TagCompound armorTag = new();
                SerializeItemArray(armorTag, value.Armor);
                // TODO make me not nameof
                tag.Add(nameof(EquipmentLoadout.Armor), armorTag);

                TagCompound dyeTag = new();
                SerializeItemArray(dyeTag, value.Dye);
                tag.Add(nameof(EquipmentLoadout.Dye), dyeTag);

                tag.Add(nameof(EquipmentLoadout.Hide), value.Hide);

                return tag;
            }

            private static void SerializeItemArray(TagCompound tag, Item[] items) {
                tag.Add(nameof(Array.Length), items.Length);
                for (int i = 0; i < items.Length; i++) {
                    tag.Add(nameof(Item) + i, SerializeItem(items[i]));
                }
            }

            private static TagCompound SerializeItem(Item item) {
                return new() {
                    { nameof(Item), ItemIO.Save(item) },
                    { nameof(SerializerVersion), SerializerVersion },
                };
            }

            public static EquipmentLoadout Deserialize(TagCompound tag) {
                EquipmentLoadout loadout = new();

                TagCompound armorTag = tag.Get<TagCompound>(nameof(EquipmentLoadout.Armor));
                loadout.Armor = DeserializeItemArray(armorTag);

                TagCompound dyeTag = tag.Get<TagCompound>(nameof(EquipmentLoadout.Dye));
                loadout.Dye = DeserializeItemArray(dyeTag);

                bool[] hide = tag.Get<bool[]>(nameof(EquipmentLoadout.Hide));
                loadout.Hide = hide;

                return loadout;
            }

            private static Item[] DeserializeItemArray(TagCompound tag) {
                int length = tag.GetInt(nameof(Array.Length));

                Item[] items = new Item[length];

                for (int i = 0; i < items.Length; i++) {
                    TagCompound itemTag = tag.Get<TagCompound>(nameof(Item) + i);
                    int version = 1;
                    if (itemTag.ContainsKey(nameof(SerializerVersion))) {
                        version = itemTag.GetInt(nameof(SerializerVersion));
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

            private static Item DeserializeItem_1(TagCompound tag) {
                int netId = tag.GetInt(nameof(Item.netID));
                int stack = tag.GetInt(nameof(Item.stack));
                int prefix = tag.GetInt(nameof(Item.prefix));

                Item item = new(netId, stack, prefix);

                if (tag.TryGet("ModData", out TagCompound modData)) {
                    item.ModItem?.LoadData(modData);
                }

                return item;
            }

            private static Item DeserializeItem_2(TagCompound tag) {
                return ItemIO.Load(tag.Get<TagCompound>(nameof(Item)));
            }
        }
    }
}
