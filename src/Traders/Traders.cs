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
	}
}
