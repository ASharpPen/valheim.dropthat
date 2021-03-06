﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Valheim.DropThat.Core;
using Valheim.DropThat.Reset;

namespace Valheim.DropThat.Locations
{
    [HarmonyPatch(typeof(ZNet))]
    internal static class ZoneSystemMultiplayerPatch
    {
        private static FieldInfo _zoneSystemLocations = AccessTools.Field(typeof(ZoneSystem), "m_locationInstances");

        private static bool HaveReceivedLocations = false;

        static ZoneSystemMultiplayerPatch()
        {
            StateResetter.Subscribe(() =>
            {
                HaveReceivedLocations = false;
            });
        }

        [HarmonyPatch("OnNewConnection")]
        [HarmonyPostfix]
        private static void TransferLocationData(ZNet __instance, ZNetPeer peer)
        {
            if (ZNet.instance.IsServer())
            {
                Log.LogDebug("Registering server RPC for sending location data on request from client.");
                peer.m_rpc.Register(nameof(RPC_RequestLocationsDropThat), new ZRpc.RpcMethod.Method(RPC_RequestLocationsDropThat));
            }
            else
            {
                Log.LogDebug("Registering client RPC for receiving location data from server.");
                peer.m_rpc.Register(nameof(RPC_ReceiveLocationsDropThat), new Action<ZRpc, ZPackage>(RPC_ReceiveLocationsDropThat));

                Log.LogDebug("Requesting location data from server.");
                peer.m_rpc.Invoke(nameof(RPC_RequestLocationsDropThat));
            }
        }

        private static void RPC_RequestLocationsDropThat(ZRpc rpc)
        {
            try
            {
                if (!ZNet.instance.IsServer())
                {
                    Log.LogWarning("Non-server instance received request for location data. Ignoring request.");
                    return;
                }

                Log.LogInfo($"Sending location data.");

                ZPackage package = new ZPackage();

                var locations = _zoneSystemLocations.GetValue(ZoneSystem.instance) as Dictionary<Vector2i, ZoneSystem.LocationInstance>;

                if (locations is null)
                {
                    Log.LogWarning("Unable to get locations from zonesystem to send to client.");
                    return;
                }

                package.Write(SerializeLocationInfo(locations));

                Log.LogDebug("Sending locations package.");

                rpc.Invoke(nameof(RPC_ReceiveLocationsDropThat), new object[] { package });

                Log.LogDebug("Finished sending locations package.");
            }
            catch (Exception e)
            {
                Log.LogError("Unexpected error while attempting to create and send locations package from server to client.", e);
            }
        }

        private static void RPC_ReceiveLocationsDropThat(ZRpc rpc, ZPackage pkg)
        {
            Log.LogDebug("Received locations package.");
            try
            {
                if (HaveReceivedLocations)
                {
                    Log.LogDebug("Already received locations previously. Skipping.");
                    return;
                }

                var serialized = pkg.ReadByteArray();

                LoadLocationInfo(serialized);
                HaveReceivedLocations = true;

                Log.LogDebug("Successfully received locations package.");
            }
            catch (Exception e)
            {
                Log.LogError("Error while attempting to read received locations package.", e);
            }
        }

        private static void LoadLocationInfo(byte[] serialized)
        {
            Log.LogTrace($"Deserializing {serialized.Length} bytes of location data.");

            using (MemoryStream memStream = new MemoryStream(serialized))
            {
                using (var zipStream = new GZipStream(memStream, CompressionMode.Decompress, true))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    var responseObject = binaryFormatter.Deserialize(zipStream);

                    if (responseObject is SimpleLocationPackage package)
                    {
                        var locations = package.Unpack();

                        Log.LogDebug($"Deserialized {locations.Count} locations.");

                        LocationHelper.SetLocations(locations);
#if DEBUG
                        Log.LogDebug($"Assigning locations: " + locations.Select(x => x.LocationName).Distinct().Join());
#endif
                    }
                }
            }
        }

        private static byte[] SerializeLocationInfo(Dictionary<Vector2i, ZoneSystem.LocationInstance> locationInstances)
        {
#if DEBUG
            Log.LogDebug($"Serializing {locationInstances.Count} location instances.");
#endif

            SimpleLocationPackage package = new SimpleLocationPackage(locationInstances);

            using (MemoryStream memStream = new MemoryStream())
            {
                using (var zipStream = new GZipStream(memStream, CompressionLevel.Optimal))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(zipStream, package);
                }

                byte[] serializedLocations = memStream.ToArray();

                Log.LogDebug($"Serialized {serializedLocations.Length} bytes of locations.");
                return serializedLocations;
            }
        }
    }
}
