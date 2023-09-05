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
		private static readonly Dictionary<string, Vector3> _savedLocations;
		private static readonly string SavedLocationsPath = "SavedLocations.csv".CalculatePath();

		static SavedLocationsManager()
		{
			_savedLocations = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
			LoadSavedLocations();
		}

		private static void LoadSavedLocations()
		{
			if (!File.Exists(SavedLocationsPath))
			{
				LogMessage(string.Format("Saved locations file does not exist at path: {0}", SavedLocationsPath));
				return;
			}
			foreach (var line in File.ReadAllLines(SavedLocationsPath))
			{
				var kv = line.Split(',');
				if (!float.TryParse(kv[1], out float @lat))
				{
					LogMessage("Failed to parse latitude from saved locations file: " + line);
					continue;
				}
				if (!float.TryParse(kv[2], out float @alt))
				{
					LogMessage("Failed to parse altitude from saved locations file: " + line);
					continue;
				}
				if (!float.TryParse(kv[3], out float @long))
				{
					LogMessage("Failed to parse longitude from saved locations file: " + line);
					continue;
				}
				_savedLocations[kv[0]] = new Vector3(@lat, @alt, @long);
			}
			if (_savedLocations.Count == 0)
			{
				LogMessage("No item aliases loaded");
				return;
			}
			LogMessage("Loaded item aliases");
		}

		private static void SaveLocations()
		{
			var csv = new string[_savedLocations.Count];
			var i = 0;
			foreach (var kv in _savedLocations)
			{
				var vector = kv.Value;
				//    lat,alt,long
				csv[i++] = string.Format("{0},{1},{2},{3}", kv.Key, vector.x, vector.y, vector.z);
			}
			File.WriteAllLines(SavedLocationsPath, csv);
		}

		public static bool TryGetLocation(string name, out Vector3 location)
		{
			return _savedLocations.TryGetValue(name, out location);
		}

		public static string AddLocation(string name, Vector3 location)
		{
			if (location != Vector3.zero)
			{
				_savedLocations[name] = location;
				SaveLocations();
				return string.Format("Added location {0} at {1}", name, location);
			}
			// None is deletion
			if (!_savedLocations.ContainsKey(name))
			{
				return string.Format("Location {0} does not exist", name);
			}
			_savedLocations.Remove(name);
			SaveLocations();
			return string.Format("Removed location {0}", name);
		}

		public static string ListSavedLocations()
		{
			if (_savedLocations.Count == 0)
			{
				return "No saved locations found...";
			}
			var sb = new StringBuilder();
			foreach (var kv in _savedLocations)
			{
				sb.AppendFormat("'{0}': {1}", kv.Key, kv.Value).AppendLine();
			}
			return sb.ToString();
		}
	}
}