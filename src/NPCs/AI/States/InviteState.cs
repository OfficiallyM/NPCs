using NPCs.Animation.Enums;
using UnityEngine;

namespace NPCs.AI.States
{
	/// <summary>
	/// A travelling NPC that has noticed the player stops, turns to face them and waves once to invite them over.
	/// After that it waits like any engaged NPC, and it never forces a conversation. It carries on when the player leaves, or when nobody comes over for long enough.
	/// </summary>
	public class InviteState : EngagedState
	{
		// Counts from the last time something happened, so a conversation restarts the wait.
		private float _waitingSince;
		private bool _waved;

		public InviteState(NPCAi ai) : base(ai) { }

		public override void Enter()
		{
			_waitingSince = Time.time;

			// A player who has already started talking doesn't need inviting.
			_waved = Ai.InConversation;
		}

		// Until the wave has played the body has to actually face the player. With the head leading and the body only following past the head's limit, the NPC can end up looking at the player but waving in the direction it was walking.
		protected override void Face(Vector3 point)
		{
			if (_waved)
				base.Face(point);
			else
				Ai.TurnBodyTowards(point);
		}

		protected override void OnEngaged()
		{
			// Talking means the invitation was taken up, and the wait starts over once it's finished.
			if (Ai.InConversation)
			{
				_waved = true;
				_waitingSince = Time.time;
				return;
			}

			if (Time.time - _waitingSince >= Ai.InviteGiveUpTime)
			{
				// Spent until the player has been out of range, so the NPC doesn't stop and wave again the moment it walks on.
				Ai.InviteSpent = true;
				Ai.Rest();
				return;
			}

			TryWave();
		}

		private void TryWave()
		{
			// Still slowing down after the travel state stopped the walk, or still turning round.
			if (_waved || Ai.Locomotion.IsMoving || Ai.AngleToPlayer > Ai.BodySettleAngle)
				return;

			// The gesture can fail to start, for instance while the arm isn't ready, in which case it's tried again next tick.
			_waved = Ai.Animation.PlayGesture(NPCGesture.Wave, NPCHand.Right);
		}

		public override string Describe()
		{
			float remaining = Mathf.Max(0f, Ai.InviteGiveUpTime - (Time.time - _waitingSince));
			return "Invite, " + (_waved ? "waved" : "turning to face the player") + ", gives up in " + remaining.ToString("F0") + " s";
		}
	}
}
