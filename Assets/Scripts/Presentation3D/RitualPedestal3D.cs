using UnityEngine;

namespace RunicTower.Presentation3D
{
    public sealed class RitualPedestal3D : MonoBehaviour
    {
        [SerializeField] private RuneSpot3D[] elementalSpots;
        [SerializeField] private RuneSpot3D modifierSpot;

        public int ElementalSpotCount => elementalSpots?.Length ?? 0;

        public RuneSpot3D GetElementalSpot(int index)
        {
            if (elementalSpots == null || index < 0 || index >= elementalSpots.Length)
            {
                return null;
            }

            return elementalSpots[index];
        }

        public RuneSpot3D GetModifierSpot()
        {
            return modifierSpot;
        }

        public void ClearOccupancy()
        {
            if (elementalSpots != null)
            {
                foreach (RuneSpot3D spot in elementalSpots)
                {
                    spot?.SetOccupied(false);
                }
            }

            modifierSpot?.SetOccupied(false);
        }
    }
}
