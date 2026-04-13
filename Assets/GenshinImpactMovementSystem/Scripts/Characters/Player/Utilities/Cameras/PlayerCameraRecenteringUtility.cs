using Cinemachine;
using System;
using UnityEngine;

namespace GenshinImpactMovementSystem
{
    [Serializable]
    public class PlayerCameraRecenteringUtility
    {
        [field: SerializeField] public CinemachineVirtualCamera VirtualCamera { get; private set; }
        [field: SerializeField] public float DefaultHorizontalWaitTime { get; private set; } = 0f;
        [field: SerializeField] public float DefaultHorizontalRecenteringTime { get; private set; } = 4f;

        private CinemachinePOV cinemachinePOV;
        private float horizontalYawVelocity;

        public void Initialize()
        {
            cinemachinePOV = VirtualCamera.GetCinemachineComponent<CinemachinePOV>();
        }

        public void EnableRecentering(
            float waitTime = -1f,
            float recenteringTime = -1f,
            float baseMovementSpeed = 1f,
            float movementSpeed = 1f)
        {
            cinemachinePOV.m_HorizontalRecentering.m_enabled = true;
            cinemachinePOV.m_HorizontalRecentering.CancelRecentering();

            if (waitTime == -1f)
                waitTime = DefaultHorizontalWaitTime;

            if (recenteringTime == -1f)
                recenteringTime = DefaultHorizontalRecenteringTime;

            recenteringTime = recenteringTime * baseMovementSpeed / movementSpeed;

            cinemachinePOV.m_HorizontalRecentering.m_WaitTime = waitTime;
            cinemachinePOV.m_HorizontalRecentering.m_RecenteringTime = recenteringTime;
        }

        public void DisableRecentering()
        {
            cinemachinePOV.m_HorizontalRecentering.m_enabled = false;
        }

        public float GetCurrentYaw()
        {
            return cinemachinePOV.m_HorizontalAxis.Value;
        }

        public void SetYawImmediate(float yaw)
        {
            var axis = cinemachinePOV.m_HorizontalAxis;
            axis.Value = Mathf.Repeat(yaw, 360f);
            cinemachinePOV.m_HorizontalAxis = axis;
            cinemachinePOV.m_HorizontalRecentering.CancelRecentering();
        }

        public void SmoothSetYaw(float targetYaw, float smoothTime)
        {
            float currentYaw = GetCurrentYaw();

            float smoothedYaw = Mathf.SmoothDampAngle(
                currentYaw,
                targetYaw,
                ref horizontalYawVelocity,
                smoothTime
            );

            SetYawImmediate(smoothedYaw);
        }

        public void SnapBehindDirection(Vector3 forward)
        {
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                return;

            forward.Normalize();
            SetYawImmediate(Quaternion.LookRotation(forward, Vector3.up).eulerAngles.y);
        }
    }
}