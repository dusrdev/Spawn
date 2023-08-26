﻿using System.Net;
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

	private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

	// Exports the help to a text file on the desktop
	public static void ExportHelpText()
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
	public static void Command(string[] args)
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
			RemoveItem(segment);
		}
		else
		{
			SpawnItem(segment);
		}
	}

	private static void SpawnItem(ArraySegment<string> args)
	{
		if (SpecialItemMap.TryGetValue(args[0], out Action itemSpawnFunction))
		{
			itemSpawnFunction.Invoke();
			return;
		}

		if (ItemAliasManager.TryGetNickname(args[0], out Enums.ItemID aliasedItemId))
		{
			SpawnItemInternal(aliasedItemId, args);
			return;
		}

		if (!ParseEnum(args[0], out Enums.ItemID itemId))
		{
			Log(string.Format("ItemId `{0}` does not exist, refer to \"spawn help\"", args[0]));
			return;
		}

		SpawnItemInternal(itemId, args);
	}

	private static void SpawnItemInternal(Enums.ItemID itemId, ArraySegment<string> args)
	{
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

	// Teleports the player to the specified coordinates
	public static void Teleport(ArraySegment<string> args)
	{
		if (args.Count < 3)
		{
			Log("Teleport requires 2 additional arguments: latitude, longitude");
			return;
		}

		if (!float.TryParse(args[1], out float latitude))
		{
			Log(string.Format("Invalid argument for 'latitude': {0}", args[1]));
			return;
		}

		if (!float.TryParse(args[2], out float longitude))
		{
			Log(string.Format("Invalid argument for 'longitude': {0}", args[2]));
			return;
		}

		// get player position
		var player = Player.Get();

		// calculate new position
		const float latModifier = 40.8185f;
		const float longModifier = 36.4861f;
		float positionLat = (latitude - 57f) * latModifier * -1f + latModifier * 0.5f;
		float positionLong = (longitude - 64f) * longModifier * -1f + longModifier * 0.5f;
		var newPos = new Vector3(positionLat, 5f, positionLong);

		// Teleport player
		var rotation = player.transform.rotation;
		player.TeleportTo(newPos, rotation, false);

		Log(string.Format("Teleported to: {0}", newPos));
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

	public static void AddItemAlias(ArraySegment<string> args)
	{
		if (args.Count < 2)
		{
			Log("Teleport requires additional arguments: [alias] [itemId=emptyToRemove]");
			return;
		}

		if (args.Count == 2)
		{
			Log(ItemAliasManager.AddNickname(args[1], Enums.ItemID.None));
			return;
		}

		if (!ParseEnum(args[2], out Enums.ItemID itemId))
		{
			Log(string.Format("ItemId `{0}` does not exist, refer to \"spawn help\"", args[2]));
			return;
		}

		Log(ItemAliasManager.AddNickname(args[1], itemId));
	}

	private static ItemInfo SpawnItemAndGetInfo(Enums.ItemID itemId)
	{
		Log(string.Format("Spawning modified \"{0}\".", itemId));
		var manager = ItemsManager.Get();
		var item = manager.CreateItem(itemId, false);
		var backpack = InventoryBackpack.Get();
		backpack.InsertItem(item, null, null, true, true, true, true, true);
		return item.m_Info;
	}

	// Spawns Supers Weapons - Infinite Damage and Durability
	public static void SpawnSuperWeapon(Enums.ItemID itemId)
	{
		var itemInfo = (WeaponInfo)SpawnItemAndGetInfo(itemId);
		itemInfo.m_Mass = 0.1f;
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

	// Spawn a special stone with minimal footprint and maximum throwing range and damage
	public static void SpawnKryptonite(Enums.ItemID itemId)
	{
		var itemInfo = SpawnItemAndGetInfo(itemId);
		itemInfo.m_Mass = 0.1f;
		itemInfo.m_ThrowForce = 480f;
		itemInfo.m_ThrowDamage = float.MaxValue;
	}

	// Spawn a special torch with infinite burning duration
	public static void SpawnEternalTorch(Enums.ItemID itemId)
	{
		var itemInfo = (TorchInfo)SpawnItemAndGetInfo(itemId);
		itemInfo.m_Mass = 0.1f;
		itemInfo.m_BurningDurationInMinutes = float.MaxValue;
		itemInfo.m_DamageWhenBurning = 1E-45f;
		itemInfo.m_HealthLossPerSec = 1E-45f;
	}

	// Spawns Supers Containers - Capacity = 1000
	public static void SpawnSuperContainer(Enums.ItemID itemId)
	{
		var itemInfo = (LiquidContainerInfo)SpawnItemAndGetInfo(itemId);
		itemInfo.m_Capacity = 1000f;
		itemInfo.m_Mass = 0.1f;
	}

	// Spawns a FireStarter that's better than a lighter
	public static void SpawnSuperFireStarter(Enums.ItemID itemId)
	{
		var itemInfo = (ItemToolInfo)SpawnItemAndGetInfo(itemId);
		itemInfo.m_Mass = 0.1f;
		itemInfo.m_HealthLossPerSec = 1E-45f;
		itemInfo.m_MakeFireStaminaConsumptionMul = 0f;
		itemInfo.m_MakeFireDuration = 0.1f;
	}

	// Spawns Supers Food - All stats
	public static void SpawnSuperFood(Enums.ItemID itemId)
	{
		var itemInfo = (FoodInfo)SpawnItemAndGetInfo(itemId);
		itemInfo.m_Mass = 0.1f;
		itemInfo.m_SpoilTime = -1f;
		itemInfo.m_TroughFood = 100f;
		itemInfo.m_Fat = 100f;
		itemInfo.m_Carbohydrates = 100f;
		itemInfo.m_Proteins = 100f;
		itemInfo.m_Water = 100f;
		itemInfo.m_AddEnergy = 100f;
		itemInfo.m_SanityChange = 100;
		itemInfo.m_ConsumeEffect = Enums.ConsumeEffect.Fever;
		itemInfo.m_ConsumeEffectChance = 1f;
		itemInfo.m_ConsumeEffectDelay = 0f;
		itemInfo.m_ConsumeEffectLevel = -15;
		itemInfo.m_PoisonDebuff = 15;
		itemInfo.m_MakeFireDuration = 100f;
	}

	private static readonly string _logPath = Path.Combine(DesktopPath, "SpawnLog.log");

	private static bool _logToDesktop = false;

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

	private static readonly Dictionary<string, Action<ArraySegment<string>>> SpecialCommands = new Dictionary<string, Action<ArraySegment<string>>>(StringComparer.OrdinalIgnoreCase) {
		{ "Help", args => ExportHelpText() },
		{ "Log", args => _logToDesktop = !_logToDesktop },
		{ "Rain", args => ToggleRain() },
		{ "UnlockNotepad", args => UnlockNotepad() },
		{ "IncreaseBackpackWeight", args => IncreaseBackpackWeight() },
		{ "Teleport", args => Teleport(args) },
		{ "Alias", args => AddItemAlias(args) },
		{ "ItemInfo", args => LogItemInfo(args) },
	};

	private static readonly Dictionary<string, Action> SpecialItemMap = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase) {
		{ "First_Blade", () => SpawnSuperWeapon(Enums.ItemID.Obsidian_Bone_Blade) },
		{ "Lucifers_Spear", () => SpawnSuperWeapon(Enums.ItemID.Obsidian_Spear) },
		{ "Super_Bidon", () => SpawnSuperContainer(Enums.ItemID.clay_bidon) },
		{ "Super_Pot", () => SpawnSuperContainer(Enums.ItemID.clay_bowl_small) },
		{ "Magic_Pills", () => SpawnSuperFood(Enums.ItemID.Painkillers) },
		{ "Kryptonite", () => SpawnKryptonite(Enums.ItemID.Stone) },
		{ "Eternal_Torch", () => SpawnEternalTorch(Enums.ItemID.Torch) },
		{ "Lighter", () => SpawnSuperFireStarter(Enums.ItemID.Rubing_Wood) },
	};

	private static string ClearName(string name)
	{
		return name.Replace("_", " ");
	}
}