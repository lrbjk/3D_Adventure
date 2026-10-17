using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using UnityEngine.UI;

    public enum EnemyState
    {
        Idle,
        Chase,
        Break,
        ToNoLeg,
        NoLegIdle,
        NoLegChase,
        Attack,
        SpAttack,
        BreakHit,
        Die,
        //特殊状态
        Recovery,
        TCEnd,
        TCLoop,
        Other,
        Angery,
        Pick,
        ShowUp,
    }
    public enum EnemyLegState
    {
        Normal,
        Broken,
    }
namespace Astraparasitus
{
    [DefaultExecutionOrder(-102)]
    public class EnemyBase : MonoBehaviour, IStateMachineOwner, Iinteractable
    {
        [Header("敌人原型")]
        [FormerlySerializedAs("enemiesInfo")]
        [SerializeField, Tooltip("集中管理属性、感知、技能、音效与掉落。")]
        private EnemyArchetype archetype;
        public EnemyArchetype Archetype => archetype;
        // 保留旧代码读取入口，后续模块可以逐步改用 Archetype。
        public EnemyArchetype enemiesInfo => archetype;
        private float currentHitStun;
        private float currentHp;
        private float currentEndurance;
        private float currentLeftLegValue;
        private float currentRightLegValue;
        private float currentLeftArmValue;
        private float currentRightArmValue;
        [HideInInspector] public float currentSpeed;
        [HideInInspector] public float recoverTime;
        public EnemyModel enemyModel;
        public Transform root;
        public CharacterController controller;
        [HideInInspector] public float gravity = -9.8f;
        public AIDetect aiDetect;
        private bool isInBreak;
        [SerializeField] private bool legBreak;
        public bool isTCHit;
        private bool isDie;
        private bool isHit;
        public string dieAnimName;
        private GameObject currentVFX;
        [Header("状态机")]
        public StateMachine stateMachine;
        private Coroutine hitStunCoroutine;
        private Coroutine hitDecreaseSpeed;
        private Coroutine recoverCoroutine;
        public EnemyLegState currentLegState;
        private int skillIndex;
        private bool isSkillReady;
        private bool startAttack;
        //[Header("寻路")]
        ////public NavMeshSurface navMeshSurface;
        //public Transform _navMeshBuildSource;
        //public Vector3 _navMeshSize = new Vector3(50, 10, 50);
        //private NavMeshData _navMeshData;
        //private NavMeshDataInstance _navMeshDataInst;
        //private Bounds _tempBounds = new Bounds();
        //private List<MeshFilter> _meshFilterList = new List<MeshFilter>();
        //private List<NavMeshBuildSource> _navMeshBuildSourceList = new List<NavMeshBuildSource>();
        public NavMeshAgent agent;
        [Header("NavMesh ID")]
        [SerializeField]
        private string navMeshAreaName = "EnemyArea";
        public string NavMeshAreaName => navMeshAreaName;
        private int navMeshAreaID;
        public Transform currentAimPosition;
        [Header("测试UI")]
        public Text debugText;
        public GameObject breakUI;
        [HideInInspector] public string idleSound;
        [HideInInspector] public string chaseSound;
        [HideInInspector] public string breakSound;
        [HideInInspector] public string attackSound;
        [HideInInspector] public string hitSound;
        [HideInInspector] public string dieSound;
        public AudioSource audioSource;
        [HideInInspector] public GameObject prefab;
        [HideInInspector] public bool isRandom;
        private bool hasRolledLoot;
        // Start is called before the first frame update
        private void OnDisable()
        {
            stateMachine?.Pause();
            if (LevelContext.Current != null && LevelContext.Current.Enemies != null)
                LevelContext.Current.Enemies.Unregister(this);
            else
                GamePlay.Instance?.UnregisterEnemy(this);
            legBreak = false;
            isTCHit = false;
            isDie = false;
            isHit = false;
            isSkillReady = false;
            startAttack = false;
            audioSource?.Stop();
            if (audioSource != null)
            {
                audioSource.enabled = false;
            }
            enemyModel?.ResetDistroyEffect();
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
        protected virtual void OnEnable()
        {
            Init();
            stateMachine?.Resume();
            if (LevelContext.Current != null && LevelContext.Current.Enemies != null)
                LevelContext.Current.Enemies.Register(this);
            else
                GamePlay.Instance?.RegisterEnemy(this);
        }
        protected void Init()
        {
            hasRolledLoot = false;
            ApplyArchetype();
            StaticData.AddEnemyId();
            //navMeshAreaName = StaticData.enemyId.ToString();
            navMeshAreaID = NavMesh.GetAreaFromName(navMeshAreaName);
            agent.areaMask = 1 << navMeshAreaID;

            audioSource = GetComponent<AudioSource>();
            audioSource.enabled = true;
            OpenIK(root);
            breakUI.SetActive(false);
            currentHitStun = enemiesInfo.hitStunValue;
            currentHp = enemiesInfo.health;
            currentEndurance = enemiesInfo.endurance;
            currentSpeed = enemiesInfo.speedRate;
            currentLeftLegValue = enemiesInfo.leftLegValue;
            currentRightLegValue = enemiesInfo.rightLegValue;
            currentLeftArmValue = enemiesInfo.leftArmValue;
            currentRightArmValue = enemiesInfo.rightArmValue;
            aiDetect.enabled = true;
            legBreak = false;
            isInBreak = false;
            SetupAgentAvoidance();
            //if (_navMeshBuildSource == null)
            //{
            //    _navMeshBuildSource = GameObject.FindGameObjectWithTag("Level").transform;
            //}
            //if (_navMeshData == null)
            //{
            //    _navMeshData = new NavMeshData();
            //}

            //if (!_navMeshDataInst.valid)
            //{
            //    _navMeshDataInst = NavMesh.AddNavMeshData(_navMeshData);
            //}


            //UpdateNavMeshBuildSource();
            //UpdateNavMash();
            agent.enabled = true;
            agent.updatePosition = false;
            agent.updateRotation = true;
            //agent.isStopped = false;
            //UpdateAgent();

        }

        private void ApplyArchetype()
        {
            if (archetype == null)
            {
                Debug.LogError("Enemy has no EnemyArchetype.", this);
                return;
            }

            recoverTime = archetype.recoverTime;
            gravity = archetype.gravity;
            navMeshAreaName = archetype.navMeshAreaName;
            idleSound = archetype.idleSound;
            chaseSound = archetype.chaseSound;
            breakSound = archetype.breakSound;
            attackSound = archetype.attackSound;
            hitSound = archetype.hitSound;
            dieSound = archetype.dieSound;
            prefab = archetype.fallbackLootPrefab;
            isRandom = archetype.useLegacyRandomLoot;

            if (aiDetect != null)
            {
                aiDetect.viewRadius = archetype.viewRadius;
                aiDetect.viewAngleStep = archetype.viewAngleStep;
                aiDetect.viewAngle = archetype.viewAngle;
                aiDetect.offset = archetype.viewOffset;
                aiDetect.attackDistence = archetype.attackDistance;
                aiDetect.farAttackDistence = archetype.farAttackDistance;
                aiDetect.tcDistence = archetype.executionDistance;
                aiDetect.forgetDelay = archetype.forgetDelay;
                aiDetect.useDistanceCheck = archetype.useDistanceCheck;
                aiDetect.ignoreLayer = archetype.ignoreLayer;
            }

            if (enemyModel != null)
            {
                enemyModel.skillConfigs = archetype.skills ?? System.Array.Empty<SkillConfig>();
            }
        }

        public void SpawnLoot()
        {
            if (hasRolledLoot) return;
            hasRolledLoot = true;
            if (!isRandom && prefab != null)
            {
                SpawnLootPrefab(prefab);
                return;
            }
            if (isRandom || archetype != null && archetype.lootTable != null)
            {
                GamePlay.Instance?.SpawnResourceDrop(ResourceDropSource.Enemy, archetype?.lootTable, transform.position);
                return;
            }
        }

        private void SpawnLootPrefab(GameObject lootPrefab)
        {
            if (lootPrefab == null || ObjectPool.Instance == null)
            {
                return;
            }

            GameObject item = ObjectPool.Instance.GetObject(lootPrefab);
            if (item != null)
            {
                item.transform.position = transform.position;
            }
        }
        public void InitNavMesh()
        {
            //UpdateNavMeshBuildSource();
            //UpdateNavMash();
            //UpdateAgent(GamePlay.Instance.player.transform.position);
            agent.Warp(transform.position);
        }
        void SetupAgentAvoidance()
        {
            agent.radius = 0.4f;              // 身体半径（很关键）
            agent.height = 2.0f;

            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            agent.avoidancePriority = UnityEngine.Random.Range(30, 60);
            // 数字越小 → 越“强势”
            // 随机一下，防止完全对称 → 卡死
        }

        public void RemoveNavMesh()
        {
            //_navMeshData = null;
            //_navMeshDataInst.Remove();
        }
        public void SetEnemyChase()
        {
            aiDetect.SetDetected(true);
        }
        public void SetCanAttack()
        {
            aiDetect.SetCanAttack(true);
        }
        public void SetNoLeg()
        {
            if (!isInBreak && !legBreak)
            {
                SetLegBreakState(true);
                ChangeState(EnemyState.ToNoLeg);
                //legBreak = true;
            }
        }

        private void Awake()
        {
            stateMachine = new StateMachine();
            stateMachine.Init(this);
            //Init();
        }
        public void UpdateNavMeshBuildSource()
        {
            //_meshFilterList.Clear();
            //_navMeshBuildSourceList.Clear();

            //_navMeshBuildSource.GetComponentsInChildren(false, _meshFilterList);

            //for (int i = 0; i < _meshFilterList.Count; ++i)
            //{
            //    var meshFilter = _meshFilterList[i];
            //    if (meshFilter == null) continue;

            //    var mesh = meshFilter.sharedMesh;
            //    if (mesh == null) continue;

            //    var source = new NavMeshBuildSource
            //    {
            //        shape = NavMeshBuildSourceShape.Mesh,
            //        sourceObject = mesh,
            //        transform = meshFilter.transform.localToWorldMatrix,
            //        area = navMeshAreaID
            //    };

            //    _navMeshBuildSourceList.Add(source);
            //}
        }

        public void UpdateNavMash()
        {
            //var defaultSettings = NavMesh.GetSettingsByID(0);
            //_tempBounds.center = transform.position;
            //_tempBounds.size = _navMeshSize;
            //NavMeshBuilder.UpdateNavMeshData(_navMeshData, defaultSettings, _navMeshBuildSourceList, _tempBounds);
        }
        public void UpdateAgent(Transform aimPosition)
        {
            if (agent == null || !agent.enabled) return;

            // 更新目标位置
            if (aimPosition != null)
            {
                //Debug.Log(aimPosition);
                agent.SetDestination(aimPosition.position);
                currentAimPosition = aimPosition;
            }
        }
        public void UpdateAgent(Vector3 aimPosition)
        {
            if (agent == null || !agent.enabled) return;

            // 更新目标位置
            if (aimPosition != null)
            {
                //Debug.Log(aimPosition);
                agent.SetDestination(aimPosition);
            }
        }
        void Start()
        {

        }
        public void ChangeState(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Idle:
                    stateMachine.ChangeState<Enemy_Idle>();
                    break;
                case EnemyState.Chase:
                    stateMachine.ChangeState<Enemy_Chase>(true);
                    break;
                case EnemyState.Break:
                    stateMachine.ChangeState<Enemy_Break>();
                    break;
                case EnemyState.ToNoLeg:
                    stateMachine.ChangeState<Enemy_ToNoLeg>();
                    break;
                case EnemyState.NoLegIdle:
                    stateMachine.ChangeState<Enemy_NoLeg>();
                    break;
                case EnemyState.NoLegChase:
                    stateMachine.ChangeState<Enemy_NoLegChase>();
                    break;
                case EnemyState.Attack:
                    stateMachine.ChangeState<Enemy_Attack>();
                    break;
                case EnemyState.SpAttack:
                    stateMachine.ChangeState<Enemy_SpAttack>();
                    break;
                case EnemyState.BreakHit:
                    stateMachine.ChangeState<Enemy_Hit>();
                    break;
                case EnemyState.Die:
                    stateMachine.ChangeState<Enemy_Dead>();
                    break;
                case EnemyState.Recovery:
                    stateMachine.ChangeState<Enemy_Recovery>();
                    break;
                case EnemyState.TCEnd:
                    stateMachine.ChangeState<Enemy_TCEnd>();
                    break;
                case EnemyState.TCLoop:
                    stateMachine.ChangeState<Enemy_TCLoop>();
                    break;
                case EnemyState.Other:
                    stateMachine.ChangeState<Enemy_Other>();
                    break;
                case EnemyState.Angery:
                    stateMachine.ChangeState<Enemy_Angery>();
                    break;
                case EnemyState.Pick:
                    stateMachine.ChangeState<Enemy_PickBomb>();
                    break;
                case EnemyState.ShowUp:
                    stateMachine.ChangeState<Enemy_ShowUp>();
                    break;
            }
        }
        public void OpenIK(Transform parent)
        {
            controller.enabled = true;
            enemyModel.animator.enabled = true;
            if (parent.gameObject.GetComponent<Rigidbody>() != null)
            {
                parent.gameObject.GetComponent<Rigidbody>().isKinematic = true;
                //parent.gameObject.GetComponent<Collider>().enabled = false;
            }
            // 检查当前对象是否有子物体
            if (parent.childCount > 0)
            {
                // 遍历所有子物体
                foreach (Transform child in parent)
                {
                    if (child.gameObject.GetComponent<Rigidbody>() != null)
                    {
                        child.gameObject.GetComponent<Rigidbody>().isKinematic = true;
                        //child.gameObject.GetComponent<Collider>().enabled = false;
                    }
                    // 递归调用自身来收集子物体的子物体中的骨骼
                    OpenIK(child);
                }
            }
        }
        public void CloseIK(Transform parent)
        {
            controller.enabled = false;
            enemyModel.animator.enabled = false;
            if (parent.gameObject.GetComponent<Rigidbody>() != null)
            {
                parent.gameObject.GetComponent<Rigidbody>().isKinematic = false;
                //parent.gameObject.GetComponent<Collider>().enabled = true;
            }
            // 检查当前对象是否有子物体
            if (parent.childCount > 0)
            {
                // 遍历所有子物体
                foreach (Transform child in parent)
                {
                    if (child.gameObject.GetComponent<Rigidbody>() != null)
                    {
                        child.gameObject.GetComponent<Rigidbody>().isKinematic = false;
                        //child.gameObject.GetComponent<Collider>().enabled = true;
                    }
                    // 递归调用自身来收集子物体的子物体中的骨骼
                    CloseIK(child);
                }
            }

        }

        // Update is called once per frame
        protected void Update()
        {
            HitStun();
            DebugLog();
            CheckLegState();
            if(currentEndurance <= 0)
            {
                recoverCoroutine = StartCoroutine(RecoveringEndurance(recoverTime));
            }
        }
        public void CheckLegState()
        {
            if (currentLegState == EnemyLegState.Normal)
            {
                aiDetect.useDistanceCheck = true;
            }
            else if (currentLegState == EnemyLegState.Broken)
            {
                aiDetect.useDistanceCheck = false;
            }
        }
        public void HitStun()
        {
            if (currentHitStun <= 0 && !legBreak)
            {
                hitStunCoroutine = StartCoroutine(HitStunCoroutine(enemiesInfo.stunTime));
            }
            else
            {
                if (hitStunCoroutine != null)
                {
                    StopCoroutine(hitStunCoroutine);
                }
            }
        }
        public void DecreaseStunValue(float value)
        {
            currentHitStun -= value;
        }
        public void DecreaseHp(float value)
        {
            currentHp -= value;
            if (currentHp <= 0)
            {
                if (hitStunCoroutine != null)
                {
                    StopCoroutine(hitStunCoroutine);
                }
                if (dieAnimName == "" && !legBreak)
                {
                    dieAnimName = "NormalDie";
                }
                else if(dieAnimName == "" && legBreak)
                {
                    dieAnimName = "TC01_End";
                }
                Death();
            }
            else
            {
                if (dieAnimName != "" && !legBreak)
                {
                    if (hitStunCoroutine != null)
                    {
                        StopCoroutine(hitStunCoroutine);
                    }
                    SetHit(true);
                }
            }
        }
        public void DecreaseSpeed(float value)
        {
            currentSpeed = value;
            hitDecreaseSpeed = StartCoroutine(HitDecreaseSpeedCoroutine(0.5f));
        }
        public void DecreaseLeftLegValue(float value)
        {
            currentLeftLegValue -= value;
            if (currentLeftLegValue <= 0)
            {
                if (!isInBreak && !legBreak)
                {
                    ChangeState(EnemyState.ToNoLeg);
                    //legBreak = true;
                }
            }
        }
        public void DecreaseRightLegValue(float value)
        {
            currentRightLegValue -= value;
            if (currentRightLegValue <= 0)
            {
                if (!isInBreak && !legBreak)
                {
                    //PlayAnimaton("ToNoLeg");
                    //legBreak = true;
                    ChangeState(EnemyState.ToNoLeg);
                }
            }
        }
        public void DecreaseLeftArmValue(float value)
        {
            currentLeftArmValue -= value;
            if (currentLeftArmValue <= 0)
            {
            }
        }
        public void DecreaseRightArmValue(float value)
        {
            currentRightArmValue -= value;
            if (currentRightArmValue <= 0)
            {
            }
        }
        private IEnumerator HitDecreaseSpeedCoroutine(float time)
        {
            yield return new WaitForSecondsRealtime(time);
            currentSpeed = enemiesInfo.speedRate;
        }
        public void Death()
        {
            isInBreak = false;
            SetDie(true);
            aiDetect.SetDetected(false);
            aiDetect.enabled = false;
            agent.enabled = false;
            audioSource.Stop();
            audioSource.enabled = false;
            
            //RemoveNavMesh();
        }

        public void ForceKill()
        {
            CloseIK(root);
            Death();
        }
        public void DebugLog()
        {
            if (debugText != null)
                debugText.text = "Current HP: " + currentHp + "\nCurrent HitStun: " + currentHitStun + "\nLeft Leg Value: " + currentLeftLegValue + "\nRight Leg Value: " + currentRightLegValue + "\nLeft Arm Value: " + currentLeftArmValue + "\nRight Arm Value: " + currentRightArmValue + "\nCurrent Speed: " + currentSpeed;

        }
        public void PlayAnimaton(string animName, float speed = 0.25f)
        {
            enemyModel.animator.CrossFadeInFixedTime(animName, speed);
        }
        public void playerChoesLayerAnimation(string animName, int layer, float speed = 0.25f)
        {
            enemyModel.animator.CrossFadeInFixedTime(animName, speed, layer);
        }
        private IEnumerator HitStunCoroutine(float time)
        {
            aiDetect.enabled = false;
            isInBreak = true;
            yield return new WaitForSeconds(time);
            breakUI.SetActive(false);
            aiDetect.enabled = true;
            currentHitStun = enemiesInfo.hitStunValue;
            isInBreak = false;
        }
        public void SetBreakState(bool state)
        {
            isInBreak = state;
        }
        public bool GetBreakState()
        {
            return isInBreak;
        }
        public void SetLegBreakState(bool state)
        {
            legBreak = state;
        }
        public bool GetLegBreakState()
        {
            return legBreak;
        }
        public void SetSkillIndex(int index)
        {
            skillIndex = index;
        }
        public int GetSkillIndex()
        {
            return skillIndex;
        }
        public void SetSkillReady(bool state)
        {
            isSkillReady = state;
        }
        public bool GetSkillReady()
        {
            return isSkillReady;
        }
        public void SetStartAttack(bool state)
        {
            startAttack = state;
        }
        public bool GetStartAttack()
        {
            return startAttack;
        }
        public void SetDie(bool state)
        {
            isDie = state;
        }
        public bool GetDie()
        {
            return isDie;
        }
        public void SetHit(bool state)
        {
            isHit = state;
        }
        public bool GetHit()
        {
            return isHit;
        }
        public void DecreaseEndurance(float value)
        {
            currentEndurance -= value;
        }
        public float GetEndurance()
        {
            return currentEndurance;
        }
        private IEnumerator RecoveringEndurance(float time)
        {
            yield return new WaitForSeconds(time);
            currentEndurance = enemiesInfo.endurance;
        }
        public void Interact()
        {

        }
        public float GetCurrentHp()
        {
            return currentHp;
        }
        public void ShowInfo()
        {
            if (GetBreakState())
            {
                breakUI.SetActive(true);
            }
        }
        private void OnDestroy()
        {
            stateMachine?.Stop();
        }
        public void HideInfo()
        {
            breakUI.SetActive(false);
        }
        public void SetCurrentVFX(GameObject obj)
        {
            currentVFX = obj;
        }
        public GameObject GetCurrentVFX()
        {
            return currentVFX;
        }
    }
}

