using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    [DisallowMultipleComponent]
    public class LevelContext : MonoBehaviour
    {
        public static LevelContext Current { get; private set; }

        [Header("场景核心引用")]
        [SerializeField] private PlayerControl player;
        [SerializeField] private GamePlay gamePlay;
        [SerializeField] private EnemyRegistry enemyRegistry;

        [Header("任务目标门")]
        [SerializeField] private Door questDoor1;
        [SerializeField] private Door questDoor2;

        [Header("出生点")]
        [SerializeField] private Transform playerSpawn;

        public PlayerControl Player => player;
        public GamePlay GamePlay => gamePlay;
        public EnemyRegistry Enemies => enemyRegistry;
        public Door QuestDoor1 => questDoor1;
        public Door QuestDoor2 => questDoor2;
        public Transform PlayerSpawn => playerSpawn;

        internal void Install()
        {
            if (Current != null && Current != this)
            {
                Debug.LogError("Only one LevelContext may be active in a playable scene.", this);
                return;
            }

            Current = this;
            enemyRegistry?.RegisterExistingSceneEnemies();
        }

        internal void Uninstall()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        public bool ValidateReferences(out string message)
        {
            List<string> missing = new List<string>();
            if (player == null) missing.Add(nameof(player));
            if (gamePlay == null) missing.Add(nameof(gamePlay));
            if (enemyRegistry == null) missing.Add(nameof(enemyRegistry));

            message = missing.Count == 0
                ? string.Empty
                : "LevelContext missing: " + string.Join(", ", missing);
            return missing.Count == 0;
        }
    }

}
