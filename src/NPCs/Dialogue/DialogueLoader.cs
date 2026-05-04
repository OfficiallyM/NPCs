using Newtonsoft.Json;
using NPCs.Dialogue.Core;
using NPCs.Utilities;
using System.IO;
using TLDLoader;

namespace NPCs.Dialogue
{
	/// <summary>
	/// Loads conversation JSON files from the mod assets folder and registers them with <see cref="DialogueRegistry"/>.
	/// </summary>
	public static class DialogueLoader
	{
		/// <summary>
		/// Loads all conversation JSON files from the mod assets folder.
		/// </summary>
		public static void Load()
		{
			string folder = ModLoader.GetModAssetsFolder(NPCs.I);
			string[] files = Directory.GetFiles(folder, "*.json");

			foreach (string file in files)
			{
				LoadFile(file);
			}

			Logging.LogDebug($"Loaded {files.Length} conversation file(s).");
		}

		/// <summary>
		/// Loads and registers a single conversation file.
		/// </summary>
		/// <param name="path">Absolute path to the JSON file.</param>
		private static void LoadFile(string path)
		{
			string json = File.ReadAllText(path);
			Conversation conversation = JsonConvert.DeserializeObject<Conversation>(json);

			if (conversation == null)
			{
				Logging.LogError($"Failed to deserialise conversation file: {path}");
				return;
			}

			if (string.IsNullOrEmpty(conversation.Id))
			{
				Logging.LogWarning($"Conversation file has no ID, skipping: {path}");
				return;
			}

			if (string.IsNullOrEmpty(conversation.Entry))
			{
				Logging.LogWarning($"Conversation '{conversation.Id}' has no entry node defined, skipping.");
				return;
			}

			// Populate each node's ID from its dictionary key, since the
			// node itself doesn't store its own key in the JSON.
			foreach (var pair in conversation.Nodes)
			{
				pair.Value.Id = pair.Key;
			}

			DialogueRegistry.Register(conversation);
			Logging.LogDebug($"Loaded conversation: {conversation.Id}");
		}
	}
}
