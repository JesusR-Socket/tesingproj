using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GenshinImpactMovementSystem
{
    public class PlayerMovementState : IState
    {
        protected PlayerMovementStateMachine stateMachine;
        protected readonly PlayerGroundedData groundedData;
        protected readonly PlayerAirborneData airborneData;

        public PlayerMovementState(PlayerMovementStateMachine playerMovementStateMachine)
        {
            stateMachine = playerMovementStateMachine;
            groundedData = stateMachine.Player.Data.GroundedData;
            airborneData = stateMachine.Player.Data.AirborneData;

            InitializeData();
        }

        public virtual void Enter()
        {
            AddInputActionsCallbacks();
        }

        public virtual void Exit()
        {
            RemoveInputActionsCallbacks();
        }

        public virtual void HandleInput()
        {
            ReadMovementInput();
        }

        public virtual void Update()
        {
            UpdateTargetModeLocomotionBlendTree();
        }

        public virtual void PhysicsUpdate()
        {
            Move();
        }

        public virtual void OnTriggerEnter(Collider collider)
        {
            if (stateMachine.Player.LayerData.IsGroundLayer(collider.gameObject.layer))
                OnContactWithGround(collider);
        }

        public virtual void OnTriggerExit(Collider collider)
        {
            if (stateMachine.Player.LayerData.IsGroundLayer(collider.gameObject.layer))
                OnContactWithGroundExited(collider);
        }

        public virtual void OnAnimationEnterEvent() { }
        public virtual void OnAnimationExitEvent() { }
        public virtual void OnAnimationTransitionEvent() { }

        private void InitializeData()
        {
            SetBaseCameraRecenteringData();
            SetBaseRotationData();
        }

        protected void SetBaseCameraRecenteringData()
        {
            stateMachine.ReusableData.SidewaysCameraRecenteringData = groundedData.SidewaysCameraRecenteringData;
            stateMachine.ReusableData.BackwardsCameraRecenteringData = groundedData.BackwardsCameraRecenteringData;
        }

        protected void SetBaseRotationData()
        {
            stateMachine.ReusableData.RotationData = groundedData.BaseRotationData;
            stateMachine.ReusableData.TimeToReachTargetRotation = stateMachine.ReusableData.RotationData.TargetRotationReachTime;
        }

        protected void StartAnimation(int animationHash)
        {
            stateMachine.Player.Animator.SetBool(animationHash, true);
        }

        protected void StopAnimation(int animationHash)
        {
            stateMachine.Player.Animator.SetBool(animationHash, false);
        }

        protected virtual void AddInputActionsCallbacks()
        {
            stateMachine.Player.Input.PlayerActions.WalkToggle.started += OnWalkToggleStarted;
            stateMachine.Player.Input.PlayerActions.Look.started += OnMouseMovementStarted;
            stateMachine.Player.Input.PlayerActions.Movement.performed += OnMovementPerformed;
            stateMachine.Player.Input.PlayerActions.Movement.canceled += OnMovementCanceled;
        }

        protected virtual void RemoveInputActionsCallbacks()
        {
            stateMachine.Player.Input.PlayerActions.WalkToggle.started -= OnWalkToggleStarted;
            stateMachine.Player.Input.PlayerActions.Look.started -= OnMouseMovementStarted;
            stateMachine.Player.Input.PlayerActions.Movement.performed -= OnMovementPerformed;
            stateMachine.Player.Input.PlayerActions.Movement.canceled -= OnMovementCanceled;
        }

        protected virtual void OnWalkToggleStarted(InputAction.CallbackContext context)
        {
            stateMachine.ReusableData.ShouldWalk = !stateMachine.ReusableData.ShouldWalk;
        }

        private void OnMouseMovementStarted(InputAction.CallbackContext context) { }

        protected virtual void OnMovementPerformed(InputAction.CallbackContext context) { }

        protected virtual void OnMovementCanceled(InputAction.CallbackContext context)
        {
            DisableCameraRecentering();
        }

        private void ReadMovementInput()
        {
            stateMachine.ReusableData.MovementInput = stateMachine.Player.Input.PlayerActions.Movement.ReadValue<Vector2>();
        }

        private void Move()
        {
            if (stateMachine.ReusableData.MovementInput == Vector2.zero || stateMachine.ReusableData.MovementSpeedModifier == 0f)
                return;

            Vector3 movementDirection = GetMovementInputDirection();
            float targetRotationYAngle = Rotate(movementDirection);
            Vector3 targetRotationDirection = GetTargetRotationDirection(targetRotationYAngle);

            float movementSpeed = GetMovementSpeed();
            Vector3 currentPlayerHorizontalVelocity = GetPlayerHorizontalVelocity();

            stateMachine.Player.Rigidbody.AddForce(
                targetRotationDirection * movementSpeed - currentPlayerHorizontalVelocity,
                ForceMode.VelocityChange
            );
        }

        protected Vector3 GetMovementInputDirection()
        {
            Vector2 input = stateMachine.ReusableData.MovementInput;
            return new Vector3(input.x, 0f, input.y);
        }

        private float Rotate(Vector3 direction)
        {
            float directionAngle = UpdateTargetRotation(direction);
            RotateTowardsTargetRotation();
            return directionAngle;
        }

        protected virtual bool ShouldUseCameraRelativeMovement()
        {
            var combat = stateMachine.Player.CombatIntentController;

            if (combat == null)
                return true;

            // Base mode = original repo movement относительно камеры.
            // Shift mode = target movement относительно facing персонажа.
            return !combat.IsTargetModeHeld;
        }

        protected float UpdateTargetRotation(Vector3 direction, bool shouldConsiderCameraRotation = true)
        {
            float directionAngle = GetDirectionAngle(direction);

            if (shouldConsiderCameraRotation && ShouldUseCameraRelativeMovement())
            {
                directionAngle = AddCameraRotationToAngle(directionAngle);
            }
            else
            {
                Vector3 movementReference = GetMovementReferenceFacing();
                float referenceYaw = Quaternion.LookRotation(movementReference, Vector3.up).eulerAngles.y;

                directionAngle += referenceYaw;

                if (directionAngle > 360f)
                    directionAngle -= 360f;
            }

            if (directionAngle != stateMachine.ReusableData.CurrentTargetRotation.y)
                UpdateTargetRotationData(directionAngle);

            return directionAngle;
        }

        protected Vector3 GetMovementReferenceFacing()
        {
            var combat = stateMachine.Player.CombatIntentController;

            if (combat != null)
            {
                Vector3 facing = combat.GetMovementReferenceFacing();
                facing.y = 0f;

                if (facing.sqrMagnitude > 0.0001f)
                    return facing.normalized;
            }

            Vector3 forward = stateMachine.Player.transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        private float GetDirectionAngle(Vector3 direction)
        {
            float directionAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            if (directionAngle < 0f)
                directionAngle += 360f;

            return directionAngle;
        }

        private float AddCameraRotationToAngle(float angle)
        {
            angle += stateMachine.Player.MainCameraTransform.eulerAngles.y;

            if (angle > 360f)
                angle -= 360f;

            return angle;
        }

        private void UpdateTargetRotationData(float targetAngle)
        {
            stateMachine.ReusableData.CurrentTargetRotation.y = targetAngle;
            stateMachine.ReusableData.DampedTargetRotationPassedTime.y = 0f;
        }

        protected void RotateTowardsTargetRotation()
        {
            var combat = stateMachine.Player.CombatIntentController;

            // Shift mode сам поворачивает персонажа в controller.
            if (combat != null && combat.IsTargetModeHeld)
                return;

            float currentYAngle = stateMachine.Player.Rigidbody.rotation.eulerAngles.y;

            if (currentYAngle == stateMachine.ReusableData.CurrentTargetRotation.y)
                return;

            float smoothedYAngle = Mathf.SmoothDampAngle(
                currentYAngle,
                stateMachine.ReusableData.CurrentTargetRotation.y,
                ref stateMachine.ReusableData.DampedTargetRotationCurrentVelocity.y,
                stateMachine.ReusableData.TimeToReachTargetRotation.y - stateMachine.ReusableData.DampedTargetRotationPassedTime.y
            );

            stateMachine.ReusableData.DampedTargetRotationPassedTime.y += Time.deltaTime;

            Quaternion targetRotation = Quaternion.Euler(0f, smoothedYAngle, 0f);
            stateMachine.Player.Rigidbody.MoveRotation(targetRotation);
        }

        protected Vector3 GetTargetRotationDirection(float targetRotationAngle)
        {
            return Quaternion.Euler(0f, targetRotationAngle, 0f) * Vector3.forward;
        }

        protected float GetMovementSpeed(bool shouldConsiderSlopes = true)
        {
            float movementSpeed = groundedData.BaseSpeed * stateMachine.ReusableData.MovementSpeedModifier;

            if (shouldConsiderSlopes)
                movementSpeed *= stateMachine.ReusableData.MovementOnSlopesSpeedModifier;

            return movementSpeed;
        }

        protected Vector3 GetPlayerHorizontalVelocity()
        {
            Vector3 playerHorizontalVelocity = stateMachine.Player.Rigidbody.linearVelocity;
            playerHorizontalVelocity.y = 0f;
            return playerHorizontalVelocity;
        }

        protected Vector3 GetPlayerVerticalVelocity()
        {
            return new Vector3(0f, stateMachine.Player.Rigidbody.linearVelocity.y, 0f);
        }

        protected virtual void OnContactWithGround(Collider collider) { }
        protected virtual void OnContactWithGroundExited(Collider collider) { }

        protected void UpdateCameraRecenteringState(Vector2 movementInput)
        {
            if (movementInput == Vector2.zero)
                return;

            if (movementInput == Vector2.up)
            {
                DisableCameraRecentering();
                return;
            }

            float cameraVerticalAngle = stateMachine.Player.MainCameraTransform.eulerAngles.x;

            if (cameraVerticalAngle >= 270f)
                cameraVerticalAngle -= 360f;

            cameraVerticalAngle = Mathf.Abs(cameraVerticalAngle);

            if (movementInput == Vector2.down)
            {
                SetCameraRecenteringState(
                    cameraVerticalAngle,
                    stateMachine.ReusableData.BackwardsCameraRecenteringData
                );
                return;
            }

            SetCameraRecenteringState(
                cameraVerticalAngle,
                stateMachine.ReusableData.SidewaysCameraRecenteringData
            );
        }

        protected void SetCameraRecenteringState(
            float cameraVerticalAngle,
            List<PlayerCameraRecenteringData> cameraRecenteringData)
        {
            foreach (PlayerCameraRecenteringData recenteringData in cameraRecenteringData)
            {
                if (!recenteringData.IsWithinRange(cameraVerticalAngle))
                    continue;

                EnableCameraRecentering(recenteringData.WaitTime, recenteringData.RecenteringTime);
                return;
            }

            DisableCameraRecentering();
        }

        protected void EnableCameraRecentering(float waitTime = -1f, float recenteringTime = -1f)
        {
            float movementSpeed = GetMovementSpeed();

            if (movementSpeed == 0f)
                movementSpeed = groundedData.BaseSpeed;

            stateMachine.Player.CameraRecenteringUtility.EnableRecentering(
                waitTime,
                recenteringTime,
                groundedData.BaseSpeed,
                movementSpeed
            );
        }

        protected void DisableCameraRecentering()
        {
            stateMachine.Player.CameraRecenteringUtility.DisableRecentering();
        }

        protected void ResetVelocity()
        {
            stateMachine.Player.Rigidbody.linearVelocity = Vector3.zero;
        }

        protected void ResetVerticalVelocity()
        {
            Vector3 playerHorizontalVelocity = GetPlayerHorizontalVelocity();
            stateMachine.Player.Rigidbody.linearVelocity = playerHorizontalVelocity;
        }

        protected void DecelerateHorizontally()
        {
            Vector3 playerHorizontalVelocity = GetPlayerHorizontalVelocity();

            stateMachine.Player.Rigidbody.AddForce(
                -playerHorizontalVelocity * stateMachine.ReusableData.MovementDecelerationForce,
                ForceMode.Acceleration
            );
        }

        protected void DecelerateVertically()
        {
            Vector3 playerVerticalVelocity = GetPlayerVerticalVelocity();

            stateMachine.Player.Rigidbody.AddForce(
                -playerVerticalVelocity * stateMachine.ReusableData.MovementDecelerationForce,
                ForceMode.Acceleration
            );
        }

        protected bool IsMovingHorizontally(float minimumMagnitude = 0.1f)
        {
            Vector3 playerHorizontalVelocity = GetPlayerHorizontalVelocity();
            Vector2 playerHorizontalMovement = new Vector2(playerHorizontalVelocity.x, playerHorizontalVelocity.z);

            return playerHorizontalMovement.magnitude > minimumMagnitude;
        }

        protected bool IsMovingUp(float minimumVelocity = 0.1f)
        {
            return GetPlayerVerticalVelocity().y > minimumVelocity;
        }

        protected bool IsMovingDown(float minimumVelocity = 0.1f)
        {
            return GetPlayerVerticalVelocity().y < -minimumVelocity;
        }

        protected void UpdateTargetModeLocomotionBlendTree()
        {
            var combat = stateMachine.Player.CombatIntentController;
            bool useTargetModeMovement = combat != null && combat.IsTargetModeHeld;
            bool hasMovementInput = stateMachine.ReusableData.MovementInput != Vector2.zero;

            Animator animator = stateMachine.Player.Animator;

            animator.SetBool(
                stateMachine.Player.AnimationData.UseAimLocomotionParameterHash,
                useTargetModeMovement && hasMovementInput
            );

            if (!(useTargetModeMovement && hasMovementInput))
            {
                animator.SetFloat(stateMachine.Player.AnimationData.AimMoveXParameterHash, 0f);
                animator.SetFloat(stateMachine.Player.AnimationData.AimMoveYParameterHash, 0f);
                return;
            }

            Vector3 localInputDirection = GetMovementInputDirection();
            float worldAngle = UpdateTargetRotation(localInputDirection);
            Vector3 worldMoveDirection = GetTargetRotationDirection(worldAngle);

            worldMoveDirection.y = 0f;

            if (worldMoveDirection.sqrMagnitude < 0.0001f)
            {
                animator.SetFloat(stateMachine.Player.AnimationData.AimMoveXParameterHash, 0f);
                animator.SetFloat(stateMachine.Player.AnimationData.AimMoveYParameterHash, 0f);
                return;
            }

            worldMoveDirection.Normalize();

            Vector3 forward = GetMovementReferenceFacing();
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                forward = stateMachine.Player.transform.forward;

            forward.Normalize();

            float signedAngle = Vector3.SignedAngle(forward, worldMoveDirection, Vector3.up);

            float aimMoveX = 0f;
            float aimMoveY = 0f;

            if (signedAngle >= -22.5f && signedAngle <= 22.5f)
            {
                aimMoveX = 0f;
                aimMoveY = 1f;
            }
            else if (signedAngle > 22.5f && signedAngle <= 67.5f)
            {
                aimMoveX = 1f;
                aimMoveY = 1f;
            }
            else if (signedAngle < -22.5f && signedAngle >= -67.5f)
            {
                aimMoveX = -1f;
                aimMoveY = 1f;
            }
            else if (signedAngle > 67.5f && signedAngle < 112.5f)
            {
                aimMoveX = 1f;
                aimMoveY = 0f;
            }
            else if (signedAngle < -67.5f && signedAngle > -112.5f)
            {
                aimMoveX = -1f;
                aimMoveY = 0f;
            }
            else
            {
                aimMoveX = 0f;
                aimMoveY = -1f;
            }

            animator.SetFloat(stateMachine.Player.AnimationData.AimMoveXParameterHash, aimMoveX);
            animator.SetFloat(stateMachine.Player.AnimationData.AimMoveYParameterHash, aimMoveY);
        }
    }
}