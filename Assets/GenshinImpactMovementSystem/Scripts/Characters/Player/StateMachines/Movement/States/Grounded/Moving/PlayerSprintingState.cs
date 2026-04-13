using UnityEngine;
using UnityEngine.InputSystem;

namespace GenshinImpactMovementSystem
{
    public class PlayerSprintingState : PlayerMovingState
    {
        private float startTime;
        private bool keepSprinting;
        private bool shouldResetSprintState;

        public PlayerSprintingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            stateMachine.ReusableData.MovementSpeedModifier = groundedData.SprintData.SpeedModifier;

            base.Enter();

            StartAnimation(stateMachine.Player.AnimationData.SprintParameterHash);
            stateMachine.ReusableData.CurrentJumpForce = airborneData.JumpData.StrongForce;

            startTime = Time.time;
            shouldResetSprintState = true;
            keepSprinting = stateMachine.ReusableData.ShouldSprint;
        }

        public override void Exit()
        {
            base.Exit();

            StopAnimation(stateMachine.Player.AnimationData.SprintParameterHash);

            if (shouldResetSprintState)
            {
                keepSprinting = false;
                stateMachine.ReusableData.ShouldSprint = false;
            }
        }

        public override void Update()
        {
            base.Update();

            if (stateMachine.Player.CombatIntentController != null &&
                stateMachine.Player.CombatIntentController.IsAimHeld)
            {
                stateMachine.ReusableData.ShouldSprint = false;
                keepSprinting = false;

                if (stateMachine.ReusableData.MovementInput == Vector2.zero)
                {
                    stateMachine.ChangeState(stateMachine.IdlingState);
                    return;
                }

                stateMachine.ChangeState(stateMachine.RunningState);
                return;
            }

            if (keepSprinting)
                return;

            if (Time.time < startTime + groundedData.SprintData.SprintToRunTime)
                return;

            StopSprinting();
        }

        private void StopSprinting()
        {
            if (stateMachine.ReusableData.MovementInput == Vector2.zero)
            {
                stateMachine.ChangeState(stateMachine.IdlingState);
                return;
            }

            stateMachine.ChangeState(stateMachine.RunningState);
        }

        protected override void AddInputActionsCallbacks()
        {
            base.AddInputActionsCallbacks();
        }

        protected override void RemoveInputActionsCallbacks()
        {
            base.RemoveInputActionsCallbacks();
        }

        protected override void OnMovementCanceled(InputAction.CallbackContext context)
        {
            stateMachine.ChangeState(stateMachine.HardStoppingState);
            base.OnMovementCanceled(context);
        }

        protected override void OnJumpStarted(InputAction.CallbackContext context)
        {
            shouldResetSprintState = false;
            base.OnJumpStarted(context);
        }

        protected override void OnFall()
        {
            shouldResetSprintState = false;
            base.OnFall();
        }
    }
}