using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class UIFollow : MonoBehaviour
    {
        private Camera mainCamera;
        // Start is called before the first frame update
        void Start()
        {
            //mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        }

        // Update is called once per frame
        void Update()
        {
            transform.localRotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
    }
}