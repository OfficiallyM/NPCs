using NPCs.Dialogue.Core;
using NPCs.Utilities;
using System;
using System.Collections.Generic;

namespace NPCs.Dialogue
{
	public static class DialogueRegistry
	{
		private const string NEGATE_PREFIX = "not:";

		private static Dictionary<string, Conversation> _conversations;
		private static Dictionary<string, Action<ConversationRunner>> _actions;
		private static Dictionary<string, Func<ConversationRunner, string, bool>> _conditions;
		private static HashSet<string> _unknownConditions;

		public static void Initialise()
		{
			_conversations = new Dictionary<string, Conversation>();
			_actions = new Dictionary<string, Action<ConversationRunner>>();
			_conditions = new Dictionary<string, Func<ConversationRunner, string, bool>>();
			_unknownConditions = new HashSet<string>();

			DialogueLoader.Load();
		}

		/// <summary>
		/// Registers a conversation. If a conversation with the same ID already
		/// exists it will be overwritten, allowing mods to override built-in conversations.
		/// </summary>
		/// <param name="conversation">Conversation tree to register.</param>
		public static void Register(Conversation conversation)
		{
			if (conversation == null) return;
			_conversations[conversation.Id] = conversation;
		}

		/// <summary>
		/// Registers a named action handler for use in conversation options.
		/// </summary>
		/// <param name="key">Action name.</param>
		/// <param name="handler">Action to perform when called.</param>
		public static void RegisterAction(string key, Action<ConversationRunner> handler)
		{
			if (string.IsNullOrEmpty(key) || handler == null) return;
			_actions[key] = handler;
		}

		/// <summary>
		/// Registers a named condition for use in conversation nodes and options.
		/// </summary>
		/// <param name="key">Condition name.</param>
		/// <param name="handler">Returns whether the condition holds. Receives the runner and the argument written after the colon in the key, or null if there wasn't one.</param>
		/// <remarks>Checked every frame while options UI is active, so handlers should be conscious of performance.</remarks>
		public static void RegisterCondition(string key, Func<ConversationRunner, string, bool> handler)
		{
			if (string.IsNullOrEmpty(key) || handler == null) return;
			_conditions[key] = handler;
		}

		/// <summary>
		/// Checks whether something with conditions is currently available.
		/// </summary>
		/// <param name="element">Node or option to check.</param>
		/// <param name="runner">Runner the conditions are evaluated for.</param>
		/// <returns>True if it has no conditions, or its conditions pass</returns>
		public static bool ConditionPasses(ConversationConditional element, ConversationRunner runner)
		{
			if (element.Conditions == null || element.Conditions.Count == 0) return true;

			foreach (string condition in element.Conditions)
			{
				bool result = Evaluate(condition, runner);

				if (element.Match == ConditionMatch.Any && result) return true;
				if (element.Match == ConditionMatch.All && !result) return false;
			}

			// Every condition passed for All, or none did for Any.
			return element.Match == ConditionMatch.All;
		}

		/// <summary>
		/// Returns the conversation with the given ID, or null if not found.
		/// </summary>
		/// <param name="id">Conversation ID to find.</param>
		/// <returns>Conversation tree if exists, otherwise, null.</returns>
		public static Conversation Get(string id)
		{
			_conversations.TryGetValue(id, out var conversation);
			return conversation;
		}

		/// <summary>
		/// Triggers the action registered under the given key, if any.
		/// </summary>
		/// <param name="key">Action key to trigger.</param>
		public static void TriggerAction(string key, ConversationRunner runner)
		{
			if (string.IsNullOrEmpty(key)) return;
			if (_actions.TryGetValue(key, out var handler))
				handler.Invoke(runner);
		}

		private static bool Evaluate(string expression, ConversationRunner runner)
		{
			if (string.IsNullOrEmpty(expression)) return false;

			bool negate = false;
			while (expression.StartsWith(NEGATE_PREFIX, StringComparison.Ordinal))
			{
				negate = !negate;
				expression = expression.Substring(NEGATE_PREFIX.Length);
			}

			ParseKey(expression, out string key, out string argument);

			if (!_conditions.TryGetValue(key, out var handler))
			{
				// Cache failed conditions so they only log once rather than every frame.
				if (_unknownConditions.Add(key))
					Logging.LogWarning($"Unknown dialogue condition '{key}'.");
				return false;
			}

			return handler(runner, argument) != negate;
		}

		// Splits "key:argument" at the first colon, so arguments can contain colons themselves.
		private static void ParseKey(string value, out string key, out string argument)
		{
			int split = value.IndexOf(':');
			key = split < 0 ? value : value.Substring(0, split);
			argument = split < 0 ? null : value.Substring(split + 1);
		}
	}
}
