using UnityEngine;

namespace NPCs.Debugging
{
	/// <summary>
	/// Pushes an NPC along the way it faces at a fixed speed, so the walk can be watched before real locomotion exists.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class NPCDebugMover : MonoBehaviour
	{
		private Rigidbody _body;
		private RigidbodyInterpolation _previousInterpolation;

		/// <summary>
		/// Metres per second along the NPC's forward. Zero holds it still.
		/// </summary>
		public float Speed { get; set; }

		private void Awake()
		{
			_body = GetComponent<Rigidbody>();
		}

		private void OnEnable()
		{
			if (_body == null)
				return;

			// The root only moves on physics steps. Interpolating it gives the gait a smooth position to measure each frame.
			_previousInterpolation = _body.interpolation;
			_body.interpolation = RigidbodyInterpolation.Interpolate;
		}

		private void OnDisable()
		{
			if (_body != null)
				_body.interpolation = _previousInterpolation;
		}

		private void FixedUpdate()
		{
			if (_body == null)
				return;

			Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
			Vector3 velocity = _body.velocity;

			// Only the horizontal speed is set, so gravity and slopes still act on the NPC.
			_body.velocity = new Vector3(forward.x * Speed, velocity.y, forward.z * Speed);
		}
	}
}
