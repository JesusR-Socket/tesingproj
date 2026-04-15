using UnityEngine;
using UnityEngine.InputSystem;

namespace GenshinImpactMovementSystem
{
    public class PlayerAttackingState : PlayerGroundedState
    {
        private const float AttackExitNormalizedTime = 0.95f;
        private const float FailSafeExitTime = 0.45f;

        private bool hasEnteredAttackAnimation;
        private bool hasExitedAttackState;
        private float attackStartedAt;

        private int currentAttackHash;
        private string currentAttackStateShortName;

        public PlayerAttackingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            stateMachine.ReusableData.MovementSpeedModifier = 0f;
            stateMachine.ReusableData.MovementInput = Vector2.zero;

            base.Enter();
            ResetVelocity();

            hasEnteredAttackAnimation = false;
            hasExitedAttackState = false;
            attackStartedAt = Time.time;

            bool useCommitAttack =
                stateMachine.Player.CombatIntentController != null &&
                stateMachine.Player.CombatIntentController.ConsumeCommitAttack();

            currentAttackHash = useCommitAttack
                ? stateMachine.Player.AnimationData.Attack1StateHash
                : stateMachine.Player.AnimationData.ShortAttackStateHash;

            currentAttackStateShortName = useCommitAttack ? "Attack1" : "ShortAttack";

            stateMachine.Player.Animator.CrossFadeInFixedTime(currentAttackHash, 0.05f, 0);
        }

        public override void HandleInput()
        {
            stateMachine.ReusableData.MovementInput = Vector2.zero;
        }

        public override void Update()
        {
            TryFinishAttack();
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate(); // Float остаётся активным
            DecelerateHorizontally();
        }

        private void TryFinishAttack()
        {
            if (hasExitedAttackState)
                return;

            if (!TryGetAttackStateInfo(out AnimatorStateInfo attackStateInfo))
            {
                if (Time.time - attackStartedAt >= FailSafeExitTime)
                    ExitAttack();

                return;
            }

            if (!hasEnteredAttackAnimation)
            {
                hasEnteredAttackAnimation = true;
                return;
            }

            if (attackStateInfo.normalizedTime >= AttackExitNormalizedTime)
                ExitAttack();
        }

        private bool TryGetAttackStateInfo(out AnimatorStateInfo stateInfo)
        {
            Animator animator = stateMachine.Player.Animator;

            for (int layer = 0; layer < animator.layerCount; layer++)
            {
                AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(layer);

                if (currentState.IsName(currentAttackStateShortName) ||
                    currentState.IsName("Base Layer." + currentAttackStateShortName))
                {
                    stateInfo = currentState;
                    return true;
                }
            }

            stateInfo = default;
            return false;
        }

        private void ExitAttack()
        {
            if (hasExitedAttackState)
                return;

            hasExitedAttackState = true;

            Vector2 liveInput = stateMachine.Player.Input.PlayerActions.Movement.ReadValue<Vector2>();
            stateMachine.ReusableData.MovementInput = liveInput;

            if (liveInput != Vector2.zero)
            {
                OnMove();
                return;
            }

            stateMachine.ChangeState(stateMachine.IdlingState);
        }

        public override void OnAnimationTransitionEvent()
        {
        }

        protected override void OnAttackStarted(InputAction.CallbackContext context) { }
        protected override void OnCommitAttackStarted(InputAction.CallbackContext context) { }
        protected override void OnDashStarted(InputAction.CallbackContext context) { }
        protected override void OnJumpStarted(InputAction.CallbackContext context) { }
        protected override void OnMovementPerformed(InputAction.CallbackContext context) { }
        protected override void OnMovementCanceled(InputAction.CallbackContext context) { }
    }
}