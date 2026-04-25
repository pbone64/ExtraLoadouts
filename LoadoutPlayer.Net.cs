using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;

namespace ExtraLoadouts;

public sealed partial class LoadoutPlayer : ModPlayer {
    public override void OnEquipmentLoadoutSwitched(int oldLoadoutIndex, int loadoutIndex) {
        if (Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.SinglePlayer) {
            CopyClientState(Main.clientPlayer.GetModPlayer<LoadoutPlayer>());
        }
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
        ExtraLoadoutsMod.Send_SyncLoadoutSelection(toWho, ofPlayer: Player);

        for (int i = 0; i < ExtraLoadouts.Length; i++) {
            ExtraLoadoutsMod.Send_SyncEntireLoadout(toWho, Player, i);
        }
    }

    public override void CopyClientState(ModPlayer targetCopy) {
        LoadoutPlayer copy = (LoadoutPlayer)targetCopy;

        copy.CurrentExtraLoadoutIndex = CurrentExtraLoadoutIndex;

        for (int i = 0; i < ExtraLoadouts.Length; i++) {
            var loadout = ExtraLoadouts[i];
            var copyLoadout = copy.ExtraLoadouts[i];

            // Copy vanilla slots
            for (int j = 0; j < loadout.Vanilla.Armor.Length; j++) {
                loadout.Vanilla.Armor[j].CopyNetStateTo(copyLoadout.Vanilla.Armor[j]);
            }

            for (int j = 0; j < loadout.Vanilla.Dye.Length; j++) {
                loadout.Vanilla.Dye[j].CopyNetStateTo(copyLoadout.Vanilla.Dye[j]);
            }

            for (int j = 0; j < loadout.Vanilla.Hide.Length; j++) {
                copyLoadout.Vanilla.Hide[j] = loadout.Vanilla.Hide[j];
            }

            // Copy modded slots
            var modAccSlotP = Player.GetModPlayer<ModAccessorySlotPlayer>();
            var slotCount = modAccSlotP.SlotCount;
            var loadedSlotCount = modAccSlotP.LoadedSlotCount;

            // We have to loop up to LoadedSlotCount instead of looping over every slot in the array (see LoadedSlotCount docs)
            for (int j = 0; j < loadedSlotCount; j++) {
                loadout.ModLoader.ExAccessorySlot[j].CopyNetStateTo(copyLoadout.ModLoader.ExAccessorySlot[j]);
                loadout.ModLoader.ExAccessorySlot[j + slotCount].CopyNetStateTo(copyLoadout.ModLoader.ExAccessorySlot[j + loadedSlotCount]);
                loadout.ModLoader.ExDyesAccessory[j].CopyNetStateTo(copyLoadout.ModLoader.ExDyesAccessory[j]);
            }
        }
    }

    public override void SendClientChanges(ModPlayer clientPlayer) {
        LoadoutPlayer client = (LoadoutPlayer)clientPlayer;

        if (client.CurrentExtraLoadoutIndex != CurrentExtraLoadoutIndex) {
            ExtraLoadoutsMod.Send_SyncLoadoutSelection(-1, Player);
        }

        for (int i = 0; i < ExtraLoadouts.Length; i++) {
            var loadout = ExtraLoadouts[i];
            var clientLoadout = client.ExtraLoadouts[i];

            bool syncedAnything = false;
            ItemUtils.TrySyncingItemArray(ref syncedAnything, loadout.Vanilla.Armor, clientLoadout.Vanilla.Armor, slot => ExtraLoadoutsMod.Send_SyncLoadoutSlot(-1, Player, i, modded: false, dye: false, slot));
            ItemUtils.TrySyncingItemArray(ref syncedAnything, loadout.Vanilla.Dye, clientLoadout.Vanilla.Dye, slot => ExtraLoadoutsMod.Send_SyncLoadoutSlot(-1, Player, i, modded: false, dye: true, slot));
            for (int j = 0; j < loadout.Vanilla.Hide.Length; j++) {
                if (loadout.Vanilla.Hide[j] != clientLoadout.Vanilla.Hide[j]) {
                    ExtraLoadoutsMod.Send_SyncLoadoutHide(-1, Player, i, false, j);
                }
            }

            ItemUtils.TrySyncingItemArray(ref syncedAnything, loadout.ModLoader.ExAccessorySlot, clientLoadout.ModLoader.ExAccessorySlot, slot => ExtraLoadoutsMod.Send_SyncLoadoutSlot(-1, Player, i, modded: true, dye: false, slot));
            ItemUtils.TrySyncingItemArray(ref syncedAnything, loadout.ModLoader.ExDyesAccessory, clientLoadout.ModLoader.ExDyesAccessory, slot => ExtraLoadoutsMod.Send_SyncLoadoutSlot(-1, Player, i, modded: true, dye: true, slot));
            for (int j = 0; j < loadout.ModLoader.ExHideAccessory.Length; j++) {
                if (loadout.ModLoader.ExHideAccessory[j] != clientLoadout.ModLoader.ExHideAccessory[j]) {
                    ExtraLoadoutsMod.Send_SyncLoadoutHide(-1, Player, i, true, j);
                }
            }

            // TODO is this needed?
            if (syncedAnything) {
                NetMessage.SendData(MessageID.ClientSyncedInventory);
            }
        }
    }
}
