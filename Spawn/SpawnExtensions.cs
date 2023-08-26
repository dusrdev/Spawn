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
}