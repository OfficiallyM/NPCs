using System.Linq;
using System.Reflection;
using TLDLoader;
using Traders.Components;
using Traders.Trading;
using Traders.Utilities;
using UnityEngine;

namespace Traders
{
	public class Traders : Mod
	{
		// Mod meta stuff.
		private string _version = "0.0.1";
		public override string ID => "M_Traders";
		public override string Name => "Traders";
		public override string Author => "M-";
		public override string Version => _version;
		public override bool UseLogger => true;
		public override bool LoadInDB => true;

		internal static Traders I;
		internal static bool Debug = false;

		private GameObject _debugSelected;

		public Traders()
		{
			I = this;
#if DEBUG
			_version += "-DEV";
			Debug = true;
#endif
		}

		public override void DbLoad()
		{
			AssetBundle bundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(Traders)}.traders"));
			Trader.Flesh = bundle.LoadAsset<Texture>("flesh.png");
			var textures = bundle.LoadAllAssets<Texture>();
			Trader.Eyes = textures.Where(t => t.name.StartsWith("eyes_")).ToArray();
			Trader.Outfits = textures.Where(t => t.name.StartsWith("outfit_")).ToArray();
			Trader.Shoes = textures.Where(t => t.name.StartsWith("shoes_")).ToArray();
			bundle.Unload(false);

			RegisterItem(itemdatabase.d.gmunkas01, 0, "Trader")
				.WithRigidbody(1)
				.AddComponent<Trader>()
				.Register();

			var trader = GetItem(0);
			Utilities.Components.StripComponents(trader);	
		}

		public override void OnLoad()
		{
			ItemValue.Initialise();
		}

		public override void Update()
		{
			if (Input.GetKeyDown(KeyCode.Comma))
				_debugSelected = null;

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
			if (_debugSelected != null)
				GUI.Button(new Rect(0, 0, 600, 30), $"Selected: {_debugSelected.name} - Value: {ItemValue.GetValue(_debugSelected)}g");
		}
	}
}
