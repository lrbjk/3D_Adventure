using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class TargetChain : MonoBehaviour
    {
        public string tcName;
        public string blockName;
        public bool canBeBlock;
        public EnemyBase enemy;
        public GameObject bloodEffect;
        public GameObject blockEffect;
        private Coroutine bloodCoroutine;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (!canBeBlock)
                {
                    //enemy.aiDetect.SetCanAttack(true);
                    if (other.TryGetComponent(out IDamage damage))
                    {
                        damage.TakeDamageStun(0);
                        enemy.isTCHit = true;
                        damage.TakeTC(tcName, enemy.enemyModel.transform, 0f,false);
                        Vector3 hitPosition = other.ClosestPoint(transform.position);
                        SpawnEffect(bloodEffect, hitPosition);
                    }
                }
                else
                {
                    if(other.TryGetComponent(out IDamage damage))
                    {
                        damage.TakeDamageStun(1);
                        if (!damage.InBlock())
                        {
                            damage.TakeDamage(enemy.enemiesInfo.damage * 3);
                            enemy.ChangeState(EnemyState.TCEnd);
                            enemy.isTCHit = true;
                            damage.TakeTC(tcName, enemy.transform, 0f,true);
                            Vector3 hitPosition = other.ClosestPoint(transform.position);
                            SpawnEffect(bloodEffect, hitPosition);
                        }
                        else
                        {
                            GameObject obj = SpawnEffect(blockEffect, GamePlay.Instance.player.knifeTrans.position);
                            if (obj != null)
                            {
                                obj.transform.SetParent(GamePlay.Instance.player.knifeTrans);
                                enemy.SetCurrentVFX(obj);
                            }
                            enemy.ChangeState(EnemyState.TCLoop);
                            enemy.isTCHit = true;
                            damage.TakeTC(blockName, enemy.transform, 0f, false);
                        }
                    }
                }
                
            }
        }

        private GameObject SpawnEffect(GameObject effectPrefab, Vector3 position)
        {
            if (effectPrefab == null || ObjectPool.Instance == null)
            {
                return null;
            }

            return ObjectPool.Instance.GetObject(effectPrefab, position, Quaternion.identity);
        }
    }
}
