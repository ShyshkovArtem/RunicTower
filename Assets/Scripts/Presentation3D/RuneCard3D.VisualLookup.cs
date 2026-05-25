using UnityEngine;

namespace RunicTower.Presentation3D
{
    public sealed partial class RuneCard3D
    {
        private Renderer[] GetEdgeTintRenderers()
        {
            if (edgeTintRenderers != null && edgeTintRenderers.Length > 0)
            {
                return edgeTintRenderers;
            }

            Transform tintRoot = edgeTintRoot != null ? edgeTintRoot : FindTintRoot(edgeTintChildName);
            return tintRoot != null ? tintRoot.GetComponentsInChildren<Renderer>(true) : null;
        }

        private Renderer[] GetBodyTintRenderers()
        {
            if (bodyTintRenderers != null && bodyTintRenderers.Length > 0)
            {
                return bodyTintRenderers;
            }

            Transform tintRoot = bodyTintRoot != null ? bodyTintRoot : FindTintRoot(bodyTintChildName);
            return tintRoot != null ? tintRoot.GetComponentsInChildren<Renderer>(true) : null;
        }

        private Transform FindTintRoot(string childName)
        {
            if (string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            Transform activeBodyRoot = GetActiveBodyRoot();
            if (activeBodyRoot == null)
            {
                return null;
            }

            return FindChildRecursive(activeBodyRoot, childName);
        }

        private Transform GetActiveBodyRoot()
        {
            if (smallBodyRoot != null && smallBodyRoot.activeInHierarchy)
            {
                return smallBodyRoot.transform;
            }

            if (mediumBodyRoot != null && mediumBodyRoot.activeInHierarchy)
            {
                return mediumBodyRoot.transform;
            }

            if (largeBodyRoot != null && largeBodyRoot.activeInHierarchy)
            {
                return largeBodyRoot.transform;
            }

            if (modifierBodyRoot != null && modifierBodyRoot.activeInHierarchy)
            {
                return modifierBodyRoot.transform;
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}

