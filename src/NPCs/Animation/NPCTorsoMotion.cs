using NPCs.Animation.Models;
using NPCs.Core;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Animation
{
	/// <summary>
	/// The upper body's part in a walk: the pelvis turns, tips and sways, the spine twists against it and the arms swing opposite the legs.
	/// All of it follows the leg gait's stride phase, so it can't drift out of step with the feet.
	/// </summary>
	public class NPCTorsoMotion
	{
		private const float EPSILON = 0.0001f;

		private class SpineBone
		{
			public Transform Bone;
			public Quaternion RestLocal;
		}

		private class ArmBone
		{
			public Transform Upper;
			public Transform Lower;

			// Which way the arm swings while the left leg is forward. The arms swing opposite their own side's leg.
			public float Direction;

			// How much of the swing the arm is allowed. A gesture takes the arm over, so this fades to zero while one plays.
			public float Freedom = 1f;
		}

		private readonly Transform _root;
		private readonly NPCAnimationProfile _profile;
		private readonly Transform _pelvis;
		private readonly Quaternion _pelvisRest;
		private readonly List<SpineBone> _spine;
		private readonly ArmBone[] _arms;

		// Which side of the root the model's left bones are on, as 1 for +x and -1 for -x. The model's left isn't always on the same side.
		private readonly float _leftSide;

		public bool HasPelvis => _pelvis != null;

		public int SpineCount => _spine.Count;

		private NPCTorsoMotion(
			Transform root,
			NPCAnimationProfile profile,
			Transform pelvis,
			Quaternion pelvisRest,
			List<SpineBone> spine,
			ArmBone[] arms,
			float leftSide)
		{
			_root = root;
			_profile = profile;
			_pelvis = pelvis;
			_pelvisRest = pelvisRest;
			_spine = spine;
			_arms = arms;
			_leftSide = leftSide;
		}

		/// <summary>
		/// Binds whatever of the torso the model has. Returns null if there is nothing to move at all.
		/// </summary>
		public static NPCTorsoMotion TryCreate(NPCBody body, Transform root, NPCAnimationProfile profile)
		{
			Transform pelvis = null;
			Quaternion pelvisRest = Quaternion.identity;
			if (body.TryGetBone("pelvis", out Transform pelvisBone) && body.TryGetNeutralRotation(pelvisBone, out pelvisRest))
				pelvis = pelvisBone;

			var spine = new List<SpineBone>();
			foreach (string name in new[] { "spine_01", "spine_02", "spine_03" })
			{
				// Each bone's rest is taken from how it sits against its parent in the neutral pose, because only the neutral pose can be trusted.
				if (body.TryGetBone(name, out Transform bone)
					&& bone.parent != null
					&& body.TryGetNeutralRotation(bone, out Quaternion boneRest)
					&& body.TryGetNeutralRotation(bone.parent, out Quaternion parentRest))
				{
					spine.Add(new SpineBone { Bone = bone, RestLocal = Quaternion.Inverse(parentRest) * boneRest });
				}
			}

			var arms = new List<ArmBone>();
			if (TryBuildArm(body, "l", -1f, out ArmBone left))
				arms.Add(left);
			if (TryBuildArm(body, "r", 1f, out ArmBone right))
				arms.Add(right);

			if (pelvis == null && spine.Count == 0 && arms.Count == 0)
				return null;

			float leftSide = 1f;
			if (body.TryGetBone("upperarm_l", out Transform leftArm))
				leftSide = root.InverseTransformPoint(leftArm.position).x >= 0f ? 1f : -1f;
			else if (body.TryGetBone("thigh_l", out Transform leftLeg))
				leftSide = root.InverseTransformPoint(leftLeg.position).x >= 0f ? 1f : -1f;

			return new NPCTorsoMotion(root, profile, pelvis, pelvisRest, spine, arms.ToArray(), leftSide);
		}

		/// <summary>
		/// Sets how much of the swing each arm gets, from 0 (a gesture has the arm) to 1 (free to swing).
		/// </summary>
		public void SetArmFreedom(float left, float right)
		{
			foreach (ArmBone arm in _arms)
				arm.Freedom = Mathf.Clamp01(arm.Direction < 0f ? left : right);
		}

		/// <summary>
		/// Puts the pelvis and spine back in the neutral pose, so nothing from last frame carries over.
		/// </summary>
		public void ResetPose()
		{
			if (_pelvis != null)
				_pelvis.rotation = _root.rotation * _pelvisRest;

			foreach (SpineBone spine in _spine)
				spine.Bone.localRotation = spine.RestLocal;
		}

		/// <summary>
		/// Adds the walking motion on top of the neutral pose. Call it after ResetPose and before the legs are solved.
		/// </summary>
		/// <param name="phase">The leg gait's stride phase, where 0 is the left foot at its furthest forward.</param>
		/// <param name="moveBlend">0 when standing still, rising to 1 once walking.</param>
		/// <param name="speedBlend">0 at the slow end of the gait and 1 at the fast end.</param>
		public void Apply(float phase, float moveBlend, float speedBlend)
		{
			if (moveBlend <= EPSILON)
				return;

			float angle = phase * Mathf.PI * 2f;

			// Positive when the left foot is ahead.
			float leftAhead = Mathf.Cos(angle);

			// Positive when the weight is over the left foot.
			float leftLoaded = Mathf.Sin(angle);

			float scale = Mathf.Lerp(1f, _profile.TorsoFastScale, speedBlend) * moveBlend;

			// How far the pelvis is tipped this frame, kept so the spine can lean back against it.
			float roll = 0f;

			if (_pelvis != null)
			{
				// Turning to the right swings the right side back, so the side with the leading foot is turned forward by turning away from it.
				float pelvisYaw = -_leftSide * _profile.PelvisYaw * leftAhead * scale;
				_pelvis.rotation = Quaternion.AngleAxis(pelvisYaw, _root.up) * _pelvis.rotation;

				// The hip on the side of the foot in the air drops. A positive turn about the forward axis lifts the +x side, so the sign follows which side the left is on.
				roll = _leftSide * _profile.PelvisRoll * leftLoaded * scale;
				_pelvis.rotation = Quaternion.AngleAxis(roll, _root.forward) * _pelvis.rotation;

				// The pelvis shifts over whichever foot is taking the weight.
				_pelvis.position += _root.right * (_leftSide * _profile.PelvisSway * leftLoaded * scale);
			}

			if (_spine.Count > 0)
			{
				// The shoulders turn against the pelvis. The spine inherits the pelvis turn, so it only has to add what is left over.
				float twist = _leftSide * (_profile.PelvisYaw + _profile.ShoulderYaw) * leftAhead * scale;
				float twistShare = twist / _spine.Count;

				// The spine leans back against the pelvis tip so the shoulders stay close to level.
				float rollShare = -roll * Mathf.Clamp(_profile.TorsoRollCompensation, 0f, 1.5f) / _spine.Count;

				// Parent first, because each bone carries everything below it along with it.
				foreach (SpineBone spine in _spine)
					spine.Bone.rotation = Quaternion.AngleAxis(twistShare, _root.up) * Quaternion.AngleAxis(rollShare, _root.forward) * spine.Bone.rotation;
			}

			foreach (ArmBone arm in _arms)
				SwingArm(arm, arm.Direction * leftAhead, speedBlend, moveBlend);
		}

		// Forward is 1 with the arm at its furthest forward and -1 at its furthest back.
		private void SwingArm(ArmBone arm, float forward, float speedBlend, float moveBlend)
		{
			float weight = moveBlend * arm.Freedom;
			if (weight <= EPSILON)
				return;

			float swing = Mathf.Lerp(_profile.ArmSwingSlow, _profile.ArmSwingFast, speedBlend) * weight;

			// A positive turn about the right axis takes a hanging arm backwards, so a forward swing is a negative one.
			arm.Upper.rotation = Quaternion.AngleAxis(-forward * swing, _root.right) * arm.Upper.rotation;

			// The elbow bends more as the arm comes forward, which is what keeps the swing from looking like a pendulum.
			float flex = Mathf.Lerp(_profile.ArmElbowFlexSlow, _profile.ArmElbowFlexFast, speedBlend) * weight * (0.5f + 0.5f * forward);
			arm.Lower.rotation = Quaternion.AngleAxis(-flex, _root.right) * arm.Lower.rotation;
		}

		private static bool TryBuildArm(NPCBody body, string side, float direction, out ArmBone arm)
		{
			arm = null;

			if (!body.TryGetBone("upperarm_" + side, out Transform upper)
				|| !body.TryGetBone("lowerarm_" + side, out Transform lower)
				|| !lower.IsChildOf(upper))
			{
				return false;
			}

			arm = new ArmBone { Upper = upper, Lower = lower, Direction = direction };
			return true;
		}
	}
}
