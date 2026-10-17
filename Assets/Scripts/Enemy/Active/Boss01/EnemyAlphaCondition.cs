using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class CanFarAttack_Alpha : EnemyConditionals
    {
        //public float distance;
        public override TaskStatus OnUpdate()
        {
            if (enemy.aiDetect.GetCanFarAttack()&& (enemy.GetEndurance() > 0))
            {
                return TaskStatus.Success;
            }
            else
            {
                return TaskStatus.Failure;
            }
        }
    }
    public class DontHaveWeapon: EnemyConditionals
    {
        public override TaskStatus OnUpdate()
        {
            if (!enemy.enemyModel.CheckWeapon())
            {
                return TaskStatus.Success;
            }
            else
            {
                return TaskStatus.Failure;
            }
        }
    }
    public class IsShowUp : EnemyConditionals
    {
        public EnemyAlptha enemyAlptha;
        public override void OnAwake()
        {
            base.OnAwake();
            enemyAlptha = GetComponent<EnemyAlptha>();
        }
        public override TaskStatus OnUpdate()
        {
            if (enemyAlptha.isfirstIdle)
            {
                return TaskStatus.Success;
            }
            else
            {
                return TaskStatus.Failure;
            }
        }
    }
}
