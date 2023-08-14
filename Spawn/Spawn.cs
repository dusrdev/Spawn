using System.Net;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using UnityEngine;

public class Spawn : Mod
{
	public void Start()
	{
		Debug.Log("Mod Spawn has been loaded!");
	}

	private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

	// Exports the item ids to a text file on the desktop
	private static void ExportItemIds()
	{
		try
		{
			var path = Path.Combine(DesktopPath, "itemIds.txt");
			var itemIds = Enum.GetNames(typeof(Enums.ItemID));
			File.WriteAllLines(path, itemIds);
			Log("ItemIds exported to: " + path);
		}
		catch (Exception e)
		{
			Log("Error while exporting item ids: " + e.Message);
		}
	}


	[ConsoleCommand("spawn", "spawn [ID/help/log] [Quantity/rm] [MaxDistance/debug]")]
	public static void Command(string[] args)
	{
		if (args.Length == 0)
		{
			return;
		}

		if (args[0] == "help")
		{
			ExportItemIds();
			return;
		}

		if (args[0] == "log") {
			_logToDesktop = !_logToDesktop;
			return;
		}

		if (ItemsManager.Get().StringToItemID(args[0]) == -1)
		{
			Log("ItemID does not exist: " + args[0]);
			return;
		}

		var segment = new ArraySegment<string>(args);
		if (segment.Count > 1 && args[1] == "rm")
		{
			RemoveItem(segment);
		}
		else
		{
			SpawnItem(segment);
		}
	}

	private static void SpawnItem(ArraySegment<string> args)
	{
		var quantity = 1;

		if (args.Count > 1 && !int.TryParse(args[1], out quantity))
		{
			Log("Invalid amount: " + args[1]);
			return;
		}

		var item = ItemsManager.Get().CreateItem(args[0], false);

		if (item.m_Info.m_CanBeAddedToInventory)
		{
			Log("Spawning \"" + args[0] + "\" in inventory");
			for (var i = 0; i < quantity; i++)
			{
				if (item == null)
				{
					item = ItemsManager.Get().CreateItem(args[0], false);
				}
				InventoryBackpack.Get().InsertItem(item, null, null, true, true, true, true, true);
				item = null;
			}
			return;
		}

		Log("Spawning at player: " + args[0]);
		var playerTransform = Player.Get().transform;
		item.transform.position = playerTransform.position;
		item.transform.rotation = playerTransform.rotation;
		item.ItemsManagerRegister(true);
	}

	public static void RemoveItem(ArraySegment<string> args)
	{
		Enums.ItemID enumItemId;
		Enum.TryParse(args[0], true, out enumItemId);

		float maxDistance = 5f;
		bool debug = false;

		if (args.Count > 2)
		{
			if (args[2] == "debug")
			{
				debug = true;
			}
			else {
				var isValid = float.TryParse(args[2], out maxDistance);
				if (!isValid) {
					Log("Invalid distance: " + args[2]);
					return;
				}
			}
		}

		var playerTransform = Player.Get().transform;
		var playerPosition = playerTransform.position;

		var sb = new StringBuilder();
		sb.Append("ItemID: ")
		  .AppendLine(enumItemId.ToString())
		  .Append("MaxDistance: ")
		  .AppendLine(maxDistance.ToString())
		  .Append("Debug: ")
		  .AppendLine(debug.ToString())
		  .AppendLine()
		  .Append("PlayerPosition: ")
		  .AppendLine(playerPosition.ToString())
		  .AppendLine();

		List<Item> items = new List<Item>();
		foreach (var item in Item.s_AllItems)
		{
			if (item.m_Info.m_ID != enumItemId)
			{
				continue;
			}

			var itemPosition = item.transform.position;
			var distance = Vector3.Distance(playerPosition, itemPosition);

			if (debug)
			{
				sb.Append("ItemPosition: ")
				  .AppendLine(itemPosition.ToString())
				  .Append("Distance: ")
				  .AppendLine(distance.ToString());
			}

			if (distance > maxDistance)
			{
				continue;
			}
			items.Add(item);
		}

		if (debug)
		{
			var path = Path.Combine(DesktopPath, "rmDebug.txt");
			File.WriteAllText(path, sb.ToString());
		}

		while (!debug && items.Count > 0)
		{
			var item = items[0];
			items.RemoveAt(0);
			item.m_Info.m_CanBeRemovedFromInventory = true;
			item.m_Info.m_DestroyByItemsManager = true;
			item.m_Info.m_CantDestroy = false;
			ItemsManager.Get().AddItemToDestroy(item);
		}
	}

	private static readonly string _logPath = Path.Combine(DesktopPath, "SpawnLog.txt");

	private static bool _logToDesktop = false;

	public static void Log(string message)
	{
		Debug.Log(message);
		if (!_logToDesktop)
		{
			return;
		}
		var time = DateTime.Now.ToString();
		var output = string.Format("[{0}] {1}{2}", time, message, Environment.NewLine);
		File.AppendAllText(_logPath, output);
	}

	public void OnModUnload()
	{
		Log("Mod Spawn has been unloaded!");
	}
}