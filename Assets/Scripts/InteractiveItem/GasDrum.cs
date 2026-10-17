using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class GasDrum : InteractiveBase
    {
        public GameObject gas;
        private bool beShooted = false;
        private Collider col;
        private Vector3 lastPos;
        private Rigidbody rb;
        public float AddForce;
        public float duringTime;
        private float tempTime;
        // Start is called before the first frame update
        void Start()
        {
            col = GetComponent<Collider>();
            rb = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            if (beShooted)
            {
                if (tempTime < duringTime)
                {
                    Vector3 cpos = col.ClosestPoint(lastPos);
                    Vector3 dir = cpos - lastPos;
                    Vector3 contactNormal = (dir).normalized;
                    rb.AddForceAtPosition(contactNormal * AddForce * Time.deltaTime, cpos, ForceMode.Impulse);
                    tempTime += Time.deltaTime;
                }

            }
        }
        public override void TakeDamage(float damage)
        {

        }
        public override void DamageVector(Vector3 pos, Quaternion rot)
        {
            if (!beShooted)
            {
                GameObject go = ObjectPool.Instance.GetObject(gas, pos, rot);
                go.transform.SetParent(transform);
                lastPos = pos;
                beShooted = true;
                ObjectPool.Instance.PushObject(go, duringTime);
            }

        }
    }
}