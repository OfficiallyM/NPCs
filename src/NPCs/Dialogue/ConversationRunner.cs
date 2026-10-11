using NPCs.Common;
using NPCs.Dialogue.Core;
using NPCs.Utilities;
using Steamworks;
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
		/// <summary>
		/// Reserved node ID that hides the conversation and keeps it running behind the scenes.
		/// </summary>
		public const string BACKGROUND_NODE = "@background";

		public string ConversationId;
		public float ConversationRange = 5f;
		public event Action OnBackground;
		public NPC Npc;

		private Conversation _conversation;
		private ConversationNode _currentNode;
		private Dictionary<string, string> _variables;
		private Transform _playerTransform;

		/// <summary>
		/// Whether a conversation is currently active.
		/// </summary>
		public bool IsActive => _currentNode != null;

		/// <summary>
		/// The current node being displayed.
		/// </summary>
		public ConversationNode CurrentNode => _currentNode;

		/// <summary>
		/// The line chosen for the current node.
		/// </summary>
		public string CurrentText { get; private set; }

		/// <summary>
		/// Fired when the current node changes.
		/// </summary>
		public event Action<ConversationNode> OnNodeChanged;

		/// <summary>
		/// Fired when the conversation ends.
		/// </summary>
		public event Action OnConversationEnded;

		/// <summary>
		/// Fired when the conversation is triggered to end.
		/// </summary>
		public event Action<ConversationNode> OnConversationEnding;

		/// <summary>
		/// Starts a conversation.
		/// </summary>
		public void StartConversation()
		{
			if (ConversationUI.HasActiveConversation)
			{
				if (ConversationUI.ActiveRunner == this) return;
				ConversationUI.ActiveRunner.EndConversation();
			}

			Conversation conversation = DialogueRegistry.Get(ConversationId);
			if (conversation == null)
			{
				Logging.LogWarning($"Conversation not found: {ConversationId}");
				return;
			}

			_conversation = conversation;
			AdvanceToAny(conversation.Entry);

			// Nothing is active if no entry node was available or the first one ended straight away.
			if (IsActive)
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

			// Conditions can change between the option being drawn and clicked.
			if (!IsOptionAvailable(option)) return;

			if (!string.IsNullOrEmpty(option.Action))
				DialogueRegistry.TriggerAction(option.Action, this);

			AdvanceToAny(option.Next);
		}

		/// <summary>
		/// Whether an option should currently be shown.
		/// </summary>
		/// <param name="option">Option from the current node.</param>
		/// <returns>True if its conditions pass and, if it leads somewhere, at least one destination is available</returns>
		public bool IsOptionAvailable(ConversationOption option)
		{
			if (!DialogueRegistry.ConditionPasses(option, this)) return false;

			// Closes the conversation, so there's nothing to be unavailable.
			if (option.Next == null || option.Next.Count == 0) return true;

			foreach (string nodeId in option.Next)
			{
				if (IsAvailable(nodeId, out _))
					return true;
			}

			return false;
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

			AdvanceToAny(_currentNode.Next);
		}

		/// <summary>
		/// Advances to one of several candidate nodes. Only available nodes are considered, the highest priority among them
		/// wins, and ties are broken by weight.
		/// </summary>
		/// <param name="candidates">Node IDs to choose between. Empty or null closes the conversation.</param>
		public void AdvanceToAny(List<string> candidates)
		{
			if (candidates == null || candidates.Count == 0)
			{
				EndConversation();
				return;
			}

			string chosen = PickCandidate(candidates);
			if (chosen == null)
			{
				Logging.LogWarning($"None of [{string.Join(", ", candidates)}] are available in conversation '{_conversation?.Id}'.");
				EndConversation();
				return;
			}

			AdvanceTo(chosen);
		}

		/// <summary>
		/// Advance conversation to a specific tree node, regardless of its conditions.
		/// </summary>
		/// <param name="nodeId">Conversation node to advance to.</param>
		public void AdvanceTo(string nodeId)
		{
			if (_conversation == null || string.IsNullOrEmpty(nodeId))
			{
				EndConversation();
				return;
			}

			// Support for special reserved keywords in nodeId.
			if (nodeId == BACKGROUND_NODE)
			{
				ConversationUI.Hide();
				OnBackground?.Invoke();
				return;
			}

			if (_conversation.Nodes == null || !_conversation.Nodes.TryGetValue(nodeId, out ConversationNode node))
			{
				Logging.LogWarning($"Node not found: '{nodeId}' in conversation '{_conversation.Id}'.");
				EndConversation();
				return;
			}

			bool hasNext = node.Next != null && node.Next.Count > 0;
			if (node.Options != null && node.Options.Count > 0 && hasNext)
				Logging.LogWarning($"Node '{node.Id}' has both options and a next node defined. Options will be used.");

			CurrentText = node.RollText();
			_currentNode = node;
			OnNodeChanged?.Invoke(_currentNode);

			bool hasOptions = node.Options != null && node.Options.Count > 0;
			if (hasOptions) return;

			// Terminal node, fade out the line and end the conversation.
			if (!hasNext)
			{
				OnConversationEnding?.Invoke(node);
				EndConversation();
				return;
			}

			// Automatically trigger a continue for background conversations.
			bool onlyBackground = node.Next.Count == 1 && node.Next[0] == BACKGROUND_NODE;
			if (onlyBackground && ConversationUI.HasBackgroundConversation)
				AdvanceToAny(node.Next);
		}

		private bool IsAvailable(string nodeId, out ConversationNode node)
		{
			node = null;

			// Background isn't a real node, so it is always available.
			if (nodeId == BACKGROUND_NODE) return true;

			return _conversation?.Nodes != null
				&& _conversation.Nodes.TryGetValue(nodeId, out node)
				&& DialogueRegistry.ConditionPasses(node, this);
		}

		private string PickCandidate(List<string> candidates)
		{
			int bestPriority = int.MinValue;
			var pool = new List<KeyValuePair<string, float>>();

			foreach (string nodeId in candidates)
			{
				if (!IsAvailable(nodeId, out ConversationNode node)) continue;

				int priority = node?.Priority ?? 0;
				float weight = node?.Weight ?? 1f;

				if (priority > bestPriority)
				{
					bestPriority = priority;
					pool.Clear();
				}

				if (priority == bestPriority)
					pool.Add(new KeyValuePair<string, float>(nodeId, Mathf.Max(0f, weight)));
			}

			if (pool.Count == 0) return null;

			float total = 0f;
			foreach (var candidate in pool)
				total += candidate.Value;

			// All weights at zero would otherwise leave nothing to pick.
			if (total <= 0f)
				return pool[UnityEngine.Random.Range(0, pool.Count)].Key;

			float roll = UnityEngine.Random.Range(0f, total);
			float cumulative = 0f;
			foreach (var candidate in pool)
			{
				cumulative += candidate.Value;
				if (roll < cumulative)
					return candidate.Key;
			}

			return pool[pool.Count - 1].Key;
		}

		/// <summary>
		/// Ends the active conversation immediately.
		/// </summary>
		public void EndConversation()
		{
			_currentNode = null;
			CurrentText = null;
			_conversation = null;
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

		/// <summary>
		/// Add a variable for use within the conversation text.
		/// </summary>
		/// <param name="key">Variable replacement key.</param>
		/// <param name="value">Variable value to show in text.</param>
		public void AddVariable(string key, string value)
		{
			_variables[key] = value;
		}

		/// <summary>
		/// Get the value of a variable associated with the specified key.
		/// </summary>
		/// <param name="key">The key of the variable to retrieve. Cannot be null.</param>
		/// <returns>The value associated with the specified key if it exists; otherwise, null.</returns>
		public string GetVariable(string key)
		{
			return _variables.ContainsKey(key) ? _variables[key] : null;
		}

		private void Awake()
		{
			_variables = new Dictionary<string, string>()
			{
				{ "playerName", SteamManager.Initialized ? SteamFriends.GetPersonaName() : "traveller" }
			};
			_playerTransform = mainscript.M.player.transform;
		}

		private void Start()
		{
			Npc.OnDeath += () =>
			{
				EndConversation();
			};
		}

		private void Update()
		{
			if (!IsActive) return;

			float distance = Vector3.Distance(transform.position, _playerTransform.position);
			if (distance > ConversationRange)
				EndConversation();
		}
	}
}
