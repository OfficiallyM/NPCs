using NPCs.AI.States;
using NPCs.Animation;
using NPCs.Dialogue;
using NPCs.Core;
using UnityEngine;
using NPCs.AI.Enums;
using NPCs.AI.Routes;

namespace NPCs.AI
{
	public class NPCAi : MonoBehaviour, IWorldShiftable
	{
		private NPCState _state;
		private ConversationRunner _runner;
		private bool _turningBody;

		/// <summary>
		/// How close the player needs to get before the NPC reacts.
		/// </summary>
		public virtual float NoticeRange => 12f;

		/// <summary>
		/// Larger than the notice range so the NPC doesn't flicker between states at the boundary.
		/// </summary>
		public virtual float LoseRange => 16f;

		/// <summary>
		/// Degrees per second.
		/// </summary>
		public virtual float TurnSpeed => 90f;

		/// <summary>
		/// How far the head can turn before the body starts following.
		/// Kept under the head's own turn limit (80 degrees, see NPCHeadLook) so the head never reaches it.
		/// </summary>
		public virtual float BodyTurnAngle => 70f;

		/// <summary>
		/// The body stops turning once the target is within this angle of straight ahead.
		/// </summary>
		public virtual float BodySettleAngle => 10f;

		/// <summary>
		/// How far to the right of the road's centre line this NPC walks, looking the way it's travelling. Negative walks on the left.
		/// </summary>
		public virtual float RoadLaneOffset => 0f;

		/// <summary>
		/// Which way along the road this NPC walks.
		/// </summary>
		public virtual RouteDirection RoadDirection => RouteDirection.Forwards;

		/// <summary>
		/// The speed a travelling NPC walks at when nothing is going on, and the one it slows to as the player gets near.
		/// </summary>
		public virtual NPCSpeed CruiseSpeed => NPCSpeed.Walk;

		public virtual NPCSpeed SlowSpeed => NPCSpeed.Walk;

		/// <summary>
		/// Whether a travelling NPC stops for the player once they're within the notice range.
		/// </summary>
		public virtual bool StopsForPlayer => false;

		// How close the player gets before a travelling NPC slows down.
		public virtual float SlowRange => 50f;

		/// <summary>
		/// Larger than the slow range so the NPC doesn't flicker between speeds at the boundary.
		/// </summary>
		public virtual float SlowLoseRange => 60f;

		/// <summary>
		/// How fast the player can be going, in metres per second, before a travelling NPC takes any notice of them.
		/// Faster than this and there's no point slowing, stopping or waving, because the player is only passing.
		/// </summary>
		public virtual float InterestSpeed => 6f;

		// Larger than the interest speed so a player hovering around it doesn't make the NPC flicker between states.
		public virtual float InterestLoseSpeed => 9f;

		/// <summary>
		/// How long, in seconds, an NPC that has stopped to invite the player over waits before it gives up and walks on.
		/// The wait starts over if a conversation happens.
		/// </summary>
		public virtual float InviteGiveUpTime => 30f;

		/// <summary>
		/// Where the NPC's head and limbs are moved. The AI decides what to do and the animation decides how it looks.
		/// </summary>
		public NPCAnimation Animation { get; private set; }

		/// <summary>
		/// Moves the NPC around. States ask it to go somewhere and never steer or set velocities themselves.
		/// </summary>
		public NPCLocomotion Locomotion { get; private set; }

		/// <summary>
		/// The follower walking the NPC along its route, or null if it isn't travelling one.
		/// </summary>
		public NPCRouteFollower Follower => (_state as TravelState)?.Follower;

		/// <summary>
		/// True once the NPC has given up waiting for the player after inviting them. It stays true until the player has been out of range, so the NPC doesn't stop and wave at them again straight away.
		/// </summary>
		public bool InviteSpent { get; set; }

		public bool InConversation => _runner != null && _runner.IsActive;

		public float DistanceToPlayer => Vector3.Distance(transform.position, mainscript.M.player.transform.position);

		/// <summary>
		/// How far round from straight ahead of the body the player is, in degrees.
		/// </summary>
		public float AngleToPlayer => Mathf.Abs(YawTo(mainscript.M.player.transform.position));

		/// <summary>
		/// What the current state is doing, for the debugger.
		/// </summary>
		public string DebugSummary => _state != null ? _state.Describe() : "No state";

		private void Start()
		{
			_runner = GetComponent<ConversationRunner>();
			Animation = GetComponent<NPC>().Animation;
			Locomotion = GetComponent<NPC>().Locomotion;

			GetComponent<NPC>().OnDeath += () => enabled = false;

			Rest();
		}

		private void Update()
		{
			// The game freezes everything beyond a certain range of the player, so a frozen body means nobody is watching and there's nothing worth doing.
			// This is the cheapest check there is, so it comes first.
			if (Locomotion.IsFrozen)
				return;

			// A given-up invitation is forgotten once the player has been out of range.
			if (InviteSpent && DistanceToPlayer > LoseRange)
				InviteSpent = false;

			_state?.Tick();
		}

		public void SetState(NPCState next)
		{
			_state?.Exit();
			_turningBody = false;
			_state = next;
			_state?.Enter();
		}

		/// <summary>
		/// Go back to whatever this NPC type does when nothing is happening.
		/// </summary>
		public virtual void Rest() => SetState(new IdleState(this));

		/// <summary>
		/// Walks to a point, then goes back to resting once it gets there or gives up.
		/// </summary>
		public void WalkTo(Vector3 point, NPCSpeed speed) => SetState(new WalkState(this, point, speed));

		/// <summary>
		/// Walks along a route, then goes back to resting once it ends or the NPC gives up.
		/// </summary>
		public void Travel(NPCRoute route, NPCSpeed speed) => SetState(new TravelState(this, route, speed));

		/// <summary>
		/// Walks along the road the NPC is beside, in the direction and lane this type uses. Returns false if there's no placed road near enough.
		/// </summary>
		public bool TravelRoad(NPCSpeed speed)
		{
			if (!RoadRoute.TryCreateNear(transform.position, RoadDirection, RoadLaneOffset, out RoadRoute route))
				return false;

			Travel(route, speed);
			return true;
		}

		/// <summary>
		/// Changes the cruising speed of a travel already under way, which a plain speed change on the locomotion would lose when the NPC next slows or speeds up.
		/// </summary>
		public void SetTravelSpeed(NPCSpeed speed) => (_state as TravelState)?.SetCruiseSpeed(speed);

		/// <summary>
		/// Called when the NPC notices the player.
		/// </summary>
		public virtual void Engage() => SetState(new EngagedState(this));

		public void OnWorldShifted(Vector3 delta) => _state?.OnWorldShifted(delta);

		/// <summary>
		/// The head leads and the body only follows once the head has turned as far as it comfortably can.
		/// </summary>
		public void FaceTowards(Vector3 point)
		{
			// Use where the head is actually looking rather than where it's heading, so the body reacts to the delayed head movement.
			float headYaw = Mathf.Abs(YawTo(Animation.LookPoint));

			if (!_turningBody && headYaw > BodyTurnAngle)
				_turningBody = true;
			else if (_turningBody && Mathf.Abs(YawTo(point)) <= BodySettleAngle)
				_turningBody = false;

			if (!_turningBody)
				return;

			Vector3 flat = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
			if (flat.sqrMagnitude < 0.0001f)
				return;

			Quaternion target = Quaternion.LookRotation(flat, Vector3.up);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, target, TurnSpeed * Time.deltaTime);
		}

		/// <summary>
		/// Turns the whole body towards a point straight away, instead of waiting for the head to reach its limit first.
		/// For when the body has to be facing the right way, such as before a wave, which would otherwise be done in the direction the NPC was walking.
		/// </summary>
		public void TurnBodyTowards(Vector3 point)
		{
			Vector3 flat = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
			if (flat.sqrMagnitude < 0.0001f)
				return;

			Quaternion target = Quaternion.LookRotation(flat, Vector3.up);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, target, TurnSpeed * Time.deltaTime);
		}

		private float YawTo(Vector3 point)
		{
			Vector3 flat = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
			return Vector3.SignedAngle(transform.forward, flat, Vector3.up);
		}
	}
}