using System.Collections.Generic;
using UnityEngine;

namespace NPCs.AI.Routes
{
	/// <summary>
	/// A hand-made route through a fixed list of points.
	/// </summary>
	public sealed class PointRoute : NPCRoute
	{
		private readonly List<GlobalPoint> _points;
		private int _cursor;

		public PointRoute(IEnumerable<GlobalPoint> points)
		{
			_points = new List<GlobalPoint>(points);
		}

		public int Count => _points.Count;

		public override bool IsFinished => _cursor >= _points.Count;

		public override bool TryPeek(int ahead, out Vector3 point)
		{
			int index = _cursor + ahead;
			if (index < 0 || index >= _points.Count)
			{
				point = Vector3.zero;
				return false;
			}

			point = _points[index].ToWorld();
			return true;
		}

		public override void Advance()
		{
			if (_cursor < _points.Count)
				_cursor++;
		}
	}
}
