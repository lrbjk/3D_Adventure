using UnityEngine;

namespace Astraparasitus
{
    [CreateAssetMenu(fileName = "Resource Balance", menuName = "Item/Resource Balance Profile")]
    public class ResourceBalanceProfile : ScriptableObject
    {
        [Header("掉落池")]
        [Tooltip("只放弹药和治疗等可消耗资源；钥匙与任务物品保持固定配置。")]
        public LootTable defaultLootTable;

        [Header("不同来源的基础掉落概率")]
        [Range(0f, 1f)] public float enemyChance = 0.18f;
        [Range(0f, 1f)] public float containerChance = 0.55f;
        [Range(0f, 1f)] public float doorChance = 0.35f;
        [Min(0)] public int maxEmptyRolls = 4;

        [Header("弹药压力")]
        [Min(0)] public int lowAmmo = 8;
        [Min(1)] public int comfortableAmmo = 24;
        [Min(1f)] public float shortageWeight = 2.5f;
        [Range(0.01f, 1f)] public float surplusWeight = 0.2f;

        [Header("治疗压力")]
        [Range(0.05f, 1f)] public float lowHealthRatio = 0.4f;
        [Min(0)] public int comfortableHealingItems = 2;

        private void OnValidate()
        {
            comfortableAmmo = Mathf.Max(lowAmmo + 1, comfortableAmmo);
        }
    }
}
