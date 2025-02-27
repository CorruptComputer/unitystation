using System;
using System.Collections;
using System.Collections.Generic;
using Core.Chat;
using UnityEngine;
using UI.Chat_UI;
using static KeybindManager;

public class KeyboardInputManager : MonoBehaviour
{
	public static KeyboardInputManager Instance;
	private KeybindManager keybindManager => KeybindManager.Instance;

	public enum KeyEventType
	{
		Down,
		Up,
		Hold
	}
	void Awake ()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}


	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void UpdateMe()
	{
		if (!UIManager.IsInputFocus)
		{
			// Perform escape key action
			if (CommonInput.GetKeyDown(KeyCode.Escape))
			{
				EscapeKeyTarget.HandleEscapeKey();
			}

			// Only check for keyboard input once in-game
			if (GameData.IsInGame && Mirror.NetworkClient.active)
			{
				CheckInGameKeybinds();
			}
		}
	}

	/// <summary>
	/// Checks all keybinds which are only used in-game
	/// </summary>
	void CheckInGameKeybinds()
	{
		var triggeredKeybinds = new Dictionary<KeyAction, KeyCombo>();
		// Perform the checks for all key actions which have functions defined here
		foreach (KeyValuePair<KeyAction, DualKeyCombo> entry in keybindManager.userKeybinds)
		{
			if (!keyActionFunctions.ContainsKey(entry.Key)) continue;
			if (CheckComboEvent(entry.Value.PrimaryCombo))
			{
				triggeredKeybinds.Add(entry.Key, entry.Value.PrimaryCombo);
			}
			if(CheckComboEvent(entry.Value.SecondaryCombo))
			{
				triggeredKeybinds.Add(entry.Key, entry.Value.SecondaryCombo);
			}
		}
		if (triggeredKeybinds.Count == 0)
			return;

		foreach (KeyValuePair<KeyAction, KeyCombo> entry in triggeredKeybinds)
		{
			bool shouldTrigger = true;
			foreach (KeyValuePair<KeyAction, KeyCombo> loopentry in triggeredKeybinds)
			{
				if (entry.Key == loopentry.Key)
					continue;
				if (KeyCombo.ShareKey(entry.Value, loopentry.Value))
				{
					if (KeyCombo.TotalKeys(entry.Value) <= KeyCombo.TotalKeys(loopentry.Value))
					{
						shouldTrigger = false;
					}
				}
			}
			if (shouldTrigger)
			{
				// Call the function associated with the KeyAction enum
				keyActionFunctions[entry.Key]();
			}
		}

	}

	/// <summary>
	/// Check if either of the key combos for the selected action have been pressed
	/// </summary>
	/// <param name="moveAction">The action to check</param>
	/// <param name="keyEventType">The type of key event to check for</param>
	public static bool CheckMoveAction(MoveAction moveAction)
	{
		return Instance.CheckKeyAction((KeyAction) moveAction, KeyEventType.Hold);
	}

	/// <summary>
	/// Check if either of the key combos for the selected action have been pressed
	/// </summary>
	/// <param name="keyAction">The action to check</param>
	/// <param name="keyEventType">The type of key event to check for</param>
	public bool CheckKeyAction(KeyAction keyAction, KeyEventType keyEventType = KeyEventType.Down)
	{
		DualKeyCombo action = keybindManager.userKeybinds[keyAction];
		return CheckComboEvent(action.PrimaryCombo, keyEventType) || CheckComboEvent(action.SecondaryCombo, keyEventType);
	}

	/// <summary>
	/// Checks if the player has pressed any movement keys
	/// </summary>
	/// <param name="keyEventType">Key event to check for like down, up or hold</param>
	public static bool IsMovementPressed(KeyEventType keyEventType = KeyEventType.Down)
	{
		if (UIManager.IsInputFocus) return false;

		return Instance.CheckKeyAction(KeyAction.MoveUp,   keyEventType) || Instance.CheckKeyAction(KeyAction.MoveDown,  keyEventType) ||
		       Instance.CheckKeyAction(KeyAction.MoveLeft, keyEventType) || Instance.CheckKeyAction(KeyAction.MoveRight, keyEventType);
	}

	/// <summary>
	/// Check if enter (the return or numpad enter keys) has been pressed
	/// </summary>
	public static bool IsEnterPressed()
	{
		return CommonInput.GetKeyDown(KeyCode.Return) || CommonInput.GetKeyDown(KeyCode.KeypadEnter);
	}

	/// <summary>
	/// Check if escape has been pressed
	/// </summary>
	public static bool IsEscapePressed()
	{
		return CommonInput.GetKeyDown(KeyCode.Escape);
	}

	/// <summary>
	/// Check if the left or right control or command keys have been pressed
	/// </summary>
	public static bool IsControlPressed()
	{
		return CommonInput.GetKey(KeyCode.LeftControl) || CommonInput.GetKey(KeyCode.RightControl) ||
		       CommonInput.GetKey(KeyCode.LeftCommand) || CommonInput.GetKey(KeyCode.RightCommand);
	}

	/// <summary>
	/// Checks if the left or right shift key has been pressed
	/// </summary>
	public static bool IsShiftPressed()
	{
		return CommonInput.GetKey(KeyCode.LeftShift) || CommonInput.GetKey(KeyCode.RightShift);
	}

	/// <summary>
	/// Checks if the left or right alt key has been pressed (AltGr sends RightAlt)
	/// </summary>
	public static bool IsAltActionKeyPressed()
	{
		return Instance.CheckKeyAction( KeyAction.InteractionModifier, KeyEventType.Down) || Instance.CheckKeyAction( KeyAction.InteractionModifier, KeyEventType.Hold);
	}

	/// <summary>
	/// Checks if the middle mouse button has been pressed
	/// </summary>
	public static bool IsMiddleMouseButtonPressed()
	{
		return CommonInput.GetKeyDown(KeyCode.Mouse2);
	}

	private bool CheckComboEvent(KeyCombo keyCombo, KeyEventType keyEventType = KeyEventType.Down)
	{
		if (keyCombo.ModKey1 != KeyCode.None && !CommonInput.GetKey(keyCombo.ModKey1))
		{
			return false;
		}
		if (keyCombo.ModKey2 != KeyCode.None && !CommonInput.GetKey(keyCombo.ModKey2))
		{
			return false;
		}

		switch (keyEventType)
		{
			case KeyEventType.Down:
				return CommonInput.GetKeyDown(keyCombo.MainKey);
			case KeyEventType.Up:
				return CommonInput.GetKeyUp(keyCombo.MainKey);
			case KeyEventType.Hold:
				return CommonInput.GetKey(keyCombo.MainKey);
			default:
				return CommonInput.GetKeyDown(keyCombo.MainKey);
		}
	}

	private readonly Dictionary<KeyAction, System.Action> keyActionFunctions = new Dictionary<KeyAction, System.Action>
	{
		// Actions
		{ KeyAction.ActionThrow,	() => { UIManager.Action.Throw(); }},
		{ KeyAction.ActionDrop,		() => {	UIManager.Action.Drop(); }},
		{ KeyAction.ActionResist,	() => { UIManager.Action.Resist(); }},
		{ KeyAction.ActionStopPull, () => { UIManager.Action.StopPulling(); }},
		{ KeyAction.ToggleWalkRun,   () => { UIManager.Intent.OnClickRunWalk(); }},

		{  KeyAction.Point, 		() => { MouseInputController.Point(); }},
		{  KeyAction.HandSwap, 		() => { HandsController.SwapHand(); }},
		{  KeyAction.HandActivate,	() => { HandsController.Activate(); }},
		{  KeyAction.HandEquip, 	() => {  HandsController.Equip(); }},

		// Intents
		{ KeyAction.IntentLeft,		() => { UIManager.Intent.CycleIntent(true); }},
		{ KeyAction.IntentRight, 	() => { UIManager.Intent.CycleIntent(false); }},
		{ KeyAction.IntentHelp, 	() => { UIManager.Intent.SetIntent(Intent.Help); }},
		{ KeyAction.IntentDisarm,	() => { UIManager.Intent.SetIntent(Intent.Disarm); }},
		{ KeyAction.IntentGrab, 	() => { UIManager.Intent.SetIntent(Intent.Grab); }},
		{ KeyAction.IntentHarm, 	() => { UIManager.Intent.SetIntent(Intent.Harm); }},

		// Chat
		{ KeyAction.ChatLocal,		() => { ChatUI.Instance.OpenChatWindow(ChatChannel.Local); }},
		{ KeyAction.ChatRadio,		() => { ChatUI.Instance.OpenChatWindow(ChatChannel.Common); }},
		{ KeyAction.ChatOOC,		() => { ChatUI.Instance.OpenChatWindow(ChatChannel.OOC); }},
		{ KeyAction.ToggleHelp,    () => { ChatUI.Instance.OnHelpButton(); }},
		{ KeyAction.ToggleAHelp,    () => { ChatUI.Instance.OnAdminHelpButton(); }},
		{ KeyAction.ToggleMHelp,    () => { ChatUI.Instance.OnMentorHelpButton(); }},

		// Body part selection
		{ KeyAction.TargetHead,		() => { UIManager.ZoneSelector.CycleZones(BodyPartType.Head, BodyPartType.Eyes, BodyPartType.Mouth); }},
		{ KeyAction.TargetChest,	() => { UIManager.ZoneSelector.CycleZones(BodyPartType.Chest, BodyPartType.Groin); }},
		{ KeyAction.TargetLeftArm,  () => { UIManager.ZoneSelector.CycleZones(BodyPartType.LeftArm, BodyPartType.LeftHand); }},
		{ KeyAction.TargetRightArm, () => { UIManager.ZoneSelector.CycleZones(BodyPartType.RightArm, BodyPartType.RightHand); }},
		{ KeyAction.TargetLeftLeg,  () => { UIManager.ZoneSelector.CycleZones(BodyPartType.LeftLeg, BodyPartType.LeftFoot); }},
		{ KeyAction.TargetRightLeg, () => { UIManager.ZoneSelector.CycleZones(BodyPartType.RightLeg, BodyPartType.RightFoot); }},
		//{ KeyAction.TargetGroin, 	() => { UIManager.ZoneSelector.CycleZone(BodyPartType.Groin); }},

		// UI
		//{ KeyAction.OpenBackpack, 	() => { UIManager.Instance.panelHudBottomController.backpackItemSlot.TryItemInteract(swapIfEmpty: false); }},
		{ KeyAction.OpenBackpack, 	() => { PlayerManager.LocalPlayerScript.DynamicItemStorage.TryItemInteract(NamedSlot.back, false); }},
		{ KeyAction.OpenPDA, 		() => { PlayerManager.LocalPlayerScript.DynamicItemStorage.TryItemInteract(NamedSlot.id, false); }},
		{ KeyAction.OpenBelt, 		() => {  PlayerManager.LocalPlayerScript.DynamicItemStorage.TryItemInteract(NamedSlot.belt, false);}},

		{ KeyAction.PocketOne, 		() => { PlayerManager.LocalPlayerScript.DynamicItemStorage.TryItemInteract(NamedSlot.storage01);}},
		{ KeyAction.PocketTwo, 		() => { PlayerManager.LocalPlayerScript.DynamicItemStorage.TryItemInteract(NamedSlot.storage02);}},
		{ KeyAction.PocketThree, 	() => { PlayerManager.LocalPlayerScript.DynamicItemStorage.TryItemInteract(NamedSlot.suitStorage); }},
		{ KeyAction.HideUi,         () => { UIManager.Instance.ToggleUiVisibility(); }},
		{ KeyAction.EmoteWindowUI,         () => { EmoteActionManager.Instance.CheckForInputForEmoteWindow(); }},
	};
}