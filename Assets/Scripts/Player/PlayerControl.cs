using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
namespace Astraparasitus
{
    public enum PlayerState
    {
        Idle,
        Move,
        Aim,
        AimMove,
        Reload,
        Attack,
        Block,
        Parry,
        TC,
        KickBack,
        Pick,
        BreakItem,
        Dead,
        Hit,
        Fall,
        Land,
        Custom,
    }
    public class PlayerControl : MonoBehaviour, IStateMachineOwner
    {
        // Start is called before the first frame update
        public PlayerModel model;
        private static PlayerControl Instance;
        public static PlayerControl Current => Instance;
        [Header("状态机")]
        public StateMachine stateMachine;
        public CharacterController controller;
        public Rigidbody rb;
        [FormerlySerializedAs("gravity"), SerializeField, HideInInspector]
        private float legacyGravity = -9.8f;
        [FormerlySerializedAs("turnSpeed"), SerializeField, HideInInspector]
        private float legacyTurnSpeed = 540f;
        private float moveSpeed = 1f;
        private PlayerMotionMotor motion;
        public PlayerMotionMotor Motion => motion ?? (motion = new PlayerMotionMotor(this));
        private Coroutine moveCoroutine;
        public PlayerState currentState
        {
            get
            {
                StateBase state = stateMachine?.CurrentState;
                if (state is PlayerState_Move) return PlayerState.Move;
                if (state is PlayerState_Aim aim) return aim.IsMoving ? PlayerState.AimMove : PlayerState.Aim;
                if (state is PlaerState_Reload reload) return reload.IsMoving ? PlayerState.Move : PlayerState.Reload;
                if (state is PlayerState_Attack) return PlayerState.Attack;
                if (state is PlayerState_KnifeBlock) return PlayerState.Block;
                if (state is PlayerState_Parry) return PlayerState.Parry;
                if (state is PlayerState_TC) return PlayerState.TC;
                if (state is PlayerState_Kick) return PlayerState.KickBack;
                if (state is PlayerState_Pick) return PlayerState.Pick;
                if (state is PlayerState_BreakItem) return PlayerState.BreakItem;
                if (state is PlayerState_Dead) return PlayerState.Dead;
                if (state is PlayerState_Hit) return PlayerState.Hit;
                if (state is PlayerState_Fall) return PlayerState.Fall;
                if (state is PlayerState_Land) return PlayerState.Land;
                if (state is PlayerState_Custom) return PlayerState.Custom;
                return PlayerState.Idle;
            }
        }
        public string CurrentCustomStateId => (stateMachine?.CurrentState as PlayerState_Custom)?.definition.id;
        public event Action<PlayerControl> StateChanged;
        public event Action<PlayerControl> PresentationChanged;
        public string currentTCName;
        [Header("玩家定义")]
        [FormerlySerializedAs("playerConfig")]
        [SerializeField] private PlayerDefinition definition;
        public PlayerDefinition Definition => definition;
        public PlayerDefinition playerConfig => definition;
        public float MaxHealth => definition != null ? definition.health : 100f;
        public float MaxDefence => definition != null ? definition.defense : 0f;
        public float DamageReduction => definition != null ? definition.reductionRate : 0.5f;
        public float gravity => definition != null ? definition.gravity : legacyGravity;
        public float turnSpeed => definition != null ? definition.turnSpeed : legacyTurnSpeed;
        private float currentHp;
        private float currentDefence;
        [Header("相机")]
        public Camera mainCamera;
        public CinemachineVirtualCamera virtualCamera;
        public CinemachineVirtualCamera aimCamera;
        //public CinemachineVirtualCamera tcCamera;
        public Transform normalTarget;
        [Header("输入")]
        public PlayerInput playerInput;
        public CameraInput cameraInput;
        private float cameraTargetYaw;
        private float cameraTargetPitch;
        [Header("武器")]
        public GameObject weapon;
        [HideInInspector] public int currentWeaponIndex = 1;
        public List<GameObject> weaponModels = new List<GameObject>();
        public WpConfig currentWp;
        public List<WpConfig> wpConfigs = new List<WpConfig>();
        private readonly Dictionary<string, int> weaponAmmo = new Dictionary<string, int>();
        private readonly HashSet<string> invalidTransitionWarnings = new HashSet<string>();
        private readonly Dictionary<string, PlayerState_Custom> customStateInstances = new Dictionary<string, PlayerState_Custom>();
        public GameObject parryRange;
        public GameObject spotlight;
        [Header("刀相关")]
        [FormerlySerializedAs("knifeConfig"), SerializeField, HideInInspector]
        private WpConfig legacyKnifeConfig;
        public WpConfig knifeConfig => definition != null && definition.meleeWeapon != null
            ? definition.meleeWeapon
            : legacyKnifeConfig;
        public WpConfig MeleeWeapon => knifeConfig;
        private bool canAttack;
        public float currentUse;
        [Header("计算相关")]
        public Transform aimTarget;
        public Transform lhand;
        [Header("敌人相关")]
        public Transform enemyPosition;
        public bool tcKnife;
        [Header("UI")]
        public GameObject aimUI;
        public GameObject qteUI;
        public Text bulletCount;
        public bool IsAimVisible { get; private set; }
        public bool IsQteVisible { get; private set; }
        public float QteProgress { get; private set; }
        public Color QteColor { get; private set; } = Color.white;
        public bool IsKnifeVisible { get; private set; }
        public string AmmoText { get; private set; } = string.Empty;
        public void SetAimVisible(bool visible)
        {
            IsAimVisible = visible;
            PresentationChanged?.Invoke(this);
        }
        public void SetQteDisplay(bool visible, float progress, Color color)
        {
            IsQteVisible = visible;
            QteProgress = Mathf.Clamp01(progress);
            QteColor = color;
            PresentationChanged?.Invoke(this);
        }
        public void SetKnifeVisible(bool visible)
        {
            IsKnifeVisible = visible;
            PresentationChanged?.Invoke(this);
        }
        public void SetAmmoText(string value)
        {
            value = value ?? string.Empty;
            if (AmmoText == value) return;
            AmmoText = value;
            PresentationChanged?.Invoke(this);
        }
        public void RefreshPresentation() => PresentationChanged?.Invoke(this);
        [Header("特效相关")]
        public Transform knifeTrans;
        public GameObject knifeHitSpark;
        public void Init()
        {
            playerInput.Enable();
            cameraInput.Enable();
            foreach (GameObject weaponModel in weaponModels)
            {
                weaponModel.SetActive(false);
            }
            IsAimVisible = false;
            IsQteVisible = false;
            IsKnifeVisible = false;
            AmmoText = string.Empty;
            canAttack = currentUse > 0f;
            if (currentWp != null)
            {
                ChangeWeapon(currentWp);
            }
            RefreshPresentation();
        }
        void Awake()
        {
            if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            // 场景重载时新玩家可能先于旧玩家的 OnDestroy 初始化，由新实例直接接管。
            Instance = this;
            ResolveDefinition();
            ApplyDefinition();
            Cursor.lockState = CursorLockMode.Locked;
            //currentWp = null;
            currentHp = MaxHealth;
            currentDefence = MaxDefence;
            currentUse = knifeConfig != null ? Mathf.Max(0f, knifeConfig.bulletSize) : 0f;
            canAttack = currentUse > 0f;
            stateMachine = new StateMachine();
            stateMachine.Init(this);
            playerInput = new PlayerInput();
            cameraInput = new CameraInput();
            playerInput.Enable();
            cameraInput.Enable();
            rb = GetComponent<Rigidbody>();
            for (int i = 0; i < weapon.transform.childCount; i++)
            {
                weaponModels.Add(weapon.transform.GetChild(i).gameObject);
            }
            foreach (GameObject weaponModel in weaponModels)
            {
                weaponModel.SetActive(false);
            }
            IsAimVisible = false;
            IsQteVisible = false;
        }

        private void ResolveDefinition()
        {
            if (definition == null && GameContentCatalog.Default != null)
            {
                definition = GameContentCatalog.Default.Player;
            }

            if (definition == null)
            {
                Debug.LogError("PlayerControl requires a PlayerDefinition.", this);
            }
        }

        private void ApplyDefinition()
        {
            if (definition == null)
            {
                return;
            }

            if (wpConfigs.Count == 0)
            {
                foreach (WpConfig config in definition.startingWeapons)
                {
                    if (config != null && !wpConfigs.Contains(config))
                    {
                        wpConfigs.Add(config);
                    }
                }
            }

            if (definition.defaultRangedWeapon != null)
            {
                currentWeaponIndex = definition.defaultRangedWeapon.ID;
            }

            if (model != null)
            {
                model.checkRadius = definition.interactRadius;
                model.enemyCheckRadius = definition.enemyCheckRadius;
                model.groundRadius = definition.groundCheckRadius;
                model.radius = definition.tacticalMoveRadius;
            }
        }
        void Start()
        {
            ChangeState(PlayerState.Idle);
            RefreshPresentation();
        }

        // Update is called once per frame
        void Update()
        {
            if (currentState == PlayerState.Dead)
            {
                return;
            }

            if (playerInput.input.Interact.triggered && currentState != PlayerState.TC)
            {
                if (model.CheckEnemy())
                {
                    ChangeState(PlayerState.KickBack);
                }
                if (model.CheckInteract())
                {
                    switch (model.currentItem)
                    {
                        case ItemType.Pickup:
                            ChangeState(PlayerState.Pick);
                            break;
                        case ItemType.Break:
                            ChangeState(PlayerState.BreakItem);
                            break;
                        case ItemType.Open:
                            model.AddInteract();
                            break;
                    }
                }
            }
        }
        void FixedUpdate()
        {

        }
        void LateUpdate()
        {
            CameraControl();
        }
        public void ChangeState(PlayerState state, bool applyTransitionRules = true)
        {
            string customTarget = null;
            if (applyTransitionRules && !TryResolveTransition(ref state, out customTarget)) return;
            if (!string.IsNullOrWhiteSpace(customTarget))
            {
                ChangeState(customTarget);
                return;
            }
            if (state == PlayerState.AimMove || state == PlayerState.Custom)
            {
                Debug.LogError("此状态不能通过枚举直接切换；自定义状态请使用配置 ID。", this);
                return;
            }
            switch (state)
            {
                case PlayerState.Idle:
                    stateMachine.ChangeState<PlayerState_Idle>();
                    break;
                case PlayerState.Move:
                    stateMachine.ChangeState<PlayerState_Move>();
                    break;
                case PlayerState.Aim:
                    stateMachine.ChangeState<PlayerState_Aim>();
                    break;
                case PlayerState.Reload:
                    stateMachine.ChangeState<PlaerState_Reload>();
                    break;
                case PlayerState.Attack:
                    stateMachine.ChangeState<PlayerState_Attack>();
                    break;
                case PlayerState.Block:
                    stateMachine.ChangeState<PlayerState_KnifeBlock>();
                    break;
                case PlayerState.Parry:
                    stateMachine.ChangeState<PlayerState_Parry>();
                    break;
                case PlayerState.TC:
                    stateMachine.ChangeState<PlayerState_TC>();
                    break;
                case PlayerState.KickBack:
                    stateMachine.ChangeState<PlayerState_Kick>();
                    break;
                case PlayerState.Pick:
                    stateMachine.ChangeState<PlayerState_Pick>();
                    break;
                case PlayerState.BreakItem:
                    stateMachine.ChangeState<PlayerState_BreakItem>();
                    break;
                case PlayerState.Dead:
                    stateMachine.ChangeState<PlayerState_Dead>();
                    break;
                case PlayerState.Hit:
                    stateMachine.ChangeState<PlayerState_Hit>();
                    break;
                case PlayerState.Fall:
                    stateMachine.ChangeState<PlayerState_Fall>();
                    break;
                case PlayerState.Land:
                    stateMachine.ChangeState<PlayerState_Land>();
                    break;
            }
            StateChanged?.Invoke(this);
        }

        public bool ChangeState(string customStateId)
        {
            PlayerCustomStateDefinition custom = definition != null ? definition.FindCustomState(customStateId) : null;
            if (custom == null)
            {
                Debug.LogWarning("未找到自定义玩家状态：" + customStateId, this);
                return false;
            }
            if (!customStateInstances.TryGetValue(custom.id, out PlayerState_Custom instance))
            {
                instance = new PlayerState_Custom(custom);
                customStateInstances.Add(custom.id, instance);
            }
            if (!stateMachine.ChangeState(instance)) return false;
            StateChanged?.Invoke(this);
            return true;
        }

        private bool TryResolveTransition(ref PlayerState requested, out string customTarget)
        {
            customTarget = null;
            StateBase active = stateMachine?.CurrentState;
            if (active == null || definition == null) return true;
            if (active is PlayerState_Custom) return true;
            // AimMove 与换弹移动只是类内部的子状态，规则始终使用所属的主状态。
            PlayerState from = active is PlayerState_Aim ? PlayerState.Aim :
                active is PlaerState_Reload ? PlayerState.Reload : currentState;
            PlayerTransitionOverride rule = definition.FindTransitionOverride(from, requested);
            if (rule == null) return true;
            if (!rule.enabled) return false;
            if (rule.useCustomDestination)
            {
                if (definition.FindCustomState(rule.customDestinationId) == null)
                {
                    Debug.LogWarning("状态转移的自定义目标不存在：" + rule.customDestinationId, definition);
                    return true;
                }
                customTarget = rule.customDestinationId;
                return true;
            }
            if (rule.destinationState == PlayerState.AimMove || rule.destinationState == PlayerState.Custom)
            {
                Debug.LogError("AimMove 是 Aim 的内部子状态，不能作为状态跳转目标。", definition);
                return true;
            }
            if (rule.destinationState != requested && !CanEnterConfiguredState(rule.destinationState))
            {
                string key = from + ":" + requested + "→" + rule.destinationState;
                if (invalidTransitionWarnings.Add(key))
                    Debug.LogWarning("状态转移目标不满足当前前置条件，继续使用代码原目标：" + key, this);
                return true;
            }
            requested = rule.destinationState;
            return true;
        }

        private bool CanEnterConfiguredState(PlayerState target)
        {
            switch (target)
            {
                case PlayerState.Aim: return CheckWeapon(currentWeaponIndex);
                case PlayerState.Reload: return currentWp != null;
                case PlayerState.Attack:
                case PlayerState.Block:
                case PlayerState.Parry: return CheckWeapon(MeleeWeapon);
                case PlayerState.TC:
                case PlayerState.Hit: return enemyPosition != null && !string.IsNullOrWhiteSpace(currentTCName);
                case PlayerState.KickBack: return model != null && model.enemyTransform != null;
                case PlayerState.Pick: return model != null && model.CheckInteract() && model.currentItem == ItemType.Pickup;
                case PlayerState.BreakItem: return model != null && model.CheckInteract() && model.currentItem == ItemType.Break;
                default: return true;
            }
        }
        public void PlayAnimaton(string animName, float speed = 0.25f)
        {
            model.animator.CrossFadeInFixedTime(animName, speed);
        }
        public void PlayerAddAniamtion(string animName, float speed = 0.25f)
        {
            model.animator.CrossFadeInFixedTime(animName, speed, 2);
        }
        public void playerChoesLayerAnimation(string animName, int layer, float speed = 0.25f)
        {
            model.animator.CrossFadeInFixedTime(animName, speed, layer);
        }
        public void AddWeapon(WpConfig wp)
        {
            if (wp == null)
            {
                return;
            }

            if (IsSameWeapon(wp, MeleeWeapon))
            {
                if (currentUse <= 0f)
                {
                    currentUse = Mathf.Max(0f, wp.bulletSize);
                }
                canAttack = currentUse > 0f;
                RefreshPresentation();
            }

            wpConfigs.Add(wp);
            if (IsSameWeapon(wp, MeleeWeapon))
            {
                MoveWeaponToFirstPosition(wp);
            }
            else
            {
                for (int i = 0; i < wpConfigs.Count; i++)
                {
                    if (IsSameWeapon(wpConfigs[i], MeleeWeapon))
                    {
                        MoveWeaponToFirstPosition(wpConfigs[i]);
                        break;
                    }
                }
            }
        }
        private void MoveWeaponToFirstPosition(WpConfig wp)
        {
            wpConfigs.Remove(wp);
            wpConfigs.Insert(0, wp);
        }
        public void ChangeWeapon(int index)
        {
            int configIndex = wpConfigs.FindIndex(x => x != null && x.ID == index);
            if (configIndex < 0)
            {
                Debug.LogWarning($"Weapon config was not found. ID: {index}");
                return;
            }

            ChangeWeapon(wpConfigs[configIndex]);
        }

        public void ChangeWeapon(WpConfig config)
        {
            WpConfig ownedConfig = wpConfigs.Find(x => IsSameWeapon(x, config));
            if (ownedConfig == null)
            {
                Debug.LogWarning($"Weapon config is not owned: {config}");
                return;
            }

            GameObject selectedModel = GetWeaponModel(ownedConfig);
            if (selectedModel == null)
            {
                Debug.LogError($"Weapon model is not registered: {ownedConfig}");
                return;
            }

            currentWp = ownedConfig;
            foreach (GameObject weaponModel in weaponModels)
            {
                weaponModel.SetActive(false);
            }
            selectedModel.SetActive(true);
            RefreshPresentation();
            PlayAnimaton(currentWp.toIdle);
        }

        public int GetWeaponAmmo(WpConfig weaponConfig, int defaultValue)
        {
            if (weaponConfig == null)
            {
                return 0;
            }

            string key = GetWeaponRuntimeKey(weaponConfig);
            if (!weaponAmmo.TryGetValue(key, out int ammo))
            {
                ammo = Mathf.Max(0, defaultValue);
                weaponAmmo[key] = ammo;
            }

            return ammo;
        }

        public void SetWeaponAmmo(WpConfig weaponConfig, int ammo)
        {
            if (weaponConfig != null)
            {
                weaponAmmo[GetWeaponRuntimeKey(weaponConfig)] = Mathf.Max(0, ammo);
            }
        }

        public List<WeaponAmmoData> CaptureWeaponAmmo()
        {
            List<WeaponAmmoData> data = new List<WeaponAmmoData>();
            foreach (WpConfig config in wpConfigs)
            {
                if (config == null)
                {
                    continue;
                }

                data.Add(new WeaponAmmoData
                {
                    weaponId = config.StableId,
                    legacyWeaponId = config.ID,
                    ammo = GetWeaponAmmo(config, config.bulletTotalNum)
                });
            }

            return data;
        }

        public PlayerRuntimeState CaptureRuntimeState()
        {
            return new PlayerRuntimeState
            {
                health = currentHp,
                defence = currentDefence,
                knifeUse = currentUse,
                weaponAmmo = CaptureWeaponAmmo()
            };
        }

        public void RestoreRuntimeState(PlayerRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            currentHp = Mathf.Max(0f, state.health);
            currentDefence = Mathf.Max(0f, state.defence);
            currentUse = Mathf.Max(0f, state.knifeUse);
            RestoreWeaponAmmo(state.weaponAmmo);
        }

        public void RestoreFromSave(PlayerRuntimeState state, List<WpConfig> weapons, WpConfig equippedWeapon, Vector3 position)
        {
            model.ClearRootMotionAction();
            RestoreRuntimeState(state);
            wpConfigs = weapons ?? new List<WpConfig>();
            currentWp = equippedWeapon;
            transform.position = position;
            Motion.ResetVerticalMotion();
            ChangeState(PlayerState.Idle, false);
            Init();
        }

        public void RestoreWeaponAmmo(List<WeaponAmmoData> data)
        {
            weaponAmmo.Clear();
            if (data == null)
            {
                return;
            }

            foreach (WeaponAmmoData item in data)
            {
                string key = !string.IsNullOrWhiteSpace(item.weaponId)
                    ? item.weaponId
                    : item.legacyWeaponId.ToString();
                weaponAmmo[key] = Mathf.Max(0, item.ammo);
            }
        }

        private static string GetWeaponRuntimeKey(WpConfig weaponConfig)
        {
            return !string.IsNullOrWhiteSpace(weaponConfig.StableId)
                ? weaponConfig.StableId
                : weaponConfig.ID.ToString();
        }

        public GameObject GetWeaponModel(WpConfig config)
        {
            if (config == null)
            {
                return null;
            }

            foreach (GameObject modelObject in weaponModels)
            {
                if (modelObject == null)
                {
                    continue;
                }

                WeaponControl rangedWeapon = modelObject.GetComponent<WeaponControl>();
                if (rangedWeapon != null && IsSameWeapon(rangedWeapon.weapon, config))
                {
                    return modelObject;
                }

                KnifeWeapon knifeWeapon = modelObject.GetComponent<KnifeWeapon>();
                if (knifeWeapon != null && IsSameWeapon(knifeWeapon.wpConfig, config))
                {
                    return modelObject;
                }
            }

            return null;
        }

        private static bool IsSameWeapon(WpConfig left, WpConfig right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(left.StableId) && !string.IsNullOrWhiteSpace(right.StableId))
            {
                return left.StableId == right.StableId;
            }

            return left.ID == right.ID;
        }
        public void RemoveWeapon(int index)
        {
            if (wpConfigs.Count > 0)
            {
                int num = wpConfigs.FindIndex(x => x.ID == index);
                if (num != -1)
                {
                    wpConfigs.RemoveAt(num);
                    foreach (GameObject weaponModel in weaponModels)
                    {
                        weaponModel.SetActive(false);
                    }
                    if (wpConfigs.Count > 0)
                    {
                        ChangeWeapon(wpConfigs[0].ID);
                    }
                    else
                    {
                        currentWp = null;
                        RefreshPresentation();
                    }
                }
            }
        }
        public bool CheckWeapon(int index)
        {
            return CheckWeapon(wpConfigs.Find(x => x != null && x.ID == index));
        }
        public bool CheckWeapon(WpConfig config)
        {
            WpConfig ownedConfig = wpConfigs.Find(x => IsSameWeapon(x, config));
            return ownedConfig != null && (!IsSameWeapon(ownedConfig, MeleeWeapon) || canAttack);
        }
        public void CameraControl()
        {
            float mouseX = cameraInput.Camera.CameraLook.ReadValue<Vector2>().x;
            float mouseY = cameraInput.Camera.CameraLook.ReadValue<Vector2>().y;
            float sensitivity = definition != null ? definition.cameraSensitivity : 1f;
            cameraTargetYaw += mouseX * sensitivity * Time.deltaTime;
            cameraTargetPitch -= mouseY * sensitivity * Time.deltaTime;

            cameraTargetYaw = ClampAngle(cameraTargetYaw, -360f, 360f);
            float pitchMin = definition != null ? definition.cameraPitchMin : -30f;
            float pitchMax = definition != null ? definition.cameraPitchMax : 45f;
            cameraTargetPitch = ClampAngle(cameraTargetPitch, pitchMin, pitchMax);

            normalTarget.transform.rotation = Quaternion.Euler(cameraTargetPitch, cameraTargetYaw, 0f);

        }
        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }
        public void SetAnimaionSpeed(float speed)
        {
            moveSpeed = speed;
        }
        public float GetAnimaionSpeed()
        {
            return moveSpeed;
        }
        public void DecreaseHp(float value)
        {
            if(currentDefence <= 0)
            {
                currentHp -= value;
            }
            else
            {
                currentHp -= value * DamageReduction;
            }
            if(currentHp >= MaxHealth)
            {
                currentHp = MaxHealth;
            }
            if(currentHp <= 0)
            {
                ChangeState(PlayerState.Dead);
            }
            RefreshPresentation();
        }
        public void SetHp(float value)
        {
            currentHp = value;
            RefreshPresentation();
        }
        public void DecreaseDefence(float value)
        {
            if(currentDefence <= 0)
            {
                return;
            }
            currentDefence -= value;
            RefreshPresentation();
        }
        public float GetHp()
        {
            return currentHp;
        }
        public float GetDefence()
        {
            return currentDefence;
        }
        public void HitSpeed(float speed)
        {
            SetAnimaionSpeed(speed);
            moveCoroutine = StartCoroutine(MoveCoroutine(0.7f));
        }
        private IEnumerator MoveCoroutine(float time)
        {
            yield return new WaitForSecondsRealtime(time);
            SetAnimaionSpeed(1f);
        }
        public float GetCurrentHp()
        {
            return currentHp;
        }
        public float GetCurrentDefence()
        {
            return currentDefence;
        }
        public void SetCurrentHp(float value)
        {
            currentHp = value;
            RefreshPresentation();
        }
        public void SetCurrentDefence(float value)
        {
            currentDefence = value;
            RefreshPresentation();
        }
        public void DescreaseKnifeUse(float value)
        {
            GameObject knifeModel = GetWeaponModel(knifeConfig);
            KnifeWeapon knifeWeapon = knifeModel != null ? knifeModel.GetComponent<KnifeWeapon>() : null;
            int maxUse = knifeWeapon != null && knifeWeapon.wpConfig != null
                ? knifeWeapon.wpConfig.bulletSize
                : knifeConfig != null ? knifeConfig.bulletSize : 0;
            currentUse = Mathf.Clamp(currentUse - value, 0f, maxUse);
            canAttack = currentUse > 0f;
            if (knifeModel != null && !canAttack)
            {
                knifeModel.SetActive(false);
            }
            RefreshPresentation();
        }
        public void SetTCKnife(bool value)
        {
            tcKnife = value;
        }
        public bool GetTCKnife()
        {
            return tcKnife;
        }

        private void OnEnable()
        {
            stateMachine?.Resume();
        }

        private void OnDisable()
        {
            stateMachine?.Pause();
        }

        private void OnDestroy()
        {
            // 场景卸载时不再执行状态的玩法退出逻辑，避免操作正在销毁的场景对象。
            stateMachine?.Stop(false);
            customStateInstances.Clear();
            playerInput?.Dispose();
            cameraInput?.Dispose();
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}

