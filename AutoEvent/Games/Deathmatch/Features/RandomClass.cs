using System.Linq;
using AutoEvent.API;
using UnityEngine;

namespace AutoEvent.Games.Deathmatch
{
    internal class RandomClass
    {
        public static Vector3 GetRandomPosition(MapObject GameMap)
        {
            if (GameMap is null)
            {
                DebugLogger.LogDebug("Карта не загружена.");
                return Vector3.zero;
            }

            if (GameMap.AttachedBlocks is null)
            {
                DebugLogger.LogDebug("Список блоков карты пуст.");
                return Vector3.zero;
            }

            var spawnpoint = GameMap.AttachedBlocks.Where(x => x.name == "Spawnpoint").ToList().RandomItem();
            if (spawnpoint is null)
            {
                DebugLogger.LogDebug("Точка появления не найдена.");
                return Vector3.zero;
            }

            return spawnpoint.transform.position;
        }
    }
}
