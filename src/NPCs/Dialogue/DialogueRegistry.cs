using NPCs.Dialogue.Core;
using System;
using System.Collections.Generic;

namespace NPCs.Dialogue
{
	public static class DialogueRegistry
	{
		private static Dictionary<string, Conversation> _conversations;
		private static Dictionary<string, Action> _actions;

		public static void Initialise()
		{
			_conversations = new Dictionary<string, Conversation>();
			_actions = new Dictionary<string, Action>();

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
		public static void RegisterAction(string key, Action handler)
		{
			if (string.IsNullOrEmpty(key) || handler == null) return;
			_actions[key] = handler;
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
		public static void TriggerAction(string key)
		{
			if (string.IsNullOrEmpty(key)) return;
			if (_actions.TryGetValue(key, out var handler))
				handler.Invoke();
		}
	}
}
