using System.Collections.Generic;

namespace NPCs.Dialogue.Core
{
	/// <summary>
	/// A single player response option within a conversation node.
	/// </summary>
	public sealed class ConversationOption : ConversationConditional
	{
		/// <summary>
		/// The text displayed to the player for this option.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// The node IDs this option can lead to. One is picked from those whose conditions pass.
		/// </summary>
		/// <remarks>Empty or null closes the conversation. The option is hidden if none of them are available.</remarks>
		public List<string> Next { get; set; }

		/// <summary>
		/// Optional named action to fire when this option is selected.
		/// </summary>
		/// <remarks>Resolved against registered action handlers by ConversationRunner.</remarks>
		public string Action { get; set; }
	}
}
