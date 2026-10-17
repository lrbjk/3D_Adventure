using UnityEngine;

namespace Astraparasitus
{
    [CreateAssetMenu(fileName = "New Enemy Archetype", menuName = "Enemy/Enemy Archetype")]
    public class EnemyArchetype : ScriptableObject, IGameContent
    {
        [SerializeField, HideInInspector] private string stableId;
        public string StableId => stableId;

        [Header("身份")]
        [Tooltip("旧数据兼容编号。新逻辑请使用 StableId。")]
        public int ID;
        [Tooltip("策划和 UI 使用的敌人名称。")]
        public string Name;
        [Tooltip("生成该敌人时使用的 Prefab。")]
        public GameObject enemyPrefab;

        [Header("基础属性")]
        [Min(1f)] public float health = 100f;
        [Min(0f)] public float endurance = 40f;
        [Min(0f)] public float speedRate = 1f;
        [Min(1)] public int size = 1;
        [Min(0f)] public float damage = 10f;

        [Header("硬直与恢复")]
        [Min(0f)] public float hitStunValue = 10f;
        [Min(0f)] public float stunTime = 1f;
        [Min(0f)] public float recoverTime = 4f;

        [Header("部位耐久")]
        [Min(0f)] public float leftLegValue = 20f;
        [Min(0f)] public float rightLegValue = 20f;
        [Min(0f)] public float leftArmValue = 20f;
        [Min(0f)] public float rightArmValue = 20f;

        [Header("移动与寻路")]
        public float gravity = -9.8f;
        [Tooltip("Project Settings/Navigation 中配置的 Area 名称。")]
        public string navMeshAreaName = "EnemyBase";

        [Header("感知与攻击距离")]
        [Min(0f)] public float viewRadius = 15f;
        [Min(1)] public int viewAngleStep = 30;
        [Range(0f, 360f)] public float viewAngle = 180f;
        public Vector3 viewOffset;
        [Min(0f)] public float attackDistance = 2f;
        [Min(0f)] public float farAttackDistance;
        [Min(0f)] public float executionDistance = 1.4f;
        [Min(0f)] public float forgetDelay = 10f;
        public bool useDistanceCheck;
        public LayerMask ignoreLayer;

        [Header("技能")]
        public SkillConfig[] skills = System.Array.Empty<SkillConfig>();

        [Header("动画状态名")]
        public string idle = "Idle";
        public string chase = "Chase";
        public string toNoLeg = "ToNoLeg";
        public string noLegIdle = "NoLegIdle";
        public string noLegChase = "NoLegChase";
        public string Break = "Break";
        public string other;

        [Header("音效事件名")]
        public string idleSound = "EnemyIdle";
        public string chaseSound = "EnemyMove";
        public string breakSound = "EnemyBreak";
        public string attackSound = "EnemyAttack";
        public string hitSound = "EnemyHit";
        public string dieSound = "EnemyDeath";

        [Header("掉落")]
        [Tooltip("配置后优先使用数据化掉落表。")]
        public LootTable lootTable;
        [Tooltip("没有掉落表时使用的固定旧版掉落 Prefab。")]
        public GameObject fallbackLootPrefab;
        [Tooltip("没有掉落表时，是否继续使用 GamePlay 的动态旧版掉落规则。")]
        public bool useLegacyRandomLoot;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                stableId = GameContentId.Create();
            }

            health = Mathf.Max(1f, health);
            endurance = Mathf.Max(0f, endurance);
            speedRate = Mathf.Max(0f, speedRate);
            size = Mathf.Max(1, size);
            damage = Mathf.Max(0f, damage);
            hitStunValue = Mathf.Max(0f, hitStunValue);
            stunTime = Mathf.Max(0f, stunTime);
            recoverTime = Mathf.Max(0f, recoverTime);
            leftLegValue = Mathf.Max(0f, leftLegValue);
            rightLegValue = Mathf.Max(0f, rightLegValue);
            leftArmValue = Mathf.Max(0f, leftArmValue);
            rightArmValue = Mathf.Max(0f, rightArmValue);
            viewRadius = Mathf.Max(0f, viewRadius);
            viewAngleStep = Mathf.Max(1, viewAngleStep);
            attackDistance = Mathf.Max(0f, attackDistance);
            farAttackDistance = Mathf.Max(0f, farAttackDistance);
            executionDistance = Mathf.Max(0f, executionDistance);
            forgetDelay = Mathf.Max(0f, forgetDelay);
            skills ??= System.Array.Empty<SkillConfig>();
        }
    }
}
