using System;
using Terraria;
using Terraria.ModLoader;

// Replace this with your own mods namespace
namespace ExtraLoadouts;

/// <summary>
/// This file is intended for mods which wish to support Extra Equipment Loadouts. It provides alternatives to <see cref="Player.Loadouts"/>, <see cref="Player.CurrentLoadoutIndex"/>, and <see cref="Player.TrySwitchingLoadout(int)"/> that additionally call ExtraEquipmentLoadouts if it is loaded.
/// To use it, copy it into your mods source file and replace the namespaces above.
/// </summary>
public static class LoadoutHelper {
    public const string ExtraEquipmentLoadoutsName = "ExtraLoadouts";

    private const int VANILLA_LOADOUTS = 3;

    /// <summary>
    /// Gets the total number of loadouts available to the player. This is 3 without Extra Equipment Loadouts loaded, or 9 if it is loaded (at the time of writing).
    /// </summary>
    /// <remarks>
    /// If Extra Equipment Loadouts is updated, the value returned may be greater than 9.
    /// </remarks>
    public static int TotalLoadouts() {
        if (ModLoader.TryGetMod(ExtraEquipmentLoadoutsName, out var mod)) {
            return VANILLA_LOADOUTS + (int)mod.Call("TotalExtraLoadouts.0");
        } else {
            return VANILLA_LOADOUTS;
        }
    }

    /// <summary>
    /// Gets the 0-based index of the current loadout. Indices >= 3 indicate an extra loadout.
    /// </summary>
    /// <remarks>
    /// As extra loadouts are stored in a separate array, do not directly index <see cref="Player.Loadouts"/> with the return value. Instead, use <see cref="GetLoadout"/>.
    /// </remarks>
    public static int CurrentLoadoutIndex(Player player) {
        if (ModLoader.TryGetMod(ExtraEquipmentLoadoutsName, out var mod)) {
            var extraLoadoutIndex = (int)mod.Call("CurrentExtraLoadoutIndex.0", player);

            if (extraLoadoutIndex >= 0) {
                return extraLoadoutIndex + VANILLA_LOADOUTS;
            }
        }

        return player.CurrentLoadoutIndex;
    }

    /// <summary>
    /// Gets the <see cref="EquipmentLoadout"/> with the specified <paramref name="index"/>. 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="index"/> exceeds 3 when Extra Equipment Loadouts is not loaded, or <see cref="TotalLoadouts"/> when it is.</exception>
    public static EquipmentLoadout GetLoadout(Player player, int index) {
        return index switch {
            < VANILLA_LOADOUTS => player.Loadouts[index],
            >= VANILLA_LOADOUTS when ModLoader.TryGetMod(ExtraEquipmentLoadoutsName, out var mod) => (EquipmentLoadout)mod.Call("GetExtraLoadoutVanilla.0", player, index - VANILLA_LOADOUTS),
            _ => throw new IndexOutOfRangeException($"Index {index} out of bounds {TotalLoadouts()}"),
        };
    }

    /// <summary>
    /// Swaps <paramref name="player"/> to the loadout of the specific <paramref name="index"/>.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="index"/> exceeds 3 when Extra Equipment Loadouts is not loaded, or <see cref="TotalLoadouts"/> when it is.</exception>
    public static void SwitchToLoadout(Player player, int index) {
        switch (index) {
            case < VANILLA_LOADOUTS: player.TrySwitchingLoadout(index); break;
            case >= VANILLA_LOADOUTS when ModLoader.TryGetMod(ExtraEquipmentLoadoutsName, out var mod): mod.Call("TrySwitchingExtraLoadout.0", player, index - VANILLA_LOADOUTS); break;
            default: throw new IndexOutOfRangeException($"Index {index} out of bounds {TotalLoadouts()}");
        };
    }
}
