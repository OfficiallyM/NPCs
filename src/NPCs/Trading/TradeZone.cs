using NPCs.Trading.Core;
using NPCs.Trading.Value;
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
		// Zone position and size, relative to the trader.
		private static readonly Vector3 _zoneOffset = new Vector3(2.5f, 0.001f, 2.5f);
		private static readonly Vector3 _zoneSize = new Vector3(3f, 3f, 3f);

		// How often the zone is checked while a trade is open, in seconds.
		private const float PollInterval = 0.1f;

		private GameObject _zoneVisual;
		private HashSet<GameObject> _excludedItems = new HashSet<GameObject>();
		private Dictionary<GameObject, ItemData> _currentItems = new Dictionary<GameObject, ItemData>();
		private bool _isOpen = false;
		private float _nextPoll = 0f;

		// Reused between polls to avoid allocating every check.
		private Collider[] _buffer = new Collider[128];
		private HashSet<tosaveitemscript> _roots = new HashSet<tosaveitemscript>();
		private HashSet<GameObject> _inZone = new HashSet<GameObject>();
		private HashSet<GameObject> _ignored = new HashSet<GameObject>();
		private List<GameObject> _toRemove = new List<GameObject>();

		/// <summary>
		/// Fired when the items in the trade zone change.
		/// </summary>
		public event System.Action<Dictionary<GameObject, ItemData>> OnItemsChanged;

		private void Awake()
		{
			CreateVisual();
		}

		private void CreateVisual()
		{
			_zoneVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
			_zoneVisual.name = "Trade zone";
			_zoneVisual.transform.SetParent(transform, false);
			_zoneVisual.transform.localPosition = _zoneOffset;
			_zoneVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
			_zoneVisual.transform.localScale = new Vector3(3f, 3f, 3f);

			// Disable the collider so the quad doesn't interfere with item physics.
			Destroy(_zoneVisual.GetComponent<Collider>());

			var renderer = _zoneVisual.GetComponent<Renderer>();
			Material mat = new Material(Shader.Find("Sprites/Default"));
			mat.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
			renderer.material = mat;

			_zoneVisual.SetActive(false);
		}

		private void Update()
		{
			if (!_isOpen || Time.time < _nextPoll) return;

			_nextPoll = Time.time + PollInterval;
			Poll();
		}

		/// <summary>
		/// Opens the trade zone, recording items already present to exclude them.
		/// </summary>
		public void Open()
		{
			_isOpen = true;
			_nextPoll = 0f;
			_excludedItems.Clear();
			_currentItems.Clear();
			_ignored.Clear();

			// One-time snapshot of items already in zone before trade opened.
			CollectItemsInZone(_excludedItems);
		}

		/// <summary>
		/// Closes the trade zone and clears state.
		/// </summary>
		public void Close()
		{
			_isOpen = false;
			_excludedItems.Clear();
			_currentItems.Clear();
			_ignored.Clear();
		}

		/// <summary>
		/// Shows the zone visual.
		/// </summary>
		public void Show() => _zoneVisual.SetActive(true);

		/// <summary>
		/// Hides the zone visual.
		/// </summary>
		public void Hide() => _zoneVisual.SetActive(false);

		/// <summary>
		/// Checks what is currently in the zone and reports any changes.
		/// </summary>
		private void Poll()
		{
			_inZone.Clear();
			CollectItemsInZone(_inZone);

			bool changed = false;

			// Items that have arrived.
			foreach (GameObject item in _inZone)
			{
				if (_excludedItems.Contains(item)) continue;
				if (_currentItems.ContainsKey(item)) continue;
				if (_ignored.Contains(item)) continue;

				ItemData data = ItemRegistry.GetData(item);
				if (data == null || data.Value <= 0f)
				{
					// Not tradeable. Remember that so it isn't looked up again every poll.
					_ignored.Add(item);
					continue;
				}

				_currentItems[item] = data;
				changed = true;
			}

			// Items that have left, or been destroyed.
			_toRemove.Clear();
			foreach (GameObject item in _currentItems.Keys)
			{
				if (!_inZone.Contains(item))
					_toRemove.Add(item);
			}

			foreach (GameObject item in _toRemove)
			{
				_currentItems.Remove(item);
				changed = true;
			}

			_ignored.IntersectWith(_inZone);

			if (changed)
				OnItemsChanged?.Invoke(_currentItems);
		}

		/// <summary>
		/// Adds every save item currently inside the zone to the set.
		/// </summary>
		private void CollectItemsInZone(HashSet<GameObject> into)
		{
			Vector3 centre = transform.TransformPoint(_zoneOffset);
			Vector3 scale = transform.lossyScale;
			Vector3 halfExtents = new Vector3(
				Mathf.Abs(_zoneSize.x * scale.x),
				Mathf.Abs(_zoneSize.y * scale.y),
				Mathf.Abs(_zoneSize.z * scale.z)
			) * 0.5f;

			// Triggers are ignored so only solid parts of items count.
			int count = Physics.OverlapBoxNonAlloc(centre, halfExtents, _buffer, transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Ignore);
			while (count == _buffer.Length)
			{
				// The buffer filled up, so there may be more. Grow it and look again.
				_buffer = new Collider[_buffer.Length * 2];
				count = Physics.OverlapBoxNonAlloc(centre, halfExtents, _buffer, transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Ignore);
			}

			_roots.Clear();
			for (int i = 0; i < count; i++)
			{
				Collider col = _buffer[i];
				_buffer[i] = null;
				if (col == null) continue;

				tosaveitemscript rootSave = col.gameObject.GetComponentInParent<tosaveitemscript>();
				if (rootSave == null || !_roots.Add(rootSave)) continue;

				foreach (tosaveitemscript save in rootSave.GetComponentsInChildren<tosaveitemscript>())
					into.Add(save.gameObject);
			}
		}
	}
}