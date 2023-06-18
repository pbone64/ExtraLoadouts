using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace ExtraLoadouts {
    internal sealed class LoadoutPlayer : ModPlayer {
        internal EquipmentLoadout CurrentExLoadout => ExLoadouts[CurrentExLoadoutIndex];

        internal readonly EquipmentLoadout[] ExLoadouts = new EquipmentLoadout[ExtraLoadoutsMod.EXTRA_LOADOUTS] {
            new(),
            new(),
            new(),
        };

        internal int CurrentExLoadoutIndex { get; set; }

        public override void Initialize() {
            CurrentExLoadoutIndex = -1;
        }

        public override void SaveData(TagCompound tag) {
            if (CurrentExLoadoutIndex != -1) {
                tag.Add(nameof(CurrentExLoadoutIndex), CurrentExLoadoutIndex);
            }

            for (int i = 0; i < ExLoadouts.Length; i++) {
                tag.Add(nameof(ExLoadouts) + i, ExLoadouts[i]);
            }
        }

        public override void LoadData(TagCompound tag) {
            if (tag.TryGet(nameof(CurrentExLoadoutIndex), out int currentExLoadoutIndex)) {
                CurrentExLoadoutIndex = currentExLoadoutIndex;
            }
            
            for (int i = 0; i < ExLoadouts.Length; i++) {
                if (tag.TryGet(nameof(ExLoadouts) + i, out EquipmentLoadout loadout)) {
                    ExLoadouts[i] = loadout;
                }
            }
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
            for (int i = 0; i < ExtraLoadoutsMod.EXTRA_LOADOUTS; i++) {
                LoadoutSyncing.SyncExLoadout(Player, i, toWho, fromWho);
            }
        }

        internal void TrySwitchingVanillaToEx(int exLoadoutIndex) {
            if (IsPlayerReadyToSwitchLoadouts() && IsExLoadoutIndexValid(exLoadoutIndex)) {
                Player.Loadouts[Player.CurrentLoadoutIndex].Swap(Player);
                ExLoadouts[exLoadoutIndex].Swap(Player);
                CurrentExLoadoutIndex = exLoadoutIndex;

                if (Player.whoAmI == Main.myPlayer) {
                    Main.mouseLeftRelease = false;

                    // I'm not sure _what_ vanilla's CloneLoadOuts(Player) does and
                    // this (see code below) crashes because Main.clientPlayer doesn't have ModPlayers
                    //CloneExLoadouts(Main.clientPlayer);
                    //CloneLoadouts(Main.clientPlayer);

                    NetMessage.TrySendData(MessageID.SyncLoadout, -1, -1, null, Player.whoAmI, Player.CurrentLoadoutIndex);
                    LoadoutSyncing.SyncExLoadout(Player, exLoadoutIndex, -1, Player.whoAmI);

                    SoundEngine.PlaySound(SoundID.MenuTick);
                    ParticleOrchestrator.RequestParticleSpawn(clientOnly: false, ParticleOrchestraType.LoadoutChange, new ParticleOrchestraSettings {
                        PositionInWorld = Player.Center,
                        UniqueInfoPiece = 0
                    }, Player.whoAmI);

                    ItemSlot.RecordLoadoutChange();
                }
            }
        }

        internal void TrySwitchingExToEx(int exLoadoutIndex) {
            if (IsPlayerReadyToSwitchLoadouts() && IsExLoadoutIndexValid(exLoadoutIndex)) {
                ExLoadouts[CurrentExLoadoutIndex].Swap(Player);
                ExLoadouts[exLoadoutIndex].Swap(Player);
                CurrentExLoadoutIndex = exLoadoutIndex;

                if (Player.whoAmI == Main.myPlayer) {
                    Main.mouseLeftRelease = false;

                    // I'm not sure _what_ vanilla's CloneLayouts(Player) does and
                    // this (see code below) crashes because Main.clientPlayer doesn't have ModPlayers
                    //CloneExLoadouts(Main.clientPlayer);
                    LoadoutSyncing.SyncExLoadout(Player, exLoadoutIndex, -1, Player.whoAmI);

                    SoundEngine.PlaySound(SoundID.MenuTick);
                    ParticleOrchestrator.RequestParticleSpawn(clientOnly: false, ParticleOrchestraType.LoadoutChange, new ParticleOrchestraSettings {
                        PositionInWorld = Player.Center,
                        UniqueInfoPiece = 0
                    }, Player.whoAmI);

                    ItemSlot.RecordLoadoutChange();
                }
            }
        }

        private bool IsPlayerReadyToSwitchLoadouts() {
            return (Player.whoAmI != Main.myPlayer || (!IsUsingItem() && !Player.CCed && !Player.dead));
        }

        private bool IsExLoadoutIndexValid(int exLoadoutIndex) {
            return exLoadoutIndex != CurrentExLoadoutIndex && exLoadoutIndex >= 0 && exLoadoutIndex < ExLoadouts.Length;
        }

        private bool IsUsingItem() {
            return Player.itemTime > 0 || Player.itemAnimation > 0;
        }

        internal void ClearExForVanilla() {
            CurrentExLoadout.Swap(Player);
            CurrentExLoadoutIndex = -1;
        }

        /*private void CloneExLoadouts(Player player) {
            CloneItemArray(Player.armor, player.armor);
            CloneItemArray(Player.dye, player.dye);

            LoadoutPlayer modPlayer = player.GetModPlayer<LoadoutPlayer>();
            for (int i = 0; i < ExEquipmentLoadouts.Length; i++) {
                CloneItemArray(ExEquipmentLoadouts[i].Armor, modPlayer.ExEquipmentLoadouts[i].Armor);
                CloneItemArray(ExEquipmentLoadouts[i].Dye, modPlayer.ExEquipmentLoadouts[i].Dye);
            }
        }

        private void CloneItemArray(Item[] local, Item[] remote) {
            for (int i = 0; i < local.Length; i++) {
                remote[i] = local[i].Clone();
            }
        }*/
    }
}
