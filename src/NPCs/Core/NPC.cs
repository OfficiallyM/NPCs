using NPCs.AI;
using NPCs.Animation;
using NPCs.Animation.Models;
using NPCs.Appearance;
using NPCs.Core.Enums;
using NPCs.Dialogue;
using NPCs.Harmony;
using NPCs.Utilities;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace NPCs.Core
{
	public abstract class NPC : MonoBehaviour
	{
		private const float FALLBACK_HEALTH = 20f;

		private visszarako _shifter;

		public string NPCName { get; private set; }
		public NPCAi Ai { get; private set; }
		public NPCBody Body { get; private set; }
		public NPCAnimation Animation { get; private set; }
		public NPCLocomotion Locomotion { get; private set; }
		public NPCAppearance Appearance { get; private set; }
		public Gender Gender { get; private set; }
		// TODO: Save once we have a save system.
		public NPCMemory Memory { get; } = new NPCMemory();
		public event Action OnDeath;
		public bool IsDead = false;

		protected System.Random Rng { get; private set; }
		protected ConversationRunner Runner { get; private set; }
		protected tosaveitemscript Save { get; private set; }
		protected breakablescript Breakable { get; private set; }

		protected virtual void Awake()
		{
			// The animation binds to the body's bones and the AI drives the animation, so they're built in that order.
			Body = gameObject.AddComponent<NPCBody>();
			Animation = gameObject.AddComponent<NPCAnimation>();
			Animation.Initialise(this, CreateAnimationProfile());
			Locomotion = gameObject.AddComponent<NPCLocomotion>();
			Ai = CreateAi();

			Runner = gameObject.AddComponent<ConversationRunner>();
			Runner.Npc = this;
			gameObject.AddComponent<SpeechRenderer>();
			Breakable = CreateBreakable();

			// Register the NPC to handle origin shifting.
			_shifter = GetComponent<visszarako>();
			if (_shifter != null)
				WorldShiftPatch.Register(_shifter, this);

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

			// The profile is built in Awake, before the gender is known, so the gender's part of it is layered on here.
			Animation.ApplyOverride(CreateAnimationOverride(Gender));
			Body.SetAppearance(Appearance);

			NPCName = GenerateName();
			Runner.AddVariable("npcName", NPCName);

			OnDeath += Body.Ragdoll;
			OnDeath += PlayDeathSound;
			OnDeath += () => StartCoroutine(ResurrectionRoutine());
		}

		// Components are looked up each time because a shift is rare and subclasses add their own after this one has been built.
		internal void OnWorldShifted(Vector3 delta)
		{
			foreach (IWorldShiftable shiftable in GetComponents<IWorldShiftable>())
				shiftable.OnWorldShifted(delta);
		}

		// Unity treats the component as null once it's destroyed, but the dictionary key still needs removing.
		private void OnDestroy()
		{
			if (!ReferenceEquals(_shifter, null))
				WorldShiftPatch.Unregister(_shifter);
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

		/// <summary>
		/// Supplies per-NPC animation tuning. Override this to customise gestures for a specific NPC type.
		/// </summary>
		protected virtual NPCAnimationProfile CreateAnimationProfile() => new NPCAnimationProfile();

		/// <summary>
		/// Supplies the animation values that depend on gender, laid over the base profile once the gender is known.
		/// Override this to give an NPC type its own, either replacing the standard one or merged over it with Merge.
		/// </summary>
		/// <param name="gender">The gender this NPC was dressed as</param>
		protected virtual NPCAnimationOverride CreateAnimationOverride(Gender gender) => NPCAnimationOverrides.For(gender);

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
