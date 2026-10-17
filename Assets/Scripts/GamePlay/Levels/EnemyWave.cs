using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    [Serializable]
    public class EnemyWaveEntry
    {
        public EnemySpawn spawnPoint;
        [Min(0f)] public float delay;
    }

    public class EnemyWave : MonoBehaviour
    {
        [SerializeField] private List<EnemyWaveEntry> entries = new List<EnemyWaveEntry>();
        [SerializeField] private bool canRepeat;

        public bool HasSpawned { get; private set; }
        public bool IsRunning { get; private set; }
        public IReadOnlyList<EnemyWaveEntry> Entries => entries;

        public void SpawnWave()
        {
            if (IsRunning || (HasSpawned && !canRepeat))
            {
                return;
            }

            StartCoroutine(SpawnRoutine());
        }

        public void ResetWave()
        {
            StopAllCoroutines();
            IsRunning = false;
            HasSpawned = false;
        }

        private IEnumerator SpawnRoutine()
        {
            IsRunning = true;
            foreach (EnemyWaveEntry entry in entries)
            {
                if (entry == null || entry.spawnPoint == null)
                {
                    continue;
                }

                if (entry.delay > 0f)
                {
                    yield return new WaitForSeconds(entry.delay);
                }

                entry.spawnPoint.SpawnEnemy();
            }

            HasSpawned = true;
            IsRunning = false;
        }
    }
}
