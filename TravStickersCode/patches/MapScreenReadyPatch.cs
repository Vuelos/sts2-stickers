using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace TravStickers.TravStickersCode;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen._Ready))]
public static class MapScreenReadyPatch
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("MapScreenReadyPatch", LogType.Generic);

    [HarmonyPostfix]
    private static void Postfix(NMapScreen __instance)
    {
        try
        {
            MapStickers.AddStickerButton(__instance);
            MapStickers.InitMapStickers(__instance);
        }
        catch (System.Exception ex)
        {
            Logger.Warn($"Failed to add sticker button to map: {ex.Message}");
        }
    }
}