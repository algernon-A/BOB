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
				UILabel angleLabel = AddUIComponent<UILabel>();
				angleLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), 367f);
				angleLabel.text = Translations.Translate("BOB_PNL_ANG");
				angleField = UIUtils.AddTextField(this, 100f, 30f);
				angleField.relativePosition = new Vector2(LeftWidth + (Margin * 2) + 90f, 360f);

				// Offset X position.
				UILabel xLabel = AddUIComponent<UILabel>();
				xLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), 407f);
				xLabel.text = Translations.Translate("BOB_PNL_XOF");
				xField = UIUtils.AddTextField(this, 100f, 30f);
				xField.relativePosition = new Vector2(LeftWidth + (Margin * 2) + 90f, 400f);

				// Offset Y position.
				UILabel yLabel = AddUIComponent<UILabel>();
				yLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), 447f);
				yLabel.text = Translations.Translate("BOB_PNL_YOF");
				yField = UIUtils.AddTextField(this, 100f, 30f);
				yField.relativePosition = new Vector2(LeftWidth + (Margin * 2) + 90f, 440f);

				// Offset Z position.
				UILabel zLabel = AddUIComponent<UILabel>();
				zLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), 487f);
				zLabel.text = Translations.Translate("BOB_PNL_ZOF");
				zField = UIUtils.AddTextField(this, 100f, 30f);
				zField.relativePosition = new Vector2(LeftWidth + (Margin * 2) + 90f, 480f);

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
			catch (Exception exception)
			{
				Debugging.LogException(exception);
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
