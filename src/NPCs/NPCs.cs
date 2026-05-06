using NPCs.Common;
using NPCs.Dialogue;
using NPCs.Dialogue.Core;
using NPCs.Trading;
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

		private GameObject _debugSelected;

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
			ItemValue.Initialise();

			DialogueRegistry.RegisterAction("open_trade", runner => {
				TradeSession session = runner.GetComponent<TradeSession>();
				if (session == null) return;
				session.Begin();
			});
		}

		public override void Update()
		{
			if (Input.GetKeyDown(KeyCode.Comma))
			{
				_debugSelected = null;
			}

			if (Input.GetKeyDown(KeyCode.Period))
			{
				Physics.Raycast(mainscript.M.player.Cam.transform.position, mainscript.M.player.Cam.transform.forward, out var raycastHit, float.PositiveInfinity, mainscript.M.player.useLayer);
				if (raycastHit.collider != null && raycastHit.collider.gameObject != null)
				{
					GameObject hitGameObject = raycastHit.collider.transform.gameObject;

					// Recurse upwards to find a tosaveitemscript.
					tosaveitemscript save = hitGameObject.GetComponentInParent<tosaveitemscript>();

					// Can't find the tosaveitemscript, return early.
					if (save == null) return;

					_debugSelected = save.gameObject;
					return;
				}
				_debugSelected = null;
			}
		}

		public override void OnGUI()
		{
			if (_debugSelected == null) return;

			GUI.Button(new Rect(0, 0, 600, 30), $"Selected: {_debugSelected.name} - Value: {ItemValue.GetValue(_debugSelected)}g");
		}
	}
}
