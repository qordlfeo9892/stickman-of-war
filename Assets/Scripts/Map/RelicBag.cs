using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StickmanOfWar.Map
{
    public class PlacedRelic
    {
        public string InstanceId;
        public string RelicId;
        public int X;
        public int Y;
        public int Rotation;
    }

    public class RelicBag
    {
        public int Width = 3;
        public int Height = 3;
        public List<PlacedRelic> Placed = new List<PlacedRelic>();

        public List<Vector2Int> GetCells(RelicDefinition def, int x, int y, int rotation)
        {
            List<Vector2Int> rotated = RelicShapeUtil.Rotate(def.Shape, rotation);
            return rotated.Select(c => new Vector2Int(c.x + x, c.y + y)).ToList();
        }

        public bool CanPlace(RelicDefinition def, int x, int y, int rotation, string excludeInstanceId = null)
        {
            List<Vector2Int> cells = GetCells(def, x, y, rotation);

            foreach (Vector2Int cell in cells)
            {
                if (cell.x < 0 || cell.y < 0 || cell.x >= Width || cell.y >= Height) return false;
            }

            foreach (PlacedRelic other in Placed)
            {
                if (other.InstanceId == excludeInstanceId) continue;
                RelicDefinition otherDef = RelicDatabase.GetById(other.RelicId);
                List<Vector2Int> otherCells = GetCells(otherDef, other.X, other.Y, other.Rotation);
                if (cells.Any(c => otherCells.Contains(c))) return false;
            }

            return true;
        }

        public PlacedRelic Place(RelicDefinition def, int x, int y, int rotation)
        {
            var placed = new PlacedRelic
            {
                InstanceId = System.Guid.NewGuid().ToString("N"),
                RelicId = def.Id,
                X = x,
                Y = y,
                Rotation = rotation,
            };
            Placed.Add(placed);
            return placed;
        }

        public void Remove(string instanceId)
        {
            Placed.RemoveAll(p => p.InstanceId == instanceId);
        }
    }
}
