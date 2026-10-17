using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Astraparasitus
{
    public static class StaticData
    {
        public static int enemyId = -1;
        public static void AddEnemyId()
        {
            enemyId++;
        }
    }
}
