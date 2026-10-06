using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace TravStickers.TravStickersCode;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen._Process))]
public static class MapScreenProcessPatch
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("MapScreenProcessPatch", LogType.Generic);

    [HarmonyPrefix]
    private static bool Prefix(NMapScreen __instance, double delta)
    {
        try
        {
            return MapStickers.ProcessMapStickerInput(__instance);
        }
        catch (System.Exception ex)
        {
            Logger.Warn($"Error in map sticker process: {ex.Message}");
        }
        return true;
    }
}