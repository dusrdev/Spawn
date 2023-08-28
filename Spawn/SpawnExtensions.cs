using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

public static class SpawnExtensions
{
	// parses the item id from the string, disregards case, and verifies result is defined
	public static bool ParseEnum<T>(string arg, out T @enum) where T : struct, Enum
	{
		return Enum.TryParse(arg, true, out @enum) && Enum.IsDefined(typeof(T), @enum);
	}

	// compares two strings disregarding case
	public static bool Equals(string a, string b)
	{
		return string.Compare(a, b, true) == 0;
	}

	public static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

	private static readonly string _logPath = Path.Combine(DesktopPath, "SpawnLog.log");

	private static bool _logToDesktop = false;

	public static void ToggleLogToDesktop()
	{
		_logToDesktop = !_logToDesktop;
		Log(string.Format("Log to desktop: {0}", _logToDesktop));
	}

	public static void Log(string message)
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