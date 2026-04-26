using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;
using static Terraria.ModLoader.Default.ModAccessorySlotPlayer;

namespace ExtraLoadouts;

public class ExtraEquipmentLoadout {
    public EquipmentLoadout Vanilla;
    public ExEquipmentLoadout ModLoader;

    public ExtraEquipmentLoadout(Player forPlayer) {
        Vanilla = new();
        ModLoader = new(-1, forPlayer.GetModPlayer<ModAccessorySlotPlayer>().SlotCount, Vanilla); // ExEquipmentLoadout.LoadoutIndex is only used by tML's syncing code for the loadouts they handle, so we dummy it out to -1
        // Also note that changing LoadoutIndex is breaking as it is included in the key for modded loadout data
    }

    public ExtraEquipmentLoadout(Player forPlayer, EquipmentLoadout vanilla) {
        Vanilla = vanilla;
        ModLoader = new(-1, forPlayer.GetModPlayer<ModAccessorySlotPlayer>().SlotCount, Vanilla);
        ModLoader.ResetAndSizeAccessoryArrays(forPlayer.GetModPlayer<ModAccessorySlotPlayer>().SlotCount);
    }

    public ExtraEquipmentLoadout(EquipmentLoadout vanilla, ExEquipmentLoadout modded) {
        Vanilla = vanilla;
        ModLoader = modded;
    }

    public void Swap(Player player) {
        Vanilla.Swap(player);
        ModLoader.Swap(player.GetModPlayer<ModAccessorySlotPlayer>());
    }

    public bool HasAnything() {
        return Vanilla.Armor.Any(item => !item.IsLikelyNone()) || Vanilla.Dye.Any(item => !item.IsLikelyNone())
            || ModLoader.ExAccessorySlot.Any(item => !item.IsLikelyNone()) || ModLoader.ExDyesAccessory.Any(item => !item.IsLikelyNone());
    }

    public void QuickSpawnOn(Player player, IEntitySource source) {
        static void QuickSpawnLoadout(Player player, IEntitySource source, Item[] slots, Item[] dye) {
            for (int i = 0; i < slots.Length; i++) {
                player.QuickSpawnItemDirect(source, slots[i]);
            }

            for (int i = 0; i < dye.Length; i++) {
                player.QuickSpawnItemDirect(source, dye[i]);
            }
        }

        QuickSpawnLoadout(player, source, Vanilla.Armor, Vanilla.Dye);
        QuickSpawnLoadout(player, source, ModLoader.ExAccessorySlot, ModLoader.ExDyesAccessory);
    }

    public void DropItems(Player player, IEntitySource source) {
        static void DropLoadout(Player player, IEntitySource source, Item[] slots, Item[] dye) {
            for (int i = 0; i < slots.Length; i++) {
                player.TryDroppingSingleItem(source, slots[i]);
            }

            for (int i = 0; i < dye.Length; i++) {
                player.TryDroppingSingleItem(source, dye[i]);
            }
        }

        DropLoadout(player, source, Vanilla.Armor, Vanilla.Dye);
        DropLoadout(player, source, ModLoader.ExAccessorySlot, ModLoader.ExDyesAccessory);
    }

    public void Send(BinaryWriter writer) {
        static void SendLoadout(BinaryWriter writer, int count, Item[] slots, Item[] dye, bool[] hide) {
            writer.Write7BitEncodedInt(count);

            // an incredibly stupid hack: if count is negative, we are syncing the Vanilla array which has three less hide entries
            var hideCountAdjustment = count < 0 ? -3 : 0;
            count = int.Abs(count);

            for (int i = 0; i < count * 2; i++) {
                ItemIO.Send(slots[i], writer, writeStack: true);
            }

            for (int i = 0; i < count; i++) {
                ItemIO.Send(dye[i], writer, writeStack: true);
            }

            for (int i = 0; i < count + hideCountAdjustment; i++) {
                writer.Write(hide[i]);
            }
        }

        SendLoadout(writer, -Vanilla.Dye.Length, Vanilla.Armor, Vanilla.Dye, Vanilla.Hide);


        SendLoadout(writer, LoaderManager.Get<AccessorySlotLoader>().TotalCount, ModLoader.ExAccessorySlot, ModLoader.ExDyesAccessory, ModLoader.ExHideAccessory);
    }

    public void Receieve(BinaryReader reader) {
        static void ReceiveLoadout(BinaryReader reader, Item[] armor, Item[] dye, bool[] hide) {
            int count = reader.Read7BitEncodedInt();

            var hideCountAdjustment = count < 0 ? -3 : 0;
            count = int.Abs(count);

            for (int i = 0; i < count * 2; i++) {
                armor[i] = ItemIO.Receive(reader, readStack: true);
            }

            for (int i = 0; i < count; i++) {
                dye[i] = ItemIO.Receive(reader, readStack: true);
            }

            for (int i = 0; i < count + hideCountAdjustment; i++) {
                hide[i] = reader.ReadBoolean();
            }
        }

        ReceiveLoadout(reader, Vanilla.Armor, Vanilla.Dye, Vanilla.Hide);
        ReceiveLoadout(reader, ModLoader.ExAccessorySlot, ModLoader.ExDyesAccessory, ModLoader.ExHideAccessory);
    }

    public static class IO {
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

            foreach ((var fullName, var index) in modLoaderPlayer.slots) {
                var accessory = value.ModLoader.ExAccessorySlot[index];
                var social = value.ModLoader.ExAccessorySlot[index + modLoaderPlayer.SlotCount];
                var dye = value.ModLoader.ExDyesAccessory[index];
                var hide = value.ModLoader.ExHideAccessory[index];

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
            var vanillaLoadout = Deserialize_1_Vanilla(data.Get<TagCompound>(VanillaLoadoutTagKey));
            var (modLoaderLoadout, leftoverItems) = Deserialize_1_ModLoader(data.Get<TagCompound>(ModLoaderLoadoutTagKey), forPlayer, vanillaLoadout);

            return (new(vanillaLoadout, modLoaderLoadout), leftoverItems);
        }

        private static EquipmentLoadout Deserialize_1_Vanilla(TagCompound vanillaTag) {
            var armor = vanillaTag.GetList<TagCompound>(VanillaArmorKey).Select(ItemIO.Load).ToArray();
            var dye = vanillaTag.GetList<TagCompound>(VanillaDyeKey).Select(ItemIO.Load).ToArray();
            var hide = vanillaTag.GetList<bool>(VanillaHideKey).ToArray();

            // There is no reason for the saved arrays to be bigger than the vanilla equipment loadout arrays, so we directly copy them over
            var loadout = new EquipmentLoadout();
            Array.Copy(armor, loadout.Armor, loadout.Armor.Length);
            Array.Copy(dye, loadout.Dye, loadout.Dye.Length);
            Array.Copy(hide, loadout.Hide, loadout.Hide.Length);

            return loadout;
        }

        private static (ExEquipmentLoadout loadout, IList<Item> leftoverItems) Deserialize_1_ModLoader(TagCompound modLoaderTag, Player forPlayer, EquipmentLoadout vanillaLoadout) {
            var modPlayer = forPlayer.GetModPlayer<ModAccessorySlotPlayer>();
            var loadout = new ExEquipmentLoadout(-1, modPlayer.SlotCount, vanillaLoadout);

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

            return (loadout, leftoverItems);
        }
    }
}
