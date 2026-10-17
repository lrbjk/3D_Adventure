using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class ExplodingDamage : MonoBehaviour
    {
        public float radius;
        public float damage;
        public float damageForPlayer;
        public float addForce;
        // Start is called before the first frame update
        void OnEnable()
        {
            Invoke("Explode", 0.15f);
        }

        // Update is called once per frame
        void Update()
        {

        }
        public void Explode()
        {
            Collider[] colliders = new Collider[100];
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, colliders);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    if (colliders[i].GetComponent<Rigidbody>() != null)
                    {
                        colliders[i].GetComponent<Rigidbody>().AddExplosionForce(addForce, transform.position, radius, 0f, ForceMode.Impulse);
                    }
                    if (colliders[i].TryGetComponent(out IDamage damageable) && colliders[i].CompareTag("Enemy"))
                    {
                        switch (colliders[i].GetComponent<EnemyBase>().enemiesInfo.size)
                        {
                            case 1:
                                damageable.TakeTC("HitBack0", transform, damage, true);
                                damageable.TakeDamageStun(damage);
                                break;
                            case 2:
                                damageable.TakeDamage(damage);
                                damageable.TakeDamageStun(damage);
                                break;
                            case 3:
                                damageable.TakeTC("BigHit", transform, damage, true);
                                damageable.TakeDamage(damage);
                                damageable.TakeDamageStun(damage*0.1f);
                                break;
                        }
                    }
                    if (colliders[i].CompareTag("Player"))
                    {
                        if (colliders[i].TryGetComponent(out IDamage damage))
                        {
                            damage.TakeTC("Hit_Axe", transform, damageForPlayer, true);
                            damage.TakeDamage(damageForPlayer);
                        }
                    }
                }
            }
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
