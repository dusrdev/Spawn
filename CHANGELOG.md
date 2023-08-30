# Version 1.4

## Added Commands

* **spawn GetUnityLogPath** - this will log the path to the unity log (useful for troubleshooting)
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
* **spawn [specialItem]** - this will spawn an existing item but with modified stats
* **spawn [specialItem] [quantity]** - this will spawn multiples of the special item
* **spawn RestoreSpecialItems** - this will attempt to restore the properties of the special items it can find in the backpack, Other items with the same id will also be granted the same properties. So either use other items for regular things, or spawn and destroy the special items according to usage.

## Features Explanation

* `SetTime` and `ProgressTime` will now allow you to manipulate the game time
* `Rain` will allow you to toggle on and off, no longer having to rely on the game weather cycles.
* `Teleport` will allow you to teleport either to coordinates, or offset (like 10 meters to some direction...), or to saved locations.
* `IncreaseSkills` will now allow you to increase all the skills by a request amount.
* `RestoreSpecialItems` will now check against all the item id's in the backpack and restore special stats if it is in the special items category.
* `SaveLocation` will now let you save any location you want by name and teleport to it whenever you want. The saves are persistent, they remain when you restart and quit the game.
* `Alias` will now let you give any requested name to an itemId, so you don't have to write or remember complex names. Aliases are also persistent and will remain when the game quits and restarts.
* `Spawn` and `Remove` commands will now let you use the saved aliases and they will be the priority.
* `SaveLocation` and `Alias` have `list` and `remove` options, check in the above command guide.
* `SpecialItems` is the new name for special weapon, as now there are more than weapons, there is another weapon `Knife`, larger capacity `Bidon` and `Pot`, `Magic_Pills` which is the best food source there is, `Ligher` which is a fire starter that is instant and requires no stamina, and `Kryptonite` which is a modified stone that can thrown with extreme force and infinite damage.

## NOTES

With this number of changes and features added there might be bugs that were missed during testing.

To submit bug reports and or feature requests, find me on the GreenHellMods [discord](https://greenhellmodding.com/discord) server, or contact me on [telegram](https://t.me/dsr47)
