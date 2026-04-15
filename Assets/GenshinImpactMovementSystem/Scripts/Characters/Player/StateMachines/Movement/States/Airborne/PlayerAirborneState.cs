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
            var combat = stateMachine.Player.CombatIntentController;

            // ¬ target mode (Shift) оставл€ем steering через target-style movement.
            bool useTargetModeAirSteering =
                combat != null &&
                combat.IsTargetModeHeld;

            if (useTargetModeAirSteering && stateMachine.ReusableData.MovementInput != Vector2.zero)
            {
                UpdateTargetRotation(GetMovementInputDirection());
                RotateTowardsTargetRotation();
                return;
            }

            // Ѕез Shift не крутимс€ за камерой,
            // а просто ориентируемс€ по реальному горизонтальному движению.
            Vector3 horizontalVelocity = stateMachine.Player.Rigidbody.linearVelocity;
            horizontalVelocity.y = 0f;

            if (horizontalVelocity.sqrMagnitude <= 0.01f)
                return;

            float velocityAngle = Quaternion.LookRotation(horizontalVelocity.normalized, Vector3.up).eulerAngles.y;
            stateMachine.ReusableData.CurrentTargetRotation.y = velocityAngle;
            stateMachine.Player.Rigidbody.MoveRotation(Quaternion.Euler(0f, velocityAngle, 0f));
        }

        protected override void OnMovementPerformed(InputAction.CallbackContext context)
        {
            base.OnMovementPerformed(context);

            var combat = stateMachine.Player.CombatIntentController;

            bool useTargetModeAirSteering =
                combat != null &&
                combat.IsTargetModeHeld;

            if (!useTargetModeAirSteering)
                return;

            Vector2 movementInput = context.ReadValue<Vector2>();

            if (movementInput == Vector2.zero)
                return;

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