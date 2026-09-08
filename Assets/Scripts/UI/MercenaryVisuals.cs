using System.Collections.Generic;
using UnityEngine;

namespace StickmanOfWar.UI
{
    public static class MercenaryVisuals
    {
        private static readonly Dictionary<int, Color> TierColors = new Dictionary<int, Color>
        {
            { 1, new Color(0.95f, 0.95f, 0.95f) },
            { 2, new Color(0.35f, 0.75f, 0.35f) },
            { 3, new Color(0.30f, 0.55f, 0.95f) },
            { 4, new Color(0.70f, 0.35f, 0.90f) },
            { 5, new Color(0.95f, 0.70f, 0.15f) },
        };

        public static Color GetTierColor(int tier)
        {
            return TierColors.TryGetValue(tier, out Color color) ? color : Color.white;
        }
    }
}
