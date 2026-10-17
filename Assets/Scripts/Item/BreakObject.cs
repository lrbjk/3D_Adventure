using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class BreakObject : MonoBehaviour
    {
        private Coroutine breakCoroutine;
        public float time;
        private Rigidbody rb;
        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            if (!rb.isKinematic)
            {
                breakCoroutine = StartCoroutine(Onbreak(time));
            }
        }
        public void Break()
        {
            gameObject.SetActive(false);
        }
        private IEnumerator Onbreak(float time)
        {
            yield return new WaitForSecondsRealtime(time);
            Break();
        }
    }
}
