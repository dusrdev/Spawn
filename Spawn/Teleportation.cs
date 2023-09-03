using System;
using UnityEngine;
using static SpawnMod.SpawnExtensions;

namespace SpawnMod
{
	public static class Teleportation
	{
		// Teleports the player to the specified coordinates
		// teleport [latitude] [longitude]
		// teleport offset [latitude] [longitude]
		// teleport [locationName]
		public static void Teleport(ArraySegment<string> args)
		{
			if (args.Count == 1 && SavedLocationsManager.TryGetLocation(args[0], out Vector3 newPos))
			{
				TeleportInternal(newPos);
				return;
			}

			if (args.Count < 2)
			{
				LogMessage("Teleport requires additional arguments: [optional=offset] [latitude] [longitude]");
				return;
			}

			// Regular teleportation to coordinates
			if (args.Count == 2)
			{
				TeleportToCoordinates(args[0], args[1]);
				return;
			}

			// Teleportation with offset
			if (Equals(args[0], "offset"))
			{
				TeleportOffset(args[1], args[2]);
				return;
			}

			LogMessage("Teleportation arguments invalid.");
		}

		private static void TeleportToCoordinates(string @lat, string @long)
		{
			if (!float.TryParse(@lat, out float latitude))
			{
				LogMessage(string.Format("Invalid argument for 'latitude': {0}", @lat));
				return;
			}

			if (!float.TryParse(@long, out float longitude))
			{
				LogMessage(string.Format("Invalid argument for 'longitude': {0}", @long));
				return;
			}

			// calculate new position
			const float latModifier = 40.8185f;
			const float longModifier = 36.4861f;
			float positionLat = ((latitude - 57f) * latModifier * -1f) + (latModifier * 0.5f);
			float positionLong = ((longitude - 64f) * longModifier * -1f) + (longModifier * 0.5f);
			var newPos = new Vector3(positionLat, 5f, positionLong);

			// Teleport player
			TeleportInternal(newPos);
		}

		private static void TeleportOffset(string @lat, string @long)
		{
			if (!float.TryParse(@lat, out float latitude))
			{
				LogMessage(string.Format("Invalid argument for 'latitude': {0}", @lat));
				return;
			}

			if (!float.TryParse(@long, out float longitude))
			{
				LogMessage(string.Format("Invalid argument for 'longitude': {0}", @long));
				return;
			}

			// calculate new position
			var player = Player.Get();
			var position = player.transform.position;
			var newPos = new Vector3(position.x + latitude, 5f, position.z + longitude);

			// Teleport player
			TeleportInternal(newPos);
		}

		private static void TeleportInternal(Vector3 position)
		{
			var player = Player.Get();
			player.Reposition(position);
			LogMessage(string.Format("Teleported to: {0}", position));
		}

		// Saves the current player position to a file
		// saveLocation list
		// saveLocation [locationName]
		// saveLocation [locationName] remove
		public static void AddSavedLocation(ArraySegment<string> args)
		{
			if (args.Count < 1)
			{
				LogMessage("Save location requires additional arguments: [locationName] ?[remove]");
				return;
			}

			// remove location
			if (args.Count == 2 && Equals(args[1], "remove"))
			{
				LogMessage(SavedLocationsManager.AddLocation(args[0], Vector3.zero));
				return;
			}

			// list saved locations
			if (Equals(args[0], "list"))
			{
				LogMessage(SavedLocationsManager.ListSavedLocations());
				return;
			}

			// save location
			var player = Player.Get();
			var position = player.transform.position;
			LogMessage(SavedLocationsManager.AddLocation(args[0], position));
		}
	}
}