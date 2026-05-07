using NPCs.Common;
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

		/// <summary>
		/// Fired when the player proposes the trade.
		/// </summary>
		public event System.Action OnProposed;

		public void Initialise()
		{
			_display = gameObject.AddComponent<WorldspaceInteractiveDisplay>();
			_display.SetPosition(new Vector3(1.25f, 0f, 0f));
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

			// Item list.
			float yOffset = 15f;
			float rowHeight = 8f;
			foreach (var entry in items)
			{
				_display.CreateLabel(entry.Value.DisplayName, new RectPercent(30f, yOffset, 75f, rowHeight));
				_display.CreateLabel($"{entry.Value.Value}g", new RectPercent(80f, yOffset, 25f, rowHeight));
				yOffset += rowHeight + 1f;
			}

			_display.CreateLabel($"Total: {total}g", new RectPercent(50f, 80f, 90f, 8f));
			_proposeButton = _display.CreateButton(
				"Propose trade",
				"Propose trade",
				new RectPercent(50f, 91f, 80f, 8f),
				() => OnProposed?.Invoke()
			);
		}

		public void SetProposeLabel(string label)
		{
			if (_proposeButton != null)
				_proposeButton.GetComponentInChildren<TextMeshProUGUI>().text = label;
		}
	}
}