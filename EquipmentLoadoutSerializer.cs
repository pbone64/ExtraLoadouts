using System;
using Terraria;
using Terraria.ModLoader.IO;

namespace ExtraLoadouts {
    public sealed class EquipmentLoadoutSerializer : TagSerializer<EquipmentLoadout, TagCompound> {
        public override TagCompound Serialize(EquipmentLoadout value) {
            TagCompound tag = new();

            TagCompound armorTag = new();
            SerializeItemArray(armorTag, value.Armor);
            tag.Add(nameof(EquipmentLoadout.Armor), armorTag);

            TagCompound dyeTag = new();
            SerializeItemArray(dyeTag, value.Dye);
            tag.Add(nameof(EquipmentLoadout.Dye), dyeTag);

            tag.Add(nameof(EquipmentLoadout.Hide), value.Hide);

            return tag;
        }

        private void SerializeItemArray(TagCompound tag, Item[] items) {
            tag.Add(nameof(Array.Length), items.Length);
            for (int i = 0; i < items.Length; i++) {
                TagCompound itemTag = new();
                SerializeItem(itemTag, items[i]);
                tag.Add(nameof(Item) + i, itemTag);
            }
        }

        private void SerializeItem(TagCompound tag, Item item) {
            tag.Add(nameof(Item.netID), item.netID);
            tag.Add(nameof(Item.stack), item.stack);
            tag.Add(nameof(Item.prefix), item.prefix);

            if (item.ModItem is not null) {
                TagCompound modData = new();
                item.ModItem.SaveData(modData);
                tag.Add("ModData", modData);
            }
        }

        private Item DeserializeItem(TagCompound tag) {
            int netId = tag.GetInt(nameof(Item.netID));
            int stack = tag.GetInt(nameof(Item.stack));
            int prefix = tag.GetInt(nameof(Item.prefix));

            Item item = new(netId, stack, prefix);

            if (tag.TryGet("ModData", out TagCompound modData)) {
                item.ModItem?.LoadData(modData);
            }

            return item;
        }

        private Item[] DeserializeItemArray(TagCompound tag) {
            int length = tag.GetInt(nameof(Array.Length));

            Item[] items = new Item[length];

            for (int i = 0; i < items.Length; i++) {
                items[i] = DeserializeItem(tag.Get<TagCompound>(nameof(Item) + i));
            }

            return items;
        }

        public override EquipmentLoadout Deserialize(TagCompound tag) {
            EquipmentLoadout loadout = new();

            TagCompound armorTag = tag.Get<TagCompound>(nameof(EquipmentLoadout.Armor));
            loadout.Armor = DeserializeItemArray(armorTag);

            TagCompound dyeTag = tag.Get<TagCompound>(nameof(EquipmentLoadout.Dye));
            loadout.Dye = DeserializeItemArray(dyeTag);

            bool[] hide = tag.Get<bool[]>(nameof(EquipmentLoadout.Hide));
            loadout.Hide = hide;

            return loadout;
        }
    }
}
