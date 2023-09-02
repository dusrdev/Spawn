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

## NOTES

To submit bug reports and or feature requests, find me on the GreenHellMods [discord](https://greenhellmodding.com/discord) server, or contact me on [telegram](https://t.me/dsr47)
