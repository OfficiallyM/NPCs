using NPCs.Animation.Models;
using NPCs.Core;
using UnityEngine;

namespace NPCs.Animation
{
	/// <summary>
	/// Walks the legs. The gait is driven by distance travelled rather than time, so a foot that is planted stays put on the ground.
	/// </summary>
	public class NPCLegGait
	{
		private const float MIN_DELTA_TIME = 0.0001f;
		private const float MIN_WEIGHT = 0.001f;
		private const float EPSILON = 0.0001f;
		private const float MIN_POLE_DISTANCE = 0.1f;

		// A jump this big in one frame is a teleport, not walking, so it must not wind the gait forward.
		private const float TELEPORT_DISTANCE = 3f;
		private const int RAY_BUFFER_SIZE = 8;

		// Keeps the stance sane if the width is tuned to something extreme, so the legs can't cross or splay.
		private const float MIN_STEP_WIDTH_SCALE = 0.2f;
		private const float MAX_STEP_WIDTH_SCALE = 1.5f;

		private class Leg
		{
			public Transform Thigh;
			public Transform Calf;
			public Transform Foot;
			public Quaternion ThighRest;
			public Quaternion CalfRest;
			public Quaternion FootRest;

			// Where the ankle stands in the root's space when idle, so the feet keep the model's own stance width.
			public Vector2 Stand;
			public float AnkleHeight;
			public float Length;
			public float RestExtension;
			public float PhaseOffset;
			public float OutwardSign;

			// Worked out when the foot is placed and used when it is solved, which needs the ground's tilt and how far through the swing the foot is.
			public Vector3 Tilt;
			public float Swing = -1f;
			public NPCFootState State = new NPCFootState();
		}

		private readonly Transform _root;
		private readonly Transform _pelvis;
		private readonly Vector3 _pelvisRestLocal;
		private readonly NPCAnimationProfile _profile;
		private readonly Leg _left;
		private readonly Leg _right;
		private readonly Leg[] _both;
		private readonly NPCTorsoMotion _torso;
		private readonly RaycastHit[] _hits = new RaycastHit[RAY_BUFFER_SIZE];
		private Vector3 _lastPosition;
		private float _speed;
		private float _phase;
		private float _pelvisNeed;
		private float _pelvisDrop;
		private float _stepLength;

		/// <summary>
		/// Horizontal speed measured from how far the root actually moved, in metres per second.
		/// </summary>
		public float Speed => _speed;

		/// <summary>
		/// How far through its stride the left foot is, from 0 to 1.
		/// </summary>
		public float Phase => _phase;

		/// <summary>
		/// How far the pelvis is currently lowered, in metres. Negative when it is lifted.
		/// </summary>
		public float PelvisDrop => _pelvisDrop;

		/// <summary>
		/// How straight the legs are in the model's own pose, as a share of their length. Well under 1 means the model starts out bent.
		/// </summary>
		public float RestExtension => Mathf.Min(_left.RestExtension, _right.RestExtension);

		/// <summary>
		/// How far the ankle sits above the ground, measured from the model.
		/// </summary>
		public float AnkleHeight => _left.AnkleHeight;

		/// <summary>
		/// The distance between one footfall and the next at the current speed.
		/// </summary>
		public float StepLength => _stepLength;

		/// <summary>
		/// True if the pelvis, spine or arms were bound to follow the walk.
		/// </summary>
		public bool HasTorso => _torso != null;

		public NPCFootState Left => _left.State;

		public NPCFootState Right => _right.State;

		private NPCLegGait(Transform root, Transform pelvis, NPCAnimationProfile profile, Leg left, Leg right, NPCTorsoMotion torso)
		{
			_root = root;
			_pelvis = pelvis;
			_pelvisRestLocal = pelvis != null ? pelvis.localPosition : Vector3.zero;
			_profile = profile;
			_left = left;
			_right = right;
			_torso = torso;
			_both = new[] { left, right };
			_right.PhaseOffset = 0.5f;
			Snap();
		}

		/// <summary>
		/// Binds the leg bones of a body. Fails if either leg is missing a bone.
		/// </summary>
		public static bool TryCreate(NPCBody body, Transform root, NPCAnimationProfile profile, out NPCLegGait gait)
		{
			gait = null;

			if (!TryBuildLeg(body, root, "l", out Leg left) || !TryBuildLeg(body, root, "r", out Leg right))
				return false;

			// Without a pelvis the legs still walk, they just can't lower the body to reach lower ground.
			body.TryGetBone("pelvis", out Transform pelvis);

			// The torso is optional. A model that lacks the bones still walks, just without the upper body following.
			NPCTorsoMotion torso = profile.TorsoMotionEnabled ? NPCTorsoMotion.TryCreate(body, root, profile) : null;

			gait = new NPCLegGait(root, pelvis, profile, left, right, torso);
			return true;
		}

		/// <summary>
		/// Forgets the last position, so moving the NPC into place doesn't count as walking.
		/// </summary>
		public void Snap()
		{
			_lastPosition = _root.position;
			_speed = 0f;
		}

		/// <summary>
		/// Tells the walk how much each arm is free to swing, so an arm that is waving or pointing isn't also swinging.
		/// </summary>
		public void SetArmFreedom(float left, float right) => _torso?.SetArmFreedom(left, right);

		public void Tick(float deltaTime)
		{
			if (deltaTime < MIN_DELTA_TIME)
				return;

			float travelled = Measure(deltaTime);

			// Fades the stride in over the first bit of speed so a standing NPC isn't twitching its feet.
			float moveBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_speed / Mathf.Max(EPSILON, _profile.MoveBlendSpeed)));
			float weight = Mathf.Lerp(Mathf.Clamp01(_profile.IdleLegWeight), 1f, moveBlend);

			// The bones are set from the neutral pose every frame so nothing from last frame's solve carries over.
			ResetPose();

			if (weight <= MIN_WEIGHT)
			{
				_pelvisNeed = 0f;
				_pelvisDrop = 0f;
				return;
			}

			float blend = Mathf.Clamp01(Mathf.InverseLerp(_profile.GaitSlowSpeed, _profile.GaitFastSpeed, _speed));
			_stepLength = Mathf.Min(_profile.StepLengthBase + _profile.StepLengthPerSpeed * _speed, _profile.StepLengthMax);
			float cycle = Mathf.Max(EPSILON, _stepLength * 2f);
			float stance = Mathf.Lerp(_profile.StanceFractionSlow, _profile.StanceFractionFast, blend);
			float lift = Mathf.Lerp(_profile.LiftSlow, _profile.LiftFast, blend);
			float bob = Mathf.Lerp(_profile.PelvisBobSlow, _profile.PelvisBobFast, blend);

			_phase = Mathf.Repeat(_phase + travelled / cycle, 1f);

			// Foot targets come first, because how far the pelvis has to drop depends on them, and the hips the legs solve from move with the pelvis.
			PlaceFoot(_left, stance, cycle, lift, moveBlend);
			PlaceFoot(_right, stance, cycle, lift, moveBlend);
			MovePelvis(bob * moveBlend, weight);

			// After the pelvis has settled and before the legs solve, because the hips move with the pelvis.
			_torso?.Apply(_phase, moveBlend, blend);

			SolveLeg(_left, weight, moveBlend);
			SolveLeg(_right, weight, moveBlend);
		}

		// Returns how far the root moved along its facing, negative when it went backwards.
		private float Measure(float deltaTime)
		{
			Vector3 delta = Vector3.ProjectOnPlane(_root.position - _lastPosition, _root.up);
			_lastPosition = _root.position;

			if (delta.magnitude > TELEPORT_DISTANCE)
			{
				_speed = 0f;
				return 0f;
			}

			// Smoothed because the root may only move on physics steps, which makes the raw per-frame figure jump about.
			float smoothing = 1f - Mathf.Exp(-Mathf.Max(0f, _profile.SpeedSmoothing) * deltaTime);
			_speed = Mathf.Lerp(_speed, delta.magnitude / deltaTime, smoothing);

			return Vector3.Dot(delta, _root.forward);
		}

		private void ResetPose()
		{
			_torso?.ResetPose();

			if (_pelvis != null)
				_pelvis.localPosition = _pelvisRestLocal;

			foreach (Leg leg in _both)
			{
				leg.Thigh.rotation = _root.rotation * leg.ThighRest;
				leg.Calf.rotation = _root.rotation * leg.CalfRest;
				leg.Foot.rotation = _root.rotation * leg.FootRest;
			}
		}

		// Works out where the ankle should be and how far the foot is through its swing, without touching the bones.
		private void PlaceFoot(Leg leg, float stance, float cycle, float lift, float moveBlend)
		{
			float foot = Mathf.Repeat(_phase + leg.PhaseOffset, 1f);
			float reach = cycle * stance * 0.5f;
			float forward;
			float height = 0f;
			bool swinging = foot >= stance;
			float swing = 0f;

			if (!swinging)
			{
				// Planted. The foot slides back under the body at exactly the speed the body moves forward, so it stays still on the ground.
				forward = reach * (1f - 2f * (foot / stance));
			}
			else
			{
				swing = (foot - stance) / Mathf.Max(EPSILON, 1f - stance);
				forward = SwingOffset(swing, reach, cycle * (1f - stance));
				height = lift * Mathf.Sin(swing * Mathf.PI);
			}

			float width = leg.Stand.x * Mathf.Clamp(_profile.StepWidthScale, MIN_STEP_WIDTH_SCALE, MAX_STEP_WIDTH_SCALE);
			Vector3 flat = _root.TransformPoint(new Vector3(width, 0f, leg.Stand.y + forward * moveBlend));
			bool hit = SampleGround(flat, out Vector3 ground, out Vector3 normal);

			// A foot should never tilt further than the ankle could.
			Vector3 tilted = Vector3.RotateTowards(_root.up, normal, _profile.MaxFootTilt * Mathf.Deg2Rad, 0f);

			leg.State.Ground = ground;
			leg.State.GroundHit = hit;
			leg.State.Swinging = swinging && moveBlend > MIN_WEIGHT;
			leg.State.Target = ground + tilted * leg.AnkleHeight + _root.up * (height * moveBlend);

			leg.Tilt = tilted;
			leg.Swing = swinging ? swing : -1f;
		}

		// A smooth path from where the foot left the ground to where it lands.
		// The ends are given the same velocity the planted foot has relative to the body, which stops the foot jerking as it lifts and lands.
		private static float SwingOffset(float t, float reach, float swingDistance)
		{
			float t2 = t * t;
			float t3 = t2 * t;
			float h00 = 2f * t3 - 3f * t2 + 1f;
			float h10 = t3 - 2f * t2 + t;
			float h01 = -2f * t3 + 3f * t2;
			float h11 = t3 - t2;
			float slope = -swingDistance;

			return h00 * -reach + h10 * slope + h01 * reach + h11 * slope;
		}

		private void MovePelvis(float bob, float weight)
		{
			if (_pelvis == null)
				return;

			float need = float.NegativeInfinity;
			foreach (Leg leg in _both)
			{
				// How far the hip has to come down before the leg can reach its foot without straightening past its limit.
				// It goes negative when the leg is shorter than that, which lifts the pelvis to straighten the leg instead of leaving the knees bent.
				Vector3 toTarget = leg.State.Target - leg.Thigh.position;
				float above = Vector3.Dot(-toTarget, _root.up);
				float sideways = Vector3.ProjectOnPlane(toTarget, _root.up).magnitude;
				float maxReach = leg.Length * Mathf.Clamp(_profile.MaxLegExtension, 0.5f, 1f);
				float allowed = Mathf.Sqrt(Mathf.Max(0f, maxReach * maxReach - sideways * sideways));
				need = Mathf.Max(need, above - allowed);
			}

			float deltaTime = Mathf.Max(MIN_DELTA_TIME, Time.deltaTime);
			_pelvisNeed = Mathf.Lerp(_pelvisNeed, need, 1f - Mathf.Exp(-Mathf.Max(0f, _profile.PelvisDropLerp) * deltaTime));

			// Lowest when both feet are down, which is twice per full stride.
			float bobbing = bob * (0.5f + 0.5f * Mathf.Cos(_phase * Mathf.PI * 4f));

			_pelvisDrop = Mathf.Clamp(_pelvisNeed + (_profile.PelvisStandDrop + bobbing) * weight, -_profile.MaxPelvisRaise, _profile.MaxPelvisDrop);
			_pelvis.position -= _root.up * _pelvisDrop;
		}

		private void SolveLeg(Leg leg, float weight, float moveBlend)
		{
			Vector3 hip = leg.Thigh.position;

			// The knee pole sits well out in front so the knee always bends forwards, with a little to the side to stop the knees knocking.
			Vector3 pole = hip
				+ _root.forward * Mathf.Max(MIN_POLE_DISTANCE, _profile.KneePoleForward)
				+ _root.right * (leg.OutwardSign * _profile.KneePoleOutward);

			leg.State.SolveSucceeded = TwoBoneIkSolver.Solve(leg.Thigh, leg.Calf, leg.Foot, leg.State.Target, pole, weight);

			// The foot lies flat on the ground and tips toes-down as it leaves and toes-up as it lands.
			float pitch = leg.Swing >= 0f
				? _profile.FootSwingPitch * Mathf.Cos(leg.Swing * Mathf.PI) * moveBlend
				: 0f;

			Quaternion flat = Quaternion.FromToRotation(_root.up, leg.Tilt) * Quaternion.AngleAxis(pitch, _root.right) * _root.rotation * leg.FootRest;
			leg.Foot.rotation = Quaternion.Slerp(leg.Foot.rotation, flat, weight);

			leg.State.Position = leg.Foot.position;
		}

		// The nearest solid thing under the point. Anything with its own rigidbody is skipped so the feet don't climb onto loose items.
		private bool SampleGround(Vector3 flat, out Vector3 point, out Vector3 normal)
		{
			Vector3 origin = flat + _root.up * _profile.FootRayStart;
			int count = Physics.RaycastNonAlloc(origin, -_root.up, _hits, _profile.FootRayLength, _profile.GroundMask, QueryTriggerInteraction.Ignore);

			float nearest = float.PositiveInfinity;
			int best = -1;
			for (int i = 0; i < count; i++)
			{
				RaycastHit hit = _hits[i];
				if (hit.collider.transform.IsChildOf(_root) || hit.collider.attachedRigidbody != null)
					continue;

				if (hit.distance < nearest)
				{
					nearest = hit.distance;
					best = i;
				}
			}

			if (best < 0)
			{
				point = flat;
				normal = _root.up;
				return false;
			}

			point = _hits[best].point;
			normal = _hits[best].normal;
			return true;
		}

		private static bool TryBuildLeg(NPCBody body, Transform root, string side, out Leg leg)
		{
			leg = null;

			if (!body.TryGetBone("thigh_" + side, out Transform thigh)
				|| !body.TryGetBone("calf_" + side, out Transform calf)
				|| !body.TryGetBone("foot_" + side, out Transform foot)
				|| !calf.IsChildOf(thigh)
				|| !foot.IsChildOf(calf))
			{
				return false;
			}

			if (!body.TryGetNeutralRotation(thigh, out Quaternion thighRest)
				|| !body.TryGetNeutralRotation(calf, out Quaternion calfRest)
				|| !body.TryGetNeutralRotation(foot, out Quaternion footRest))
			{
				return false;
			}

			Vector3 local = root.InverseTransformPoint(foot.position);

			leg = new Leg
			{
				Thigh = thigh,
				Calf = calf,
				Foot = foot,
				ThighRest = thighRest,
				CalfRest = calfRest,
				FootRest = footRest,
				Stand = new Vector2(local.x, local.z),

				// The root stands on the ground, so the ankle's height above it is how far the ankle sits above the sole.
				AnkleHeight = Mathf.Max(0f, local.y),
				Length = Vector3.Distance(thigh.position, calf.position) + Vector3.Distance(calf.position, foot.position),
				RestExtension = Vector3.Distance(thigh.position, foot.position) / Mathf.Max(EPSILON, Vector3.Distance(thigh.position, calf.position) + Vector3.Distance(calf.position, foot.position)),
				OutwardSign = local.x >= 0f ? 1f : -1f,
			};

			return leg.Length > EPSILON;
		}
	}
}
