using System.Net;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using UnityEngine;
using System.Reflection;

public class Spawn : Mod
{
	private const string SpecialFeaturesHelp = "Special Commands:\n\nlog - turn on logging to desktop file\nunlockNotepad - unlocks the whole notepad\n\nSpecial Weapons:\n\nFirst_Blade - spawns the First Blade\nLucifers_Spear - spawns Lucifer's Spear\n\n";

	public void Start()
	{
		Debug.Log("Mod Spawn has been loaded!");
	}

	private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

	// Exports the help to a text file on the desktop
	private static void ExportItemIds()
	{
		try
		{
			var path = Path.Combine(DesktopPath, "SpawnHelp.txt");
			File.WriteAllText(path, SpecialFeaturesHelp);
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

	// parses the item id from the string, disregards case, and verifies result is defined
	private static bool ParseEnum<T>(string arg, out T @enum) where T : struct, Enum
	{
		return Enum.TryParse(arg, true, out @enum) && Enum.IsDefined(typeof(T), @enum);
	}

	// compares two strings disregarding case
	private static bool Equals(string a, string b)
	{
		return string.Compare(a, b, true) == 0;
	}


	[ConsoleCommand("spawn", "spawn [ID/help/log] [Quantity/rm] [MaxDistance/debug]")]
	public static void Command(string[] args)
	{
		if (args.Length == 0)
		{
			return;
		}

		if (Equals(args[0], "Help"))
		{
			ExportItemIds();
			return;
		}

		if (Equals(args[0], "Log"))
		{
			_logToDesktop = !_logToDesktop;
			return;
		}

		if (Equals(args[0], "UnlockNotepad"))
		{
			UnlockNotepad();
			return;
		}

		if (WeaponMap.TryGetValue(args[0], out Enums.ItemID weapon))
		{
			SpawnSpecialWeapon(args[0], weapon);
			return;
		}

		if (string.IsNullOrWhiteSpace(args[0]))
		{
			Log("Empty ItemId is invalid!");
			return;
		}

		var segment = new ArraySegment<string>(args);
		if (segment.Count > 1 && Equals(args[1], "rm"))
		{
			RemoveItem(segment);
		}
		else
		{
			SpawnItem(segment);
		}
	}

	private static void SpawnItem(ArraySegment<string> args)
	{
		if (!ParseEnum(args[0], out Enums.ItemID itemId))
		{
			Log(string.Format("ItemId `{0}` does not exist, refer to \"spawn help\"", args[0]));
			return;
		}

		var quantity = 1;

		if (args.Count > 1 && !int.TryParse(args[1], out quantity))
		{
			Log(string.Format("Quantity `{0}` is invalid!", args[1]));
			return;
		}

		var manager = ItemsManager.Get();
		var itemInfo = manager.GetInfo(itemId);

		if (itemInfo.m_CanBeAddedToInventory)
		{
			Log(string.Format("Spawning {0} x `{1}` to backpack", quantity, itemId));
			var backpack = InventoryBackpack.Get();
			for (var i = 0; i < quantity; i++)
			{
				var itemInstance = manager.CreateItem(itemId, false);
				backpack.InsertItem(itemInstance, null, null, true, true, true, true, true);
			}
			return;
		}

		Log(string.Format("Spawning at player: {0}", itemId));
		var item = manager.CreateItem(itemId, false);
		var playerTransform = Player.Get().transform;
		item.transform.position = playerTransform.position;
		item.transform.rotation = playerTransform.rotation;
		item.ItemsManagerRegister(true);
	}

	public static void RemoveItem(ArraySegment<string> args)
	{
		if (!ParseEnum(args[0], out Enums.ItemID itemId))
		{
			Log(string.Format("ItemId `{0}` does not exist, refer to \"spawn help\"", args[0]));
			return;
		}

		float maxDistance = 5f;
		bool debug = false;

		if (args.Count > 2)
		{
			if (Equals(args[2], "debug"))
			{
				debug = true;
			}
			else
			{
				var isValid = float.TryParse(args[2], out maxDistance);
				if (!isValid)
				{
					Log(string.Format("Invalid distance: {0}", args[2]));
					return;
				}
			}
		}

		var playerTransform = Player.Get().transform;
		var playerPosition = playerTransform.position;

		var sb = new StringBuilder();
		sb.Append("ItemID: ")
		  .AppendLine(itemId.ToString())
		  .Append("MaxDistance: ")
		  .AppendLine(maxDistance.ToString())
		  .Append("Debug: ")
		  .AppendLine(debug.ToString())
		  .AppendLine()
		  .Append("PlayerPosition: ")
		  .AppendLine(playerPosition.ToString())
		  .AppendLine();

		var items = new Dictionary<int, Item>();
		int index = 1;
		Log(string.Format("Searching for {0} at distance {1}", itemId, maxDistance));
		foreach (var item in Item.s_AllItems)
		{
			if (item.m_Info.m_ID != itemId)
			{
				continue;
			}

			var itemPosition = item.transform.position;
			var distance = Vector3.Distance(playerPosition, itemPosition);

			if (debug)
			{
				sb.Append("ItemPosition: ")
				  .AppendLine(itemPosition.ToString())
				  .Append("Distance: ")
				  .AppendLine(distance.ToString());
			}

			if (distance > maxDistance)
			{
				continue;
			}
			Log(string.Format("Item {0} found at distance {1}", index, distance));
			items[index] = item;
			index++;
		}

		if (index == 1)
		{
			Log("No items found!");
		}

		if (debug)
		{
			var path = Path.Combine(DesktopPath, "rmDebug.txt");
			File.WriteAllText(path, sb.ToString());
		}

		index = 1;

		while (!debug && items.Count > 0)
		{
			var item = items[index];
			items.Remove(index);
			item.m_Info.m_CanBeRemovedFromInventory = true;
			item.m_Info.m_DestroyByItemsManager = true;
			item.m_Info.m_CantDestroy = false;
			ItemsManager.Get().AddItemToDestroy(item);
			index++;
			Log(string.Format("Item {0} removed!", index));
		}
	}

	// Unlocks the whole notepad
	public static void UnlockNotepad()
	{
		var manager = ItemsManager.Get();
		manager.UnlockWholeNotepad();
		Log("Notepad unlocked!");
	}

	// Spawns Special Weapons - Infinite Damage and Durability
	public static void SpawnSpecialWeapon(string weaponName, Enums.ItemID itemId)
	{
		Log(string.Format("Spawning \"{0}\".", weaponName));
		var manager = ItemsManager.Get();
		var item = manager.CreateItem(itemId, false);
		var backpack = InventoryBackpack.Get();
		backpack.InsertItem(item, null, null, true, true, true, true, true);
		var itemInfo = (WeaponInfo)item.m_Info;
		itemInfo.m_DamageSelf = 1E-45f; // Smallest number that is greater than 0
		itemInfo.m_HealthLossPerSec = 0f;
		itemInfo.m_DefaultDamage = float.MaxValue;
		itemInfo.m_HumanDamage = float.MaxValue;
		itemInfo.m_AnimalDamage = float.MaxValue;
		itemInfo.m_PlantDamage = float.MaxValue;
		itemInfo.m_TreeDamage = float.MaxValue;
		itemInfo.m_IronVeinDamage = float.MaxValue;
		itemInfo.m_ThrowDamage = float.MaxValue;
	}

	private static readonly string _logPath = Path.Combine(DesktopPath, "SpawnLog.log");

	private static bool _logToDesktop = false;

	// private static string GetProps(object obj)
	// {
	// 	var props = obj.GetType().GetProperties();
	// 	var sb = new StringBuilder();
	// 	sb.AppendLine();
	// 	sb.AppendLine("Type: " + obj.GetType().Name);
	// 	foreach (var prop in props)
	// 	{
	// 		var value = prop.GetValue(obj);
	// 		if (value == null)
	// 		{
	// 			continue;
	// 		}
	// 		sb.Append(prop.Name)
	// 		  .Append(": ")
	// 		  .AppendLine(value.ToString());
	// 	}
	// 	return sb.ToString();
	// }

	public static void Log(string message)
	{
		Debug.Log(message);
		if (!_logToDesktop)
		{
			return;
		}
		var time = DateTime.Now.ToString();
		var output = string.Format("[{0}]: {1}\n", time, message);
		File.AppendAllText(_logPath, output);
	}

	public void OnModUnload()
	{
		Log("Mod Spawn has been unloaded!");
	}

	private static readonly Dictionary<string, Enums.ItemID> WeaponMap = new Dictionary<string, Enums.ItemID>(StringComparer.OrdinalIgnoreCase) {
		{ "First_Blade", Enums.ItemID.Obsidian_Bone_Blade },
		{ "Lucifers_Spear", Enums.ItemID.Obsidian_Spear }
	};
}