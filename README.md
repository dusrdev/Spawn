# Spawn - Mod for Green Hell

The mod onces loaded allows you to use the console to spawn any item by its Id.

It works with the latest updates

## Usage

* **spawn help** - this will export a list of all available item ids to the desktop so you could use to reference
* **spawn [id]** - this will spawn the item with the corresponding [id]

## v1.1

* Quantity parameter was added:

**spawn [id] [quantity]** - spawn multiple instances of the item with the [id]

* Fixed bug where each help request would append to the previous file instead of overwriting

## v1.1.1

* Fixed bug where outputting text would cause an exception if quantity parameter is not provided. Now it will properly default to 1 if quantity is omitted

## v1.2

### Added Remove Function

Spawned an item that can't be used, moved or destroyed? you are no longer stuck with it and forced to create a new save or change location.

**spawn [id] rm [distance]** - command will remove item from the game, the distance can be omitted and will default to 5 (which is visually similar to 3-5 meters/yards)

#### Debug Option

**spawn [id] rm debug** - command will export information to a file on the desktop called "rmDebug.txt", this file will contain the requested item id, the player position, and all the found items positions and distance relative to the player.

* As it is made for debugging, executing this command will not actually remove any of the items.

### Added Second Debug Output Location

**spawn log** - will activate secondary log location, in addition to the debug console, everything will be logged to a file name "SpawnLog.txt" on the desktop.

* This is essentially a switch, you call it once, it will activate, call again to deactivate and so on. While it is on, it will continue logging to the file on the desktop until turned off / mod unloaded / game exited
