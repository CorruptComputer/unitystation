﻿using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Mirror;
using Player.Movement;
using UnityEngine.Serialization;

namespace Objects
{
	/// <summary>
	/// Behavior for an object which has a different sprite for each direction it is facing and changes
	/// facing when Directional tells it to. Also can become "occupied" by a player - modifying how it is drawn
	/// so that it appears that a player is occupying it.
	///
	/// Initial orientation should be set in Directional.
	/// </summary>
	[RequireComponent(typeof(Rotatable))]
	[RequireComponent(typeof(Integrity))]
	[ExecuteInEditMode]
	public class OccupiableDirectionalSprite : NetworkBehaviour
	{
		[Header("Base Sprites")]
		[Tooltip("Base sprite when facing right")]
		[FormerlySerializedAs("s_right")]
		public Sprite Right;
		[Tooltip("Base sprite when facing down")]
		[FormerlySerializedAs("s_down")]
		public Sprite Down;
		[Tooltip("Base sprite when facing left")]
		[FormerlySerializedAs("s_left")]
		public Sprite Left;
		[Tooltip("Base sprite when facing up")]
		[FormerlySerializedAs("s_up")]
		public Sprite Up;

		[Tooltip("sprite renderer on which to render the base sprite")]
		public SpriteRenderer spriteRenderer;

		[Header("Sprites Over Player When Occupied")]
		[FormerlySerializedAs("s_right_front")]
		[Tooltip("Sprite to render in front of player when occupied facing right")]
		public Sprite OccupiedRight;
		[FormerlySerializedAs("s_down_front")]
		[Tooltip("Sprite to render in front of player when occupied facing down")]
		public Sprite OccupiedDown;
		[FormerlySerializedAs("s_left_front")]
		[Tooltip("Sprite to render in front of player when occupied facing left")]
		public Sprite OccupiedLeft;
		[FormerlySerializedAs("s_up_front")]
		[Tooltip("Sprite to render in front of player when occupied facing up")]
		public Sprite OccupiedUp;

		[Tooltip("sprite renderer on which to render the front sprites")]
		public SpriteRenderer spriteRendererFront;

		//set to NetId.Empty when unoccupied.
		[SyncVar(hook = nameof(SyncOccupantNetId))]
		private uint occupantNetId;

		/// <summary>
		/// Current occupant. Valid on client / server. Null if no occupant.
		/// </summary>
		public GameObject Occupant => occupant;
		//cached occupant for fast lookup.
		private GameObject occupant;
		public bool HasOccupant => occupant != null;

		private const string BASE_SPRITE_LAYER_NAME = "Machines";
		private const string FRONT_SPRITE_LAYER_NAME = "OverPlayers";

		private Rotatable rotatable;

		// The Cached PlayerScript of the Buckled player
		private PlayerScript occupantPlayerScript;
		/// <summary>
		/// PlayerScript of the buckled player, null if no buckled player.
		/// </summary>
		public PlayerScript OccupantPlayerScript => occupantPlayerScript;

		private void EnsureInit()
		{
			if (rotatable != null || gameObject == null) return;
			rotatable = GetComponent<Rotatable>();
			rotatable.OnRotationChange.AddListener(OnDirectionChanged);
			OnDirectionChanged(rotatable.CurrentDirection);
			GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
		}

		private void OnDisable()
		{
			rotatable.OnRotationChange.RemoveListener(OnDirectionChanged);
		}

		private void OnWillDestroyServer(DestructionInfo info)
		{
			//release the player
			if (HasOccupant)
			{
				var playerMoveAtPosition =
					MatrixManager.GetAt<MovementSynchronisation>(this.gameObject.AssumedWorldPosServer().RoundToInt(), true)
					.FirstOrDefault(pm => pm.IsBuckled);

				if (playerMoveAtPosition != null)
				{
					playerMoveAtPosition.Unbuckle();
				}
			}

		}

		// Only runs in editor - useful for updating the sprite direction
		// when the initial direction is altered via inspector.
		private void OnValidate()
		{
			if (Application.isPlaying) return;
			OnEditorDirectionChange();
		}



		public override void OnStartClient()
		{
			EnsureInit();
			//must invoke this because SyncVar hooks are not called on client init
			SyncOccupantNetId(occupantNetId, occupantNetId);
			OnDirectionChanged(rotatable.CurrentDirection);
		}

		public override void OnStartServer()
		{
			EnsureInit();
			OnDirectionChanged(rotatable.CurrentDirection);
		}

		public void OnEditorDirectionChange()
		{
			if (this == null) return;
			if (rotatable == null) rotatable = GetComponent<Rotatable>();
			SetDirectionalSprite(rotatable.CurrentDirection);
		}

		private void OnDirectionChanged(OrientationEnum newDir)
		{
			SetDirectionalSprite(newDir);
			UpdateFrontSprite();
			EnsureSpriteLayer();
		}

		private void SetDirectionalSprite(OrientationEnum orientation)
		{
			if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

			if (orientation == OrientationEnum.Up_By0) spriteRenderer.sprite = Up;
			else if (orientation == OrientationEnum.Down_By180) spriteRenderer.sprite = Down;
			else if (orientation == OrientationEnum.Left_By90) spriteRenderer.sprite = Left;
			else spriteRenderer.sprite = Right;
		}

		// Updates the sprite that's drawn over the occupant when the occupant is buckled in (e.g. the seatbelt)
		private void UpdateFrontSprite()
		{
			if (spriteRendererFront)
			{
				if (HasOccupant)
				{
					if (rotatable.CurrentDirection == OrientationEnum.Up_By0)
					{
						spriteRendererFront.sprite = OccupiedUp;
					}
					else if (rotatable.CurrentDirection == OrientationEnum.Down_By180)
					{
						spriteRendererFront.sprite = OccupiedDown;
					}
					else if (rotatable.CurrentDirection == OrientationEnum.Left_By90)
					{
						spriteRendererFront.sprite = OccupiedLeft;
					}
					else
					{
						spriteRendererFront.sprite = OccupiedRight;
					}
				}
				else
					spriteRendererFront.sprite = null;
			}
		}

		/// <summary>
		/// Set the occupant of this object (also indicate if the object should render itself as if it is occupied or vacant).
		/// Pass NetId.Empty to set empty occupant.
		/// </summary>
		[Server]
		public void SetOccupant(uint occupant)
		{
			SyncOccupantNetId(occupantNetId, occupant);
		}

		//syncvar hook for occupant
		private void SyncOccupantNetId(uint occupantOldValue, uint occupantNewValue)
		{
			EnsureInit();
			occupantNetId = occupantNewValue;
			occupant = NetworkUtils.FindObjectOrNull(occupantNetId);

			if (occupant != null)
			{
				occupantPlayerScript = occupant.GetComponent<PlayerScript>();
			}
			else
			{
				occupantPlayerScript = null;
			}

			UpdateFrontSprite();
			EnsureSpriteLayer();
		}

		//ensures we are rendering in the correct sprite layer
		private void EnsureSpriteLayer()
		{
			if (rotatable.CurrentDirection == OrientationEnum.Up_By0 && HasOccupant)
			{
				spriteRenderer.sortingLayerName = FRONT_SPRITE_LAYER_NAME;
			}
			else
			{
				//restore original layer
				if (BASE_SPRITE_LAYER_NAME != null)
				{
					spriteRenderer.sortingLayerName = BASE_SPRITE_LAYER_NAME;
				}
			}
		}
	}
}
