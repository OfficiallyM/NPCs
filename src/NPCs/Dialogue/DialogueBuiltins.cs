using NPCs.Common;
using NPCs.Trading;
using NPCs.Utilities;
using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace NPCs.Dialogue
{
	public static class DialogueBuiltins
	{
		public static void Register()
		{
			RegisterCommon();
			RegisterTrade();
			RegisterMemory();
		}

		private static void RegisterCommon()
		{
			DialogueRegistry.RegisterAction("give_item", (runner, argument) =>
			{
				if (string.IsNullOrEmpty(argument)) return;
				GameObject prefab = null;
				switch (argument)
				{
					case "silver":
						prefab = NPCs.I.GetItem(1000);
						break;
					default:
						prefab = itemdatabase.d.items.FirstOrDefault(i => i != null && i.name.ToLowerInvariant().Contains(argument));
						break;
				}

				if (prefab == null) return;
				Vector3 position = runner.transform.position + runner.transform.forward * 1f + Vector3.up * 1f;
				GameObject.Instantiate(prefab, position, Quaternion.identity);
			});

			DialogueRegistry.RegisterCondition("player_in_vehicle", (runner, argument) => mainscript.M.player.Car != null);
		}

		private static void RegisterTrade()
		{
			DialogueRegistry.RegisterAction("open_trade", (runner, argument) => {
				TradeSession session = runner.GetComponent<TradeSession>();
				if (session == null) return;
				session.Begin();
			});
		}

		private static void RegisterMemory()
		{
			DialogueRegistry.RegisterAction("set_memory", (runner, argument) =>
			{
				if (!TryGetMemory(runner, out NPCMemory memory) || !TrySplit("set_memory", argument, out string key, out string text)) return;

				object value = ParseValue(text);
				if (value is bool flag)
					memory.Set(key, flag);
				else if (value is float number)
					memory.Set(key, number);
				else
					memory.Set(key, (string)value);
			});

			DialogueRegistry.RegisterAction("add_memory", (runner, argument) =>
			{
				if (!TryGetMemory(runner, out NPCMemory memory) || !TrySplit("add_memory", argument, out string key, out string text)) return;

				if (!TryParseNumber("add_memory", text, out float amount)) return;
				memory.Set(key, memory.GetFloat(key) + amount);
			});

			DialogueRegistry.RegisterAction("clear_memory", (runner, argument) =>
			{
				if (!TryGetMemory(runner, out NPCMemory memory) || string.IsNullOrEmpty(argument)) return;
				memory.Remove(argument);
			});

			DialogueRegistry.RegisterCondition("memory", (runner, argument) =>
				TryGetMemory(runner, out NPCMemory memory) && !string.IsNullOrEmpty(argument) && memory.GetBool(argument));

			DialogueRegistry.RegisterCondition("memory_equals", (runner, argument) =>
			{
				if (!TryGetMemory(runner, out NPCMemory memory) || !TrySplit("memory_equals", argument, out string key, out string text)) return false;

				memory.TryGet(key, out object actual);
				object expected = ParseValue(text);

				// An unset key counts as the default of whatever it is being compared with.
				if (expected is bool flag)
					return (actual is bool storedFlag && storedFlag) == flag;
				if (expected is float number)
					return Mathf.Approximately(actual is float storedNumber ? storedNumber : 0f, number);
				return string.Equals(actual as string ?? string.Empty, (string)expected, StringComparison.Ordinal);
			});

			DialogueRegistry.RegisterCondition("memory_greater", (runner, argument) =>
				Compare("memory_greater", runner, argument, (actual, expected) => actual > expected));

			DialogueRegistry.RegisterCondition("memory_less", (runner, argument) =>
				Compare("memory_less", runner, argument, (actual, expected) => actual < expected));
		}

		private static bool TryGetMemory(ConversationRunner runner, out NPCMemory memory)
		{
			memory = runner.Npc?.Memory;
			return memory != null;
		}

		// Arguments look like "key=value".
		private static bool TrySplit(string name, string argument, out string key, out string value)
		{
			key = null;
			value = null;

			int split = argument?.IndexOf('=') ?? -1;
			if (split <= 0)
			{
				Logging.LogWarning($"'{name}' needs an argument like key=value, got '{argument}'.");
				return false;
			}

			key = argument.Substring(0, split).Trim();
			value = argument.Substring(split + 1).Trim();
			return true;
		}

		// Booleans first, then numbers, and anything else is text.
		private static object ParseValue(string text)
		{
			if (bool.TryParse(text, out bool flag)) return flag;
			if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number)) return number;
			return text;
		}

		private static bool TryParseNumber(string name, string text, out float number)
		{
			if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return true;

			Logging.LogWarning($"'{name}' needs a number, got '{text}'.");
			return false;
		}

		// Numeric comparison against a remembered value, which counts as 0 when unset.
		private static bool Compare(string name, ConversationRunner runner, string argument, Func<float, float, bool> test)
		{
			if (!TryGetMemory(runner, out NPCMemory memory)
				|| !TrySplit(name, argument, out string key, out string text)
				|| !TryParseNumber(name, text, out float expected))
				return false;

			return test(memory.GetFloat(key), expected);
		}
	}
}
