using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace TravStickers.TravStickersCode;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap))]
public static class MapScreenSetMapPatch
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("MapScreenSetMapPatch", LogType.Generic);

    [HarmonyPostfix]
    private static void Postfix(NMapScreen __instance)
    {
        try
        {
            MapStickers.ReloadMapStickers(__instance);
        }
        catch (System.Exception ex)
        {
            Logger.Warn($"Failed to reload stickers on map change: {ex.Message}");
        }
    }
}