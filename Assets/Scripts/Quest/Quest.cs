using Astraparasitus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quest
{
    public QuestInfo info;
    public QuestStepStatus state;
    private int currentMainQuestStepIndex;
    private int currentSubQuestStepIndex;
    //private int currentFavQuestStepIndex;

    public Quest(QuestInfo info)
    {
        this.info = info;
        this.state = QuestStepStatus.未达到任务需求;
        this.currentMainQuestStepIndex = 0;
    }
    public void MoveToNextStep()
    {
        switch (info.type)
        {
            case QuestType.主线任务:
                currentMainQuestStepIndex++;
                break;
            case QuestType.支线任务:
                currentSubQuestStepIndex++;
                break;
        }
        
    }
    public bool CurrentStepExists()
    {
        bool exists = false;
        switch (info.type)
        {
            case QuestType.主线任务:
                exists = currentMainQuestStepIndex < info.questStepPrefab.Length;
                break;
            case QuestType.支线任务:
                exists = currentSubQuestStepIndex < info.questStepPrefab.Length;
                break;
        }
        return exists;
    }
    public void InstantiateCurrentQueatStep(Transform parentTransform)
    {
        GameObject questStepPrefab = GetCurrentQuestStepPrefab();
        if(questStepPrefab!= null)
        {
            QuestStep questStep = Object.Instantiate<GameObject>(questStepPrefab, parentTransform).GetComponent<QuestStep>();
            questStep.InstantiateQuestStep(info.id,info.type,state);
        }
    }
    private GameObject GetCurrentQuestStepPrefab()
    {
        GameObject questStepPrefab = null;
        if (CurrentStepExists())
        {
            switch (info.type)
            {
                case QuestType.主线任务:
                    questStepPrefab = info.questStepPrefab[currentMainQuestStepIndex];
                    break;
                case QuestType.支线任务:
                    questStepPrefab = info.questStepPrefab[currentSubQuestStepIndex];
                    break;
            }
        }
        else
        {
        }
        return questStepPrefab;
    }
}
