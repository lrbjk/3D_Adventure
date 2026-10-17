using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    [Serializable]
    public class LootEntry
    {
        public ItemConfig item;
        public GameObject prefab;
        [Min(1)] public int weight = 1;
        [Min(1)] public int minAmount = 1;
        [Min(1)] public int maxAmount = 1;
    }

    public struct LootResult
    {
        public ItemConfig item;
        public GameObject prefab;
        public int amount;
    }

    [CreateAssetMenu(fileName = "New Loot Table", menuName = "Item/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [SerializeField] private List<LootEntry> entries = new List<LootEntry>();
        public IReadOnlyList<LootEntry> Entries => entries;

        private void OnValidate()
        {
            foreach (LootEntry entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                entry.weight = Mathf.Max(1, entry.weight);
                entry.minAmount = Mathf.Max(1, entry.minAmount);
                entry.maxAmount = Mathf.Max(entry.minAmount, entry.maxAmount);
            }
        }
    }
}
