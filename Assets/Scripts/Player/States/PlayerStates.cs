using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Astraparasitus
{
    public class PlayerState_Idle : PlayerBaseState
    {
        protected override PlayerMotionMode MotionMode => PlayerMotionMode.Idle;
        protected override bool ApplyAnimationRotation => true;
        private enum IdleSubState
        {
            Idle,
            Turning,
        }
        private IdleSubState subState;
        private IdleSubState SubState
        {
            get => subState;
            set
            {
                subState = value;
                switch (subState)
                {
                    case IdleSubState.Idle:

                        if (player.currentWp == null)
                        {
                            player.PlayAnimaton(player.Motion.Animation(PlayerMotionAnimation.Idle));
                        }
                        else
                        {
                            player.PlayAnimaton(player.currentWp.IdleAnim);
                        }
                        break;
                    case IdleSubState.Turning:
                        player.PlayAnimaton(player.Motion.Animation(PlayerMotionAnimation.IdleTurn));
                        break;
                }
            }
        }
        private const int CACHE_SIZE = 3;
        private float[] turnSpeedCache = new float[CACHE_SIZE];
        private int currentCacheIndex = 0;
        private float turningRate;
        private float averageTurnSpeed = 0f;
        private float AverageTurnSpeed(float currentSpeed)
        {
            turnSpeedCache[currentCacheIndex] = currentSpeed;
            currentCacheIndex++;
            currentCacheIndex %= CACHE_SIZE;
            float averageSpeed = 0f;
            foreach (float speed in turnSpeedCache)
            {
                averageSpeed += speed;
            }
            return averageSpeed / CACHE_SIZE;
        }
        public override void Enter()
        {
            SubState = IdleSubState.Idle;
            if (player.currentWp != null)
            {
                player.playerChoesLayerAnimation(player.currentWp.onHandAnim, 4);
            }
            else
            {
                player.playerChoesLayerAnimation("Empty", 4);
            }
            player.model.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (player.Motion.HasMoveInput)
            {
                player.ChangeState(PlayerState.Move);
            }
            if (player.playerInput.input.Aim.IsPressed() && player.CheckWeapon(player.currentWeaponIndex))
            {
                player.ChangeState(PlayerState.Aim);
            }
            if (!player.playerInput.input.Aim.IsPressed() && player.playerInput.input.Shoot.triggered && player.CheckWeapon(player.MeleeWeapon))
            {
                player.ChangeState(PlayerState.Attack);
            }
            if (player.playerInput.input.Block.IsPressed() && player.CheckWeapon(player.MeleeWeapon))
            {
                player.ChangeState(PlayerState.Block);
            }
            if (!player.model.CheckGround())
            {
                player.ChangeState(PlayerState.Fall);
            }
            switch (SubState)
            {
                case IdleSubState.Idle:
                    IdleUpdate();
                    break;
                case IdleSubState.Turning:
                    TurningUpdate();
                    break;
            }
        }
        private void IdleUpdate()
        {
            if (!player.playerInput.input.Aim.IsPressed())
                CalculateAngleBetweenCameraAndPlayer();
            if (CheckAnimationName("Knife_Idle", 0, out float time))
            {
                if (time >= 1f)
                {
                    player.ChangeWeapon(player.currentWeaponIndex);
                    SubState = IdleSubState.Idle;
                    if (player.currentWp != null)
                    {
                        player.playerChoesLayerAnimation(player.currentWp.onHandAnim, 4);
                    }
                    else
                    {
                        player.playerChoesLayerAnimation("Empty", 4);
                    }
                }
            }
        }
        private void TurningUpdate()
        {
            if (player.playerInput.input.Aim.IsPressed())
            {
                SubState = IdleSubState.Idle;
            }
            // 获取角色的前向向量，并忽略Y分量
            Vector3 playerForward = new Vector3(player.model.transform.forward.x, 0, player.model.transform.forward.z);

            // 获取相机的前向向量，并忽略Y分量
            Vector3 cameraForward = player.Motion.CameraForward;

            // 计算两个向量之间的夹角
            float angle = Vector3.Angle(playerForward, cameraForward);

            // 计算叉积以确定方向
            Vector3 crossProduct = Vector3.Cross(playerForward, cameraForward);
            if (crossProduct.y < 0)
            {
                turningRate = angle / 180;
                angle = -angle;
            }
            else
            {
                turningRate = -angle / 180;

            }
            float turnSpeed = angle / Time.deltaTime;
            player.model.animator.SetFloat("Turning", AverageTurnSpeed(turnSpeed));
            float stopAngle = player.Definition != null ? player.Definition.idleTurnStopAngle : 54f;
            if (Mathf.Abs(turningRate) * 180f < stopAngle)
            {
                SubState = IdleSubState.Idle;
            }
        }
        public override void Exit()
        {
            //player.model.animator.
            player.model.ClearRootMotionAction();
        }
        private void CalculateAngleBetweenCameraAndPlayer()
        {
            // 获取角色的前向向量，并忽略Y分量
            Vector3 playerForward = new Vector3(player.model.transform.forward.x, 0, player.model.transform.forward.z);

            // 获取相机的前向向量，并忽略Y分量
            Vector3 cameraForward = player.Motion.CameraForward;

            // 计算两个向量之间的夹角
            float angle = Vector3.Angle(playerForward, cameraForward);

            // 计算叉积以确定方向
            Vector3 crossProduct = Vector3.Cross(playerForward, cameraForward);
            if (crossProduct.y < 0)
            {
                turningRate = angle / 180;
            }
            else
            {
                turningRate = -angle / 180;
            }
            float startAngle = player.Definition != null ? player.Definition.idleTurnStartAngle : 54f;
            if (Mathf.Abs(turningRate) * 180f >= startAngle)
            {
                SubState = IdleSubState.Turning;
            }
            //// 根据叉积的Y分量判断方向
            //Debug.Log("Cross product: " + crossProduct.y);
            //string direction = crossProduct.y > 0 ? "左" : "右";

            //// 打印夹角和方向到控制台
            //Debug.Log("The angle between the player and the camera is: " + angle + " degrees towards " + direction);

        }
    }
    public class PlayerState_Move : PlayerBaseState
    {
        private enum SubMoveState
        {
            Walk,
            Run,
            Turn,
        }
        private SubMoveState subState;
        protected override PlayerMotionMode MotionMode => subState == SubMoveState.Run || subState == SubMoveState.Turn
            ? PlayerMotionMode.Run : PlayerMotionMode.Walk;
        protected override bool ApplyAnimationRotation => subState == SubMoveState.Turn;
        private string turnD = "R";
        private SubMoveState SubState
        {
            get => subState;
            set
            {
                subState = value;
                switch (subState)
                {
                    case SubMoveState.Walk:
                        player.PlayAnimaton(player.Motion.Animation(PlayerMotionAnimation.Walk));
                        break;
                    case SubMoveState.Run:
                        player.PlayAnimaton(player.Motion.Animation(PlayerMotionAnimation.Run));
                        break;
                    case SubMoveState.Turn:
                        player.PlayAnimaton(RunTurnAnimation);
                        break;
                }
            }
        }
        private string RunTurnAnimation => player.Motion.Animation(turnD == "L"
            ? PlayerMotionAnimation.RunTurnLeft : PlayerMotionAnimation.RunTurnRight);

        public override void Enter()
        {
            SubState = SubMoveState.Walk;
            if (player.currentWp != null)
            {
                player.playerChoesLayerAnimation(player.currentWp.onHandAnim, 4);
            }
            else
            {
                player.playerChoesLayerAnimation("Empty", 4);
            }
            player.model.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (player.playerInput.input.Aim.IsPressed() && player.CheckWeapon(player.currentWeaponIndex))
            {
                player.ChangeState(PlayerState.Aim);
            }
            if (!player.playerInput.input.Aim.IsPressed() && player.playerInput.input.Shoot.triggered && player.CheckWeapon(player.MeleeWeapon))
            {
                player.ChangeState(PlayerState.Attack);
            }
            if (player.playerInput.input.Block.IsPressed() && player.CheckWeapon(player.MeleeWeapon))
            {
                player.ChangeState(PlayerState.Block);
            }
            if (!player.model.CheckGround())
            {
                player.ChangeState(PlayerState.Fall);
            }
            switch (subState)
            {
                case SubMoveState.Walk:
                    WalkUpdate();
                    break;
                case SubMoveState.Run:
                    RunUpdate();
                    break;
                case SubMoveState.Turn:
                    TurnUpdate();
                    break;
            }


        }
        public void WalkUpdate()
        {
            if (player.Motion.HasMoveInput)
            {
                if (player.Motion.IsRunning)
                {
                    SubState = SubMoveState.Run;
                }
                PlayerFacingMode facing = player.Definition != null ? player.Definition.walkFacingMode : PlayerFacingMode.CameraForward;
                player.Motion.FaceConfiguredDirection(facing);
                player.Motion.UpdateMoveBlend();
                //if (!player.model.audioSource.isPlaying) player.model.FootStepSound();
            }
            else
            {
                player.ChangeState(PlayerState.Idle);
            }
        }
        public void RunUpdate()
        {
            if (player.Motion.HasMoveInput)
            {
                if (!player.Motion.IsRunning)
                {
                    SubState = SubMoveState.Walk;
                }
                PlayerFacingMode facing = player.Definition != null ? player.Definition.runFacingMode : PlayerFacingMode.InputDirection;
                Vector3 dir = facing == PlayerFacingMode.InputDirection
                    ? player.Motion.CameraRelativeDirection : player.Motion.CameraForward;

                // 检测输入方向与当前移动方向的关系
                float angle = Vector3.Angle(player.model.transform.forward, dir);
                //Debug.Log(angle);
                if (angle > (player.Definition != null ? player.Definition.runTurnAngle : 150f))
                {
                    Vector3 crossProduct = Vector3.Cross(player.model.transform.forward, dir);
                    float turnDirection = crossProduct.y > 0 ? 1 : -1; // 1表示向左转，-1表示向右转

                    if (turnDirection > 0)
                    {
                        turnD = "L";
                    }
                    else
                    {
                        turnD = "R";
                    }
                    SubState = SubMoveState.Turn;
                }
                else
                {
                    float steerAngle = player.Definition != null ? player.Definition.runSteerTriggerAngle : 5f;
                    if (angle >= steerAngle)
                    {
                        float turnSide = Vector3.Cross(player.model.transform.forward, dir).y;
                        if (turnSide > 0f) player.Motion.TriggerRunSteer(true);
                        else if (turnSide < 0f) player.Motion.TriggerRunSteer(false);
                    }
                    player.Motion.FaceConfiguredDirection(facing);
                }

            }
            else
            {
                player.ChangeState(PlayerState.Idle);
            }
        }
        public void TurnUpdate()
        {
            if (player.Motion.HasMoveInput)
            {
                if (CheckAnimationName(RunTurnAnimation, 0, out float currentTime))
                {
                    if (currentTime >= 1f)
                    {
                        SubState = SubMoveState.Run;
                    }
                }
            }
            else
            {
                player.ChangeState(PlayerState.Idle);
            }
        }
        public override void Exit()
        {
            player.model.ClearRootMotionAction();
        }
    }
    public class PlayerState_Aim : PlayerBaseState
    {
        protected override PlayerMotionMode MotionMode => PlayerMotionMode.Aim;
        private enum AimSubState
        {
            Aim,
            Move,
        }
        private AimSubState subState;
        public bool IsMoving => subState == AimSubState.Move;
        private AimSubState SubState
        {
            get => subState;
            set
            {
                subState = value;
                switch (subState)
                {
                    case AimSubState.Aim:
                        player.PlayAnimaton(player.currentWp.AimAnim, 0f);
                        break;
                    case AimSubState.Move:
                        player.playerChoesLayerAnimation(player.Motion.Animation(PlayerMotionAnimation.AimMove), 3);
                        break;
                }
            }
        }
        public override void Enter()
        {
            player.ChangeWeapon(player.currentWeaponIndex);
            player.model.rig.weight = 1f;
            SubState = AimSubState.Aim;
            //player.currentWeaponIndex = 1;
            player.playerChoesLayerAnimation("Empty", 4, 0f);
            player.SetAimVisible(true);
            player.aimCamera.Priority = 10;
            player.virtualCamera.Priority = 0;
            player.model.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            float obstacleDistance = player.model.GetShortestDistance();
            float nearDistance = player.Definition != null ? player.Definition.aimNearObstacleDistance : 2.5f;
            float farDistance = player.Definition != null ? player.Definition.aimFarObstacleDistance : 3f;
            if (obstacleDistance < nearDistance)
            {
                player.Motion.SetAimClose(true);
            }
            else if (obstacleDistance > farDistance)
            {
                player.Motion.SetAimClose(false);
            }

            switch (subState)
            {
                case AimSubState.Aim:
                    AimUpdate();
                    break;
                case AimSubState.Move:
                    AimMovingUpdate();
                    break;
            }
        }
        public void CalculateAimXY()
        {
            // 获取角色的前向向量，并忽略Y分量
            Vector3 playerForward = new Vector3(player.model.transform.forward.x, 0, player.model.transform.forward.z);
            //playerForward.Normalize(); // 确保向量是单位向量

            // 获取相机的前向向量，并忽略Y分量
            Vector3 cameraForward = player.Motion.CameraForward;
            //cameraForward.Normalize(); // 确保向量是单位向量

            // 计算两个向量之间的夹角
            float angleX = Vector3.Angle(playerForward, cameraForward);

            // 计算叉积以确定方向
            Vector3 crossProduct = Vector3.Cross(playerForward, cameraForward);
            if (crossProduct.y < 0)
            {
                angleX = -angleX;
            }
            float turnSpeedX = angleX / Time.deltaTime;

            // 获取角色的向上向量 
            Vector3 playerUp = player.model.transform.up;

            // 获取相机的右向向量（用于确定Y方向的角度）
            Vector3 cameraUp = player.mainCamera.transform.up;

            // 计算角色的前向向量和右向量（垂直于playerUp和playerForward）
            Vector3 playerForward2 = player.model.transform.forward; // 假设这是角色的前向向量
            playerForward2.Normalize(); // 确保向量是单位向量
            Vector3 playerRight = Vector3.Cross(playerUp, playerForward2);
            playerRight.Normalize(); // 确保向量是单位向量

            // 将cameraUp投影到playerForward和playerUp所在的平面
            Vector3 cameraUpProjected = playerForward2 * Vector3.Dot(cameraUp, playerForward2)
                                         + playerUp * Vector3.Dot(cameraUp, playerUp);
            cameraUpProjected.Normalize(); // 确保向量是单位向量

            // 计算角色的向上向量与相机的右向向量之间的夹角，这个角度即为相机Y轴方向的角度
            float yAngle = Vector3.Angle(playerUp, cameraUpProjected);

            // 计算叉积以确定方向
            Vector3 crossProductY = Vector3.Cross(playerUp, cameraUpProjected);
            if (crossProductY.x > 0) // 根据实际情况调整条件
            {
                yAngle = -yAngle;
            }
            float turnSpeedY = yAngle;

            // 设置动画参数
            //player.model.animator.SetFloat("AimX", turnSpeedX);
            player.model.animator.SetFloat("AimY", turnSpeedY);
        }
        public void AimUpdate()
        {
            //CalculateAimXY();
            player.Motion.FaceCameraForward();
            if (!player.playerInput.input.Aim.IsPressed())
            {
                player.ChangeState(PlayerState.Idle);
            }
            if (player.Motion.HasMoveInput)
            {
                if (player.playerInput.input.Aim.IsPressed())
                {
                    SubState = AimSubState.Move;
                }
                else
                {
                    player.ChangeState(PlayerState.Move);
                }
            }

        }
        public void AimMovingUpdate()
        {
            if (player.Motion.HasMoveInput)
            {
                if (!player.playerInput.input.Aim.IsPressed())
                {
                    player.ChangeState(PlayerState.Move);
                }
                player.Motion.FaceCameraForward();
                player.Motion.UpdateMoveBlend();
            }
            else
            {
                if (!player.playerInput.input.Aim.IsPressed())
                {
                    player.ChangeState(PlayerState.Idle);
                }
                else
                {
                    player.playerChoesLayerAnimation("Empty", 3);
                    SubState = AimSubState.Aim;
                }
            }
        }

        public override void Exit()
        {
            player.model.rig.weight = 0f;
            player.aimCamera.Priority = 0;
            player.virtualCamera.Priority = 10;
            player.playerChoesLayerAnimation("Empty", 3);
            player.SetAimVisible(false);
            player.model.ClearRootMotionAction();
        }
    }
    public class PlaerState_Reload : PlayerBaseState
    {
        protected override PlayerMotionMode MotionMode => PlayerMotionMode.Aim;
        private enum ReloadSubState
        {
            Reload,
            Move,
        }
        private ReloadSubState subState;
        public bool IsMoving => subState == ReloadSubState.Move;
        private ReloadSubState SubState
        {
            get => subState;
            set
            {
                subState = value;
                switch (subState)
                {
                    case ReloadSubState.Reload:
                        player.playerChoesLayerAnimation("Empty", 3);
                        break;
                    case ReloadSubState.Move:
                        player.playerChoesLayerAnimation(player.Motion.Animation(PlayerMotionAnimation.AimMove), 3);
                        break;
                }
            }
        }
        public override void Enter()
        {
            player.PlayAnimaton(player.Motion.Animation(PlayerMotionAnimation.Reload));
            player.playerChoesLayerAnimation("Empty", 4, 0f);
            SubState = ReloadSubState.Reload;
            player.model.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (CheckAnimationName(player.Motion.Animation(PlayerMotionAnimation.Reload), 0, out float currentTime))
            {
                switch (subState)
                {
                    case ReloadSubState.Reload:
                        ReloadUpdate();
                        break;
                    case ReloadSubState.Move:
                        ReloadMoveUpdate();
                        break;
                }
                if (currentTime >= 1f)
                {
                    if (player.playerInput.input.Aim.IsPressed() && player.CheckWeapon(player.currentWeaponIndex))
                    {
                        player.ChangeState(PlayerState.Aim);
                    }
                    else if (player.Motion.HasMoveInput)
                    {
                        player.ChangeState(PlayerState.Move);
                    }
                    else
                    {
                        player.ChangeState(PlayerState.Idle);
                    }
                }
            }
        }
        public void ReloadUpdate()
        {
            if (player.Motion.HasMoveInput)
            {
                SubState = ReloadSubState.Move;
            }
        }
        public void ReloadMoveUpdate()
        {
            if (player.Motion.HasMoveInput)
            {
                //player.playerChoesLayerAnimation("AimMoving", 3);
                player.Motion.FaceCameraForward();
                player.Motion.UpdateMoveBlend();
            }
            else
            {
                SubState = ReloadSubState.Reload;
            }
        }
        public override void Exit()
        {
            player.playerChoesLayerAnimation("Empty", 3);
            player.model.ClearRootMotionAction();
        }
    }
    public class PlayerState_Attack : PlayerBaseState
    {
        private int normalSkillsIndex = 0;
        private string currentAttackName;
        private int NormalSkillsIndex
        {
            get => normalSkillsIndex;
            set
            {
                if (value >= player.model.attackAnimations.Length)
                {
                    normalSkillsIndex = 0;
                }
                else normalSkillsIndex = value;
            }
        }
        public override void Enter()
        {
            player.ChangeWeapon(player.MeleeWeapon);
            StartAttack();
            player.playerChoesLayerAnimation("Empty", 4, 0f);
            player.model.SetRootMotionAction(OnRootMotion);
        }
        public void StartAttack()
        {
            WpConfig knifeConfig = player.currentWp;
            GameObject knifeModel = player.GetWeaponModel(knifeConfig);
            if (knifeConfig != null && knifeModel != null)
            {
                AudioManager.instance.PlaySound(knifeConfig.shootSound, knifeModel.GetComponent<AudioSource>());
            }
            player.PlayAnimaton(player.model.attackAnimations[NormalSkillsIndex]);
            currentAttackName = player.model.attackAnimations[NormalSkillsIndex];
            player.model.canAttack = false;
            NormalSkillsIndex++;
        }
        public override void Update()
        {
            if (CheckAnimationName(currentAttackName, 0, out float currentTime))
            {
                if (currentTime >= 1f)
                {
                    if (player.playerInput.input.Aim.IsPressed() && player.CheckWeapon(player.currentWeaponIndex))
                    {
                        player.ChangeState(PlayerState.Aim);
                    }
                    else if (player.Motion.HasMoveInput)
                    {
                        player.ChangeState(PlayerState.Move);
                    }
                    else
                    {
                        player.ChangeState(PlayerState.Idle);
                    }
                }
                if (player.model.canAttack)
                {
                    if (!player.playerInput.input.Aim.IsPressed() && player.playerInput.input.Shoot.triggered)
                    {
                        StartAttack();
                    }
                }
            }
            if (player.playerInput.input.Block.IsPressed() && player.CheckWeapon(player.MeleeWeapon))
            {
                player.ChangeState(PlayerState.Block);
            }
        }
        public override void Exit()
        {
            // An interrupted attack may skip the animation event that closes the hit window.
            player.model.EndAttack();
            player.model.ClearRootMotionAction();
            NormalSkillsIndex = 0;
            player.model.canAttack = false;
        }
    }
    public class PlayerState_KnifeBlock : PlayerBaseState
    {
        public override void Enter()
        {
            player.ChangeWeapon(player.MeleeWeapon);
            player.PlayAnimaton("Knife_Idle");
            player.playerChoesLayerAnimation("Empty", 4, 0f);
            player.model.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (!player.playerInput.input.Block.IsPressed())
            {
                player.ChangeState(PlayerState.Idle);
            }
        }
        public override void Exit()
        {
            player.model.ClearRootMotionAction();
        }
    }
    public class PlayerState_TC : PlayerBaseState
    {
        protected override PlayerMotionMode MotionMode => PlayerMotionMode.Cinematic;
        private enum TCSubState
        {
            Start,
            FailEnd,
            SuccessEnd,
        }
        private TCSubState subState;
        private TCSubState SubState
        {
            get => subState;
            set
            {
                subState = value;
                switch (subState)
                {
                    case TCSubState.Start:
                        switch (player.enemyPosition.gameObject.name)
                        {
                            case "Enemy_butcher(Clone)":
                                CameraManager.Instance.SetVirtualCameraWeight(2);
                                player.enemyPosition.position = player.model.tcPositions[0].position;
                                break;
                        }
                        player.PlayAnimaton(player.currentTCName);
                        break;
                    case TCSubState.FailEnd:
                        CameraManager.Instance.ResetVirtualCamera();
                        player.enemyPosition.gameObject.GetComponent<EnemyBase>().ChangeState(EnemyState.TCEnd);
                        player.PlayAnimaton(player.currentTCName + "Fail");
                        break;
                    case TCSubState.SuccessEnd:
                        CameraManager.Instance.ResetVirtualCamera();
                        player.PlayAnimaton(player.currentTCName + "Success");
                        break;
                }
            }
        }
        private float tempTime;
        private float tempFillAmount;
        public override void Enter()
        {
            player.SetQteDisplay(true, 0f, Color.white);
            SubState = TCSubState.Start;
            CheckEnemyPosition();
            //player.playerChoesLayerAnimation("Empty", 4, 0f);
            player.model.SetRootMotionAction(OnRootMotion);
        }
        public void CheckEnemyPosition()
        {
            Vector3 enemyPos = (player.enemyPosition.position - player.model.transform.position).normalized;
            player.Motion.FaceDirection(enemyPos, true);
        }
        public override void Update()
        {
            switch (subState)
            {
                case TCSubState.Start:
                    StartUpdate();
                    break;
                case TCSubState.FailEnd:
                    FailEndUpdate();
                    break;
                case TCSubState.SuccessEnd:
                    SuccessEndUpdate();
                    break;
            }
        }
        public void StartUpdate()
        {
            if (!player.GetTCKnife())
            {
                if (player.GetHp() <= 0f)
                {
                    if (tempFillAmount < 1f)
                    {
                        SubState = TCSubState.FailEnd;
                    }
                }
                else
                {
                    player.DecreaseHp(0.017f);
                }
            }
            else
            {
                if (player.currentUse <= 0)
                {
                    if (tempFillAmount < 1f)
                    {
                        SubState = TCSubState.FailEnd;
                    }
                }
                else
                {
                    player.DescreaseKnifeUse(0.017f);
                }
            }
            if (player.playerInput.input.Interact.triggered)
            {
                tempFillAmount += 0.1f;

            }
            if (tempFillAmount > 0f)
            {
                tempFillAmount -= 0.1f * Time.deltaTime;
            }
            if (tempFillAmount >= 1f)
            {
                player.SetQteDisplay(true, tempFillAmount, Color.green);
                SubState = TCSubState.SuccessEnd;
            }
            else if (tempFillAmount <= 0.3f)
            {
                player.SetQteDisplay(true, tempFillAmount, Color.red);
            }
            else
            {
                player.SetQteDisplay(true, tempFillAmount, Color.white);
            }
        }
        public void SuccessEndUpdate()
        {
            if (CheckAnimationName(player.currentTCName + "Success", 0, out float currentTime))
            {
                if (currentTime >= 1f)
                {
                    player.ChangeState(PlayerState.Idle);
                }
            }
        }
        public void FailEndUpdate()
        {
            if (CheckAnimationName(player.currentTCName + "Fail", 0, out float currentTime))
            {
                if (currentTime >= 1f)
                {
                    if (player.GetCurrentHp() <= 0f)
                    {
                        player.ChangeState(PlayerState.Dead);
                    }
                    else
                    {
                        player.ChangeState(PlayerState.Idle);
                    }
                }
            }
        }
        public override void Exit()
        {
            tempTime = 0f;
            tempFillAmount = 0f;
            player.SetQteDisplay(false, 0f, Color.white);
            player.model.ClearRootMotionAction();
        }
    }
    public class PlayerState_Kick : PlayerBaseState
    {
        private string currentKickAnim;
        public override void Enter()
        {
            if(Vector3.Distance(player.model.enemyTransform.position, player.model.transform.position) <= 1f)
            {
                currentKickAnim = "KickBackClose";
            }
            else
            {
                currentKickAnim = "KickBack";
            }
            player.PlayAnimaton(currentKickAnim);
            player.playerChoesLayerAnimation("Empty", 4, 0f);
            player.model.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            FaceTo();
            if (CheckAnimationName(currentKickAnim, 0, out float currentTime))
            {
                if (currentTime >= 1f)
                {
                    player.ChangeState(PlayerState.Idle);
                }
            }
        }
        public void FaceTo()
        {
            Vector3 enemyPos = (player.model.enemyTransform.position - player.model.transform.position).normalized;
            player.Motion.FaceDirection(enemyPos, true);
        }
        public override void Exit()
        {
            player.model.ClearRootMotionAction();
        }
    }
    public class PlayerState_Pick : PlayerBaseState
    {
        public override void Enter()
        {
            player.PlayAnimaton("Idle");
            player.playerChoesLayerAnimation("Pick", 7);
            player.model.AddInteract();
            player.model.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (player.playerInput.input.Block.IsPressed() && player.CheckWeapon(player.MeleeWeapon))
            {
                player.ChangeState(PlayerState.Block);
            }
            if (CheckAnimationName("Pick", 7, out float currentTime))
            {
                if (currentTime >= 1f)
                {
                    player.ChangeState(PlayerState.Idle);
                }
            }
        }
        public override void Exit()
        {
            player.playerChoesLayerAnimation("Empty", 7);
            player.model.ClearRootMotionAction();
        }
    }
    public class PlayerState_BreakItem : PlayerBaseState
    {
        private string currentBreakName;
        public override void Enter()
        {
            player.playerChoesLayerAnimation("Empty", 4);
            if (player.model.GetIsUp())
            {
                player.PlayAnimaton("ElbowAttack");
                currentBreakName = "ElbowAttack";
            }
            else
            {
                player.PlayAnimaton("Kicklow");
                currentBreakName = "Kicklow";
            }
            player.model.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (CheckAnimationName(currentBreakName, 0, out float currentTime))
            {
                if (currentTime >= 1f)
                {
                    player.ChangeState(PlayerState.Idle);
                }
            }
        }
        public override void Exit()
        {
            player.model.ClearRootMotionAction();
        }
    }
    public class PlayerState_Dead : PlayerBaseState
    {
        public override void Enter()
        {
            player.playerInput.Disable();
            player.model.animator.speed = 1f;
            player.PlayAnimaton("Dead");
            player.playerChoesLayerAnimation("Empty", 4, 0f);
            player.model.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
        }
        public override void Exit()
        {
            player.model.ClearRootMotionAction();
        }
    }
    public class PlayerState_Parry : PlayerBaseState
    {
        public override void Enter()
        {
            player.ChangeWeapon(player.MeleeWeapon);
            player.PlayAnimaton("Parry");
            player.playerChoesLayerAnimation("Empty", 4, 0f);
            player.model.SetRootMotionAction(OnRootMotion);
            AudioManager.instance.PlaySound("Parry", player.model.audioSource);
            GameObject obj = ObjectPool.Instance.GetObject(player.knifeHitSpark);
            obj.transform.position = player.knifeTrans.position;
            player.parryRange.SetActive(true);
        }
        public override void Update()
        {
            if(CheckAnimationName("Parry", 0, out float currentTime))
            {
                if(currentTime >= 0.7f)
                {
                    player.ChangeState(PlayerState.Idle);
                }
            }
        }
        public override void Exit()
        {
            player.parryRange.SetActive(false);
            player.model.ClearRootMotionAction();
        }
    }
    public class PlayerState_Hit : PlayerBaseState
    {
        protected override PlayerMotionMode MotionMode => PlayerMotionMode.Cinematic;
        public override void Enter()
        {
            switch (player.enemyPosition.gameObject.name)
            {
                case "Enemy_alpha(Clone)":
                    player.enemyPosition.GetComponent<EnemyBase>().enemyModel.ClearRootMotionAction();
                    player.enemyPosition.position = player.model.tcPositions[1].position;
                    player.enemyPosition.SetParent(player.model.tcPositions[1]);
                    CameraManager.Instance.SetVirtualCameraWeight(3);
                    //desiredDirection.y = 0;
                    break;
            }
            player.PlayAnimaton(player.currentTCName);
            player.playerChoesLayerAnimation("Empty", 4, 0f);
            player.model.SetRootMotionAction(OnRootMotion);
            //AudioManager.instance.PlaySound("Parry", player.model.audioSource);
            //GameObject obj = ObjectPool.Instance.GetObject(player.knifeHitSpark);
            //obj.transform.position = player.knifeTrans.position;
            //player.parryRange.SetActive(true);
        }
        public override void Update()
        {
            if (CheckAnimationName(player.currentTCName, 0, out float currentTime))
            {
                if (currentTime >= 1f)
                {
                    if(player.GetCurrentHp() <= 0f)
                    {
                        player.ChangeState(PlayerState.Dead);
                    }
                    else
                    {
                        player.ChangeState(PlayerState.Idle);
                    } 
                }
            }
        }
        public override void Exit()
        {
            //player.parryRange.SetActive(false);
            CameraManager.Instance.ResetVirtualCamera();
            Scene currentScene = SceneManager.GetActiveScene();
            player.currentTCName = "";
            player.enemyPosition.SetParent(null);
            SceneManager.MoveGameObjectToScene(player.enemyPosition.gameObject, currentScene);
            player.model.ClearRootMotionAction();
        }
    }
    public class PlayerState_Fall : PlayerBaseState
    {
        protected override PlayerMotionMode MotionMode => PlayerMotionMode.Airborne;
        public override void Enter()
        {
            player.PlayAnimaton(player.Motion.Animation(PlayerMotionAnimation.Fall));
            player.playerChoesLayerAnimation("Empty", 4, 0f);
            player.model.SetRootMotionAction(OnRootMotion);
            //AudioManager.instance.PlaySound("Parry", player.model.audioSource);
            //GameObject obj = ObjectPool.Instance.GetObject(player.knifeHitSpark);
            //obj.transform.position = player.knifeTrans.position;
            //player.parryRange.SetActive(true);
        }
        public override void Update()
        {
            if (player.model.CheckGround())
            {
                player.ChangeState(PlayerState.Land);
            }
        }
        public override void Exit()
        {
            //player.parryRange.SetActive(false);
            player.model.ClearRootMotionAction();
        }
    }
    public class PlayerState_Land : PlayerBaseState
    {
        public override void Enter()
        {
            player.PlayAnimaton(player.Motion.Animation(PlayerMotionAnimation.Land));
            player.playerChoesLayerAnimation("Empty", 4, 0f);
            player.model.SetRootMotionAction(OnRootMotion);
            //AudioManager.instance.PlaySound("Parry", player.model.audioSource);
            //GameObject obj = ObjectPool.Instance.GetObject(player.knifeHitSpark);
            //obj.transform.position = player.knifeTrans.position;
            //player.parryRange.SetActive(true);
        }
        public override void Update()
        {
            if (CheckAnimationName(player.Motion.Animation(PlayerMotionAnimation.Land), 0, out float currentTime))
            {
                if (currentTime >= 1f)
                {
                    player.ChangeState(PlayerState.Idle);
                }
            }
        }
        public override void Exit()
        {
            //player.parryRange.SetActive(false);
            player.model.ClearRootMotionAction();
        }
    }
}

