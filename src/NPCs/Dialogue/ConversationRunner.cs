using NPCs.Dialogue.Core;
using NPCs.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs.Dialogue
{
	/// <summary>
	/// Manages the state of an active conversation.
	/// </summary>
	public class ConversationRunner : MonoBehaviour
	{
		private Conversation _conversation;
		private ConversationNode _currentNode;
		private Dictionary<string, string> _variables;

		/// <summary>
		/// Whether a conversation is currently active.
		/// </summary>
		public bool IsActive => _currentNode != null;

		/// <summary>
		/// The current node being displayed.
		/// </summary>
		public ConversationNode CurrentNode => _currentNode;

		/// <summary>
		/// Fired when the current node changes.
		/// </summary>
		public event Action<ConversationNode> OnNodeChanged;

		/// <summary>
		/// Fired when the conversation ends.
		/// </summary>
		public event Action OnConversationEnded;

		/// <summary>
		/// Starts a conversation by ID, with optional variable substitutions.
		/// </summary>
		/// <param name="conversationId">ID of the conversation to start.</param>
		/// <param name="variables">Optional variables to substitute into node text.</param>
		public void StartConversation(string conversationId, Dictionary<string, string> variables = null)
		{
			if (ConversationUI.HasActiveConversation)
			{
				Logging.LogWarning($"Conversation '{conversationId}' blocked, player is already in a conversation.");
				return;
			}

			Conversation conversation = DialogueRegistry.Get(conversationId);
			if (conversation == null)
			{
				Logging.LogWarning($"Conversation not found: {conversationId}");
				return;
			}

			_conversation = conversation;
			_variables = variables ?? new Dictionary<string, string>();

			AdvanceTo(conversation.Entry);

			ConversationUI.SetActiveRunner(this);
		}

		/// <summary>
		/// Advances the conversation using the selected option index.
		/// </summary>
		/// <param name="optionIndex">Index of the selected option.</param>
		public void SelectOption(int optionIndex)
		{
			if (!IsActive) return;
			if (_currentNode.Options == null || optionIndex >= _currentNode.Options.Count)
			{
				Logging.LogWarning($"Invalid option index {optionIndex} for node '{_currentNode.Id}'.");
				return;
			}

			ConversationOption option = _currentNode.Options[optionIndex];

			if (!string.IsNullOrEmpty(option.Action))
				DialogueRegistry.TriggerAction(option.Action);

			AdvanceTo(option.Next);
		}

		/// <summary>
		/// Advances the conversation from an auto-advance or terminal node.
		/// Should only be called when the current node has no options.
		/// </summary>
		public void Advance()
		{
			if (!IsActive) return;
			if (_currentNode.Options != null && _currentNode.Options.Count > 0)
			{
				Logging.LogWarning($"Advance() called on node '{_currentNode.Id}' which has options. Use SelectOption() instead.");
				return;
			}

			AdvanceTo(_currentNode.Next);
		}

		/// <summary>
		/// Ends the active conversation immediately.
		/// </summary>
		public void EndConversation()
		{
			_currentNode = null;
			_conversation = null;
			_variables = null;
			ConversationUI.ClearActiveRunner();
			OnConversationEnded?.Invoke();
		}

		/// <summary>
		/// Resolves variable substitutions in the given text.
		/// </summary>
		/// <param name="text">Raw text containing variable placeholders.</param>
		/// <returns>Text with all known variables substituted.</returns>
		public string ResolveText(string text)
		{
			if (string.IsNullOrEmpty(text) || _variables == null) return text;

			foreach (var variable in _variables)
				text = text.Replace($"{{{variable.Key}}}", variable.Value);

			return text;
		}

		private void AdvanceTo(string nodeId)
		{
			if (string.IsNullOrEmpty(nodeId))
			{
				EndConversation();
				return;
			}

			if (!_conversation.Nodes.TryGetValue(nodeId, out ConversationNode node))
			{
				Logging.LogWarning($"Node not found: '{nodeId}' in conversation '{_conversation.Id}'.");
				EndConversation();
				return;
			}

			if (node.Options != null && node.Options.Count > 0 && node.Next != null)
				Logging.LogWarning($"Node '{node.Id}' has both options and a next node defined. Options will be used.");

			node.RollText();
			_currentNode = node;
			OnNodeChanged?.Invoke(_currentNode);
		}
	}
}
