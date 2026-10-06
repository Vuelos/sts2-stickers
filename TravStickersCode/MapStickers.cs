using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using TravStickers.TravStickersCode;
using DirAccess = Godot.DirAccess;
using FileAccess = Godot.FileAccess;
using NGame = MegaCrit.Sts2.Core.Nodes.NGame;

namespace TravStickers.TravStickersCode;

public static class MapStickers
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("MapStickers", LogType.Generic);

    public static bool IsStickerModeActive { get; private set; } = false;
    public static string? SelectedStickerId { get; set; } = null;
    public static bool IsDraggingSticker { get; private set; } = false;

    private static NMapDrawings? _mapDrawingsInstance = null;
    private static Node2D? _mapStickersContainer = null;

    private static readonly string _stickerSheetScenePath = "res://" + MainFile.ModId + "/scenes/sticker_sheet.tscn";
    private static readonly string _stickerButtonScenePath = "res://" + MainFile.ModId + "/scenes/sticker_editor_button.tscn";
    private static Node? _stickerSheetInstance = null;
    private static StickerPreview? _stickerPreview = null;

    public static void SetMapDrawingsInstance(NMapDrawings? instance) => _mapDrawingsInstance = instance;
    public static NMapDrawings? GetMapDrawingsInstance() => _mapDrawingsInstance;

    public static void SetMapStickersContainer(Node2D? container) => _mapStickersContainer = container;
    public static Node2D? GetMapStickersContainer() => _mapStickersContainer;

    public static void InitMapStickers(NMapScreen mapScreen)
    {
        var drawings = mapScreen.GetNodeOrNull<NMapDrawings>("TheMap/Drawings");
        if (drawings == null)
        {
            SetMapStickersContainer(null);
            return;
        }
        SetMapDrawingsInstance(drawings);

        _mapStickersContainer?.QueueFree();
        var container = new Node2D();
        container.Name = "StickerOverlays";
        drawings.AddChild(container);
        SetMapStickersContainer(container);
    }

    public static void ReloadMapStickers(NMapScreen mapScreen)
    {
        InitMapStickers(mapScreen);
    }

    public static void SetStickerMode(bool active, string? stickerId = null)
    {
        IsStickerModeActive = active;
        SelectedStickerId = stickerId;

        if (active)
        {
            ShowStickerSheet();
        }
        else
        {
            CloseStickerSheet();
            IsDraggingSticker = false;
        }

        Logger.Info($"Map sticker mode: {(active ? "ON" : "OFF")}" + (stickerId != null ? $" ({stickerId})" : ""));
    }

    public static void GrabStickerFromSheet(StickerOption option)
    {
        IsStickerModeActive = true;
        SelectedStickerId = option.StickerID;
        IsDraggingSticker = true;

        if (_stickerPreview == null && NGame.Instance != null)
        {
            _stickerPreview = new StickerPreview();
            NGame.Instance.AddChild(_stickerPreview);
        }
        _stickerPreview?.Show(option.StickerID);
        Logger.Info($"Grabbed map sticker: {option.StickerID}");
    }

    private static void CloseStickerSheet()
    {
        if (_stickerSheetInstance != null)
        {
            _stickerSheetInstance.QueueFree();
            _stickerSheetInstance = null;
        }
        _stickerPreview?.Hide();
    }

    public static void ShowStickerSheet()
    {
        try
        {
            CloseStickerSheet();

            var sheetScene = ResourceLoader.Load<PackedScene>(_stickerSheetScenePath);
            if (sheetScene == null)
            {
                Logger.Warn("Sticker sheet scene not found.");
                return;
            }

            _stickerSheetInstance = sheetScene.Instantiate<Node>();
            NGame.Instance?.AddChild(_stickerSheetInstance);

            if (_stickerSheetInstance is Control sheetControl)
            {
                sheetControl.LayoutMode = 1;
                sheetControl.Position = new Vector2(1320, 0);
            }

            if (_stickerSheetInstance is StickerSheet stickerSheet)
            {
                stickerSheet.CallDeferred(nameof(StickerSheet.PopulateStickers));
            }

            var closeBtn = new Button
            {
                Icon = ResourceLoader.Load<Texture2D>("res://" + MainFile.ModId + "/images/ui/close_icon.png"),
                Flat = true,
                Position = new Vector2(340, 10),
                Size = new Vector2(48, 48)
            };
            closeBtn.Connect(Button.SignalName.Pressed, Callable.From(new Action(() => {
                SetStickerMode(false);
            })));
            _stickerSheetInstance.AddChild(closeBtn);

            Logger.Info("Opened map sticker sheet.");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to show sticker sheet: {ex.Message}");
        }
    }

    public static void AddStickerButton(NMapScreen mapScreen)
    {
        if (mapScreen.GetNodeOrNull("StickerButton") != null)
            return;

        var stickerBtn = PreloadManager.Cache.GetScene(_stickerButtonScenePath).Instantiate<OpenEditorButton>();
        stickerBtn.Name = "StickerButton";
        stickerBtn.Connect(NClickableControl.SignalName.Released, Callable.From(new Action<NButton>(_ => {
            SetStickerMode(true);
        })));
        mapScreen.AddChild(stickerBtn);

        Logger.Info("Added sticker button to map screen.");
    }

    public static void PlaceStickerOnMap(PlacedSticker sticker, NMapScreen mapScreen)
    {
        if (!IsStickerModeActive || sticker == null || string.IsNullOrEmpty(sticker.ID))
            return;

        try
        {
            var container = _mapStickersContainer;
            if (container == null) return;

            // Convert the click's screen position to the scrolling container's local space,
            // so the sticker stays at the clicked map location and scrolls with the map.
            var localPos = container.GetGlobalTransform().AffineInverse() * sticker.position;

            var stickerData = new Dictionary
            {
                { "id", sticker.ID },
                { "position_x", localPos.X },
                { "position_y", localPos.Y },
                { "scale", sticker.scale },
                { "rotation", sticker.rotation }
            };

            RenderMapSticker(stickerData, mapScreen);
            SaveStickerToRunSave(stickerData);

            Logger.Info($"Placed sticker {sticker.ID} on map at {localPos}");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to place sticker: {ex.Message}");
        }
    }

    private static void RenderMapSticker(Dictionary stickerData, NMapScreen mapScreen)
    {
        if (_mapStickersContainer == null || mapScreen == null)
            return;

        try
        {
            var stickerId = stickerData["id"].ToString();
            var localPos = new Vector2((float)stickerData["position_x"], (float)stickerData["position_y"]);
            var scale = (float)stickerData["scale"];
            var rot = (float)stickerData["rotation"];

            var texture = StickerTexture.Load(stickerId);
            if (texture == null)
            {
                Logger.Warn($"Map sticker texture not found: {stickerId}");
                return;
            }

            var sprite = new Sprite2D
            {
                Texture = texture,
                Position = localPos,
                Scale = new Vector2(scale, scale),
                Rotation = rot,
                Centered = true
            };

            _mapStickersContainer.AddChild(sprite);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to render map sticker: {ex.Message}");
        }
    }

    private static void LoadStickersFromSave(NMapScreen mapScreen)
    {
        if (_mapStickersContainer == null)
            return;

        try
        {
            foreach (Node child in _mapStickersContainer.GetChildren())
            {
                child.QueueFree();
            }

            var savePath = "user://modded/profile1/saves/current_run.save";
            if (!FileAccess.FileExists(savePath))
            {
                var mpSavePath = "user://modded/profile1/saves/current_run_mp.save";
                if (FileAccess.FileExists(mpSavePath))
                    savePath = mpSavePath;
                else
                    return;
            }

            using var file = FileAccess.Open(savePath, FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            var parsed = Json.ParseString(json).AsGodotDictionary();

            if (!parsed.ContainsKey("map_drawings"))
                return;

            var mapDrawings = (Dictionary)parsed["map_drawings"];
            if (!mapDrawings.ContainsKey("stickers"))
                return;

            var stickers = (Godot.Collections.Array)mapDrawings["stickers"];
            foreach (Dictionary dict in stickers)
            {
                RenderMapSticker(dict, mapScreen);
            }

            Logger.Info($"Loaded {stickers.Count} stickers from save.");
        }
        catch (System.Exception ex)
        {
            Logger.Warn($"Failed to load stickers from save: {ex.Message}");
        }
    }

    public static bool ProcessMapStickerInput(NMapScreen mapScreen)
    {
        if (!IsStickerModeActive || string.IsNullOrEmpty(SelectedStickerId) || !IsDraggingSticker)
            return true;

        try
        {
            if (Input.IsActionJustPressed("ui_accept") || Input.IsMouseButtonPressed(MouseButton.Left))
            {
                var viewport = mapScreen.GetViewport();
                if (viewport == null) return true;

                var mousePos = viewport.GetMousePosition();

                // Don't place when clicking over the sticker sheet (right side of the screen).
                if (mousePos.X >= 1320) return true;

                var stats = _stickerPreview?.CurrentStats();
                if (stats != null)
                {
                    PlaceStickerOnMap(stats, mapScreen);
                }

                IsDraggingSticker = false;
                SelectedStickerId = null;
                _stickerPreview?.Hide();
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Error in map sticker process: {ex.Message}");
        }

        return true;
    }

    private static void SaveStickerToRunSave(Dictionary stickerData)
    {
        try
        {
            var savePath = "user://modded/profile1/saves/current_run.save";
            if (!FileAccess.FileExists(savePath))
            {
                var mpSavePath = "user://modded/profile1/saves/current_run_mp.save";
                if (FileAccess.FileExists(mpSavePath))
                    savePath = mpSavePath;
                else
                    return;
            }

            using var file = FileAccess.Open(savePath, FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            var parsed = Json.ParseString(json).AsGodotDictionary();

            if (!parsed.ContainsKey("map_drawings"))
            {
                parsed["map_drawings"] = new Dictionary();
            }
            var mapDrawings = (Dictionary)parsed["map_drawings"];

            if (!mapDrawings.ContainsKey("stickers"))
            {
                mapDrawings["stickers"] = new Godot.Collections.Array();
            }
            var stickersArray = (Godot.Collections.Array)mapDrawings["stickers"];
            stickersArray.Add(stickerData);

            file.Close();

            using var writeFile = FileAccess.Open(savePath, FileAccess.ModeFlags.Write);
            writeFile.StoreString(Json.Stringify(parsed));
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to save sticker: {ex.Message}");
        }
    }

    public static void ClearMapStickers()
    {
        try
        {
            if (_mapStickersContainer != null)
            {
                foreach (Node child in _mapStickersContainer.GetChildren())
                {
                    child.QueueFree();
                }
            }

            var savePath = "user://modded/profile1/saves/current_run.save";
            if (!FileAccess.FileExists(savePath))
            {
                var mpSavePath = "user://modded/profile1/saves/current_run_mp.save";
                if (FileAccess.FileExists(mpSavePath))
                    savePath = mpSavePath;
                else
                    return;
            }

            using var file = FileAccess.Open(savePath, FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            var parsed = Json.ParseString(json).AsGodotDictionary();

            if (parsed.ContainsKey("map_drawings"))
            {
                var mapDrawings = (Dictionary)parsed["map_drawings"];
                if (mapDrawings.ContainsKey("stickers"))
                {
                    mapDrawings["stickers"] = new Godot.Collections.Array();
                    file.Close();

                    using var writeFile = FileAccess.Open(savePath, FileAccess.ModeFlags.Write);
                    writeFile.StoreString(Json.Stringify(parsed));

                    Logger.Info("Cleared all map stickers.");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to clear map stickers: {ex.Message}");
        }
    }
}