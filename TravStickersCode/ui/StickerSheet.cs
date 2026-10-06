using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace TravStickers.TravStickersCode;

public partial class StickerSheet : Control
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("StickerSheet", LogType.Generic);
    
    public Control? StickerBox { get; private set; } = null;
    
    public override void _Ready()
    {
        base._Ready();
        StickerBox = GetNodeOrNull<Control>((NodePath)"BG/MarginContainer/ScrollContainer/stickerBox");
    }
    
    public void PopulateStickers()
    {
        PopulateStickers(null);
    }
    
    public void PopulateStickers(StickerUi? parentUi)
    {
        if (StickerBox == null) return;
        
        foreach (var child in StickerBox.GetChildren())
        {
            child.QueueFree();
        }
        
        var files = DirAccess.GetFilesAt("res://" + MainFile.ModId + "/images/stickers");
        foreach (var file in files)
        {
            var stickerId = file.Split('.')[0];
            var sticker = StickerOption.Create(parentUi);
            if (sticker == null) continue;
            StickerBox.AddChild(sticker);
            sticker.setTexture(stickerId);
        }
        
        foreach (var relicId in GameRelicIds.All)
        {
            if (StickerTexture.Load(relicId) == null)
                continue;
            var sticker = StickerOption.Create(parentUi);
            if (sticker == null) continue;
            StickerBox.AddChild(sticker);
            sticker.setTexture(relicId);
        }
        
        foreach (var modifierId in GameModifierIds.All)
        {
            if (StickerTexture.Load(modifierId) == null)
                continue;
            var sticker = StickerOption.Create(parentUi);
            if (sticker == null) continue;
            StickerBox.AddChild(sticker);
            sticker.setTexture(modifierId);
        }
        
        foreach (var ascensionId in GameAscensionIds.All)
        {
            if (StickerTexture.Load(ascensionId) == null)
                continue;
            var sticker = StickerOption.Create(parentUi);
            if (sticker == null) continue;
            StickerBox.AddChild(sticker);
            sticker.setTexture(ascensionId);
        }
        
        foreach (var badgeId in GameBadgeIds.All)
        {
            if (badgeId == "badge_bronze" || badgeId == "badge_silver" || badgeId == "badge_gold" || badgeId == "badge_outline")
            {
                if (StickerTexture.Load(badgeId) == null)
                    continue;
                var sticker = StickerOption.Create(parentUi);
                if (sticker == null) continue;
                StickerBox.AddChild(sticker);
                sticker.setTexture(badgeId);
            }
            else
            {
                foreach (var tier in new[] { "badge_bronze", "badge_silver", "badge_gold" })
                {
                    var texture = StickerTexture.LoadBadge(badgeId, tier);
                    if (texture == null) continue;
                    var sticker = StickerOption.Create(parentUi);
                    if (sticker == null) continue;
                    StickerBox.AddChild(sticker);
                    sticker.setTexture($"{badgeId}_{tier}");
                }
            }
        }
        
        Logger.Info($"Populated sticker sheet with {StickerBox.GetChildCount()} stickers.");
    }
    
    public void SelectSticker(string stickerId)
    {
        MapStickers.SelectedStickerId = stickerId;
        Logger.Info($"Selected map sticker: {stickerId}");
    }
}
