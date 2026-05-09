using NPCs.Common;
using NPCs.Dialogue;
using NPCs.Harmony;
using NPCs.Trading;
using NPCs.Trading.Value;
using NPCs.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TLDLoader;
using UnityEngine;

namespace NPCs
{
	public class NPCs : Mod
	{
		// Mod meta stuff.
		private string _version = "0.0.1";
		public override string ID => "M_NPCs";
		public override string Name => "Friendly NPCs";
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

		internal List<int> traderSpawnQueue = new List<int>();

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
			NPC.Flesh = bundle.LoadAsset<Texture>("flesh.png");
			var textures = bundle.LoadAllAssets<Texture>();
			NPC.Eyes = textures.Where(t => t.name.StartsWith("eyes_")).ToArray();
			NPC.Outfits = textures.Where(t => t.name.StartsWith("outfit_")).ToArray();
			NPC.Shoes = textures.Where(t => t.name.StartsWith("shoes_")).ToArray();
			var silver = bundle.LoadAsset<GameObject>("silver");
			bundle.Unload(false);

			// NPC item IDs: 0 - 100.
			RegisterItem(itemdatabase.d.gmunkas01, 0, "Trader")
				.WithRigidbody(90, 5)
				.AddComponent<Trader>()
				.AddComponent<ConversationRunner>()
				.AddComponent<TradeSession>()
				.AddComponent<SpeechRenderer>()
				.Register();

			// Other item IDs: 100+.
			RegisterItem(silver, 100)
				.WithRigidbody(15.5f)
				.AsPickupable(new PickupableOptions() { Attachable = true, CanInventory = true })
				.SpawnInBox(maxPerBox: 6)
				.SpawnAt(6, itemdatabase.d.ggold)
				.Register();

			var trader = GetItem(0);
			Components.StripComponents(trader);
			trader.GetComponent<ConversationRunner>().ConversationId = "trader_test";

			if (mainscript.M.player.GetComponent<ConversationUI>() == null)
				mainscript.M.player.gameObject.AddComponent<ConversationUI>();
		}

		public override void OnLoad()
		{
			DialogueRegistry.Initialise();
			ItemRegistry.Initialise();

			DialogueRegistry.RegisterAction("open_trade", runner => {
				TradeSession session = runner.GetComponent<TradeSession>();
				if (session == null) return;
				session.Begin();
			});

			RoadGenPlaceOne.HasInitalised = false;
		}

		public override void Update()
		{
			// Process trader spawning queue.
			if (traderSpawnQueue.Count > 0)
			{
				var processed = new List<int>();
				foreach (var index in traderSpawnQueue)
				{
					var road = TerrainGenerator.TG.roads[0].roadList[index];
					var spawnedTrader = GameObject.Instantiate(NPCs.I.GetItem(0), road.nr.position + road.nr.up * 4f + road.nr.right * 7f + -road.nr.forward * 4.5f, Quaternion.LookRotation(-road.nr.right, road.nr.up));
					// Freeze trader on spawn.
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
					traderSpawnQueue.Remove(index);
			}
		}
	}
}
