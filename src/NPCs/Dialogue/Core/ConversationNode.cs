using Newtonsoft.Json;
using System.Collections.Generic;

namespace NPCs.Dialogue.Core
{
	/// <summary>
	/// A single node in a conversation tree.
	/// </summary>
	public sealed class ConversationNode
	{
		/// <summary>
		/// The unique identifier for this node within the conversation.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// All possible text options for this node. One is selected at random when the node is entered.
		/// </summary>
		[JsonProperty("text")]
		public List<string> TextOptions { get; set; }

		/// <summary>
		/// Picks a random line from <see cref="TextOptions"/>.
		/// </summary>
		/// <returns>The chosen line, or an empty string if the node has none</returns>
		public string RollText()
		{
			if (TextOptions == null || TextOptions.Count == 0)
				return string.Empty;

			return TextOptions[UnityEngine.Random.Range(0, TextOptions.Count)];
		}

		/// <summary>
		/// Player-selectable responses.
		/// </summary>
		public List<ConversationOption> Options { get; set; }

		/// <summary>
		/// The next node ID to advance to.
		/// </summary>
		/// <remarks>Only used when Options is empty. Null closes the conversation.</remarks>
		public string Next { get; set; }
	}
}
