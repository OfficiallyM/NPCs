using NPCs.AI.Enums;
using UnityEngine;

namespace NPCs.AI.Routes
{
	public sealed class RoadRoute : NPCRoute
	{
		// Bones closer together than this are skipped when working out which way the road runs, because duplicates at the joins between pieces have no direction.
		private const float MIN_SEGMENT = 0.05f;
		private const int MAX_TANGENT_STEPS = 3;

		private readonly roadGenScript _road;
		private readonly int _step;
		private readonly float _laneOffset;
		private int _piece;
		private int _bone;

		private RoadRoute(roadGenScript road, int piece, int bone, RouteDirection direction, float laneOffset)
		{
			_road = road;
			_piece = piece;
			_bone = bone;
			_step = direction == RouteDirection.Forwards ? 1 : -1;
			_laneOffset = laneOffset;
		}

		/// <summary>
		/// Starts a route at the placed road bone nearest a position.
		/// </summary>
		/// <param name="position">Where the NPC is</param>
		/// <param name="direction">Which way along the road to go</param>
		/// <param name="laneOffset">How far to the right of the centre line to keep, looking the way the NPC travels. Negative is left</param>
		/// <param name="route">The route, if a placed road piece was found</param>
		public static bool TryCreateNear(Vector3 position, RouteDirection direction, float laneOffset, out RoadRoute route)
		{
			route = null;

			roadGenScript bestRoad = null;
			int bestPiece = -1;
			int bestBone = -1;
			float bestSqr = float.PositiveInfinity;

			var roads = TerrainGenerator.TG.roads;
			for (int r = 0; r < roads.Count; r++)
			{
				roadGenScript road = roads[r];
				if (road == null || road.roadList == null)
					continue;

				// The game keeps a list of the pieces it has placed, which is far shorter than the whole road.
				for (int i = 0; i < road.EzekenVanUt.Count; i++)
				{
					int index = road.EzekenVanUt[i];
					roadscript piece = GetPiece(road, index);
					if (piece == null || piece.bones == null)
						continue;

					for (int b = 0; b < piece.bones.Length; b++)
					{
						Transform bone = piece.bones[b];
						if (bone == null)
							continue;

						float sqr = (bone.position - position).sqrMagnitude;
						if (sqr >= bestSqr)
							continue;

						bestSqr = sqr;
						bestRoad = road;
						bestPiece = index;
						bestBone = b;
					}
				}
			}

			if (bestRoad == null)
				return false;

			route = new RoadRoute(bestRoad, bestPiece, bestBone, direction, laneOffset);
			return true;
		}

		public override bool IsFinished
		{
			get
			{
				if (_step < 0)
					return _piece <= 0 && _bone <= 0;

				roadscript piece = GetPiece(_road, _piece);
				return piece != null && piece.bones != null && _piece >= _road.roadList.Length - 1 && _bone >= piece.bones.Length - 1;
			}
		}

		public override bool TryPeek(int ahead, out Vector3 point)
		{
			point = Vector3.zero;

			int piece = _piece;
			int bone = _bone;
			for (int i = 0; i < ahead; i++)
			{
				if (!TryStep(ref piece, ref bone, _step))
					return false;
			}

			return TryGetLanePoint(piece, bone, out point);
		}

		public override void Advance() => TryStep(ref _piece, ref _bone, _step);

		// A piece the game has destroyed stays in the list until it's placed again, and Unity treats it as null.
		private static roadscript GetPiece(roadGenScript road, int index)
		{
			if (road.roadList == null || index < 0 || index >= road.roadList.Length)
				return null;

			roadscript piece = road.roadList[index];
			return piece != null ? piece : null;
		}

		// Moves a cursor one bone along, crossing into the next piece at the end of this one.
		private bool TryStep(ref int piece, ref int bone, int direction)
		{
			roadscript current = GetPiece(_road, piece);
			if (current == null || current.bones == null)
				return false;

			int next = bone + direction;
			if (next >= 0 && next < current.bones.Length)
			{
				bone = next;
				return true;
			}

			roadscript neighbour = GetPiece(_road, piece + direction);
			if (neighbour == null || neighbour.bones == null || neighbour.bones.Length == 0)
				return false;

			piece += direction;
			bone = direction > 0 ? 0 : neighbour.bones.Length - 1;
			return true;
		}

		private bool TryGetBonePosition(int piece, int bone, out Vector3 position)
		{
			position = Vector3.zero;

			roadscript road = GetPiece(_road, piece);
			if (road == null || road.bones == null || bone < 0 || bone >= road.bones.Length)
				return false;

			Transform transform = road.bones[bone];
			if (transform == null)
				return false;

			position = transform.position;
			return true;
		}

		private bool TryGetLanePoint(int piece, int bone, out Vector3 point)
		{
			point = Vector3.zero;

			if (!TryGetBonePosition(piece, bone, out Vector3 here))
				return false;

			point = here;
			if (Mathf.Approximately(_laneOffset, 0f))
				return true;

			if (!TryGetTangent(piece, bone, here, out Vector3 tangent))
				return false;

			// Cross(up, forward) points to the right of the direction of travel. The bones' own axes aren't used because their forward points up.
			point = here + Vector3.Cross(Vector3.up, tangent) * _laneOffset;
			return true;
		}

		// The flat direction the road runs in at a bone, in the direction of travel.
		private bool TryGetTangent(int piece, int bone, Vector3 here, out Vector3 tangent)
		{
			tangent = Vector3.zero;

			bool hasFront = TryFindDistinct(piece, bone, here, _step, out Vector3 front);
			bool hasBack = TryFindDistinct(piece, bone, here, -_step, out Vector3 back);

			Vector3 span;
			if (hasFront && hasBack)
				span = front - back;
			else if (hasFront)
				span = front - here;
			else if (hasBack)
				span = here - back;
			else
				return false;

			span.y = 0f;
			if (span.sqrMagnitude < 0.0001f)
				return false;

			tangent = span.normalized;
			return true;
		}

		private bool TryFindDistinct(int piece, int bone, Vector3 here, int direction, out Vector3 found)
		{
			found = Vector3.zero;

			for (int i = 0; i < MAX_TANGENT_STEPS; i++)
			{
				if (!TryStep(ref piece, ref bone, direction) || !TryGetBonePosition(piece, bone, out found))
					return false;

				Vector3 flat = found - here;
				flat.y = 0f;
				if (flat.magnitude > MIN_SEGMENT)
					return true;
			}

			return false;
		}
	}
}
