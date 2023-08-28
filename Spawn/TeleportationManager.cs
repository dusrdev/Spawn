using System.Net;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using UnityEngine;
using static SpawnExtensions;

namespace SpawnComponents
{
	public static class TeleportationManager
	{
		// Teleports the player to the specified coordinates
		public static void Teleport(ArraySegment<string> args)
		{
			if (args.Count == 2 && SavedLocationsManager.TryGetLocation(args[1], out Vector3 newPos))
			{
				TeleportInternal(newPos);
				return;
			}

			if (args.Count < 3)
			{
				LogMessage("Teleport requires additional arguments: [optional=offset] [latitude] [longitude]");
				return;
			}

			if (args.Count == 3)
			{
				TeleportToCoordinates(args[1], args[2]);
				return;
			}

			if (Equals(args[1], "offset"))
			{
				TeleportOffset(args[2], args[3]);
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
			float positionLat = (latitude - 57f) * latModifier * -1f + latModifier * 0.5f;
			float positionLong = (longitude - 64f) * longModifier * -1f + longModifier * 0.5f;
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
			var rotation = player.transform.rotation;
			player.TeleportTo(position, rotation, false);
			LogMessage(string.Format("Teleported to: {0}", position));
		}

		public static void AddSavedLocation(ArraySegment<string> args)
		{
			if (args.Count < 2)
			{
				LogMessage("Save location requires additional arguments: [locationName] (optional)[remove]");
				return;
			}

			if (args.Count == 3 && Equals(args[2], "remove"))
			{
				LogMessage(SavedLocationsManager.AddLocation(args[1], Vector3.zero));
				return;
			}

			if (Equals(args[1], "list"))
			{
				LogMessage(SavedLocationsManager.ListSavedLocations());
				return;
			}

			var player = Player.Get();
			var position = player.transform.position;
			LogMessage(SavedLocationsManager.AddLocation(args[1], position));
		}
	}
}