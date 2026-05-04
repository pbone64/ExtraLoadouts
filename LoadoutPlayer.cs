using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.UI;

namespace ExtraLoadouts;

public sealed partial class LoadoutPlayer : ModPlayer {
    public ExtraEquipmentLoadout CurrentExLoadout => ExtraLoadouts[CurrentExtraLoadoutIndex];

    public ExtraEquipmentLoadout[] ExtraLoadouts { get; } = new ExtraEquipmentLoadout[ExtraLoadoutsMod.EXTRA_LOADOUTS];
    public int CurrentExtraLoadoutIndex { get; set; } = -1;

    private List<ExtraEquipmentLoadout> OverflowExtraLoadouts { get; set; } = [];

    private bool DidWeLoadData { get; set; } = false;

    public LoadoutPlayer() {
        for (int i = 0; i < ExtraLoadouts.Length; i++) {
            ExtraLoadouts[i] = new(LoaderManager.Get<AccessorySlotLoader>().list.Count);
        }
    }

    public override void OnEnterWorld() {
        foreach (ExtraEquipmentLoadout overflowLoadout in OverflowExtraLoadouts) {
            overflowLoadout.QuickSpawnOn(Player, null);
        }

        OverflowExtraLoadouts.Clear();
    }

    public static void DropItems(Player player) {
        var modPlayer = player.GetModPlayer<LoadoutPlayer>();
        var entitySource = player.GetItemSource_Death();
        foreach (var loadout in modPlayer.ExtraLoadouts) {
            loadout.DropItems(player, entitySource);
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        for (int i = 0; i < ExtraLoadoutsMod.EXTRA_LOADOUTS; i++) {
            if (ModContent.GetInstance<LoadoutKeybinds>().ExLoadoutKeybinds[i].JustPressed) {
                if (i < ModContent.GetInstance<LoadoutsConfig>().ExtraLoadouts) {
                    TrySwitchingExtraLoadout(i);
                }
            }
        }
    }

    public void TrySwitchingExtraLoadout(int exLoadoutIndex) {
        if (!IsExLoadoutIndexValid(exLoadoutIndex)) {
            return;
        }

        if (CurrentExtraLoadoutIndex < 0) {
            // We're on a vanilla layout currently
            TrySwitchingVanillaToEx(exLoadoutIndex);
        } else {
            // We're already on a modded layout
            TrySwitchingExToEx(exLoadoutIndex);
        }

        // This is normally called in ModAccessorySlotPlayer::OnEquipmentLoadoutsSwitched(), which only runs when the Vanilla loadout index changes
        Player.GetModPlayer<ModAccessorySlotPlayer>().DetectConflictsWithSharedSlots();
    }

    private void TrySwitchingVanillaToEx(int exLoadoutIndex) {
        if (IsPlayerReadyToSwitchLoadouts()) {
            ModContent.GetInstance<ExtraLoadoutsMod>().InvokePreSwapLoadoutsCallback(Player, false, Player.CurrentLoadoutIndex, true, exLoadoutIndex);

            Player.Loadouts[Player.CurrentLoadoutIndex].Swap(Player);
            ExtraLoadouts[exLoadoutIndex].Swap(Player);

            CurrentExtraLoadoutIndex = exLoadoutIndex;

            if (Player.whoAmI == Main.myPlayer) {
                Main.mouseLeftRelease = false;

                SwitchLoadoutFX();
            }

            ModContent.GetInstance<ExtraLoadoutsMod>().InvokePostSwapLoadoutsCallback(Player, false, Player.CurrentLoadoutIndex, true, exLoadoutIndex);
        }
    }

    private void TrySwitchingExToEx(int exLoadoutIndex) {
        if (IsPlayerReadyToSwitchLoadouts()) {
            ModContent.GetInstance<ExtraLoadoutsMod>().InvokePreSwapLoadoutsCallback(Player, true, CurrentExtraLoadoutIndex, true, exLoadoutIndex);

            ExtraLoadouts[CurrentExtraLoadoutIndex].Swap(Player);
            ExtraLoadouts[exLoadoutIndex].Swap(Player);

            CurrentExtraLoadoutIndex = exLoadoutIndex;

            if (Player.whoAmI == Main.myPlayer) {
                Main.mouseLeftRelease = false;

                SwitchLoadoutFX();
            }

            ModContent.GetInstance<ExtraLoadoutsMod>().InvokePostSwapLoadoutsCallback(Player, true, CurrentExtraLoadoutIndex, true, exLoadoutIndex);
        }
    }

    private void SwitchLoadoutFX() {
        SoundEngine.PlaySound(SoundID.MenuTick);
        ParticleOrchestrator.RequestParticleSpawn(clientOnly: false, ParticleOrchestraType.LoadoutChange, new ParticleOrchestraSettings {
            PositionInWorld = Player.Center,
            UniqueInfoPiece = 0
        }, Player.whoAmI);

        ItemSlot.RecordLoadoutChange();
    }

    private bool IsPlayerReadyToSwitchLoadouts() {
        return Player.whoAmI != Main.myPlayer || (!IsUsingItem() && !Player.CCed && !Player.dead);
    }

    private bool IsExLoadoutIndexValid(int exLoadoutIndex) {
        return exLoadoutIndex != CurrentExtraLoadoutIndex && exLoadoutIndex >= 0 && exLoadoutIndex < ExtraLoadouts.Length;
    }

    private bool IsUsingItem() {
        return Player.itemTime > 0 || Player.itemAnimation > 0;
    }

    public void ClearExForVanilla() {
        CurrentExLoadout.Swap(Player);
        CurrentExtraLoadoutIndex = -1;
    }
}
