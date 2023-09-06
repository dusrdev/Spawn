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
		private static readonly Dictionary<string, ItemID> ItemAliases;

		private static readonly string AliasesPath = "SpawnAliases.csv".CalculatePath();

		static ItemAliasManager()
		{
			ItemAliases = new Dictionary<string, ItemID>(StringComparer.OrdinalIgnoreCase);
			LoadAliases();
		}

		private static void LoadAliases()
		{
			if (!File.Exists(AliasesPath))
			{
				LogMessage($"Aliases file does not exist at path: {AliasesPath}");
				return;
			}
			foreach (var line in File.ReadAllLines(AliasesPath))
			{
				var kv = line.Split(',');
				if (!kv[1].ParseEnum(out ItemID itemId))
				{
					LogMessage($"Failed to parse item id from alias file at line: {line}");
					continue;
				}
				ItemAliases[kv[0]] = itemId;
			}
			if (ItemAliases.Count is 0)
			{
				LogMessage("No item aliases loaded");
				return;
			}
			LogMessage("Loaded item aliases");
		}

		private static void SaveItemAliases()
		{
			var csv = new string[ItemAliases.Count];
			var i = 0;
			foreach (var kv in ItemAliases)
			{
				csv[i++] = string.Join(',', kv.Key, kv.Value);
			}
			File.WriteAllLines(AliasesPath, csv);
		}

		public static bool TryGetAlias(string alias, out ItemID itemID)
		{
			return ItemAliases.TryGetValue(alias, out itemID);
		}

		public static string AddAlias(string alias, ItemID itemID)
		{
			if (itemID != ItemID.None)
			{
				ItemAliases[alias] = itemID;
				SaveItemAliases();
				return $"Added alias `{alias}` for item `{itemID}`";
			}
			// None is deletion
			if (!ItemAliases.ContainsKey(alias))
			{
				return $"Alias `{alias}` does not exist";
			}
			ItemAliases.Remove(alias);
			SaveItemAliases();
			return $"Removed alias `{alias}`";
		}

		public static string ListSavedAliases()
		{
			if (ItemAliases.Count is 0)
			{
				return "No aliases saved...";
			}
			var sb = new StringBuilder();
			foreach (var kv in ItemAliases)
			{
				sb.AppendFormat("'{0}': {1}", kv.Key, kv.Value).AppendLine();
			}
			return sb.ToString();
		}
	}
}