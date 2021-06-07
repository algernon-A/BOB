using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
	/// <summary>
	/// Prop row fastlist item for loaded props/trees.
	/// </summary>
	public class UILoadedPropRow : UIPropRow
	{
		/// <summary>
		/// Called when this item is selected.
		/// </summary>
		public override void UpdateSelection()
		{
			// Update currently selected replacement prefab.
			InfoPanelManager.Panel.ReplacementPrefab = thisPrefab;
		}
	}


	/// <summary>
	/// Prop row fastlist item for building props/trees.
	/// </summary>
	public class UIPrefabPropRow : UIPropRow
	{
		/// <summary>
		/// Called when this item is selected.
		/// </summary>
		public override void UpdateSelection()
		{
			// Update currently selected target prefab.
			InfoPanelManager.Panel.CurrentTargetItem = thisItem;
		}
	}


	/// <summary>
	/// Prop row fastlist item for loaded props/trees for random setup.
	/// </summary>
	public class UILoadedRandomPropRow : UIPropRow
	{
		/// <summary>
		/// Called when this item is selected.
		/// </summary>
		public override void UpdateSelection()
		{
			// Update currently selected loaded prefab.
			BOBRandomPanel.Panel.SelectedLoadedPrefab = thisPrefab;
		}
	}


	/// <summary>
	/// Prop row fastlist item for random prop/tree variations.
	/// </summary>
	public class UIRandomComponentRow : UIPropRow
	{
		private BOBVariant thisVariant;
		private UILabel probLabel;
		private UISprite lockSprite;

		/// <summary>
		/// Called when this item is selected.
		/// </summary>
		public override void UpdateSelection()
		{
			// Update currently selected variation prefab.
			BOBRandomPanel.Panel.SelectedVariation = thisVariant;
		}

		public override void Display(object data, bool isRowOdd)
		{
			thisVariant = data as BOBVariant;

			// Perform initial setup for new rows.

			if (nameLabel == null)
			{
				isVisible = true;
				canFocus = true;
				isInteractive = true;
				width = parent.width;
				height = RowHeight;

				// Add object name label.
				nameLabel = AddUIComponent<UILabel>();
				nameLabel.width = this.width - 10f;
				nameLabel.textScale = TextScale;

				// Add index text label.
				indexLabel = AddUIComponent<UILabel>();
				indexLabel.width = IndexWidth;
				indexLabel.textScale = TextScale;
				indexLabel.relativePosition = new Vector2(IndexLabelX, PaddingY);
			}

			// Set label position
			nameLabel.relativePosition = new Vector2(labelX, PaddingY);

			// Set initial background as deselected state.
			Deselect(isRowOdd);

			nameLabel.text = PrefabLists.GetDisplayName(thisVariant.prefab);

			if (lockSprite == null)
			{
				lockSprite = AddUIComponent<UISprite>();

				lockSprite.size = new Vector2(17f, 17f);
				lockSprite.relativePosition = new Vector2(width - 20f, 3f);
				lockSprite.atlas = TextureUtils.LoadSpriteAtlas("bob_padlock_small");
				SetLockSprite();

				lockSprite.eventClicked += (control, clickEvent) =>
				{
					if (thisVariant != null)
                    {
						thisVariant.probLocked = !thisVariant.probLocked;
						SetLockSprite();
                    }
				};
			}

			if (probLabel == null)
			{
				probLabel = AddUIComponent<UILabel>();
			}
			probLabel.text = (thisVariant?.probability.ToString() ?? "0") + "%";
			probLabel.relativePosition = new Vector2(width - 20f - 5f - probLabel.width, 3f);
		}


		private void SetLockSprite()
		{
			if (thisVariant != null && lockSprite != null)
			{
				if (thisVariant.probLocked)
				{
					lockSprite.spriteName = "disabled";
				}
				else
				{
					lockSprite.spriteName = "pressed";
				}
			}
		}
	}
}
