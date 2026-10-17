using UnityEngine;

namespace Astraparasitus
{
    public enum PlayerMotionMode
    {
        Idle,
        Walk,
        Run,
        Aim,
        Action,
        Airborne,
        Cinematic,
    }

    public enum PlayerMotionAnimation
    {
        Idle,
        IdleTurn,
        Walk,
        Run,
        RunTurnLeft,
        RunTurnRight,
        AimMove,
        Reload,
        Fall,
        Land,
    }

    // 动画决定水平位移；这里统一处理配置倍率、重力、转向和 CharacterController 碰撞。
    public class PlayerMotionMotor
    {
        private readonly PlayerControl player;
        private float verticalSpeed;
        private float footstepDistance;

        public PlayerMotionMotor(PlayerControl player)
        {
            this.player = player;
        }

        public Vector2 MoveInput => player.playerInput.input.Move.ReadValue<Vector2>();
        public bool HasMoveInput
        {
            get
            {
                float threshold = player.Definition != null ? player.Definition.moveInputThreshold : 0.5f;
                return MoveInput.sqrMagnitude > threshold * threshold;
            }
        }
        public bool IsRunning => player.playerInput.input.Run.IsPressed();

        public Vector3 CameraForward
        {
            get
            {
                Vector3 forward = player.mainCamera.transform.forward;
                forward.y = 0f;
                return forward.sqrMagnitude > 0.0001f ? forward.normalized : player.model.transform.forward;
            }
        }

        public Vector3 CameraRelativeDirection
        {
            get
            {
                Vector3 right = player.mainCamera.transform.right;
                right.y = 0f;
                Vector3 direction = right.normalized * MoveInput.x + CameraForward * MoveInput.y;
                return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
            }
        }

        public void FaceCameraForward() => FaceDirection(CameraForward);
        public void FaceMoveDirection() => FaceDirection(CameraRelativeDirection);
        public void FaceConfiguredDirection(PlayerFacingMode mode)
        {
            FaceDirection(mode == PlayerFacingMode.InputDirection ? CameraRelativeDirection : CameraForward);
        }

        public string Animation(PlayerMotionAnimation animation)
        {
            PlayerDefinition definition = player.Definition;
            switch (animation)
            {
                case PlayerMotionAnimation.Idle: return Configured(definition?.idleAnimation, "Idle");
                case PlayerMotionAnimation.IdleTurn: return Configured(definition?.idleTurnAnimation, "Turning");
                case PlayerMotionAnimation.Walk: return Configured(definition?.walkAnimation, "Moving");
                case PlayerMotionAnimation.Run: return Configured(definition?.runAnimation, "Running");
                case PlayerMotionAnimation.RunTurnLeft: return Configured(definition?.runTurnLeftAnimation, "Run_TurnL");
                case PlayerMotionAnimation.RunTurnRight: return Configured(definition?.runTurnRightAnimation, "Run_TurnR");
                case PlayerMotionAnimation.AimMove: return Configured(definition?.aimMoveAnimation, "AimMoving");
                case PlayerMotionAnimation.Reload: return Configured(definition?.reloadAnimation, "Reload");
                case PlayerMotionAnimation.Fall: return Configured(definition?.fallAnimation, "Fall");
                case PlayerMotionAnimation.Land: return Configured(definition?.landAnimation, "Land");
                default: return string.Empty;
            }
        }

        private static string Configured(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        public void FaceDirection(Vector3 direction, bool instant = false)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            Quaternion target = Quaternion.LookRotation(direction);
            player.model.transform.rotation = instant
                ? target
                : Quaternion.RotateTowards(player.model.transform.rotation, target, player.turnSpeed * Time.deltaTime);
        }

        public void UpdateMoveBlend()
        {
            Vector2 input = MoveInput;
            PlayerDefinition definition = player.Definition;
            float dampTime = definition != null ? definition.moveBlendDampTime : 0.25f;
            player.model.animator.SetFloat(Configured(definition?.verticalBlendParameter, "Ver"), input.y, dampTime, Time.deltaTime);
            player.model.animator.SetFloat(Configured(definition?.horizontalBlendParameter, "Hor"), input.x, dampTime, Time.deltaTime);
        }

        public void TriggerRunSteer(bool left)
        {
            PlayerDefinition definition = player.Definition;
            string parameter = left
                ? Configured(definition?.leftTurnTrigger, "L")
                : Configured(definition?.rightTurnTrigger, "R");
            player.model.animator.SetTrigger(parameter);
        }

        public void SetAimClose(bool close)
        {
            string parameter = Configured(player.Definition?.aimCloseParameter, "isClose");
            player.model.animator.SetBool(parameter, close);
        }

        public void ApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation, PlayerMotionMode mode, bool applyAnimationRotation = false)
        {
            if (player.controller == null) return;

            float scale = player.GetAnimaionSpeed() * GetRootMotionScale(mode);
            Vector3 displacement = deltaPosition * scale;
            if (mode != PlayerMotionMode.Cinematic)
            {
                if (player.controller.isGrounded && verticalSpeed < 0f)
                {
                    verticalSpeed = -GetGroundedDownSpeed();
                }
                else
                {
                    float maxFallSpeed = player.Definition != null ? player.Definition.maxFallSpeed : 30f;
                    verticalSpeed = Mathf.Max(verticalSpeed + player.gravity * Time.deltaTime, -maxFallSpeed);
                }
                displacement.y = verticalSpeed * Time.deltaTime;
            }

            if (applyAnimationRotation)
            {
                player.model.transform.rotation *= deltaRotation;
            }

            Vector3 previousPosition = player.controller.transform.position;
            CollisionFlags collision = player.controller.Move(displacement);
            if ((collision & CollisionFlags.Below) != 0 && verticalSpeed < 0f)
            {
                verticalSpeed = -GetGroundedDownSpeed();
            }

            if (mode == PlayerMotionMode.Walk || mode == PlayerMotionMode.Run)
            {
                Vector3 moved = player.controller.transform.position - previousPosition;
                footstepDistance += new Vector2(moved.x, moved.z).magnitude;
                float stepDistance = player.Definition != null ? player.Definition.footstepDistance : 1f;
                if (stepDistance > 0f && footstepDistance >= stepDistance)
                {
                    player.model.FootStepSound();
                    footstepDistance %= stepDistance;
                }
            }
        }

        public void ResetVerticalMotion()
        {
            verticalSpeed = 0f;
            footstepDistance = 0f;
        }

        public void LaunchVertical(float speed)
        {
            verticalSpeed = Mathf.Max(0f, speed);
        }

        private float GetRootMotionScale(PlayerMotionMode mode)
        {
            PlayerDefinition definition = player.Definition;
            if (definition == null) return 1f;
            switch (mode)
            {
                case PlayerMotionMode.Idle: return definition.idleRootMotionScale;
                case PlayerMotionMode.Walk: return definition.walkRootMotionScale;
                case PlayerMotionMode.Run: return definition.runRootMotionScale;
                case PlayerMotionMode.Aim: return definition.aimRootMotionScale;
                case PlayerMotionMode.Action: return definition.actionRootMotionScale;
                case PlayerMotionMode.Airborne: return definition.airborneRootMotionScale;
                case PlayerMotionMode.Cinematic: return definition.cinematicRootMotionScale;
                default: return 1f;
            }
        }

        private float GetGroundedDownSpeed()
        {
            return player.Definition != null ? player.Definition.groundedDownSpeed : 2f;
        }
    }
}
