using UnityEngine;
using UnityEngine.InputSystem;

namespace GenshinImpactMovementSystem
{
    public class PlayerAttackingState : PlayerGroundedState
    {
        private const float AttackExitNormalizedTime = 0.90f;

        private bool hasEnteredAttackAnimation;
        private bool hasExitedAttackState;

        private int currentAttackHash;
        private string currentAttackStateShortName;

        public PlayerAttackingState(PlayerMovementStateMachine playerMovementStateMachine) : base(playerMovementStateMachine)
        {
        }

        public override void Enter()
        {
            stateMachine.ReusableData.MovementSpeedModifier = 0f;

            base.Enter();
            ResetVelocity();

            hasEnteredAttackAnimation = false;
            hasExitedAttackState = false;

            bool useAimCommitAttack =
                stateMachine.Player.CombatIntentController != null &&
                stateMachine.Player.CombatIntentController.ConsumeAimCommitAttack();

            currentAttackHash = useAimCommitAttack
                ? stateMachine.Player.AnimationData.Attack1StateHash
                : stateMachine.Player.AnimationData.ShortAttackStateHash;

            currentAttackStateShortName = useAimCommitAttack
                ? "Attack1"
                : "ShortAttack";

            stateMachine.Player.Animator.CrossFadeInFixedTime(currentAttackHash, 0.05f);
        }

        public override void Update()
        {
            base.Update();
            TryFinishAttack();
        }

        private void TryFinishAttack()
        {
            if (hasExitedAttackState)
            {
                return;
            }

            Animator animator = stateMachine.Player.Animator;
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

            bool isInAttackState =
                currentState.IsName(currentAttackStateShortName) ||
                currentState.IsName("Base Layer." + currentAttackStateShortName);

            if (!hasEnteredAttackAnimation)
            {
                if (isInAttackState)
                {
                    hasEnteredAttackAnimation = true;
                }

                return;
            }

            if (!isInAttackState)
            {
                return;
            }

            if (currentState.normalizedTime >= AttackExitNormalizedTime)
            {
                ExitAttack();
            }
        }

        private void ExitAttack()
        {
            if (hasExitedAttackState)
            {
                return;
            }

            hasExitedAttackState = true;

            if (stateMachine.ReusableData.MovementInput != Vector2.zero)
            {
                OnMove();
                return;
            }

            stateMachine.ChangeState(stateMachine.IdlingState);
        }

        public override void OnAnimationTransitionEvent()
        {
            // hit window можно оставить через animation event
        }

        protected override void OnAttackStarted(InputAction.CallbackContext context)
        {
            // Блокируем спам ЛКМ во время текущей атаки
        }

        protected override void OnDashStarted(InputAction.CallbackContext context)
        {
        }

        protected override void OnJumpStarted(InputAction.CallbackContext context)
        {
        }
    }
}