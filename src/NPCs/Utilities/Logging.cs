namespace NPCs.Utilities
{
	internal static class Logging
	{
		public static void Log(string message, TLDLoader.Logger.LogLevel logLevel = TLDLoader.Logger.LogLevel.Info) =>
			NPCs.I.Logger.Log(message, logLevel);

		public static void LogDebug(string message)
		{
			if (!NPCs.Debug) return;
			NPCs.I.Logger.LogDebug(message);
		}

		public static void LogInfo(string message) =>
			NPCs.I.Logger.LogInfo(message);

		public static void LogWarning(string message) =>
			NPCs.I.Logger.LogWarning(message);

		public static void LogError(string message) =>
			NPCs.I.Logger.LogError(message);

		public static void LogCritical(string message) =>
			NPCs.I.Logger.LogCritical(message);
	}
}
