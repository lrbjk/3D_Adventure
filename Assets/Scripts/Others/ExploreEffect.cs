using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class ExploreEffect : MonoBehaviour
    {
        public float damageRadius = 1f;
        public float explosionForce = 10f;
        // Start is called before the first frame update
        private void OnEnable()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, damageRadius);

            foreach (Collider objectInRange in colliders)
            {
                Rigidbody rb = objectInRange.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, damageRadius);
                }
            }
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, damageRadius);
        }
    }
}
