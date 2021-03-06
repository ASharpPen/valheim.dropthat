﻿using System.Collections.Generic;
using UnityEngine;
using Valheim.DropThat.Core;
using Valheim.DropThat.Reset;

namespace Valheim.DropThat.Locations
{
    internal static class LocationHelper
    {
        private static Dictionary<Vector2i, ZoneSystem.LocationInstance> _locationInstances { get; set; }
        private static Dictionary<Vector2i, SimpleLocation> _simpleLocationsByZone { get; set; }

        static LocationHelper()
        {
            StateResetter.Subscribe(() =>
            {
                _locationInstances = null;
                _simpleLocationsByZone = null;
            });
        }

        internal static void SetLocationInstances(Dictionary<Vector2i, ZoneSystem.LocationInstance> locations)
        {
            _locationInstances = locations;
        }

        internal static void SetLocations(IEnumerable<SimpleLocation> locations)
        {
            if (_simpleLocationsByZone is null)
            {
#if DEBUG
                Log.LogDebug($"Instantiating new dictionary for SimpleLocations.");
#endif
                _simpleLocationsByZone = new Dictionary<Vector2i, SimpleLocation>();
            }

#if DEBUG
            Log.LogDebug($"Assigning locations.");
#endif

            foreach (var location in locations)
            {
                _simpleLocationsByZone[location.ZonePosition] = location;
            }
        }

        public static SimpleLocation FindLocation(Vector3 position)
        {
            if (ZoneSystem.instance is null)
            {
                Log.LogWarning("Attempting to retrieve location before ZoneSystem is initialized.");
                return null;
            }

            var zoneId = ZoneSystem.instance.GetZone(position);

            if ((_locationInstances?.Count ?? 0) > 0)
            {
                if (_locationInstances.TryGetValue(zoneId, out ZoneSystem.LocationInstance defaultLocation))
                {
                    return new SimpleLocation
                    {
                        LocationName = defaultLocation.m_location?.m_prefabName ?? "",
                        Position = defaultLocation.m_position,
                        ZonePosition = zoneId,
                    };
                }
            }

            if (_simpleLocationsByZone is not null)
            {
                if (_simpleLocationsByZone.TryGetValue(zoneId, out SimpleLocation location))
                {
                    return location;
                }
            }

            return null;
        }
    }
}
