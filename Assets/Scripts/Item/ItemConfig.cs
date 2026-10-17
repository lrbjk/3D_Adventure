using Astraparasitus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemUseEffect
{
    None,
    RestoreHealth,
}

[CreateAssetMenu(fileName = "New Item Config", menuName = "Item/Item Config")]
public class ItemConfig : ScriptableObject, IGameContent
{
    private const int CurrentConfigVersion = 2;

    [SerializeField, HideInInspector] private string stableId;
    [SerializeField, HideInInspector] private List<string> legacyStableIds = new List<string>();
    [SerializeField, HideInInspector] private int configVersion;
    public string StableId => stableId;
    public IReadOnlyList<string> LegacyStableIds => legacyStableIds ?? (IReadOnlyList<string>)Array.Empty<string>();

    [Header("基础信息")]
    [Min(0)] public int id;
    public string itemName;
    [TextArea(2, 4)] public string description;
    public ItemType itemType = ItemType.Pickup;
    [Min(1)] public int defaultAmount = 1;

    [Header("表现资源")]
    public Sprite icon;
    [Tooltip("场景中掉落和拾取时使用的 Prefab。")]
    public GameObject worldPrefab;
    [Tooltip("拾取物品后，在查看界面中展示的 Prefab。")]
    public GameObject inspectPrefab;

    [Header("使用效果")]
    public ItemUseEffect useEffect;
    [Min(0f)] public float effectAmount;

    [Header("组合规则")]
    public bool isCombinable;
    [Tooltip("背包中还需要拥有的另一件物品。")]
    public ItemConfig combineWith;
    [Tooltip("消耗两件物品后生成的结果。")]
    public ItemConfig combineResult;

    // 旧存档与现有背包仍使用 Item DTO。Inspector 隐藏它，由上面的策划字段自动同步。
    [SerializeField, HideInInspector] public Item item;

    public bool IsUsable => useEffect != ItemUseEffect.None;

    private void OnEnable()
    {
        MigrateLegacyData();
        SyncLegacyData();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(stableId))
        {
            stableId = GameContentId.Create();
        }

        MigrateLegacyData();
        defaultAmount = Mathf.Max(1, defaultAmount);
        effectAmount = Mathf.Max(0f, effectAmount);
        SyncLegacyData();
    }

    public Item CreateRuntimeItem(int amount = -1)
    {
        SyncLegacyData();
        Item runtimeItem = item;
        runtimeItem.itemNum = amount > 0 ? amount : defaultAmount;
        return runtimeItem;
    }

    public bool TryApplyUseEffect(PlayerControl player)
    {
        if (player == null)
        {
            return false;
        }

        switch (useEffect)
        {
            case ItemUseEffect.RestoreHealth:
                if (player.GetHp() >= player.MaxHealth || effectAmount <= 0f)
                {
                    return false;
                }

                player.DecreaseHp(-effectAmount);
                return true;
            default:
                return false;
        }
    }

    private void MigrateLegacyData()
    {
        if (configVersion >= CurrentConfigVersion)
        {
            return;
        }

        defaultAmount = Mathf.Max(1, item.itemNum);
        isCombinable = item.isCombinable;
        combineResult = item.combineObject;

        if (inspectPrefab == null && !string.IsNullOrWhiteSpace(item.itemObject))
        {
            inspectPrefab = Resources.Load<GameObject>(item.itemObject);
        }

        if (icon == null && !string.IsNullOrWhiteSpace(item.itemIcon))
        {
            icon = Resources.Load<Sprite>(item.itemIcon);
        }

        if (item.isUseable)
        {
            useEffect = ItemUseEffect.RestoreHealth;
            effectAmount = id == 21 ? 50f : 100f;
        }

        configVersion = CurrentConfigVersion;
    }

    private void SyncLegacyData()
    {
        item.id = id;
        item.itemName = itemName;
        item.itemNum = Mathf.Max(1, defaultAmount);
        item.isUseable = IsUsable;
        item.isCombinable = isCombinable;
        item.combineId = combineWith != null ? combineWith.id : item.combineId;
        item.combineObject = combineResult;
    }
}

[Serializable]
public struct Item
{
    public int id;
    public string itemName;
    [Min(1)] public int itemNum;
    public string itemObject;
    public string itemIcon;
    public bool isUseable;
    public bool isCombinable;
    public int combineId;
    // 运行时组合配置属于 Unity 资源引用，不能进入 Newtonsoft 存档图。
    [JsonIgnore]
    public ItemConfig combineObject;
}
