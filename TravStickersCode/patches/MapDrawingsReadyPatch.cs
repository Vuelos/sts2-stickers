using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace TravStickers.TravStickersCode;

[HarmonyPatch(typeof(NMapDrawings), nameof(NMapDrawings._Ready))]
public static class MapDrawingsReadyPatch
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("MapDrawingsReadyPatch", LogType.Generic);

    [HarmonyPostfix]
    private static void Postfix(NMapDrawings __instance)
    {
        MapStickers.SetMapDrawingsInstance(__instance);
    }
}