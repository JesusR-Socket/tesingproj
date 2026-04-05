using UnityEngine;
using UnityEngine.InputSystem;

namespace GenshinImpactMovementSystem
{
    public class PlayerJumpingState : PlayerAirborneState
    {
        private bool canStartFalling;

        public PlayerJumpingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            bool isFastMovingJump =
                stateMachine.ReusableData.ShouldSprint ||
                (stateMachine.ReusableData.MovementInput != Vector2.zero &&
                 !stateMachine.ReusableData.ShouldWalk);

            // Всегда включаем общий старт прыжка
            StartAnimation(stateMachine.Player.AnimationData.JumpParameterHash);

            // Отдельно отмечаем быстрый прыжок из Run/Sprint
            if (isFastMovingJump)
            {
                StartAnimation(stateMachine.Player.AnimationData.MovingJumpParameterHash);
            }
            else
            {
                StopAnimation(stateMachine.Player.AnimationData.MovingJumpParameterHash);
            }

            // На всякий случай снимаем fall
            StopAnimation(stateMachine.Player.AnimationData.FallParameterHash);

            stateMachine.ReusableData.MovementSpeedModifier = 0f;
            stateMachine.ReusableData.MovementDecelerationForce = airborneData.JumpData.DecelerationForce;
            stateMachine.ReusableData.RotationData = airborneData.JumpData.RotationData;
            stateMachine.ReusableData.TimeToReachTargetRotation =
                stateMachine.ReusableData.RotationData.TargetRotationReachTime;

            Jump();
        }

        public override void Exit()
        {
            StopAnimation(stateMachine.Player.AnimationData.JumpParameterHash);
            StopAnimation(stateMachine.Player.AnimationData.MovingJumpParameterHash);

            base.Exit();
            SetBaseRotationData();
            canStartFalling = false;
        }

        public override void Update()
        {
            base.Update();

            if (!canStartFalling && IsMovingUp(0f))
            {
                canStartFalling = true;
            }

            if (!canStartFalling || IsMovingUp(0f))
            {
                return;
            }

            stateMachine.ChangeState(stateMachine.FallingState);
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();

            if (IsMovingUp())
            {
                DecelerateVertically();
            }
        }

        private void Jump()
        {
            Vector3 jumpForce = stateMachine.ReusableData.CurrentJumpForce;
            Vector3 jumpDirection = stateMachine.Player.transform.forward;

            if (stateMachine.ReusableData.MovementInput != Vector2.zero)
            {
                UpdateTargetRotation(GetMovementInputDirection());
                jumpDirection = GetTargetRotationDirection(stateMachine.ReusableData.CurrentTargetRotation.y);
            }

            jumpForce.x *= jumpDirection.x;
            jumpForce.z *= jumpDirection.z;

            jumpForce = GetJumpForceOnSlope(jumpForce);

            ResetVelocity();
            stateMachine.Player.Rigidbody.AddForce(jumpForce, ForceMode.VelocityChange);
        }

        private Vector3 GetJumpForceOnSlope(Vector3 jumpForce)
        {
            Vector3 capsuleColliderCenterInWorldSpace =
                stateMachine.Player.ResizableCapsuleCollider.CapsuleColliderData.Collider.bounds.center;

            Ray downwardsRayFromCapsuleCenter = new Ray(capsuleColliderCenterInWorldSpace, Vector3.down);

            if (Physics.Raycast(
                    downwardsRayFromCapsuleCenter,
                    out RaycastHit hit,
                    airborneData.JumpData.JumpToGroundRayDistance,
                    stateMachine.Player.LayerData.GroundLayer,
                    QueryTriggerInteraction.Ignore))
            {
                float groundAngle = Vector3.Angle(hit.normal, -downwardsRayFromCapsuleCenter.direction);

                if (IsMovingUp())
                {
                    float forceModifier = airborneData.JumpData.JumpForceModifierOnSlopeUpwards.Evaluate(groundAngle);
                    jumpForce.x *= forceModifier;
                    jumpForce.z *= forceModifier;
                }

                if (IsMovingDown())
                {
                    float forceModifier = airborneData.JumpData.JumpForceModifierOnSlopeDownwards.Evaluate(groundAngle);
                    jumpForce.y *= forceModifier;
                }
            }

            return jumpForce;
        }

        protected override void ResetSprintState()
        {
        }

        protected override void OnMovementCanceled(InputAction.CallbackContext context)
        {
        }
    }
}