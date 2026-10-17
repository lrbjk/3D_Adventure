using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class EnemyAlptha : EnemyBase
    {
        public bool isfirstIdle = true;
        public Transform bombPoint;
        protected override void OnEnable()
        {
            base.OnEnable();
            bombPoint = GameObject.Find("bombPoint").transform;
        }
        private void OnDisable()
        {
            Death();
        }
    }
}
