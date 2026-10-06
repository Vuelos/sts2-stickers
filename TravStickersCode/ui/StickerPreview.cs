using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace TravStickers.TravStickersCode;

public partial class StickerPreview : Node
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("StickerPreview", LogType.Generic);

    private Sprite2D? _sprite;
    private PlacedSticker? _stats;

    public string? StickerID { get; private set; }
    public bool Active => _sprite != null;

    public void Show(string stickerId)
    {
        if (_sprite != null) Hide();
        if (string.IsNullOrEmpty(stickerId)) return;

        StickerID = stickerId;

        var texture = StickerTexture.Load(stickerId);
        if (texture == null)
        {
            Logger.Warn($"Preview texture not found: {stickerId}");
            return;
        }

        _sprite = new Sprite2D
        {
            Texture = texture,
            Centered = true,
            Modulate = new Color(1, 1, 1, 0.5f)
        };

        // Normalize scale so the sticker matches its display size on the sheet (~80px wide).
        float baseScale = 80f / Mathf.Max(texture.GetWidth(), 1);
        _stats = new PlacedSticker(stickerId, 0, Vector2.Zero, baseScale);
        _sprite.Scale = new Vector2(baseScale, baseScale);
        _sprite.GlobalPosition = GetViewport().GetMousePosition();
        AddChild(_sprite);
    }

    public void Hide()
    {
        if (_sprite != null)
        {
            _sprite.QueueFree();
            _sprite = null;
        }
        _stats = null;
        StickerID = null;
    }

    public PlacedSticker? CurrentStats()
    {
        if (_sprite == null || _stats == null) return null;
        _stats.position = _sprite.GlobalPosition;
        _stats.scale = _sprite.Scale.X;
        _stats.rotation = _sprite.Rotation;
        return _stats;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_sprite != null)
        {
            _sprite.GlobalPosition = GetViewport().GetMousePosition();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_sprite == null) return;
        base._UnhandledInput(@event);

        if (@event is InputEventMouseButton emb && @event.IsPressed())
        {
            if (emb.ButtonIndex == MouseButton.WheelUp)
                _sprite.Scale += new Vector2(0.005f, 0.005f);
            else if (emb.ButtonIndex == MouseButton.WheelDown)
                _sprite.Scale -= new Vector2(0.005f, 0.005f);
            _sprite.Scale = _sprite.Scale.Clamp(new Vector2(0.015f, 0.015f), new Vector2(1.0f, 1.0f));
        }

        if (@event is InputEventKey ke)
        {
            if (ke.Keycode == Key.E)
                _sprite.Rotation += 0.05f;
            else if (ke.Keycode == Key.Q)
                _sprite.Rotation -= 0.05f;
        }
    }
}