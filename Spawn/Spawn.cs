using System.Net;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using UnityEngine;
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
		sb.AppendLine()
			.AppendLine("Special Items:")
			.AppendLine(GetSpecialItemNames())
			.AppendLine();
		return sb.ToString();
	}

	public void Start()
	{
		LogMessage("Mod Spawn has been loaded!");
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

	public void OnModUnload()
	{
		LogMessage("Mod Spawn has been unloaded!");
	}

	private static readonly Dictionary<string, Action<ArraySegment<string>>> SpecialCommands = new Dictionary<string, Action<ArraySegment<string>>>(StringComparer.OrdinalIgnoreCase) {
		{ "Help", args => ExportHelpText() },
		{ "ToggleLog", args => ToggleLogToDesktop() },
		{ "Rain", args => ToggleRain() },
		{ "RestoreSpecialItems", args => RestoreSpecialItems() },
		{ "UnlockNotepad", args => UnlockNotepad() },
		{ "IncreaseBackpackWeight", args => IncreaseBackpackWeight() },
		{ "Teleport", args => Teleport(args.Slice(1)) },
		{ "Alias", args => AddItemAlias(args.Slice(1)) },
		{ "SaveLocation", args => AddSavedLocation(args.Slice(1)) },
		{ "ItemInfo", args => LogItemInfo(args.Slice(1)) },
		{ "SetTime", args => SetDayTime(args.Slice(1)) },
		{ "ProgressTime", args => TimeProgress(args.Slice(1)) },
		{ "IncreaseSkills", args => IncreaseSkills(args.Slice(1)) },
	};

	#region Spawn and Remove
	public static void SpawnItem(ArraySegment<string> args)
	{
		if (SpecialItemMap.TryGetValue(args[0], out Action itemSpawnFunction))
		{
			var count = 1;
			if (args.Count > 1 && !int.TryParse(args[1], out count))
			{
				LogMessage(string.Format("Quantity `{0}` is invalid!", args[1]));
				return;
			}
			for (var i = 0; i < count; i++)
			{
				itemSpawnFunction.Invoke();
			}
			return;
		}

		if (ItemAliasManager.TryGetAlias(args[0], out Enums.ItemID aliasedItemId))
		{
			SpawnItemInternal(aliasedItemId, args);
			return;
		}

		if (!ParseEnum(args[0], out Enums.ItemID itemId))
		{
			LogMessage(string.Format("ItemId `{0}` does not exist, refer to \"spawn help\"", args[0]));
			return;
		}

		SpawnItemInternal(itemId, args);
	}

	private static void SpawnItemInternal(Enums.ItemID itemId, ArraySegment<string> args)
	{
		var quantity = 1;

		if (args.Count > 1 && !int.TryParse(args[1], out quantity))
		{
			LogMessage(string.Format("Quantity `{0}` is invalid!", args[1]));
			return;
		}

		var manager = ItemsManager.Get();
		var itemInfo = manager.GetInfo(itemId);

		if (itemInfo.m_CanBeAddedToInventory)
		{
			LogMessage(string.Format("Spawning {0} x `{1}` to backpack", quantity, itemId));
			var backpack = InventoryBackpack.Get();
			for (var i = 0; i < quantity; i++)
			{
				var itemInstance = manager.CreateItem(itemId, false);
				backpack.InsertItem(itemInstance, null, null, true, true, true, true, true);
			}
			return;
		}

		LogMessage(string.Format("Spawning at player: {0}", itemId));
		var item = manager.CreateItem(itemId, false);
		var playerTransform = Player.Get().transform;
		item.transform.position = playerTransform.position;
		item.transform.rotation = playerTransform.rotation;
		item.ItemsManagerRegister(true);
	}

	private static void SpawnItemAndModify<T>(Enums.ItemID itemId, bool differentTypeIsError = true) where T : ItemInfo
	{
		LogMessage(string.Format("Spawning modified \"{0}\".", itemId));
		var manager = ItemsManager.Get();
		var item = manager.CreateItem(itemId, false);
		var backpack = InventoryBackpack.Get();
		backpack.InsertItem(item, null, null, true, true, true, true, true);
		ModifyItemProperties(typeof(T), item.m_Info, differentTypeIsError);
	}

	public static void RemoveItem(ArraySegment<string> args)
	{
		var itemId = Enums.ItemID.None;
		var isAlias = ItemAliasManager.TryGetAlias(args[0], out itemId);
		if (!isAlias && !ParseEnum(args[0], out itemId))
		{
			LogMessage(string.Format("ItemId `{0}` does not exist, refer to \"spawn help\"", args[0]));
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
					LogMessage(string.Format("Invalid distance: {0}", args[2]));
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
		LogMessage(string.Format("Searching for {0} at distance {1}", itemId, maxDistance));
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
			LogMessage(string.Format("Item {0} found at distance {1}", index, distance));
			items[index] = item;
			index++;
		}

		if (index == 1)
		{
			LogMessage("No items found!");
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
			LogMessage(string.Format("Item {0} removed!", index));
			index++;
		}
	}

	public static void RestoreSpecialItems()
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
		LogMessage(string.Format("Restoring \"{0}\"", itemInfo.m_ID));
		if (itemInfo.m_ID == Enums.ItemID.Stone)
		{
			ModifyItemProperties(type, itemInfo, false);
			return;
		}
		ModifyItemProperties(type, itemInfo);
	}

	// Modifies weapons - Infinite Damage and Durability
	private static void ModifyWeapon(WeaponInfo itemInfo)
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
	private static void ModifyThrowable(ItemInfo itemInfo)
	{
		itemInfo.m_Mass = 0.1f;
		itemInfo.m_ThrowForce = 480f;
		itemInfo.m_ThrowDamage = float.MaxValue;
	}

	// Modifies containers - Capacity = 1000
	private static void ModifyContainer(LiquidContainerInfo itemInfo)
	{
		itemInfo.m_Capacity = 1000f;
		itemInfo.m_Mass = 0.1f;
	}

	// Modifies the stats of a firestarter
	private static void ModifyFireStarter(ItemToolInfo itemInfo)
	{
		itemInfo.m_Mass = 0.1f;
		itemInfo.m_HealthLossPerSec = 1E-45f;
		itemInfo.m_MakeFireStaminaConsumptionMul = 0f;
		itemInfo.m_MakeFireDuration = 0.1f;
	}

	// Modifies foods - All stats
	private static void ModifyFood(FoodInfo itemInfo)
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
			LogMessage(string.Format("Modification of '{0}' and type '{1}' is not supported!", itemInfo.m_ID, type.Name));
			return;
		}
	}

	// Adds an item alias to the list
	// alias [alias] [itemId=emptyToRemove]
	public static void AddItemAlias(ArraySegment<string> args)
	{
		if (args.Count < 1)
		{
			LogMessage("Teleport requires additional arguments: [alias] [itemId=emptyToRemove]");
			return;
		}

		// list or remove
		if (args.Count == 1)
		{
			// list existing aliases
			if (Equals(args[0], "list"))
			{
				LogMessage(ItemAliasManager.ListSavedAliases());
				return;
			}
			// remove alias
			LogMessage(ItemAliasManager.AddAlias(args[0], Enums.ItemID.None));
			return;
		}

		// add alias
		if (!ParseEnum(args[1], out Enums.ItemID itemId))
		{
			LogMessage(string.Format("ItemId `{0}` does not exist, refer to \"spawn help\"", args[1]));
			return;
		}

		LogMessage(ItemAliasManager.AddAlias(args[0], itemId));
	}

	private static readonly Dictionary<string, Action> SpecialItemMap = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
		 {
		{ "Knife", () => SpawnItemAndModify<WeaponInfo>(Enums.ItemID.metal_blade_weapon) },
		{ "First_Blade", () => SpawnItemAndModify<WeaponInfo>(Enums.ItemID.Obsidian_Bone_Blade) },
		{ "Lucifers_Spear", () => SpawnItemAndModify<WeaponInfo>(Enums.ItemID.Obsidian_Spear) },
		{ "Super_Bidon", () => SpawnItemAndModify<LiquidContainerInfo>(Enums.ItemID.Bidon) },
		{ "Super_Pot", () => SpawnItemAndModify<BowlInfo>(Enums.ItemID.Pot) },
		{ "Magic_Pills", () => SpawnItemAndModify<FoodInfo>(Enums.ItemID.Painkillers) },
		{ "Kryptonite", () => SpawnItemAndModify<ItemInfo>(Enums.ItemID.Stone, false) },
		{ "Lighter", () => SpawnItemAndModify<ItemToolInfo>(Enums.ItemID.Rubing_Wood) },
	};

	public static string GetSpecialItemNames()
	{
		var sb = new StringBuilder();
		foreach (var kv in SpecialItemMap)
		{
			sb.AppendLine(kv.Key);
		}
		return sb.ToString();
	}

	public static readonly HashSet<Enums.ItemID> SpecialItemIds = new HashSet<Enums.ItemID> {
		Enums.ItemID.metal_blade_weapon,
		Enums.ItemID.Obsidian_Bone_Blade,
		Enums.ItemID.Obsidian_Spear,
		Enums.ItemID.Bidon,
		Enums.ItemID.Pot,
		Enums.ItemID.Painkillers,
		Enums.ItemID.Stone,
		Enums.ItemID.Rubing_Wood,
	};
	#endregion

	#region Special Commands
	// Toggles rain on/off
	public static void ToggleRain()
	{
		var manager = RainManager.Get();
		if (manager.IsRain())
		{
			manager.ScenarioStopRain();
			LogMessage("Stopping rain!");
			return;
		}
		manager.ScenarioStartRain();
		LogMessage("Starting rain!");
	}

	// Unlocks the whole notepad
	public static void UnlockNotepad()
	{
		var manager = ItemsManager.Get();
		manager.UnlockWholeNotepad();
		LogMessage("Notepad unlocked!");
	}

	// Increases the backpack weight to 999
	public static void IncreaseBackpackWeight()
	{
		var backpack = InventoryBackpack.Get();
		backpack.m_MaxWeight = 999f;
		LogMessage("Backpack weight increased to 999!");
		LogMessage("To prevent errors, save the game only when the backpack weight is <= 50");
	}

	// Logs the item info to the console
	// itemInfo [itemId]
	public static void LogItemInfo(ArraySegment<string> args)
	{
		if (args.Count < 1)
		{
			LogMessage("Teleport requires additional argument: ItemID");
			return;
		}

		if (!ParseEnum(args[0], out Enums.ItemID itemId))
		{
			LogMessage(string.Format("ItemId `{0}` does not exist, refer to \"spawn help\"", args[0]));
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
		LogMessage(sb.ToString());
	}

	// Starts or stops the progress of dayTime
	// TimeProgress [true/false]
	public static void TimeProgress(ArraySegment<string> args)
	{
		if (args.Count < 1)
		{
			LogMessage("TimeProgress requires additional argument: [true/false]");
			return;
		}

		if (!bool.TryParse(args[0], out bool progress))
		{
			LogMessage(string.Format("`{0}` is invalid boolean!", args[0]));
			return;
		}

		var level = MainLevel.Instance;

		if (progress)
		{
			level.StartDayTimeProgress();
			LogMessage("Time progress started!");
			return;
		}

		level.StopDayTimeProgress();
		LogMessage("Time progress stopped!");
	}

	// Sets the time to requested time
	// setDayTime [hour] [minutes]
	public static void SetDayTime(ArraySegment<string> args)
	{
		if (args.Count < 2)
		{
			LogMessage("SetDayTime requires additional arguments: [hour] [minutes]");
			return;
		}

		if (!int.TryParse(args[0], out int hour))
		{
			LogMessage(string.Format("Hour `{0}` is invalid!", args[0]));
			return;
		}

		if (!int.TryParse(args[1], out int minutes))
		{
			LogMessage(string.Format("Minutes `{0}` is invalid!", args[1]));
			return;
		}

		if (hour < 0 || hour > 23 || minutes < 0 || minutes > 59)
		{
			LogMessage("Arguments invalid, hour must be between 0 and 23, minutes between 0 and 59");
			return;
		}

		var level = MainLevel.Instance;
		level.SetDayTime(hour, minutes);
		LogMessage(string.Format("Added {0} hours to the current time!", hour));
	}

	// Increases the skills of the player
	// increaseSkills [amount]
	public static void IncreaseSkills(ArraySegment<string> args)
	{
		if (args.Count < 1)
		{
			LogMessage("IncreaseSkills requires additional argument: [amount]");
			return;
		}

		if (!int.TryParse(args[0], out int amount))
		{
			LogMessage(string.Format("'{0}' is invalid for argument 'amount'", args[0]));
			return;
		}

		if (amount < 1)
		{
			LogMessage("Amount must be greater than 0");
			return;
		}

		const float min = 0f;
		const float max = 100f;
		var fistSkill = Skill.Get<FistsSkill>();
		fistSkill.m_Value = Mathf.Clamp(fistSkill.m_Value + amount, min, max);
		var axeSkill = Skill.Get<AxeSkill>();
		axeSkill.m_Value = Mathf.Clamp(axeSkill.m_Value + amount, min, max);
		var bladeSkill = Skill.Get<BladeSkill>();
		bladeSkill.m_Value = Mathf.Clamp(bladeSkill.m_Value + amount, min, max);
		var spearSkill = Skill.Get<SpearSkill>();
		spearSkill.m_Value = Mathf.Clamp(spearSkill.m_Value + amount, min, max);
		var twoHandedSkill = Skill.Get<TwoHandedSkill>();
		twoHandedSkill.m_Value = Mathf.Clamp(twoHandedSkill.m_Value + amount, min, max);
		var craftingSkill = Skill.Get<CraftingSkill>();
		craftingSkill.m_Value = Mathf.Clamp(craftingSkill.m_Value + amount, min, max);
		var makeFireSkill = Skill.Get<MakeFireSkill>();
		makeFireSkill.m_Value = Mathf.Clamp(makeFireSkill.m_Value + amount, min, max);
		var cookingSkill = Skill.Get<CookingSkill>();
		cookingSkill.m_Value = Mathf.Clamp(cookingSkill.m_Value + amount, min, max);
		var archerySkill = Skill.Get<ArcherySkill>();
		archerySkill.m_Value = Mathf.Clamp(archerySkill.m_Value + amount, min, max);
		var throwingSkill = Skill.Get<ThrowingSkill>();
		throwingSkill.m_Value = Mathf.Clamp(throwingSkill.m_Value + amount, min, max);
		var fishingSkill = Skill.Get<FishingSkill>();
		fishingSkill.m_Value = Mathf.Clamp(fishingSkill.m_Value + amount, min, max);
		var harvestingAnimalsSkill = Skill.Get<HarvestingAnimalsSkill>();
		harvestingAnimalsSkill.m_Value = Mathf.Clamp(harvestingAnimalsSkill.m_Value + amount, min, max);
		var spearFishingSkill = Skill.Get<SpearFishingSkill>();
		spearFishingSkill.m_Value = Mathf.Clamp(spearFishingSkill.m_Value + amount, min, max);
		var potterySkill = Skill.Get<PotterySkill>();
		potterySkill.m_Value = Mathf.Clamp(potterySkill.m_Value + amount, min, max);
		var blowgunSkill = Skill.Get<BlowgunSkill>();
		blowgunSkill.m_Value = Mathf.Clamp(blowgunSkill.m_Value + amount, min, max);

		LogMessage(string.Format("Skills increased by {0}!", amount));
	}
	#endregion

	#region Teleportation
	// Teleports the player to the specified coordinates
	// teleport [latitude] [longitude]
	// teleport offset [latitude] [longitude]
	// teleport [locationName]
	public static void Teleport(ArraySegment<string> args)
	{
		if (args.Count == 1 && SavedLocationsManager.TryGetLocation(args[0], out Vector3 newPos))
		{
			TeleportInternal(newPos);
			return;
		}

		if (args.Count < 2)
		{
			LogMessage("Teleport requires additional arguments: [optional=offset] [latitude] [longitude]");
			return;
		}

		// Regular teleportation to coordinates
		if (args.Count == 2)
		{
			TeleportToCoordinates(args[0], args[1]);
			return;
		}

		// Teleportation with offset
		if (Equals(args[0], "offset"))
		{
			TeleportOffset(args[1], args[2]);
			return;
		}

		LogMessage("Teleportation arguments invalid.");
	}

	private static void TeleportToCoordinates(string @lat, string @long)
	{
		if (!float.TryParse(@lat, out float latitude))
		{
			LogMessage(string.Format("Invalid argument for 'latitude': {0}", @lat));
			return;
		}

		if (!float.TryParse(@long, out float longitude))
		{
			LogMessage(string.Format("Invalid argument for 'longitude': {0}", @long));
			return;
		}

		// calculate new position
		const float latModifier = 40.8185f;
		const float longModifier = 36.4861f;
		float positionLat = (latitude - 57f) * latModifier * -1f + latModifier * 0.5f;
		float positionLong = (longitude - 64f) * longModifier * -1f + longModifier * 0.5f;
		var newPos = new Vector3(positionLat, 5f, positionLong);

		// Teleport player
		TeleportInternal(newPos);
	}

	private static void TeleportOffset(string @lat, string @long)
	{
		if (!float.TryParse(@lat, out float latitude))
		{
			LogMessage(string.Format("Invalid argument for 'latitude': {0}", @lat));
			return;
		}

		if (!float.TryParse(@long, out float longitude))
		{
			LogMessage(string.Format("Invalid argument for 'longitude': {0}", @long));
			return;
		}

		// calculate new position
		var player = Player.Get();
		var position = player.transform.position;
		var newPos = new Vector3(position.x + latitude, 5f, position.z + longitude);

		// Teleport player
		TeleportInternal(newPos);
	}

	private static void TeleportInternal(Vector3 position)
	{
		var player = Player.Get();
		var rotation = player.transform.rotation;
		// show_loading
		player.TeleportTo(position, rotation, true);
		LogMessage(string.Format("Teleported to: {0}", position));
	}

	// Saves the current player position to a file
	// saveLocation list
	// saveLocation [locationName]
	// saveLocation [locationName] remove
	public static void AddSavedLocation(ArraySegment<string> args)
	{
		if (args.Count < 1)
		{
			LogMessage("Save location requires additional arguments: [locationName] ?[remove]");
			return;
		}

		// remove location
		if (args.Count == 2 && Equals(args[1], "remove"))
		{
			LogMessage(SavedLocationsManager.AddLocation(args[0], Vector3.zero));
			return;
		}

		// list saved locations
		if (Equals(args[0], "list"))
		{
			LogMessage(SavedLocationsManager.ListSavedLocations());
			return;
		}

		// save location
		var player = Player.Get();
		var position = player.transform.position;
		LogMessage(SavedLocationsManager.AddLocation(args[0], position));
	}
	#endregion
}

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
			LogMessage("Aliases file does not exist - loading aborted");
			return;
		}
		var csv = File.ReadAllLines(AliasesPath);
		foreach (var line in csv)
		{
			var kv = line.Split(',');
			if (!ParseEnum(kv[1], out Enums.ItemID itemId))
			{
				LogMessage(string.Format("Failed to parse item id from alias file at line: {0}", line));
				continue;
			}
			_itemAliases[kv[0]] = itemId;
		}
		if (_itemAliases.Count == 0)
		{
			LogMessage("No item aliases loaded");
			return;
		}
		LogMessage("Loaded item aliases");
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

public static class SavedLocationsManager
{
	private static Dictionary<string, Vector3> _savedLocations;
	private static readonly string BaseFolder = Application.dataPath;
	private static readonly string SavedLocationsPath = Path.Combine(BaseFolder, "SavedLocations.csv");

	static SavedLocationsManager()
	{
		_savedLocations = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
		LoadSavedLocations();
	}

	private static void LoadSavedLocations()
	{
		if (!File.Exists(SavedLocationsPath))
		{
			LogMessage("Saved locations file does not exist - loading aborted");
			return;
		}
		var csv = File.ReadAllLines(SavedLocationsPath);
		foreach (var line in csv)
		{
			var kv = line.Split(',');
			if (!float.TryParse(kv[1], out float @lat))
			{
				LogMessage("Failed to parse latitude from saved locations file: " + line);
				continue;
			}
			if (!float.TryParse(kv[2], out float @alt))
			{
				LogMessage("Failed to parse altitude from saved locations file: " + line);
				continue;
			}
			if (!float.TryParse(kv[3], out float @long))
			{
				LogMessage("Failed to parse longitude from saved locations file: " + line);
				continue;
			}
			_savedLocations[kv[0]] = new Vector3(@lat, @alt, @long);
		}
		if (_savedLocations.Count == 0)
		{
			LogMessage("No item aliases loaded");
			return;
		}
		LogMessage("Loaded item aliases");
	}

	private static void SaveLocations()
	{
		var csv = new string[_savedLocations.Count];
		var i = 0;
		foreach (var kv in _savedLocations)
		{
			var vector = kv.Value;
			//    lat,alt,long
			csv[i++] = string.Format("{0},{1},{2},{3}", kv.Key, vector.x, vector.y, vector.z);
		}
		File.WriteAllLines(SavedLocationsPath, csv);
	}

	public static bool TryGetLocation(string name, out Vector3 location)
	{
		return _savedLocations.TryGetValue(name, out location);
	}

	public static string AddLocation(string name, Vector3 location)
	{
		if (location != Vector3.zero)
		{
			_savedLocations[name] = location;
			SaveLocations();
			return string.Format("Added location {0} at {1}", name, location);
		}
		// None is deletion
		if (!_savedLocations.ContainsKey(name))
		{
			return string.Format("Location {0} does not exist", name);
		}
		_savedLocations.Remove(name);
		SaveLocations();
		return string.Format("Removed location {0}", name);
	}

	public static string ListSavedLocations()
	{
		if (_savedLocations.Count == 0)
		{
			return "No saved locations found...";
		}
		var sb = new StringBuilder();
		foreach (var kv in _savedLocations)
		{
			sb.AppendLine(string.Format("'{0}': {1}", kv.Key, kv.Value));
		}
		return sb.ToString();
	}
}

public static class SpawnExtensions
{
	// parses the item id from the string, disregards case, and verifies result is defined
	public static bool ParseEnum<T>(string arg, out T @enum) where T : struct, Enum
	{
		return Enum.TryParse(arg, true, out @enum) && Enum.IsDefined(typeof(T), @enum);
	}

	// compares two strings disregarding case
	public static bool Equals(string a, string b)
	{
		return string.Compare(a, b, true) == 0;
	}

	public static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

	private static readonly string _logPath = Path.Combine(DesktopPath, "SpawnLog.log");

	private static bool _logToDesktop = false;

	public static void ToggleLogToDesktop()
	{
		_logToDesktop = !_logToDesktop;
		LogMessage(string.Format("Log to desktop: {0}", _logToDesktop));
	}

	public static void LogMessage(string message)
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
}
