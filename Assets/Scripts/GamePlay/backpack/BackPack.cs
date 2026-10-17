using System;
using System.Collections.Generic;
using Astraparasitus;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct ItemList
{
    public int id;
    public Item Item;
}

[Serializable]
public struct ItemObject
{
    public int id;
    public GameObject itemPrefab;
}

public class BackPack : SingletonMono<BackPack>
{
    public event Action ItemsChanged;

    [Tooltip("只作为初始配置来源；运行时背包保存在 items 中。")]
    [FormerlySerializedAs("playerConfig")]
    public PlayerDefinition playerDefinition;
    public Dictionary<int, Item> items = new Dictionary<int, Item>();
    public List<ItemObject> itemPrefabs = new List<ItemObject>();

    private void Start()
    {
        PlayerDefinition definition = playerDefinition != null
            ? playerDefinition
            : PlayerControl.Current != null ? PlayerControl.Current.Definition : GameContentCatalog.Default?.Player;
        if (items.Count > 0 || definition == null)
        {
            return;
        }

        foreach (StartingItemEntry entry in definition.startingItems)
        {
            if (entry.item != null)
            {
                Item item = entry.item.CreateRuntimeItem(entry.amount);
                items[item.id] = item;
            }
        }
    }

    public void AddToShow()
    {
        ItemsChanged?.Invoke();
    }

    public void AddItem(Item item)
    {
        if (!items.TryGetValue(item.id, out Item current))
        {
            items.Add(item.id, item);
        }
        else
        {
            current.itemNum += item.itemNum;
            items[item.id] = current;
        }

        ItemsChanged?.Invoke();
    }

    public void AddItem(ItemConfig config, int amount = -1)
    {
        if (config == null)
        {
            return;
        }

        AddItem(config.CreateRuntimeItem(amount));
    }

    public bool TryGetItemConfig(int itemId, out ItemConfig config)
    {
        config = null;
        GameContentCatalog catalog = GameContentCatalog.Default;
        return catalog != null && catalog.TryGetItem(itemId, out config);
    }

    public void RemoveItem(int itemId, int num)
    {
        if (!items.TryGetValue(itemId, out Item current))
        {
            return;
        }

        int removeAmount = Mathf.Max(0, num);
        if (current.itemNum > removeAmount)
        {
            current.itemNum -= removeAmount;
            items[itemId] = current;
        }
        else
        {
            items.Remove(itemId);
        }

        ItemsChanged?.Invoke();
    }

    public void RemoveAllItem(int itemId)
    {
        if (items.Remove(itemId))
        {
            ItemsChanged?.Invoke();
        }
    }

    public bool CheckItem(int itemId)
    {
        return items.ContainsKey(itemId);
    }

    public int GetItemAmount(int itemId)
    {
        return items.TryGetValue(itemId, out Item item) ? item.itemNum : 0;
    }

    public Astraparasitus.InventoryState CaptureState()
    {
        var state = new Astraparasitus.InventoryState();
        foreach (KeyValuePair<int, Item> entry in items)
        {
            Item legacyItem = entry.Value;
            legacyItem.combineObject = null;
            TryGetItemConfig(entry.Key, out ItemConfig config);
            state.items.Add(new Astraparasitus.InventoryItemData
            {
                id = entry.Key,
                itemId = config != null ? config.StableId : string.Empty,
                amount = entry.Value.itemNum,
                item = legacyItem
            });
        }
        return state;
    }

    public void RestoreState(Astraparasitus.InventoryState state)
    {
        items.Clear();
        if (state?.items != null)
        {
            foreach (Astraparasitus.InventoryItemData entry in state.items)
            {
                int amount = entry.amount > 0 ? entry.amount : entry.item.itemNum;
                if (amount <= 0)
                {
                    continue;
                }

                Item restoredItem;
                GameContentCatalog catalog = GameContentCatalog.Default;
                ItemConfig config = null;
                if (catalog != null && !string.IsNullOrWhiteSpace(entry.itemId))
                    catalog.TryGetItem(entry.itemId, out config);
                if (catalog != null && config == null)
                    catalog.TryResolveLegacyItem(entry.id, entry.item.itemName, out config);

                if (config != null)
                {
                    restoredItem = config.CreateRuntimeItem(amount);
                }
                else
                {
                    restoredItem = entry.item;
                    restoredItem.id = entry.id;
                    restoredItem.itemNum = amount;
                    restoredItem.combineObject = null;
                }

                if (items.TryGetValue(restoredItem.id, out Item existing))
                    restoredItem.itemNum += existing.itemNum;
                items[restoredItem.id] = restoredItem;
            }
        }
        ItemsChanged?.Invoke();
    }

    public void RestoreLegacyItems(Dictionary<int, Item> savedItems)
    {
        var state = new Astraparasitus.InventoryState();
        if (savedItems != null)
        {
            foreach (KeyValuePair<int, Item> entry in savedItems)
                state.items.Add(new Astraparasitus.InventoryItemData
                {
                    id = entry.Key,
                    amount = entry.Value.itemNum,
                    item = entry.Value
                });
        }
        RestoreState(state);
    }

    public bool GiveItemPrefab(int itemId, Vector3 point, int amount = 0)
    {
        if (TryGetItemConfig(itemId, out ItemConfig config) && config.worldPrefab != null)
        {
            SpawnPickup(config.worldPrefab, point, amount);
            return true;
        }

        int index = itemPrefabs.FindIndex(x => x.id == itemId);
        if (index < 0 || itemPrefabs[index].itemPrefab == null)
        {
            Debug.LogWarning($"Item prefab is not registered. Item ID: {itemId}");
            return false;
        }

        GameObject prefab = itemPrefabs[index].itemPrefab;
        SpawnPickup(prefab, point, amount);
        return true;
    }

    private static void SpawnPickup(GameObject prefab, Vector3 point, int amount)
    {
        GameObject spawned = Instantiate(prefab, point, prefab.transform.rotation);
        ItemBase pickup = spawned.GetComponentInChildren<ItemBase>();
        if (pickup != null && amount > 0)
            pickup.pickupAmount = amount;
    }
}
