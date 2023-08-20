using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExtraLoadouts {
    public static class LoadoutSyncing {
        public const byte SyncLoadoutId = 0;

        public static void SyncExLoadout(Player player, int exLoadoutIndex, int toPlayer, int fromPlayer) {
            if (Main.netMode == NetmodeID.SinglePlayer) {
                return;
            }

            ModPacket packet = ModContent.GetInstance<ExtraLoadoutsMod>().GetPacket();
            packet.Write(SyncLoadoutId);
            packet.Write((byte)player.whoAmI);
            packet.Write((byte)exLoadoutIndex);

            EquipmentLoadout exLoadout = player.GetModPlayer<LoadoutPlayer>().ExLoadouts[exLoadoutIndex];
            SendLoadoutItemArray(packet, exLoadout.Armor);
            SendLoadoutItemArray(packet, exLoadout.Dye);

            for (int i = 0; i < exLoadout.Hide.Length; i++) {
                packet.Write(exLoadout.Hide[i]);
            }

            packet.Send(toPlayer, fromPlayer);
        }

        private static void SendLoadoutItemArray(ModPacket packet, Item[] arr) {
            for (int i = 0; i < arr.Length; i++) {
                Item item = arr[i];
                SendLoadoutItem(packet, item, i);
            }
        }

        private static void SendLoadoutItem(ModPacket packet, Item item, int index) {
            if (item.IsLikelyNone()) {
                item.SetDefaults(0, true);
            }

            int stack = Math.Max(item.stack, 0);
            int netId = item.netID;

            packet.Write((byte)index);
            packet.Write((short)stack);
            packet.Write(netId);
            packet.Write((short)item.prefix);
        }

        private static void ReadLoadoutItem(BinaryReader reader, Item[] intoArr) {
            byte index = reader.ReadByte();
            short stack = reader.ReadInt16();
            int netId = reader.ReadInt32();
            short prefix = reader.ReadInt16();

            intoArr[index].SetDefaults(netId);
            intoArr[index].stack = stack;
            intoArr[index].Prefix(prefix);
        }

        private static void ReadLoadoutItemArray(BinaryReader reader, Item[] intoArr) {
            for (int i = 0; i < intoArr.Length; i++) {
                ReadLoadoutItem(reader, intoArr);
            }
        }

        public static void HandlePacket(BinaryReader reader) {
            byte id = reader.ReadByte();
            switch (id) {
                case SyncLoadoutId:
                    byte whoAmI = reader.ReadByte();
                    byte exLoadoutIndex = reader.ReadByte();

                    EquipmentLoadout exLoadout = Main.player[whoAmI].GetModPlayer<LoadoutPlayer>().ExLoadouts[exLoadoutIndex];
                    ReadLoadoutItemArray(reader, exLoadout.Armor);
                    ReadLoadoutItemArray(reader, exLoadout.Dye);

                    for (int i = 0; i < exLoadout.Hide.Length; i++) {
                        exLoadout.Hide[i] = reader.ReadBoolean();
                    }

                    break;
            }
        }
    }
}
