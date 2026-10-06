using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace TravStickers.TravStickersCode;

[HarmonyPatch(typeof(NMapScreen), "OnClearMapDrawingButtonPressed")]
public static class MapScreenClearPatch
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("MapScreenClearPatch", LogType.Generic);

    [HarmonyPrefix]
    private static bool Prefix()
    {
        try
        {
            MapStickers.ClearMapStickers();
        }
        catch (System.Exception ex)
        {
            Logger.Warn($"Failed to clear map stickers: {ex.Message}");
        }
        return true;
    }
}