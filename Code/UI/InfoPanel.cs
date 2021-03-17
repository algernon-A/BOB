using System;
using ColossalFramework.UI;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Abstract class for building and network BOB tree/prop replacement panels.
	/// </summary>
	public abstract class BOBInfoPanel : BOBInfoPanelBase
	{
		// Component locations.
		private const float LabelHeight = 17f;
		private const float TextFieldWidth = 100f;
		private const float TextFieldHeight = 15f;
		private const float Padding = 10f;
		private const float FieldOffset = LabelHeight + TextFieldHeight + Padding;
		protected const float ProbabilityY = RevertY + 45f;
		private const float AngleY = ProbabilityY + FieldOffset;
		private const float XOffsetY = AngleY + FieldOffset;
		private const float YOffsetY = XOffsetY + FieldOffset;
		private const float ZOffsetY = YOffsetY + FieldOffset;

		// Current selections.
		protected int probability;

		// Panel components.
		protected UIButton replaceAllButton, configButton;
		protected UICheckBox treeCheck, propCheck;
		internal UITextField probabilityField;
		internal UITextField angleField, xField, yField, zField;

		// Button labels.
		protected abstract string ReplaceAllLabel { get; }


		// Trees or props?
		protected override bool IsTree => treeCheck?.isChecked ?? false;


		/// <summary>
		/// Performs initial setup 
		/// </summary>
		/// <param name="parentTransform">Parent transform</param>
		/// <param name="targetPrefabInfo">Currently selected target prefab</param>
		internal override void Setup(Transform parentTransform, PrefabInfo targetPrefabInfo)
		{
			try
			{
				// Perform basic panel setup.
				base.Setup(parentTransform, targetPrefabInfo);

				// Replace all button.
				replaceAllButton = UIControls.AddSmallerButton(this, LeftWidth + (Margin * 2), ReplaceAllY, ReplaceAllLabel, ButtonWidth);

				// Probability label and textfield.
				UILabel probabilityLabel = UIControls.AddLabel(this, LeftWidth + (Margin * 2), ProbabilityY, Translations.Translate("BOB_PNL_PRB"), textScale: 0.7f);
				probabilityField = UIControls.SmallTextField(this, LeftWidth + (Margin * 2), ProbabilityY + probabilityLabel.height, width: TextFieldWidth);

				// Angle label and textfield.
				UILabel angleLabel = UIControls.AddLabel(this, LeftWidth + (Margin * 2), AngleY, Translations.Translate("BOB_PNL_ANG"), textScale: 0.7f);
				angleField = UIControls.SmallTextField(this, LeftWidth + (Margin * 2), AngleY + angleLabel.height, width: TextFieldWidth);

				// Offset X position.
				UILabel xLabel = UIControls.AddLabel(this, LeftWidth + (Margin * 2), XOffsetY, Translations.Translate("BOB_PNL_XOF"), textScale: 0.7f);
				xField = UIControls.SmallTextField(this, LeftWidth + (Margin * 2), XOffsetY + xLabel.height, width: TextFieldWidth);

				// Offset Y position.
				UILabel yLabel = UIControls.AddLabel(this, LeftWidth + (Margin * 2), YOffsetY, Translations.Translate("BOB_PNL_YOF"), textScale: 0.7f);
				yField = UIControls.SmallTextField(this, LeftWidth + (Margin * 2), YOffsetY + yLabel.height, width: TextFieldWidth);

				// Offset Z position.
				UILabel zLabel = UIControls.AddLabel(this, LeftWidth + (Margin * 2), ZOffsetY, Translations.Translate("BOB_PNL_ZOF"), textScale: 0.7f);
				zField = UIControls.SmallTextField(this, LeftWidth + (Margin * 2), ZOffsetY + zLabel.height, width: TextFieldWidth);

				// Add checkboxes.
				propCheck = UIControls.AddCheckBox(this, Margin, TitleHeight, Translations.Translate("BOB_PNL_PRP"));
				treeCheck = UIControls.AddCheckBox(this, Margin, TitleHeight + Margin + propCheck.height, Translations.Translate("BOB_PNL_TRE"));

				// Set initial button and checkbox states.
				hideVanilla.isChecked = ModSettings.hideVanilla;
				UpdateButtonStates();

			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception setting up InfoPanel");
			}
		}


		/// <summary>
		/// Updates button states (enabled/disabled) according to current control states.
		/// </summary>
		protected override void UpdateButtonStates()
		{
			// Disable by default (selectively (re)-enable if eligible).
			replaceButton.Disable();
			replaceAllButton.Disable();
			revertButton.Disable();

			// Buttons are only enabled if a current target item is selected.
			if (CurrentTargetItem != null)
			{
				// Reversion requires a currently active replacement (for the relevant target/all-building setting).
				if (CurrentTargetItem.individualPrefab != null || CurrentTargetItem.replacementPrefab != null || CurrentTargetItem.allPrefab != null)
				{
					revertButton.Enable();
				}

				// Replacement requires a valid replacement selection.
				if (replacementPrefab != null)
				{
					replaceButton.Enable();
					replaceAllButton.Enable();
				}
			}
		}
	}
}
