using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum QuestStepStatus
{
    未达到任务需求,
    开始任务,
    任务中,
    能否完成任务,
    任务完成,
}
namespace Astraparasitus
{
    public abstract class QuestStep : MonoBehaviour
    {
        private bool isFinished = false;
        protected string questId;
        protected QuestType questType;
        //protected QuestStepStatus questStepStatus;
        public void InstantiateQuestStep(string questId, QuestType questType, QuestStepStatus questStepStatus)
        {
            this.questId = questId;
            this.questType = questType;
            //this.questStepStatus = questStepStatus;
        }
        protected void FinishQuestStep()
        {
            if (!isFinished)
            {
                isFinished = true;
                QuestManager.Instance.questEvent.FinishQuest(questId);
                Destroy(this.gameObject);
            }
        }
    }
}