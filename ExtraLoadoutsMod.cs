using System.IO;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace ExtraLoadouts {
    public sealed class ExtraLoadoutsMod : Mod {
        public const int VANILLA_LOADOUTS = 3;
        public const int EXTRA_LOADOUTS = 6;
        public static readonly Color[,] ExLoadoutColors = new Color[EXTRA_LOADOUTS, 3] {
            {
                new(130, 116, 63),
                new(122, 97, 59),
                new(117, 70, 53)
            },
            {
                new(108, 50, 137),
                new(70, 56, 107),
                new(77, 51, 104)
            },
            {
                new(127, 72, 125),
                new(102, 52, 104),
                new(78, 46, 96),
            },
            {
                new(170, 192, 179),
                new(109, 166, 160),
                new(46, 106, 110)
            },
            {
                new(69, 69, 161),
                new(46, 46, 102),
                new(34, 34, 77)
            },
            {
                new(190, 154, 235),
                new(178, 139, 208),
                new(172, 106, 190),
            }
        };

        public override void HandlePacket(BinaryReader reader, int whoAmI) {
            LoadoutSyncing.HandlePacket(reader);
        }

        public static int LoadoutNumber(int loadoutIndex, bool ex) {
            return loadoutIndex + 1 + (ex ? VANILLA_LOADOUTS : 0);
        }
    }
}