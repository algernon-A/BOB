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
		protected override bool IsTree => treeCheck == null ? false : treeCheck.isChecked;


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
				replaceAllButton = UIUtils.CreateButton(this, ReplaceAllLabel, 190f, LeftWidth + (Margin * 2), ReplaceAllY);

				// Angle label and textfield.
				UILabel angleLabel = UIControls.AddLabel(this, Translations.Translate("BOB_PNL_ANG"), LeftWidth + (Margin * 2), AngleY);
				angleField = UIControls.AddTextField(this, 100f, LeftWidth + (Margin * 2), AngleY + LabelHeight, height: TextFieldHeight);

				// Offset X position.
				UILabel xLabel = UIControls.AddLabel(this, Translations.Translate("BOB_PNL_XOF"), LeftWidth + (Margin * 2), XOffsetY);
				xField = UIControls.AddTextField(this, 100f, LeftWidth + (Margin * 2), XOffsetY + LabelHeight, height: TextFieldHeight);

				// Offset Y position.
				UILabel yLabel = UIControls.AddLabel(this, Translations.Translate("BOB_PNL_YOF"), LeftWidth + (Margin * 2), YOffsetY);
				yField = UIControls.AddTextField(this, 100f, LeftWidth + (Margin * 2), YOffsetY + LabelHeight, height: TextFieldHeight);

				// Offset Z position.
				UILabel zLabel = UIControls.AddLabel(this, Translations.Translate("BOB_PNL_ZOF"), LeftWidth + (Margin * 2), ZOffsetY);
				zField = UIControls.AddTextField(this, 100f, LeftWidth + (Margin * 2), ZOffsetY + LabelHeight, height: TextFieldHeight);

				// Add checkboxes.
				propCheck = UIUtils.AddCheckBox(this, Translations.Translate("BOB_PNL_PRP"), Margin, TitleHeight);
				treeCheck = UIUtils.AddCheckBox(this, Translations.Translate("BOB_PNL_TRE"), Margin, TitleHeight + Margin + propCheck.height);

				// Probability label and textfield.
				UILabel probabilityLabel = AddUIComponent<UILabel>();
				probabilityLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), ProbabilityY);
				probabilityLabel.text = Translations.Translate("BOB_PNL_PRB");

				probabilityField = UIUtils.AddTextField(this, 190f, 30f);
				probabilityField.relativePosition = new Vector2(LeftWidth + (Margin * 2), ProbabilityY + probabilityLabel.height);

				// Name filter.
				nameFilter = UIUtils.LabelledTextField(this, Translations.Translate("BOB_FIL_NAME"));
				nameFilter.relativePosition = new Vector2(width - nameFilter.width - Margin, 40f);
				// Event handlers for name filter textbox.
				nameFilter.eventTextChanged += (control, text) =>
				{
					loadedList.rowsData = LoadedList(IsTree);
				};
				nameFilter.eventTextSubmitted += (control, text) =>
				{
					loadedList.rowsData = LoadedList(IsTree);
				};

				// Vanilla filter.
				hideVanilla = UIUtils.AddCheckBox((UIComponent)(object)this, Translations.Translate("BOB_PNL_HDV"), nameFilter.relativePosition.x, 75f);
				hideVanilla.eventCheckChanged += (control, isChecked) =>
				{
					// Filter list.
					loadedList.rowsData = LoadedList(IsTree);

					// Store state.
					ModSettings.hideVanilla = isChecked;
				};


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
