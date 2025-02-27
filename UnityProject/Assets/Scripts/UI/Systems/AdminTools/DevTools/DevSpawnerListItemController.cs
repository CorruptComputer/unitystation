﻿using System.Collections;
using System.Collections.Generic;
using Items;
using Messages.Client.DevSpawner;
using Objects.Atmospherics;
using Systems.Pipes;
using UI.Systems.AdminTools.DevTools.Search;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;


[RequireComponent(typeof(EscapeKeyTarget))]
public class DevSpawnerListItemController : MonoBehaviour
{
	public Image image;
	private bool isPaletted = false;
	public List<Color> palette;
	public Text titleText;
	public Text detailText;
	public GameObject drawingMessage;

	// holds which item is currently selected, shared between instances of this component.
	private static DevSpawnerListItemController selectedItem;

	//prefab to use for our cursor when painting
	public GameObject cursorPrefab;
	// prefab to spawn
	private GameObject prefab;

	// sprite under cursor for showing what will be spawned
	private GameObject cursorObject;

	// so we can escape while drawing - enabled while drawing, disabled when done
	private EscapeKeyTarget escapeKeyTarget;

	private bool cachedLightingState;

	public Vector3? StartPressPosition = null;

	public GameGizmoLine GameGizmoLine;

	public bool HasRotatable = false;

	private void Awake()
	{
		// unity doesn't support property blocks on ui renderers, so this is a workaround
		image.material = Instantiate(image.material);
	}

	private void OnEnable()
	{
		escapeKeyTarget = GetComponent<EscapeKeyTarget>();
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		OnEscape();
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	/// <summary>
	/// Initializes it to display the document
	/// </summary>
	/// <param name="resultDoc">document to display</param>
	public void Initialize(DevSpawnerDocument resultDoc)
	{
		prefab = resultDoc.Prefab;
		Sprite toUse = prefab.GetComponentInChildren<SpriteRenderer>()?.sprite;
		if (toUse != null)
		{
			image.sprite = toUse;
			CheckAndApplyPalette();
		}

		detailText.text = "Prefab";

		titleText.text = resultDoc.Prefab.name;

		HasRotatable = prefab.GetComponent<Rotatable>();

	}

	private void UpdateMe()
	{
		if (selectedItem == this)
		{
			cursorObject.transform.position = MouseUtils.MouseToWorldPos();

			if (CommonInput.GetMouseButtonDown(0))
			{
				//Ignore spawn if pointer is hovering over GUI
				if (EventSystem.current.IsPointerOverGameObject())
				{
					return;
				}

				if (HasRotatable == false)
				{
					TrySpawn(null);
				}
				else
				{
					StartPressPosition = cursorObject.transform.position;

					GameGizmoLine = GameGizmomanager.AddNewLineStatic(null, StartPressPosition.Value.RoundToInt() , null,StartPressPosition.Value  , Color.green);
				}
			}

			if (GameGizmoLine != null && StartPressPosition != null)
			{
				GameGizmoLine.To = StartPressPosition.Value.RoundToInt()+ ((cursorObject.transform.position - StartPressPosition).Value.ToOrientationEnum()
					.ToLocalVector3());

				GameGizmoLine.UpdateMe();
			}

			if (CommonInput.GetMouseButtonUp(0) && HasRotatable && StartPressPosition != null)
			{
				GameGizmoLine.OrNull()?.Remove();
				GameGizmoLine = null;
				cursorObject.transform.position = StartPressPosition.Value;
				TrySpawn( ( MouseUtils.MouseToWorldPos() - StartPressPosition).Value.ToOrientationEnum());
				StartPressPosition = null;
			}
		}
	}

	public void OnEscape()
	{
		if (selectedItem == this)
		{
			//stop drawing
			Destroy(cursorObject);
			UIManager.IsMouseInteractionDisabled = false;
			escapeKeyTarget.enabled = false;
			selectedItem = null;
			drawingMessage.SetActive(false);
			Camera.main.GetComponent<LightingSystem>().enabled = cachedLightingState;
		}
	}

	public void OnSelected()
	{
		if (selectedItem != this)
		{
			if (selectedItem != null)
			{
				//tell the other selected one that it's time to stop
				selectedItem.OnEscape();
			}
			//just chosen to be spawned on the map. Put our object under the mouse cursor
			cursorObject = Instantiate(cursorPrefab, transform.root);
			SpriteRenderer curRend = cursorObject.GetComponent<SpriteRenderer>();
			curRend.sprite = image.sprite;

			curRend.material = prefab.GetComponentInChildren<SpriteRenderer>().sharedMaterial;
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			curRend.GetPropertyBlock(block);
			if (isPaletted)
			{
				Debug.Assert(palette != null, "Palette must not be null on paletteable objects.");
				List<Vector4> pal = palette.ConvertAll((c) => new Vector4(c.r, c.g, c.b, c.a));
				block.SetVectorArray("_ColorPalette", pal);
				block.SetInt("_IsPaletted", 1);
				block.SetInt("_PaletteSize", pal.Count);
			}
			else
			{
				block.SetInt("_IsPaletted", 0);
			}
			curRend.SetPropertyBlock(block);

			UIManager.IsMouseInteractionDisabled = true;
			escapeKeyTarget.enabled = true;
			selectedItem = this;
			drawingMessage.SetActive(true);
			cachedLightingState = Camera.main.GetComponent<LightingSystem>().enabled;
			Camera.main.GetComponent<LightingSystem>().enabled = false;
		}
	}

	private void CheckAndApplyPalette()
	{
		isPaletted = false;
		//image.material.SetInt("_IsPaletted", 0);

		if (prefab.TryGetComponent(out ItemAttributesV2 prefabAttributes))
		{
			ItemsSprites sprites = prefabAttributes.ItemSprites;
			if (sprites.IsPaletted)
			{
				palette = sprites.Palette;
				Debug.Assert(palette != null, "Palette must not be null on paletteable objects.");

				isPaletted = true;
				image.material.SetInt("_IsPaletted", 1);
				image.material.SetInt("_PaletteSize", palette.Count);
				image.material.SetColorArray("_ColorPalette", palette.ToArray());
				palette = new List<Color>(image.material.GetColorArray("_ColorPalette"));
			}
			else
			{
				palette = null;
			}
		}

		if (isPaletted == false)
		{
			image.material.SetInt("_IsPaletted", 0);
		}
	}

	/// <summary>
	/// Tries to spawn at the specified position. Lets you spawn anywhere, even impassable places. Go hog wild!
	/// </summary>
	private void TrySpawn(OrientationEnum? OrientationEnum)
	{
		Vector3Int position = cursorObject.transform.position.RoundToInt();

		if (CustomNetworkManager.IsServer)
		{
			var game = Spawn.ServerPrefab(prefab, position).GameObject;

			if (game.TryGetComponent<Stackable>(out var Stackable) && GUI_DevSpawner.Instance.StackAmount != -1)
			{
				Stackable.ServerSetAmount(GUI_DevSpawner.Instance.StackAmount);
			}

			if (game.TryGetComponent<Rotatable>(out var Rotatable) && OrientationEnum != null)
			{
				Rotatable.FaceDirection(OrientationEnum.Value);
				if (game.TryGetComponent<MonoPipe>(out var MonoPipe))
				{
					MonoPipe.RotatePipe(OrientationEnum.Value.ToPipeRotate(), false);
				}
			}

			var player = PlayerManager.LocalPlayerObject.Player();
			UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
					$"{player.Username} spawned a {prefab.name} at {position}", player.UserId);
		}
		else
		{
			DevSpawnMessage.Send(prefab, (Vector3) position, GUI_DevSpawner.Instance.StackAmount, OrientationEnum);
		}
	}
}
