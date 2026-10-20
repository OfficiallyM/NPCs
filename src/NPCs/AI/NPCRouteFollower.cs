using NPCs.AI.Enums;
using NPCs.AI.Routes;
using UnityEngine;

namespace NPCs.AI
{
	/// <summary>
	/// Walks an NPC along a route. It keeps a target a short way ahead on the path and moves that along as the NPC walks, so the NPC carries on through the points instead of braking to a stop at each one.
	/// </summary>
	public sealed class NPCRouteFollower
	{
		// How far ahead on the route the NPC aims. Far enough that the locomotion never starts braking for it, and close enough to hug the bends in the road.
		private const float LOOKAHEAD = 5f;

		// Limits on how many points are looked at in one tick, so a route with very closely spaced points can't make a tick expensive.
		private const int MAX_PEEK = 24;
		private const int MAX_ADVANCE = 16;

		// The NPC counts as having reached a point that's the end of the route, or of the part of it that exists, once it's this close.
		private const float END_HOLD_RADIUS = 0.1f;

		private readonly NPCLocomotion _locomotion;
		private readonly NPCRoute _route;
		private NPCSpeed _speed;
		private bool _started;

		public NPCRouteFollower(NPCLocomotion locomotion, NPCRoute route, NPCSpeed speed)
		{
			_locomotion = locomotion;
			_route = route;
			_speed = speed;
		}

		public NPCRouteStatus Status { get; private set; } = NPCRouteStatus.Waiting;

		/// <summary>
		/// How far the NPC is from the point it's currently aiming at, for the debugger.
		/// </summary>
		public float TargetDistance { get; private set; }

		public void Start()
		{
			if (_started)
				return;

			_locomotion.Stuck += OnStuck;
			_started = true;
		}

		public void Dispose()
		{
			if (!_started)
				return;

			_locomotion.Stuck -= OnStuck;
			_started = false;
		}

		public void SetSpeed(NPCSpeed speed)
		{
			_speed = speed;

			if (_locomotion.HasDestination)
				_locomotion.SetSpeed(speed);
		}

		/// <summary>
		/// Call every frame. Points are read from the route each time, so nothing here goes stale when the game origin shifts the world.
		/// </summary>
		/// <param name="position">Where the NPC is</param>
		public void Tick(Vector3 position)
		{
			if (Status == NPCRouteStatus.Stuck || Status == NPCRouteStatus.Finished)
				return;

			AdvancePassed(position);

			if (!_route.TryPeek(0, out _))
			{
				Hold(_route.IsFinished ? NPCRouteStatus.Finished : NPCRouteStatus.Waiting);
				return;
			}

			Vector3 target = FindTarget(position, out bool atEnd);
			TargetDistance = Flat(target - position).magnitude;

			// Nowhere further to go, either because the route ends here or because the next part isn't loaded yet.
			if (atEnd && TargetDistance <= END_HOLD_RADIUS)
			{
				Hold(_route.IsFinished ? NPCRouteStatus.Finished : NPCRouteStatus.Waiting);
				return;
			}

			Status = NPCRouteStatus.Following;

			// Retargeting keeps the avoidance and stuck tracking going, which MoveTo would reset every frame.
			if (_locomotion.HasDestination)
				_locomotion.Retarget(target);
			else
				_locomotion.MoveTo(target, _speed);
		}

		// Drops the points the NPC has already walked past, so the target never swings back to something behind it.
		private void AdvancePassed(Vector3 position)
		{
			for (int i = 0; i < MAX_ADVANCE; i++)
			{
				if (!_route.TryPeek(0, out Vector3 current))
					return;

				Vector3 toNpc = Flat(position - current);

				if (!_route.TryPeek(1, out Vector3 next))
				{
					// The last point has no direction to be passed along, so it counts once the NPC is near it.
					if (toNpc.magnitude <= END_HOLD_RADIUS)
						_route.Advance();

					return;
				}

				// Points sitting on top of each other have no direction either, so they're skipped.
				Vector3 segment = Flat(next - current);
				if (segment.sqrMagnitude > 0.0001f && Vector3.Dot(toNpc, segment) <= 0f)
					return;

				_route.Advance();
			}
		}

		// Walks along the path from the NPC for the lookahead distance and returns the point reached.
		private Vector3 FindTarget(Vector3 position, out bool atEnd)
		{
			atEnd = false;

			Vector3 from = position;
			float remaining = LOOKAHEAD;

			for (int i = 0; i < MAX_PEEK; i++)
			{
				if (!_route.TryPeek(i, out Vector3 point))
				{
					atEnd = true;
					return from;
				}

				float length = Flat(point - from).magnitude;
				if (length >= remaining)
					return Vector3.Lerp(from, point, remaining / length);

				remaining -= length;
				from = point;
			}

			return from;
		}

		private void Hold(NPCRouteStatus status)
		{
			Status = status;

			if (_locomotion.HasDestination)
				_locomotion.Stop();
		}

		// The locomotion has already stopped itself by the time this is raised.
		private void OnStuck()
		{
			Status = NPCRouteStatus.Stuck;
		}

		private static Vector3 Flat(Vector3 vector)
		{
			vector.y = 0f;
			return vector;
		}
	}
}
