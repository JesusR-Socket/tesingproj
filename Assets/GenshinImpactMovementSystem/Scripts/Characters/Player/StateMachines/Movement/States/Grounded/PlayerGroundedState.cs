using UnityEngine;
using UnityEngine.InputSystem;

namespace GenshinImpactMovementSystem
{
    public class PlayerGroundedState : PlayerMovementState
    {
        private const float DoubleTapDashWindow = 0.22f;

        private Vector2 lastDashTapDirection = Vector2.zero;
        private float lastDashTapTime = -10f;

        public PlayerGroundedState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            StartAnimation(stateMachine.Player.AnimationData.GroundedParameterHash);
            UpdateShouldSprintState();
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(stateMachine.Player.AnimationData.GroundedParameterHash);
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            Float();
        }

        private void UpdateShouldSprintState()
        {
            if (!stateMachine.ReusableData.ShouldSprint)
                return;

            if (stateMachine.ReusableData.MovementInput != Vector2.zero)
                return;

            stateMachine.ReusableData.ShouldSprint = false;
        }

        private void Float()
        {
            Vector3 capsuleColliderCenterInWorldSpace =
                stateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.Collider.bounds.center;

            Ray downwardsRayFromCapsuleCenter =
                new Ray(capsuleColliderCenterInWorldSpace, Vector3.down);

            if (Physics.Raycast(
                    downwardsRayFromCapsuleCenter,
                    out RaycastHit hit,
                    stateMachine.Player.ResizableCapsuleCollider.SlopeData.FloatRayDistance,
                    stateMachine.Player.LayerData.GroundLayer,
                    QueryTriggerInteraction.Ignore))
            {
                float groundAngle = Vector3.Angle(hit.normal, -downwardsRayFromCapsuleCenter.direction);
                float slopeSpeedModifier = SetSlopeSpeedModifierOnAngle(groundAngle);

                if (slopeSpeedModifier == 0f)
                    return;

                float distanceToFloatingPoint =
                    stateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.ColliderCenterInLocalSpace.y *
                    stateMachine.Player.transform.localScale.y -
                    hit.distance;

                if (distanceToFloatingPoint == 0f)
                    return;

                float amountToLift =
                    distanceToFloatingPoint *
                    stateMachine.Player.ResizableCapsuleCollider.SlopeData.StepReachForce -
                    GetPlayerVerticalVelocity().y;

                Vector3 liftForce = new Vector3(0f, amountToLift, 0f);

                stateMachine.Player.Rigidbody.AddForce(liftForce, ForceMode.VelocityChange);
            }
        }

        private float SetSlopeSpeedModifierOnAngle(float angle)
        {
            float slopeSpeedModifier = groundedData.SlopeSpeedAngles.Evaluate(angle);

            if (stateMachine.ReusableData.MovementOnSlopesSpeedModifier != slopeSpeedModifier)
                stateMachine.ReusableData.MovementOnSlopesSpeedModifier = slopeSpeedModifier;

            return slopeSpeedModifier;
        }

        protected override void AddInputActionsCallbacks()
        {
            base.AddInputActionsCallbacks();

            stateMachine.Player.Input.PlayerActions.Dash.started += OnDashStarted;
            stateMachine.Player.Input.PlayerActions.Jump.started += OnJumpStarted;
            stateMachine.Player.Input.PlayerActions.Attack.started += OnAttackStarted;
            stateMachine.Player.Input.PlayerActions.CommitAttack.started += OnCommitAttackStarted;
        }

        protected override void RemoveInputActionsCallbacks()
        {
            base.RemoveInputActionsCallbacks();

            stateMachine.Player.Input.PlayerActions.Dash.started -= OnDashStarted;
            stateMachine.Player.Input.PlayerActions.Jump.started -= OnJumpStarted;
            stateMachine.Player.Input.PlayerActions.Attack.started -= OnAttackStarted;
            stateMachine.Player.Input.PlayerActions.CommitAttack.started -= OnCommitAttackStarted;
        }

        protected virtual void OnDashStarted(InputAction.CallbackContext context)
        {
            // Dash не на Shift.
        }

        protected virtual void OnJumpStarted(InputAction.CallbackContext context)
        {
            stateMachine.ChangeState(stateMachine.JumpingState);
        }

        protected virtual void OnAttackStarted(InputAction.CallbackContext context)
        {
            var combat = stateMachine.Player.CombatIntentController;

            if (combat != null && combat.ShouldAttacksUseIntent())
                combat.CommitIntentToCharacter();

            stateMachine.ChangeState(stateMachine.AttackingState);
        }

        protected virtual void OnCommitAttackStarted(InputAction.CallbackContext context)
        {
            var combat = stateMachine.Player.CombatIntentController;

            if (combat != null && combat.ShouldAttacksUseIntent())
                combat.CommitIntentToCharacter();

            if (combat != null)
                combat.PrepareCommitAttack();

            stateMachine.ChangeState(stateMachine.AttackingState);
        }

        protected virtual void OnMove()
        {
            if (stateMachine.ReusableData.ShouldSprint)
            {
                stateMachine.ChangeState(stateMachine.SprintingState);
                return;
            }

            if (stateMachine.ReusableData.ShouldWalk)
            {
                stateMachine.ChangeState(stateMachine.WalkingState);
                return;
            }

            stateMachine.ChangeState(stateMachine.RunningState);
        }

        protected override void OnContactWithGroundExited(Collider collider)
        {
            if (IsThereGroundUnderneath())
                return;

            Vector3 capsuleColliderCenterInWorldSpace =
                stateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.Collider.bounds.center;

            Ray downwardsRayFromCapsuleBottom =
                new Ray(
                    capsuleColliderCenterInWorldSpace -
                    stateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.ColliderVerticalExtents,
                    Vector3.down);

            if (!Physics.Raycast(
                    downwardsRayFromCapsuleBottom,
                    out _,
                    groundedData.GroundToFallRayDistance,
                    stateMachine.Player.LayerData.GroundLayer,
                    QueryTriggerInteraction.Ignore))
            {
                OnFall();
            }
        }

        private bool IsThereGroundUnderneath()
        {
            PlayerTriggerColliderData triggerColliderData =
                stateMachine.Player.ResizableCapsuleCollider.TriggerColliderData;

            Vector3 groundColliderCenterInWorldSpace =
                triggerColliderData.GroundCheckCollider.bounds.center;

            Collider[] overlappedGroundColliders = Physics.OverlapBox(
                groundColliderCenterInWorldSpace,
                triggerColliderData.GroundCheckColliderVerticalExtents,
                triggerColliderData.GroundCheckCollider.transform.rotation,
                stateMachine.Player.LayerData.GroundLayer,
                QueryTriggerInteraction.Ignore);

            return overlappedGroundColliders.Length > 0;
        }

        protected virtual void OnFall()
        {
            stateMachine.ChangeState(stateMachine.FallingState);
        }

        protected override void OnMovementPerformed(InputAction.CallbackContext context)
        {
            Vector2 rawInput = context.ReadValue<Vector2>();

            if (TryDoubleTapDash(rawInput))
                return;

            base.OnMovementPerformed(context);

            var combat = stateMachine.Player.CombatIntentController;

            // “олько base mode вращаетс€ как оригинальный repo.
            if (combat != null && combat.IsTargetModeHeld)
                return;

            UpdateTargetRotation(GetMovementInputDirection());
        }

        private Vector2 GetSingleKeyTapDirection(Vector2 input)
        {
            bool xPressed = Mathf.Abs(input.x) > 0.5f;
            bool yPressed = Mathf.Abs(input.y) > 0.5f;

            if (xPressed && yPressed)
                return Vector2.zero;

            if (xPressed)
                return new Vector2(Mathf.Sign(input.x), 0f);

            if (yPressed)
                return new Vector2(0f, Mathf.Sign(input.y));

            return Vector2.zero;
        }

        private bool IsDirectionStillHeld(Vector2 direction)
        {
            if (Keyboard.current == null)
                return false;

            if (direction == Vector2.up) return Keyboard.current.wKey.isPressed;
            if (direction == Vector2.down) return Keyboard.current.sKey.isPressed;
            if (direction == Vector2.left) return Keyboard.current.aKey.isPressed;
            if (direction == Vector2.right) return Keyboard.current.dKey.isPressed;

            return false;
        }

        private bool TryDoubleTapDash(Vector2 rawInput)
        {
            Vector2 tapDirection = GetSingleKeyTapDirection(rawInput);

            if (tapDirection == Vector2.zero)
                return false;

            if (tapDirection == lastDashTapDirection &&
                Time.time - lastDashTapTime <= DoubleTapDashWindow)
            {
                stateMachine.ReusableData.MovementInput = tapDirection;
                stateMachine.ReusableData.ShouldSprint = IsDirectionStillHeld(tapDirection);

                UpdateTargetRotation(GetMovementInputDirection());
                stateMachine.ChangeState(stateMachine.DashingState);

                lastDashTapDirection = Vector2.zero;
                lastDashTapTime = -10f;
                return true;
            }

            lastDashTapDirection = tapDirection;
            lastDashTapTime = Time.time;
            return false;
        }
    }
}