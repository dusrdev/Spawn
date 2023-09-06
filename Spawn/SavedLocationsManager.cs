using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using static SpawnMod.SpawnExtensions;

namespace SpawnMod
{
	public static class SavedLocationsManager
	{
		private static readonly Dictionary<string, Vector3> SavedLocations;
		private static readonly string SavedLocationsPath = "SavedLocations.csv".CalculatePath();

		static SavedLocationsManager()
		{
			SavedLocations = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
			LoadSavedLocations();
		}

		private static void LoadSavedLocations()
		{
			if (!File.Exists(SavedLocationsPath))
			{
				LogMessage($"Saved locations file does not exist at path: {SavedLocationsPath}");
				return;
			}
			foreach (var line in File.ReadAllLines(SavedLocationsPath))
			{
				var kv = line.Split(',');
				if (!float.TryParse(kv[1], out float @lat))
				{
					LogMessage($"Failed to parse latitude from saved locations file: line {line}");
					continue;
				}
				if (!float.TryParse(kv[2], out float @alt))
				{
					LogMessage($"Failed to parse altitude from saved locations file: line {line}");
					continue;
				}
				if (!float.TryParse(kv[3], out float @long))
				{
					LogMessage($"Failed to parse longitude from saved locations file: line {line}");
					continue;
				}
				SavedLocations[kv[0]] = new Vector3(@lat, @alt, @long);
			}
			if (SavedLocations.Count is 0)
			{
				LogMessage("No item aliases loaded");
				return;
			}
			LogMessage("Loaded item aliases");
		}

		private static void SaveLocations()
		{
			var csv = new string[SavedLocations.Count];
			var i = 0;
			foreach (var kv in SavedLocations)
			{
				var vector = kv.Value;
				//    lat,alt,long
				csv[i++] = string.Join(',', kv.Key, vector.x, vector.y, vector.z);
			}
			File.WriteAllLines(SavedLocationsPath, csv);
		}

		public static bool TryGetLocation(string name, out Vector3 location)
		{
			return SavedLocations.TryGetValue(name, out location);
		}

		public static string AddLocation(string name, Vector3 location)
		{
			if (location != Vector3.zero)
			{
				SavedLocations[name] = location;
				SaveLocations();
				return $"Added location `{name}` at {location}";
			}
			// None is deletion
			if (!SavedLocations.ContainsKey(name))
			{
				return $"Location `{name}` does not exist";
			}
			SavedLocations.Remove(name);
			SaveLocations();
			return $"Removed location `{name}`";
		}

		public static string ListSavedLocations()
		{
			if (SavedLocations.Count is 0)
			{
				return "No saved locations found...";
			}
			var sb = new StringBuilder();
			foreach (var kv in SavedLocations)
			{
				sb.AppendFormat("'{0}': {1}", kv.Key, kv.Value).AppendLine();
			}
			return sb.ToString();
		}
	}
}