using System;
using System.IO;
using UnityEngine;

namespace SpawnMod
{
	public static class SpawnExtensions
	{
		// parses the item id from the string, disregards case, and verifies result is defined
		public static bool ParseEnum<T>(this string arg, out T @enum) where T : struct, Enum
		{
			return Enum.TryParse(arg, true, out @enum) && Enum.IsDefined(typeof(T), @enum);
		}

		// compares two strings disregarding case
		public static bool EqualsInsensitive(this string a, string b)
		{
			return string.Compare(a, b, true) == 0;
		}

		public static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

		private static readonly string _logPath = Path.Combine(DesktopPath, "SpawnLog.log");

		private static bool _logToDesktop;

		private static readonly string _dataPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
															 "mods",
															 "ModData");

		public static string CalculatePath(this string fileName)
		{
			return Path.Combine(_dataPath, fileName);
		}

		public static void ToggleLogToDesktop(ArraySegment<string> args)
		{
			_logToDesktop = !_logToDesktop;
			LogMessage(string.Format("Log to desktop: {0}", _logToDesktop));
		}

		public static void LogMessage(string message)
		{
			Debug.Log(message);
			if (!_logToDesktop)
			{
				return;
			}
			var time = DateTime.Now.ToString();
			var output = string.Format("[{0}]: {1}\n", time, message);
			File.AppendAllText(_logPath, output);
		}
	}
}
