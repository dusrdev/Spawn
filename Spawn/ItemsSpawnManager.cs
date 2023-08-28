using System.Net;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using UnityEngine;
using static SpawnExtensions;

namespace SpawnComponents
{
	public static class ItemsSpawnManager
	{
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

		public static void AddItemAlias(ArraySegment<string> args)
		{
			if (args.Count < 2)
			{
				LogMessage("Teleport requires additional arguments: [alias] [itemId=emptyToRemove]");
				return;
			}

			if (args.Count == 2)
			{
				if (Equals(args[1], "list"))
				{
					LogMessage(ItemAliasManager.ListSavedAliases());
					return;
				}
				LogMessage(ItemAliasManager.AddAlias(args[1], Enums.ItemID.None));
				return;
			}

			if (!ParseEnum(args[2], out Enums.ItemID itemId))
			{
				LogMessage(string.Format("ItemId `{0}` does not exist, refer to \"spawn help\"", args[2]));
				return;
			}

			LogMessage(ItemAliasManager.AddAlias(args[1], itemId));
		}

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
	}
}