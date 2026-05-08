using NPCs.Common;
using NPCs.Utilities;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NPCs.Trading
{
	/// <summary>
	/// Worldspace billboard displaying the player's current trade offer.
	/// Positioned to the right of the trader.
	/// </summary>
	internal class PlayerBillboard : MonoBehaviour
	{
		private WorldspaceInteractiveDisplay _display;
		private Button _proposeButton;
		private TextMeshProUGUI _totalLabel;

		/// <summary>
		/// Fired when the player proposes the trade.
		/// </summary>
		public event System.Action OnProposed;

		public void Initialise()
		{
			_display = gameObject.AddComponent<WorldspaceInteractiveDisplay>();
			_display.SetPosition(new Vector3(1.25f, 0.15f, 0f));
			_display.SetSize(new Vector2(400f, 550f));
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
		/// Rebuilds the billboard with the current player offer.
		/// </summary>
		/// <param name="items">Items in the trade zone keyed by GameObject with their resolved value.</param>
		/// <param name="total">Combined value of all offered items.</param>
		public void Build(Dictionary<GameObject, ItemData> items, float total)
		{
			_display.Clear();

			_display.CreateLabel("Your offer", new RectPercent(50f, 5f, 90f, 8f));

			var scrollList = _display.CreateScrollList(15f, 85f, 7, 8f);

			foreach (var entry in items)
			{
				RectTransform row = scrollList.AddRow();
				AddLabelToRow(row, entry.Value.DisplayName, new Vector2(0f, 0f), new Vector2(0.8f, 1f));
				AddLabelToRow(row, $"{Maths.RoundToNearestHalf(entry.Value.Value)}g", new Vector2(0.85f, 0f), new Vector2(1f, 1f));
			}

			_totalLabel = _display.CreateLabel($"Total: {total}g", new RectPercent(50f, 80f, 90f, 8f));
			_proposeButton = _display.CreateButton(
				"Propose trade",
				"Propose trade",
				new RectPercent(50f, 91f, 80f, 8f),
				() => OnProposed?.Invoke()
			);
		}

		/// <summary>
		/// Updates the total label without rebuilding the full layout.
		/// </summary>
		/// <param name="total">New total value to display.</param>
		public void UpdateTotal(float total)
		{
			if (_totalLabel != null)
				_totalLabel.text = $"Total: {Maths.RoundToNearestHalf(total)}g";
		}

		public void SetProposeLabel(string label)
		{
			if (_proposeButton != null)
				_proposeButton.GetComponentInChildren<TextMeshProUGUI>().text = label;
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
	}
}