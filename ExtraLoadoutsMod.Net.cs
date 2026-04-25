using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ExtraLoadouts;

public sealed partial class ExtraLoadoutsMod : Mod {
    public const byte PACKET_SYNC_ENTIRE_LOADOUT = 0;
    public const byte PACKET_SYNC_LOADOUT_SLOT = 1;
    public const byte PACKET_SYNC_LOADOUT_SELECTION = 2;
    public const byte PACKET_SYNC_LOADOUT_HIDE = 3;

    public override void HandlePacket(BinaryReader reader, int whoAmI) {
        switch (reader.ReadByte()) {
            case PACKET_SYNC_ENTIRE_LOADOUT:
                Receieve_SyncEntireLoadout(reader, whoAmI);
                break;
            case PACKET_SYNC_LOADOUT_SLOT:
                Recieve_SyncLoadoutSlot(reader, whoAmI);
                break;
            case PACKET_SYNC_LOADOUT_SELECTION:
                Receive_SyncLoadoutSelection(reader, whoAmI);
                break;
            case PACKET_SYNC_LOADOUT_HIDE:
                Receive_SyncLoadoutHide(reader, whoAmI);
                break;
        }
    }

    #region SyncEntireLoadout
    public static void Send_SyncEntireLoadout(int toWho, Player fromPlayer, int loadoutIndex) {
        Main.NewText($"Send_SyncEntireLoadout({toWho}, {fromPlayer}, {loadoutIndex})");
        ModPacket packet = ModContent.GetInstance<ExtraLoadoutsMod>().GetPacket();
        packet.Write(PACKET_SYNC_ENTIRE_LOADOUT);

        packet.Write7BitEncodedInt(fromPlayer.whoAmI);
        packet.Write7BitEncodedInt(loadoutIndex);
        fromPlayer.GetModPlayer<LoadoutPlayer>().ExtraLoadouts[loadoutIndex].Send(packet);

        packet.Send(toWho, fromPlayer.whoAmI);
    }

    public static void Receieve_SyncEntireLoadout(BinaryReader reader, int fromWho) {
        Main.NewText("Receive_SyncEntireLoadout()");

        int whosLoadout = reader.Read7BitEncodedInt();
        int loadout = reader.Read7BitEncodedInt();

        var player = Main.player[whosLoadout];

        player.GetModPlayer<LoadoutPlayer>().ExtraLoadouts[loadout].Receieve(reader);

        if (Main.netMode == NetmodeID.Server) {
            Send_SyncEntireLoadout(-1, player, loadout);
        }
    }
    #endregion

    #region SyncLoadoutSlot
    public static void Send_SyncLoadoutSlot(int toWho, Player fromPlayer, int extraLoadoutIndex, bool modded, bool dye, int slot) {
        Main.NewText($"Send_SyncLoadoutSlot({toWho}, {fromPlayer}, {extraLoadoutIndex}, {modded}, {dye}, {slot})");
        ModPacket packet = ModContent.GetInstance<ExtraLoadoutsMod>().GetPacket();
        packet.Write(PACKET_SYNC_LOADOUT_SLOT);

        packet.Write7BitEncodedInt(fromPlayer.whoAmI);
        packet.Write7BitEncodedInt(extraLoadoutIndex);
        packet.WriteFlags(modded, dye);
        packet.Write7BitEncodedInt(slot);

        Item item = SyncLoadoutSlot_GetItem(fromPlayer, extraLoadoutIndex, modded, dye, slot);

        ItemIO.Send(item, packet, writeStack: true);
        packet.Send(toWho, fromPlayer.whoAmI);
    }

    public static void Recieve_SyncLoadoutSlot(BinaryReader reader, int fromWho) {
        Main.NewText("Recieve_SyncLoadoutSlot()");

        int whosLoadout = reader.Read7BitEncodedInt();
        int extraLoadoutIndex = reader.Read7BitEncodedInt();
        reader.ReadFlags(out bool modded, out bool dye);
        int slot = reader.Read7BitEncodedInt();

        Item item = ItemIO.Receive(reader, readStack: true);

        var player = Main.player[whosLoadout];

        SyncLoadoutSlot_GetItem(player, extraLoadoutIndex, modded, dye, slot) = item;

        if (Main.netMode == NetmodeID.Server) {
            Send_SyncLoadoutSlot(-1, player, extraLoadoutIndex, modded, dye, slot);
        }
    }

    private static ref Item SyncLoadoutSlot_GetItem(Player player, int extraLoadoutIndex, bool modded, bool dye, int slot) {
        var modPlayer = player.GetModPlayer<LoadoutPlayer>().ExtraLoadouts[extraLoadoutIndex];

        if (!modded) {
            if (!dye) {
                return ref modPlayer.Vanilla.Armor[slot];
            } else {
                return ref modPlayer.Vanilla.Dye[slot];
            }
        } else {
            if (!dye) {
                return ref modPlayer.ModLoader.ExAccessorySlot[slot];
            } else {
                return ref modPlayer.ModLoader.ExDyesAccessory[slot];
            }
        }
    }
    #endregion

    #region SyncLoadoutSelection
    public static void Send_SyncLoadoutSelection(int toWho, Player ofPlayer) {
        Main.NewText($"Send_SyncLoadoutSlot({toWho}, {ofPlayer})");
        ModPacket packet = ModContent.GetInstance<ExtraLoadoutsMod>().GetPacket();
        packet.Write(PACKET_SYNC_LOADOUT_SELECTION);

        packet.Write7BitEncodedInt(ofPlayer.whoAmI);
        packet.Write7BitEncodedInt(ofPlayer.GetModPlayer<LoadoutPlayer>().CurrentExtraLoadoutIndex);

        packet.Send(toWho, ofPlayer.whoAmI);
    }

    public static void Receive_SyncLoadoutSelection(BinaryReader reader, int fromWho) {
        Main.NewText("Receive_SyncExLoadoutSelection");
        int whosLoadout = reader.Read7BitEncodedInt();
        int loadoutIndex = reader.Read7BitEncodedInt();

        var player = Main.player[whosLoadout];
        player.GetModPlayer<LoadoutPlayer>().CurrentExtraLoadoutIndex = loadoutIndex;

        if (Main.netMode == NetmodeID.Server) {
            Send_SyncLoadoutSelection(-1, player);
        }
    }

    #endregion

    #region SyncLoadoutHide
    public static void Send_SyncLoadoutHide(int toWho, Player fromPlayer, int loadoutIndex, bool modded, int slot) {
        ModPacket packet = ModContent.GetInstance<ExtraLoadoutsMod>().GetPacket();
        packet.Write(PACKET_SYNC_LOADOUT_HIDE);

        var modPlayer = fromPlayer.GetModPlayer<LoadoutPlayer>();

        packet.Write7BitEncodedInt(fromPlayer.whoAmI);
        packet.Write7BitEncodedInt(loadoutIndex);
        packet.Write(modded, SyncLoadoutHide_GetFlag(fromPlayer, loadoutIndex, modded, slot));
        packet.Write7BitEncodedInt(slot);

        packet.Send(toWho, ignoreClient: fromPlayer.whoAmI);
    }

    public static void Receive_SyncLoadoutHide(BinaryReader reader, int fromWho) {
        int whosLoadout = reader.Read7BitEncodedInt();
        int loadoutIndex = reader.Read7BitEncodedInt();
        reader.ReadFlags(out bool modded, out bool hide);
        int slot = reader.Read7BitEncodedInt();

        var player = Main.player[whosLoadout];
        var loadout = player.GetModPlayer<LoadoutPlayer>().ExtraLoadouts[loadoutIndex];

        SyncLoadoutHide_GetFlag(player, loadoutIndex, modded, slot) = hide;

        if (Main.netMode == NetmodeID.Server) {
            Send_SyncLoadoutHide(-1, player, loadoutIndex, modded, slot);
        }
    }

    private static ref bool SyncLoadoutHide_GetFlag(Player player, int loadoutIndex, bool modded, int slot) {
        var modPlayer = player.GetModPlayer<LoadoutPlayer>().ExtraLoadouts[loadoutIndex];

        if (!modded) {
            return ref modPlayer.Vanilla.Hide[slot];
        } else {
            return ref modPlayer.ModLoader.ExHideAccessory[slot];
        }
    }
    #endregion
}
