using System.Collections.Generic;

namespace NPCs.Dialogue.Models
{
	/// <summary>
	/// A complete conversation tree.
	/// </summary>
	public sealed class Conversation
	{
		/// <summary>
		/// Unique identifier for this conversation.
		/// </summary>
		/// <remarks>Used to reference and optionally override conversations from other mods.</remarks>
		public string Id { get; set; }

		/// <summary>
		/// The node IDs the conversation can start from. One is picked from those whose conditions pass.
		/// </summary>
		public List<string> Entry { get; set; }

		/// <summary>
		/// All nodes in this conversation, keyed by node ID.
		/// </summary>
		public Dictionary<string, ConversationNode> Nodes { get; set; }
	}
}
