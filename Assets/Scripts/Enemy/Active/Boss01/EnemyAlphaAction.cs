using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class PickBomb : EnemyAction
    {
        public EnemyAlptha enemyAlptha;
        public override void OnAwake()
        {
            base.OnAwake();
            enemyAlptha = GetComponent<EnemyAlptha>();
        }
        public override void OnStart()
        {
            enemy.UpdateAgent(enemyAlptha.bombPoint);
            enemy.ChangeState(EnemyState.Chase);
        }
        public override TaskStatus OnUpdate()
        {
            if(Vector3.Distance(enemy.transform.position, enemyAlptha.bombPoint.position) < 0.5f)
            {
                enemy.ChangeState(EnemyState.Pick);
            }
            if (enemy.enemyModel.weapon.activeSelf)
            {
                return TaskStatus.Success;
            }
            return TaskStatus.Running;
        }
    }
    public class ShowUp : EnemyAction
    {
        public EnemyAlptha enemyAlptha;
        public override void OnAwake()
        {
            base.OnAwake();
            enemyAlptha = GetComponent<EnemyAlptha>();
        }
        public override void OnStart()
        {
            enemy.ChangeState(EnemyState.ShowUp);
        }
        public override TaskStatus OnUpdate()
        {
            if (!enemyAlptha.isfirstIdle)
            {
                return TaskStatus.Success;
            }
            return TaskStatus.Running;
        }
    }
}
