using NPCs.Common;
using NPCs.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static itemdatabase;

namespace NPCs.Trading
{
	/// <summary>
	/// Worldspace billboard displaying the trader's inventory for selection.
	/// Positioned to the left of the trader.
	/// </summary>
	internal class TraderBillboard : MonoBehaviour
	{
		private WorldspaceInteractiveDisplay _display;
		private TraderInventory _inventory;
		private TraderPersonality _personality;
		private Func<ItemData, float> _valueResolver;

		private TextMeshProUGUI _totalLabel;
		private List<TextMeshProUGUI> _quantityLabels = new List<TextMeshProUGUI>();
		private List<Button> _minusButtons = new List<Button>();
		private List<Button> _plusButtons = new List<Button>();
		private List<int> _selectedQuantities = new List<int>();

		private float _totalSelected = 0f;

		/// <summary>
		/// Items currently selected by the player, keyed by prefab with quantity and total value.
		/// </summary>
		public Dictionary<GameObject, (int Quantity, float TotalValue)> SelectedItems { get; private set; }
			= new Dictionary<GameObject, (int, float)>();

		/// <summary>
		/// Fired when the selection changes.
		/// </summary>
		public event System.Action OnSelectionChanged;

		public void Initialise(TraderInventory inventory, TraderPersonality personality, Func<ItemData, float> valueResolver)
		{
			_inventory = inventory;
			_personality = personality;
			_valueResolver = valueResolver;

			_display = gameObject.AddComponent<WorldspaceInteractiveDisplay>();
			_display.SetPosition(new Vector3(-1.25f, 0f, 0f));
			_display.SetSize(new Vector2(550f, 550f));
			_display.Init();
		}

		/// <summary>
		/// Shows the billboard.
		/// </summary>
		public void Show()
		{
			_display.Show();
		}

		/// <summary>
		/// Hides the billboard.
		/// </summary>
		public void Hide()
		{
			_display.Hide();
		}

		/// <summary>
		/// Rebuilds the billboard layout from current inventory.
		/// </summary>
		public void Build()
		{
			_display.Clear();
			_selectedQuantities.Clear();
			_quantityLabels.Clear();
			_minusButtons.Clear();
			_plusButtons.Clear();
			SelectedItems.Clear();
			_totalSelected = 0f;

			_display.CreateLabel("Their offer", new RectPercent(50f, 5f, 90f, 8f));

			float yOffset = 15f;
			float rowHeight = 8f;

			for (int i = 0; i < _inventory.Items.Count; i++)
			{
				int index = i;
				var inventoryItem = _inventory.Items.ElementAt(i);
				GameObject item = inventoryItem.Key;
				ItemData data = inventoryItem.Value.Item1;
				int maxQuantity = inventoryItem.Value.Item2;
				float unitValue = _valueResolver(data);

				_selectedQuantities.Add(0);

				// Item name and max stock.
				_display.CreateLabel($"{data.DisplayName} (x{maxQuantity})", new RectPercent(30f, yOffset, 40f, rowHeight));

				// Minus button.
				var minusBtn = _display.CreateButton("-", $"Remove {data.DisplayName}", new RectPercent(65f, yOffset, 8f, rowHeight), () => AdjustQuantity(index, -1));
				minusBtn.interactable = false;
				_minusButtons.Add(minusBtn);

				// Quantity label.
				var quantityLabel = _display.CreateLabel("x0", new RectPercent(73f, yOffset, 10f, rowHeight));
				_quantityLabels.Add(quantityLabel);

				// Plus button.
				var plusBtn = _display.CreateButton("+", $"Add {data.DisplayName}", new RectPercent(81f, yOffset, 8f, rowHeight), () => AdjustQuantity(index, 1));
				_plusButtons.Add(plusBtn);

				// Unit value.
				_display.CreateLabel($"{unitValue}g", new RectPercent(92f, yOffset, 20f, rowHeight));

				yOffset += rowHeight + 1f;
			}

			_totalLabel = _display.CreateLabel("Selected: 0g", new RectPercent(50f, 88f, 90f, 8f));
		}

		private void AdjustQuantity(int index, int delta)
		{
			if (index >= _inventory.Items.Count) return;

			var inventoryItem = _inventory.Items.ElementAt(index);
			GameObject item = inventoryItem.Key;
			ItemData data = inventoryItem.Value.Item1;
			int maxQuantity = inventoryItem.Value.Item2;
			float unitValue = _valueResolver(data);

			int current = _selectedQuantities[index];
			int updated = Mathf.Clamp(current + delta, 0, maxQuantity);
			if (updated == current) return;

			_selectedQuantities[index] = updated;

			// Update selected items.
			if (updated == 0)
				SelectedItems.Remove(item);
			else
				SelectedItems[item] = (updated, unitValue * updated);

			// Recalculate total.
			_totalSelected = SelectedItems.Values.Sum(e => e.TotalValue);

			// Update quantity label.
			_quantityLabels[index].text = $"x{updated}";

			// Update button interactability.
			_minusButtons[index].interactable = updated > 0;
			_plusButtons[index].interactable = updated < maxQuantity;

			UpdateTotalLabel();
			OnSelectionChanged?.Invoke();
		}

		private void UpdateTotalLabel()
		{
			if (_totalLabel != null)
				_totalLabel.text = $"Selected: {Maths.RoundToNearestHalf(_totalSelected)}g";
		}
	}
}