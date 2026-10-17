using UnityEngine;
namespace Astraparasitus
{
    public class SpawnWaveAction : LevelAction
    {
        [SerializeField] private EnemyWave wave;
        public override Object Target => wave;
        public override void Execute(LevelContext context) => wave?.SpawnWave();
        public override bool IsConfigured(out string message) { message = wave == null ? "EnemyWave 未配置" : string.Empty; return wave != null; }
    }
}
