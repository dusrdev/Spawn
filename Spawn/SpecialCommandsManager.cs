using System.Numerics;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using UnityEngine;
using static SpawnExtensions;

public static class SpecialCommandsManager
{
    // Toggles rain on/off
	public static void ToggleRain()
	{
		var manager = RainManager.Get();
		if (manager.IsRain())
		{
			manager.ScenarioStopRain();
			Log("Stopping rain!");
			return;
		}
		manager.ScenarioStartRain();
		Log("Starting rain!");
	}

	// Unlocks the whole notepad
	public static void UnlockNotepad()
	{
		var manager = ItemsManager.Get();
		manager.UnlockWholeNotepad();
		Log("Notepad unlocked!");
	}

	// Increases the backpack weight to 999
	public static void IncreaseBackpackWeight()
	{
		var backpack = InventoryBackpack.Get();
		backpack.m_MaxWeight = 999f;
		Log("Backpack weight increased to 999!");
		Log("To prevent errors, save the game only when the backpack weight is <= 50");
	}

	public static void LogItemInfo(ArraySegment<string> args)
	{
		if (args.Count < 2)
		{
			Log("Teleport requires additional argument: ItemID");
			return;
		}

		if (!ParseEnum(args[1], out Enums.ItemID itemId))
		{
			Log(string.Format("ItemId `{0}` does not exist, refer to \"spawn help\"", args[1]));
			return;
		}

		GetProps(itemId);
	}

	private static void GetProps(Enums.ItemID itemId)
	{
		var manager = ItemsManager.Get();
		var item = manager.CreateItem(itemId, false);
		var backpack = InventoryBackpack.Get();
		backpack.InsertItem(item, null, null, true, true, true, true, true);
		var itemInfo = item.m_Info;
		var props = itemInfo.GetType().GetProperties();
		var sb = new StringBuilder();
		sb.AppendLine();
		sb.AppendLine("Type: " + itemInfo.GetType().Name);
		foreach (var prop in props)
		{
			var value = prop.GetValue(itemInfo);
			if (value == null)
			{
				continue;
			}
			sb.Append(prop.Name)
			  .Append(": ")
			  .AppendLine(value.ToString());
		}
		Log(sb.ToString());
	}
}