# Version 1.5

* Spawn item and remove item syntax changed to pave ground for adding more commands:
  * To spawn use the keyword `Get`, it will no longer be inferred instead of command name.
  `Get [itemId/SpecialItemName/Alias] [Quantity(Default=1)]`
  * To remove use the keyword `Remove`, since "rm" will no longer be needed the [debug/quantity] parameter comes right after [itemId/alias].
  `Remove [itemId/Alias] [maxDistance/Debug]`
* New Command - `FillLiquid [liquidType] [Capacity(Optional)]`. This command will fill all liquid non-bowl containers in the backpack with the liquid and amount requested
  * All `LiquidType`s were added to the help text, you can re-export help using the `help` command to see them.
  * If left out, `Capacity` will default to the size of the container, I.E Bidon=100, Coconut_Bidon=40 and so on...
  * `Capacity` parameter can fill beyond the capacity of the container, for example you can use 1000 in the capacity and you will get 1000 units of liquid. The only caveat is **more liquid = more weight**
* `Teleportation` no longer features a loading screen and should be more stable in accounting for environment features
* New **SpecialItems**:
  * `Stormbreaker` a modified metal_axe
* New Command - `CompleteConstructions` - this will complete all placed constructions
* New Command - `RemoveConstructions [maxDistance(Default=10)]` - this will remove all constructions within the distance
  * If `maxDistance` is not specified it will default to 10.
* New Command - `EndlessFires`
  * This command is a toggle
  * All fires within the distance of 5 will be ignited and endless (require no fuel and can't be distinguished). Calling the command again will extinguish and restore those fires to normal.
  * Upon saving, if any firecamp was turned to "endless" it will extinguished to preserve its construction. **FOR THIS TO HAPPEN MAKE SURE THE MOD IS STILL LOADED WHILE SAVING**.
* New Command - `UnlockMaps` - this command will give the player all available maps and unlock all the locations on them.
* Codebase restructured to allow better maintainability and make it easier to add functionality.
* New Command `LighterBackpack [true/false]` - this command will set the max backpack weight to 999, if you specify [true/false] the setting will be remember and re-activate automatically upon loading the mod. If not it will be turned on manually.
* Changed Command `RestoreSpecialItems [true/false]` - like the previous command, now [true/false] parameter can be used to save the state and it will be automatically turned on once the mod is loaded.

## NOTES

To submit bug reports and or feature requests, find me on the GreenHellMods [discord](https://greenhellmodding.com/discord) server, or contact me on [telegram](https://t.me/dsr47)
