using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    [CreateAssetMenu(fileName = "GameContentCatalog", menuName = "Game/Content Catalog")]
    public class GameContentCatalog : ScriptableObject
    {
        private static GameContentCatalog defaultCatalog;

        [Header("内容定义")]
        [SerializeField] private PlayerDefinition player;
        [SerializeField] private List<ItemConfig> items = new List<ItemConfig>();
        [SerializeField] private List<WpConfig> weapons = new List<WpConfig>();
        [SerializeField] private List<EnemyArchetype> enemies = new List<EnemyArchetype>();
        [SerializeField] private List<QuestInfo> quests = new List<QuestInfo>();

        private readonly Dictionary<string, ItemConfig> itemMap = new Dictionary<string, ItemConfig>();
        private readonly Dictionary<string, WpConfig> weaponMap = new Dictionary<string, WpConfig>();
        private readonly Dictionary<string, EnemyArchetype> enemyMap = new Dictionary<string, EnemyArchetype>();
        private readonly Dictionary<string, QuestInfo> questMap = new Dictionary<string, QuestInfo>();
        private readonly Dictionary<int, ItemConfig> legacyItemMap = new Dictionary<int, ItemConfig>();
        private readonly HashSet<int> ambiguousLegacyItemIds = new HashSet<int>();

        public static GameContentCatalog Default
        {
            get
            {
                if (defaultCatalog == null)
                {
                    defaultCatalog = Resources.Load<GameContentCatalog>("GameContentCatalog");
                }

                return defaultCatalog;
            }
        }

        public IReadOnlyList<ItemConfig> Items => items;
        public PlayerDefinition Player => player;
        public IReadOnlyList<WpConfig> Weapons => weapons;
        public IReadOnlyList<EnemyArchetype> Enemies => enemies;
        public IReadOnlyList<QuestInfo> Quests => quests;

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnValidate()
        {
            Rebuild();
        }

        public bool TryGetItem(string id, out ItemConfig item)
        {
            EnsureBuilt();
            return itemMap.TryGetValue(id, out item);
        }

        public bool TryGetItem(int legacyId, out ItemConfig item)
        {
            EnsureBuilt();
            if (ambiguousLegacyItemIds.Contains(legacyId))
            {
                item = null;
                return false;
            }

            return legacyItemMap.TryGetValue(legacyId, out item);
        }

        public bool TryResolveLegacyItem(int legacyId, string legacyName, out ItemConfig item)
        {
            EnsureBuilt();
            if (!string.IsNullOrWhiteSpace(legacyName))
            {
                foreach (ItemConfig candidate in items)
                {
                    if (candidate != null && string.Equals(candidate.itemName, legacyName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        item = candidate;
                        return true;
                    }
                }
            }
            return TryGetItem(legacyId, out item);
        }

        public bool TryGetWeapon(string id, out WpConfig weapon)
        {
            EnsureBuilt();
            return weaponMap.TryGetValue(id, out weapon);
        }

        public bool TryGetEnemy(string id, out EnemyArchetype enemy)
        {
            EnsureBuilt();
            return enemyMap.TryGetValue(id, out enemy);
        }

        public bool TryGetQuest(string id, out QuestInfo quest)
        {
            EnsureBuilt();
            return questMap.TryGetValue(id, out quest);
        }

        private void EnsureBuilt()
        {
            if (itemMap.Count == 0 && weaponMap.Count == 0 && enemyMap.Count == 0 && questMap.Count == 0)
            {
                Rebuild();
            }
        }

        private void Rebuild()
        {
            BuildMap(items, itemMap, "item");
            foreach (ItemConfig content in items)
            {
                if (content == null) continue;
                foreach (string oldId in content.LegacyStableIds)
                {
                    if (!string.IsNullOrWhiteSpace(oldId) && !itemMap.ContainsKey(oldId))
                        itemMap.Add(oldId, content);
                }
            }
            BuildLegacyItemMap();
            BuildMap(weapons, weaponMap, "weapon");
            BuildMap(enemies, enemyMap, "enemy");
            BuildMap(quests, questMap, "quest");
        }

        private void BuildLegacyItemMap()
        {
            legacyItemMap.Clear();
            ambiguousLegacyItemIds.Clear();
            foreach (ItemConfig content in items)
            {
                if (content == null || ambiguousLegacyItemIds.Contains(content.id))
                {
                    continue;
                }

                if (legacyItemMap.ContainsKey(content.id))
                {
                    legacyItemMap.Remove(content.id);
                    ambiguousLegacyItemIds.Add(content.id);
                    continue;
                }

                legacyItemMap.Add(content.id, content);
            }
        }

        private void BuildMap<T>(List<T> source, Dictionary<string, T> target, string contentType)
            where T : Object, IGameContent
        {
            target.Clear();
            foreach (T content in source)
            {
                if (content == null || string.IsNullOrWhiteSpace(content.StableId))
                {
                    continue;
                }

                if (target.ContainsKey(content.StableId))
                {
                    Debug.LogWarning($"Duplicate {contentType} ID: {content.StableId}", content);
                    continue;
                }

                target.Add(content.StableId, content);
            }
        }
    }
}
