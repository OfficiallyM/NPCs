using UnityEngine;

namespace NPCs.Common
{
	/// <summary>
	/// Forwards Unity trigger events from a child GameObject to the parent.
	/// Required because MonoBehaviour trigger callbacks must be on the same GameObject as the collider.
	/// </summary>
	internal class TriggerForwarder : MonoBehaviour
	{
		public event System.Action<Collider> OnEnter;
		public event System.Action<Collider> OnExit;

		private void OnTriggerEnter(Collider other) => OnEnter?.Invoke(other);
		private void OnTriggerExit(Collider other) => OnExit?.Invoke(other);
	}
}
