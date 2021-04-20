﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Valheim.DropThat.Core;

namespace Valheim.DropThat.Caches
{
    /// <summary>
    /// Temporary caching for carrying config information between CharacterDrop.GenerateDropList and CharacterDrop.DropItems.
    /// </summary>
    internal class TempDropListCache
    {
        private static ConditionalWeakTable<object, TempDropListCache> DropListTable = new ConditionalWeakTable<object, TempDropListCache>();

        public static void SetDrop(object key, int index, DropExtended extended)
        {
#if DEBUG
            Log.LogDebug($"Setting temp drop cache {index}:{key.GetHashCode()}");
#endif

            var cache = DropListTable.GetOrCreateValue(key);
            cache.ConfigByIndex[index] = extended;
        }

        public static TempDropListCache GetDrops(object key)
        {
            if (DropListTable.TryGetValue(key, out TempDropListCache cache))
            {
                return cache;
            }

            return null;
        }

        public static DropExtended GetDrop(object key, int index)
        {
            if(DropListTable.TryGetValue(key, out TempDropListCache cache))
            {
#if DEBUG
                Log.LogDebug($"Found temp drop cache for {key.GetHashCode()}");
#endif

                if (cache.ConfigByIndex.TryGetValue(index, out DropExtended extended))
                {
#if DEBUG
                    Log.LogDebug($"Successfully retrieved config from temp drop cache");
#endif

                    return extended;
                }
            }

            return null;
        }

        public Dictionary<int, DropExtended> ConfigByIndex = new Dictionary<int, DropExtended>();
    }
}
