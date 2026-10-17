using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class Q1 : QuestStep
    {
        private void Update()
        {
            if (LevelContext.Current != null && LevelContext.Current.QuestDoor1 != null &&
                LevelContext.Current.QuestDoor1.GetOpenState())
            {
                FinishQuestStep();
            }
        }
    }
}
