using System.Collections;
using System.Collections.Generic;
using Astraparasitus;
using UnityEngine;

[CreateAssetMenu(fileName = "New Quest Info", menuName = "Quest System/Quest Info")]
public class QuestInfo : ScriptableObject, IGameContent
{
    [SerializeField, HideInInspector] private string stableId;
    public string StableId => stableId;

    [field:SerializeField] public string id { get; private set; }
    [Header("任务名字")]
    public string displayName;
    [Header("任务类型")]
    public QuestType type;
    //[Header("委托人")]
    //public string clientName;
    [Header("任务前提")]
    public QuestInfo[] prerequisites;
    [Header("任务描述")]
    public string description;
    [Header("任务目标")]
    public GameObject[] questStepPrefab;
    [Header("任务奖励")]
    public int reward;
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(stableId))
        {
            stableId = GameContentId.Create();
        }

      #if UNITY_EDITOR
        id = this.name;
        UnityEditor.EditorUtility.SetDirty(this);
      #endif
    }
}
public enum QuestType
{
    主线任务,
    支线任务,
}
