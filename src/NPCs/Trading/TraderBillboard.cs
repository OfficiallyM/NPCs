using NPCs.Common;
using NPCs.Utilities;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

		private TextMeshProUGUI _totalLabel;
		private List<TextMeshProUGUI> _itemValueLabels = new List<TextMeshProUGUI>();
		private List<bool> _selected = new List<bool>();
		private List<GameObject> _itemRows = new List<GameObject>();
		private List<Button> _toggleButtons = new List<Button>();

		private float _totalSelected = 0f;

		/// <summary>
		/// Items currently selected by the player, with their resolved values.
		/// </summary>
		public Dictionary<GameObject, float> SelectedItems { get; private set; } = new Dictionary<GameObject, float>();

		/// <summary>
		/// Fired when the selection changes.
		/// </summary>
		public event System.Action OnSelectionChanged;

		public void Initialise(TraderInventory inventory, TraderPersonality personality)
		{
			_inventory = inventory;
			_personality = personality;

			_display = gameObject.AddComponent<WorldspaceInteractiveDisplay>();
			_display.SetPosition(new Vector3(-1.25f, 0f, 0f));
			_display.SetSize(new Vector2(500f, 550f));
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
			_selected.Clear();
			_itemValueLabels.Clear();
			_toggleButtons.Clear();
			_itemRows.Clear();
			SelectedItems.Clear();
			_totalSelected = 0f;

			// Header.
			_display.CreateLabel("Their offer", new RectPercent(50f, 5f, 90f, 8f));

			// Scrollable item list.
			// Each row: item name, value, toggle button.
			float yOffset = 15f;
			float rowHeight = 8f;

			for (int i = 0; i < _inventory.Items.Count; i++)
			{
				int index = i;
				GameObject item = _inventory.Items[i];
				float value = ItemValue.GetValue(item);

				_selected.Add(false);

				// Item name.
				_display.CreateLabel(item.name, new RectPercent(27.5f, yOffset, 70f, rowHeight));

				// Item value.
				var valueLabel = _display.CreateLabel($"{value}g", new RectPercent(72.5f, yOffset, 25f, rowHeight));
				_itemValueLabels.Add(valueLabel);

				// Select toggle.
				var toggleButton = _display.CreateButton(
					"[ ]",
					$"Select {item.name}",
					new RectPercent(91f, yOffset, 15f, rowHeight),
					() => ToggleSelection(index)
				);
				_toggleButtons.Add(toggleButton);

				yOffset += rowHeight + 1f;
			}

			_totalLabel = _display.CreateLabel("Selected: 0g", new RectPercent(50f, 88f, 90f, 8f));
		}

		private void ToggleSelection(int index)
		{
			if (index >= _inventory.Items.Count) return;

			_selected[index] = !_selected[index];
			GameObject item = _inventory.Items[index];
			float value = ItemValue.GetValue(item);

			var label = _toggleButtons[index].GetComponentInChildren<TextMeshProUGUI>();
			if (label != null)
				label.text = _selected[index] ? "[x]" : "[ ]";

			if (_selected[index])
			{
				SelectedItems[item] = value;
				_totalSelected += value;
			}
			else
			{
				SelectedItems.Remove(item);
				_totalSelected -= value;
			}

			_totalSelected = Mathf.Max(0f, _totalSelected);
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