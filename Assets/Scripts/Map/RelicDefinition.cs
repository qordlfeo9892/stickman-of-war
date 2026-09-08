using System.Collections.Generic;
using UnityEngine;

namespace StickmanOfWar.Map
{
    public class RelicDefinition
    {
        public string Id;
        public string DisplayName;
        public string EffectDescription;
        public List<Vector2Int> Shape;
        public Color IconColor;

        public RelicDefinition(string id, string displayName, string effectDescription, List<Vector2Int> shape, Color iconColor)
        {
            Id = id;
            DisplayName = displayName;
            EffectDescription = effectDescription;
            Shape = shape;
            IconColor = iconColor;
        }
    }
}
