using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ExtraLoadouts {
    public sealed class EquipmentLoadoutSerializer : TagSerializer<EquipmentLoadout, TagCompound> {
        public const int SerializerVersion = 2;

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
                tag.Add(nameof(Item) + i, SerializeItem(items[i]));
            }
        }

        private TagCompound SerializeItem(Item item) {
            return new() {
                { nameof(Item), ItemIO.Save(item) },
                { nameof(SerializerVersion), SerializerVersion },
            };
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

        private Item[] DeserializeItemArray(TagCompound tag) {
            int length = tag.GetInt(nameof(Array.Length));

            Item[] items = new Item[length];

            for (int i = 0; i < items.Length; i++) {
                TagCompound itemTag = tag.Get<TagCompound>(nameof(Item) + 1);
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

        private Item DeserializeItem_1(TagCompound tag) {
            int netId = tag.GetInt(nameof(Item.netID));
            int stack = tag.GetInt(nameof(Item.stack));
            int prefix = tag.GetInt(nameof(Item.prefix));

            Item item = new(netId, stack, prefix);

            if (tag.TryGet("ModData", out TagCompound modData)) {
                item.ModItem?.LoadData(modData);
            }

            return item;
        }

        private Item DeserializeItem_2(TagCompound tag) {
            return ItemIO.Load(tag.Get<TagCompound>(nameof(Item)));
        }
    }
}
