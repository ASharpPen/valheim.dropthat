﻿using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Valheim.DropThat.Core;

namespace Valheim.DropThat.Debugging
{
    internal static class LocationsFileWriter
    {
        private const string FileName = "drop_that_locations.txt";

        public static void WriteToList(List<ZoneSystem.ZoneLocation> zoneLocations)
        {
            HashSet<string> printedLocations = new();
            StringBuilder stringBuilder = new ();
            Dictionary<Heightmap.Biome, List<string>> locationsByBiome = new();

            foreach(var location in zoneLocations)
            {
                var biomes = SplitBiome(location.m_biome);

                foreach(var biome in biomes)
                {
                    List<string> biomeLocations;

                    if (!locationsByBiome.TryGetValue(biome, out biomeLocations))
                    {
                        locationsByBiome[biome] = biomeLocations = new();
                    }

                    biomeLocations.Add(location.m_prefabName);
                }
            }

            foreach (var biome in locationsByBiome.OrderBy(x => x.Key))
            {
                var currentBiome = biome.Key.ToString();

                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"[{currentBiome}]");

                foreach (var location in biome.Value)
                {
                    var locationKey = location + "." + currentBiome;

                    if (!printedLocations.Contains(locationKey))
                    {
                        stringBuilder.AppendLine(location);
                        printedLocations.Add(locationKey);
                    }
                }
            }

            string filePath = Path.Combine(Paths.PluginPath, FileName);

            Log.LogInfo($"Writing locations to {filePath}");

            File.WriteAllText(filePath, stringBuilder.ToString());
        }

        private static List<Heightmap.Biome> SplitBiome(Heightmap.Biome bitmaskedBiome)
        {
            List<Heightmap.Biome> results = new();

            foreach(var b in Enum.GetValues(typeof(Heightmap.Biome)))
            {
                if(b is Heightmap.Biome biome && biome != Heightmap.Biome.BiomesMax)
                {
                    if (biome == 0 && bitmaskedBiome == 0)
                    {
                        results.Add(biome);
                    }
                    else if(biome != Heightmap.Biome.None)
                    {
                        var filteredBiome = biome & bitmaskedBiome;

                        if (filteredBiome > 0)
                        {
                            results.Add(filteredBiome);
                        }
                    }
                }
            }

            return results;
        }
    }
}
