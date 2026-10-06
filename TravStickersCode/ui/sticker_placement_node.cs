using Godot;
using System.Reflection;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using TravStickers.TravStickersCode;

public partial class sticker_placement_node : NClickableControl
{
    public StickerUi? parent;
    private StickerPreview? _preview;

    public override void _Ready()
    {
        ConnectSignals();
        _preview = new StickerPreview();
        AddChild(_preview);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        var cardsField = typeof(NClickableControl).GetField("_isHovered", BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance);
        bool isHovered = cardsField.GetValue(this) is bool b && b;
        var activeSticker = parent?._activeSticker;

        if (_preview == null) return;

        if (isHovered && activeSticker != null)
        {
            if (!_preview.Active || _preview.StickerID != activeSticker.StickerID)
            {
                _preview.Show(activeSticker.StickerID);
            }
        }
        else
        {
            if (_preview.Active)
            {
                _preview.Hide();
            }
        }
    }

    protected override void OnFocus()
    {
    }

    protected override void OnUnfocus()
    {
    }

    protected override void OnPress()
    {
        if (_preview == null || !_preview.Active) return;
        if (parent == null) return;

        var stats = _preview.CurrentStats();
        if (stats == null) return;

        // The preview follows the mouse; store the position in the card's local space for placement.
        stats.position = GetLocalMousePosition();
        parent.placeSticker(stats);
        _preview.Hide();
    }

    protected override void OnRelease()
    {
    }
}