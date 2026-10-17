using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public enum ResourceDropSource { Enemy, Container, Door }

    [Serializable]
    public class ResourceDirectorState
    {
        public int emptyRolls;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(GamePlay))]
    public class ResourceDirector : MonoBehaviour
    {
        [SerializeField] private ResourceBalanceProfile profile;
        [SerializeField, Tooltip("运行时状态，由存档恢复。")] private int emptyRolls;

        private readonly List<LootEntry> candidates = new List<LootEntry>();
        private readonly List<float> weights = new List<float>();

        public ResourceBalanceProfile Profile => profile;
        public int EmptyRolls => emptyRolls;

        public ResourceDirectorState CaptureState() => new ResourceDirectorState { emptyRolls = emptyRolls };

        public void RestoreState(ResourceDirectorState state)
        {
            emptyRolls = Mathf.Max(0, state?.emptyRolls ?? 0);
        }

        public bool TryRoll(ResourceDropSource source, LootTable sourceTable, out LootResult result)
        {
            result = default;
            GamePlay gamePlay = GetComponent<GamePlay>();
            PlayerControl player = gamePlay != null ? gamePlay.player : null;
            BackPack bag = BackPack.Instance;
            LootTable table = sourceTable != null ? sourceTable : profile != null ? profile.defaultLootTable : null;
            if (profile == null || table == null || player == null || bag == null)
                return false;

            candidates.Clear();
            weights.Clear();
            float totalWeight = 0f;
            bool hasShortage = false;
            bool allSurplus = true;
            foreach (LootEntry entry in table.Entries)
            {
                float weight = EvaluateWeight(entry, player, bag);
                if (weight <= 0f) continue;
                candidates.Add(entry);
                weights.Add(weight);
                totalWeight += weight;
                if (weight > entry.weight + 0.001f) hasShortage = true;
                if (weight > entry.weight * profile.surplusWeight + 0.001f) allSurplus = false;
            }
            if (totalWeight <= 0f) return false;

            float chance = source == ResourceDropSource.Enemy ? profile.enemyChance :
                source == ResourceDropSource.Door ? profile.doorChance : profile.containerChance;
            if (allSurplus) chance *= profile.surplusWeight;
            // 保底只救急；资源充裕时连续空掉落不会自动变成补给。
            if ((!hasShortage || emptyRolls < profile.maxEmptyRolls) && UnityEngine.Random.value > chance)
            {
                emptyRolls = Mathf.Min(emptyRolls + 1, profile.maxEmptyRolls);
                return false;
            }

            float roll = UnityEngine.Random.value * totalWeight;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= weights[i];
                if (roll > 0f && i < candidates.Count - 1) continue;
                LootEntry selected = candidates[i];
                int min = Mathf.Max(1, selected.minAmount);
                int max = Mathf.Max(min, selected.maxAmount);
                result = new LootResult
                {
                    item = selected.item,
                    prefab = selected.prefab,
                    amount = UnityEngine.Random.Range(min, max + 1)
                };
                emptyRolls = 0;
                return true;
            }
            return false;
        }

        private float EvaluateWeight(LootEntry entry, PlayerControl player, BackPack bag)
        {
            if (entry == null || entry.item == null || entry.prefab == null || entry.weight <= 0)
                return 0f;
            ItemBase pickup = entry.prefab.GetComponentInChildren<ItemBase>(true);
            if (pickup == null || pickup.itemConfig != entry.item)
                return 0f;

            if (entry.item.useEffect == ItemUseEffect.RestoreHealth)
            {
                int healingItems = 0;
                foreach (KeyValuePair<int, Item> owned in bag.items)
                    if (bag.TryGetItemConfig(owned.Key, out ItemConfig config) && config.useEffect == ItemUseEffect.RestoreHealth)
                        healingItems += owned.Value.itemNum;

                bool scarce = player.GetHp() / Mathf.Max(1f, player.MaxHealth) <= profile.lowHealthRatio && healingItems == 0;
                return entry.weight * (scarce ? profile.shortageWeight :
                    healingItems >= profile.comfortableHealingItems ? profile.surplusWeight : 1f);
            }

            int ammo = bag.GetItemAmount(entry.item.id);
            bool ownedWeapon = false;
            foreach (WpConfig weapon in player.wpConfigs)
            {
                if (weapon == null || !weapon.UsesAmmo || weapon.AmmoItemId != entry.item.id) continue;
                ownedWeapon = true;
                ammo += player.GetWeaponAmmo(weapon, weapon.bulletTotalNum);
            }
            if (!ownedWeapon) return 0f;
            return entry.weight * (ammo <= profile.lowAmmo ? profile.shortageWeight :
                ammo >= profile.comfortableAmmo ? profile.surplusWeight : 1f);
        }
    }
}
