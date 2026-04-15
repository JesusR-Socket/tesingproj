using UnityEngine;
using UnityEngine.InputSystem;

namespace GenshinImpactMovementSystem
{
    public class PlayerCombatIntentController : MonoBehaviour
    {
        [Header("Target Mode")]
        [SerializeField] private float targetModeCameraBehindSmoothTime = 0.07f;
        [SerializeField] private float targetModeTurnSmoothTime = 0.05f;

        [Header("Intent Raycast")]
        [SerializeField] private LayerMask aimSurfaceLayers = ~0;
        [SerializeField] private float maxAimDistance = 60f;

        [Header("Marker Placement")]
        [SerializeField] private float markerDistance = 2.2f;
        [SerializeField] private float markerHeight = 1.7f;
        [SerializeField] private float verticalIntentVisualScale = 0.12f;

        public bool IsTargetModeHeld { get; private set; }

        public Vector3 CharacterFacing { get; private set; }
        public Vector3 IntentFacing { get; private set; }
        public Vector3 IntentWorldPoint { get; private set; }
        public float VerticalIntentDelta { get; private set; }

        private Player player;
        private InputAction targetModeAction;

        private bool nextAttackUsesCommitAttack;
        private float targetModeYawVelocity;

        private void Awake()
        {
            player = GetComponent<Player>();
        }

        private void OnEnable()
        {
            targetModeAction = player.Input.PlayerActions.Sprint;
            targetModeAction.started += OnTargetModeStarted;
            targetModeAction.canceled += OnTargetModeCanceled;
            targetModeAction.Enable();
        }

        private void OnDisable()
        {
            if (targetModeAction != null)
            {
                targetModeAction.started -= OnTargetModeStarted;
                targetModeAction.canceled -= OnTargetModeCanceled;
            }
        }

        private void Update()
        {
            if (player == null)
                return;

            CacheCharacterFacing();
            UpdateIntentFromCamera();

            if (IsTargetModeHeld)
                UpdateTargetModeFollow();
        }

        private Vector3 NormalizePlanar(Vector3 vector, Vector3 fallback)
        {
            vector.y = 0f;

            if (vector.sqrMagnitude < 0.0001f)
            {
                fallback.y = 0f;

                if (fallback.sqrMagnitude < 0.0001f)
                    fallback = Vector3.forward;

                return fallback.normalized;
            }

            return vector.normalized;
        }

        private void SetCharacterFacing(Vector3 facing)
        {
            CharacterFacing = NormalizePlanar(facing, Vector3.forward);
        }

        private void SetIntentFacing(Vector3 facing)
        {
            IntentFacing = NormalizePlanar(facing, CharacterFacing);
        }

        private void CacheCharacterFacing()
        {
            SetCharacterFacing(player.transform.forward);
        }

        private float GetYawFromFacing(Vector3 facing)
        {
            Vector3 planarFacing = NormalizePlanar(facing, Vector3.forward);
            return Quaternion.LookRotation(planarFacing, Vector3.up).eulerAngles.y;
        }

        private void UpdateIntentFromCamera()
        {
            Camera cam = Camera.main;

            if (cam == null)
            {
                SetIntentFacing(CharacterFacing);
                IntentWorldPoint = player.transform.position + IntentFacing * markerDistance;
                VerticalIntentDelta = 0f;
                return;
            }

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            Vector3 rawPoint;

            if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimSurfaceLayers, QueryTriggerInteraction.Ignore))
                rawPoint = hit.point;
            else
                rawPoint = cam.transform.position + cam.transform.forward * maxAimDistance;

            Vector3 rawDirection = rawPoint - player.transform.position;
            VerticalIntentDelta = rawDirection.y;
            rawDirection.y = 0f;

            SetIntentFacing(rawDirection);
            IntentWorldPoint = player.transform.position + IntentFacing * markerDistance;
        }

        private void UpdateTargetModeFollow()
        {
            float targetCameraYaw = GetYawFromFacing(CharacterFacing);
            player.CameraRecenteringUtility.SmoothSetYaw(targetCameraYaw, targetModeCameraBehindSmoothTime);

            float currentYaw = player.transform.eulerAngles.y;
            float targetYaw = GetYawFromFacing(IntentFacing);

            float newYaw = Mathf.SmoothDampAngle(
                currentYaw,
                targetYaw,
                ref targetModeYawVelocity,
                targetModeTurnSmoothTime
            );

            Quaternion rotation = Quaternion.Euler(0f, newYaw, 0f);
            transform.rotation = rotation;

            if (player.Rigidbody != null)
                player.Rigidbody.MoveRotation(rotation);

            SetCharacterFacing(rotation * Vector3.forward);
        }

        private void OnTargetModeStarted(InputAction.CallbackContext context)
        {
            IsTargetModeHeld = true;

            float targetYaw = GetYawFromFacing(CharacterFacing);
            player.CameraRecenteringUtility.SmoothSetYaw(targetYaw, targetModeCameraBehindSmoothTime);
        }

        private void OnTargetModeCanceled(InputAction.CallbackContext context)
        {
            IsTargetModeHeld = false;
        }

        public void PrepareCommitAttack()
        {
            nextAttackUsesCommitAttack = true;
        }

        public bool ConsumeCommitAttack()
        {
            bool result = nextAttackUsesCommitAttack;
            nextAttackUsesCommitAttack = false;
            return result;
        }

        public bool ShouldAttacksUseIntent()
        {
            return IsTargetModeHeld;
        }

        public Vector3 GetMovementReferenceFacing()
        {
            return CharacterFacing;
        }

        public void CommitIntentToCharacter(bool snapCameraToo = false)
        {
            SetCharacterFacing(IntentFacing);

            Quaternion rotation = Quaternion.LookRotation(CharacterFacing, Vector3.up);
            transform.rotation = rotation;

            if (player.Rigidbody != null)
                player.Rigidbody.rotation = rotation;

            if (snapCameraToo)
                player.CameraRecenteringUtility.SnapBehindDirection(CharacterFacing);
        }

        public Vector3 GetTriangleFacing()
        {
            return CharacterFacing;
        }

        public Vector3 GetTrianglePosition()
        {
            return player.transform.position
                   + CharacterFacing * markerDistance
                   + Vector3.up * markerHeight;
        }

        public Vector3 GetIntentPosition()
        {
            float visualYOffset = Mathf.Clamp(VerticalIntentDelta * verticalIntentVisualScale, -1.5f, 1.5f);

            return player.transform.position
                   + IntentFacing * markerDistance
                   + Vector3.up * (markerHeight + visualYOffset);
        }
    }
}