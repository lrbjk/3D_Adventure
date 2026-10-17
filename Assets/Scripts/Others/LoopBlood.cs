using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class LoopBlood : MonoBehaviour
    {
        public GameObject bloodPrefab;
        public float loopTime;
        private float currentTime = 0f;
        private Coroutine loop;
        // Start is called before the first frame update
        void Start()
        {
            GameObject obj = ObjectPool.Instance.GetObject(bloodPrefab);
            obj.transform.position = transform.position;
            loop = StartCoroutine(Loop());
        }
        // Update is called once per frame
        void Update()
        {
            if(currentTime >= loopTime)
            {
                StopCoroutine(loop);
                currentTime = 0f;
                ObjectPool.Instance.PushObject(gameObject);
            }
        }
        private IEnumerator Loop()
        {
            yield return new WaitForSeconds(0.033f);
            GameObject obj = ObjectPool.Instance.GetObject(bloodPrefab);
            obj.transform.position = transform.position;
        }
    }
}
