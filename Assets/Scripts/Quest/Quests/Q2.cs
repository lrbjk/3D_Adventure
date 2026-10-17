using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class Q2 : QuestStep
    {
        private void Update()
        {
            if (LevelContext.Current != null && LevelContext.Current.QuestDoor2 != null &&
                LevelContext.Current.QuestDoor2.GetOpenState())
            {
                FinishQuestStep();
            }
        }
    }
}
