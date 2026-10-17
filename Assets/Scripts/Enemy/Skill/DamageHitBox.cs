using UnityEngine;
namespace Astraparasitus
{
    public class DamageHitBox : MonoBehaviour
    {
        public string hitName;
        public string targetTag = "Player";
        public EnemyBase enemy;
        public GameObject bloodEffect;
        public float damageRate = 1;
        [Header("��������")]
        public int hitlayer = 6;
        public bool canBeBlocked = false;
        public float decreaseKnifeUse = 0;
        public bool hardHit = false;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(targetTag))
            {
                if (!canBeBlocked)
                {
                    if (other.TryGetComponent(out IDamage damage))
                    {
                        
                        if (hardHit)
                        {
                            damage.TakeDamage(enemy.enemiesInfo.damage * damageRate);
                            damage.TakeTC(hitName, enemy.transform, enemy.enemiesInfo.damage * damageRate, true);
                        }
                        else
                        {
                            damage.TakeDamage(enemy.enemiesInfo.damage * damageRate);
                            damage.DamageName(hitName, hitlayer);
                        }
                        Vector3 hitPosition = other.ClosestPoint(transform.position);
                        SpawnBloodEffect(hitPosition);
                    }
                }
                else
                {
                    if (other.TryGetComponent(out IDamage damage))
                    {
                        if (!damage.InBlock())
                        {
                            
                            if (hardHit)
                            {
                                damage.TakeTC(hitName, enemy.transform, enemy.enemiesInfo.damage * damageRate, true);
                            }
                            else
                            {
                                damage.TakeDamage(enemy.enemiesInfo.damage * damageRate);
                                damage.DamageName(hitName, hitlayer);
                            }
                            Vector3 hitPosition = other.ClosestPoint(transform.position);
                            SpawnBloodEffect(hitPosition);
                        }
                        else
                        {
                            GamePlay.Instance.player.DescreaseKnifeUse(decreaseKnifeUse);
                            damage.DamageName("Hit_defence", hitlayer);
                        }
                    }
                }
            }
        }

        private void SpawnBloodEffect(Vector3 position)
        {
            if (bloodEffect != null && ObjectPool.Instance != null)
            {
                ObjectPool.Instance.GetObject(bloodEffect, position, Quaternion.identity);
            }
        }

    }
}
