using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExtraLoadouts {
    public static class LoadoutSyncing {
        public const byte SyncLoadoutId = 0;

        [Obsolete("")]
        public static void SyncExLoadout(Player player, int exLoadoutIndex, int remoteClient, int ignoreClient) {
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

            packet.Send(remoteClient, ignoreClient);
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

        private static void SyncExLoadout_Read(BinaryReader reader) {
            byte whoAmI = reader.ReadByte();
            byte exLoadoutIndex = reader.ReadByte();

            EquipmentLoadout exLoadout = Main.player[whoAmI].GetModPlayer<LoadoutPlayer>().ExLoadouts[exLoadoutIndex];
            ReadLoadoutItemArray(reader, exLoadout.Armor);
            ReadLoadoutItemArray(reader, exLoadout.Dye);

            for (int i = 0; i < exLoadout.Hide.Length; i++) {
                exLoadout.Hide[i] = reader.ReadBoolean();
            }
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

        public const byte SyncExLoadoutSelectionId = 1;

        public static void SyncExLoadoutSelection(Player player, int exLoadout, bool set, int remoteClient, int ignoreClient) {
            if (Main.netMode == NetmodeID.SinglePlayer) {
                return;
            }

            ModPacket packet = ModContent.GetInstance<ExtraLoadoutsMod>().GetPacket();
            packet.Write(SyncExLoadoutSelectionId);

            packet.Write((byte)player.whoAmI);
            packet.Write((sbyte)exLoadout);
            packet.Write(set);

            packet.Send(remoteClient, ignoreClient);
        }

        private static void SyncExLoadoutSelection_Read(BinaryReader reader) {
            byte whoAmI = reader.ReadByte();
            sbyte exLoadoutIndex = reader.ReadSByte();
            bool set = reader.ReadBoolean();

            Player player = Main.player[whoAmI];
            LoadoutPlayer modPlayer = player.GetModPlayer<LoadoutPlayer>();

            if (exLoadoutIndex >= 0 && set) {
                modPlayer.TrySwitchToExLoadout(exLoadoutIndex);
            }

            if (Main.netMode == NetmodeID.Server) {
                SyncExLoadoutSelection(player, modPlayer.CurrentExLoadoutIndex, set, -1, whoAmI);
            }
        }

        public static void HandlePacket(BinaryReader reader) {
            byte id = reader.ReadByte();
            switch (id) {
                case SyncLoadoutId:
                    SyncExLoadout_Read(reader);
                    break;

                case SyncExLoadoutSelectionId:
                    SyncExLoadoutSelection_Read(reader);
                    break;
            }
        }
    }
}
