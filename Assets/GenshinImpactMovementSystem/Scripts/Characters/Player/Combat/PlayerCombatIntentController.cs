using UnityEngine;
using UnityEngine.InputSystem;

namespace GenshinImpactMovementSystem
{
    public class PlayerCombatIntentController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference aimActionReference;

        [Header("Shift")]
        [SerializeField] private float shiftHoldThreshold = 0.14f;
        [SerializeField] private float shiftSnapBehindSmoothTime = 0.07f;
        [SerializeField] private float shiftCharacterFollowSmoothTime = 0.05f;
        [SerializeField] private float shiftMoveDeadZone = 15f;
        [SerializeField] private float shiftIdleTurnThresholdNoAim = 90f;
        [SerializeField] private float shiftIdleTurnThresholdAim = 75f;
        [SerializeField] private float turnStepDegrees = 90f;
        [SerializeField] private float turnStepCooldown = 0.15f;

        [Header("Right Mouse")]
        [SerializeField] private float rightMouseDoubleTapWindow = 0.22f;
        [SerializeField] private float doubleAimTurnSmoothTime = 0.06f;
        [SerializeField] private float doubleAimTurnFinishThreshold = 0.75f;

        [Header("Intent Raycast")]
        [SerializeField] private LayerMask aimSurfaceLayers = ~0;
        [SerializeField] private float maxAimDistance = 60f;

        [Header("Marker Placement")]
        [SerializeField] private float markerDistance = 2.2f;
        [SerializeField] private float markerHeight = 1.7f;
        [SerializeField] private float verticalIntentVisualScale = 0.12f;

        public bool IsAimHeld { get; private set; }
        public bool IsBackCameraHeld { get; private set; }
        public bool IsBackCameraPressed => shiftPressed;

        public Vector3 CharacterFacing { get; private set; }
        public Vector3 IntentFacing { get; private set; }
        public Vector3 IntentWorldPoint { get; private set; }
        public float VerticalIntentDelta { get; private set; }

        private Player player;
        private InputAction aimAction;
        private InputAction backCameraAction;

        private bool nextAttackUsesAimCommitAttack;
        private bool shiftPressed;
        private float shiftPressedTime = -10f;
        private float lastAimTapTime = -10f;
        private float lastStepTurnTime = -10f;
        private float shiftCharacterYawVelocity;

        private bool isSmoothAimTurnActive;
        private float smoothAimTargetYaw;
        private float smoothAimTurnVelocity;

        private void Awake()
        {
            player = GetComponent<Player>();
        }

        private void OnEnable()
        {
            if (aimActionReference != null)
                aimAction = aimActionReference.action;

            if (aimAction == null)
            {
                Debug.LogError("Aim Action Reference is not assigned.", this);
                enabled = false;
                return;
            }

            aimAction.started += OnAimStarted;
            aimAction.canceled += OnAimCanceled;
            aimAction.Enable();

            backCameraAction = player.Input.PlayerActions.Sprint;
            backCameraAction.started += OnBackCameraStarted;
            backCameraAction.canceled += OnBackCameraCanceled;
            backCameraAction.Enable();
        }

        private void OnDisable()
        {
            if (aimAction != null)
            {
                aimAction.started -= OnAimStarted;
                aimAction.canceled -= OnAimCanceled;
            }

            if (backCameraAction != null)
            {
                backCameraAction.started -= OnBackCameraStarted;
                backCameraAction.canceled -= OnBackCameraCanceled;
            }
        }

        private void Update()
        {
            if (player == null)
                return;

            CacheCharacterFacing();
            UpdateIntentFromCamera();
            UpdateShiftHoldState();

            Vector2 moveInput = player.Input.PlayerActions.Movement.ReadValue<Vector2>();
            bool isMoving = moveInput != Vector2.zero;

            if (IsBackCameraHeld)
                UpdateShiftFollow(isMoving);

            UpdateSmoothAimTurn();
        }

        private void CacheCharacterFacing()
        {
            Vector3 facing = player.transform.forward;
            facing.y = 0f;

            if (facing.sqrMagnitude < 0.0001f)
                facing = Vector3.forward;

            CharacterFacing = facing.normalized;
        }

        private float GetYawFromFacing(Vector3 facing)
        {
            facing.y = 0f;

            if (facing.sqrMagnitude < 0.0001f)
                facing = Vector3.forward;

            return Quaternion.LookRotation(facing.normalized, Vector3.up).eulerAngles.y;
        }

        private void UpdateIntentFromCamera()
        {
            Camera cam = Camera.main;

            if (cam == null)
            {
                IntentFacing = CharacterFacing;
                IntentWorldPoint = player.transform.position + CharacterFacing * markerDistance;
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

            if (rawDirection.sqrMagnitude < 0.0001f)
                rawDirection = CharacterFacing;

            rawDirection.Normalize();

            IntentFacing = rawDirection;
            IntentWorldPoint = player.transform.position + IntentFacing * markerDistance;
        }

        private void UpdateShiftHoldState()
        {
            if (!shiftPressed || IsBackCameraHeld)
                return;

            if (Time.time - shiftPressedTime >= shiftHoldThreshold)
                IsBackCameraHeld = true;
        }

        private void UpdateShiftFollow(bool isMoving)
        {
            if (!IsBackCameraHeld)
                return;

            // Без ПКМ + Shift + движение:
            // только держим камеру за спиной, сам поворот персонажа оставляем движению из оригинального репо.
            if (!IsAimHeld && isMoving)
            {
                float targetCameraYaw = GetYawFromFacing(CharacterFacing);
                player.CameraRecenteringUtility.SmoothSetYaw(targetCameraYaw, shiftSnapBehindSmoothTime);
                return;
            }

            // С ПКМ + Shift + движение:
            // follow mode с dead zone 15°
            if (IsAimHeld && isMoving)
            {
                float targetCameraYaw = GetYawFromFacing(CharacterFacing);
                player.CameraRecenteringUtility.SmoothSetYaw(targetCameraYaw, shiftSnapBehindSmoothTime);

                float signedMoveAngle = Vector3.SignedAngle(CharacterFacing, IntentFacing, Vector3.up);

                if (Mathf.Abs(signedMoveAngle) < shiftMoveDeadZone)
                    return;

                float currentYaw = player.transform.eulerAngles.y;
                float targetYaw = GetYawFromFacing(IntentFacing);

                float newYaw = Mathf.SmoothDampAngle(
                    currentYaw,
                    targetYaw,
                    ref shiftCharacterYawVelocity,
                    shiftCharacterFollowSmoothTime
                );

                Quaternion moveRotation = Quaternion.Euler(0f, newYaw, 0f);
                transform.rotation = moveRotation;

                if (player.Rigidbody != null)
                    player.Rigidbody.MoveRotation(moveRotation);

                CacheCharacterFacing();
                return;
            }

            // Shift + без движения:
            // камеру за персонажем не тянем.
            // Делаем только дискретный поворот персонажа шагом 90°.
            float threshold = IsAimHeld ? shiftIdleTurnThresholdAim : shiftIdleTurnThresholdNoAim;
            float signedIdleAngle = Vector3.SignedAngle(CharacterFacing, IntentFacing, Vector3.up);

            if (Mathf.Abs(signedIdleAngle) < threshold)
                return;

            if (Time.time - lastStepTurnTime < turnStepCooldown)
                return;

            float currentIdleYaw = player.transform.eulerAngles.y;
            float nextIdleYaw = currentIdleYaw + Mathf.Sign(signedIdleAngle) * turnStepDegrees;

            Quaternion idleTurnRotation = Quaternion.Euler(0f, nextIdleYaw, 0f);
            transform.rotation = idleTurnRotation;

            if (player.Rigidbody != null)
                player.Rigidbody.MoveRotation(idleTurnRotation);

            lastStepTurnTime = Time.time;
            CacheCharacterFacing();
        }

        private void UpdateSmoothAimTurn()
        {
            if (!isSmoothAimTurnActive)
                return;

            float currentYaw = player.transform.eulerAngles.y;

            float newYaw = Mathf.SmoothDampAngle(
                currentYaw,
                smoothAimTargetYaw,
                ref smoothAimTurnVelocity,
                doubleAimTurnSmoothTime
            );

            Quaternion newRotation = Quaternion.Euler(0f, newYaw, 0f);
            transform.rotation = newRotation;

            if (player.Rigidbody != null)
                player.Rigidbody.MoveRotation(newRotation);

            CacheCharacterFacing();

            if (Mathf.Abs(Mathf.DeltaAngle(newYaw, smoothAimTargetYaw)) <= doubleAimTurnFinishThreshold)
            {
                Quaternion finalRotation = Quaternion.Euler(0f, smoothAimTargetYaw, 0f);
                transform.rotation = finalRotation;

                if (player.Rigidbody != null)
                    player.Rigidbody.MoveRotation(finalRotation);

                CacheCharacterFacing();
                isSmoothAimTurnActive = false;
            }
        }

        private void OnAimStarted(InputAction.CallbackContext context)
        {
            IsAimHeld = true;

            if (Time.time - lastAimTapTime <= rightMouseDoubleTapWindow)
                BeginSmoothAimTurnToIntent();

            lastAimTapTime = Time.time;
        }

        private void OnAimCanceled(InputAction.CallbackContext context)
        {
            IsAimHeld = false;
        }

        private void OnBackCameraStarted(InputAction.CallbackContext context)
        {
            shiftPressed = true;
            shiftPressedTime = Time.time;
            IsBackCameraHeld = false;

            CacheCharacterFacing();
            float targetYaw = GetYawFromFacing(CharacterFacing);
            player.CameraRecenteringUtility.SmoothSetYaw(targetYaw, shiftSnapBehindSmoothTime);
        }

        private void OnBackCameraCanceled(InputAction.CallbackContext context)
        {
            shiftPressed = false;
            IsBackCameraHeld = false;
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

        public void CommitIntentToCharacter(bool snapCameraToo = false)
        {
            Vector3 facing = IntentFacing;
            facing.y = 0f;

            if (facing.sqrMagnitude < 0.0001f)
                return;

            facing.Normalize();

            Quaternion rotation = Quaternion.LookRotation(facing, Vector3.up);
            transform.rotation = rotation;

            if (player.Rigidbody != null)
                player.Rigidbody.rotation = rotation;

            CharacterFacing = facing;
            IntentFacing = facing;
            IntentWorldPoint = player.transform.position + facing * markerDistance;
            VerticalIntentDelta = 0f;

            if (snapCameraToo)
                player.CameraRecenteringUtility.SnapBehindDirection(facing);
        }

        public void BeginSmoothAimTurnToIntent()
        {
            Vector3 facing = IntentFacing;
            facing.y = 0f;

            if (facing.sqrMagnitude < 0.0001f)
                return;

            facing.Normalize();
            smoothAimTargetYaw = GetYawFromFacing(facing);
            isSmoothAimTurnActive = true;
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