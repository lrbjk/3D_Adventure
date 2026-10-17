using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class DestroyVFX : MonoBehaviour
    {
        public float destroyTime;
        private float tempTime;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (tempTime >= destroyTime)
            {
                ObjectPool.Instance.PushObject(gameObject);
                tempTime = 0;
            }
            else
            {
                tempTime += Time.deltaTime;
            }
        }
    }
}