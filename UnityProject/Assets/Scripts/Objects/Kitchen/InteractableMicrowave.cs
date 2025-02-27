using System;
using System.Linq;
using UnityEngine;


namespace Objects.Kitchen
{
	/// <summary>
	/// Allows Microwave to be interacted with. Player can put food in the microwave to cook it.
	/// The microwave can be interacted with to, for example, check the remaining time.
	/// </summary>
	[RequireComponent(typeof(Microwave))]
	public class InteractableMicrowave : MonoBehaviour, IExaminable, ICheckedInteractable<PositionalHandApply>,
			IRightClickable, ICheckedInteractable<ContextMenuApply>
	{
		const int TIMER_INCREMENT = 5; // Seconds

		[SerializeField]
		[Tooltip("The GameObject in this hierarchy that contains the SpriteClickRegion component defining the microwave's door.")]
		private SpriteClickRegion doorRegion = default;

		[SerializeField]
		[Tooltip("The GameObject in this hierarchy that contains the SpriteClickRegion component defining the microwave's power button.")]
		private SpriteClickRegion powerRegion = default;

		[SerializeField]
		[Tooltip("The GameObject in this hierarchy that contains the SpriteClickRegion component defining the microwave's add time button.")]
		private SpriteClickRegion timerAddRegion = default;

		[SerializeField]
		[Tooltip("The GameObject in this hierarchy that contains the SpriteClickRegion component defining the microwave's remove time button.")]
		private SpriteClickRegion timerRemoveRegion = default;

		private Microwave microwave;

		private void Start()
		{
			microwave = GetComponent<Microwave>();
		}

		public string Examine(Vector3 worldPos = default)
		{
			var contents = microwave.HasContents
					? string.Join(", ", microwave.Slots.Where(slot => slot.IsOccupied).Select(slot => $"<b>{slot.ItemObject.ExpensiveName()}</b>"))
					: "<b>nothing</b>";

			return $"The microwave is currently <b>{microwave.CurrentState.StateMsgForExamine}</b>. " +
					$"You see {contents} inside. " +
					$"There {(microwave.StorageSize == 1 ? "is <b>one</b> slot" : $"are <b>{microwave.StorageSize}</b> slots")} available. "+
					$"The timer shows <b>{Math.Ceiling(microwave.MicrowaveTimer)}</b> seconds remaining.";
		}

		#region Interaction-PositionalHandApply

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) 
				|| Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar)) == false;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (doorRegion.Contains(interaction.WorldPositionTarget))
			{
				microwave.RequestDoorInteraction(interaction);
			}
			else if (powerRegion.Contains(interaction.WorldPositionTarget))
			{
				microwave.RequestToggleActive();
			}
			else if (timerAddRegion.Contains(interaction.WorldPositionTarget))
			{
				microwave.RequestAddTime(TIMER_INCREMENT);
			}
			else if (timerRemoveRegion.Contains(interaction.WorldPositionTarget))
			{
				microwave.RequestAddTime(-TIMER_INCREMENT);
			}
		}

		#endregion Interaction-PositionalHandApply

		#region Interaction-ContextMenu

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();

			var activateInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "ToggleActive");
			if (!WillInteract(activateInteraction, NetworkSide.Client)) return result;
			result.AddElement("Activate", () => ContextMenuOptionClicked(activateInteraction));

			var ejectInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "ToggleDoor");
			result.AddElement("Toggle Door", () => ContextMenuOptionClicked(ejectInteraction));

			var addTimeInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "AddTime");
			result.AddElement("Add 5 Sec", () => ContextMenuOptionClicked(addTimeInteraction));

			var removeTimeInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "RemoveTime");
			result.AddElement("Take 5 Sec", () => ContextMenuOptionClicked(removeTimeInteraction));

			return result;
		}

		private void ContextMenuOptionClicked(ContextMenuApply interaction)
		{
			InteractionUtils.RequestInteract(interaction, this);
		}

		public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(ContextMenuApply interaction)
		{
			switch (interaction.RequestedOption)
			{
				case "ToggleActive":
					microwave.RequestToggleActive();
					break;
				case "ToggleDoor":
					microwave.RequestDoorInteraction();
					break;
				case "AddTime":
					microwave.RequestAddTime(TIMER_INCREMENT);
					break;
				case "RemoveTime":
					microwave.RequestAddTime(-TIMER_INCREMENT);
					break;
			}
		}

		#endregion Interaction-ContextMenu
	}
}
