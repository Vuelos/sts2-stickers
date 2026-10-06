using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using TravStickers.TravStickersCode;

namespace TravStickers.TravStickersCode;

[HarmonyPatch("MegaCrit.Sts2.Core.Nodes.Screens.NInspectCardScreen, sts2", "SetCard")]
public class InspectCardScreenSetCardPatch
{
    [HarmonyPostfix]
    public static void Postfix(object __instance)
    {
        if (__instance is not Godot.Node screen) return;
        var cardField = screen.Get("_card");
        if (cardField.VariantType != Variant.Type.Object) return;
        var card = cardField.AsGodotObject() as NCard;
        if (card == null || card.Model == null) return;
        var layer = NStickerLayer.StickerLayer[card];
        if (layer == null) return;
        layer.Card_ID = card.Model.Id.ToString();
        if (layer.GetParent() != card)
        {
            card.AddChild(layer);
        }
        layer.Visible = true;
        layer.AddStickerChildren();
    }
}
