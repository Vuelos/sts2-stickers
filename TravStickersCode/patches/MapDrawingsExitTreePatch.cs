using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;

namespace TravStickers.TravStickersCode;

[HarmonyPatch(typeof(Node), nameof(Node._ExitTree))]
public static class MapDrawingsExitTreePatch
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("MapDrawingsExitTreePatch", LogType.Generic);

    [HarmonyPostfix]
    private static void Postfix(Node __instance)
    {
        if (MapStickers.GetMapDrawingsInstance() == __instance)
        {
            MapStickers.SetMapDrawingsInstance(null);
            MapStickers.SetMapStickersContainer(null);
        }
    }
}