using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Enums;

using UnityEngine;

using static SpawnMod.SpawnExtensions;

namespace SpawnMod {
    public static class SpawnAndRemove {
        // GetItem [itemId/SpecialItemName/Alias] [quantity(Default=1)]
        public static void SpawnItem(ArraySegment<string> args) {
            if (SpecialItemMap.TryGetValue(args[0], out Action itemSpawnFunction)) {
                var count = 1;
                if (args.Count > 1 && !int.TryParse(args[1], out count)) {
                    LogMessage($"Quantity `{args[1]}` is invalid!");
                    return;
                }
                for (var i = 0; i < count; i++) {
                    itemSpawnFunction.Invoke();
                }
                return;
            }

            if (ItemAliasManager.TryGetAlias(args[0], out ItemID aliasedItemId)) {
                SpawnItemInternal(aliasedItemId, args);
                return;
            }

            if (!args[0].ParseEnum(out ItemID itemId)) {
                LogMessage($"ItemId `{args[0]}` does not exist, refer to \"spawn help\"");
                return;
            }

            SpawnItemInternal(itemId, args);
        }

        private static void SpawnItemInternal(Enums.ItemID itemId, ArraySegment<string> args) {
            var quantity = 1;

            if (args.Count > 1 && !int.TryParse(args[1], out quantity)) {
                LogMessage($"Quantity `{args[1]}` is invalid!");
                return;
            }

            var manager = ItemsManager.Get();
            var itemInfo = manager.GetInfo(itemId);

            if (itemInfo.m_CanBeAddedToInventory) {
                LogMessage($"Spawning {quantity} x `{itemId}` to backpack");
                var backpack = InventoryBackpack.Get();
                for (var i = 0; i < quantity; i++) {
                    var itemInstance = manager.CreateItem(itemId, false);
                    backpack.InsertItem(itemInstance, null, null, true, true, true, true, true);
                }
                return;
            }

            LogMessage($"Spawning at player: `{itemId}`");
            var item = manager.CreateItem(itemId, false);
            var playerTransform = Player.Get().transform;
            item.transform.position = playerTransform.position;
            item.transform.rotation = playerTransform.rotation;
            item.ItemsManagerRegister(true);
        }

        private static void SpawnItemAndModify<T>(ItemID itemId, bool differentTypeIsError = true) where T : ItemInfo {
            LogMessage($"Spawning modified `{itemId}`.");
            var manager = ItemsManager.Get();
            var item = manager.CreateItem(itemId, false);
            var backpack = InventoryBackpack.Get();
            backpack.InsertItem(item, null, null, true, true, true, true, true);
            ModifyItemProperties(typeof(T), item.m_Info, differentTypeIsError);
        }

        // RemoveItem [itemId/Alias] [maxDistance(Default=5)/debug]
        public static void RemoveItem(ArraySegment<string> args) {
            var isAlias = ItemAliasManager.TryGetAlias(args[0], out ItemID itemId);
            if (!isAlias && !args[0].ParseEnum(out itemId)) {
                LogMessage($"ItemId `{args[0]}` does not exist, refer to \"spawn help\"");
                return;
            }

            float maxDistance = 5f;
            bool debug = false;

            if (args.Count > 1) {
                if (args[1].EqualsInsensitive("debug")) {
                    debug = true;
                } else {
                    var isValid = float.TryParse(args[1], out maxDistance);
                    if (!isValid) {
                        LogMessage($"Invalid distance: {args[1]}");
                        return;
                    }
                }
            }

            var playerPosition = Player.Get().GetWorldPosition();

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
            LogMessage($"Searching for `{itemId}` at distance `{maxDistance}`");
            foreach (var item in Item.s_AllItems) {
                if (item.m_Info.m_ID != itemId) {
                    continue;
                }

                var itemPosition = item.transform.position;
                var distance = Vector3.Distance(playerPosition, itemPosition);

                if (debug) {
                    sb.Append("ItemPosition: ")
                        .AppendLine(itemPosition.ToString())
                        .Append("Distance: ")
                        .AppendLine(distance.ToString());
                }

                if (distance > maxDistance) {
                    continue;
                }
                LogMessage($"Item `{index}` found at distance `{distance}`");
                items[index] = item;
                index++;
            }

            if (index is 1) {
                LogMessage("No items found!");
            }

            if (debug) {
                var path = Path.Combine(DesktopPath, "rmDebug.txt");
                File.WriteAllText(path, sb.ToString());
            }

            index = 1;

            while (!debug && items.Count > 0) {
                var item = items[index];
                items.Remove(index);
                item.m_Info.m_CanBeRemovedFromInventory = true;
                item.m_Info.m_DestroyByItemsManager = true;
                item.m_Info.m_CantDestroy = false;
                ItemsManager.Get().AddItemToDestroy(item);
                LogMessage($"Item `{index}` removed!");
                index++;
            }
        }

        // RemoveConstructions [maxDistance(Default=10)]
        public static void RemoveConstructions(ArraySegment<string> args) {
            float maxDistance = 10f;

            if (args.Count > 1) {
                var isValid = float.TryParse(args[1], out maxDistance);
                if (!isValid) {
                    LogMessage($"Invalid distance: `{args[1]}`");
                    return;
                }
            }

            var playerPosition = Player.Get().GetWorldPosition();

            var items = new Dictionary<int, Item>();
            int index = 1;
            foreach (var item in Item.s_AllItems) {
                if (!item.m_Info.IsConstruction()) {
                    continue;
                }

                var itemPosition = item.transform.position;
                var distance = Vector3.Distance(playerPosition, itemPosition);
                if (distance > maxDistance) {
                    continue;
                }

                LogMessage($"Construction `{index}` found at distance `{distance}`");
                items[index] = item;
                index++;
            }

            if (index is 1) {
                LogMessage("No items found!");
            }

            index = 1;

            while (items.Count > 0) {
                var item = items[index];
                items.Remove(index);
                item.m_Info.m_CanBeRemovedFromInventory = true;
                item.m_Info.m_DestroyByItemsManager = true;
                item.m_Info.m_CantDestroy = false;
                ItemsManager.Get().AddItemToDestroy(item);
                LogMessage($"Construction `{index}` removed!");
                index++;
            }
        }

        private const string SpecialItemsKey = "spawn_mod_special_items";

        public static void RestoreSpecialItemsFromMemory() {
            if (!PlayerPrefs.HasKey(SpecialItemsKey) || !Convert.ToBoolean(PlayerPrefs.GetInt(SpecialItemsKey))) {
                return;
            }
            RestoreSpecialItems(ArraySegment<string>.Empty);
        }

        private static void SetRestoreSpecialItems(bool value) {
            PlayerPrefs.SetInt(SpecialItemsKey, value ? 1 : 0);
        }

        // RestoreSpecialItems [true/false]
        public static void RestoreSpecialItems(ArraySegment<string> args) {
            if (args.Count >= 1 && bool.TryParse(args[0], out bool value)) {
                SetRestoreSpecialItems(value);
            }

            var backpack = InventoryBackpack.Get();
            int count = 0;

            foreach (var item in backpack.m_Items) {
                var itemInfo = item.m_Info;
                if (TryRestoreSpecialItemProperties(itemInfo)) {
                    count++;
                }
            }
            if (TryRestoreSpecialItemProperties(backpack.m_EquippedItem.m_Info)) {
                count++;
            }

            if (count > 0) {
                LogMessage($"Restored {count} special items!");
                return;
            }

            LogMessage("No special items found!");
        }

        private static bool TryRestoreSpecialItemProperties(ItemInfo itemInfo) {
            if (!SpecialItemIds.Contains(itemInfo.m_ID)) { // Regular item
                return false;
            }
            var type = itemInfo.GetType();
            if (itemInfo.m_ID == ItemID.Stone) {
                ModifyItemProperties(type, itemInfo, false);
                return true;
            }
            ModifyItemProperties(type, itemInfo);
            return true;
        }

        // Modifies weapons - Infinite Damage and Durability
        private static void ModifyWeapon(WeaponInfo itemInfo) {
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
        private static void ModifyThrowable(ItemInfo itemInfo) {
            itemInfo.m_Mass = 0.1f;
            itemInfo.m_ThrowForce = 480f;
            itemInfo.m_ThrowDamage = float.MaxValue;
        }

        // Modifies containers - Optional Capacity
        private static void ModifyContainer(LiquidContainerInfo itemInfo, float capacity = 1000f) {
            itemInfo.m_Capacity = capacity;
            itemInfo.m_Mass = 0.1f;
        }

        // Modifies the stats of a fire starter
        private static void ModifyFireStarter(ItemToolInfo itemInfo) {
            itemInfo.m_Mass = 0.1f;
            itemInfo.m_HealthLossPerSec = 1E-45f;
            itemInfo.m_MakeFireStaminaConsumptionMul = 0f;
            itemInfo.m_MakeFireDuration = 0.1f;
        }

        // Modifies foods - All stats
        private static void ModifyFood(FoodInfo itemInfo) {
            itemInfo.m_Mass = 0.1f;
            itemInfo.m_SpoilTime = -1f;
            itemInfo.m_TroughFood = 100f;
            itemInfo.m_Fat = 100f;
            itemInfo.m_Carbohydrates = 100f;
            itemInfo.m_Proteins = 100f;
            itemInfo.m_Water = 100f;
            itemInfo.m_AddEnergy = 100f;
            itemInfo.m_SanityChange = 100;
            itemInfo.m_ConsumeEffect = ConsumeEffect.Fever;
            itemInfo.m_ConsumeEffectChance = 1f;
            itemInfo.m_ConsumeEffectDelay = 0f;
            itemInfo.m_ConsumeEffectLevel = -15;
            itemInfo.m_PoisonDebuff = 15;
            itemInfo.m_MakeFireDuration = 100f;
        }

        private static void ModifyItemProperties(Type type, ItemInfo itemInfo, bool differentIsError = true) {
            if (type == typeof(WeaponInfo) || type == typeof(SpearInfo)) {
                ModifyWeapon((WeaponInfo)itemInfo);
            } else if (type == typeof(LiquidContainerInfo) || type == typeof(BowlInfo)) {
                ModifyContainer((LiquidContainerInfo)itemInfo);
            } else if (type == typeof(FoodInfo)) {
                ModifyFood((FoodInfo)itemInfo);
            } else if (type == typeof(ItemToolInfo)) {
                ModifyFireStarter((ItemToolInfo)itemInfo);
            } else if (!differentIsError) {
                ModifyThrowable(itemInfo);
            } else {
                LogMessage($"Modification of '{itemInfo.m_ID}' and type '{type.Name}' is not supported!");
            }
        }

        // Adds an item alias to the list
        // alias [alias] [itemId=emptyToRemove]
        public static void AddItemAlias(ArraySegment<string> args) {
            if (args.Count < 1) {
                LogMessage("Teleport requires additional arguments: [alias] [itemId=emptyToRemove]");
                return;
            }

            // list or remove
            if (args.Count is 1) {
                // list existing aliases
                if (Equals(args[0], "list")) {
                    LogMessage(ItemAliasManager.ListSavedAliases());
                    return;
                }
                // remove alias
                LogMessage(ItemAliasManager.AddAlias(args[0], ItemID.None));
                return;
            }

            // add alias
            if (!args[1].ParseEnum(out ItemID itemId)) {
                LogMessage($"ItemId `{args[1]}` does not exist, refer to \"spawn help\"");
                return;
            }

            LogMessage(ItemAliasManager.AddAlias(args[0], itemId));
        }

        private static readonly Dictionary<string, Action> SpecialItemMap = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase) { { "Stormbreaker", () => SpawnItemAndModify<WeaponInfo> (ItemID.metal_axe) }, { "Knife", () => SpawnItemAndModify<WeaponInfo> (ItemID.metal_blade_weapon) }, { "FirstBlade", () => SpawnItemAndModify<WeaponInfo> (ItemID.Obsidian_Bone_Blade) }, { "LucifersSpear", () => SpawnItemAndModify<WeaponInfo> (ItemID.Obsidian_Spear) }, { "SuperBidon", () => SpawnItemAndModify<LiquidContainerInfo> (ItemID.Bidon) }, { "SuperPot", () => SpawnItemAndModify<BowlInfo> (ItemID.Pot) }, { "MagicPills", () => SpawnItemAndModify<FoodInfo> (ItemID.Painkillers) }, { "Kryptonite", () => SpawnItemAndModify<ItemInfo> (ItemID.Stone, false) }, { "Lighter", () => SpawnItemAndModify<ItemToolInfo> (ItemID.Rubing_Wood) },
        };

        public static string GetSpecialItemNames() {
            var sb = new StringBuilder();
            foreach (var kv in SpecialItemMap) {
                sb.AppendLine(kv.Key);
            }
            return sb.ToString();
        }

        public static readonly HashSet<ItemID> SpecialItemIds = new HashSet<ItemID> {
            ItemID.metal_axe,
            ItemID.metal_blade_weapon,
            ItemID.Obsidian_Bone_Blade,
            ItemID.Obsidian_Spear,
            ItemID.Bidon,
            ItemID.Pot,
            ItemID.Painkillers,
            ItemID.Stone,
            ItemID.Rubing_Wood,
        };
    }
}