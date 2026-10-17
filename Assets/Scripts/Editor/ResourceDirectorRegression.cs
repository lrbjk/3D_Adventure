#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astraparasitus.Editor
{
    [InitializeOnLoad]
    public static class ResourceDirectorRegression
    {
        private const string PendingKey = "3DAdventure.ResourceDirectorRegression.Pending";
        private static int waitFrames;

        static ResourceDirectorRegression() => EditorApplication.update += Tick;

        [MenuItem("Tools/3D Adventure/Run Dynamic Resource Regression")]
        public static void Run()
        {
            if (SceneManager.GetActiveScene().path != "Assets/Scenes/Level01.unity")
            {
                Debug.LogError("Open Level01 before running dynamic resource regression.");
                return;
            }
            waitFrames = 0;
            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying) return;
            if (GamePlay.Instance?.player == null || BackPack.Instance == null)
            {
                if (++waitFrames < 300) return;
                Finish("GamePlay, Player or BackPack did not initialize.");
                return;
            }
            if (++waitFrames < 5) return;

            try
            {
                Check();
                Finish(null);
            }
            catch (Exception exception)
            {
                Finish(exception.ToString());
            }
        }

        private static void Check()
        {
            GamePlay gamePlay = GamePlay.Instance;
            ResourceDirector director = gamePlay.Resources;
            Require(director != null && director.Profile != null, "ResourceDirector profile is missing.");
            Require(director.Profile.defaultLootTable != null && director.Profile.defaultLootTable.Entries.Count == 3,
                "Level01 resource table is not configured.");

            ResourceBalanceProfile originalProfile = director.Profile;
            ResourceBalanceProfile testProfile = UnityEngine.Object.Instantiate(originalProfile);
            FieldInfo profileField = typeof(ResourceDirector).GetField("profile", BindingFlags.NonPublic | BindingFlags.Instance);
            Require(profileField != null, "ResourceDirector profile field is missing.");
            profileField.SetValue(director, testProfile);
            try
            {
                UnityEngine.Random.InitState(1819);
                testProfile.containerChance = 0f;
                testProfile.maxEmptyRolls = 2;
                RemoveHealing(BackPack.Instance);
                gamePlay.player.SetCurrentHp(gamePlay.player.MaxHealth * 0.2f);
                director.RestoreState(null);
                Require(!director.TryRoll(ResourceDropSource.Container, null, out _), "First roll should be empty.");
                Require(!director.TryRoll(ResourceDropSource.Container, null, out _), "Second roll should be empty.");
                Require(director.EmptyRolls == 2, "Empty roll counter was not recorded.");
                ResourceDirectorState saved = director.CaptureState();
                director.RestoreState(null);
                director.RestoreState(saved);
                Require(director.EmptyRolls == 2, "Empty roll counter was not restored.");
                Require(director.TryRoll(ResourceDropSource.Container, null, out LootResult guaranteed),
                    "Soft pity did not produce a resource.");
                Require(guaranteed.item != null && guaranteed.prefab != null && guaranteed.amount > 0,
                    "Guaranteed resource is incomplete.");
                Require(director.EmptyRolls == 0, "Successful roll did not reset the counter.");

                testProfile.containerChance = 1f;
                GameObject spawned = gamePlay.SpawnResourceDrop(ResourceDropSource.Container, null, gamePlay.player.transform.position);
                Require(spawned != null, "Resource pickup did not spawn.");
                ItemBase pickup = spawned.GetComponentInChildren<ItemBase>(true);
                Require(pickup != null && pickup.itemConfig != null && pickup.pickupAmount > 0,
                    "Spawned pickup lost its item or amount.");
                Require(pickup.itemConfig.id != 0 || pickup.pickupAmount >= 3 && pickup.pickupAmount <= 6,
                    "Ammo amount is outside the tuned range.");
                ObjectPool.Instance.PushObject(spawned);
                GameObject reused = ObjectPool.Instance.GetObject(pickup.itemConfig.worldPrefab);
                ItemBase reusedPickup = reused.GetComponentInChildren<ItemBase>(true);
                ItemBase prefabPickup = pickup.itemConfig.worldPrefab.GetComponentInChildren<ItemBase>(true);
                Require(reusedPickup.pickupAmount == prefabPickup.pickupAmount,
                    "Pooled pickup kept the previous dynamic amount.");
                ObjectPool.Instance.PushObject(reused);
                CheckWeights(director, gamePlay.player, BackPack.Instance);
                testProfile.containerChance = 0f;
                director.RestoreState(new ResourceDirectorState { emptyRolls = 2 });
                Require(!director.TryRoll(ResourceDropSource.Container, null, out _),
                    "Surplus resources incorrectly activated soft pity.");
                EnemyBase enemy = UnityEngine.Object.FindFirstObjectByType<EnemyBase>(FindObjectsInactive.Include);
                Require(enemy != null, "Level01 has no enemy for loot regression.");
                enemy.isRandom = true;
                testProfile.enemyChance = 0f;
                testProfile.maxEmptyRolls = 10;
                director.RestoreState(null);
                enemy.SpawnLoot();
                enemy.SpawnLoot();
                Require(director.EmptyRolls == 1, "One enemy rolled loot more than once.");
            }
            finally
            {
                profileField.SetValue(director, originalProfile);
                UnityEngine.Object.Destroy(testProfile);
            }
        }

        private static void CheckWeights(ResourceDirector director, PlayerControl player, BackPack bag)
        {
            LootEntry ammo = director.Profile.defaultLootTable.Entries[0];
            LootEntry green = director.Profile.defaultLootTable.Entries[1];
            WpConfig pistol = AssetDatabase.LoadAssetAtPath<WpConfig>("Assets/Scripts/SO/weapons/Wp1.asset");
            Require(pistol != null && pistol.AmmoItemId == ammo.item.id, "Pistol ammo mapping is invalid.");
            if (!player.wpConfigs.Contains(pistol)) player.wpConfigs.Add(pistol);
            MethodInfo evaluate = typeof(ResourceDirector).GetMethod("EvaluateWeight", BindingFlags.NonPublic | BindingFlags.Instance);
            Require(evaluate != null, "Resource weighting method is missing.");

            bag.RemoveAllItem(ammo.item.id);
            player.SetWeaponAmmo(pistol, 0);
            float scarceAmmoWeight = (float)evaluate.Invoke(director, new object[] { ammo, player, bag });
            bag.AddItem(ammo.item, 30);
            float surplusAmmoWeight = (float)evaluate.Invoke(director, new object[] { ammo, player, bag });
            Require(scarceAmmoWeight > surplusAmmoWeight, "Ammo shortage does not raise its drop weight.");

            RemoveHealing(bag);
            player.SetCurrentHp(player.MaxHealth * 0.2f);
            float scarceHealingWeight = (float)evaluate.Invoke(director, new object[] { green, player, bag });
            player.SetCurrentHp(player.MaxHealth);
            bag.AddItem(green.item, 2);
            float surplusHealingWeight = (float)evaluate.Invoke(director, new object[] { green, player, bag });
            Require(scarceHealingWeight > surplusHealingWeight, "Healing shortage does not raise its drop weight.");
        }

        private static void RemoveHealing(BackPack bag)
        {
            List<int> healingIds = new List<int>();
            foreach (KeyValuePair<int, Item> owned in bag.items)
                if (bag.TryGetItemConfig(owned.Key, out ItemConfig item) && item.useEffect == ItemUseEffect.RestoreHealth)
                    healingIds.Add(owned.Key);
            foreach (int id in healingIds) bag.RemoveAllItem(id);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish(string error)
        {
            SessionState.SetBool(PendingKey, false);
            if (error == null) Debug.Log("Dynamic resource regression passed: shortage weights, empty rolls, soft pity, save state, pickup amount, pool reset and once-only enemy loot.");
            else Debug.LogError("Dynamic resource regression failed: " + error);
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
