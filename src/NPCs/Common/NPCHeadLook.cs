using UnityEngine;

namespace NPCs.Common
{
	/// <summary>
	/// Turns the neck and head bones towards a world point, lagging behind the target and respecting turn limits.
	/// Replaces the look-at IK the vanilla munkas animator provided.
	/// </summary>
	public class NPCHeadLook
	{
		// Degrees. Kept under a full side-on turn so the neck never looks broken.
		private const float MAX_YAW = 80f;
		private const float MAX_PITCH_UP = 45f;
		private const float MAX_PITCH_DOWN = 35f;

		// The neck takes this share of the turn so the head isn't doing all the work.
		private const float NECK_SHARE = 0.4f;

		private readonly Transform _root;
		private readonly Transform _neck;
		private readonly Transform _head;
		private readonly Quaternion _neckRest;
		private readonly Quaternion _headRest;
		private readonly float _lerp;

		/// <summary>
		/// The world point the NPC wants to look at.
		/// </summary>
		public Vector3 Target { get; set; }

		/// <summary>
		/// The world point the head is actually looking at, which trails Target.
		/// </summary>
		public Vector3 Current { get; private set; }

		/// <param name="root">The NPC root, whose forward is the direction the model faces.</param>
		/// <param name="neckRest">The neck's rotation relative to the root in the model's neutral pose.</param>
		/// <param name="headRest">The head's rotation relative to the root in the model's neutral pose.</param>
		/// <param name="lerp">How quickly the head catches up with the target.</param>
		public NPCHeadLook(Transform root, Transform neck, Transform head, Quaternion neckRest, Quaternion headRest, float lerp)
		{
			_root = root;
			_neck = neck;
			_head = head;
			_neckRest = neckRest;
			_headRest = headRest;
			_lerp = lerp;
		}

		/// <summary>
		/// Jumps straight to the target so there is no swing on the first frame.
		/// </summary>
		public void Snap() => Current = Target;

		public void Tick(float deltaTime)
		{
			Current = Vector3.Lerp(Current, Target, deltaTime * _lerp);
		}

		public void Apply()
		{
			Vector3 local = _root.InverseTransformDirection(Current - _head.position);
			if (local.sqrMagnitude < 0.0001f)
				return;

			local.Normalize();

			// Positive pitch looks down and positive yaw looks right, matching Quaternion.Euler.
			float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -MAX_YAW, MAX_YAW);
			float pitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg, -MAX_PITCH_UP, MAX_PITCH_DOWN);
			Quaternion look = Quaternion.Euler(pitch, yaw, 0f);

			// Both are set absolutely from the neutral pose, so nothing accumulates between frames.
			_neck.rotation = _root.rotation * Quaternion.Slerp(Quaternion.identity, look, NECK_SHARE) * _neckRest;
			_head.rotation = _root.rotation * look * _headRest;
		}
	}
}
