using System.Numerics;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using UnityEngine;
using System.Reflection;
using static SpawnExtensions;

public class Spawn : Mod
{
	private static string GetHelpText()
	{
		var sb = new StringBuilder();
		sb.AppendLine("Special Commands:");
		foreach (var command in SpecialCommands.Keys)
		{
			sb.AppendLine(command);
		}
		sb.AppendLine();
		sb.AppendLine("Special Items:");
		foreach (var item in SpecialItemMap.Keys)
		{
			sb.AppendLine(item);
		}
		sb.AppendLine();
		return sb.ToString();
	}

	public void Start()
	{
		Debug.Log("Mod Spawn has been loaded!");
	}

	// Exports the help to a text file on the desktop
	private static void ExportHelpText()
	{
		try
		{
			var path = Path.Combine(DesktopPath, "SpawnHelp.txt");
			File.WriteAllText(path, GetHelpText());
			File.AppendAllText(path, "ItemIds:\n\n");
			var itemIds = Enum.GetNames(typeof(Enums.ItemID));
			File.AppendAllLines(path, itemIds);
			Log(string.Format("help exported to: {0}", path));
		}
		catch (Exception e)
		{
			Log(string.Format("Error while exporting item ids: `{0}`\nStackTrace: {1}", e.Message, e.StackTrace));
		}
	}


	[ConsoleCommand("spawn", "spawn [ID/help/log] [Quantity/rm] [MaxDistance/debug]")]
	private static void Command(string[] args)
	{
		if (args.Length == 0)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(args[0]))
		{
			Log("Empty ItemId is invalid!");
			return;
		}

		var segment = new ArraySegment<string>(args);

		if (SpecialCommands.TryGetValue(args[0], out Action<ArraySegment<string>> action))
		{
			action(segment);
			return;
		}

		if (segment.Count > 1 && Equals(args[1], "rm"))
		{
			ItemsSpawnManager.RemoveItem(segment);
		}
		else
		{
			ItemsSpawnManager.SpawnItem(segment);
		}
	}

	public void OnModUnload()
	{
		Log("Mod Spawn has been unloaded!");
	}

	private static readonly Dictionary<string, Action<ArraySegment<string>>> SpecialCommands = new Dictionary<string, Action<ArraySegment<string>>>(StringComparer.OrdinalIgnoreCase) {
		{ "Help", args => ExportHelpText() },
		{ "ToggleLog", args => ToggleLogToDesktop() },
		{ "Rain", args => SpecialCommandsManager.ToggleRain() },
		{ "RestoreSpecialItems", args => ItemsSpawnManager.RestoreSpecialItems() },
		{ "UnlockNotepad", args => SpecialCommandsManager.UnlockNotepad() },
		{ "IncreaseBackpackWeight", args => SpecialCommandsManager.IncreaseBackpackWeight() },
		{ "Teleport", args => TeleportationManager.Teleport(args) },
		{ "Alias", args => ItemsSpawnManager.AddItemAlias(args) },
		{ "SaveLocation", args => TeleportationManager.AddSavedLocation(args) },
		{ "ItemInfo", args => SpecialCommandsManager.LogItemInfo(args) },
	};
}