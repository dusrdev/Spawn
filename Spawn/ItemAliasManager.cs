using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Enums;

using static SpawnMod.SpawnExtensions;

namespace SpawnMod {
    public static class ItemAliasManager {
        private static readonly Dictionary<string, ItemID> ItemAliases;

        private static readonly string AliasesPath = "SpawnAliases.csv".CalculatePath();

        static ItemAliasManager() {
            ItemAliases = new Dictionary<string, ItemID>(StringComparer.OrdinalIgnoreCase);
            LoadAliases();
        }

        private static void LoadAliases() {
            if (!File.Exists(AliasesPath)) {
                LogMessage($"Aliases file does not exist at path: {AliasesPath}");
                return;
            }
            foreach (var line in File.ReadAllLines(AliasesPath)) {
                var kv = line.Split(',');
                if (!kv[1].ParseEnum(out ItemID itemId)) {
                    LogMessage($"Failed to parse item id from alias file at line: {line}");
                    continue;
                }
                ItemAliases[kv[0]] = itemId;
            }
            if (ItemAliases.Count is 0) {
                LogMessage("No item aliases loaded");
                return;
            }
            LogMessage("Loaded item aliases");
        }

        private static void SaveItemAliases() {
            using (var file = File.Open(AliasesPath, FileMode.Create)) {
                using (var writer = new StreamWriter(file)) {
                    foreach (var kv in ItemAliases) {
                        var id = ((int)kv.Value).ToString();
                        var line = string.Concat(kv.Key, ",", id);
                        writer.WriteLine(line);
                    }
                }
            }
        }

        public static bool TryGetAlias(string alias, out ItemID itemID) {
            return ItemAliases.TryGetValue(alias, out itemID);
        }

        public static string AddAlias(string alias, ItemID itemID) {
            if (itemID != ItemID.None) {
                ItemAliases[alias] = itemID;
                SaveItemAliases();
                return $"Added alias `{alias}` for item `{itemID}`";
            }
            // None is deletion
            if (!ItemAliases.ContainsKey(alias)) {
                return $"Alias `{alias}` does not exist";
            }
            ItemAliases.Remove(alias);
            SaveItemAliases();
            return $"Removed alias `{alias}`";
        }

        private static readonly StringBuilder Builder = new StringBuilder();

        public static string ListSavedAliases() {
            if (ItemAliases.Count is 0) {
                return "No aliases saved...";
            }
            Builder.Clear();
            foreach (var kv in ItemAliases) {
                Builder.Append(kv.Key)
                  .Append(": ")
                  .Append(kv.Value)
                  .AppendLine();
            }
            return Builder.ToString();
        }
    }
}