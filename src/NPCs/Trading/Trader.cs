using System.Linq;
using NPCs.Utilities;
using UnityEngine;

namespace NPCs.Trading
{
	internal class Trader : MonoBehaviour
	{
		public static Texture Flesh;
		public static Texture[] Eyes;
		public static Texture[] Outfits;
		public static Texture[] Shoes;

		private System.Random _rng;
		private Rigidbody _rb;
		private tosaveitemscript _save;

		public void Start()
		{
			_rb = GetComponent<Rigidbody>();
			_rb.drag = 5f;
			_save = GetComponent<tosaveitemscript>();
			_rng = new System.Random(_save.idInSave);
			SetAppearance();
			GetComponent<newAiScript>().enabled = false;
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
			var eyeTexture = Eyes[_rng.Next(Eyes.Length)];
			var outfitTexture = Outfits[_rng.Next(Outfits.Length)];
			var shoesTexture = Shoes[_rng.Next(Shoes.Length)];
			eyes.material.SetTexture("_MainTex", eyeTexture);
			clothes.material.SetTexture("_MainTex", outfitTexture);
			shoes.material.SetTexture("_MainTex", shoesTexture);
		}
	}
}
