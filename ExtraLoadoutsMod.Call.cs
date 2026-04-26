using System;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ModLoader;

namespace ExtraLoadouts;

public sealed partial class ExtraLoadoutsMod : Mod {
    private event Action<Player, bool, int, bool, int> PreSwapLoadoutCallbacks;
    private event Action<Player, bool, int, bool, int> PostSwapLoadoutCallbacks;

    public void InvokePreSwapLoadoutsCallback(Player player, bool oldLoadoutModded, int oldLoadoutIndex, bool newLoadoutModded, int newLoadoutIndex) {
        PreSwapLoadoutCallbacks?.Invoke(player, oldLoadoutModded, oldLoadoutIndex, newLoadoutModded, newLoadoutIndex);
    }

    public void InvokePostSwapLoadoutsCallback(Player player, bool oldLoadoutModded, int oldLoadoutIndex, bool newLoadoutModded, int newLoadoutIndex) {
        PostSwapLoadoutCallbacks?.Invoke(player, oldLoadoutModded, oldLoadoutIndex, newLoadoutModded, newLoadoutIndex);
    }

    public override object Call(params object[] args) {
        if (args[0] is not string method) {
            return "args[0] must be a string specifying the call";
        }

        switch (method) {
            case "AreWeCallYet.0": return true;

            case "CurrentExtraLoadoutIndex.0" when args[1] is Player player: return player.GetModPlayer<LoadoutPlayer>().CurrentExtraLoadoutIndex;

            case "TotalExtraLoadouts.0": return EXTRA_LOADOUTS;

            case "GetExtraLoadoutVanilla.0" when args[1] is Player player && args[2] is int index: return player.GetModPlayer<LoadoutPlayer>().ExtraLoadouts[index].Vanilla;
            case "GetExtraLoadoutModLoader.0" when args[1] is Player player && args[2] is int index: return player.GetModPlayer<LoadoutPlayer>().ExtraLoadouts[index].ModLoader;

            case "SwitchToExtraLoadout.0" when args[1] is Player player && args[2] is int index: player.GetModPlayer<LoadoutPlayer>().TrySwitchingExtraLoadout(index); break;

            case "AddPreSwapLoadoutCallback.0" when args[1] is Action<Player, bool, int, bool, int> callback: PreSwapLoadoutCallbacks += callback; break;
            case "AddPostSwapLoadoutCallback.0" when args[1] is Action<Player, bool, int, bool, int> callback: PostSwapLoadoutCallbacks += callback; break;

            default:
                var argTypeString = new StringBuilder();

                if (args.Length > 1) {
                    argTypeString.AppendJoin(',', args[1..].Select(obj => obj.GetType().Name));
                } else {
                    argTypeString.Append("void");
                }

                throw new InvalidOperationException($"Unknown method \"{method}({argTypeString})\"");
        }

        return true;
    }
}
