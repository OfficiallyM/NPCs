using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCs.Common
{
	// Runs before the model's own late updates so the visible skeleton copies the pose set this frame.
	[DefaultExecutionOrder(-50)]
	public class NPCBody : MonoBehaviour
	{
		public const int MALE = 0;
		public const int FEMALE = 1;

		private const int MAX_CHARACTER_ATTEMPTS = 16;
		private const float DEFAULT_LOOK_LERP = 5f;

		private const float HITBOX_HEIGHT = 1.6f;
		private const float HITBOX_RADIUS = 0.3f;
		private const float HITBOX_CENTRE_Y = 0.8f;
		private const float SPEECH_ANCHOR_HEIGHT = 1.725f;

		// Resting arm positioning.
		private const float UPPER_ARM_OUTWARD = 0.11f;
		private const float UPPER_ARM_FORWARD = 0.05f;
		private const float FOREARM_FORWARD = 0.2f;

		private class Arm
		{
			public Transform Upper;
			public Transform Lower;
			public Transform Hand;
			public Quaternion UpperRest;
			public Quaternion LowerRest;
			public Vector3 UpperDirection;
			public Vector3 LowerDirection;
		}

		private playermodeloutfitscript _outfit;
		private ragdolPosScript _rig;
		private Dictionary<string, Transform> _bones;
		private Arm[] _arms;
		private Rigidbody _rootBody;
		private Rigidbody[] _limbBodies;
		private Collider[] _limbColliders;
		private GameObject _hitbox;
		private bool _ragdolled;

		public NPCHeadLook Look { get; private set; }

		public Transform Head => _rig.head;

		/// <summary>
		/// A point above the head for worldspace UI to hang from. It stays upright when the head turns.
		/// </summary>
		public Transform SpeechAnchor { get; private set; }

		public bool IsRagdolled => _ragdolled;

		private void Awake()
		{
			_outfit = GetComponent<playermodeloutfitscript>();
			_rig = GetComponent<ragdolPosScript>();
			_bones = IndexBones(_rig);

			BuildPose();
			PrepareBodies();
			BuildHitbox();

			SpeechAnchor = new GameObject("SpeechAnchor").transform;
			SpeechAnchor.SetParent(transform, false);
			SpeechAnchor.localPosition = new Vector3(0f, SPEECH_ANCHOR_HEIGHT, 0f);
		}

		/// <summary>
		/// Gives the NPC a random outfit that is the same every time for the same seed.
		/// </summary>
		public void SetOutfit(int seed, int character)
		{
			// The prefab ships with its renderers off. Refresh only toggles the character meshes, so the rest must be on first.
			foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
				renderer.enabled = true;

			// Seeded so an NPC keeps its look between loads, without disturbing the global random state.
			Random.State state = Random.state;
			Random.InitState(seed);

			_outfit.SetRandom(false);

			// SetRandom picks the model itself, so keep rolling until it lands on the one asked for.
			for (int i = 0; i < MAX_CHARACTER_ATTEMPTS && _outfit.selectedCharacter != character; i++)
				_outfit.SetRandom(false);

			Random.state = state;

			// SetRandom leaves the player-camera fade distance applied, which would dither the body.
			_outfit.ReSetDistance();
		}

		/// <summary>
		/// Hands the body over to physics.
		/// </summary>
		public void Ragdoll()
		{
			if (_ragdolled)
				return;
			_ragdolled = true;

			Vector3 velocity = _rootBody != null ? _rootBody.velocity : Vector3.zero;

			if (_hitbox != null)
				Destroy(_hitbox);

			// The limbs carry the physics from here, same as the vanilla munkas.
			if (_rootBody != null)
				Destroy(_rootBody);

			foreach (Collider limb in _limbColliders)
				limb.enabled = true;

			foreach (Rigidbody limb in _limbBodies)
			{
				limb.isKinematic = false;
				limb.velocity = velocity;
			}
		}

		private void LateUpdate()
		{
			if (_ragdolled)
				return;

			foreach (Arm arm in _arms)
			{
				arm.Upper.localRotation = arm.UpperRest;
				Aim(arm.Upper, arm.Lower, arm.UpperDirection);
				arm.Lower.localRotation = arm.LowerRest;
				Aim(arm.Lower, arm.Hand, arm.LowerDirection);
			}

			Look.Tick(Time.deltaTime);
			Look.Apply();
		}

		// Swings a bone about its pivot until the line to its child points the wanted way, whatever the bone's own axes are.
		private void Aim(Transform bone, Transform child, Vector3 localDirection)
		{
			Vector3 current = child.position - bone.position;
			if (current.sqrMagnitude < 0.0001f)
				return;

			bone.rotation = Quaternion.FromToRotation(current, transform.TransformDirection(localDirection)) * bone.rotation;
		}

		private static Dictionary<string, Transform> IndexBones(ragdolPosScript rig)
		{
			var bones = new Dictionary<string, Transform>();
			foreach (Transform bone in rig.ragdolT)
			{
				if (bone != null)
					bones[bone.name] = bone;
			}
			return bones;
		}

		private void BuildPose()
		{
			// The neutral pose comes from the prefab asset, because a loaded save may already have posed these bones.
			GameObject template = itemdatabase.d.gragdoll;
			ragdolPosScript templateRig = template.GetComponent<ragdolPosScript>();
			Dictionary<string, Transform> templateBones = IndexBones(templateRig);
			Transform templateRoot = template.transform;

			_arms = new[]
			{
				BuildArm("l", templateBones, templateRoot),
				BuildArm("r", templateBones, templateRoot),
			};

			float lerp = DEFAULT_LOOK_LERP;
			newAiAnim munkasAnim = itemdatabase.d.gmunkas01.GetComponentInChildren<newAiAnim>(true);
			if (munkasAnim != null)
				lerp = munkasAnim.lookLerp;

			Look = new NPCHeadLook(
				transform,
				_bones["neck_01"],
				_rig.head,
				RotationInRoot(templateRoot, templateBones["neck_01"]),
				RotationInRoot(templateRoot, templateRig.head),
				lerp
			);
		}

		private Arm BuildArm(string side, Dictionary<string, Transform> templateBones, Transform templateRoot)
		{
			Transform templateUpper = templateBones["upperarm_" + side];
			Transform templateLower = templateBones["lowerarm_" + side];

			// The arms stick out sideways in the neutral pose, so which way they point says which way is outwards.
			float outward = Mathf.Sign(templateRoot.InverseTransformDirection(templateLower.position - templateUpper.position).x);

			return new Arm
			{
				Upper = _bones["upperarm_" + side],
				Lower = _bones["lowerarm_" + side],
				Hand = _bones["hand_" + side],
				UpperRest = templateUpper.localRotation,
				LowerRest = templateLower.localRotation,
				UpperDirection = new Vector3(outward * UPPER_ARM_OUTWARD, -1f, UPPER_ARM_FORWARD).normalized,
				LowerDirection = new Vector3(outward * UPPER_ARM_OUTWARD * 0.5f, -1f, FOREARM_FORWARD).normalized,
			};
		}

		private static Quaternion RotationInRoot(Transform root, Transform bone) => Quaternion.Inverse(root.rotation) * bone.rotation;

		private void PrepareBodies()
		{
			_rootBody = GetComponent<Rigidbody>();
			if (_rootBody == null)
				_rootBody = gameObject.AddComponent<Rigidbody>();

			// Keeps the living NPC upright; the player body uses the same constraint.
			_rootBody.constraints = RigidbodyConstraints.FreezeRotation;

			// While alive the limbs are puppeted, and their colliders stay off like the player's own ragdoll.
			// That also stops the limbs offering themselves up as pickups.
			_limbBodies = GetComponentsInChildren<Rigidbody>(true).Where(b => b.gameObject != gameObject).ToArray();
			_limbColliders = GetComponentsInChildren<Collider>(true).Where(c => c.gameObject != gameObject).ToArray();

			foreach (Rigidbody limb in _limbBodies)
				limb.isKinematic = true;

			foreach (Collider limb in _limbColliders)
				limb.enabled = false;
		}

		private void BuildHitbox()
		{
			_hitbox = new GameObject("NPC_Hitbox");
			_hitbox.transform.SetParent(transform, false);

			var capsule = _hitbox.AddComponent<CapsuleCollider>();
			capsule.height = HITBOX_HEIGHT;
			capsule.radius = HITBOX_RADIUS;
			capsule.center = new Vector3(0f, HITBOX_CENTRE_Y, 0f);

			// Borrow the munkas collider's layer and material so the player's talk and shoot rays and the ground friction behave as before.
			newAiScript munkas = itemdatabase.d.gmunkas01.GetComponent<newAiScript>();
			if (munkas != null && munkas.col != null)
			{
				_hitbox.layer = munkas.col.gameObject.layer;
				capsule.sharedMaterial = munkas.col.sharedMaterial;
			}
		}
	}
}
