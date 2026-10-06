using NPCs.AI.States;
using NPCs.AI.States.NPCs.AI.States;
using NPCs.Common;
using NPCs.Dialogue;
using UnityEngine;

namespace NPCs.AI
{
	public class NPCAi : MonoBehaviour
	{
		private const float LOOK_HEIGHT = 1.6f;
		private const float LOOK_DISTANCE = 5f;
		private const float MIN_GESTURE_DISTANCE = 2f;

		private NPCState _state;
		private ConversationRunner _runner;
		private NPCBody _body;
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

		public bool InConversation => _runner != null && _runner.IsActive;

		public float DistanceToPlayer => Vector3.Distance(transform.position, mainscript.M.player.transform.position);

		private void Start()
		{
			_runner = GetComponent<ConversationRunner>();
			_body = GetComponent<NPC>().Body;

			GetComponent<NPC>().OnDeath += () => enabled = false;

			Rest();

			// The head's look point starts at the world origin, so snap it to avoid an initial head swing.
			_body.Look.Snap();
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

		// Called when the NPC notices the player.
		public virtual void Engage() => SetState(new EngagedState(this));

		// The head lerps towards this point on its own, so we only need to move it.
		public void LookAt(Vector3 point)
		{
			_body.Look.Target = point;
		}

		public void LookAhead()
		{
			LookAt(transform.position + Vector3.up * LOOK_HEIGHT + transform.forward * LOOK_DISTANCE);
		}

		// The head leads and the body only follows once the head has turned as far as it comfortably can.
		public void FaceTowards(Vector3 point)
		{
			// Use where the head is actually looking rather than where it's heading, so the body reacts to the delayed head movement.
			float headYaw = Mathf.Abs(YawTo(_body.Look.Current));

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

		public void Nod() => SetState(new GestureState(this, GestureType.Nod));
		public void ShakeHead() => SetState(new GestureState(this, GestureType.Shake));

		// Looks at a point rotated away from the straight-line view of it.
		// Positive pitch looks down, positive yaw looks right.
		public void LookAtOffset(Vector3 point, float pitch, float yaw)
		{
			Vector3 head = transform.position + Vector3.up * LOOK_HEIGHT;
			Vector3 toPoint = point - head;
			float distance = Mathf.Max(toPoint.magnitude, MIN_GESTURE_DISTANCE);
			Vector3 dir = toPoint.sqrMagnitude > 0.0001f ? toPoint.normalized : transform.forward;

			Vector3 right = Vector3.Cross(Vector3.up, dir);
			right = right.sqrMagnitude > 0.0001f ? right.normalized : transform.right;

			dir = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, right) * dir;
			LookAt(head + dir * distance);
		}

		private float YawTo(Vector3 point)
		{
			Vector3 flat = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
			return Vector3.SignedAngle(transform.forward, flat, Vector3.up);
		}
	}
}