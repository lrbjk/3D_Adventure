using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class CameraManager : SingletonMono<CameraManager>
    {
        public CinemachineVirtualCamera[] VirtualCameras;
        private int currentIndex = 0;
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
        public void SetVirtualCameraWeight(int index)
        {
            VirtualCameras[currentIndex].Priority = 0;
            VirtualCameras[index].Priority = 10;
            currentIndex = index;
        }
        public void ResetVirtualCamera()
        {
            VirtualCameras[currentIndex].Priority = 0;
            VirtualCameras[0].Priority = 10;
            currentIndex = 0;
        }

    }
}
