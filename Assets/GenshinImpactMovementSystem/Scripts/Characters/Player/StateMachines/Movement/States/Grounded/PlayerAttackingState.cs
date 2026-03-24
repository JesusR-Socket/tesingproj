using UnityEngine;
using UnityEngine.InputSystem;

namespace GenshinImpactMovementSystem
{
    public class PlayerAttackingState : PlayerGroundedState
    {
        private bool hasEnteredAttackAnimation;
        private bool hasExitedAttackState;

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

            stateMachine.Player.Animator.CrossFadeInFixedTime(
                stateMachine.Player.AnimationData.Attack1StateHash,
                0.05f
            );

            Debug.Log("ENTER ATTACK STATE");
        }

        public override void Update()
        {
            base.Update();

            if (stateMachine.ReusableData.MovementInput != Vector2.zero)
            {
                UpdateTargetRotation(GetMovementInputDirection());
            }

            TryFinishAttack();
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            RotateTowardsTargetRotation();
        }

        private const float AttackExitNormalizedTime = 0.90f;

        private void TryFinishAttack()
        {
            if (hasExitedAttackState)
            {
                return;
            }

            Animator animator = stateMachine.Player.Animator;
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

            bool isInAttackState =
                currentState.IsName("Attack1") ||
                currentState.IsName("Base Layer.Attack1");

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

            // Выходим ЧУТЬ РАНЬШЕ конца клипа, чтобы Moving/Run успели включиться
            // до того, как Animator свалится в Grounded/Idle.
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
            // Для атаки выход больше не завязан на event конца.
            // Event оставляем только для самого удара / hit window.
        }

        protected override void OnAttackStarted(InputAction.CallbackContext context)
        {
            // Пока без комбо
        }

        protected override void OnDashStarted(InputAction.CallbackContext context)
        {
        }

        protected override void OnJumpStarted(InputAction.CallbackContext context)
        {
        }
    }
}