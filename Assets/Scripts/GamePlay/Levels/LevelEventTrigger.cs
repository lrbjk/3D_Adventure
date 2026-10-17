using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Astraparasitus
{
    public enum LevelEventMode
    {
        Manual,
        OnStart,
        PlayerEnter,
        WhenConditionsMet,
    }

    public enum LevelConditionType
    {
        Always,
        GameObjectActive,
        DoorOpen,
        HasItem,
        QuestState,
        NoActiveEnemies,
    }

    [Serializable]
    public class LevelCondition
    {
        public LevelConditionType type;
        public bool invert;
        public GameObject targetObject;
        public Door door;
        public ItemConfig item;
        public QuestInfo quest;
        public QuestStepStatus questState = QuestStepStatus.任务完成;

        public bool Evaluate()
        {
            bool value;
            switch (type)
            {
                case LevelConditionType.GameObjectActive:
                    value = targetObject != null && targetObject.activeSelf;
                    break;
                case LevelConditionType.DoorOpen:
                    value = door != null && door.GetOpenState();
                    break;
                case LevelConditionType.HasItem:
                    value = item != null && BackPack.Instance != null && BackPack.Instance.CheckItem(item.id);
                    break;
                case LevelConditionType.QuestState:
                    value = quest != null && QuestManager.Instance != null &&
                            QuestManager.Instance.TryGetQuestState(quest.id, out QuestStepStatus state) && state == questState;
                    break;
                case LevelConditionType.NoActiveEnemies:
                    value = LevelContext.Current != null && LevelContext.Current.Enemies != null &&
                            LevelContext.Current.Enemies.Count == 0;
                    break;
                default:
                    value = true;
                    break;
            }

            return invert ? !value : value;
        }
    }

    public class LevelEventTrigger : MonoBehaviour
    {
        [Header("身份")]
        [SerializeField, Tooltip("存档和协作使用的稳定事件 ID。")]
        private string eventId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 4)] private string designerNotes;

        [Header("触发方式")]
        [SerializeField] private LevelEventMode mode = LevelEventMode.PlayerEnter;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField, Min(0.05f)] private float checkInterval = 0.2f;

        [Header("触发条件")]
        [SerializeField] private List<LevelCondition> conditions = new List<LevelCondition>();

        [Header("满足条件后执行")]
        [SerializeField] private List<LevelAction> actions = new List<LevelAction>();
        [Tooltip("仅保留给按钮和纯表现连接；玩法逻辑优先使用上方强类型 Action。")]
        [SerializeField] private UnityEvent onTriggered = new UnityEvent();

        private bool hasTriggered;
        private float nextCheckTime;

        public string EventId => eventId;
        public string DisplayName => displayName;
        public string DesignerNotes => designerNotes;
        public IReadOnlyList<LevelAction> Actions => actions;
        public IReadOnlyList<LevelCondition> Conditions => conditions;
        public bool HasTriggered => hasTriggered;

        private void Start()
        {
            if (mode == LevelEventMode.OnStart)
            {
                TryTrigger();
            }
        }

        private void Update()
        {
            if (mode != LevelEventMode.WhenConditionsMet || Time.unscaledTime < nextCheckTime)
            {
                return;
            }

            nextCheckTime = Time.unscaledTime + checkInterval;
            TryTrigger();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (mode == LevelEventMode.PlayerEnter && other.CompareTag("Player"))
            {
                TryTrigger();
            }
        }

        public bool TryTrigger()
        {
            if (hasTriggered && triggerOnce)
            {
                return false;
            }

            foreach (LevelCondition condition in conditions)
            {
                if (condition != null && !condition.Evaluate())
                {
                    return false;
                }
            }

            hasTriggered = true;
            LevelContext context = LevelContext.Current;
            foreach (LevelAction action in actions)
            {
                action?.Execute(context);
            }
            onTriggered.Invoke();
            return true;
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                eventId = GameContentId.Create();
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = gameObject.name;
            }
            checkInterval = Mathf.Max(0.05f, checkInterval);
            Collider trigger = GetComponent<Collider>();
            if (trigger != null && mode == LevelEventMode.PlayerEnter)
            {
                trigger.isTrigger = true;
            }
        }

        public bool ValidateConfiguration(out string message)
        {
            if (mode == LevelEventMode.PlayerEnter && GetComponent<Collider>() == null)
            {
                message = "玩家进入模式需要 Collider";
                return false;
            }
            if (string.IsNullOrWhiteSpace(eventId))
            {
                message = "事件 ID 为空";
                return false;
            }
            if (actions.Count == 0 && onTriggered.GetPersistentEventCount() == 0)
            {
                message = "事件没有配置任何 Action";
                return false;
            }
            foreach (LevelCondition condition in conditions)
            {
                if (condition == null)
                {
                    message = "触发条件包含空引用";
                    return false;
                }
                if (condition.type == LevelConditionType.GameObjectActive && condition.targetObject == null ||
                    condition.type == LevelConditionType.DoorOpen && condition.door == null ||
                    condition.type == LevelConditionType.HasItem && condition.item == null ||
                    condition.type == LevelConditionType.QuestState && condition.quest == null)
                {
                    message = "触发条件缺少目标引用";
                    return false;
                }
            }
            foreach (LevelAction action in actions)
            {
                if (action == null)
                {
                    message = "Action 列表包含空引用";
                    return false;
                }
                if (!action.IsConfigured(out message))
                {
                    return false;
                }
            }
            message = string.Empty;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Collider trigger = GetComponent<Collider>();
            if (trigger != null)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
                Gizmos.DrawWireCube(trigger.bounds.center, trigger.bounds.size);
            }

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            foreach (LevelAction action in actions)
            {
                if (action == null || action.Target == null) continue;
                Transform targetTransform = action.Target is GameObject targetObject
                    ? targetObject.transform
                    : action.Target is Component component ? component.transform : null;
                if (targetTransform != null) Gizmos.DrawLine(transform.position, targetTransform.position);
            }
        }
    }
}
