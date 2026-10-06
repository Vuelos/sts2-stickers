using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using TravStickers.TravStickersCode;

namespace TravStickers.TravStickersCode;

[HarmonyPatch("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NSimpleCardSelectScreen, sts2", "ConnectSignalsAndInitGrid")]
public class SimpleCardSelectScreenInitPatch
{
    [HarmonyPostfix]
    public static void Postfix(object __instance)
    {
        if (__instance is not NSimpleCardSelectScreen screen) return;
        foreach (var child in screen.GetChildren())
        {
            if (child is not NCard card || card.Model == null) continue;
            var layer = NStickerLayer.StickerLayer[card];
            if (layer == null) continue;
            layer.Card_ID = card.Model.Id.ToString();
            layer.AddStickerChildren();
        }
    }
}
