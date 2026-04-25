using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using static Terraria.ModLoader.BackupIO;

namespace ExtraLoadouts;

// TODO DropItems
public sealed partial class LoadoutPlayer : ModPlayer {
    public ExtraEquipmentLoadout CurrentExLoadout => ExtraLoadouts[CurrentExtraLoadoutIndex];

    public ExtraEquipmentLoadout[] ExtraLoadouts { get; } = new ExtraEquipmentLoadout[ExtraLoadoutsMod.EXTRA_LOADOUTS];
    public int CurrentExtraLoadoutIndex { get; set; } = -1;

    private List<ExtraEquipmentLoadout> OverflowExtraLoadouts { get; set; } = new();

    private int DidWeLoadData { get; set; } = -1;

    public override void Initialize() {
        DidWeLoadData = -1;
        for (int i = 0; i < ExtraLoadoutsMod.EXTRA_LOADOUTS; i++) {
            ExtraLoadouts[i] = new ExtraEquipmentLoadout(Player);
        }

        CurrentExtraLoadoutIndex = -1;
    }

    public override void OnEnterWorld() {
        Main.NewText("DidWeLoadData: " + DidWeLoadData);

        foreach (ExtraEquipmentLoadout overflowLoadout in OverflowExtraLoadouts) {
            overflowLoadout.DropOn(Player);
        }

        OverflowExtraLoadouts.Clear();
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        for (int i = 0; i < ExtraLoadoutsMod.EXTRA_LOADOUTS; i++) {
            if (ModContent.GetInstance<LoadoutKeybinds>().ExLoadoutKeybinds[i].JustPressed) {
                if (i < ModContent.GetInstance<LoadoutsConfig>().ExtraLoadouts) {
                    TrySwitchToExLoadout(i);
                }
            }
        }
    }

    public bool TrySwitchToExLoadout(int exLoadoutIndex) {
        if (!IsExLoadoutIndexValid(exLoadoutIndex)) {
            return false;
        }

        if (CurrentExtraLoadoutIndex < 0) {
            // We're on a vanilla layout currently
            TrySwitchingVanillaToEx(exLoadoutIndex);
        } else {
            // We're already on a modded layout
            TrySwitchingExToEx(exLoadoutIndex);
        }

        return true;
    }

    private void TrySwitchingVanillaToEx(int exLoadoutIndex) {
        if (IsPlayerReadyToSwitchLoadouts()) {
            Player.Loadouts[Player.CurrentLoadoutIndex].Swap(Player);

            ExtraLoadouts[exLoadoutIndex].Swap(Player);
            CurrentExtraLoadoutIndex = exLoadoutIndex;

            if (Player.whoAmI == Main.myPlayer) {
                Main.mouseLeftRelease = false;

                SwitchLoadoutFX();
            }
        }
    }

    private void TrySwitchingExToEx(int exLoadoutIndex) {
        if (IsPlayerReadyToSwitchLoadouts()) {
            ExtraLoadouts[CurrentExtraLoadoutIndex].Swap(Player);
            ExtraLoadouts[exLoadoutIndex].Swap(Player);
            CurrentExtraLoadoutIndex = exLoadoutIndex;

            if (Player.whoAmI == Main.myPlayer) {
                Main.mouseLeftRelease = false;

                SwitchLoadoutFX();
            }
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
