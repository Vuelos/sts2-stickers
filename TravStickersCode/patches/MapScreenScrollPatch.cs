using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace TravStickers.TravStickersCode;

[HarmonyPatch(typeof(NMapScreen), "ProcessScrollEvent")]
public static class MapScreenScrollPatch
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        // While scaling/placing a sticker on the map, don't scroll the map with the wheel.
        return !MapStickers.IsDraggingSticker;
    }
}