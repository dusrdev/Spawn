using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Enums;
using static SpawnMod.SpawnExtensions;

namespace SpawnMod
{
	public static class ItemAliasManager
	{
		private static readonly Dictionary<string, ItemID> _itemAliases;

		private static readonly string AliasesPath = "SpawnAliases.csv".CalculatePath();

		static ItemAliasManager()
		{
			_itemAliases = new Dictionary<string, ItemID>(StringComparer.OrdinalIgnoreCase);
			LoadAliases();
		}

		private static void LoadAliases()
		{
			if (!File.Exists(AliasesPath))
			{
				LogMessage(string.Format("Aliases file does not exist at path: {0}", AliasesPath));
				return;
			}
			foreach (var line in File.ReadAllLines(AliasesPath))
			{
				var kv = line.Split(',');
				if (!kv[1].ParseEnum(out ItemID itemId))
				{
					LogMessage(string.Format("Failed to parse item id from alias file at line: {0}", line));
					continue;
				}
				_itemAliases[kv[0]] = itemId;
			}
			if (_itemAliases.Count == 0)
			{
				LogMessage("No item aliases loaded");
				return;
			}
			LogMessage("Loaded item aliases");
		}

		private static void SaveItemAliases()
		{
			var csv = new string[_itemAliases.Count];
			var i = 0;
			foreach (var kv in _itemAliases)
			{
				csv[i++] = string.Format("{0},{1}", kv.Key, kv.Value);
			}
			File.WriteAllLines(AliasesPath, csv);
		}

		public static bool TryGetAlias(string alias, out ItemID itemID)
		{
			return _itemAliases.TryGetValue(alias, out itemID);
		}

		public static string AddAlias(string alias, ItemID itemID)
		{
			if (itemID != ItemID.None)
			{
				_itemAliases[alias] = itemID;
				SaveItemAliases();
				return string.Format("Added alias {0} for item {1}", alias, itemID);
			}
			// None is deletion
			if (!_itemAliases.ContainsKey(alias))
			{
				return string.Format("Alias {0} does not exist", alias);
			}
			_itemAliases.Remove(alias);
			SaveItemAliases();
			return string.Format("Removed alias {0}", alias);
		}

		public static string ListSavedAliases()
		{
			if (_itemAliases.Count == 0)
			{
				return "No aliases saved...";
			}
			var sb = new StringBuilder();
			foreach (var kv in _itemAliases)
			{
				sb.AppendFormat("'{0}': {1}", kv.Key, kv.Value).AppendLine();
			}
			return sb.ToString();
		}
	}
}