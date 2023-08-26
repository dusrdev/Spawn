# Spawn - General Purpose Mod for Green Hell

## Usage

* **spawn Help** - this will export HelpText to `SpawnHelp.txt` on desktop (with commands and all item ids)
* **spawn ToggleLog** - switches logging to `SpawnLog.log` on desktop (off by default)
* **spawn UnlockNotepad** - this will unlock the whole notepad (crafting, recipes, info...)
* **spawn Teleport [latitude] [longitude]** - this will teleport the player to the desired location
* **spawn Rain** - Will toggle between start and stop rain (depending on the current weather)
* **spawn Alias [alias] [id]** - this will save an alias for an item id, so you don't have to remember complicated item names, these aliases are stored in a file and will be re-loaded with the mod, so they are persistent. If you repeat the command with a different [id] the alias will be overridden.
* **spawn Alias [alias]** - Without specifying an item id, the alias if exists, will be removed.
* **spawn ItemInfo [id]** - This will log all the properties inside the ItemInfo for that specific item.
* **spawn [id]** - this will spawn the item with the corresponding [id]
* **spawn [id] [quantity]** - this will spawn multiples of the item, only applicable to items that can be places inside the backpack.
* **spawn [id] rm** - this will destroy all items with that [id] in radius of 5 (approximately 2.5 meters/yards)
* **spawn [id] rm [maxDistance]** - this is same as last one but can configure distance
* **spawn [id] rm debug** - this will list all found items with their positions and distances but **NOT REMOVE THEM**
* **spawn [specialItem]** - this will spawn an existing item but with modified stats
* **spawn [specialItem] [quantity]** - this will spawn multiples of the special item
* **spawn RestoreSpecialItems** - this will attempt to restore the properties of the special items it can find in the backpack, Other items with the same id will also be granted the same properties. So either use other items for regular things, or spawn and destroy the special items according to usage.

### Special Items

#### Tools

* `First_Blade` - this will look like the Obsidian_Bone_Blade, but will have near infinite durability and will kill/destroy every object with one strike/throw.
* `Lucifers_Spear` - this will look like the Obsidian_Spear, but will be able to kill any enemy/animal with one strike/throw.
* `Kryptonite` - this is a small stone, but has way stronger throwing force and infinite damage, it will kill anything you throw it at with one blow.
* `Lighter` - this is a fire starter that will ignite instantly, have unlimited uses and not use any stamina.

#### Containers

* `Super-Bidon` - this will look like the Bidon but will hold 1000 units of liquid.
* `Super-Pot` - like Pot and also hold 1000 units of liquid.

#### Others

* `Super_Pills` - this is a modified version of Painkillers, it will fill all of your stats, cure insomnia, fever, and venom/poison.
