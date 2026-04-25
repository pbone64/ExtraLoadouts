using System;
using Terraria;
using Terraria.ID;

namespace ExtraLoadouts;

public static class ItemUtils {
    public static bool IsLikelyNone(this Item item) {
        return item is not null && item.Name == "" || item.stack == 0 || item.type == ItemID.None;
    }

    public static void TrySyncingItemArray(ref bool syncedAnything, Item[] my, Item[] your, Action<int> sync) {
        for (int i = 0; i < my.Length; i++) {
            if (my[i].IsNetStateDifferent(your[i])) {
                syncedAnything = true;
                sync(i);
            }
        }
    }
}
