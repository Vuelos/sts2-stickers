using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;

namespace TravStickers.TravStickersCode;

[HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen._Ready))]
public class InspectionScreenButtonPatch
{
    private static readonly string _scenePath = "res://" + MainFile.ModId + "/scenes/sticker_editor_button.tscn";
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("StickerEditorButton", LogType.Generic);

    private static OpenEditorButton? _stickerButton;
    private static NInspectCardScreen? _screen;
    
    [HarmonyPostfix]
    public static void Postfix(NInspectCardScreen __instance)
    {
        _screen = __instance;
        Logger.Info($"Inspect screen ready, adding sticker editor button. Screen={__instance}");
        OpenEditorButton overlay = PreloadManager.Cache.GetScene(_scenePath).Instantiate<OpenEditorButton>();
        __instance.AddChild(overlay);
        _stickerButton = overlay;
        _stickerButton.Connect(NClickableControl.SignalName.Released, Callable.From(new Action<NButton>(_ =>openStickerMenu())));
    }
    
    public static void openStickerMenu()
    {
        Logger.Info("openStickerMenu clicked");
        if (_screen == null)
        {
            Logger.Warn("openStickerMenu: _screen is null, cannot open editor.");
            return;
        }
        HideButton();
        var cardField = typeof(NInspectCardScreen).GetField("_card", BindingFlags.NonPublic | BindingFlags.Instance);
        var card = (NCard?)cardField?.GetValue(_screen);
        var model = card?.Model;
        if (model == null)
        {
            Logger.Warn("openStickerMenu: model is null, cannot open editor.");
            ShowButton();
            return;
        }
        var ui = StickerUi.OpenStickerMenu();
        ui.Open(model);
    }

    public static void HideButton()
    {
        if (_stickerButton != null)
        {
            _stickerButton.Visible = false;
        }
    }

    public static void ShowButton()
    {
        if (_stickerButton != null)
        {
            _stickerButton.Visible = true;
        }
    }
}
