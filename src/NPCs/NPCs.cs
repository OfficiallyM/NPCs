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
		private ConversationRunner _debugRunner;

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
			Trader.Flesh = bundle.LoadAsset<Texture>("flesh.png");
			var textures = bundle.LoadAllAssets<Texture>();
			Trader.Eyes = textures.Where(t => t.name.StartsWith("eyes_")).ToArray();
			Trader.Outfits = textures.Where(t => t.name.StartsWith("outfit_")).ToArray();
			Trader.Shoes = textures.Where(t => t.name.StartsWith("shoes_")).ToArray();
			bundle.Unload(false);

			RegisterItem(itemdatabase.d.gmunkas01, 0, "Trader")
				.WithRigidbody(1)
				.AddComponent<Trader>()
				.AddComponent<ConversationRunner>()
				.AddComponent<SpeechRenderer>()
				.Register();

			Components.StripComponents(GetItem(0));
			if (mainscript.M.player.GetComponent<ConversationUI>() == null)
				mainscript.M.player.gameObject.AddComponent<ConversationUI>();
		}

		public override void OnLoad()
		{
			DialogueRegistry.Initialise();
			ItemValue.Initialise();

			DialogueRegistry.RegisterAction("open_trade", () => Logging.LogDebug("Action fired: open_trade"));
		}

		public override void Update()
		{
			if (Input.GetKeyDown(KeyCode.Comma))
			{
				_debugSelected = null;
				_debugRunner = null;
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
					_debugRunner = _debugSelected.GetComponent<ConversationRunner>();
					return;
				}
				_debugSelected = null;
				_debugRunner = null;
			}

			if (Input.GetKeyDown(KeyCode.Slash) && _debugRunner != null)
				_debugRunner.StartConversation("trader_test", new Dictionary<string, string>()
				{
					{ "playerName", System.Environment.UserName }
				});
		}

		public override void OnGUI()
		{
			if (_debugSelected == null) return;

			GUI.Button(new Rect(0, 0, 600, 30), $"Selected: {_debugSelected.name} - Value: {ItemValue.GetValue(_debugSelected)}g");

			if (_debugRunner == null) return;

			if (!_debugRunner.IsActive)
			{
				GUI.Button(new Rect(0, 35, 600, 30), "No active conversation. Press / to start trader_test.");
				return;
			}

			//ConversationNode node = _debugRunner.CurrentNode;
			//GUI.Button(new Rect(0, 35, 600, 30), $"Node: {node.Id}");
			//GUI.Button(new Rect(0, 70, 600, 60), _debugRunner.ResolveText(node.Text));

			//bool hasOptions = node.Options != null && node.Options.Count > 0;

			//if (hasOptions)
			//{
			//	for (int i = 0; i < node.Options.Count; i++)
			//	{
			//		if (GUI.Button(new Rect(0, 135 + (i * 35), 600, 30), $"[{i}] {node.Options[i].Text}"))
			//			_debugRunner.SelectOption(i);
			//	}
			//}
			//else
			//{
			//	if (GUI.Button(new Rect(0, 135, 600, 30), node.Next != null ? "Continue..." : "Close"))
			//		_debugRunner.Advance();
			//}
		}
	}
}
