using UnityEngine;

namespace NPCs.AI.Routes
{
	/// <summary>
	/// A position that stays correct when the game moves the world, because it's stored relative to the world's offset instead of to the scene.
	/// Doubles are used because the offset gets large enough that floats would lose the detail.
	/// </summary>
	/// <remarks>
	/// Deliberately not using the vanilla doublecoord for flexibility.
	/// </remarks>
	public readonly struct GlobalPoint
	{
		public double X { get; }
		public double Y { get; }
		public double Z { get; }

		public GlobalPoint(double x, double y, double z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		/// <summary>
		/// Records where a point in the scene is, as it is right now.
		/// </summary>
		public static GlobalPoint FromWorld(Vector3 world)
		{
			var offset = mainscript.M.mainWorld.coord;
			return new GlobalPoint(world.x - offset.x, world.y - offset.y, world.z - offset.z);
		}

		/// <summary>
		/// Where the point is in the scene right now.
		/// </summary>
		public Vector3 ToWorld()
		{
			var offset = mainscript.M.mainWorld.coord;
			return new Vector3((float)(X + offset.x), (float)(Y + offset.y), (float)(Z + offset.z));
		}
	}
}
