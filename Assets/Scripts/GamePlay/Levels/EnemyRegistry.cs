using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    [DisallowMultipleComponent]
    public class EnemyRegistry : MonoBehaviour
    {
        [SerializeField, HideInInspector] private List<EnemyBase> enemies = new List<EnemyBase>();
        public event Action<int> CountChanged;
        public IReadOnlyList<EnemyBase> Enemies => enemies;
        public int Count { get { RemoveMissingReferences(); return enemies.Count; } }

        public void Register(EnemyBase enemy)
        {
            if (enemy == null || enemies.Contains(enemy)) return;
            enemies.Add(enemy);
            CountChanged?.Invoke(enemies.Count);
        }

        public void Unregister(EnemyBase enemy)
        {
            if (enemy != null && enemies.Remove(enemy)) CountChanged?.Invoke(enemies.Count);
        }

        public void RegisterExistingSceneEnemies()
        {
            foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                Register(enemy);
        }

        private void RemoveMissingReferences()
        {
            int previousCount = enemies.Count;
            enemies.RemoveAll(enemy => enemy == null);
            if (previousCount != enemies.Count) CountChanged?.Invoke(enemies.Count);
        }
    }
}
