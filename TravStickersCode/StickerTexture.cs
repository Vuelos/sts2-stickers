using Godot;
using System;
using System.Collections.Generic;

namespace TravStickers.TravStickersCode;

public static class StickerTexture
{
    private static readonly HashSet<string> BadgeTierBackgrounds = new()
    {
        "badge_bronze",
        "badge_silver",
        "badge_gold",
        "badge_outline"
    };

    public static Texture2D? Load(string stickerId)
    {
        if (string.IsNullOrEmpty(stickerId))
            return null;

        // Tier backgrounds like badge_bronze must not be re-parsed as composite badges.
        if (!BadgeTierBackgrounds.Contains(stickerId))
        {
            int lastUnderscore = stickerId.LastIndexOf('_');
            if (lastUnderscore > 0)
            {
                string potentialIconId = stickerId.Substring(0, lastUnderscore);
                string potentialTier = stickerId.Substring(lastUnderscore + 1);

                if (potentialTier is "bronze" or "silver" or "gold")
                {
                    if (!BadgeTierBackgrounds.Contains(potentialIconId))
                    {
                        return LoadBadge(potentialIconId, potentialTier);
                    }
                }
            }
        }

        string modPath = $"res://{MainFile.ModId}/images/stickers/{stickerId}.png";
        if (ResourceLoader.Exists(modPath))
            return ResourceLoader.Load<Texture2D>(modPath);

        string relicPath = $"res://images/relics/{stickerId}.png";
        if (ResourceLoader.Exists(relicPath))
            return ResourceLoader.Load<Texture2D>(relicPath);

        string atlasPath = $"res://images/atlases/relic_atlas.sprites/{stickerId}.tres";
        if (ResourceLoader.Exists(atlasPath))
            return ResourceLoader.Load<Texture2D>(atlasPath);

        string outlineAtlasPath = $"res://images/atlases/relic_outline_atlas.sprites/{stickerId}.tres";
        if (ResourceLoader.Exists(outlineAtlasPath))
            return ResourceLoader.Load<Texture2D>(outlineAtlasPath);

        string modifierPath = $"res://images/atlases/power_atlas.sprites/{stickerId}.tres";
        if (ResourceLoader.Exists(modifierPath))
            return ResourceLoader.Load<Texture2D>(modifierPath);

        string badgePath = $"res://images/ui/game_over_screen/{stickerId}.png";
        if (ResourceLoader.Exists(badgePath))
            return ResourceLoader.Load<Texture2D>(badgePath);

        string ascensionAtlasPath = $"res://images/atlases/ui_atlas.sprites/top_bar/{stickerId}.tres";
        if (ResourceLoader.Exists(ascensionAtlasPath))
            return ResourceLoader.Load<Texture2D>(ascensionAtlasPath);

        string ascensionPath = $"res://images/ui/game_over_screen/{stickerId}.png";
        if (ResourceLoader.Exists(ascensionPath))
            return ResourceLoader.Load<Texture2D>(ascensionPath);

        string coinAtlasPath = $"res://images/atlases/ui_atlas.sprites/top_bar/{stickerId}.tres";
        if (ResourceLoader.Exists(coinAtlasPath))
            return ResourceLoader.Load<Texture2D>(coinAtlasPath);

        string coinIconPath = $"res://images/packed/sprite_fonts/{stickerId}.png";
        if (ResourceLoader.Exists(coinIconPath))
            return ResourceLoader.Load<Texture2D>(coinIconPath);

        return null;
    }
    
    public static Texture2D? LoadBadge(string badgeIconId, string tier)
    {
        var icon = Load(badgeIconId);
        var bg = Load(tier);
        if (icon == null || bg == null) return icon;
        
        return Composite(bg, icon);
    }
    
    private static Texture2D? Composite(Texture2D background, Texture2D foreground)
    {
        var bgImage = background.GetImage();
        var fgImage = foreground.GetImage();
        if (bgImage == null || fgImage == null) return null;

        // GetImage may return compressed data; blit_rect cannot operate on compressed images.
        bgImage.Decompress();
        fgImage.Decompress();
        if (bgImage.GetFormat() != Image.Format.Rgba8) bgImage.Convert(Image.Format.Rgba8);
        if (fgImage.GetFormat() != Image.Format.Rgba8) fgImage.Convert(Image.Format.Rgba8);

        int w = Math.Min(bgImage.GetWidth(), fgImage.GetWidth());
        int h = Math.Min(bgImage.GetHeight(), fgImage.GetHeight());

        var result = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        result.BlitRect(bgImage, new Rect2I(0, 0, w, h), Vector2I.Zero);
        result.BlitRect(fgImage, new Rect2I(0, 0, w, h), Vector2I.Zero);

        var texture = ImageTexture.CreateFromImage(result);
        return texture;
    }
}
