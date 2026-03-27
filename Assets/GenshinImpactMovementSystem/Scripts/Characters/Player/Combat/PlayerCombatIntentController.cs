using UnityEngine;
using UnityEngine.InputSystem;

namespace GenshinImpactMovementSystem
{
    public class PlayerCombatIntentController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference aimActionReference;

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

        public float FrozenCameraYaw { get; private set; }

        private Player player;
        private InputAction aimAction;

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
                UpdateIntentFromCamera();
            }
            else
            {
                SyncIntentToCharacter();
            }
        }

        private void SyncIntentToCharacter()
        {
            Vector3 facing = player.transform.forward;
            facing.y = 0f;

            if (facing.sqrMagnitude < 0.0001f)
            {
                facing = Vector3.forward;
            }

            CharacterFacing = facing.normalized;
            IntentFacing = CharacterFacing;
            IntentWorldPoint = player.transform.position + CharacterFacing * markerDistance;
            VerticalIntentDelta = 0f;
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

            if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimSurfaceLayers, QueryTriggerInteraction.Ignore))
            {
                IntentWorldPoint = hit.point;
            }
            else
            {
                IntentWorldPoint = cam.transform.position + cam.transform.forward * maxAimDistance;
            }

            Vector3 direction = IntentWorldPoint - player.transform.position;
            VerticalIntentDelta = direction.y;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = CharacterFacing;
            }

            IntentFacing = direction.normalized;
        }

        private void OnAimStarted(InputAction.CallbackContext context)
        {
            IsAimHeld = true;

            Vector3 facing = player.transform.forward;
            facing.y = 0f;

            if (facing.sqrMagnitude < 0.0001f)
            {
                facing = Vector3.forward;
            }

            CharacterFacing = facing.normalized;

            if (player.MainCameraTransform != null)
            {
                FrozenCameraYaw = player.MainCameraTransform.eulerAngles.y;
            }
            else
            {
                FrozenCameraYaw = player.transform.eulerAngles.y;
            }
        }

        private void OnAimCanceled(InputAction.CallbackContext context)
        {
            IsAimHeld = false;
            CommitIntentToCharacter();
        }

        public void CommitIntentToCharacter()
        {
            Vector3 facing = IntentFacing;
            facing.y = 0f;

            if (facing.sqrMagnitude < 0.0001f)
            {
                return;
            }

            facing.Normalize();

            Quaternion rotation = Quaternion.LookRotation(facing, Vector3.up);

            transform.rotation = rotation;

            if (player.Rigidbody != null)
            {
                player.Rigidbody.rotation = rotation;
            }

            CharacterFacing = facing;
            IntentFacing = facing;
            IntentWorldPoint = player.transform.position + facing * markerDistance;
            VerticalIntentDelta = 0f;
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