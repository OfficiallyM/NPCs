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
		private GameObject _zoneVisual;
		private BoxCollider _trigger;
		private HashSet<GameObject> _excludedItems = new HashSet<GameObject>();
		private Dictionary<GameObject, ItemData> _currentItems = new Dictionary<GameObject, ItemData>();
		private bool _isOpen = false;

		/// <summary>
		/// Fired when the items in the trade zone change.
		/// </summary>
		public event System.Action<Dictionary<GameObject, ItemData>> OnItemsChanged;

		private void Awake()
		{
			CreateVisual();
			CreateTrigger();
		}

		private void CreateVisual()
		{
			_zoneVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
			_zoneVisual.name = "Trade zone";
			_zoneVisual.transform.SetParent(transform, false);
			_zoneVisual.transform.localPosition = new Vector3(2.5f, -0.92f, 2.5f);
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

		private void CreateTrigger()
		{
			// Trigger sits on a child object at the same position as the visual.
			GameObject triggerObj = new GameObject("TradeZoneTrigger");
			triggerObj.transform.SetParent(transform, false);
			triggerObj.transform.localPosition = new Vector3(2.5f, -0.92f, 2.5f);

			_trigger = triggerObj.AddComponent<BoxCollider>();
			_trigger.isTrigger = true;
			_trigger.size = new Vector3(3f, 3f, 3f);
			_trigger.enabled = false;

			// Forward trigger events to this component.
			var forwarder = triggerObj.AddComponent<TriggerForwarder>();
			forwarder.OnEnter += OnTriggerItemEnter;
			forwarder.OnExit += OnTriggerItemExit;
		}

		/// <summary>
		/// Opens the trade zone, recording items already present to exclude them.
		/// </summary>
		public void Open()
		{
			_isOpen = true;
			_excludedItems.Clear();
			_currentItems.Clear();

			// One-time snapshot of items already in zone before trade opened.
			Collider[] existing = Physics.OverlapBox(
				_trigger.transform.position,
				_trigger.size / 2f,
				_trigger.transform.rotation
			);

			foreach (Collider col in existing)
			{
				tosaveitemscript rootSave = col.gameObject.GetComponentInParent<tosaveitemscript>();
				if (rootSave == null) continue;

				foreach (tosaveitemscript save in rootSave.GetComponentsInChildren<tosaveitemscript>())
					_excludedItems.Add(save.gameObject);
			}

			_trigger.enabled = true;
		}

		/// <summary>
		/// Closes the trade zone and clears state.
		/// </summary>
		public void Close()
		{
			_isOpen = false;
			_trigger.enabled = false;
			_excludedItems.Clear();
			_currentItems.Clear();
		}

		/// <summary>
		/// Shows the zone visual.
		/// </summary>
		public void Show() => _zoneVisual.SetActive(true);

		/// <summary>
		/// Hides the zone visual.
		/// </summary>
		public void Hide() => _zoneVisual.SetActive(false);

		private void OnTriggerItemEnter(Collider other)
		{
			if (!_isOpen) return;

			tosaveitemscript rootSave = other.gameObject.GetComponentInParent<tosaveitemscript>();
			if (rootSave == null) return;

			bool changed = false;
			foreach (tosaveitemscript save in rootSave.GetComponentsInChildren<tosaveitemscript>())
			{
				if (_excludedItems.Contains(save.gameObject)) continue;
				if (_currentItems.ContainsKey(save.gameObject)) continue;

				ItemData data = ItemRegistry.GetData(save.gameObject);
				if (data == null || data.Value <= 0f) continue;

				_currentItems[save.gameObject] = data;
				changed = true;
			}

			if (changed)
				OnItemsChanged?.Invoke(_currentItems);
		}

		private void OnTriggerItemExit(Collider other)
		{
			if (!_isOpen) return;

			tosaveitemscript rootSave = other.gameObject.GetComponentInParent<tosaveitemscript>();
			if (rootSave == null) return;

			bool changed = false;
			foreach (tosaveitemscript save in rootSave.GetComponentsInChildren<tosaveitemscript>())
			{
				if (_currentItems.Remove(save.gameObject))
					changed = true;
			}

			if (changed)
				OnItemsChanged?.Invoke(_currentItems);
		}
	}
}