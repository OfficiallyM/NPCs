using NPCs.Common;
using NPCs.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
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
			_display.SetPosition(new Vector3(-1.25f, 0.15f, 0f));
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

			var scrollList = _display.CreateScrollList(15f, 85f, 7, 8f);

			for (int i = 0; i < _inventory.Items.Count; i++)
			{
				int index = i;
				var inventoryItem = _inventory.Items.ElementAt(i);
				GameObject item = inventoryItem.Key;
				ItemData data = inventoryItem.Value.data;
				int maxQuantity = inventoryItem.Value.quantity;
				float unitValue = _valueResolver(data);

				_selectedQuantities.Add(0);

				RectTransform row = scrollList.AddRow();

				// Item name.
				AddLabelToRow(row, $"{data.DisplayName} (x{maxQuantity})", new Vector2(0f, 0f), new Vector2(0.5f, 1f));

				// Minus button.
				var minusBtn = AddButtonToRow(row, "-", $"Remove {data.DisplayName}", new Vector2(0.6f, 0.1f), new Vector2(0.68f, 0.9f), () => AdjustQuantity(index, -1));
				minusBtn.interactable = false;
				_minusButtons.Add(minusBtn);

				// Quantity label.
				var quantityLabel = AddLabelToRow(row, "x0", new Vector2(0.7f, 0f), new Vector2(0.80f, 1f));
				_quantityLabels.Add(quantityLabel);

				// Plus button.
				var plusBtn = AddButtonToRow(row, "+", $"Add {data.DisplayName}", new Vector2(0.80f, 0.1f), new Vector2(0.88f, 0.9f), () => AdjustQuantity(index, 1));
				_plusButtons.Add(plusBtn);

				// Unit value.
				AddLabelToRow(row, $"{unitValue}g", new Vector2(0.90f, 0f), new Vector2(1f, 1f));
			}

			_totalLabel = _display.CreateLabel("Selected: 0g", new RectPercent(50f, 92f, 90f, 6f));
		}

		private TextMeshProUGUI AddLabelToRow(RectTransform row, string text, Vector2 anchorMin, Vector2 anchorMax)
		{
			GameObject obj = new GameObject("Label");
			obj.transform.SetParent(row, false);
			obj.transform.localPosition = new Vector3(0f, 0f, -0.1f);
			_display.RegisterLabel(obj);

			TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
			tmp.text = text;
			tmp.fontSize = 26f;
			tmp.alignment = TextAlignmentOptions.MidlineLeft;
			tmp.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
			tmp.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field Overlay");

			RectTransform rt = obj.GetComponent<RectTransform>();
			rt.anchorMin = anchorMin;
			rt.anchorMax = anchorMax;
			rt.offsetMin = rt.offsetMax = Vector2.zero;

			return tmp;
		}

		private Button AddButtonToRow(RectTransform row, string label, string interactLabel, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
		{
			GameObject obj = new GameObject(interactLabel);
			obj.transform.SetParent(row, false);
			obj.transform.localPosition = new Vector3(0f, 0f, -0.1f);

			Button button = obj.AddComponent<Button>();
			Image image = obj.AddComponent<Image>();
			button.targetGraphic = image;
			image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

			RectTransform rt = obj.GetComponent<RectTransform>();
			rt.anchorMin = anchorMin;
			rt.anchorMax = anchorMax;
			rt.offsetMin = rt.offsetMax = Vector2.zero;

			GameObject labelObj = new GameObject("Label");
			labelObj.transform.SetParent(obj.transform, false);
			labelObj.transform.localPosition = new Vector3(0f, 0f, -0.1f);
			_display.RegisterLabel(labelObj);
			TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
			tmp.text = label;
			tmp.fontSize = 28f;
			tmp.alignment = TextAlignmentOptions.Center;
			tmp.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
			tmp.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field Overlay");

			RectTransform labelRect = labelObj.GetComponent<RectTransform>();
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

			button.onClick.AddListener(() => onClick?.Invoke());
			return button;
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