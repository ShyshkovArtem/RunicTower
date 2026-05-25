using UnityEngine;

namespace RunicTower.Presentation3D
{
    public sealed class RuneSpot3D : MonoBehaviour
    {
        [SerializeField] private Transform anchor;
        [SerializeField] private GameObject emptyVisual;
        [SerializeField] private GameObject occupiedVisual;

        public Transform Anchor => anchor != null ? anchor : transform;

        public void SetOccupied(bool occupied)
        {
            if (emptyVisual != null)
            {
                emptyVisual.SetActive(!occupied);
            }

            if (occupiedVisual != null)
            {
                occupiedVisual.SetActive(occupied);
            }
        }
    }
}
