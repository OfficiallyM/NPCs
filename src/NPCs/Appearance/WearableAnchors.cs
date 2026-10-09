using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Appearance
{
	/// <summary>
	/// Where each kind of wearable sits relative to an NPC's head bone.
	/// </summary>
	public static class WearableAnchors
	{
		private static readonly Dictionary<wearableType, WearableAnchor> _anchors = new Dictionary<wearableType, WearableAnchor>();

		/// <summary>
		/// Goes up whenever an anchor is edited, so worn items know to follow.
		/// </summary>
		public static int Version { get; private set; }

		static WearableAnchors()
		{
			_anchors[wearableType.glasses] = new WearableAnchor { Position = new Vector3(0f, 0.025f, 0.125f) };
			_anchors[wearableType.hat] = new WearableAnchor { Position = new Vector3(0f, 0.02f, 0.1f) };
			_anchors[wearableType.cap] = new WearableAnchor { Position = new Vector3(-0.003f, 0.04f, 0.07f) };
			_anchors[wearableType.helmet] = new WearableAnchor { Position = new Vector3(0f, -0.12f, 0f), Rotation = new Vector3(0f, 180f, 0f) };
			_anchors[wearableType.mask] = new WearableAnchor { Position = new Vector3(-0.005f, 0.027f, 0.105f) };

			// Anything the game adds later sits on the bone until it's tuned.
			foreach (wearableType type in Enum.GetValues(typeof(wearableType)))
			{
				if (!_anchors.ContainsKey(type))
					_anchors[type] = new WearableAnchor();
			}
		}

		public static WearableAnchor Get(wearableType type) => _anchors[type];

		public static void MarkChanged() => Version++;
	}
}
