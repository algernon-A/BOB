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
		protected const float ProbabilityY = 95f;
		private const float LabelHeight = 18f;
		private const float TextFieldWidth = 100f;
		private const float TextFieldHeight = 20f;
		private const float Padding = 10f;
		private const float FieldOffset = LabelHeight + TextFieldHeight + Padding;
		private const float AngleY = 365f;
		private const float XOffsetY = AngleY + FieldOffset;
		private const float YOffsetY = XOffsetY + FieldOffset;
		private const float ZOffsetY = YOffsetY + FieldOffset;

		// Current selections.
		protected int probability;

		// Panel components.
		protected UIButton replaceAllButton;
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

				// Get text maximum width.
				float textWidth = MiddleWidth - (Margin * 2);

				// Replace all button.
				replaceAllButton = UIControls.AddButton(this, LeftWidth + (Margin * 2), ReplaceAllY, ReplaceAllLabel, 190f);

				// Angle label and textfield.
				UILabel angleLabel = UIControls.AddLabel(this, LeftWidth + (Margin * 2), AngleY, Translations.Translate("BOB_PNL_ANG"), textWidth);
				angleField = UIControls.AddTextField(this, LeftWidth + (Margin * 2), AngleY + LabelHeight, width: TextFieldWidth);

				// Offset X position.
				UILabel xLabel = UIControls.AddLabel(this, LeftWidth + (Margin * 2), XOffsetY, Translations.Translate("BOB_PNL_XOF"), textWidth);
				xField = UIControls.AddTextField(this, LeftWidth + (Margin * 2), XOffsetY + LabelHeight, width: TextFieldWidth);

				// Offset Y position.
				UILabel yLabel = UIControls.AddLabel(this, LeftWidth + (Margin * 2), YOffsetY, Translations.Translate("BOB_PNL_YOF"), textWidth);
				yField = UIControls.AddTextField(this, LeftWidth + (Margin * 2), YOffsetY + LabelHeight, width: TextFieldWidth);

				// Offset Z position.
				UILabel zLabel = UIControls.AddLabel(this, LeftWidth + (Margin * 2), ZOffsetY, Translations.Translate("BOB_PNL_ZOF"), textWidth);
				zField = UIControls.AddTextField(this, LeftWidth + (Margin * 2), ZOffsetY + LabelHeight, width: TextFieldWidth);

				// Add checkboxes.
				propCheck = UIControls.AddCheckBox(this, Margin, TitleHeight, Translations.Translate("BOB_PNL_PRP"));
				treeCheck = UIControls.AddCheckBox(this, Margin, TitleHeight + Margin + propCheck.height, Translations.Translate("BOB_PNL_TRE"));

				// Probability label and textfield.
				UILabel probabilityLabel = AddUIComponent<UILabel>();
				probabilityLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), ProbabilityY);
				probabilityLabel.text = Translations.Translate("BOB_PNL_PRB");

				probabilityField = UIControls.AddTextField(this, LeftWidth + (Margin * 2), ProbabilityY + probabilityLabel.height, width: TextFieldWidth);


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
			if (currentTargetItem != null)
			{
				// Reversion requires a currently active replacement (for the relevant target/all-building setting).
				if (currentTargetItem.individualPrefab != null || currentTargetItem.replacementPrefab != null || currentTargetItem.allPrefab != null)
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
