using UnityEngine;

namespace RunicTower.Presentation3D
{
    public sealed partial class RuneCard3D
    {
        private void CacheInteractionColliderShape()
        {
            if (interactionCollider == null)
            {
                return;
            }

            _boxInteractionCollider = interactionCollider as BoxCollider;
            _sphereInteractionCollider = interactionCollider as SphereCollider;
            _capsuleInteractionCollider = interactionCollider as CapsuleCollider;

            if (_boxInteractionCollider != null)
            {
                _baseBoxColliderSize = _boxInteractionCollider.size;
                _baseBoxColliderCenter = _boxInteractionCollider.center;
            }

            if (_sphereInteractionCollider != null)
            {
                _baseSphereColliderRadius = _sphereInteractionCollider.radius;
                _baseSphereColliderCenter = _sphereInteractionCollider.center;
            }

            if (_capsuleInteractionCollider != null)
            {
                _baseCapsuleColliderRadius = _capsuleInteractionCollider.radius;
                _baseCapsuleColliderHeight = _capsuleInteractionCollider.height;
                _baseCapsuleColliderCenter = _capsuleInteractionCollider.center;
            }
        }

        private void SyncInteractionColliderScale(Vector3 visualScale)
        {
            if (interactionCollider == null)
            {
                return;
            }

            Vector3 handVisualScale = GetHandVisualScale();
            Vector3 scaleRatio = new(
                SafeDivide(visualScale.x, handVisualScale.x),
                SafeDivide(visualScale.y, handVisualScale.y),
                SafeDivide(visualScale.z, handVisualScale.z));

            if (_boxInteractionCollider != null)
            {
                _boxInteractionCollider.size = Vector3.Scale(_baseBoxColliderSize, scaleRatio);
                _boxInteractionCollider.center = Vector3.Scale(_baseBoxColliderCenter, scaleRatio);
            }

            if (_sphereInteractionCollider != null)
            {
                float uniformRatio = Mathf.Max(scaleRatio.x, scaleRatio.y, scaleRatio.z);
                _sphereInteractionCollider.radius = _baseSphereColliderRadius * uniformRatio;
                _sphereInteractionCollider.center = Vector3.Scale(_baseSphereColliderCenter, scaleRatio);
            }

            if (_capsuleInteractionCollider != null)
            {
                int direction = _capsuleInteractionCollider.direction;
                float heightRatio = direction switch
                {
                    0 => scaleRatio.x,
                    1 => scaleRatio.y,
                    2 => scaleRatio.z,
                    _ => 1f
                };

                float radiusRatio = direction switch
                {
                    0 => Mathf.Max(scaleRatio.y, scaleRatio.z),
                    1 => Mathf.Max(scaleRatio.x, scaleRatio.z),
                    2 => Mathf.Max(scaleRatio.x, scaleRatio.y),
                    _ => 1f
                };

                _capsuleInteractionCollider.height = _baseCapsuleColliderHeight * heightRatio;
                _capsuleInteractionCollider.radius = _baseCapsuleColliderRadius * radiusRatio;
                _capsuleInteractionCollider.center = Vector3.Scale(_baseCapsuleColliderCenter, scaleRatio);
            }
        }
    }
}

