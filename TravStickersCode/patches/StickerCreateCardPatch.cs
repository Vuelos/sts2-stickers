using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace TravStickers.TravStickersCode;

[HarmonyPatch(typeof(NCard), nameof(NCard.Create))]
public class StickerCreateCardPatch
{
    [HarmonyPostfix]
    public static NCard? Postfix(NCard __result)
    {
        if (__result == null || __result.Model == null)
            return __result;
        var stickerLayer = NStickerLayer.StickerLayer[__result];
        if (stickerLayer == null) return __result;
        stickerLayer.Card_ID = __result.Model.Id.ToString();
        stickerLayer.AddStickerChildren();
        return __result;
    }
}
