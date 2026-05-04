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

    public ExtraEquipmentLoadout(Player forPlayer) : this(forPlayer.GetModPlayer<ModAccessorySlotPlayer>().SlotCount) { }

    public ExtraEquipmentLoadout(int slotCount) {
        Vanilla = new();
        ModLoader = new(-1, slotCount, Vanilla); // ExEquipmentLoadout.LoadoutIndex is only used by tML's syncing code for the loadouts they handle, so we dummy it out to -1
                                                 // Also note that changing LoadoutIndex is breaking as it is included in the key for modded loadout data    }
    }

    public ExtraEquipmentLoadout(Player forPlayer, EquipmentLoadout vanilla) {
        Vanilla = vanilla;
        ModLoader = new(-1, forPlayer.GetModPlayer<ModAccessorySlotPlayer>().SlotCount, Vanilla);
    }

    public void ResetAndSizeAccessoryArrays(Player forPlayer) {
        ModLoader.ResetAndSizeAccessoryArrays(forPlayer.GetModPlayer<ModAccessorySlotPlayer>().SlotCount);
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
            // TODO: get rid of this incredibly stupid hack
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
}
