        using System;
        using System.Reflection;
        using Terraria;
        using Terraria.ModLoader;
        using Terraria.ModLoader.Default;

        // Replace with your mod's namespace
        namespace YourModName; // TODO CHANGE

        /// <summary>
        /// This file is intended for mods which wish to support Extra Equipment Loadouts. It provides alternatives to <see cref="Player.Loadouts"/>, <see cref="Player.CurrentLoadoutIndex"/>, and <see cref="Player.TrySwitchingLoadout(int)"/> that additionally call ExtraEquipmentLoadouts if it is loaded.
        /// To use it, copy it into your mods source file and replace the namespaces above.
        /// </summary>
        public static class LoadoutHelper {
            public const string ExtraEquipmentLoadoutsName = "ExtraLoadouts";
            public const int VANILLA_LOADOUTS = 3;

            private static bool TryGetExtraLoadouts(out Mod mod) {
                if (ModLoader.TryGetMod(ExtraEquipmentLoadoutsName, out mod)) {
                    // We only want to support versions of the mod that have Mod.Call support
                    object response = mod.Call("AreWeCallYet.0");

                    if (response != null && (bool)response) {
                        return true;
                    }
                }

                mod = null;
                return false;
            }

            /// <summary>
            /// Gets the total number of loadouts available to the player. This is <see cref="VANILLA_LOADOUTS"/> without Extra Equipment Loadouts loaded, or 9 if it is loaded (at the time of writing).
            /// </summary>
            /// <remarks>
            /// If Extra Equipment Loadouts is updated, the value returned may be greater than 9.
            /// </remarks>
            public static int TotalLoadouts() {
                if (TryGetExtraLoadouts(out var mod)) {
                    return VANILLA_LOADOUTS + (int)mod.Call("TotalExtraLoadouts.0");
                } else {
                    return VANILLA_LOADOUTS;
                }
            }

            /// <summary>
            /// Gets the 0-based index of the current loadout. Values >= <see cref="VANILLA_LOADOUTS"/> indicate an extra loadout.
            /// </summary>
            /// <remarks>
            /// As extra loadouts are stored in a separate array, do not directly index <see cref="Player.Loadouts"/> with the return value. Instead, use <see cref="GetLoadout"/>.
            /// </remarks>
            public static int CurrentLoadoutIndex(Player player) {
                if (TryGetExtraLoadouts(out var mod)) {
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
            /// <exception cref="IndexOutOfRangeException">If <paramref name="index"/> is greater than <see cref="TotalLoadouts"></see>.</exception>
            public static EquipmentLoadout GetLoadout(Player player, int index) {
                return index switch {
                    < VANILLA_LOADOUTS => player.Loadouts[index],
                    >= VANILLA_LOADOUTS when TryGetExtraLoadouts(out var mod) => (EquipmentLoadout)mod.Call("GetExtraLoadoutVanilla.0", player, index - VANILLA_LOADOUTS),
                    _ => throw new IndexOutOfRangeException($"Index {index} out of bounds {TotalLoadouts()}"),
                };
            }

            /// <summary>
            /// Swaps <paramref name="player"/> to the loadout of the specific <paramref name="index"/>.
            /// </summary>
            /// <exception cref="IndexOutOfRangeException">If <paramref name="index"/> is greater than <see cref="TotalLoadouts"></see>.</exception>
            public static void SwitchToLoadout(Player player, int index) {
                switch (index) {
                    case < VANILLA_LOADOUTS: player.TrySwitchingLoadout(index); break;
                    case >= VANILLA_LOADOUTS when TryGetExtraLoadouts(out var mod): mod.Call("TrySwitchingExtraLoadout.0", player, index - VANILLA_LOADOUTS); break;
                    default: throw new IndexOutOfRangeException($"Index {index} out of bounds {TotalLoadouts()}");
                }
            }

            /// <summary>
            /// This class provides a couple advanced methods for mods that interact deeply with the loadout system.
            /// </summary>
            public static class Advanced {
                /// <summary>
                /// See <see cref="RegisterPreSwapCallback(OnSwapCallback)"/> or <see cref="RegisterPostSwapCallback(OnSwapCallback)"/>.
                /// </summary>
                /// <param name="oldLoadoutIndex">The 0-based index of the loadout being swapped from; see <see cref="CurrentLoadoutIndex(Player)"/>"/> for details on using this index.</param>
                /// <param name="newLoadoutIndex">The 0-based index of the loadout being swapped to; see <see cref="CurrentLoadoutIndex(Player)"/>"/> for details on using this index.</param>
                public delegate void OnSwapCallback(Player player, int oldLoadoutIndex, int newLoadoutIndex);

                private static Action<Player, bool, int, bool, int> WrapOnSwapCallback(OnSwapCallback callback) {
                    return (player, oldLoadoutModded, oldLoadoutIndex, newLoadoutModded, newLoadoutIndex) => callback(
                        player,
                        oldLoadoutIndex + (oldLoadoutModded ? VANILLA_LOADOUTS : 0),
                        newLoadoutIndex + (newLoadoutModded ? VANILLA_LOADOUTS : 0)
                    );
                }

                /// <summary>
                /// <para>Registers a callback to be executed immediately before loadouts are swapped. This callback is only registered if Extra Equipment Loadouts is loaded; this function does nothing otherwise.</para>
                /// <para>To run code even when Extra Equipment Loadouts is <em>not</em> loaded, use <see cref="ModPlayer.OnEquipmentLoadoutSwitched(int, int)"/>. Note that that hook will not run when switching to or, in certain cases, from loadouts added by Extra Equipment Loadouts.</para>
                /// </summary>
                /// <seealso cref="OnSwapCallback"/>
                /// <remarks>
                /// Unlike <see cref="ModPlayer.OnEquipmentLoadoutSwitched(int, int)"/>, <paramref name="callback"/> is called before <see cref="Player.CurrentLoadoutIndex"/> is updated and before Vanilla runs multiplayer syncing code for this swap.
                /// This callback is a good spot to swap corollary values that are associated with the current loadout.
                /// </remarks>
                public static bool RegisterPreSwapCallback(OnSwapCallback callback) {
                    if (!TryGetExtraLoadouts(out var mod)) {
                        return false;
                    }

                    var wrappedCb = WrapOnSwapCallback(callback);
                    mod.Call("AddPreSwapLoadoutCallback.0", wrappedCb);

                    return true;
                }

                /// <summary>
                /// <para>Registers a callback to be executed immediately after loadouts are swapped. This callback is only registered if Extra Equipment Loadouts is loaded; this function does nothing otherwise.</para>
                /// <para>To run code even when Extra Equipment Loadouts is <em>not</em> loaded, use <see cref="ModPlayer.OnEquipmentLoadoutSwitched(int, int)"/>. Note that that hook will not run when switching to or, in certain cases, from loadouts added by Extra Equipment Loadouts.</para>
                /// </summary>
                /// <seealso cref="OnSwapCallback"/>
                /// <remarks>
                /// Unlike <see cref="ModPlayer.OnEquipmentLoadoutSwitched(int, int)"/>, <paramref name="callback"/> is called before <see cref="Player.CurrentLoadoutIndex"/> is updated and before Vanilla runs multiplayer syncing code for this swap.
                /// This callback is a good spot to swap corollary values that are associated with the current loadout.
                /// </remarks>
                public static bool RegisterPostSwapCallback(OnSwapCallback callback) {
                    if (!TryGetExtraLoadouts(out var mod)) {
                        return false;
                    }

                    var wrappedCb = WrapOnSwapCallback(callback);
                    mod.Call("AddPostSwapLoadoutCallback.0", wrappedCb);

                    return true;
                }

                /// <summary>
                /// Gets a view into the slots storing <see cref="ModAccessorySlot"/> items in the loadout <paramref name="index"/>.
                /// This will work even if Extra Equipment Loadouts is not loaded.
                /// </summary>
                /// <remarks>As tModLoader does not make the <see cref="ModAccessorySlotPlayer.ExEquipmentLoadout"/> class <see langword="public"/>, this returns a proxy type with references to relevant fields from the object.</remarks>
                /// <exception cref="IndexOutOfRangeException">if <paramref name="index"/> is greater than <see cref="TotalLoadouts"/></exception>
                public static IModLoaderEquipmentLoadoutView GetModLoaderLoadout(Player player, int index) {
                    return index switch {
                        < VANILLA_LOADOUTS
                            => ModLoaderEquipmentLoadoutViewImpl.OfModLoaderLoadout(ReflectDefaultModLoaderLoadout(player, index)),
                        >= VANILLA_LOADOUTS when TryGetExtraLoadouts(out var mod)
                            => ModLoaderEquipmentLoadoutViewImpl.OfModLoaderLoadout(mod.Call("GetExtraLoadoutModLoader.0", player, index - VANILLA_LOADOUTS)),
                        _
                            => throw new IndexOutOfRangeException($"Index {index} out of bounds {TotalLoadouts()}"),
                    };
                }

                private static readonly FieldInfo F_ModAccessorySlotPlayer_exLoadouts = typeof(ModAccessorySlotPlayer).GetField("exLoadouts", BindingFlags.NonPublic | BindingFlags.Instance);
                private static object ReflectDefaultModLoaderLoadout(Player player, int index) {
                    if (F_ModAccessorySlotPlayer_exLoadouts is null) {
                        throw new Exception("Could not find ModAccessorySlotPlayer::exLoadouts");
                    }

                    return ((object[])F_ModAccessorySlotPlayer_exLoadouts.GetValue(player.GetModPlayer<ModAccessorySlotPlayer>()))[index];
                }

                /// <summary>
                /// This is a "view" into a <see cref="ModAccessorySlotPlayer.ExEquipmentLoadout"/>, a type which is <see langword="internal"/> to tModLoader by default.
                /// It does not provide methods for swapping, but can be used to access the items in <see cref="ModAccessorySlot"/>s in other loadouts.
                /// </summary>
                public interface IModLoaderEquipmentLoadoutView {
                    Item[] Items { get; }
                    Item[] Dye { get; }
                    bool[] Hide { get; }
                }

                /// <summary>
                /// Internal note: we use a public interface and private implementation as I don't want this type to be constructable by users of LoadoutHelper;
                /// I want instances of it to only be obtainable via <see cref="GetModLoaderLoadout(Player, int)"/>
                /// </summary>
                private class ModLoaderEquipmentLoadoutViewImpl : IModLoaderEquipmentLoadoutView {
                    public Item[] Items { get; init; }
                    public Item[] Dye { get; init; }
                    public bool[] Hide { get; init; }

                    private static readonly Type T_ModAccessorySlotPlayer_ExEquipmentLoadout = typeof(ModAccessorySlotPlayer).GetNestedType("ExEquipmentLoadout", BindingFlags.NonPublic);
                    private static readonly PropertyInfo F_ModAccessorySlotPlayer_ExEquipmentLoadout_ExAccessorySlot = T_ModAccessorySlotPlayer_ExEquipmentLoadout?.GetProperty("ExAccessorySlot");
                    private static readonly PropertyInfo F_ModAccessorySlotPlayer_ExEquipmentLoadout_ExDyesAccessory = T_ModAccessorySlotPlayer_ExEquipmentLoadout?.GetProperty("ExDyesAccessory");
                    private static readonly PropertyInfo F_ModAccessorySlotPlayer_ExEquipmentLoadout_ExHideAccessory = T_ModAccessorySlotPlayer_ExEquipmentLoadout?.GetProperty("ExHideAccessory");
                    public static ModLoaderEquipmentLoadoutViewImpl OfModLoaderLoadout(object exEquipmentLoadout) {
                        if (T_ModAccessorySlotPlayer_ExEquipmentLoadout is null) {
                            throw new Exception("Could not find ModAccessorySlotPlayer::ExEquipmentLoadout");
                        }

                        if (F_ModAccessorySlotPlayer_ExEquipmentLoadout_ExAccessorySlot is null) {
                            throw new Exception("Could not find ModAccessorySlotPlayer::ExEquipmentLoadout::ExAccessorySlot");
                        }

                        if (F_ModAccessorySlotPlayer_ExEquipmentLoadout_ExDyesAccessory is null) {
                            throw new Exception("Could not find ModAccessorySlotPlayer::ExEquipmentLoadout::ExDyesAccessory");
                        }

                        if (F_ModAccessorySlotPlayer_ExEquipmentLoadout_ExHideAccessory is null) {
                            throw new Exception("Could not find ModAccessorySlotPlayer::ExEquipmentLoadout::ExHideAccessory");
                        }

                        return new() {
                            Items = (Item[])F_ModAccessorySlotPlayer_ExEquipmentLoadout_ExAccessorySlot.GetValue(exEquipmentLoadout),
                            Dye = (Item[])F_ModAccessorySlotPlayer_ExEquipmentLoadout_ExDyesAccessory.GetValue(exEquipmentLoadout),
                            Hide = (bool[])F_ModAccessorySlotPlayer_ExEquipmentLoadout_ExHideAccessory.GetValue(exEquipmentLoadout)
                        };
                    }
                }
            }
        }
