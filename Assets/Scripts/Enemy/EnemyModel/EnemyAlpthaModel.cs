using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class EnemyAlpthaModel : EnemyModel
    {
        public EnemyAlptha enemyAlptha;
        public ShakeData roarShake;
        public ShakeData footStepShake;
        public void RoarShake()
        {
            CameraShakerHandler.Shake(roarShake);
        }
        public void FootStepShake()
        {
            CameraShakerHandler.Shake(footStepShake);
        }
        public void SetIsfirstIdle()
        {
            enemyAlptha.isfirstIdle = false;
        }
    }
}
