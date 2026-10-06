using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Logging;
using FileAccess = Godot.FileAccess;
using Logger = Godot.Logger;

namespace TravStickers.TravStickersCode;

public partial class PlacedSticker : Resource
{
    private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("StickerDatabase", LogType.Generic);
    
    public String ID;
    public float rotation;
    public Vector2 position;
    public float scale;

    public PlacedSticker(String id, float rotation, Vector2 position, float scale)
    {
        ID = id;
        this.rotation = rotation;
        this.position = position;
        this.scale = scale;
    }

    public static System.Collections.Generic.Dictionary<String, List<PlacedSticker>> StickerDB = new System.Collections.Generic.Dictionary<String, List<PlacedSticker>>();

    public static string CurrentPlayerId { get; private set; } = "singleplayer";
    public static bool IsMultiplayer { get; private set; } = false;
    public static bool IsRunActive { get; private set; } = false;

    public static void SetCurrentPlayerId(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
            return;
        CurrentPlayerId = playerId;
        Logger.Info($"Set current player ID: {playerId}");
    }

    public static string GetPlayerScopedKey(string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
            return cardId;
        if (IsMultiplayer)
            return cardId;
        return $"{CurrentPlayerId}:{cardId}";
    }
    
    public static void Save(String card_ID, PlacedSticker sticker)
    {
        var key = GetPlayerScopedKey(card_ID);
        if (!StickerDB.ContainsKey(key))
        {
            Logger.Info($"Initializing sticker list for {key}");
            StickerDB.Add(key, new List<PlacedSticker>());
        }
        StickerDB[key].Add(sticker);
        Logger.Info($"Placed sticker {sticker.ID} onto {key}");
        SaveStickers();
        PersistToRunSave();
    }

    public static void SaveStickers()
    {
        var data = serializeDB();
        var json = Json.Stringify(data);
        
        using var file = FileAccess.Open("user://mod_configs/Stickers.json", FileAccess.ModeFlags.Write);
        file.StoreString(json);
    }

    public static Dictionary serializeDB()
    {
        var result = new Dictionary();

        foreach (var kvp in StickerDB)
        {
            var list = new Godot.Collections.Array();
            foreach (var sticker in kvp.Value)
            {
                list.Add(sticker.ToDictionary());
            }
            
            result[kvp.Key] = list;
        }   
        
        return result;
    }
    
    public Dictionary ToDictionary()
    {
        return new Dictionary
        {
            {"id", ID},
            {"rotation", rotation},
            {"position_x", position.X},
            {"position_y", position.Y},
            {"scale", scale}
        };
    }

    public static PlacedSticker FromDictionary(Dictionary dictionary)
    {
        return new PlacedSticker(
            (String)dictionary["id"],(float)dictionary["rotation"],new Vector2((float)dictionary["position_x"],(float)dictionary["position_y"]),(float)dictionary["scale"]);
    }
    
    public static void Load()
    {
        DetectMultiplayerMode();
        DetectRunState();
        DetectCurrentPlayerId();
        
        StickerDB.Clear();
        
        if (!FileAccess.FileExists("user://mod_configs/Stickers.json")) return;
        Logger.Info("Loading Stickers.json!");
        using var file = FileAccess.Open("user://mod_configs/Stickers.json", FileAccess.ModeFlags.Read);
        var json = file.GetAsText();
        var parsed = Json.ParseString(json).AsGodotDictionary();

        foreach (var key in parsed.Keys)
        {
            var list = new List<PlacedSticker>();
            var array = (Godot.Collections.Array)parsed[key];

            foreach (Dictionary dict in array)
            {
                list.Add(FromDictionary(dict));
            }

            if (!IsMultiplayer && key.ToString().Contains(":"))
            {
                StickerDB.Add(key.ToString(), list);
            }
            else
            {
                StickerDB.Add(GetPlayerScopedKey(key.ToString()), list);
            }
        }

        if (IsMultiplayer && IsRunActive)
        {
            LoadFromRunSave();
        }
    }

    public static void DetectMultiplayerMode()
    {
        try
        {
            var mpSavePath = "user://modded/profile1/saves/current_run_mp.save";
            IsMultiplayer = FileAccess.FileExists(mpSavePath);
            if (IsMultiplayer)
            {
                Logger.Info("Multiplayer mode detected.");
            }
        }
        catch (System.Exception ex)
        {
            Logger.Warn($"Failed to detect multiplayer mode: {ex.Message}");
            IsMultiplayer = false;
        }
    }

    public static void DetectRunState()
    {
        try
        {
            var mpSavePath = "user://modded/profile1/saves/current_run_mp.save";
            var spSavePath = "user://modded/profile1/saves/current_run.save";
            
            IsRunActive = FileAccess.FileExists(mpSavePath) || FileAccess.FileExists(spSavePath);
            Logger.Info($"Run active: {IsRunActive}");
        }
        catch (System.Exception ex)
        {
            Logger.Warn($"Failed to detect run state: {ex.Message}");
            IsRunActive = false;
        }
    }

    public static void DetectCurrentPlayerId()
    {
        try
        {
            if (!IsMultiplayer)
            {
                CurrentPlayerId = "singleplayer";
                return;
            }

            var mpSavePath = "user://modded/profile1/saves/current_run_mp.save";
            if (FileAccess.FileExists(mpSavePath))
            {
                using var file = FileAccess.Open(mpSavePath, FileAccess.ModeFlags.Read);
                var json = file.GetAsText();
                var parsed = Json.ParseString(json).AsGodotDictionary();
                
                if (parsed.ContainsKey("players"))
                {
                    var players = (Godot.Collections.Array)parsed["players"];
                    if (players.Count > 0)
                    {
                        var player = (Dictionary)players[0];
                        if (player.ContainsKey("player_id"))
                        {
                            SetCurrentPlayerId(player["player_id"].ToString());
                            return;
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Logger.Warn($"Failed to detect player ID: {ex.Message}");
        }
        
        CurrentPlayerId = "singleplayer";
        Logger.Info($"Using default player ID: {CurrentPlayerId}");
    }

    public static void PersistToRunSave()
    {
        if (!IsMultiplayer || !IsRunActive)
            return;

        try
        {
            var mpSavePath = "user://modded/profile1/saves/current_run_mp.save";
            if (!FileAccess.FileExists(mpSavePath))
                return;

            using var file = FileAccess.Open(mpSavePath, FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            var parsed = Json.ParseString(json).AsGodotDictionary();

            var stickerData = serializeDB();
            if (!parsed.ContainsKey("mod_data"))
            {
                parsed["mod_data"] = new Dictionary();
            }
            var modData = (Dictionary)parsed["mod_data"];
            modData["TravStickers"] = stickerData;

            file.Close();

            using var writeFile = FileAccess.Open(mpSavePath, FileAccess.ModeFlags.Write);
            var newJson = Json.Stringify(parsed);
            writeFile.StoreString(newJson);

            Logger.Info("Persisted stickers to multiplayer run save.");
        }
        catch (System.Exception ex)
        {
            Logger.Warn($"Failed to persist stickers to run save: {ex.Message}");
        }
    }

    public static void LoadFromRunSave()
    {
        if (!IsMultiplayer || !IsRunActive)
            return;

        try
        {
            var mpSavePath = "user://modded/profile1/saves/current_run_mp.save";
            if (!FileAccess.FileExists(mpSavePath))
                return;

            using var file = FileAccess.Open(mpSavePath, FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            var parsed = Json.ParseString(json).AsGodotDictionary();

            if (!parsed.ContainsKey("mod_data"))
                return;

            var modData = (Dictionary)parsed["mod_data"];
            if (!modData.ContainsKey("TravStickers"))
                return;

            var stickerData = (Dictionary)modData["TravStickers"];
            StickerDB.Clear();

            foreach (var key in stickerData.Keys)
            {
                var list = new List<PlacedSticker>();
                var array = (Godot.Collections.Array)stickerData[key];

                foreach (Dictionary dict in array)
                {
                    list.Add(FromDictionary(dict));
                }

                StickerDB.Add(key.ToString(), list);
            }

            Logger.Info("Loaded stickers from multiplayer run save.");
            RefreshAllActiveLayers();
        }
        catch (System.Exception ex)
        {
            Logger.Warn($"Failed to load stickers from run save: {ex.Message}");
        }
    }

    public static void RefreshAllActiveLayers()
    {
        foreach (var layer in NStickerLayer.ActiveHolders)
        {
            if (layer == null || layer.IsQueuedForDeletion())
                continue;
            layer.AddStickerChildren();
        }
    }
}
