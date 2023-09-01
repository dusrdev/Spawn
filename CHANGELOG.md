# Version 1.5

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

* New **SpecialItems**: `Stormbreaker` a modified metal_axe

## NOTES

To submit bug reports and or feature requests, find me on the GreenHellMods [discord](https://greenhellmodding.com/discord) server, or contact me on [telegram](https://t.me/dsr47)
