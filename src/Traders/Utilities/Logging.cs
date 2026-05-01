namespace Traders.Utilities
{
	internal static class Logging
	{
		public static void Log(string message, TLDLoader.Logger.LogLevel logLevel = TLDLoader.Logger.LogLevel.Info) =>
			Traders.I.Logger.Log(message, logLevel);

		public static void LogDebug(string message)
		{
			if (!Traders.Debug) return;
			Traders.I.Logger.LogDebug(message);
		}

		public static void LogInfo(string message) =>
			Traders.I.Logger.LogInfo(message);

		public static void LogWarning(string message) =>
			Traders.I.Logger.LogWarning(message);

		public static void LogError(string message) =>
			Traders.I.Logger.LogError(message);

		public static void LogCritical(string message) =>
			Traders.I.Logger.LogCritical(message);
	}
}
