using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Trading
{
	/// <summary>
	/// Defines the physical area the player places items into as their trade offer.
	/// Items present before the trade opens are excluded from the offer.
	/// </summary>
	internal class TradeZone : MonoBehaviour
	{
		private GameObject _zoneVisual;
		private HashSet<GameObject> _excludedItems = new HashSet<GameObject>();
		private Dictionary<GameObject, float> _currentItems = new Dictionary<GameObject, float>();
		private bool _isOpen = false;

		/// <summary>
		/// Fired when the items in the trade zone change.
		/// </summary>
		public event System.Action<Dictionary<GameObject, float>> OnItemsChanged;

		private void Awake()
		{
			CreateVisual();
		}

		private void CreateVisual()
		{
			_zoneVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
			_zoneVisual.transform.SetParent(transform, false);
			_zoneVisual.transform.localPosition = new Vector3(2.5f, -0.92f, 2.5f);
			_zoneVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
			_zoneVisual.transform.localScale = new Vector3(3f, 3f, 3f);

			// Disable the collider so the quad doesn't interfere with item placement.
			Destroy(_zoneVisual.GetComponent<Collider>());

			var renderer = _zoneVisual.GetComponent<Renderer>();
			Material mat = new Material(Shader.Find("Sprites/Default"));
			mat.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
			renderer.material = mat;

			_zoneVisual.SetActive(false);
		}

		/// <summary>
		/// Opens the trade zone, recording items already present to exclude them.
		/// </summary>
		public void Open()
		{
			_isOpen = true;
			_excludedItems.Clear();
			_currentItems.Clear();

			// Pre-check — exclude anything already in the zone before the trade opened.
			foreach (Collider col in GetItemsInZone())
			{
				if (col.gameObject != null)
					_excludedItems.Add(col.gameObject);
			}
		}

		/// <summary>
		/// Closes the trade zone and clears state.
		/// </summary>
		public void Close()
		{
			_isOpen = false;
			_excludedItems.Clear();
			_currentItems.Clear();
		}

		/// <summary>
		/// Shows the zone visual.
		/// </summary>
		public void Show()
		{
			_zoneVisual.SetActive(true);
		}

		/// <summary>
		/// Hides the zone visual.
		/// </summary>
		public void Hide()
		{
			_zoneVisual.SetActive(false);
		}

		private void Update()
		{
			if (!_isOpen) return;

			var detected = new Dictionary<GameObject, float>();
			foreach (Collider col in GetItemsInZone())
			{
				GameObject obj = col.gameObject;

				if (obj == null) continue;
				if (_excludedItems.Contains(obj)) continue;

				// Only include items with a tosaveitemscript to avoid picking up world geometry.
				tosaveitemscript save = obj.GetComponentInParent<tosaveitemscript>();
				if (save == null) continue;

				float value = ItemValue.GetValue(save.gameObject);
				if (value <= 0f) continue;

				detected[save.gameObject] = value;
			}

			// Only fire if the offer has actually changed.
			if (!OfferChanged(detected)) return;

			_currentItems = detected;
			OnItemsChanged?.Invoke(_currentItems);
		}

		private Collider[] GetItemsInZone()
		{
			Vector3 centre = _zoneVisual.transform.position;
			Vector3 halfExtents = new Vector3(1.5f, 1.5f, 1.5f);
			return Physics.OverlapBox(centre, halfExtents, _zoneVisual.transform.rotation);
		}

		private bool OfferChanged(Dictionary<GameObject, float> detected)
		{
			if (detected.Count != _currentItems.Count) return true;

			foreach (var key in detected.Keys)
				if (!_currentItems.ContainsKey(key)) return true;

			foreach (var key in _currentItems.Keys)
				if (!detected.ContainsKey(key)) return true;

			return false;
		}
	}
}