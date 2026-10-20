using NPCs.AI.Enums;
using NPCs.AI.Routes;
using NPCs.Core;
using UnityEngine;

namespace NPCs.AI.States
{
	/// <summary>
	/// Walks along a route, then goes back to resting once it ends or the NPC gives up. The body faces the way it's travelling.
	/// Types that stop for the player slow down as the player gets near and stop to invite them over once they're close, but only while the player is slow enough for it to matter.
	/// </summary>
	public class TravelState : NPCState
	{
		// How long the head stays on the player once it has glanced at them, before it looks away again.
		private const float GLANCE_DURATION = 2f;

		private readonly NPCRoute _route;
		private NPCSpeed _cruiseSpeed;

		// Whether the player is slow enough to be worth reacting to. Has its own exit threshold so a player hovering around the limit doesn't flicker it.
		private bool _interested;
		private bool _slowed;

		// A glance is allowed once per approach, so the trader doesn't stare at the player as they close in.
		private bool _glanceArmed;

		// When the current glance ends, or zero when the head isn't glancing.
		private float _glanceEnd;

		public TravelState(NPCAi ai, NPCRoute route, NPCSpeed speed) : base(ai)
		{
			_route = route;
			_cruiseSpeed = speed;
		}

		/// <summary>
		/// The follower doing the walking, for the debugger. Null until the state has been entered.
		/// </summary>
		public NPCRouteFollower Follower { get; private set; }

		public override void Enter()
		{
			Ai.Animation.LookAhead();
			Follower = new NPCRouteFollower(Ai.Locomotion, _route, _cruiseSpeed);
			Follower.Start();
		}

		public void SetCruiseSpeed(NPCSpeed speed)
		{
			_cruiseSpeed = speed;

			if (!_slowed)
				Follower?.SetSpeed(speed);
		}

		public override void Tick()
		{
			// A conversation the player started takes priority over walking on, whatever their speed.
			if (Ai.InConversation)
			{
				Ai.Engage();
				return;
			}

			float distance = Ai.DistanceToPlayer;
			_interested = PlayerMotion.Speed <= (_interested ? Ai.InterestLoseSpeed : Ai.InterestSpeed);

			if (Ai.StopsForPlayer && !Ai.InviteSpent && _interested && distance <= Ai.NoticeRange)
			{
				Ai.Engage();
				return;
			}

			bool slowed = _interested && distance <= (_slowed ? Ai.SlowLoseRange : Ai.SlowRange);
			if (slowed != _slowed)
			{
				_slowed = slowed;
				_glanceArmed = slowed;
				_glanceEnd = 0f;
				Follower.SetSpeed(_slowed ? Ai.SlowSpeed : _cruiseSpeed);
			}

			UpdateLook();

			Follower.Tick(Ai.transform.position);

			if (Follower.Status == NPCRouteStatus.Finished || Follower.Status == NPCRouteStatus.Stuck)
				Ai.Rest();
		}

		// The look point is a fixed spot in the world, so it has to be refreshed every frame or the head ends up staring at ground the NPC has already walked past.
		private void UpdateLook()
		{
			// The head only reaches the player while they're within its comfortable turn, so a player coming up from behind doesn't make it snap round.
			bool reachable = Ai.AngleToPlayer <= Ai.BodyTurnAngle;

			// The glance starts the first time the player is somewhere the head can reach.
			if (_glanceArmed && reachable)
			{
				_glanceArmed = false;
				_glanceEnd = Time.time + GLANCE_DURATION;
			}

			// It ends when the time is up, or sooner if the player moves somewhere the head can't follow.
			if (_glanceEnd > 0f && (Time.time >= _glanceEnd || !reachable))
				_glanceEnd = 0f;

			if (_glanceEnd > 0f)
				Ai.Animation.LookAt(mainscript.M.player.Th.position);
			else
				Ai.Animation.LookAhead();
		}

		public override void Exit()
		{
			Follower.Dispose();

			// Another state may be taking over mid-walk, in which case the NPC should slow down rather than carry on towards a target that's no longer being moved.
			if (Ai.Locomotion.HasDestination)
				Ai.Locomotion.Stop();
		}

		public override string Describe()
		{
			string status = Follower != null ? Follower.Status.ToString() : "not started";
			return "Travel, " + (_slowed ? "slowed" : "cruising") + (_glanceEnd > 0f ? ", glancing" : "") + ", " + (_interested ? "player slow enough to notice" : "player too fast to notice") + ", " + status;
		}
	}
}
