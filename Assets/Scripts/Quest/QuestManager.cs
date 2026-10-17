using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Astraparasitus
{
    public class QuestManager : SingletonMono<QuestManager>
    {
        public QuestEvent questEvent = new QuestEvent();
        private Dictionary<string, Quest> questsMap;
        public List<QuestInfo> questInfo = new List<QuestInfo>();
        private int currentQuestIndex = 0;
        private QuestStepStatus currentStepStatus;
        public bool AllCompleted { get; private set; }
        //public TextMeshProUGUI[] texts;
        //public QuestUI questUI;
        protected override void Awake()
        {
            // 场景预制体把 QuestManager 与 GamePlay 放在同一个对象上。
            // 首次运行时将任务系统迁移到独立常驻对象，避免连同 GamePlay、UI 和场景引用一起常驻。
            if (GetComponent<GamePlay>() != null)
            {
                if (Instance == null)
                {
                    GameObject questManagerObject = new GameObject(nameof(QuestManager));
                    QuestManager persistentManager = questManagerObject.AddComponent<QuestManager>();
                    persistentManager.questInfo = new List<QuestInfo>(questInfo);
                }

                enabled = false;
                Destroy(this);
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            //questEvent = new QuestEvent();
            questsMap = CreateQuestsMap();

        }
        private void OnEnable()
        {
            questEvent.onStartQuest += StartQuest;
            questEvent.onAdvanceQuest += AdvanceQuest;
            questEvent.onFinishQuest += FinishQuest;
            questEvent.onQuestStateChange += QuestStateChange;
        }
        private void OnDisable()
        {
            questEvent.onStartQuest -= StartQuest;
            questEvent.onAdvanceQuest -= AdvanceQuest;
            questEvent.onFinishQuest -= FinishQuest;
            questEvent.onQuestStateChange -= QuestStateChange;
        }
        private void Start()
        {
            foreach (Quest quest in questsMap.Values)
            {
                questEvent.QuestStateChange(quest);
            }
            QuestStart();
        }
        private void QuestStart()
        {
            if (currentQuestIndex < questInfo.Count)
            {
                questEvent.StartQuest(questInfo[currentQuestIndex].id);
            }
            else
            {
                AllCompleted = true;
            }
        }
        public void ChangeQuestState(string id, QuestStepStatus state)
        {
            Quest quest = GetQuest(id);
            if (quest == null)
            {
                return;
            }
            quest.state = state;
            questEvent.QuestStateChange(quest);
        }
        private void QuestStateChange(Quest quest)
        {
            if (currentQuestIndex < questInfo.Count && quest.info.id.Equals(questInfo[currentQuestIndex].id))
            {
                currentStepStatus = quest.state;
            }

        }
        private bool CheckRequirements(Quest quest)
        {
            bool requirementsMet = true;
            foreach (QuestInfo prerequisiteQuestInfo in quest.info.prerequisites)
            {
                Quest prerequisiteQuest = GetQuest(prerequisiteQuestInfo.id);
                if (prerequisiteQuest == null || prerequisiteQuest.state != QuestStepStatus.任务完成)
                {
                    requirementsMet = false;
                }
            }
            return requirementsMet;
        }
        private void Update()
        {
            foreach (Quest quest in questsMap.Values)
            {
                if (quest.state == QuestStepStatus.未达到任务需求 && CheckRequirements(quest))
                {
                    ChangeQuestState(quest.info.id, QuestStepStatus.开始任务);

                }
            }
            if (!AllCompleted && currentStepStatus == QuestStepStatus.任务完成)
            {
                //questInfo.RemoveAt(currentQuestIndex);
                currentQuestIndex++;
                QuestStart();
            }
        }
        private void StartQuest(string id)
        {
            Quest quest = GetQuest(id);
            if (quest == null)
            {
                return;
            }
            //texts[0].text = quest.info.displayName;
            //texts[1].text = quest.info.description;
            quest.InstantiateCurrentQueatStep(this.transform);
            ChangeQuestState(quest.info.id, QuestStepStatus.任务中);
            //questUI.ShowQuestInfo(quest);
            //MessageManager.Instance.CreatMessage("当前任务：",quest.info.displayName,quest.info.description,3f);
            GamePlay.Instance.questUI.GetComponent<QuestUI>().SetQuest(quest);
            GamePlay.Instance.questUI.SetActive(true);
        }
        private void AdvanceQuest(string id)
        {
            Quest quest = GetQuest(id);
            if (quest == null)
            {
                return;
            }
            quest.MoveToNextStep();
            if (quest.CurrentStepExists())
            {
                quest.InstantiateCurrentQueatStep(this.transform);
            }
            else
            {
                ChangeQuestState(quest.info.id, QuestStepStatus.能否完成任务);
            }
        }
        private void FinishQuest(string id)
        {
            Quest quest = GetQuest(id);
            if (quest == null)
            {
                return;
            }
            ClaimRewards(quest);
            ChangeQuestState(quest.info.id, QuestStepStatus.任务完成);
        }
        private void ClaimRewards(Quest quest)
        {
            //Debug.Log("好感度：" +quest.info.favorability.ToString());
            //Debug.Log("特殊奖励：" + quest.info.specialReward.ToString());
        }
        private Dictionary<string, Quest> CreateQuestsMap()
        {
            QuestInfo[] allQuests = Resources.LoadAll<QuestInfo>("Quest");
            Dictionary<string, Quest> idToquestsMap = new Dictionary<string, Quest>();
            foreach (QuestInfo questInfo in allQuests)
            {
                if (idToquestsMap.ContainsKey(questInfo.id))
                {
                    Debug.LogWarning("Duplicate quest id found: " + questInfo.id);
                    continue;
                }
                idToquestsMap.Add(questInfo.id, new Quest(questInfo));
            }
            return idToquestsMap;
        }
        private Quest GetQuest(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !questsMap.TryGetValue(id, out Quest quest))
            {
                Debug.LogError("Quest not found: " + id);
                return null;
            }
            return quest;
        }

        public bool TryGetQuestState(string id, out QuestStepStatus state)
        {
            Quest quest = GetQuest(id);
            if (quest == null)
            {
                state = QuestStepStatus.未达到任务需求;
                return false;
            }

            state = quest.state;
            return true;
        }

        public void RequestStartQuest(QuestInfo questInfo)
        {
            if (questInfo != null)
            {
                questEvent.StartQuest(questInfo.id);
            }
        }
    }
}
