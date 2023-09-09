# Spawn - General Purpose Mod for Green Hell

## Usage

* **spawn Help** - this will export HelpText to `SpawnHelp.txt` on desktop (with commands and all item ids)
* **spawn ToggleLog** - switches logging to `SpawnLog.log` on desktop (off by default)
* **spawn GetUnityLogPath** - this will log the path to the unity log (useful for troubleshooting)
* **spawn UnlockNotepad** - this will unlock the whole notepad (crafting, recipes, info...)
* **spawn UnlockMaps** - gives the player all available maps and unlocks all locations
* **spawn EndlessFires** - this will toggle on/off for all the fires within distance=5 to be endless (and will light them if they aren't burning)
* **spawn FillLiquid [liquidType] [capacity]** - this will fill all the containers in the backpack with the [liquidType] and the [capacity] you want. if you don't specify [capacity] each container will be filled to the its capacity, [capacity] can also be larger then the container just make sure to account for liquid weight. a list of [liquidType] was added to the help text. Use `help` command to get the updated help text.
* **spawn SetTime [Hour] [Minutes]** - This will set the clock to the desired time [24 hour format]
* **spawn ProgressTime [true/false]** - This will start or stop time progression
* **spawn SaveLocation list** - this will list the saved locations and their coordinates
* **spawn SaveLocation [locationName]** - this will save the current location under the requested name
* **spawn SaveLocation [locationName] remove** - remove the requested location
* **spawn Teleport [savedLocationName]** - this will teleport the player to the desired saved location
* **spawn Teleport [latitude] [longitude]** - this will teleport the player to the desired location
* **spawn Teleport offset [latitude] [longitude]** - this will teleport the player to the offset location
* **spawn Rain** - Will toggle between start and stop rain (depending on the current weather)
* **spawn IncreaseSkills [amount]** - This will increase all skills by the request amount.
* **spawn Alias [alias] [id]** - this will save an alias for an item id, so you don't have to remember complicated item names, these aliases are stored in a file and will be re-loaded with the mod, so they are persistent. If you repeat the command with a different [id] the alias will be overridden.
* **spawn Alias [alias]** - Without specifying an item id, the alias if exists, will be removed.
* **spawn Alias list** - this will list the saved item aliases.
* **spawn ItemInfo [id]** - This will log all the properties inside the ItemInfo for that specific item.
* **spawn Get [Id/Alias/specialItemName] [quantity]** - this will spawn multiples of the item by id, alias or special item name, quantity is optional and will default to 1 if left out
* **spawn Remove [id/alias] [maxDistance/debug]** - this will remove the item by id or alias, maxDistance will be defaulted to 5 if left out, if instead you ask for "debug", it will log all items at all distances and location but not remove them.
* **spawn RemoveConstructions [maxDistance]** - this removes all constructions placed within the distance.
* **spawn CompleteConstructions** - this will complete all started constructions.
* **spawn RestoreSpecialItems [true/false]** - this will attempt to restore the properties of the special items it can find in the backpack, Other items with the same id will also be granted the same properties. So either use other items for regular things, or spawn and destroy the special items according to usage. Specifying `true` will make the mod remember the setting and load it automatically. Setting it to `false` will make the mod forget.
* **spawn LighterBackpack [true/false]** - this will make the backpack maximum weight 999, same as last command, specifying `true/false` will make the mod `remember/forget`.

### Special Items

#### Tools

* `Knife` - metal_blade_weapon but with infinite durability and damage.
* `Stormbreaker` - modified metal_axe.
* `FirstBlade` - this will look like the Obsidian_Bone_Blade, but will have near infinite durability and will kill/destroy every object with one strike/throw.
* `LucifersSpear` - this will look like the Obsidian_Spear, but will be able to kill any enemy/animal with one strike/throw.
* `Kryptonite` - this is a small stone, but has way stronger throwing force and infinite damage, it will kill anything you throw it at with one blow.
* `Lighter` - this is a fire starter that will ignite instantly, have unlimited uses and not use any stamina.

#### Containers

* `SuperBidon` - this will look like the Bidon but will hold 1000 units of liquid.
* `SuperPot` - like Pot and also hold 1000 units of liquid.

#### Others

* `SuperPills` - this is a modified version of Painkillers, it will fill all of your stats, cure insomnia, fever, and venom/poison.

## NOTES

To submit bug reports and or feature requests, find me on the GreenHellMods [discord](https://greenhellmodding.com/discord) server, or contact me on [telegram](https://t.me/dsr47)
