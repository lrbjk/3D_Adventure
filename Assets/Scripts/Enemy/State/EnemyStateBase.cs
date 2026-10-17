using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class EnemyStateBase : StateBase
    {
        protected EnemyBase enemy;
        public override void Init(IStateMachineOwner owner)
        {
            base.Init(owner);
            enemy = (EnemyBase)owner;
        }
        protected virtual bool CheckAnimationName(string stateName, int layerIndex, out float currentTime)
        {
            AnimatorStateInfo info = enemy.enemyModel.animator.GetCurrentAnimatorStateInfo(layerIndex);
            currentTime = info.normalizedTime;
            return info.IsName(stateName);
        }
        protected virtual void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            deltaPosition.y = enemy.gravity * Time.deltaTime;
            //player.transform.rotation = deltaRotation;
            enemy.controller.Move(deltaPosition * enemy.currentSpeed);
        }
    }
}