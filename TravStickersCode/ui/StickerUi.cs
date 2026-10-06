using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using TravStickers;
using TravStickers.TravStickersCode;

public partial class StickerUi : Control, IScreenContext
{
 	private Control? _cardAnchor;
 	private Control? _cardContainer; // For stupid layering
 	private AnimationPlayer? _animationPlayer;
 	
 	private NCard? _card;
 	private Control? _activeStickerParent;
 	public StickerOption? _activeSticker;
 	private sticker_placement_node? _stickerPlacement;
 	private NButton? _trashButton;
 	private NButton? _clearAllButton;
 	private NButton? _closeButton;

 	private Control? _stickerSheet;
 	public Control? _stickerBox;
 	
 	private static readonly string _scenePath = "res://" + MainFile.ModId + "/scenes/sticker_ui.tscn";
	private static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new("StickerEditor", LogType.Generic);

	public static readonly SpireField<NGame, Control> StickerEditorLayer = new((node) =>
	{
		Control layer = new Control();
		layer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		layer.MouseFilter = MouseFilterEnum.Ignore;
		Logger.Info("Created StickerEditorLayer");
		return layer;
	});

	public static StickerUi? StickerEditScreen = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Logger.Info("StickerUi._Ready running.");
		EnsureInitialized();
	}

  	private void EnsureInitialized()
  	{
  		if (_cardContainer != null) return;
  		Logger.Info("StickerUi.EnsureInitialized running.");
  		_cardAnchor = this.GetNode<Control>((NodePath)"%travCardAnchor");
  		_activeStickerParent = this.GetNode<Control>((NodePath)"%stickerMouseFollower");
  		_cardContainer = this.GetNode<Control>((NodePath)"%cardContainer");
  		_animationPlayer = this.GetNode<AnimationPlayer>("%travIntroAnimator");
  		_stickerPlacement = this.GetNode<sticker_placement_node>((NodePath)"%stickerPlacement");
  		if (_stickerPlacement != null) _stickerPlacement.parent = this;
   	_trashButton = this.GetNode<NButton>((NodePath)"%TrashButton");
   	if (_trashButton != null) _trashButton.Connect(NClickableControl.SignalName.Released, Callable.From(new Action<NButton>(_ =>clearStickers())));
   	_clearAllButton = this.GetNode<NButton>((NodePath)"%ClearAllButton");
   	if (_clearAllButton != null) _clearAllButton.Connect(NClickableControl.SignalName.Released, Callable.From(new Action<NButton>(_ =>clearAllStickers())));
   	_closeButton = this.GetNode<NButton>((NodePath)"%CloseButton");
  		if (_closeButton != null) _closeButton.Connect(NClickableControl.SignalName.Released, Callable.From(new Action<NButton>(_ =>Close())));
  		
  		Logger.Info($"StickerUi.EnsureInitialized: cardContainer={_cardContainer}");
  		if (_animationPlayer != null) _animationPlayer.Play("slide");
  		
if (_stickerSheet == null)
	{
		_stickerSheet = GetNodeOrNull<Control>((NodePath)"StickerSheet");
		if (_stickerSheet is StickerSheet stickerSheet)
		{
			stickerSheet.PopulateStickers(this);
		}
	}
  		
  		Logger.Info($"StickerUi.EnsureInitialized complete.");
  	}
 	
 	public void OpenMapStickerMode()
 	{
 		Close();
 		MapStickers.SetStickerMode(true);
 		Logger.Info("Opened map sticker mode.");
 	}

 	public void clearStickers()
 	{
 		if (_card == null || _card.Model == null) return;
 		var key = PlacedSticker.GetPlayerScopedKey(_card.Model.Id.ToString());
 		Logger.Info($"{PlacedSticker.StickerDB.Remove(key)}");
 		_card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
 		PlacedSticker.SaveStickers();
 	}
 
 	public void clearAllStickers()
 	{
 		var keysToRemove = new List<string>();
 		
 		if (PlacedSticker.IsMultiplayer)
 		{
 			foreach (var key in PlacedSticker.StickerDB.Keys)
 			{
 				keysToRemove.Add(key);
 			}
 		}
 		else
 		{
 			var currentPlayerId = PlacedSticker.CurrentPlayerId;
 			foreach (var key in PlacedSticker.StickerDB.Keys)
 			{
 				if (key.StartsWith(currentPlayerId + ":"))
 				{
 					keysToRemove.Add(key);
 				}
 			}
 		}
 		
 		foreach (var key in keysToRemove)
 		{
 			PlacedSticker.StickerDB.Remove(key);
 		}
 		
 		PlacedSticker.SaveStickers();
 		Logger.Info($"Cleared {keysToRemove.Count} sticker entries.");
 	}

	public void Open(CardModel template)
	{
		EnsureInitialized();
		PlacedSticker.DetectCurrentPlayerId();
		InspectionScreenButtonPatch.HideButton();
		Logger.Info($"StickerUi.Open called. template={template?.Id}, Visible={Visible}, _card={_card}, _cardContainer={_cardContainer}");
		if (template == null)
		{
			Logger.Warn("Open: template is null, returning.");
			return;
		}
		if (_card == null)
		{
			_card = NCard.Create(template);
			if (_card == null || _cardContainer == null)
			{
				Logger.Warn($"Open: could not create preview card (card={_card}, container={_cardContainer}).");
				return;
			}
			_cardContainer.AddChildSafely(_card);
		}
		_card.Model = template;
		_card.Visible = true;
		_card!.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
		RefreshPreviewStickers();
		Visible = true;
		ActiveScreenContext.Instance?.Update();
		NHotkeyManager.Instance?.AddBlockingScreen(this);
		NHotkeyManager.Instance?.PushHotkeyPressedBinding(MegaInput.cancel, Close);
		NHotkeyManager.Instance?.PushHotkeyPressedBinding(MegaInput.pauseAndBack, Close);
	}

	private void RefreshPreviewStickers()
	{
		if (_card == null || _card.Model == null) return;
		var layer = NStickerLayer.StickerLayer[_card];
		if (layer == null) return;
		layer.Card_ID = _card.Model.Id.ToString();
		if (layer.GetParent() != _card)
		{
			_card.AddChildSafely(layer);
		}
		layer.Visible = true;
		layer.AddStickerChildren();
	}
	
	public static StickerUi Create()
	{
		return PreloadManager.Cache.GetScene(_scenePath).Instantiate<StickerUi>();
	}

	public void grabSticker(StickerOption option)
	{
		if (_activeSticker != null)
		{
			_activeSticker.QueueFreeSafely();
			_activeSticker = null;
		}

		_activeSticker = option;
		_activeSticker._isDummy = true;
		_activeSticker.MouseFilter = MouseFilterEnum.Ignore;
		_activeSticker.Scale = new Vector2(0.5f,0.5f);
		_activeStickerParent?.AddChild(_activeSticker);
		Logger.Info($"Grabbed Sticker {_activeSticker.Name}");
		_activeSticker.Position = -_activeSticker.PivotOffset;
	}

	public void placeSticker(PlacedSticker sticker)
	{
		if (_activeSticker == null || _card == null || _card.Model == null) return;
		PlacedSticker.Save(_card.Model.Id.ToString(), sticker);
		_activeSticker.QueueFreeSafely();
		_activeSticker = null;
		_card.UpdateVisuals(_card.DisplayingPile,CardPreviewMode.Normal);
		RefreshPreviewStickers();
	}
	
	public static StickerUi OpenStickerMenu()
	{
		if (StickerEditScreen == null)
		{
			StickerEditScreen = Create();
			if (NGame.Instance == null) return StickerEditScreen;
			var layer = StickerEditorLayer.Get(NGame.Instance);
			if (layer == null) return StickerEditScreen;
			if (!layer.IsInsideTree())
			{
				Logger.Info("OpenStickerMenu: adding editor layer to game tree.");
				NGame.Instance.AddChild(layer);
			}
			layer.AddChild(StickerEditScreen);
		}
		return StickerEditScreen;
	}

	public void Close()
	{
		if (Visible)
		{
			MouseFilter = MouseFilterEnum.Ignore;
			Visible = false;
			NHotkeyManager.Instance?.RemoveHotkeyPressedBinding(MegaInput.cancel, Close);
			NHotkeyManager.Instance?.RemoveHotkeyPressedBinding(MegaInput.pauseAndBack, Close);
			NHotkeyManager.Instance?.RemoveBlockingScreen(this);
			if (_activeSticker != null)
			{
				_activeSticker.QueueFreeSafely();
			}
			_activeSticker = null;
			InspectionScreenButtonPatch.ShowButton();
		}
	}

	private void OnBackstopPressed(NButton _)
	{
		Close();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (_activeSticker != null && _activeStickerParent != null)
		{
			_activeStickerParent.Position =  GetViewport().GetMousePosition();
		}
	}

	public Control? DefaultFocusedControl => null;
}
