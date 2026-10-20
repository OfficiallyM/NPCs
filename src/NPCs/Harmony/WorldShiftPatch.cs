using HarmonyLib;
using NPCs.Core;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Harmony
{
	// The game moves the world, and everything registered with it, whenever the player gets far from the origin, and warps single objects that reach the edge of the world. Both go through this method.
	// The distance moved is measured instead of read from the argument, because the method can return without moving anything and can move along one axis only.
	[HarmonyPatch(typeof(visszarako), nameof(visszarako.Visszarakas), new[] { typeof(Vector3) })]
	internal static class WorldShiftPatch
	{
		// Anything that moved less than this squared isn't worth broadcasting.
		private const float MIN_SHIFT_SQR = 0.0001f;

		// Every object in the world passes through the patch on a shift, so the lookup has to be cheap.
		private static readonly Dictionary<visszarako, NPC> _npcs = new Dictionary<visszarako, NPC>();

		public static void Register(visszarako shifter, NPC npc) => _npcs[shifter] = npc;

		public static void Unregister(visszarako shifter) => _npcs.Remove(shifter);

		private static void Prefix(visszarako __instance, out Vector3? __state)
		{
			__state = _npcs.ContainsKey(__instance) ? __instance.transform.position : (Vector3?)null;
		}

		private static void Postfix(visszarako __instance, Vector3? __state)
		{
			if (__state == null || !_npcs.TryGetValue(__instance, out NPC npc) || npc == null)
				return;

			Vector3 delta = __instance.transform.position - __state.Value;
			if (delta.sqrMagnitude < MIN_SHIFT_SQR)
				return;

			npc.OnWorldShifted(delta);
		}
	}
}
