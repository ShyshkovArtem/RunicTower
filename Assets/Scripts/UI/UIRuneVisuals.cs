using UnityEngine;
using RunicTower.Core;

namespace RunicTower.UI
{
    public static class UIRuneVisuals
    {
        public static Color GetElementColor(ElementType element)
        {
            return element switch
            {
                ElementType.Fire => new Color(0.96f, 0.25f, 0.14f),
                ElementType.Air => new Color(0.24f, 0.82f, 0.33f),
                ElementType.Earth => new Color(0.63f, 0.40f, 0.18f),
                ElementType.Water => new Color(0.16f, 0.58f, 0.98f),
                _ => Color.white
            };
        }
    }
}
