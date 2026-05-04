using ExtraLoadouts.Patches;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace ExtraLoadouts.Hooks;

public interface IPostDeserializePlayer {
    void PostDeserializePlayer();
}

public static class PostDeserializePlayerLoader {
    private static readonly HookList<ModPlayer> _hookList = PlayerLoader.AddModHook(HookList<ModPlayer>.Create(i => ((IPostDeserializePlayer)i).PostDeserializePlayer));

    public static void Invoke(Player player) {
        foreach (var modPlayer in _hookList.Enumerate(player)) {
            (modPlayer as IPostDeserializePlayer).PostDeserializePlayer();
        }
    }
}

public class PostDeserializePlayerPatch : BasePatch {
    public override void Patch(Mod mod) {
        On_Player.Deserialize_PlayerFileData_Player_BinaryReader_TagCompound_int_refBoolean += (orig, data, newPlayer, fileIO, tplrData, release, out gotToReadName) => {
            orig(data, newPlayer, fileIO, tplrData, release, out gotToReadName);
            PostDeserializePlayerLoader.Invoke(newPlayer);
        };

        On_Player.Deserialize_PlayerFileData_Player_BinaryReader_ByteArray_int_refBoolean += (orig, data, newPlayer, fileIO, tplrData, release, out gotToReadName) => {
            orig(data, newPlayer, fileIO, tplrData, release, out gotToReadName);
            PostDeserializePlayerLoader.Invoke(newPlayer);
        };
    }
}
