using UnityEngine;
using UnityEngine.InputSystem;

namespace GenshinImpactMovementSystem
{
    public class PlayerCombatIntentController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference aimActionReference;

        [Header("Aim Limits")]
        [SerializeField, Range(0f, 180f)] private float maxAimAngleFromCharacter = 75f;
        [SerializeField] private float tapThreshold = 0.18f;

        [Header("Camera Smooth")]
        [SerializeField] private float aimClampCameraSmoothTime = 0.03f;
        [SerializeField] private float releaseCameraReturnSmoothTime = 0.10f;
        [SerializeField] private float commitCharacterTurnSmoothTime = 0.045f;
        [SerializeField] private float commitCameraTurnSmoothTime = 0.05f;
        [SerializeField] private float finishAngleThreshold = 0.5f;

        [Header("Intent Raycast")]
        [SerializeField] private LayerMask aimSurfaceLayers = ~0;
        [SerializeField] private float maxAimDistance = 60f;

        [Header("Marker Placement")]
        [SerializeField] private float markerDistance = 2.2f;
        [SerializeField] private float markerHeight = 1.7f;
        [SerializeField] private float verticalIntentVisualScale = 0.12f;

        public bool IsAimHeld { get; private set; }
        public Vector3 CharacterFacing { get; private set; }
        public Vector3 IntentFacing { get; private set; }
        public Vector3 IntentWorldPoint { get; private set; }
        public float VerticalIntentDelta { get; private set; }

        private Player player;
        private InputAction aimAction;

        private float aimStartedTime;
        private bool nextAttackUsesAimCommitAttack;

        private bool shouldReturnCameraBehindCharacter;
        private bool isSmoothCommitTurnActive;
        private float smoothCommitTargetYaw;
        private float smoothCharacterTurnVelocity;

        private void Awake()
        {
            player = GetComponent<Player>();
        }

        private void OnEnable()
        {
            if (aimActionReference != null)
            {
                aimAction = aimActionReference.action;
            }

            if (aimAction == null)
            {
                Debug.LogError("Aim Action Reference is not assigned.", this);
                enabled = false;
                return;
            }

            aimAction.started += OnAimStarted;
            aimAction.canceled += OnAimCanceled;
            aimAction.Enable();
        }

        private void OnDisable()
        {
            if (aimAction != null)
            {
                aimAction.started -= OnAimStarted;
                aimAction.canceled -= OnAimCanceled;
            }
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            if (IsAimHeld)
            {
                ClampCameraWithinAimCone();
                UpdateIntentFromCamera();
            }
            else
            {
                SyncIntentToCharacter();
                UpdateReturnCameraBehindCharacter();
            }

            UpdateSmoothCommitTurn();
        }

        private void CacheCharacterFacing()
        {
            Vector3 facing = player.transform.forward;
            facing.y = 0f;

            if (facing.sqrMagnitude < 0.0001f)
            {
                facing = Vector3.forward;
            }

            CharacterFacing = facing.normalized;
        }

        private float GetYawFromFacing(Vector3 facing)
        {
            facing.y = 0f;

            if (facing.sqrMagnitude < 0.0001f)
            {
                facing = Vector3.forward;
            }

            return Quaternion.LookRotation(facing.normalized, Vector3.up).eulerAngles.y;
        }

        private void SyncIntentToCharacter()
        {
            CacheCharacterFacing();

            IntentFacing = CharacterFacing;
            IntentWorldPoint = player.transform.position + CharacterFacing * markerDistance;
            VerticalIntentDelta = 0f;
        }

        private void ClampCameraWithinAimCone()
        {
            float characterYaw = GetYawFromFacing(CharacterFacing);
            float currentCameraYaw = player.CameraRecenteringUtility.GetCurrentYaw();

            float signedDelta = Mathf.DeltaAngle(characterYaw, currentCameraYaw);
            float clampedDelta = Mathf.Clamp(signedDelta, -maxAimAngleFromCharacter, maxAimAngleFromCharacter);
            float targetYaw = characterYaw + clampedDelta;

            player.CameraRecenteringUtility.SmoothSetYaw(targetYaw, aimClampCameraSmoothTime);
        }

        private void UpdateReturnCameraBehindCharacter()
        {
            if (!shouldReturnCameraBehindCharacter)
            {
                return;
            }

            CacheCharacterFacing();

            float targetYaw = GetYawFromFacing(CharacterFacing);
            float currentYaw = player.CameraRecenteringUtility.GetCurrentYaw();

            player.CameraRecenteringUtility.SmoothSetYaw(targetYaw, releaseCameraReturnSmoothTime);

            if (Mathf.Abs(Mathf.DeltaAngle(currentYaw, targetYaw)) <= finishAngleThreshold)
            {
                player.CameraRecenteringUtility.SetYawImmediate(targetYaw);
                shouldReturnCameraBehindCharacter = false;
            }
        }

        private void UpdateSmoothCommitTurn()
        {
            if (!isSmoothCommitTurnActive)
            {
                return;
            }

            float currentCharacterYaw = player.transform.eulerAngles.y;

            float newCharacterYaw = Mathf.SmoothDampAngle(
                currentCharacterYaw,
                smoothCommitTargetYaw,
                ref smoothCharacterTurnVelocity,
                commitCharacterTurnSmoothTime
            );

            Quaternion rotation = Quaternion.Euler(0f, newCharacterYaw, 0f);

            transform.rotation = rotation;

            if (player.Rigidbody != null)
            {
                player.Rigidbody.rotation = rotation;
            }

            player.CameraRecenteringUtility.SmoothSetYaw(smoothCommitTargetYaw, commitCameraTurnSmoothTime);

            Vector3 facing = rotation * Vector3.forward;
            facing.y = 0f;
            facing.Normalize();

            CharacterFacing = facing;
            IntentFacing = facing;
            IntentWorldPoint = player.transform.position + facing * markerDistance;
            VerticalIntentDelta = 0f;

            bool characterDone =
                Mathf.Abs(Mathf.DeltaAngle(newCharacterYaw, smoothCommitTargetYaw)) <= finishAngleThreshold;

            bool cameraDone =
                Mathf.Abs(Mathf.DeltaAngle(player.CameraRecenteringUtility.GetCurrentYaw(), smoothCommitTargetYaw)) <= finishAngleThreshold;

            if (characterDone && cameraDone)
            {
                player.CameraRecenteringUtility.SetYawImmediate(smoothCommitTargetYaw);
                isSmoothCommitTurnActive = false;
            }
        }

        private void UpdateIntentFromCamera()
        {
            Camera cam = Camera.main;

            if (cam == null)
            {
                SyncIntentToCharacter();
                return;
            }

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);

            Vector3 rawPoint;

            if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimSurfaceLayers, QueryTriggerInteraction.Ignore))
            {
                rawPoint = hit.point;
            }
            else
            {
                rawPoint = cam.transform.position + cam.transform.forward * maxAimDistance;
            }

            Vector3 rawDirection = rawPoint - player.transform.position;
            VerticalIntentDelta = rawDirection.y;
            rawDirection.y = 0f;

            if (rawDirection.sqrMagnitude < 0.0001f)
            {
                rawDirection = CharacterFacing;
            }

            rawDirection.Normalize();

            float signedAngle = Vector3.SignedAngle(CharacterFacing, rawDirection, Vector3.up);
            float clampedAngle = Mathf.Clamp(signedAngle, -maxAimAngleFromCharacter, maxAimAngleFromCharacter);

            Vector3 intentFacing = Quaternion.AngleAxis(clampedAngle, Vector3.up) * CharacterFacing;
            intentFacing.y = 0f;
            intentFacing.Normalize();

            IntentFacing = intentFacing;
            IntentWorldPoint = player.transform.position + IntentFacing * markerDistance;
        }

        private void OnAimStarted(InputAction.CallbackContext context)
        {
            IsAimHeld = true;
            aimStartedTime = Time.time;

            shouldReturnCameraBehindCharacter = false;

            CacheCharacterFacing();
            player.CameraRecenteringUtility.DisableRecentering();

            ClampCameraWithinAimCone();
            UpdateIntentFromCamera();
        }

        private void OnAimCanceled(InputAction.CallbackContext context)
        {
            bool wasTap = Time.time - aimStartedTime <= tapThreshold;

            IsAimHeld = false;

            // Персонажа НЕ крутим при обычном отпускании ПКМ.
            // Возвращаем только камеру за спину.
            shouldReturnCameraBehindCharacter = true;

            // Для простого клика просто тот же возврат,
            // но за счёт smooth time это будет быстро и не резко.
            if (wasTap)
            {
                shouldReturnCameraBehindCharacter = true;
            }

            SyncIntentToCharacter();
        }

        public void PrepareAimCommitAttack()
        {
            nextAttackUsesAimCommitAttack = true;
        }

        public bool ConsumeAimCommitAttack()
        {
            bool result = nextAttackUsesAimCommitAttack;
            nextAttackUsesAimCommitAttack = false;
            return result;
        }

        public void BeginSmoothCommitTurn()
        {
            Vector3 facing = IntentFacing;
            facing.y = 0f;

            if (facing.sqrMagnitude < 0.0001f)
            {
                return;
            }

            facing.Normalize();

            CharacterFacing = facing;
            IntentFacing = facing;
            IntentWorldPoint = player.transform.position + facing * markerDistance;
            VerticalIntentDelta = 0f;

            smoothCommitTargetYaw = GetYawFromFacing(facing);
            isSmoothCommitTurnActive = true;
            shouldReturnCameraBehindCharacter = false;
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