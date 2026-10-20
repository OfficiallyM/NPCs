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

		// How close the player needs to get before the NPC reacts.
		public virtual float NoticeRange => 12f;

		// Larger than the notice range so the NPC doesn't flicker between states at the boundary.
		public virtual float LoseRange => 16f;

		// Degrees per second.
		public virtual float TurnSpeed => 90f;

		// How far the head can turn before the body starts following.
		// Kept under the head's own turn limit (80 degrees, see NPCHeadLook) so the head never reaches it.
		public virtual float BodyTurnAngle => 70f;

		// The body stops turning once the target is within this angle of straight ahead.
		public virtual float BodySettleAngle => 10f;

		// How far to the right of the road's centre line this NPC walks, looking the way it's travelling. Negative walks on the left.
		public virtual float RoadLaneOffset => 0f;

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

		public bool InConversation => _runner != null && _runner.IsActive;

		public float DistanceToPlayer => Vector3.Distance(transform.position, mainscript.M.player.transform.position);

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
			_state?.Tick();
		}

		public void SetState(NPCState next)
		{
			_state?.Exit();
			_turningBody = false;
			_state = next;
			_state?.Enter();
		}

		// Go back to whatever this NPC type does when nothing is happening.
		public virtual void Rest() => SetState(new IdleState(this));

		// Walks to a point, then goes back to resting once it gets there or gives up.
		public void WalkTo(Vector3 point, NPCSpeed speed) => SetState(new WalkState(this, point, speed));

		// Walks along a route, then goes back to resting once it ends or the NPC gives up.
		public void Travel(NPCRoute route, NPCSpeed speed) => SetState(new TravelState(this, route, speed));

		// Called when the NPC notices the player.
		public virtual void Engage() => SetState(new EngagedState(this));

		public void OnWorldShifted(Vector3 delta) => _state?.OnWorldShifted(delta);

		// The head leads and the body only follows once the head has turned as far as it comfortably can.
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

		private float YawTo(Vector3 point)
		{
			Vector3 flat = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
			return Vector3.SignedAngle(transform.forward, flat, Vector3.up);
		}
	}
}