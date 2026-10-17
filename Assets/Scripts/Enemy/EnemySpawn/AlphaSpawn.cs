using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class AlphaSpawn : EnemySpawn
    {
        public EnemyAlptha enemyAlptha;
        public GameObject bombPoint;

        public void SetBombPoint()
        {
            enemyAlptha = enemyPrefab.GetComponent<EnemyAlptha>();
            enemyAlptha.bombPoint = bombPoint.transform;
        }
    }
}
