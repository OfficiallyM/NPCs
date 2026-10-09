using NPCs.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCs.Appearance
{
	/// <summary>
	/// Every wearable item in the item database, so NPCs can be dressed from the same pool the player picks up from.
	/// </summary>
	public static class WearableCatalog
	{
		private static readonly Dictionary<wearableType, List<GameObject>> _byType = new Dictionary<wearableType, List<GameObject>>();
		private static readonly Dictionary<string, GameObject> _byName = new Dictionary<string, GameObject>();
		private static bool _built;

		public static List<GameObject> OfType(wearableType type)
		{
			Build();
			return _byType[type];
		}

		public static GameObject Find(string name)
		{
			Build();
			_byName.TryGetValue(name, out GameObject item);
			return item;
		}

		// Built on first use because the item database isn't ready when the mod loads.
		private static void Build()
		{
			if (_built)
				return;
			_built = true;

			foreach (wearableType type in Enum.GetValues(typeof(wearableType)))
				_byType[type] = new List<GameObject>();

			foreach (GameObject item in itemdatabase.d.items)
			{
				if (item == null)
					continue;

				wearable wear = item.GetComponentInChildren<wearable>(true);
				if (wear == null)
					continue;

				_byType[wear.tipus].Add(item);
				_byName[item.name] = item;
			}

			foreach (var pair in _byType)
				Logging.LogDebug($"Wearables ({pair.Key}): {string.Join(", ", pair.Value.Select(i => i.name).ToArray())}");
		}
	}
}
