﻿using Chemistry;
using UI.Core.NetUI;
using UnityEngine;

namespace UI.Objects.Medical
{
	/// <summary>
	/// DynamicEntry for ChemMaster NetTab buffer page.
	/// </summary>
	public class GUI_ChemBufferEntry : DynamicEntry
	{
		private GUI_ChemMaster chemMasterTab;
		private Reagent reagent = null;
		private float reagentAmount;

		public Reagent Reagent
		{
			get => reagent;
			set => reagent = value;
		}

		[SerializeField]
		private NetText_label reagentName = default;
		[SerializeField]
		private NetText_label reagentAmountDisplay = default;

		public void ReInit(Reagent newReagent, float amount, GUI_ChemMaster tab)
		{
			reagent = newReagent;
			reagentAmount = amount;
			chemMasterTab = tab;
			reagentName.MasterSetValue(reagent.Name);
			reagentAmountDisplay.MasterSetValue($"{reagentAmount:F2}u");
		}
		public void OpenCustomPrompt()
		{
			chemMasterTab.OpenCustomPrompt(reagent,false);
		}
		public void Transfer(float amount)
		{
			if (amount <= reagentAmount) chemMasterTab.BufferTransfer(reagent, amount);
			else chemMasterTab.BufferTransfer(reagent, reagentAmount);
		}
		public void TransferAll()
		{
			Transfer(reagentAmount);
		}
		public void Analyze(PlayerInfo player)
		{
			chemMasterTab.Analyze(reagent, player);
		}
	}
}
