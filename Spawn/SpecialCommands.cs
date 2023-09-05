using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            if (mapTab == null || mapTab.m_MapDatas.Count == 0) {
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
                LogMessage("Teleport requires additional argument: ItemID");
                return;
            }

            if (!args[0].ParseEnum(out ItemID itemId)) {
                LogMessage(string.Format("ItemId `{0}` does not exist, refer to \"spawn help\"", args[0]));
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
                LogMessage(string.Format("`{0}` is invalid boolean!", args[0]));
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
                LogMessage(string.Format("Hour `{0}` is invalid!", args[0]));
                return;
            }

            if (!int.TryParse(args[1], out int minutes)) {
                LogMessage(string.Format("Minutes `{0}` is invalid!", args[1]));
                return;
            }

            if (hour < 0 || hour > 23 || minutes < 0 || minutes > 59) {
                LogMessage("Arguments invalid, hour must be between 0 and 23, minutes between 0 and 59");
                return;
            }

            var level = MainLevel.Instance;
            level.SetDayTime(hour, minutes);
            LogMessage(string.Format("Added {0} hours to the current time!", hour));
        }

        // Increases the skills of the player
        // increaseSkills [amount]
        public static void IncreaseSkills(ArraySegment<string> args) {
            if (args.Count < 1) {
                LogMessage("IncreaseSkills requires additional argument: [amount]");
                return;
            }

            if (!int.TryParse(args[0], out int amount)) {
                LogMessage(string.Format("'{0}' is invalid for argument 'amount'", args[0]));
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

            LogMessage(string.Format("Skills increased by {0}!", amount));
        }

        // FillLiquid [LiquidType] [Capacity(Optional)]
        public static void FillLiquid(ArraySegment<string> args) {
            if (args.Count < 1) {
                LogMessage("FillLiquid requires additional arguments: [LiquidType] [Capacity(Default=100)]");
                return;
            }

            if (!args[0].ParseEnum(out LiquidType liquidType)) {
                LogMessage(string.Format("LiquidType `{0}` does not exist, refer to \"spawn help\"", args[0]));
                return;
            }

            var capacity = 0f;
            var capacityModified = false;
            if (args.Count > 1) {
                if (float.TryParse(args[1], out capacity)) {
                    capacityModified = true;
                } else {
                    LogMessage(string.Format("Capacity `{0}` is invalid!", args[1]));
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

            LogMessage(string.Format("Filled liquid containers with '{0}'", liquidType));
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

        private const string _lighterBackpackKey = "spawn_mod_lighter_backpack";
        private const float _backpackDefaultWeight = 50f;
        private const float _backpackMaxWeight = 999f;

        // LighterBackpack [true/false]
        public static void LighterBackpack(ArraySegment<string> args) {
            var backpack = InventoryBackpack.Get();
            if (args.Count < 1) {
                backpack.m_MaxWeight = _backpackMaxWeight;
                LogMessage("Backpack max weight set to 999f!");
                return;
            }
            if (!bool.TryParse(args[0], out bool lighter)) {
                LogMessage(string.Format("`{0}` is invalid boolean!", args[0]));
                return;
            }
            int lighterVal = Convert.ToInt32(lighter);
            if (PlayerPrefs.HasKey(_lighterBackpackKey) && PlayerPrefs.GetInt(_lighterBackpackKey) == lighterVal) {
                LogMessage(string.Format("Lighter backpack is already set to {0}!", lighter));
                return;
            }
            if (!lighter) {
                PlayerPrefs.SetInt(_lighterBackpackKey, lighterVal);
                backpack.m_MaxWeight = _backpackDefaultWeight;
                LogMessage("Backpack max weight restored to 50f!");
                return;
            }
            PlayerPrefs.SetInt(_lighterBackpackKey, lighterVal);
            backpack.m_MaxWeight = _backpackMaxWeight;
            LogMessage("Backpack max weight set to 999f!");
        }

        public static async Task RestoreLighterBackpackAsync(CancellationToken token) {
            if (!PlayerPrefs.HasKey(_lighterBackpackKey)) {
                return;
            }
            if (!Convert.ToBoolean(PlayerPrefs.GetInt(_lighterBackpackKey))) {
                return;
            }
            while (InventoryBackpack.Get() == null) {
                if (token.IsCancellationRequested) {
                    return;
                }
                await Task.Delay(1000, token);
            }
            var backpack = InventoryBackpack.Get();
            backpack.m_MaxWeight = _backpackMaxWeight;
            LogMessage("Lighter backpack restored!");
        }

        private static readonly FieldInfo FireCampSound = typeof(Firecamp).GetField("m_Sound", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo AudioSourcesField = typeof(AnimationEventsReceiver).GetField("m_FootstepAudioSources", BindingFlags.NonPublic | BindingFlags.Instance);

        public static async Task FixAudioBugBackground(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                try {
                    FixAudioBug(default);
                } catch (Exception e) {
                    LogMessage(string.Format("Error while fixing audio bug: {0}", e.Message));
                }
                await Task.Delay(10000, token);
            }
        }

        public static void FixAudioBug(ArraySegment<string> args) {
            if (args.Count > 0 && bool.TryParse(args[0], out bool force) && force) {
                LogMessage("Forcing continuous audio bug fixing every 10 seconds!");
                Task.Run(() => FixAudioBugBackground(Spawn.CancellationToken.Token)).ConfigureAwait(false);
                return;
            }
            try {
                foreach (var campfire in Firecamp.s_Firecamps) // Solve for firecamp audio bug
                {
                    var sound = (AudioSource)FireCampSound.GetValue(campfire);
                    if (sound is null) {
                        continue;
                    }
                    FireCampSound.SetValue(campfire, null);
                    LogMessage(string.Format("'{0}' firecamp m_Sound field set to null!", campfire.name));
                }
                foreach (var being in BeingsManager.GetAllBeings()) {
                    var receiver = being.m_AnimationEventsReceiver;
					if (receiver is null) {
						continue;
					}
                    var sources = (List<AudioSource>)AudioSourcesField.GetValue(receiver);
                    if (sources.Count == 0) {
                        continue;
                    }
                    int i = 0;
                    while (i < sources.Count) {
                        var source = sources[i];
                        if (source is null) {
                            continue;
                        }
                        sources[i] = null;
                        i++;
                    }
                    LogMessage(string.Format("Neutralized `{0}` m_FootstepAudioSources", being.name));
                }
            } catch {
                throw;
            }
        }

        public static void GetUnityLogPath(ArraySegment<string> args) {
            LogMessage(Application.consoleLogPath);
        }
    }
}