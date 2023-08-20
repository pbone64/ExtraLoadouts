using Terraria;
using Terraria.ID;

namespace ExtraLoadouts {
    public static class ItemUtils {
        public static bool IsLikelyNone(this Item item) {
            return item.Name == "" || item.stack == 0 || item.type == ItemID.None;
        }
    }
}
