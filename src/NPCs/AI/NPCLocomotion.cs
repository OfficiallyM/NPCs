using NPCs.AI.Enums;
using NPCs.Core;
using System;
using UnityEngine;

namespace NPCs.AI
{
	[DisallowMultipleComponent]
	public sealed class NPCLocomotion : MonoBehaviour
	{
		// Cruising speeds in metres per second. The leg gait is tuned for roughly 1.2 to 3.5.
		private const float WALK_SPEED = 1.4f;
		private const float WALK_FAST_SPEED = 2f;
		private const float JOG_SPEED = 3.5f;
		private const float RUN_SPEED = 5f;

		private const float ACCELERATION = 2.5f;
		private const float DECELERATION = 3f;

		// Degrees per second.
		private const float TURN_RATE = 200f;

		// Past this angle between the body and where it wants to go the NPC stops and pivots instead of walking in an arc.
		private const float PIVOT_ANGLE = 75f;

		// Horizontal distance from the destination at which the NPC counts as having arrived.
		private const float ARRIVAL_RADIUS = 0.2f;

		// Below this the NPC is treated as standing still.
		private const float STOPPED_SPEED = 0.05f;

		// Obstacle sensing runs on its own interval rather than every physics step, and each NPC starts at a random offset so they don't all cast on the same frame.
		private const float SENSE_INTERVAL = 0.125f;

		// The probe capsule sits above the knees so kerbs and small bumps don't count as obstacles, and it's narrower than the hitbox so it never starts inside walls the hitbox is touching.
		private const float PROBE_RADIUS = 0.25f;
		private const float PROBE_BOTTOM = 0.45f;
		private const float PROBE_TOP = 1.3f;

		// How far ahead to look, as a minimum and as seconds of travel at the current speed.
		private const float MIN_LOOKAHEAD = 1.5f;
		private const float LOOKAHEAD_SECONDS = 1f;

		// Surfaces flatter than this are ground rather than obstacles. This stands in for a layer mask.
		private const float WALKABLE_NORMAL_Y = 0.7f;

		// A ground check this far ahead decides whether a heading leads off a ledge.
		private const float GROUND_CHECK_UP = 1f;
		private const float GROUND_CHECK_DOWN = 2.5f;
		private const float MAX_DROP = 1f;

		// Angles tried, in order, when the way straight on is blocked.
		private static readonly float[] _detourAngles = { 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f };

		// Stuck detection compares how far the NPC got against how far it should have.
		private const float STUCK_WINDOW = 1.5f;
		private const float STUCK_PROGRESS_RATIO = 0.2f;
		private const float STUCK_IGNORE_DISTANCE = 1.5f;
		private const float RECOVERY_DURATION = 1f;
		private const int MAX_RECOVERY_ATTEMPTS = 3;

		private const int MAX_PROBE_HITS = 16;

		private NPC _npc;
		private Rigidbody _body;
		private RigidbodyInterpolation _restingInterpolation;
		private bool _interpolating;

		private bool _hasDestination;
		private Vector3 _destination;
		private float _targetSpeed;
		private float _speed;

		// The way the NPC is trying to face. Kept between senses so the body turns smoothly towards it.
		private Vector3 _heading = Vector3.forward;
		private float _nextSenseTime;
		private float _detourSide;

		private Vector3 _windowStart;
		private float _windowStartTime;
		private float _recoveryEndTime;
		private float _recoverySide = 1f;
		private int _recoveryAttempts;

		private readonly RaycastHit[] _hits = new RaycastHit[MAX_PROBE_HITS];

		/// <summary>
		/// Raised once when the NPC reaches the destination it was sent to.
		/// </summary>
		public event Action Arrived;

		/// <summary>
		/// Raised once when the NPC has failed to make progress several times in a row and given up on the destination.
		/// </summary>
		public event Action Stuck;

		public bool HasDestination => _hasDestination;

		public Vector3 Destination => _destination;

		public float CurrentSpeed => _speed;

		// Still counts as moving while slowing down after Stop.
		public bool IsMoving => _hasDestination || _speed > STOPPED_SPEED;

		private void Awake()
		{
			_npc = GetComponent<NPC>();
			_body = GetComponent<Rigidbody>();
			_nextSenseTime = Time.time + UnityEngine.Random.value * SENSE_INTERVAL;
		}

		private void Start()
		{
			_npc.OnDeath += OnDeath;
		}

		private void OnDisable()
		{
			SetInterpolating(false);
		}

		/// <summary>
		/// Sends the NPC towards a point, replacing any destination it already had.
		/// </summary>
		/// <param name="point">Where to go. Only the horizontal position matters</param>
		/// <param name="speed">How fast to get there</param>
		public void MoveTo(Vector3 point, NPCSpeed speed)
		{
			_destination = point;
			_targetSpeed = ResolveSpeed(speed);
			_hasDestination = true;

			_recoveryAttempts = 0;
			_recoveryEndTime = 0f;
			_detourSide = 0f;
			_heading = FlatForward();
			ResetStuckWindow();

			// Work out the way to go on the next step instead of waiting out the sense interval.
			_nextSenseTime = 0f;

			SetInterpolating(true);
		}

		/// <summary>
		/// Changes how fast the NPC travels without touching its destination, easing to the new speed.
		/// Does nothing when there's no destination, so an NPC that's stopping can't be set walking again by accident.
		/// </summary>
		/// <param name="speed">The new cruising speed</param>
		public void SetSpeed(NPCSpeed speed)
		{
			if (!_hasDestination)
				return;

			_targetSpeed = ResolveSpeed(speed);

			// The stuck check measures progress against the target speed, so a window that started at the old one would be judged unfairly.
			ResetStuckWindow();
		}

		/// <summary>
		/// Drops the destination and slows to a halt, carrying on in the current direction while the speed falls.
		/// </summary>
		public void Stop()
		{
			_hasDestination = false;
			_targetSpeed = 0f;
		}

		private static float ResolveSpeed(NPCSpeed speed)
		{
			switch (speed)
			{
				case NPCSpeed.Jog:
					return JOG_SPEED;
				case NPCSpeed.Run:
					return RUN_SPEED;
				case NPCSpeed.WalkFast:
					return WALK_FAST_SPEED;
				default:
					return WALK_SPEED;
			}
		}

		private void FixedUpdate()
		{
			if (_body == null)
				return;

			if (_npc.IsDead || _npc.Body == null || _npc.Body.IsRagdolled)
			{
				_hasDestination = false;
				_speed = 0f;
				enabled = false;
				return;
			}

			if (!IsMoving)
			{
				SetInterpolating(false);
				return;
			}

			float deltaTime = Time.fixedDeltaTime;
			Vector3 toDestination = Vector3.zero;
			float remaining = 0f;

			if (_hasDestination)
			{
				toDestination = Vector3.ProjectOnPlane(_destination - _body.position, Vector3.up);
				remaining = toDestination.magnitude;

				if (remaining <= ARRIVAL_RADIUS)
				{
					Stop();
					Arrived?.Invoke();
				}
			}

			// How far the body is from facing the way it wants to go.
			float angle = Mathf.Abs(Vector3.SignedAngle(FlatForward(), _heading, Vector3.up));

			if (_hasDestination)
			{
				if (Time.time >= _nextSenseTime && remaining > 0.01f)
				{
					_nextSenseTime = Time.time + SENSE_INTERVAL;
					_heading = ChooseHeading(toDestination / remaining);
				}

				// Pivoting on the spot or braking for the destination covers little ground without anything being wrong, so those don't count towards being stuck.
				if (angle > PIVOT_ANGLE * 0.5f || remaining < STUCK_IGNORE_DISTANCE)
					ResetStuckWindow();
				else
					CheckForStuck();
			}

			// Slow down early enough to stop at the destination, and while still pivoting towards the way to go.
			// Braking aims a little short of the arrival radius so the NPC finishes its stop on the destination rather than past it.
			float desiredSpeed = 0f;
			if (_hasDestination)
			{
				float alignment = Mathf.Clamp01(1f - angle / PIVOT_ANGLE);
				float brakingSpeed = Mathf.Sqrt(2f * DECELERATION * Mathf.Max(0f, remaining - ARRIVAL_RADIUS * 0.5f));
				desiredSpeed = Mathf.Min(_targetSpeed, brakingSpeed) * alignment;
			}

			float rate = desiredSpeed > _speed ? ACCELERATION : DECELERATION;
			_speed = Mathf.MoveTowards(_speed, desiredSpeed, rate * deltaTime);

			// The body only turns while a destination is held, so a stopping NPC keeps the way it was facing.
			if (_hasDestination && _heading.sqrMagnitude > 0.0001f)
			{
				Quaternion target = Quaternion.LookRotation(_heading, Vector3.up);
				_body.MoveRotation(Quaternion.RotateTowards(_body.rotation, target, TURN_RATE * deltaTime));
			}

			// Only the horizontal speed is set, so gravity and slopes still act on the NPC.
			Vector3 forward = FlatForward();
			Vector3 velocity = _body.velocity;
			_body.velocity = new Vector3(forward.x * _speed, velocity.y, forward.z * _speed);
		}

		// Works out which way to face this tick. The way straight on is tried first because it's the common case and costs the least.
		private Vector3 ChooseHeading(Vector3 wanted)
		{
			// During a recovery the NPC deliberately goes sideways to get out of whatever it's wedged against.
			if (Time.time < _recoveryEndTime)
				return Quaternion.AngleAxis(90f * _recoverySide, Vector3.up) * wanted;

			float lookahead = Mathf.Max(MIN_LOOKAHEAD, _speed * LOOKAHEAD_SECONDS);

			if (IsClear(wanted, lookahead))
			{
				_detourSide = 0f;
				return wanted;
			}

			// Keep to the side already chosen first so the NPC doesn't dither between left and right in front of a flat wall.
			for (int i = 0; i < _detourAngles.Length; i++)
			{
				float angle = _detourAngles[i] * (_detourSide < 0f ? -1f : 1f);
				Vector3 candidate = Quaternion.AngleAxis(angle, Vector3.up) * wanted;

				if (!IsClear(candidate, lookahead))
					continue;

				_detourSide = Mathf.Sign(angle);
				return candidate;
			}

			// Boxed in on every side, so carry on towards the target and let the stuck check deal with it.
			return wanted;
		}

		// Whether the NPC could walk this far in this direction without hitting something solid or stepping off a ledge.
		private bool IsClear(Vector3 direction, float distance)
		{
			Vector3 origin = _body.position;
			Vector3 bottom = origin + Vector3.up * PROBE_BOTTOM;
			Vector3 top = origin + Vector3.up * PROBE_TOP;

			int count = Physics.CapsuleCastNonAlloc(
				bottom,
				top,
				PROBE_RADIUS,
				direction,
				_hits,
				distance,
				Physics.AllLayers,
				QueryTriggerInteraction.Ignore
			);

			for (int i = 0; i < count; i++)
			{
				RaycastHit hit = _hits[i];

				// The NPC's own hitbox overlaps the probe from the start.
				if (hit.collider.transform.IsChildOf(transform))
					continue;

				// Gentle slopes are ground, not walls.
				if (hit.normal.y >= WALKABLE_NORMAL_Y)
					continue;

				return false;
			}

			return HasGroundAhead(origin + direction * distance);
		}

		private bool HasGroundAhead(Vector3 point)
		{
			Vector3 start = point + Vector3.up * GROUND_CHECK_UP;
			int count = Physics.RaycastNonAlloc(start, Vector3.down, _hits, GROUND_CHECK_UP + GROUND_CHECK_DOWN, Physics.AllLayers, QueryTriggerInteraction.Ignore);

			float nearest = float.PositiveInfinity;
			for (int i = 0; i < count; i++)
			{
				if (_hits[i].collider.transform.IsChildOf(transform))
					continue;

				if (_hits[i].distance < nearest)
					nearest = _hits[i].distance;
			}

			if (float.IsPositiveInfinity(nearest))
				return false;

			// The ray starts above the NPC's feet, so subtract that to get how far the ground is below them.
			float drop = nearest - GROUND_CHECK_UP - (point.y - _body.position.y);
			return drop <= MAX_DROP;
		}

		private void CheckForStuck()
		{
			if (Time.time < _windowStartTime + STUCK_WINDOW)
				return;

			float moved = Vector3.ProjectOnPlane(_body.position - _windowStart, Vector3.up).magnitude;
			float expected = _targetSpeed * STUCK_WINDOW;

			bool progressing = moved >= expected * STUCK_PROGRESS_RATIO;

			ResetStuckWindow();

			if (progressing)
			{
				_recoveryAttempts = 0;
				return;
			}

			_recoveryAttempts++;

			if (_recoveryAttempts > MAX_RECOVERY_ATTEMPTS)
			{
				Stop();
				Stuck?.Invoke();
				return;
			}

			_recoverySide = _recoveryAttempts % 2 == 0 ? -1f : 1f;
			_recoveryEndTime = Time.time + RECOVERY_DURATION;
			_nextSenseTime = 0f;
		}

		private void ResetStuckWindow()
		{
			_windowStart = _body != null ? _body.position : transform.position;
			_windowStartTime = Time.time;
		}

		private Vector3 FlatForward()
		{
			Vector3 flat = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
			return flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
		}

		// The root only moves on physics steps. Interpolating it gives the gait a smooth position to measure each frame.
		private void SetInterpolating(bool interpolate)
		{
			if (_body == null || interpolate == _interpolating)
				return;

			if (interpolate)
			{
				_restingInterpolation = _body.interpolation;
				_body.interpolation = RigidbodyInterpolation.Interpolate;
			}
			else
			{
				_body.interpolation = _restingInterpolation;
			}

			_interpolating = interpolate;
		}

		private void OnDeath()
		{
			_hasDestination = false;
			_speed = 0f;
			enabled = false;
		}
	}
}
