using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Enums;

using SpawnMod;

using UnityEngine;

using static SpawnMod.SpawnExtensions;

public class Spawn : Mod {
    private static string GetHelpText() {
        var sb = new StringBuilder();
        sb.AppendLine("Commands:");
        foreach (var command in Commands.Keys) {
            sb.AppendLine(command);
        }
        sb.AppendLine()
            .AppendLine("Special Items:")
            .AppendLine(SpawnAndRemove.GetSpecialItemNames())
            .AppendLine();
        return sb.ToString();
    }

    private readonly Action[] _inventoryActions = new Action[] {
        SpawnAndRemove.RestoreSpecialItemsFromMemory,
        SpecialCommands.RestoreLighterBackpack,
    };

    private Coroutine _inventoryCoroutine;

    private IEnumerator InventoryCoroutine() {
        bool inventoryLoaded = false;
        while (true) {
            if (InventoryBackpack.Get() == null) {
                inventoryLoaded = false;
            } else if (!inventoryLoaded) {
                inventoryLoaded = true;
                foreach (var action in _inventoryActions) {
                    action();
                }
            }
            yield return new WaitForSeconds(10f);
        }
    }

    public void Start() {
        LogMessage("Mod Spawn has been loaded!");
        RestoreLogToggle();
        _inventoryCoroutine = StartCoroutine(InventoryCoroutine());
    }

    // Exports the help to a text file on the desktop
    private static void ExportHelpText(ArraySegment<string> args) {
        try {
            var path = Path.Combine(DesktopPath, "SpawnHelp.txt");
            File.WriteAllText(path, GetHelpText());
            File.AppendAllText(path, "LiquidTypes:\n\n");
            var liquidTypes = Enum.GetNames(typeof(LiquidType));
            File.AppendAllLines(path, liquidTypes);
            File.AppendAllText(path, "ItemIds:\n\n");
            var itemIds = Enum.GetNames(typeof(ItemID));
            File.AppendAllLines(path, itemIds);
            LogMessage($"help exported to: {path}");
        } catch (Exception e) {
            LogMessage($"Error while exporting item ids: `{e.Message}`\nStackTrace: {e.StackTrace}");
        }
    }

    [ConsoleCommand("spawn", "Check help for more info")]
    public static void Command(string[] args) {
        if (args.Length is 0) {
            LogMessage("Spawn command received no arguments...");
            return;
        }

        if (string.IsNullOrWhiteSpace(args[0])) {
            LogMessage("Empty ItemId/Command is invalid!");
            return;
        }

        var segment = new ArraySegment<string>(args);

        if (Commands.TryGetValue(args[0], out Action<ArraySegment<string>> action)) {
            try {
                if (segment.Count > 1) {
                    segment = segment.Slice(1);
                }
                action(segment);
            } catch (Exception e) {
                LogMessage($"Error while executing command: `{e.Message}`\nStackTrace: {e.StackTrace}");
            }
            return;
        }

        LogMessage("Invalid command, check help for more info!");
    }

    public void OnModUnload() {
        if (_inventoryCoroutine != null) {
            StopCoroutine(_inventoryCoroutine);
        }
        LogMessage("Mod Spawn has been unloaded!");
    }

    private static readonly Dictionary<string, Action<ArraySegment<string>>> Commands = new Dictionary<string, Action<ArraySegment<string>>>(StringComparer.OrdinalIgnoreCase)
 {
        { "Alias", SpawnAndRemove.AddItemAlias },
        { "CompleteConstructions", SpecialCommands.CompleteConstructions },
        { "EndlessFires", SpecialCommands.EndlessFires },
        { "FillLiquid", SpecialCommands.FillLiquid },
        { "Get", SpawnAndRemove.SpawnItem },
        { "GetUnityLogPath", SpecialCommands.GetUnityLogPath },
        { "Help", ExportHelpText },
        { "IncreaseSkills", SpecialCommands.IncreaseSkills },
        { "ItemInfo", SpecialCommands.LogItemInfo },
        { "LighterBackpack", SpecialCommands.LighterBackpack },
        { "ProgressTime", SpecialCommands.TimeProgress },
        { "Rain", SpecialCommands.ToggleRain },
        { "Remove", SpawnAndRemove.RemoveItem },
        { "RemoveConstructions", SpawnAndRemove.RemoveConstructions },
        { "RestoreSpecialItems", SpawnAndRemove.RestoreSpecialItems },
        { "SaveLocation", Teleportation.AddSavedLocation },
        { "SetTime", SpecialCommands.SetDayTime },
        { "Teleport", Teleportation.Teleport },
        { "ToggleLog", ToggleLogToDesktop },
        { "UnlockMaps", SpecialCommands.UnlockMaps },
        { "UnlockNotepad", SpecialCommands.UnlockNotepad },
 };
}