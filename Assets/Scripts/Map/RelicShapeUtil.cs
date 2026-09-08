using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StickmanOfWar.Map
{
    public static class RelicShapeUtil
    {
        public static List<Vector2Int> Rotate(List<Vector2Int> shape, int steps)
        {
            List<Vector2Int> current = shape;
            int normalizedSteps = ((steps % 4) + 4) % 4;

            for (int i = 0; i < normalizedSteps; i++)
            {
                current = current.Select(c => new Vector2Int(c.y, -c.x)).ToList();
            }

            int minX = current.Min(c => c.x);
            int minY = current.Min(c => c.y);
            return current.Select(c => new Vector2Int(c.x - minX, c.y - minY)).ToList();
        }
    }
}
