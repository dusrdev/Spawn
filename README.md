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
