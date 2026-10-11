using Newtonsoft.Json;
using System.Collections.Generic;

namespace NPCs.Dialogue.Core
{
	/// <summary>
	/// A single node in a conversation tree.
	/// </summary>
	public sealed class ConversationNode : ConversationConditional
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
		/// Registered action keys, optionally with an argument after a colon, run in order when the node is entered.
		/// </summary>
		/// <remarks>Run after the line is shown. If one moves the conversation on, the rest are skipped.</remarks>
		public List<string> Actions { get; set; }

		/// <summary>
		/// Player-selectable responses.
		/// </summary>
		public List<ConversationOption> Options { get; set; }

		/// <summary>
		/// The node IDs to advance to. One is picked from those whose conditions pass.
		/// </summary>
		/// <remarks>Only used when Options is empty. Empty or null closes the conversation.</remarks>
		public List<string> Next { get; set; }

		/// <summary>
		/// When several nodes are candidates, only those with the highest priority among the available ones are considered.
		/// </summary>
		public int Priority { get; set; }

		/// <summary>
		/// How likely this node is to be picked compared to other candidates of the same priority.
		/// </summary>
		public float Weight { get; set; } = 1f;
	}
}
