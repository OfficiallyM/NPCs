using NPCs.Dialogue;
using System;
using System.Collections.Generic;
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
		public event Action OnDeath;
		public bool IsDead = false;

		protected System.Random Rng { get; private set; }
		protected ConversationRunner Runner { get; private set; }
		protected tosaveitemscript Save { get; private set; }
		protected breakablescript Breakable { get; private set; }

		protected virtual void Start()
		{
			Save = GetComponent<tosaveitemscript>();
			Rng = new System.Random(Save.idInSave);
			Runner = GetComponent<ConversationRunner>();
			Breakable = GetComponent<breakablescript>();
			NPCName = GenerateName();
			Runner.AddVariable("npcName", NPCName);
			Runner.Npc = this;
			SetAppearance();

			var ai = GetComponent<newAiScript>();
			// Prevent any default voice clips from playing.
			ai.Sound_Idle = new AudioClip[0];
			ai.Sound_Chase = new AudioClip[0];
			ai.Sound_Notice = new AudioClip[0];
			ai.Sound_Attack = new AudioClip[0];
			ai.SIdleList.Clear();
			ai.SChaseList.Clear();
			ai.SNoticeList.Clear();
			ai.SAttackList.Clear();

			// Remove any death sounds that don't fit the NPCs.
			var deathSounds = new List<AudioClip>(ai.Sound_Death);
			deathSounds.RemoveAt(8);
			deathSounds.RemoveAt(6);
			ai.Sound_Death = deathSounds.ToArray();

			// Stop any potentially running clips.
			ai.SourceVoice.Stop();

			ai.enabled = false;
		}

		private void Update()
		{
			if (IsDead)
				return;

			if (Breakable.destroyed)
			{
				OnDeath?.Invoke();
				IsDead = true;
			}
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
