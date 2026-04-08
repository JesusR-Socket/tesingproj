using UnityEngine;

namespace GenshinImpactMovementSystem
{
    public class PlayerCombatIntentView : MonoBehaviour
    {
        [SerializeField] private PlayerCombatIntentController controller;
        [SerializeField] private Transform intentPoint;
        [SerializeField] private LineRenderer intentLine;
        [SerializeField] private LineRenderer triangleLine;
        [SerializeField] private float lineHalfLength = 0.35f;
        [SerializeField] private float triangleSize = 0.2f;
        [SerializeField] private float triangleForwardOffset = 0.1f;

        private Camera mainCamera;

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<PlayerCombatIntentController>();
            }

            mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (controller == null || intentPoint == null || intentLine == null || triangleLine == null)
            {
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            Vector3 triangleCenter = controller.GetTrianglePosition();
            Vector3 intentCenter = controller.IsAimHeld
                ? controller.GetIntentPosition()
                : controller.GetTrianglePosition();

            DrawTriangle(triangleCenter, controller.CharacterFacing);
            DrawIntentLine(intentCenter);

            intentPoint.position = intentCenter;

            bool showIntent = controller.IsAimHeld;
            intentPoint.gameObject.SetActive(showIntent);
            intentLine.gameObject.SetActive(showIntent);
        }

        private void DrawIntentLine(Vector3 center)
        {
            Vector3 right = Vector3.Cross(Vector3.up, controller.IntentFacing);

            if (right.sqrMagnitude < 0.0001f)
            {
                right = mainCamera != null ? mainCamera.transform.right : Vector3.right;
            }

            right.y = 0f;
            right.Normalize();

            intentLine.positionCount = 2;
            intentLine.SetPosition(0, center - right * lineHalfLength);
            intentLine.SetPosition(1, center + right * lineHalfLength);
        }

        private void DrawTriangle(Vector3 center, Vector3 forward)
        {
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 tip = center + forward * (triangleSize + triangleForwardOffset);
            Vector3 left = center - forward * triangleSize * 0.5f - right * triangleSize * 0.7f;
            Vector3 rightPoint = center - forward * triangleSize * 0.5f + right * triangleSize * 0.7f;

            triangleLine.positionCount = 4;
            triangleLine.SetPosition(0, tip);
            triangleLine.SetPosition(1, left);
            triangleLine.SetPosition(2, rightPoint);
            triangleLine.SetPosition(3, tip);
        }
    }
}