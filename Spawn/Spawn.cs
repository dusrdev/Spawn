using System;
using System.IO;
using UnityEngine;

public class Spawn : Mod
{
	public void Start()
	{
		Debug.Log("Mod Spawn has been loaded!");
	}

	// Exports the item ids to a text file on the desktop
	private static void ExportItemIds()
	{
		try
		{
			var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			var path = Path.Combine(desktopPath, "itemIds.txt");
			var itemIds = Enum.GetNames(typeof(Enums.ItemID));
			File.AppendAllLines(path, itemIds);
			Debug.Log("ItemIds exported to: " + path);
		}
		catch (Exception e)
		{
			Debug.Log("Error while exporting item ids: " + e.Message);
		}
	}


	[ConsoleCommand("spawn", "spawn <ItemID enum name or value>")]
	public static void Command(string[] args)
	{
		if (args.Length == 0)
		{
			return;
		}

		var itemIdString = args[0];

		if (itemIdString == "help")
		{
			ExportItemIds();
			return;
		}

		var itemDoesNotExist = ItemsManager.Get().StringToItemID(itemIdString) == -1;
		if (itemDoesNotExist)
		{
			Debug.Log("ItemID does not exist: " + itemIdString);
			return;
		}

		Item item = ItemsManager.Get().CreateItem(itemIdString, false);

		if (item == null)
		{
			Debug.Log("ItemID does not exist: " + itemIdString);
		}

		if (item.m_Info.m_CanBeAddedToInventory)
		{
			Debug.Log("Spawning in inventory: " + itemIdString);
			InventoryBackpack.Get().InsertItem(item, null, null, true, true, true, true, true);
			return;
		}
		Debug.Log("Spawning at player: " + itemIdString);
		var playerTransform = Player.Get().transform;
		item.transform.position = playerTransform.position;
		item.transform.rotation = playerTransform.rotation;
		item.ItemsManagerRegister(true);
	}

	public void OnModUnload()
	{
		Debug.Log("Mod Spawn has been unloaded!");
	}
}