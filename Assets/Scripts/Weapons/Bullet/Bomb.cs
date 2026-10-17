using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class Bomb : BezierCurve
    {
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.collider.CompareTag("Enemy") && !collision.collider.CompareTag("Untagged"))
            {
                GameObject obj = ObjectPool.Instance.GetObject(stayEffect);
                obj.transform.position = gameObject.transform.position;
                ObjectPool.Instance.PushObject(gameObject);
            }
        }
    }
}
