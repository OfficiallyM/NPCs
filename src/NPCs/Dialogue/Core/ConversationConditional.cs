using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace NPCs.Dialogue.Core
{
	public abstract class ConversationConditional
	{
		/// <summary>
		/// Registered condition keys, optionally with an argument after a colon and a "not:" prefix to negate.
		/// </summary>
		/// <remarks>No conditions means it is always available.</remarks>
		public List<string> Conditions { get; set; }

		/// <summary>
		/// Whether all conditions must pass or just one.
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public ConditionMatch Match { get; set; } = ConditionMatch.All;
	}
}
