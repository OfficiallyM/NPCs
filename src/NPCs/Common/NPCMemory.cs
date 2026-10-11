using System.Collections.Generic;

namespace NPCs.Common
{
	/// <summary>
	/// Things an NPC remembers, such as having already given the player a freebie.
	/// </summary>
	/// <remarks>
	/// Values are limited to bool, float and string so they stay simple to save later.
	/// A key that was never set reads as false, 0 or an empty string.
	/// </remarks>
	public sealed class NPCMemory
	{
		private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

		public bool Has(string key) => _values.ContainsKey(key);

		/// <summary>
		/// Gets the stored value as it was set.
		/// </summary>
		/// <returns>True if the key has a value</returns>
		public bool TryGet(string key, out object value) => _values.TryGetValue(key, out value);

		public bool GetBool(string key) => _values.TryGetValue(key, out object value) && value is bool stored && stored;

		public float GetFloat(string key) => _values.TryGetValue(key, out object value) && value is float stored ? stored : 0f;

		public string GetString(string key) => _values.TryGetValue(key, out object value) && value is string stored ? stored : string.Empty;

		public void Set(string key, bool value) => _values[key] = value;

		public void Set(string key, float value) => _values[key] = value;

		public void Set(string key, string value) => _values[key] = value ?? string.Empty;

		public bool Remove(string key) => _values.Remove(key);
	}
}
