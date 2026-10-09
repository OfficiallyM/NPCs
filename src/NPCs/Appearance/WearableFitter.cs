using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NPCs.Appearance
{
	/// <summary>
	/// Turns a wearable item prefab into a visual copy attached to an NPC.
	/// </summary>
	public static class WearableFitter
	{
		private static GameObject _staging;

		/// <param name="variant">Decides which look the item takes if it has several</param>
		public static GameObject Fit(GameObject prefab, Transform anchor, int variant)
		{
			// An inactive parent stops the item's scripts waking up, so it never registers with the save system.
			GameObject item = UnityEngine.Object.Instantiate(prefab, Staging());

			ApplyVariants(item, new System.Random(variant));

			// Items can have their own pivot for sitting on the head, which has to be read before the scripts holding it go.
			wearable wear = item.GetComponentInChildren<wearable>(true);
			Transform centre = wear != null && wear.useCenterObj ? wear.CenterObj : null;

			Vector3 centreOffset = Vector3.zero;
			Quaternion centreRotation = Quaternion.identity;
			if (centre != null)
			{
				centreOffset = Quaternion.Inverse(item.transform.rotation) * (centre.position - item.transform.position);
				centreRotation = Quaternion.Inverse(item.transform.rotation) * centre.rotation;
			}

			Strip(item);

			item.transform.SetParent(anchor, false);

			// Same alignment the player's wear code uses: the centre point sits on the anchor, otherwise the item's own pivot does.
			if (centre != null)
			{
				Quaternion rotation = Quaternion.Inverse(centreRotation);
				item.transform.localRotation = rotation;
				item.transform.localPosition = -(rotation * centreOffset);
			}
			else
			{
				item.transform.localPosition = Vector3.zero;
				item.transform.localRotation = Quaternion.identity;
			}

			return item;
		}

		// Items with several looks, like sunglasses, switch all but one off from a script on wake-up. That script is stripped
		// and never runs here, so the choice is made from the seed instead using the same rules.
		private static void ApplyVariants(GameObject item, System.Random rng)
		{
			randomTypeSelector[] selectors = item.GetComponentsInChildren<randomTypeSelector>(true);

			// Selectors listed as children are driven by their parent, which hands down its choice.
			var driven = new HashSet<randomTypeSelector>(selectors.Where(s => s.childs != null).SelectMany(s => s.childs));

			foreach (randomTypeSelector selector in selectors)
			{
				if (driven.Contains(selector) || selector.tipusok == null || selector.tipusok.Length == 0)
					continue;

				selector.rtipus = RollType(rng, selector);
				selector.Refresh();
			}
		}

		// Weighted by each look's chance, as the game does it.
		private static int RollType(System.Random rng, randomTypeSelector selector)
		{
			float roll = (float)rng.NextDouble() * selector.tipusok.Sum(t => t.chance);
			float cumulative = 0f;

			for (int i = 0; i < selector.tipusok.Length; i++)
			{
				cumulative += selector.tipusok[i].chance;
				if (roll < cumulative)
					return i;
			}

			return selector.tipusok.Length - 1;
		}

		/// <summary>
		/// Lists an item's children, whether each starts active, and the components on them. For working out why an item looks wrong once stripped.
		/// </summary>
		public static string Describe(GameObject root)
		{
			var builder = new StringBuilder(root.name);

			foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
			{
				int depth = 0;
				for (Transform parent = child; parent != root.transform; parent = parent.parent)
					depth++;

				string components = string.Join(", ", child.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name).ToArray());
				builder.Append($"\n{new string(' ', depth * 2)}{child.name} (active: {child.gameObject.activeSelf}) [{components}]");
			}

			return builder.ToString();
		}

		// Leaves only what is needed to draw the item, so it can't be picked up, saved, collided with or do anything.
		private static void Strip(GameObject item)
		{
			Component[] components = item.GetComponentsInChildren<Component>(true);

			foreach (Component component in components.OrderBy(RemovalOrder))
			{
				if (component == null || component is Transform || component is Renderer || component is MeshFilter)
					continue;

				UnityEngine.Object.DestroyImmediate(component);
			}
		}

		// Scripts and joints have to go before the rigidbodies and colliders they depend on.
		private static int RemovalOrder(Component component)
		{
			if (component is MonoBehaviour)
				return 0;
			if (component is Joint)
				return 1;
			if (component is Rigidbody)
				return 3;
			return 2;
		}

		private static Transform Staging()
		{
			if (_staging == null)
			{
				_staging = new GameObject("NPCWearableStaging");
				_staging.SetActive(false);
				UnityEngine.Object.DontDestroyOnLoad(_staging);
			}

			return _staging.transform;
		}
	}
}
