using System;
using System.Text;

using Enums;

using UnityEngine;

using static SpawnMod.SpawnExtensions;

namespace SpawnMod {
    public static class SpecialCommands {
        // Toggles rain on/off
        public static void ToggleRain(ArraySegment<string> args) {
            var manager = RainManager.Get();
            if (manager.IsRain()) {
                manager.ScenarioStopRain();
                LogMessage("Stopping rain!");
                return;
            }
            manager.ScenarioStartRain();
            LogMessage("Starting rain!");
        }

        // Unlocks the whole notepad
        public static void UnlockNotepad(ArraySegment<string> args) {
            var manager = ItemsManager.Get();
            manager.UnlockWholeNotepad();
            LogMessage("Notepad unlocked!");
        }

        // Unlocks all the maps and locations
        public static void UnlockMaps(ArraySegment<string> args) {
            var mapTab = MapTab.Get();
            if (mapTab == null || mapTab.m_MapDatas.Count is 0) {
                LogMessage("{0}: Map could not be unlocked.");
                return;
            }

            foreach (var map in mapTab.m_MapDatas) {
                if (!map.Value.m_Unlocked) {
                    mapTab.UnlockPage(map.Key);
                }

                foreach (var mapElement in map.Value.m_Elemets) {
                    mapTab.UnlockElement(mapElement.name);
                }
            }

            LogMessage("Maps unlocked!");
        }

        // Logs the item info to the console
        // itemInfo [itemId]
        public static void LogItemInfo(ArraySegment<string> args) {
            if (args.Count < 1) {
                LogMessage("Teleport requires additional argument: [ItemID]");
                return;
            }

            if (!args[0].ParseEnum(out ItemID itemId)) {
                LogMessage($"ItemId `{args[0]}` does not exist, refer to \"spawn help\"");
                return;
            }

            GetProps(itemId);
        }

        private static void GetProps(ItemID itemId) {
            var manager = ItemsManager.Get();
            var item = manager.CreateItem(itemId, false);
            var backpack = InventoryBackpack.Get();
            backpack.InsertItem(item, null, null, true, true, true, true, true);
            var itemInfo = item.m_Info;
            var props = itemInfo.GetType().GetProperties();
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("Type: ").AppendLine(itemInfo.GetType().Name);
            foreach (var prop in props) {
                var value = prop.GetValue(itemInfo);
                if (value == null) {
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
        public static void TimeProgress(ArraySegment<string> args) {
            if (args.Count < 1) {
                LogMessage("TimeProgress requires additional argument: [true/false]");
                return;
            }

            if (!bool.TryParse(args[0], out bool progress)) {
                LogMessage($"`{args[0]}` is invalid boolean!");
                return;
            }

            var level = MainLevel.Instance;

            if (progress) {
                level.StartDayTimeProgress();
                LogMessage("Time progress started!");
                return;
            }

            level.StopDayTimeProgress();
            LogMessage("Time progress stopped!");
        }

        // Sets the time to requested time
        // setDayTime [hour] [minutes]
        public static void SetDayTime(ArraySegment<string> args) {
            if (args.Count < 2) {
                LogMessage("SetDayTime requires additional arguments: [hour] [minutes]");
                return;
            }

            if (!int.TryParse(args[0], out int hour)) {
                LogMessage($"Hour `{args[0]}` is invalid!");
                return;
            }

            if (!int.TryParse(args[1], out int minutes)) {
                LogMessage($"Minutes `{args[1]}` is invalid!");
                return;
            }

            if (hour < 0 || hour > 23 || minutes < 0 || minutes > 59) {
                LogMessage("Arguments invalid, hour must be between 0 and 23, minutes between 0 and 59");
                return;
            }

            var level = MainLevel.Instance;
            level.SetDayTime(hour, minutes);
            LogMessage($"Added {hour} hours to the current time!");
        }

        // Increases the skills of the player
        // increaseSkills [amount]
        public static void IncreaseSkills(ArraySegment<string> args) {
            if (args.Count < 1) {
                LogMessage("IncreaseSkills requires additional argument: [amount]");
                return;
            }

            if (!int.TryParse(args[0], out int amount)) {
                LogMessage($"'{args[0]}' is invalid for argument 'amount'");
                return;
            }

            if (amount < 1) {
                LogMessage("Amount must be greater than 0");
                return;
            }

            const float minSkillValue = 0f;
            const float maxSkillValue = 100f;

            foreach (var skill in SkillsManager.Get().m_Skills) {
                skill.m_Value = Mathf.Clamp(skill.m_Value + amount, minSkillValue, maxSkillValue);
            }

            LogMessage($"Skills increased by {amount}!");
        }

        // FillLiquid [LiquidType] [Capacity(Optional)]
        public static void FillLiquid(ArraySegment<string> args) {
            if (args.Count < 1) {
                LogMessage("FillLiquid requires additional arguments: [LiquidType] [Capacity(Default=100)]");
                return;
            }

            if (!args[0].ParseEnum(out LiquidType liquidType)) {
                LogMessage($"LiquidType `{args[0]}` does not exist, refer to \"spawn help\"");
                return;
            }

            var capacity = 0f;
            var capacityModified = false;
            if (args.Count > 1) {
                if (float.TryParse(args[1], out capacity)) {
                    capacityModified = true;
                } else {
                    LogMessage($"Capacity `{args[1]}` is invalid!");
                    return;
                }
            }

            var backpack = InventoryBackpack.Get();
            foreach (var item in backpack.m_Items) {
                if (!item.m_Info.IsLiquidContainer() || item.m_Info.IsBowl()) {
                    continue;
                }
                var info = (LiquidContainerInfo)item.m_Info;
                info.m_LiquidType = liquidType;
                if (capacityModified) {
                    info.m_Amount = capacity;
                    continue;
                }
                // default capacity
                info.m_Amount = info.m_Capacity;
            }

            LogMessage($"Filled liquid containers with '{liquidType}'");
        }

        public static void EndlessFires(ArraySegment<string> args) {
            const float radius = 5f;
            var pos = Player.Get().GetWorldPosition();

            foreach (var firecamp in Firecamp.s_Firecamps) {
                var distance = Vector3.Distance(pos, firecamp.transform.position);
                if (distance > radius) {
                    continue;
                }
                if (firecamp.m_EndlessFire) {
                    firecamp.Extinguish();
                    firecamp.m_EndlessFire = false;
                    continue;
                }
                firecamp.Ignite();
                firecamp.m_EndlessFire = true;
            }

            LogMessage("Endless fires toggled for closest campfires!");
        }

        public static void CompleteConstructions(ArraySegment<string> args) {
            foreach (var constructionGhost in ConstructionGhostManager.Get().GetAll()) {
                constructionGhost.m_CurrentStep = constructionGhost.m_Steps.Count;
            }
            LogMessage("All constructions completed!");
        }

        private const string LighterBackpackKey = "spawn_mod_lighter_backpack";
        private const float BackpackDefaultWeight = 50f;
        private const float BackpackMaxWeight = 999f;

        // LighterBackpack [true/false]
        public static void LighterBackpack(ArraySegment<string> args) {
            var backpack = InventoryBackpack.Get();
            if (args.Count < 1) {
                backpack.m_MaxWeight = BackpackMaxWeight;
                LogMessage("Backpack max weight set to 999f!");
                return;
            }
            if (!bool.TryParse(args[0], out bool lighter)) {
                LogMessage($"`{args[0]}` is invalid boolean!");
                return;
            }
            int lighterVal = Convert.ToInt32(lighter);
            if (PlayerPrefs.HasKey(LighterBackpackKey) && PlayerPrefs.GetInt(LighterBackpackKey) == lighterVal) {
                LogMessage($"Lighter backpack is already set to {lighter}!");
                return;
            }
            if (!lighter) {
                PlayerPrefs.SetInt(LighterBackpackKey, lighterVal);
                backpack.m_MaxWeight = BackpackDefaultWeight;
                LogMessage("Backpack max weight restored to 50f!");
                return;
            }
            PlayerPrefs.SetInt(LighterBackpackKey, lighterVal);
            backpack.m_MaxWeight = BackpackMaxWeight;
            LogMessage("Backpack max weight set to 999f!");
        }

        public static void RestoreLighterBackpack() {
            if (!PlayerPrefs.HasKey(LighterBackpackKey) || !Convert.ToBoolean(PlayerPrefs.GetInt(LighterBackpackKey))) {
                return;
            }
            var backpack = InventoryBackpack.Get();
            backpack.m_MaxWeight = BackpackMaxWeight;
            LogMessage("Lighter backpack restored!");
        }

        public static void GetUnityLogPath(ArraySegment<string> args) {
            LogMessage(Application.consoleLogPath);
        }
    }
}