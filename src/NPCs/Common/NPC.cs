using NPCs.AI;
using NPCs.Appearance;
using NPCs.Dialogue;
using NPCs.Enums;
using NPCs.Utilities;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace NPCs.Common
{
	public abstract class NPC : MonoBehaviour
	{
		private const float FALLBACK_HEALTH = 20f;

		public string NPCName { get; private set; }
		public NPCAi Ai { get; private set; }
		public NPCBody Body { get; private set; }
		public NPCAppearance Appearance { get; private set; }
		public Gender Gender { get; private set; }
		public event Action OnDeath;
		public bool IsDead = false;

		protected System.Random Rng { get; private set; }
		protected ConversationRunner Runner { get; private set; }
		protected tosaveitemscript Save { get; private set; }
		protected breakablescript Breakable { get; private set; }

		protected virtual void Awake()
		{
			// The AI needs the body's head look, so the body goes first.
			Body = gameObject.AddComponent<NPCBody>();
			Ai = CreateAi();

			Runner = gameObject.AddComponent<ConversationRunner>();
			Runner.Npc = this;
			gameObject.AddComponent<SpeechRenderer>();
			Breakable = CreateBreakable();

			// Remove any mod components that aren't ours from the NPCs.
			Components.StripComponents(gameObject);
		}

		protected virtual void Start()
		{
			Save = GetComponent<tosaveitemscript>();
			Rng = new System.Random(Save.idInSave);
			Runner = GetComponent<ConversationRunner>();

			// The look comes first because the name list depends on gender.
			Appearance = CreateAppearance();
			Gender = (Gender)Appearance.Character;
			Body.SetAppearance(Appearance);

			NPCName = GenerateName();
			Runner.AddVariable("npcName", NPCName);

			OnDeath += Body.Ragdoll;
			OnDeath += PlayDeathSound;
			OnDeath += () => StartCoroutine(ResurrectionRoutine());
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
			string[] names = NPCNames.For(Gender);
			return names[Rng.Next(names.Length)];
		}

		/// <summary>
		/// Override to give an NPC a set look, or to force a gender, instead of a fully random one.
		/// </summary>
		/// <returns>The appearance to dress the NPC in</returns>
		protected virtual NPCAppearance CreateAppearance() => AppearanceGenerator.Generate(Body.Outfit, Save.idInSave);

		/// <summary>
		/// Override to provide an NPC with their own behaviour.
		/// </summary>
		/// <returns>NPCAi instance to be added to the NPC</returns>
		protected virtual NPCAi CreateAi() => gameObject.AddComponent<NPCAi>();

		// The ragdoll prefab has no health of its own, so borrow from the munkas.
		private breakablescript CreateBreakable()
		{
			var breakable = gameObject.AddComponent<breakablescript>();
			var munkas = itemdatabase.d.gmunkas01.GetComponent<breakablescript>();

			if (munkas != null)
			{
				breakable.health = munkas.health;
				breakable.decreaseAfterAttack = munkas.decreaseAfterAttack;
				breakable.onlyDecreaseAfterAttackIfAbove = munkas.onlyDecreaseAfterAttackIfAbove;
				breakable.onlyDecreaseAfterAttackIfAboveHealth = munkas.onlyDecreaseAfterAttackIfAboveHealth;
				breakable.onlyVelocity = munkas.onlyVelocity;
				breakable.massModifier = munkas.massModifier;
				breakable.childModifier = munkas.childModifier;
				breakable.shootDifferent = munkas.shootDifferent;
				breakable.shootHealth = munkas.shootHealth;
				breakable.explosionDifferent = munkas.explosionDifferent;
				breakable.explosionHealth = munkas.explosionHealth;
				breakable.noTrigger = munkas.noTrigger;
				breakable.noCollider = munkas.noCollider;
				breakable.noShoot = munkas.noShoot;
			}
			else
			{
				breakable.health = FALLBACK_HEALTH;
			}

			breakable.childs = new breakchilds[0];
			breakable.clips = new AudioClip[0];
			breakable.noGib = true;
			breakable.noDestroy = true;

			return breakable;
		}

		private void PlayDeathSound()
		{
			var munkas = itemdatabase.d.gmunkas01.GetComponent<newAiScript>();
			if (munkas == null)
				return;

			// Two of the munkas death sounds don't fit a person, so leave them out.
			AudioClip[] clips = munkas.Sound_Death.Where((clip, index) => index != 6 && index != 8).ToArray();
			if (clips.Length == 0)
				return;

			mainscript.PlayClipAtPoint(clips[Rng.Next(clips.Length)], Body.Head.position, 1f, mainscript.AudioPriorities[6]);
		}

		private IEnumerator ResurrectionRoutine()
		{
			// 1 in 3 chance of resurrection.
			if (Rng.Next(3) != 0)
				yield break;

			yield return new WaitForSeconds(3f + Rng.Next(1, 6));

			Instantiate(
				itemdatabase.d.gmunkas01,
				transform.position + Vector3.up * 1f,
				transform.rotation
			);

			foreach (var save in GetComponentsInChildren<tosaveitemscript>())
				save.removeFromMemory = true;
			Destroy(gameObject);
		}
	}
}
