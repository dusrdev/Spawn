using System.Net;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using UnityEngine;
using static SpawnExtensions;

public static class ItemAliasManager
{
    private static Dictionary<string, Enums.ItemID> _itemAliases;
    private static readonly string BaseFolder = Application.dataPath;
    private static readonly string AliasesPath = Path.Combine(BaseFolder, "SpawnAliases.csv");

    static ItemAliasManager()
    {
        _itemAliases = new Dictionary<string, Enums.ItemID>(StringComparer.OrdinalIgnoreCase);
        LoadAliases();
    }

    private static void LoadAliases()
    {
        if (!File.Exists(AliasesPath))
        {
            Spawn.Log("Aliases file does not exist - loading aborted");
            return;
        }
        var csv = File.ReadAllLines(AliasesPath);
        foreach (var line in csv)
        {
            var kv = line.Split(',');
            if (!ParseEnum(kv[1], out Enums.ItemID itemId))
            {
                Spawn.Log("Failed to parse item id from alias file: " + line);
                continue;
            }
            _itemAliases[kv[0]] = itemId;
        }
        if (_itemAliases.Count == 0)
        {
            Spawn.Log("No item aliases loaded");
            return;
        }
        Spawn.Log("Loaded item aliases");
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

    public static bool TryGetAlias(string alias, out Enums.ItemID itemID)
    {
        return _itemAliases.TryGetValue(alias, out itemID);
    }

    public static string AddAlias(string alias, Enums.ItemID itemID)
    {
        if (itemID != Enums.ItemID.None)
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
            sb.AppendLine(string.Format("'{0}': {1}", kv.Key, kv.Value));
        }
        return sb.ToString();
    }
}