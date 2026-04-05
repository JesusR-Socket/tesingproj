using System;
using UnityEngine;

namespace GenshinImpactMovementSystem
{
    [Serializable]
    public class PlayerAnimationData
    {
        [Header("State Group Parameter Names")]
        [SerializeField] private string groundedParameterName = "Grounded";
        [SerializeField] private string movingParameterName = "Moving";
        [SerializeField] private string stoppingParameterName = "Stopping";
        [SerializeField] private string landingParameterName = "Landing";
        [SerializeField] private string airborneParameterName = "Airborne";

        [Header("Grounded Parameter Names")]
        [SerializeField] private string idleParameterName = "isIdling";
        [SerializeField] private string dashParameterName = "isDashing";
        [SerializeField] private string walkParameterName = "isWalking";
        [SerializeField] private string runParameterName = "isRunning";
        [SerializeField] private string sprintParameterName = "isSprinting";
        [SerializeField] private string mediumStopParameterName = "isMediumStopping";
        [SerializeField] private string hardStopParameterName = "isHardStopping";
        [SerializeField] private string rollParameterName = "isRolling";
        [SerializeField] private string hardLandParameterName = "isHardLanding";
        [field: SerializeField] public string UseAimLocomotionParameterName { get; private set; } = "UseAimLocomotion";
        [field: SerializeField] public string AimMoveXParameterName { get; private set; } = "AimMoveX";
        [field: SerializeField] public string AimMoveYParameterName { get; private set; } = "AimMoveY";
        [field: SerializeField] public string Attack1StateName { get; private set; } = "Base Layer.Attack1";

        [SerializeField] private string jumpParameterName = "isJumping";
        [SerializeField] private string movingJumpParameterName = "isMovingJumping";
        [Header("Airborne Parameter Names")]
        [SerializeField] private string fallParameterName = "isFalling";

        public int GroundedParameterHash { get; private set; }
        public int MovingParameterHash { get; private set; }
        public int StoppingParameterHash { get; private set; }
        public int LandingParameterHash { get; private set; }
        public int AirborneParameterHash { get; private set; }

        public int IdleParameterHash { get; private set; }
        public int DashParameterHash { get; private set; }
        public int WalkParameterHash { get; private set; }
        public int RunParameterHash { get; private set; }
        public int SprintParameterHash { get; private set; }
        public int MediumStopParameterHash { get; private set; }
        public int HardStopParameterHash { get; private set; }
        public int RollParameterHash { get; private set; }
        public int HardLandParameterHash { get; private set; }

        public int UseAimLocomotionParameterHash { get; private set; }
        public int AimMoveXParameterHash { get; private set; }
        public int AimMoveYParameterHash { get; private set; }
        public int JumpParameterHash { get; private set; }
        public int MovingJumpParameterHash { get; private set; }

        public int FallParameterHash { get; private set; }
        public int Attack1StateHash { get; private set; }

        public void Initialize()
        {
            GroundedParameterHash = Animator.StringToHash(groundedParameterName);
            MovingParameterHash = Animator.StringToHash(movingParameterName);
            StoppingParameterHash = Animator.StringToHash(stoppingParameterName);
            LandingParameterHash = Animator.StringToHash(landingParameterName);
            AirborneParameterHash = Animator.StringToHash(airborneParameterName);

            IdleParameterHash = Animator.StringToHash(idleParameterName);
            DashParameterHash = Animator.StringToHash(dashParameterName);
            WalkParameterHash = Animator.StringToHash(walkParameterName);
            RunParameterHash = Animator.StringToHash(runParameterName);
            SprintParameterHash = Animator.StringToHash(sprintParameterName);
            MediumStopParameterHash = Animator.StringToHash(mediumStopParameterName);
            HardStopParameterHash = Animator.StringToHash(hardStopParameterName);
            RollParameterHash = Animator.StringToHash(rollParameterName);
            HardLandParameterHash = Animator.StringToHash(hardLandParameterName);

            FallParameterHash = Animator.StringToHash(fallParameterName);
            Attack1StateHash = Animator.StringToHash(Attack1StateName);
            JumpParameterHash = Animator.StringToHash(jumpParameterName);
            MovingJumpParameterHash = Animator.StringToHash(movingJumpParameterName);

            UseAimLocomotionParameterHash = Animator.StringToHash(UseAimLocomotionParameterName);
            AimMoveXParameterHash = Animator.StringToHash(AimMoveXParameterName);
            AimMoveYParameterHash = Animator.StringToHash(AimMoveYParameterName);
        }
    }
}