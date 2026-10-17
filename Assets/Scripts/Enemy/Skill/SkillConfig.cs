using System.Collections;
using System.Collections.Generic;
using Astraparasitus;
using UnityEngine;
[CreateAssetMenu(fileName = "New Skill Config", menuName = "Skill Config")]

public class SkillConfig : ScriptableObject, IGameContent
{
    [SerializeField, HideInInspector] private string stableId;
    public string StableId => stableId;

    public string skillName;
    public string clipName;
    public float endurance;
    public float damageRate;
    public EventName eventName;
    public float startframe;
    public float endframe;
    public int hitboxIndex;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(stableId))
        {
            stableId = GameContentId.Create();
        }

        endurance = Mathf.Max(0f, endurance);
        damageRate = Mathf.Max(0f, damageRate);
        startframe = Mathf.Max(0f, startframe);
        endframe = Mathf.Max(startframe, endframe);
        hitboxIndex = Mathf.Max(0, hitboxIndex);
    }
}
public enum EventName
{
    Attack,
    TC,
    FarAttack
}
