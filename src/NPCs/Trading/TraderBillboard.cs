using NPCs.Common;
using NPCs.Trading.Core;
using NPCs.Trading.Value;
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
	/// Worldspace billboard displaying the trader's inventory as a paged grid of picture cards.
	/// Positioned to the left of the trader.
	/// </summary>
	internal class TraderBillboard : MonoBehaviour
	{
		private const int Columns = 3;
		private const int Rows = 2;
		private const int PageSize = Columns * Rows;

		// Card layout as percentages of the canvas.
		private static readonly float[] _cardCentresX = { 17.5f, 50f, 82.5f };
		private static readonly float[] _cardCentresY = { 26.25f, 59.75f };
		private const float CardWidth = 29f;
		private const float CardHeight = 31.5f;

		private static readonly Color _placeholderColour = new Color(0.25f, 0.25f, 0.25f, 0.6f);

		private WorldspaceInteractiveDisplay _display;
		private TraderInventory _inventory;
		private TraderPersonality _personality;
		private Func<TraderItem, float> _valueResolver;

		private RectTransform _cardsRoot;
		private TextMeshProUGUI _pageLabel;
		private Button _previousButton;
		private Button _nextButton;
		private int _page = 0;

		private TextMeshProUGUI _totalLabel;
		private TextMeshProUGUI[] _quantityLabels = new TextMeshProUGUI[0];
		private Button[] _minusButtons = new Button[0];
		private Button[] _plusButtons = new Button[0];
		private List<int> _selectedQuantities = new List<int>();

		private float _totalSelected = 0f;

		private Image _colourPreview;
		private TextMeshProUGUI _colourMatchToggleLabel;
		private bool _colourMatchActive = false;
		private GameObject _colourMatchRow;

		public Color? CachedCarColour;

		/// <summary>
		/// Items currently selected by the player, keyed by prefab with quantity and total value.
		/// </summary>
		public Dictionary<TraderItem, (int Quantity, float TotalValue)> SelectedItems { get; private set; }
			= new Dictionary<TraderItem, (int, float)>();

		/// <summary>
		/// Whether the colour match service is currently active.
		/// </summary>
		public bool ColourMatchActive => _colourMatchActive;

		/// <summary>
		/// Fired when the selection changes.
		/// </summary>
		public event System.Action OnSelectionChanged;

		private int PageCount => Mathf.Max(1, Mathf.CeilToInt(_inventory.Items.Count / (float)PageSize));

		public void Initialise(TraderInventory inventory, TraderPersonality personality, Func<TraderItem, float> valueResolver)
		{
			_inventory = inventory;
			_personality = personality;
			_valueResolver = valueResolver;

			_display = gameObject.AddComponent<WorldspaceInteractiveDisplay>();
			_display.SetPosition(new Vector3(-1.7f, 0.3f, 0f));
			_display.SetSize(new Vector2(900f, 720f));
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
			SelectedItems.Clear();
			_totalSelected = 0f;
			_colourMatchActive = false;
			_page = 0;

			int count = _inventory.Items.Count;
			_quantityLabels = new TextMeshProUGUI[count];
			_minusButtons = new Button[count];
			_plusButtons = new Button[count];
			for (int i = 0; i < count; i++)
				_selectedQuantities.Add(0);

			_display.CreateLabel("Their offer", new RectPercent(50f, 4f, 90f, 6f));

			// Full-canvas container that each page of cards is built inside.
			_cardsRoot = _display.CreateContainer(new RectPercent(50f, 50f, 100f, 100f));

			if (count == 0)
				_display.CreateLabel("Nothing for sale right now", new RectPercent(50f, 40f, 80f, 8f));

			BuildPager();

			_totalLabel = _display.CreateLabel("Selected: 0g", new RectPercent(50f, 89f, 90f, 5f));
			BuildColourMatchRow();

			// The first page is queued first so its pictures arrive first.
			RenderPage();

			// Queue the rest so turning the page doesn't have to wait.
			foreach (TraderItem item in _inventory.Items)
				ThumbnailGenerator.GetThumbnail(item, null);
		}

		private void BuildPager()
		{
			_previousButton = _display.CreateButton("<", "Previous page", new RectPercent(34f, 82f, 10f, 6f), () => ChangePage(-1));
			_pageLabel = _display.CreateLabel("", new RectPercent(50f, 82f, 18f, 6f));
			_nextButton = _display.CreateButton(">", "Next page", new RectPercent(66f, 82f, 10f, 6f), () => ChangePage(1));
		}

		private void ChangePage(int delta)
		{
			int page = Mathf.Clamp(_page + delta, 0, PageCount - 1);
			if (page == _page) return;

			_page = page;
			RenderPage();
		}

		/// <summary>
		/// Rebuilds the cards for the current page.
		/// </summary>
		private void RenderPage()
		{
			for (int i = _cardsRoot.childCount - 1; i >= 0; i--)
				Destroy(_cardsRoot.GetChild(i).gameObject);

			// Cards on other pages no longer exist.
			Array.Clear(_quantityLabels, 0, _quantityLabels.Length);
			Array.Clear(_minusButtons, 0, _minusButtons.Length);
			Array.Clear(_plusButtons, 0, _plusButtons.Length);

			int start = _page * PageSize;
			int end = Mathf.Min(start + PageSize, _inventory.Items.Count);
			for (int index = start; index < end; index++)
				BuildCard(index, index - start);

			_pageLabel.text = $"{_page + 1} / {PageCount}";
			_previousButton.interactable = _page > 0;
			_nextButton.interactable = _page < PageCount - 1;
		}

		private void BuildCard(int index, int slot)
		{
			TraderItem item = _inventory.Items[index];
			float unitValue = _valueResolver(item);
			int selected = _selectedQuantities[index];

			RectTransform card = _display.CreateContainer(
				new RectPercent(_cardCentresX[slot % Columns], _cardCentresY[slot / Columns], CardWidth, CardHeight),
				_cardsRoot
			);

			Image background = card.gameObject.AddComponent<Image>();
			background.color = new Color(0f, 0f, 0f, 0.35f);
			background.raycastTarget = false;

			// Picture, filled in once it has been rendered.
			RawImage picture = AddPicture(card, new Vector2(0.291f, 0.5f), new Vector2(0.709f, 0.98f));
			Texture2D cached = ThumbnailGenerator.GetThumbnail(item, texture => ApplyThumbnail(picture, texture));
			if (cached != null)
				ApplyThumbnail(picture, cached);

			// Name gets its own line and shrinks to fit so the whole thing is always readable.
			AddLabelToRow(card, item.Data.DisplayName, new Vector2(0.03f, 0.32f), new Vector2(0.97f, 0.5f),
				fontSize: 24f, alignment: TextAlignmentOptions.Center, autoSize: true);

			string condition = Trade.ConditionTag(item.Condition);
			string info = string.IsNullOrEmpty(condition) ? $"{unitValue}g" : $"{condition}  {unitValue}g";
			AddLabelToRow(card, info, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.32f),
				fontSize: 22f, alignment: TextAlignmentOptions.Center);

			// Quantity controls.
			Button minus = AddButtonToRow(card, "-", $"Remove {item.Data.DisplayName}", new Vector2(0.05f, 0.02f), new Vector2(0.28f, 0.19f), () => AdjustQuantity(index, -1));
			TextMeshProUGUI quantity = AddLabelToRow(card, $"{selected}/{item.Quantity}", new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.19f),
				fontSize: 24f, alignment: TextAlignmentOptions.Center);
			Button plus = AddButtonToRow(card, "+", $"Add {item.Data.DisplayName}", new Vector2(0.72f, 0.02f), new Vector2(0.95f, 0.19f), () => AdjustQuantity(index, 1));

			minus.interactable = selected > 0;
			plus.interactable = selected < item.Quantity;

			_minusButtons[index] = minus;
			_quantityLabels[index] = quantity;
			_plusButtons[index] = plus;
		}

		private RawImage AddPicture(RectTransform card, Vector2 anchorMin, Vector2 anchorMax)
		{
			GameObject obj = new GameObject("Picture", typeof(RectTransform));
			obj.transform.SetParent(card, false);

			RectTransform rt = obj.GetComponent<RectTransform>();
			rt.anchorMin = anchorMin;
			rt.anchorMax = anchorMax;
			rt.offsetMin = rt.offsetMax = Vector2.zero;
			rt.anchoredPosition3D = new Vector3(0f, 0f, -0.1f);

			RawImage image = obj.AddComponent<RawImage>();
			image.color = _placeholderColour;
			image.raycastTarget = false;
			return image;
		}

		private void ApplyThumbnail(RawImage picture, Texture2D texture)
		{
			// The card may have been replaced by the time the picture is ready.
			if (picture == null || texture == null) return;

			picture.texture = texture;
			picture.color = Color.white;
		}

		private TextMeshProUGUI AddLabelToRow(RectTransform row, string text, Vector2 anchorMin, Vector2 anchorMax,
			bool overflow = false, float fontSize = 26f, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft, bool autoSize = false)
		{
			GameObject obj = new GameObject("Label");
			obj.transform.SetParent(row, false);
			obj.transform.localPosition = new Vector3(0f, 0f, -0.1f);

			TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
			tmp.text = text;
			tmp.fontSize = fontSize;
			tmp.alignment = alignment;
			tmp.overflowMode = overflow ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
			tmp.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
			tmp.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field Overlay");

			if (autoSize)
			{
				tmp.enableWordWrapping = true;
				tmp.enableAutoSizing = true;
				tmp.fontSizeMax = fontSize;
				tmp.fontSizeMin = fontSize * 0.6f;
			}

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

			var inventoryItem = _inventory.Items[index];
			float unitValue = _valueResolver(inventoryItem);

			int current = _selectedQuantities[index];
			int updated = Mathf.Clamp(current + delta, 0, inventoryItem.Quantity);
			if (updated == current) return;

			_selectedQuantities[index] = updated;

			// Update selected items.
			if (updated == 0)
				SelectedItems.Remove(inventoryItem);
			else
				SelectedItems[inventoryItem] = (updated, unitValue * updated);

			// Recalculate total.
			_totalSelected = SelectedItems.Values.Sum(e => e.TotalValue);

			// Update the card. Only cards on the current page exist.
			if (_quantityLabels[index] != null)
				_quantityLabels[index].text = $"{updated}/{inventoryItem.Quantity}";
			if (_minusButtons[index] != null)
				_minusButtons[index].interactable = updated > 0;
			if (_plusButtons[index] != null)
				_plusButtons[index].interactable = updated < inventoryItem.Quantity;

			UpdateTotalLabel();
			OnSelectionChanged?.Invoke();

			// Update colour match service.
			bool anyColoured = SelectedItems.Keys.Any(item => item.Color.HasValue);
			_colourMatchRow.SetActive(anyColoured && CachedCarColour.HasValue);
			if (!anyColoured)
			{
				_colourMatchActive = false;
				// Reset toggle label.
				if (_colourMatchToggleLabel != null)
					_colourMatchToggleLabel.text = "No";
			}
		}

		private void UpdateTotalLabel()
		{
			if (_totalLabel == null) return;
			float total = _totalSelected + (_colourMatchActive ? ValueConstants.ColourMatchFee : 0f);
			_totalLabel.text = $"Selected: {Maths.RoundToNearestHalf(total)}g";
		}

		private void CacheCarColour()
		{
			CachedCarColour = null;
			var car = mainscript.M.player?.Car ?? mainscript.M.player?.lastCar;
			if (car == null) return;
			var condition = car.GetComponentInChildren<partconditionscript>();
			if (condition == null) return;
			var color = condition.color;
			color.a = 1f;
			CachedCarColour = color;
		}

		private void BuildColourMatchRow()
		{
			CacheCarColour();

			RectTransform rt = _display.CreateContainer(new RectPercent(50f, 95.5f, 90f, 6f));
			_colourMatchRow = rt.gameObject;

			// Colour preview square — left side of container.
			GameObject previewObj = new GameObject("ColourPreview");
			previewObj.transform.SetParent(rt, false);
			_display.RegisterLabel(previewObj);

			_colourPreview = previewObj.AddComponent<Image>();
			_colourPreview.material = new Material(Shader.Find("UI/Default"));
			_colourPreview.color = CachedCarColour ?? Color.grey;

			RectTransform previewRect = previewObj.GetComponent<RectTransform>();
			previewRect.anchorMin = new Vector2(0f, 0.1f);
			previewRect.anchorMax = new Vector2(0.06f, 0.9f);
			previewRect.offsetMin = previewRect.offsetMax = Vector2.zero;

			// Label.
			GameObject labelObj = new GameObject("ColourMatchLabel");
			labelObj.transform.SetParent(rt, false);
			labelObj.transform.localPosition = new Vector3(0f, 0f, -0.1f);
			_display.RegisterLabel(labelObj);

			var label = labelObj.AddComponent<TextMeshProUGUI>();
			label.text = $"Colour match service - {ValueConstants.ColourMatchFee}g";
			label.fontSize = 24f;
			label.alignment = TextAlignmentOptions.MidlineLeft;
			label.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
			label.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field Overlay");

			RectTransform labelRect = labelObj.GetComponent<RectTransform>();
			labelRect.anchorMin = new Vector2(0.08f, 0f);
			labelRect.anchorMax = new Vector2(0.72f, 1f);
			labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

			// Toggle button.
			var button = AddButtonToRow(rt, "No", "Toggle colour match", new Vector2(0.74f, 0.1f), new Vector2(0.95f, 0.9f), ToggleColourMatch);
			_colourMatchToggleLabel = button.GetComponentInChildren<TextMeshProUGUI>();

			_colourMatchRow.SetActive(false);
		}

		private void ToggleColourMatch()
		{
			_colourMatchActive = !_colourMatchActive;
			if (_colourMatchToggleLabel != null)
				_colourMatchToggleLabel.text = _colourMatchActive ? "Yes" : "No";
			UpdateTotalLabel();
			OnSelectionChanged?.Invoke();
		}
	}
}