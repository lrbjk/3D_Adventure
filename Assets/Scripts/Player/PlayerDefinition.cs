using System.Collections.Generic;
using Astraparasitus;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct StartingItemEntry
{
    public ItemConfig item;
    [Min(1)] public int amount;
}

public enum PlayerFacingMode
{
    CameraForward,
    InputDirection,
}

[System.Serializable]
public class PlayerTransitionOverride
{
    public PlayerState fromState;
    public PlayerState requestedState;
    public bool enabled = true;
    public PlayerState destinationState;
    public bool useCustomDestination;
    public string customDestinationId;
}

[System.Serializable]
public class PlayerCustomStateDefinition
{
    [HideInInspector] public string id;
    public string displayName = "新状态";
    [Tooltip("Animator 中的状态名；留空时由逻辑脚本自行播放动画。")]
    public string animationState;
    public PlayerMotionMode motionMode = PlayerMotionMode.Action;
    public bool applyAnimationRotation;
    [Min(0f)] public float rootMotionScale = 1f;
    [Min(0f)] public float crossFadeTime = 0.25f;
    public bool returnToIdleOnAnimationEnd;
    [Tooltip("可选。继承 PlayerCustomStateLogic 编写机制，创建对应资产后拖入。")]
    public PlayerCustomStateLogic logic;
}

[CreateAssetMenu(fileName = "PlayerDefinition", menuName = "Player/Player Definition")]
public class PlayerDefinition : ScriptableObject, IGameContent
{
    [SerializeField, HideInInspector] private string stableId;
    public string StableId => stableId;

    [Header("基础信息")]
    public string displayName = "Player";
    [TextArea(2, 4)] public string description;

    [Header("生存属性")]
    [Min(1f)] public float health = 100f;
    [Min(0f)] public float defense;
    [Range(0f, 1f), Tooltip("有防御值时，实际扣除生命的伤害比例。")]
    public float reductionRate = 0.5f;

    [Header("移动与相机")]
    public float gravity = -9.8f;
    [Tooltip("角色每秒最多转过的角度，单位为度。")]
    [Min(0f)] public float turnSpeed = 540f;
    [Range(0f, 1f), Tooltip("输入摇杆超过此强度才进入移动状态。")]
    public float moveInputThreshold = 0.5f;
    [Min(0f)] public float idleRootMotionScale = 1f;
    [Tooltip("1 为动画原始位移；仅缩放走路的水平 Root Motion。")]
    [Min(0f)] public float walkRootMotionScale = 1f;
    [Tooltip("1 为动画原始位移；仅缩放跑步的水平 Root Motion。")]
    [Min(0f)] public float runRootMotionScale = 1f;
    [Tooltip("瞄准和换弹移动的水平 Root Motion 倍率。")]
    [Min(0f)] public float aimRootMotionScale = 1f;
    [Tooltip("攻击和互动动作的 Root Motion 倍率。")]
    [Min(0f)] public float actionRootMotionScale = 1f;
    [Min(0f)] public float airborneRootMotionScale = 1f;
    [Min(0f)] public float cinematicRootMotionScale = 1f;
    [Tooltip("移动方向动画参数的平滑时间，单位为秒。")]
    [Min(0f)] public float moveBlendDampTime = 0.25f;
    [Tooltip("跑步时输入方向偏离角色朝向超过此角度，播放急转动画。")]
    [Range(0f, 180f)] public float runTurnAngle = 150f;
    [Range(0f, 180f), Tooltip("跑步微调方向超过此角度才触发左右转身动画，避免直行时反复触发。")]
    public float runSteerTriggerAngle = 5f;
    [Tooltip("走路时角色朝向。相机前方适合侧移，输入方向适合自由移动。")]
    public PlayerFacingMode walkFacingMode = PlayerFacingMode.CameraForward;
    [Tooltip("跑步时角色朝向。")]
    public PlayerFacingMode runFacingMode = PlayerFacingMode.InputDirection;
    [Range(0f, 180f), Tooltip("相机与角色朝向差达到此角度时，开始播放原地转身。")]
    public float idleTurnStartAngle = 54f;
    [Range(0f, 180f), Tooltip("相机与角色朝向差低于此角度时，结束原地转身。应不大于开始角度。")]
    public float idleTurnStopAngle = 54f;
    [Min(0f), Tooltip("接地时保持的小幅向下速度，帮助 CharacterController 紧贴地面。")]
    public float groundedDownSpeed = 2f;
    [Tooltip("下落的最大垂直速度，单位为米/秒。")]
    [Min(0.1f)] public float maxFallSpeed = 30f;
    [Tooltip("走跑累计此位移距离后触发一次脚步声，单位为米。")]
    [Min(0.1f)] public float footstepDistance = 1f;
    [Min(0f), Tooltip("小于此距离时，瞄准动画进入近距离姿态。")]
    public float aimNearObstacleDistance = 2.5f;
    [Min(0f), Tooltip("大于此距离时，瞄准动画退出近距离姿态。应不小于近距离阈值。")]
    public float aimFarObstacleDistance = 3f;

    [Header("运动动画")]
    public string idleAnimation = "Idle";
    public string idleTurnAnimation = "Turning";
    public string walkAnimation = "Moving";
    public string runAnimation = "Running";
    public string runTurnLeftAnimation = "Run_TurnL";
    public string runTurnRightAnimation = "Run_TurnR";
    public string aimMoveAnimation = "AimMoving";
    public string reloadAnimation = "Reload";
    public string fallAnimation = "Fall";
    public string landAnimation = "Land";
    [Tooltip("Animator 中的前后移动混合参数名。")]
    public string verticalBlendParameter = "Ver";
    [Tooltip("Animator 中的左右移动混合参数名。")]
    public string horizontalBlendParameter = "Hor";
    public string leftTurnTrigger = "L";
    public string rightTurnTrigger = "R";
    [Tooltip("瞄准距离切换姿态的 Animator Bool 参数名。")]
    public string aimCloseParameter = "isClose";

    [Header("相机")]
    [Min(0f)] public float cameraSensitivity = 1f;
    [Range(-89f, 0f)] public float cameraPitchMin = -30f;
    [Range(0f, 89f)] public float cameraPitchMax = 45f;

    [Header("交互检测")]
    [Min(0f)] public float interactRadius = 0.82f;
    [Min(0f)] public float enemyCheckRadius = 1.48f;
    [Min(0f)] public float groundCheckRadius = 0.3f;
    [Min(0f)] public float tacticalMoveRadius = 2f;

    [Header("初始物品与武器")]
    public List<StartingItemEntry> startingItems = new List<StartingItemEntry>();
    [FormerlySerializedAs("wps")]
    public List<WpConfig> startingWeapons = new List<WpConfig>();
    [Tooltip("玩家按瞄准键时默认选择的远程武器；不代表开局自动拥有。")]
    public WpConfig defaultRangedWeapon;
    [Tooltip("近战攻击、格挡与耐久使用的武器定义；拥有该武器后才能攻击。")]
    public WpConfig meleeWeapon;

    [Header("状态转移覆盖")]
    [Tooltip("只存与代码默认跳转不同的规则；从运动与状态表编辑。触发条件仍由状态代码决定。")]
    [HideInInspector]
    public List<PlayerTransitionOverride> transitionOverrides = new List<PlayerTransitionOverride>();

    [Header("自定义状态")]
    [HideInInspector] public List<PlayerCustomStateDefinition> customStates = new List<PlayerCustomStateDefinition>();

    // 兼容旧代码和旧资产命名，策划只需要编辑上面的字段。
    public IReadOnlyList<StartingItemEntry> Items => startingItems;
    public IReadOnlyList<WpConfig> Weapons => startingWeapons;

    public PlayerTransitionOverride FindTransitionOverride(PlayerState from, PlayerState requested)
    {
        if (transitionOverrides == null) return null;
        foreach (PlayerTransitionOverride rule in transitionOverrides)
            if (rule != null && rule.fromState == from && rule.requestedState == requested) return rule;
        return null;
    }

    public PlayerCustomStateDefinition FindCustomState(string id)
    {
        if (customStates == null || string.IsNullOrWhiteSpace(id)) return null;
        foreach (PlayerCustomStateDefinition state in customStates)
            if (state != null && state.id == id) return state;
        return null;
    }

    protected virtual void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(stableId))
        {
            stableId = GameContentId.Create();
        }

        health = Mathf.Max(1f, health);
        defense = Mathf.Max(0f, defense);
        reductionRate = Mathf.Clamp01(reductionRate);
        turnSpeed = Mathf.Max(0f, turnSpeed);
        moveInputThreshold = Mathf.Clamp01(moveInputThreshold);
        idleRootMotionScale = Mathf.Max(0f, idleRootMotionScale);
        walkRootMotionScale = Mathf.Max(0f, walkRootMotionScale);
        runRootMotionScale = Mathf.Max(0f, runRootMotionScale);
        aimRootMotionScale = Mathf.Max(0f, aimRootMotionScale);
        actionRootMotionScale = Mathf.Max(0f, actionRootMotionScale);
        airborneRootMotionScale = Mathf.Max(0f, airborneRootMotionScale);
        cinematicRootMotionScale = Mathf.Max(0f, cinematicRootMotionScale);
        moveBlendDampTime = Mathf.Max(0f, moveBlendDampTime);
        runTurnAngle = Mathf.Clamp(runTurnAngle, 0f, 180f);
        runSteerTriggerAngle = Mathf.Clamp(runSteerTriggerAngle, 0f, runTurnAngle);
        idleTurnStartAngle = Mathf.Clamp(idleTurnStartAngle, 0f, 180f);
        idleTurnStopAngle = Mathf.Clamp(idleTurnStopAngle, 0f, idleTurnStartAngle);
        groundedDownSpeed = Mathf.Max(0f, groundedDownSpeed);
        maxFallSpeed = Mathf.Max(0.1f, maxFallSpeed);
        footstepDistance = Mathf.Max(0.1f, footstepDistance);
        aimNearObstacleDistance = Mathf.Max(0f, aimNearObstacleDistance);
        aimFarObstacleDistance = Mathf.Max(aimNearObstacleDistance, aimFarObstacleDistance);
        cameraSensitivity = Mathf.Max(0f, cameraSensitivity);
        cameraPitchMin = Mathf.Clamp(cameraPitchMin, -89f, 0f);
        cameraPitchMax = Mathf.Clamp(cameraPitchMax, 0f, 89f);
        interactRadius = Mathf.Max(0f, interactRadius);
        enemyCheckRadius = Mathf.Max(0f, enemyCheckRadius);
        groundCheckRadius = Mathf.Max(0f, groundCheckRadius);
        tacticalMoveRadius = Mathf.Max(0f, tacticalMoveRadius);
        for (int i = 0; i < startingItems.Count; i++)
        {
            StartingItemEntry entry = startingItems[i];
            entry.amount = Mathf.Max(1, entry.amount);
            startingItems[i] = entry;
        }
        if (customStates != null)
            foreach (PlayerCustomStateDefinition state in customStates)
                if (state != null)
                {
                    state.rootMotionScale = Mathf.Max(0f, state.rootMotionScale);
                    state.crossFadeTime = Mathf.Max(0f, state.crossFadeTime);
                }
    }
}
