﻿using System.Collections.Generic;
using System.Linq;
using Items;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class manages items sprites rendering for UI Images
/// It creates new Image instances in root gameobject for each sprite render in item
/// </summary>
public class UI_ItemImage
{
	private readonly GameObject root;
	private bool hidden;

	private Stack<ImageAndHandler> usedImages = new Stack<ImageAndHandler>();
	private Stack<ImageAndHandler> freeImages = new Stack<ImageAndHandler>();
	private Image overlay;

	/// <summary>
	/// The first sprite in rendered item
	/// Null if there is no item
	/// </summary>
	public Sprite MainSprite
	{
		get
		{
			if (usedImages.Count != 0)
			{
				var firstImage = usedImages.Peek();
				if (firstImage != null && firstImage.Handler)
				{
					return firstImage.Handler.CurrentSprite;
				}
			}

			return null;
		}
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="root">Object to be used as parent for new Image instances</param>
	public UI_ItemImage(GameObject root, Material imgMat)
	{
		this.root = root;

		// generate and hide overlay image
		overlay = CreateNewImage(imgMat, "uiItemImageOverlay");
		SetOverlay(null);
	}

	/// <summary>
	/// Disable all sprites, but not reset their value
	/// </summary>
	public void SetHidden(bool hidden)
	{
		this.hidden = hidden;
		foreach (var pair in usedImages)
		{
			pair.UIImage.enabled = !hidden;
			pair.UIImage.preserveAspect = !hidden;
		}
	}

	/// <summary>
	/// Display item as a composition of Image objects in UI
	/// </summary>
	public void ShowItem(GameObject item,  Material imgMat , Color? forcedColor = null)
	{
		// hide previous image
		ClearAll();
		//determine the sprites to display based on the new item
		var spriteHandlers = item.GetComponentsInChildren<SpriteHandler>(includeInactive: true);
		spriteHandlers = spriteHandlers.Where(x => x != Highlight.instance.spriteRenderer).ToArray();

		foreach (var handler in spriteHandlers)
		{
			// get unused image from stack and subscribe it handler updates
			var image = ConnectFreeImageToHandler(handler, imgMat);

			// check if handler is hidden
			image.gameObject.SetActive(!handler.IsHidden);

			// set sprite
			var sprite = handler.CurrentSprite;
			image.sprite = sprite;

			// set color
			if (forcedColor != null)
			{
				image.color = forcedColor.GetValueOrDefault(Color.white);
			}
			else
			{
				var color = handler.CurrentColor;
				image.color = color;
			}

			// Configure the shader to use palette if item uses it
			var itemAttrs = item.GetComponent<ItemAttributesV2>();
			if (itemAttrs.ItemSprites.IsPaletted)
			{
				image.material.SetInt("_IsPaletted", 1);
				image.material.SetInt("_PaletteSize", itemAttrs.ItemSprites.Palette.Count);
				image.material.SetColorArray("_ColorPalette", itemAttrs.ItemSprites.Palette.ToArray());
			}
			else
			{
				image.material.SetInt("_IsPaletted", 0);
			}

			var colorSync = item.GetComponent<SpriteColorSync>();
			if (colorSync != null)
			{   //later find a way to remove this listener when no longer needed
				colorSync.OnColorChange.AddListener(TrackColor);

				void TrackColor(Color newColor)
				{
					if (colorSync.SpriteRenderer != null
						&& colorSync.SpriteRenderer.sprite == image.sprite)
					{
						image.color = newColor;
					}
				}
			}

			image.enabled = !hidden;
			image.preserveAspect = !hidden;
		}
	}

	/// <summary>
	/// Set overlay image for item (like handcufs icon)
	/// Null to clear sprite and hide image
	/// </summary>
	/// <param name="sprite"></param>
	public void SetOverlay(Sprite overlaySprite)
	{
		if (overlaySprite != null)
		{
			overlay.sprite = overlaySprite;
			overlay.enabled = !hidden;
			overlay.preserveAspect = true;
		}
		else
		{
			overlay.sprite = null;
			overlay.enabled = false;
		}
	}

	/// <summary>
	/// Disable all images and reset their sprites
	/// </summary>
	public void ClearAll()
	{
		while (usedImages.Count != 0)
		{
			var usedImage = usedImages.Pop();
			usedImage.Clear();

			if (usedImage.UIImage != null)
			{
				freeImages.Push(usedImage);
			}
			else
			{
				usedImage.Clear();
			}

			// reset and hide used image
			//usedImage.Handler = null;
			//usedImage.UIImage.enabled = false;
		}

		SetOverlay(null);
	}

	private Image ConnectFreeImageToHandler(SpriteHandler handler, Material imgMat)
	{
		ImageAndHandler pair;
		if (freeImages.Count > 0)
		{
			pair = freeImages.Pop();
		}
		else
		{
			var img = CreateNewImage(imgMat);
			pair = new ImageAndHandler(img);
		}

		pair.Handler = handler;
		usedImages.Push(pair);

		return pair.UIImage;
	}

	private Image CreateNewImage(Material imgMat, string name = "uiItemImage")
	{
		var go = new GameObject(name, typeof(RectTransform));

		var rt = go.GetComponent<RectTransform>();
		rt.SetParent(root.transform);
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.sizeDelta = Vector2.zero;
		rt.anchoredPosition = Vector2.zero;
		rt.localScale = Vector3.one;

		var img = go.AddComponent<Image>();
		img.material = Object.Instantiate(imgMat);
		img.alphaHitTestMinimumThreshold = 0.5f;

		return img;
	}

	/// <summary>
	/// This class subscribe UIImage to SpriteHandler updates
	/// If SpriteHandler updates sprite this will also update it for UIImage
	/// </summary>
	public class ImageAndHandler
	{
		public static List<System.WeakReference<ImageAndHandler>> item_list = new List<System.WeakReference<ImageAndHandler>>();

		System.WeakReference<Image> _img;

		public Image UIImage
		{
			get
			{
				Image trg;
				if (!_img.TryGetTarget(out trg))
				{
					return null;
				}
				else
				{
					return trg;
				}
			}
			private set
			{
				_img = new System.WeakReference<Image>(value);
			}
		}
		private SpriteHandler handler;

		public static void ClearAll()
		{
			foreach (var a in item_list)
			{
				ImageAndHandler iah;

				if (a.TryGetTarget(out iah))
				{
					try
					{
						iah.Clear();
					}
					catch(System.Exception ee)
					{
						Debug.LogException(ee);
					}
				}
			}

			item_list.Clear();
		}

		public ImageAndHandler(Image image)
		{
			item_list.Add(new System.WeakReference<ImageAndHandler>(this));
			UIImage = image;
		}

		public SpriteHandler Handler
		{
			get
			{
				return handler;
			}
			set
			{
				// unsubscribe from old handler changes
				if (handler != null)
				{
					handler.OnSpriteChanged.Remove(OnHandlerSpriteChanged);
					handler.OnColorChanged.Remove(OnHandlerColorChanged);
				}

				handler = value;

				// subscribe to new handler changes
				if (handler)
				{
					OnHandlerSpriteChanged(handler.CurrentSprite);
					OnHandlerColorChanged(handler.CurrentColor);
					handler.OnSpriteChanged.Add(OnHandlerSpriteChanged);
					handler.OnColorChanged.Add(OnHandlerColorChanged);
				}
			}
		}

		private void OnHandlerColorChanged(Color newColor)
		{
			if (!UIImage)
			{
				// looks like image was deleted from scene
				// this happens when item is moved in container
				// and player close this container
				handler.OnSpriteChanged.Remove(OnHandlerSpriteChanged);
				handler.OnColorChanged.Remove(OnHandlerColorChanged);
				return;
			}

			UIImage.color = newColor;
		}

		private void OnHandlerSpriteChanged(Sprite sprite)
		{
			if (UIImage == false)
			{
				// looks like image was deleted from scene
				// this happens when item is moved in container
				// and player close this container
				handler.OnSpriteChanged.Remove(OnHandlerSpriteChanged);
				handler.OnColorChanged.Remove(OnHandlerColorChanged);
				return;
			}

			if (sprite && handler.gameObject.activeInHierarchy)
			{
				UIImage.gameObject.SetActive (true);
				UIImage.sprite = sprite;
			}
			else
			{
				UIImage.gameObject.SetActive(false);
			}

		}

		internal void Clear()
		{
			OnHandlerSpriteChanged(null);
			OnHandlerColorChanged(Color.white);
			handler.OnSpriteChanged.Remove(OnHandlerSpriteChanged);
			handler.OnColorChanged.Remove(OnHandlerColorChanged);
		}
	}
}