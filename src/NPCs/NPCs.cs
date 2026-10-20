using NPCs.Dialogue;
using NPCs.Harmony;
using NPCs.Trading;
using NPCs.Trading.Value;
using System.Collections.Generic;
using System.Reflection;
using TLDLoader;
using UnityEngine;

namespace NPCs
{
	public class NPCs : Mod
	{
		private string _version = "0.0.1";
		public override string ID => "M_NPCs";
		public override string Name => "NPCs";
		public override string Author => "M-";
		public override string Version => _version;
		public override bool UseLogger => true;
		public override bool LoadInDB => true;
		public override bool UseAssetsFolder => true;
		public override bool UseHarmony => true;

		internal static NPCs I;
		internal static bool Debug = false;
		internal static bool IsPaused
		{
			get
			{
				var menu = mainscript.M?.menu?.Menu;
				return menu != null && menu.activeSelf;
			}
		}

		// TODO: Build proper spawn system once AI is working.
		internal List<int> TraderSpawnQueue = new List<int>();

		public NPCs()
		{
			I = this;
#if DEBUG
			_version += "-DEV";
			Debug = true;
#endif
		}

		public override void DbLoad()
		{
			AssetBundle bundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(NPCs)}.npcs"));
			var silver = bundle.LoadAsset<GameObject>("silver");
			bundle.Unload(false);

			// NPC item IDs: 0 - 1000.
			RegisterItem(itemdatabase.d.gragdoll, 0, "NPC - Trader")
				.WithRigidbody(90, 5)
				.AddComponent<Trader>()
				.Register();

			// Other item IDs: 1000+.
			RegisterItem(silver, 1000)
				.WithRigidbody(15.5f)
				.AsPickupable(new PickupableOptions() { Attachable = true, CanInventory = true })
				.SpawnInBox(maxPerBox: 6)
				.SpawnAt(6, itemdatabase.d.ggold)
				.Register();

			if (mainscript.M.player.GetComponent<ConversationUI>() == null)
				mainscript.M.player.gameObject.AddComponent<ConversationUI>();
		}

		public override void OnLoad()
		{
			DialogueRegistry.Initialise();
			ItemRegistry.Initialise();
			DialogueBuiltins.Register();

			RoadGenPlaceOne.HasInitalised = false;

#if DEBUG
			Debugging.NPCDebugger.Create();
#endif
		}

		public override void Update()
		{
			// Process trader spawning queue.
			if (TraderSpawnQueue.Count > 0)
			{
				var processed = new List<int>();
				foreach (var index in TraderSpawnQueue)
				{
					var road = TerrainGenerator.TG.roads[0].roadList[index];
					var spawnedTrader = GameObject.Instantiate(NPCs.I.GetItem(0), road.nr.position + road.nr.up * 4f + road.nr.right * 7f + -road.nr.forward * 4.5f, Quaternion.LookRotation(-road.nr.right, road.nr.up));
					// Freeze trader on spawn.
					// TODO: Test moving this to NPC Start() to have all NPCs handle it
					// themselves by default, rather than needing the spawn logic to handle it.
					var rb = spawnedTrader.GetComponent<Rigidbody>();
					if (rb != null)
						rb.isKinematic = true;

					// Ensure trader can be unfrozen correctly.
					var traderSave = spawnedTrader.GetComponent<tosaveitemscript>();
					if (traderSave != null)
					{
						traderSave.started = false;
						traderSave.FInit();
						traderSave.FStart();
					}
					processed.Add(index);
				}

				foreach (var index in processed)
					TraderSpawnQueue.Remove(index);
			}
		}
	}
}
