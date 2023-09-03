using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Enums;
using SpawnMod;
using UnityEngine;
using static SpawnMod.SpawnExtensions;

public class Spawn : Mod
{
	private static string GetHelpText()
	{
		var sb = new StringBuilder();
		sb.AppendLine("Commands:");
		foreach (var command in Commands.Keys)
		{
			sb.AppendLine(command);
		}
		sb.AppendLine()
			.AppendLine("Special Items:")
			.AppendLine(SpawnAndRemove.GetSpecialItemNames())
			.AppendLine();
		return sb.ToString();
	}

	public void Start()
	{
		LogMessage("Mod Spawn has been loaded!");
	}

	// Exports the help to a text file on the desktop
	private static void ExportHelpText(ArraySegment<string> args)
	{
		try
		{
			var path = Path.Combine(DesktopPath, "SpawnHelp.txt");
			File.WriteAllText(path, GetHelpText());
			File.AppendAllText(path, "LiquidTypes:\n\n");
			var liquidTypes = Enum.GetNames(typeof(LiquidType));
			File.AppendAllLines(path, liquidTypes);
			File.AppendAllText(path, "ItemIds:\n\n");
			var itemIds = Enum.GetNames(typeof(ItemID));
			File.AppendAllLines(path, itemIds);
			LogMessage(string.Format("help exported to: {0}", path));
		}
		catch (Exception e)
		{
			LogMessage(string.Format("Error while exporting item ids: `{0}`\nStackTrace: {1}", e.Message, e.StackTrace));
		}
	}

	[ConsoleCommand("spawn", "Check help for more info")]
	public static void Command(string[] args)
	{
		if (args.Length == 0)
		{
			LogMessage("Spawn command received no arguments...");
			return;
		}

		if (string.IsNullOrWhiteSpace(args[0]))
		{
			LogMessage("Empty ItemId/Command is invalid!");
			return;
		}

		var segment = new ArraySegment<string>(args);

		if (Commands.TryGetValue(args[0], out Action<ArraySegment<string>> action))
		{
			try
			{
				action(segment.Slice(1));
			}
			catch (Exception e)
			{
				LogMessage(string.Format("Error while executing command: `{0}`\nStackTrace: {1}", e.Message, e.StackTrace));
			}
			return;
		}

		LogMessage("Invalid command, check help for more info!");
	}

	public void OnModUnload()
	{
		LogMessage("Mod Spawn has been unloaded!");
	}

	private static readonly Dictionary<string, Action<ArraySegment<string>>> Commands = new Dictionary<string, Action<ArraySegment<string>>>(StringComparer.OrdinalIgnoreCase) {
		{ "Alias", SpawnAndRemove.AddItemAlias },
		{ "CompleteConstructions", SpecialCommands.CompleteConstructions },
		{ "EndlessFires", SpecialCommands.EndlessFires },
		{ "FillLiquid", SpecialCommands.FillLiquid },
		{ "Get", SpawnAndRemove.SpawnItem },
		{ "GetUnityLogPath", SpecialCommands.GetUnityLogPath },
		{ "Help", ExportHelpText },
		{ "IncreaseSkills", SpecialCommands.IncreaseSkills },
		{ "ItemInfo", SpecialCommands.LogItemInfo },
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