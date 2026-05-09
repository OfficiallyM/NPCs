using NPCs.Dialogue;
using System.Linq;
using UnityEngine;

namespace NPCs.Common
{
	public abstract class NPC : MonoBehaviour
	{
		public static Texture Flesh;
		public static Texture[] Eyes;
		public static Texture[] Outfits;
		public static Texture[] Shoes;

		public string NPCName { get; private set; }

		protected System.Random Rng { get; private set; }
		protected ConversationRunner Runner { get; private set; }
		protected tosaveitemscript Save { get; private set; }

		protected virtual void Start()
		{
			Save = GetComponent<tosaveitemscript>();
			Rng = new System.Random(Save.idInSave);
			Runner = GetComponent<ConversationRunner>();
			NPCName = GenerateName();
			Runner.AddVariable("npcName", NPCName);
			SetAppearance();
			GetComponent<newAiScript>().enabled = false;
		}

		protected virtual string GenerateName()
		{
			string[] names = new string[]
			{
				"Dave", "Terry", "Mick", "Gary", "Kev", "Baz", "Steve", "Len", "Pete",
				"Reg", "Norm", "Brian",
			};

			return names[Rng.Next(names.Length)];
		}

		private void SetAppearance()
		{
			Transform meshRoot = transform.Find("munkas01");
			if (meshRoot == null)
				return;

			var renderers = meshRoot.GetComponentsInChildren<Renderer>();
			var skin = renderers.FirstOrDefault(r => r.name == "munkas01_20221025cMesh");
			var eyes = renderers.FirstOrDefault(r => r.name == "low-polyMesh");
			var clothes = renderers.FirstOrDefault(r => r.name == "male_worksuit01Mesh");
			var shoes = renderers.FirstOrDefault(r => r.name == "shoes01Mesh");

			// Unmunkasify the munkas.
			skin.material.SetTexture("_DetailAlbedoMap", Flesh);
			skin.material.SetTexture("_DetailMask", null);
			skin.material.SetTexture("_DetailAlbedoMap", null);
			skin.material.color = Color.white;
			eyes.material.SetTexture("_DetailMask", null);
			eyes.material.SetTexture("_DetailAlbedoMap", null);
			eyes.material.color = Color.white;
			clothes.material.SetTexture("_DetailMask", null);
			clothes.material.SetTexture("_DetailAlbedoMap", null);
			shoes.material.SetTexture("_DetailMask", null);
			shoes.material.SetTexture("_DetailAlbedoMap", null);

			// Randomise their appearance.
			var eyeTexture = Eyes[Rng.Next(Eyes.Length)];
			var outfitTexture = Outfits[Rng.Next(Outfits.Length)];
			var shoesTexture = Shoes[Rng.Next(Shoes.Length)];
			eyes.material.SetTexture("_MainTex", eyeTexture);
			clothes.material.SetTexture("_MainTex", outfitTexture);
			shoes.material.SetTexture("_MainTex", shoesTexture);
		}
	}
}
