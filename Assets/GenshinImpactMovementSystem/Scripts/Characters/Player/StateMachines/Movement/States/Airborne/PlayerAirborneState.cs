using UnityEngine;
using UnityEngine.InputSystem;

namespace GenshinImpactMovementSystem
{
    public class PlayerAirborneState : PlayerMovementState
    {
        public PlayerAirborneState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            StartAnimation(stateMachine.Player.AnimationData.AirborneParameterHash);
            ResetSprintState();
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(stateMachine.Player.AnimationData.AirborneParameterHash);
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            RotateInAir();
        }

        protected virtual void RotateInAir()
        {
            if (stateMachine.ReusableData.MovementInput == Vector2.zero)
            {
                return;
            }

            // Если позже дашь air movement speed > 0,
            // базовый Move() уже сам вызовет rotation.
            if (stateMachine.ReusableData.MovementSpeedModifier > 0f)
            {
                return;
            }

            UpdateTargetRotation(GetMovementInputDirection());
            RotateTowardsTargetRotation();
        }

        protected override void OnMovementPerformed(InputAction.CallbackContext context)
        {
            base.OnMovementPerformed(context);

            Vector2 movementInput = context.ReadValue<Vector2>();

            if (movementInput == Vector2.zero)
            {
                return;
            }

            UpdateTargetRotation(new Vector3(movementInput.x, 0f, movementInput.y));
        }

        protected virtual void ResetSprintState()
        {
            stateMachine.ReusableData.ShouldSprint = false;
        }

        protected override void OnContactWithGround(Collider collider)
        {
            stateMachine.ChangeState(stateMachine.LightLandingState);
        }
    }
}