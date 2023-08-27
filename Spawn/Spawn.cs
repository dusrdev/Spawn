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
			var count = 1;
			if (args.Count > 1 && !int.TryParse(args[1], out count))
			{
				Log(string.Format("Quantity `{0}` is invalid!", args[1]));
				return;
			}
			for (var i = 0; i < count; i++)
			{
				itemSpawnFunction.Invoke();
			}
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
			Log(string.Format("Item {0} removed!", index));
			index++;
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

	// Gives the player all diseases and most wounds
	// Used for the "Mr... I don't feel so good" achievement
	public static void GetThePlague() {
		var diseasesModule = PlayerDiseasesModule.Get();
		diseasesModule.RequestDisease(Enums.ConsumeEffect.Fever, 0f, 1, 1);
		diseasesModule.RequestDisease(Enums.ConsumeEffect.FoodPoisoning, 1f, 2, 1);
		diseasesModule.RequestDisease(Enums.ConsumeEffect.ParasiteSickness, 1f, 2, 1);
		diseasesModule.RequestDisease(Enums.ConsumeEffect.DirtSickness, 0f, 2, 1);
		diseasesModule.RequestDisease(Enums.ConsumeEffect.Insomnia, 0f, 2, 1);
		var bodyInspectionController = BodyInspectionController.Get();
		var injuryModule = PlayerInjuryModule.Get();
		// Leech
		var leechPlace = Enums.InjuryPlace.LHand;
		var leechInjuryType = Enums.InjuryType.Leech;
		var leechSlot = bodyInspectionController.GetFreeWoundSlot(leechPlace, leechInjuryType);
		injuryModule.AddInjury(leechInjuryType, leechPlace, leechSlot, Enums.InjuryState.Closed);
		var leechSlot2 = bodyInspectionController.GetFreeWoundSlot(leechPlace, leechInjuryType);
		injuryModule.AddInjury(leechInjuryType, leechPlace, leechSlot2, Enums.InjuryState.Closed);
		// Laceration
		var lacerationInjuryType = Enums.InjuryType.Laceration;
		var lacerationSlot = bodyInspectionController.GetFreeWoundSlot(leechPlace, lacerationInjuryType);
		injuryModule.AddInjury(lacerationInjuryType, leechPlace, lacerationSlot, Enums.InjuryState.Bleeding);
		// Worm
		var wormPlace = Enums.InjuryPlace.RHand;
		var wormInjuryType = Enums.InjuryType.Worm;
		var wormSlot = bodyInspectionController.GetFreeWoundSlot(wormPlace, wormInjuryType);
		injuryModule.AddInjury(wormInjuryType, wormPlace, wormSlot, Enums.InjuryState.WormInside);
		// Rash
		var rashPlace = Enums.InjuryPlace.LLeg;
		var rashInjuryType = Enums.InjuryType.Rash;
		var rashSlot = bodyInspectionController.GetFreeWoundSlot(rashPlace, rashInjuryType);
		injuryModule.AddInjury(rashInjuryType, rashPlace, rashSlot, Enums.InjuryState.Closed);
		// Poison
		var poisonPlace = Enums.InjuryPlace.RLeg;
		var poisonInjuryType = Enums.InjuryType.SnakeBite;
		var poisonSlot = bodyInspectionController.GetFreeWoundSlot(poisonPlace, poisonInjuryType);
		injuryModule.AddInjury(poisonInjuryType, poisonPlace, poisonSlot, Enums.InjuryState.Open, 3);
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

	private static void RestoreSpecialItems()
	{
		var backpack = InventoryBackpack.Get();
		int i = 0;

		foreach (var item in backpack.m_Items)
		{
			var itemInfo = item.m_Info;
			TryRestoreSpecialItemProperties(itemInfo);
		}
		TryRestoreSpecialItemProperties(backpack.m_EquippedItem.m_Info);
	}

	private static void TryRestoreSpecialItemProperties(ItemInfo itemInfo)
	{
		if (!SpecialItemIds.Contains(itemInfo.m_ID))
		{ // Regular item
			return;
		}
		var type = itemInfo.GetType();
		Log(string.Format("Restoring \"{0}\"", itemInfo.m_ID));
		if (itemInfo.m_ID == Enums.ItemID.Stone)
		{
			ModifyItemProperties(type, itemInfo, false);
			return;
		}
		ModifyItemProperties(type, itemInfo);
	}

	private static void SpawnItemAndModify<T>(Enums.ItemID itemId, bool differentTypeIsError = true) where T : ItemInfo
	{
		Log(string.Format("Spawning modified \"{0}\".", itemId));
		var manager = ItemsManager.Get();
		var item = manager.CreateItem(itemId, false);
		var backpack = InventoryBackpack.Get();
		backpack.InsertItem(item, null, null, true, true, true, true, true);
		ModifyItemProperties(typeof(T), item.m_Info, differentTypeIsError);
	}

	// Modifies weapons - Infinite Damage and Durability
	public static void ModifyWeapon(WeaponInfo itemInfo)
	{
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

	// Modifies an item with max throw force and damage
	public static void ModifyThrowable(ItemInfo itemInfo)
	{
		itemInfo.m_Mass = 0.1f;
		itemInfo.m_ThrowForce = 480f;
		itemInfo.m_ThrowDamage = float.MaxValue;
	}

	// Modifies containers - Capacity = 1000
	public static void ModifyContainer(LiquidContainerInfo itemInfo)
	{
		itemInfo.m_Capacity = 1000f;
		itemInfo.m_Mass = 0.1f;
	}

	// Modifies the stats of a firestarter
	public static void ModifyFireStarter(ItemToolInfo itemInfo)
	{
		itemInfo.m_Mass = 0.1f;
		itemInfo.m_HealthLossPerSec = 1E-45f;
		itemInfo.m_MakeFireStaminaConsumptionMul = 0f;
		itemInfo.m_MakeFireDuration = 0.1f;
	}

	// Modifies foods - All stats
	public static void ModifyFood(FoodInfo itemInfo)
	{
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

	private static void ModifyItemProperties(Type type, ItemInfo itemInfo, bool differentIsError = true)
	{
		if (type == typeof(WeaponInfo) || type == typeof(SpearInfo))
		{
			ModifyWeapon((WeaponInfo)itemInfo);
		}
		else if (type == typeof(LiquidContainerInfo) || type == typeof(BowlInfo))
		{
			ModifyContainer((LiquidContainerInfo)itemInfo);
		}
		else if (type == typeof(FoodInfo))
		{
			ModifyFood((FoodInfo)itemInfo);
		}
		else if (type == typeof(ItemToolInfo))
		{
			ModifyFireStarter((ItemToolInfo)itemInfo);
		}
		else if (!differentIsError)
		{
			ModifyThrowable(itemInfo);
		}
		else
		{
			Log(string.Format("Modification of '{0}' and type '{1}' is not supported!", itemInfo.m_ID, type.Name));
			return;
		}
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
		{ "ToggleLog", args => _logToDesktop = !_logToDesktop },
		{ "Rain", args => ToggleRain() },
		{ "RestoreSpecialItems", args => RestoreSpecialItems() },
		{ "UnlockNotepad", args => UnlockNotepad() },
		{ "IncreaseBackpackWeight", args => IncreaseBackpackWeight() },
		{ "Teleport", args => Teleport(args) },
		{ "Alias", args => AddItemAlias(args) },
		{ "ItemInfo", args => LogItemInfo(args) },
		{ "GetThePlague", args => GetThePlague() },
	};

	private static readonly Dictionary<string, Action> SpecialItemMap = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase) {
		{ "Knife", () => SpawnItemAndModify<WeaponInfo>(Enums.ItemID.metal_blade_weapon) },
		{ "First_Blade", () => SpawnItemAndModify<WeaponInfo>(Enums.ItemID.Obsidian_Bone_Blade) },
		{ "Lucifers_Spear", () => SpawnItemAndModify<WeaponInfo>(Enums.ItemID.Obsidian_Spear) },
		{ "Super_Bidon", () => SpawnItemAndModify<LiquidContainerInfo>(Enums.ItemID.Bidon) },
		{ "Super_Pot", () => SpawnItemAndModify<BowlInfo>(Enums.ItemID.Pot) },
		{ "Magic_Pills", () => SpawnItemAndModify<FoodInfo>(Enums.ItemID.Painkillers) },
		{ "Kryptonite", () => SpawnItemAndModify<ItemInfo>(Enums.ItemID.Stone, false) },
		{ "Lighter", () => SpawnItemAndModify<ItemToolInfo>(Enums.ItemID.Rubing_Wood) },
	};

	private static readonly HashSet<Enums.ItemID> SpecialItemIds = new HashSet<Enums.ItemID> {
		Enums.ItemID.metal_blade_weapon,
		Enums.ItemID.Obsidian_Bone_Blade,
		Enums.ItemID.Obsidian_Spear,
		Enums.ItemID.Bidon,
		Enums.ItemID.Pot,
		Enums.ItemID.Painkillers,
		Enums.ItemID.Stone,
		Enums.ItemID.Rubing_Wood,
	};
}