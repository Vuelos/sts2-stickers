using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using Logger = Godot.Logger;

namespace TravStickers.TravStickersCode;

public partial class NStickerLayer : TextureRect, IPoolable
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("StickerLayer", LogType.Generic);
    public static readonly SpireField<NCard, NStickerLayer> StickerLayer = new((node) =>
    {
        NStickerLayer stickers = Create();
        node.AddChild(stickers);
        stickers.Owner = node;
        if (node.Model != null)
        {
            stickers.Card_ID = node.Model.Id.ToString();
        }
        return stickers;
    });

    public string? Card_ID;

    private static GeneratedNodePool<NStickerLayer>? _pool;

    public static void InitPool()
    {
        _pool = GeneratedNodePool.Init(NewInstanceForPool, 32);
    }

    public static NStickerLayer NewInstanceForPool()
    {
        NStickerLayer layer = new()
        {
            Size = new Vector2(300, 422),
            Position = new Vector2(-150, -211),
            PivotOffset = new Vector2(150, 211),
            ExpandMode = ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore,
            ClipChildren = ClipChildrenMode.Only
        };
        return layer;
    }

    public static NStickerLayer Create()
    {
        if (_pool == null)
        {
            InitPool();
        }
        return _pool!.Get();
    }

    public void AddStickerChildren()
    {
        Reset();
        if (Card_ID == null)
        {
            Logger.Info("No card ID!");
            return;
        }
        Texture = ResourceLoader.Load<Texture2D>("res://images/atlases/compressed.sprites/card_template/ancient_portrait_mask_large.tres");
        var key = PlacedSticker.GetPlayerScopedKey(Card_ID);
        if (!PlacedSticker.StickerDB.ContainsKey(key))
        {
            Logger.Info("No Stickers Found!");
            return;
        }
        var AttachedStickers = PlacedSticker.StickerDB[key];
        foreach (var sticker in AttachedStickers)
        {
            if (sticker == null) continue;
            var texture = StickerTexture.Load(sticker.ID);
            if (texture == null) continue;

            var instance = new Sprite2D
            {
                Texture = texture,
                Scale = new Vector2(sticker.scale, sticker.scale),
                Rotation = sticker.rotation,
                Position = sticker.position
            };

            AddChild(instance);
        }
    }

    public void OnInstantiated()
    {
        //Reset();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (string.IsNullOrEmpty(Card_ID)) { Reset(); return; }
        var key = PlacedSticker.GetPlayerScopedKey(Card_ID);
        if (!PlacedSticker.StickerDB.ContainsKey(key)) { Reset(); return; }
        if (GetChildCount() != PlacedSticker.StickerDB[key].Count)
        {
            Reset();
            AddStickerChildren();
        }
    }

    public override void _Ready()
    {
        //I think this is where I shove all the rendering elements??
        //AddStickerChildren();
    }

    public void Reset()
    {
        if (GetChildCount() != 0)
        {
            foreach (var child in GetChildren())
            {
                child.QueueFreeSafely();
            }
        }
    }

    public void OnReturnedFromPool()
    {
    }

    public void OnFreedToPool()
    {
        //Reset();
    }

    private static readonly List<NStickerLayer> _activeHolders = [];
    public static IEnumerable<NStickerLayer> ActiveHolders => _activeHolders;

    public override void _EnterTree()
    {
        base._EnterTree();
        _activeHolders.Add(this);
        
        var parentCard = GetParent<NCard>();
        if (parentCard != null && parentCard.Model != null)
        {
            var newCardId = parentCard.Model.Id.ToString();
            if (newCardId != Card_ID)
            {
                Card_ID = newCardId;
            }
            AddStickerChildren();
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _activeHolders.Remove(this);
    }

}
