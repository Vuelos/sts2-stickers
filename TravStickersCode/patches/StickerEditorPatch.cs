using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace TravStickers.TravStickersCode;

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public class StickerEditorPatch
{
    [HarmonyPostfix]
    public static void Postfix(NGame __instance)
    {
        __instance.AddChild(StickerUi.StickerEditorLayer[__instance]);
    }
}
