using System.Collections;
using UnityEngine;

namespace RunicTower.Presentation3D
{
    public sealed partial class RuneCard3D
    {
        private void StartIdleAnimation()
        {
            Transform idleTarget = GetIdleTarget();
            if (idleTarget == null || _idleRoutine != null)
            {
                return;
            }

            _idleRoutine = StartCoroutine(PlayIdleAnimation(idleTarget));
        }

        private void StopIdleAnimation()
        {
            if (_idleRoutine != null)
            {
                StopCoroutine(_idleRoutine);
                _idleRoutine = null;
            }

            ResetIdlePose();
        }

        private IEnumerator PlayIdleAnimation(Transform idleTarget)
        {
            while (true)
            {
                float time = Time.time + _idleSeed;
                Vector3 positionAmplitude = GetCurrentIdlePositionAmplitude();
                Vector3 rotationAmplitude = GetCurrentIdleRotationAmplitude();
                float positionFrequency = GetCurrentIdlePositionFrequency();
                float rotationFrequency = GetCurrentIdleRotationFrequency();

                Vector3 positionOffset = new Vector3(
                    Mathf.Sin(time * positionFrequency * 0.83f) * positionAmplitude.x,
                    Mathf.Sin(time * positionFrequency * 1.21f) * positionAmplitude.y,
                    Mathf.Cos(time * positionFrequency) * positionAmplitude.z);

                Vector3 rotationOffset = new Vector3(
                    Mathf.Sin(time * rotationFrequency * 0.71f) * rotationAmplitude.x,
                    Mathf.Cos(time * rotationFrequency) * rotationAmplitude.y,
                    Mathf.Sin(time * rotationFrequency * 1.17f) * rotationAmplitude.z);

                idleTarget.localPosition = _visualBaseLocalPosition + positionOffset;
                idleTarget.localRotation = _visualBaseLocalRotation * Quaternion.Euler(rotationOffset);
                yield return null;
            }
        }

        private void ResetIdlePose()
        {
            Transform idleTarget = GetIdleTarget();
            if (idleTarget == null)
            {
                return;
            }

            idleTarget.localPosition = _visualBaseLocalPosition;
            idleTarget.localRotation = _visualBaseLocalRotation;
        }

        private Transform GetIdleTarget()
        {
            return visualRoot != null ? visualRoot : transform;
        }
    }
}

