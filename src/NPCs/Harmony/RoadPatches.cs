using HarmonyLib;
using NPCs.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Harmony
{
	[HarmonyPatch(typeof(roadGenScript), nameof(roadGenScript.PlaceOneRoad))]
	internal static class RoadGenPlaceOne
	{
		public static bool HasInitalised;
		private static int _nextSpawnIndex = 0;
		private const int MIN_SPAWN = 100;
		private const int MAX_SPAWN = 501;

		private static void Postfix(roadGenScript __instance, int pi)
		{
			if (!HasInitalised)
			{
				_nextSpawnIndex = pi + Random.Range(MIN_SPAWN, MAX_SPAWN);
				HasInitalised = true;
			}

			if (!HasInitalised || pi != _nextSpawnIndex)
				return;

			var road = __instance.roadList[pi];
			NPCs.I.traderSpawnQueue.Add(pi);
			_nextSpawnIndex = pi + Random.Range(MIN_SPAWN, MAX_SPAWN);
		}
	}
}
