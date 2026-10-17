using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class EnemyNoLeg : EnemyBase
    {
        protected override void OnEnable()
        {
            SetNoLeg();
            Init();
        }
    }
}
