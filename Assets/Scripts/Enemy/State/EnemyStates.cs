using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class Enemy_Idle : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.currentLegState = EnemyLegState.Normal;
            enemy.PlayAnimaton(enemy.enemiesInfo.idle);
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (!enemy.audioSource.isPlaying && enemy.audioSource.isActiveAndEnabled) AudioManager.instance.PlaySound(enemy.idleSound, enemy.audioSource);
        }
        public override void Exit()
        {
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_Chase : EnemyStateBase
    {
        private Vector3 randomPosition;
        private Transform aimPosition;
        public override void Enter()
        {
            enemy.currentLegState = EnemyLegState.Normal;
            enemy.PlayAnimaton(enemy.enemiesInfo.chase);
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
            randomPosition = GamePlay.Instance.player.model.FindPlayerPosition();
            aimPosition = enemy.currentAimPosition;
            
            enemy.InitNavMesh();
        }
        public override void Update()
        {
            if (enemy == null || !enemy.isActiveAndEnabled || enemy.enemyModel == null)
            {
                return;
            }

            if (enemy.audioSource != null && enemy.audioSource.isActiveAndEnabled && !enemy.audioSource.isPlaying)
            {
                AudioManager.instance.PlaySound(enemy.chaseSound, enemy.audioSource);
            }
            //enemy.UpdateNavMeshBuildSource();
            //enemy.UpdateNavMash();
            aimPosition = enemy.currentAimPosition;
            if (aimPosition == null)
            {
                return;
            }
            if (Vector3.Distance(enemy.enemyModel.transform.position, aimPosition.position) > 3)
            {
                enemy.UpdateAgent(aimPosition.position + randomPosition);
            }
            else
            {
                enemy.UpdateAgent(aimPosition);
            }
            
            
            //Vector3 dir = (GamePlay.Instance.player.transform.position - enemy.enemyModel.transform.position).normalized;
            //Vector3 turnForward = Vector3.RotateTowards(enemy.enemyModel.transform.forward, dir, 30f * Time.deltaTime, 0f); // 降低旋转速度
            //turnForward.y = 0f;
            //enemy.enemyModel.transform.rotation = Quaternion.Lerp(enemy.enemyModel.transform.rotation, Quaternion.LookRotation(turnForward), 10f * Time.deltaTime); // 使用插值函数进一步平滑旋转
            if (enemy.agent != null && enemy.agent.hasPath)
            {
                // 获取导航方向
                Vector3 desiredDirection = (enemy.agent.steeringTarget - enemy.enemyModel.transform.position).normalized;
                Vector3 turnForward = Vector3.RotateTowards(enemy.enemyModel.transform.forward, desiredDirection, 90f * Time.deltaTime, 0f);
                turnForward.y = 0f;
                enemy.enemyModel.transform.rotation = Quaternion.Lerp(enemy.enemyModel.transform.rotation, Quaternion.LookRotation(turnForward), 10f * Time.deltaTime);
                //desiredDirection.y = 0;
                ////Debug.Log(desiredDirection);
                //if (desiredDirection != Vector3.zero)
                //{
                //    // 将根运动位移重定向到导航方向
                //    //float rootMotionMagnitude = deltaPosition.magnitude;
                //    ///finalMovement = desiredDirection * rootMotionMagnitude;

                //    // 平滑旋转朝向导航方向
                //    Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
                //    targetRotation.y = 0f;
                //    enemy.enemyModel.transform.rotation = Quaternion.Slerp(enemy.enemyModel.transform.rotation, targetRotation,
                //        180f * Time.deltaTime);
                //}
            }
        }
        protected override void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            if (enemy == null || enemy.controller == null) return;

            Vector3 finalMovement = deltaPosition;
            //Debug.Log(enemy.agent.hasPath);
            // 如果正在导航，将根运动位移朝向导航方向

            finalMovement.y = enemy.gravity * Time.deltaTime;
            // 应用移动
            
            enemy.controller.Move(finalMovement * enemy.currentSpeed);
            enemy.agent.nextPosition = enemy.transform.position;
        }
        public override void Exit()
        {
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_Break : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.PlayAnimaton(enemy.enemiesInfo.Break);
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
            AudioManager.instance.PlaySound(enemy.breakSound, enemy.audioSource);
        }
        public override void Update()
        {

        }
        public override void Exit()
        {
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_ToNoLeg : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.currentLegState = EnemyLegState.Broken;
            enemy.PlayAnimaton(enemy.enemiesInfo.toNoLeg);
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (CheckAnimationName(enemy.enemiesInfo.toNoLeg, 0, out float time))
            {
                if (time >= 0.7f)
                {
                    enemy.SetLegBreakState(true);
                }
            }
        }
        public override void Exit()
        {
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_NoLeg : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.PlayAnimaton(enemy.enemiesInfo.noLegIdle);
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (!enemy.audioSource.isPlaying && enemy.audioSource.isActiveAndEnabled)
            {
                AudioManager.instance.PlaySound(enemy.idleSound, enemy.audioSource);
            }
        }
        public override void Exit()
        {
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_NoLegChase : EnemyStateBase
    {
        private Vector3 randomPosition;
        public override void Enter()
        {
            enemy.PlayAnimaton(enemy.enemiesInfo.noLegChase);
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
            randomPosition = GamePlay.Instance.player.model.FindPlayerPosition();
            enemy.InitNavMesh();
        }
        public override void Update()
        {
            if (enemy == null || !enemy.isActiveAndEnabled || enemy.enemyModel == null)
            {
                return;
            }

            if (enemy.audioSource != null && enemy.audioSource.isActiveAndEnabled && !enemy.audioSource.isPlaying)
            {
                AudioManager.instance.PlaySound(enemy.chaseSound, enemy.audioSource);
            }
            //enemy.UpdateNavMeshBuildSource();
            //enemy.UpdateNavMash();
            if (Vector3.Distance(enemy.enemyModel.transform.position, GamePlay.Instance.player.transform.position) > 3)
            {
                enemy.UpdateAgent(GamePlay.Instance.player.transform.position + randomPosition);
            }
            else
            {
                enemy.UpdateAgent(GamePlay.Instance.player.transform);
            }
            Vector3 dir = (GamePlay.Instance.player.transform.position - enemy.enemyModel.transform.position).normalized;
            Vector3 turnForward = Vector3.RotateTowards(enemy.enemyModel.transform.forward, dir, 30f * Time.deltaTime, 0f); // 降低旋转速度
            turnForward.y = 0f;
            enemy.enemyModel.transform.rotation = Quaternion.Lerp(enemy.enemyModel.transform.rotation, Quaternion.LookRotation(turnForward), 10f * Time.deltaTime); // 使用插值函数进一步平滑旋转
        }
        protected override void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            if (enemy == null || enemy.controller == null) return;

            Vector3 finalMovement = deltaPosition;
            //Debug.Log(enemy.agent.hasPath);
            // 如果正在导航，将根运动位移朝向导航方向

            finalMovement.y = enemy.gravity * Time.deltaTime;
            // 应用移动

            enemy.controller.Move(finalMovement * enemy.currentSpeed);
            enemy.agent.nextPosition = enemy.transform.position;
        }
        public override void Exit()
        {
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_Attack : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.SetSkillReady(false);
            enemy.PlayAnimaton(enemy.enemyModel.skillConfigs[enemy.GetSkillIndex()].skillName);
            enemy.DecreaseEndurance(enemy.enemyModel.skillConfigs[enemy.GetSkillIndex()].endurance);
            Vector3 dir = (GamePlay.Instance.player.transform.position - enemy.enemyModel.transform.position).normalized;
            Vector3 turnForward = Vector3.RotateTowards(enemy.enemyModel.transform.forward, dir, 30f, 0f); // 降低旋转速度
            turnForward.y = 0f;
            enemy.enemyModel.transform.rotation = Quaternion.Lerp(enemy.enemyModel.transform.rotation, Quaternion.LookRotation(turnForward), 100f); // 使用插值函数进一步平滑旋转
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
            AudioManager.instance.PlaySound(enemy.attackSound, enemy.audioSource);
        }
        public override void Update()
        {
            if (CheckAnimationName(enemy.enemyModel.skillConfigs[enemy.GetSkillIndex()].skillName, 0, out float time))
            {
                if (time >= 1f)
                {
                    enemy.SetSkillReady(true);
                    if (!enemy.isTCHit)
                    {
                        enemy.aiDetect.SetCanAttack(false);
                    }
                    
                }
            }
        }
        public override void Exit()
        {
            enemy.isTCHit = false;
            enemy.enemyModel.EndAttack();
            enemy.aiDetect.SetCanAttack(false);
            enemy.aiDetect.SetCanFarAttack(false);
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_SpAttack : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.SetSkillReady(false);
            enemy.PlayAnimaton(enemy.enemyModel.skillConfigs[enemy.GetSkillIndex()].skillName);
            enemy.DecreaseEndurance(enemy.enemyModel.skillConfigs[enemy.GetSkillIndex()].endurance);
            
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
            AudioManager.instance.PlaySound(enemy.attackSound, enemy.audioSource);
        }
        public override void Update()
        {
            FaceTo();
            if (CheckAnimationName(enemy.enemyModel.skillConfigs[enemy.GetSkillIndex()].skillName, 0, out float time))
            {
                if (time >= 1f)
                {
                    enemy.SetSkillReady(true);
                    if (!enemy.isTCHit)
                    {
                        enemy.aiDetect.SetCanAttack(false);
                    }

                }
            }
        }
        private void FaceTo()
        {
            Vector3 dir = (GamePlay.Instance.player.transform.position - enemy.enemyModel.transform.position).normalized;
            Vector3 turnForward = Vector3.RotateTowards(enemy.enemyModel.transform.forward, dir, 30f, 0f); // 降低旋转速度
            turnForward.y = 0f;
            enemy.enemyModel.transform.rotation = Quaternion.Lerp(enemy.enemyModel.transform.rotation, Quaternion.LookRotation(turnForward), 100f); // 使用插值函数进一步平滑旋转
        }
        public override void Exit()
        {
            enemy.isTCHit = false;
            enemy.enemyModel.EndAttack();
            enemy.aiDetect.SetCanAttack(false);
            enemy.aiDetect.SetCanFarAttack(false);
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_Hit : EnemyStateBase
    {
        private bool canEnd = false;
        public string currentHitAnimName;
        public override void Enter()
        {
            enemy.enemyModel.EndAttack();
            Vector3 dir = (GamePlay.Instance.player.transform.position - enemy.enemyModel.transform.position).normalized;
            dir.y = 0f;
            enemy.enemyModel.transform.rotation = Quaternion.Lerp(enemy.enemyModel.transform.rotation, Quaternion.LookRotation(dir), 1000f);
            enemy.PlayAnimaton(enemy.dieAnimName);
            currentHitAnimName = enemy.dieAnimName;
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (CheckAnimationName(currentHitAnimName, 0, out float time))
            {
                if (enemy.GetCurrentHp() > 0 && currentHitAnimName.Contains("0"))
                {
                    canEnd = true;
                    enemy.enemyModel.animator.SetBool("CanEnd", canEnd);
                }
                else
                {
                    if (time >= 1f)
                    {
                        enemy.SetHit(false);
                    }
                }
            }
            if (canEnd)
            {
                if (CheckAnimationName(currentHitAnimName + "_End", 0, out float time1))
                {
                    if (time1 >= 0.8f)
                    {
                        canEnd = false;
                        enemy.enemyModel.animator.SetBool("CanEnd", canEnd);
                        enemy.SetHit(false);
                    }
                }
            }
        }
        public override void Exit()
        {
            canEnd = false;
            enemy.dieAnimName = "";
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_Dead : EnemyStateBase
    {
        public override void Enter()
        {
            if (enemy.dieAnimName == "")
            {
                enemy.dieAnimName = "NormalDie";
            }
            enemy.enemyModel.EndAttack();
            enemy.PlayAnimaton(enemy.dieAnimName);
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
            AudioManager.instance.PlaySound(enemy.dieSound, enemy.audioSource);
        }
        public override void Update()
        {
            if (CheckAnimationName(enemy.dieAnimName, 0, out float time))
            {
                if (time >= 1f)
                {
                    enemy.Death();
                    enemy.enemyModel.animator.enabled = false;
                    enemy.CloseIK(enemy.root);
                    enemy.SpawnLoot();
                    enemy.ChangeState(EnemyState.Idle);
                }
            }
        }
        public override void Exit()
        {
            enemy.dieAnimName = "";
            enemy.enemyModel.ClearRootMotionAction();
            enemy.enemyModel.PlayDistroyEffect();
        }
    }
    public class Enemy_Recovery : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.currentLegState = EnemyLegState.Normal;
            enemy.PlayAnimaton("PullStart");
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (!enemy.audioSource.isPlaying && enemy.audioSource.isActiveAndEnabled) AudioManager.instance.PlaySound(enemy.idleSound, enemy.audioSource);
            if(CheckAnimationName("PullStart", 0, out float time))
            {
                if(time >= 1)
                {
                    enemy.ChangeState(EnemyState.Idle);
                }
            }
        }
        public override void Exit()
        {
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_TCEnd : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.PlayAnimaton(enemy.enemyModel.skillConfigs[enemy.GetSkillIndex()].skillName+"_End");
            Vector3 dir = (GamePlay.Instance.player.transform.position - enemy.enemyModel.transform.position).normalized;
            Vector3 turnForward = Vector3.RotateTowards(enemy.enemyModel.transform.forward, dir, 30f, 0f); // 降低旋转速度
            turnForward.y = 0f;
            enemy.enemyModel.transform.rotation = Quaternion.Lerp(enemy.enemyModel.transform.rotation, Quaternion.LookRotation(turnForward), 100f); // 使用插值函数进一步平滑旋转
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (CheckAnimationName(enemy.enemyModel.skillConfigs[enemy.GetSkillIndex()].skillName + "_End", 0, out float time))
            {
                if (time >= 1f)
                {
                    enemy.SetSkillReady(true);
                    enemy.aiDetect.SetCanAttack(false);
                }
            }
        }
        public override void Exit()
        {
            enemy.isTCHit = false;
            enemy.enemyModel.EndAttack();
            enemy.aiDetect.SetCanAttack(false);
            enemy.aiDetect.SetCanFarAttack(false);
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_TCLoop : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.PlayAnimaton(enemy.enemyModel.skillConfigs[enemy.GetSkillIndex()].skillName + "Loop");
            //enemy.GetCurrentVFX().transform.position = GamePlay.Instance.player.knifeTrans.position;
            Vector3 dir = (GamePlay.Instance.player.transform.position - enemy.enemyModel.transform.position).normalized;
            Vector3 turnForward = Vector3.RotateTowards(enemy.enemyModel.transform.forward, dir, 30f, 0f); // 降低旋转速度
            turnForward.y = 0f;
            enemy.enemyModel.transform.rotation = Quaternion.Lerp(enemy.enemyModel.transform.rotation, Quaternion.LookRotation(turnForward), 100f); // 使用插值函数进一步平滑旋转
            //enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {

        }
        public override void Exit()
        {
            if (enemy.GetCurrentVFX() != null)
            {
                ObjectPool.Instance.PushObject(enemy.GetCurrentVFX());
            }
            enemy.isTCHit = false;
            enemy.enemyModel.EndAttack();
            enemy.aiDetect.SetCanAttack(false);
            enemy.aiDetect.SetCanFarAttack(false);
            //enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_Other : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.PlayAnimaton(enemy.enemiesInfo.other);
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            
        }
        public override void Exit()
        {
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_Angery : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.PlayAnimaton("Angery");
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if(CheckAnimationName("Angery", 0, out float time))
            {
                if(time >= 1f)
                {
                    enemy.ChangeState(EnemyState.Idle);
                }
            }
        }
        public override void Exit()
        {
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_PickBomb : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.PlayAnimaton("Pick");
            enemy.UpdateAgent(GamePlay.Instance.player.transform);
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (CheckAnimationName("Pick", 0, out float time))
            {
                if (time >= 1f)
                {
                    enemy.enemyModel.weapon.SetActive(true);
                    enemy.ChangeState(EnemyState.Idle);
                }
            }
        }
        public override void Exit()
        {
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
    public class Enemy_ShowUp : EnemyStateBase
    {
        public override void Enter()
        {
            enemy.PlayAnimaton("ShowUp");
            enemy.UpdateAgent(GamePlay.Instance.player.transform);
            enemy.enemyModel.SetRootMotionAction(OnRootMotion);
        }
        public override void Update()
        {
            if (CheckAnimationName("ShowUp", 0, out float time))
            {
                if (time >= 1f)
                {
                    enemy.ChangeState(EnemyState.Idle);
                }
            }
        }
        public override void Exit()
        {
            enemy.enemyModel.ClearRootMotionAction();
        }
    }
}