using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class EnemySpawn : MonoBehaviour
    {
        public GameObject enemyPrefab;
        public int count = 1;
        public float spawnRadius = 5f;
        //public float spawnOffset = 0f;
        public Vector3 direction;
        [Header("生成设置")]
        public bool isAttack = false;
        public bool isChase = false;
        public bool isNoLeg = false;
        public void SpawnEnemy()
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning("Enemy prefab is missing.", this);
                return;
            }

            Vector3 spawnDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            Quaternion rotation = Quaternion.LookRotation(spawnDirection);
            // 实例化敌人
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * spawnRadius;
                Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);
                //GameObject enemy = ObjectPool.Instance.GetObject(enemyPrefab);
                //enemy.transform.localPosition = position;
                //enemy.transform.localRotation = rotation;
                GameObject enemy = Instantiate(enemyPrefab, position, rotation);
                EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
                if (enemyBase == null)
                {
                    Debug.LogError("Enemy prefab must contain EnemyBase.", enemyPrefab);
                    Destroy(enemy);
                    continue;
                }

                enemyBase.InitNavMesh();
                if (isAttack)
                {
                    enemyBase.SetCanAttack();
                }
                if (isChase)
                {
                    enemyBase.SetEnemyChase();
                }
                if (isNoLeg)
                {
                    enemyBase.SetNoLeg();
                }
            }
        }

        private void OnValidate()
        {
            count = Mathf.Max(1, count);
            spawnRadius = Mathf.Max(0f, spawnRadius);
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector3.forward;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            Gizmos.color = Color.blue;
            Vector3 rayEnd = transform.position + direction * 10f;
            Gizmos.DrawRay(transform.position, rayEnd);
        }
    }
}
