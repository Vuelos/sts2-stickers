using Godot;
using System;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using TravStickers;

namespace TravStickers.TravStickersCode;

public partial class StickerOption : NClickableControl
{
	private StickerUi? parent;
	public bool _isDummy = false;
	private TextureRect? _stickerTexture;
	public required String StickerID;
	public override void _Ready()
	{
		ConnectSignals();
		_stickerTexture = this.GetNode<TextureRect>((NodePath)"%stickerTexture");
	}

	private static readonly string _scenePath = "res://" + MainFile.ModId + "/scenes/sticker_option.tscn";
	public static StickerOption Create(StickerUi? parent)
	{
		StickerOption instance = PreloadManager.Cache.GetScene(_scenePath).Instantiate<StickerOption>();
		instance.parent = parent;
		return instance;
	}

    public void setTexture(String StickerID)
    {
        var texture = StickerTexture.Load(StickerID);
        if (texture == null || _stickerTexture == null) return;

        _stickerTexture.Texture = texture;
        this.StickerID = StickerID;
    }
	
	protected override void OnFocus()
	{
	}

	protected override void OnUnfocus()
	{
	}

	protected override void OnPress()
	{
		if (_isDummy) return;
		StickerOption dummy = (StickerOption)Duplicate();
		dummy.StickerID = this.StickerID;
		if (parent != null)
			parent.grabSticker(dummy);
		else
			MapStickers.GrabStickerFromSheet(dummy);
	}

	protected override void OnRelease()
	{
	}
}
