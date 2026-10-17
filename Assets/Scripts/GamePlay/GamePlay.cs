using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
namespace Astraparasitus
{
    public class GamePlay : SingletonMono<GamePlay>
    {
        public PlayerControl player;
        [SerializeField] private EnemyRegistry enemyRegistry;
        [FormerlySerializedAs("enemies"), SerializeField, HideInInspector]
        private List<EnemyBase> legacyEnemies = new List<EnemyBase>();
        public EnemyRegistry EnemyRegistry => enemyRegistry;
        public IReadOnlyList<EnemyBase> enemies => enemyRegistry != null ? enemyRegistry.Enemies : legacyEnemies;
        public UIInput uiInput;
        public AudioSource audioSource;
        [Header("动态资源")]
        [SerializeField] private ResourceDirector resourceDirector;
        [Header("UI")]
        public GameObject backpackUI;
        public Text tipsUI;
        public GameObject questUI;
        public GameObject textAreaUI;
        public Text textArea;
        private bool colding;
        private Coroutine coldTime;
        private Coroutine showTipsTime;
        public GameObject ItemUI;
        public Transform ShowPoint;
        public Text ItemName;
        public GameObject[] interactUI;
        public Image playerHp;
        public GameObject playerKnife;
        public Image knifeUse;
        public Image wpIcon;
        public GameObject dieUI;
        private Coroutine dieUITime;
        private Coroutine destroyObj;
        public GameObject loadingUI;
        public GameObject autoSaveUI;
        public GameObject saveMenu;
        [Header("提前销毁物品")]
        public List<GameObject> objs = new List<GameObject>();
        // Start is called before the first frame update
        protected override void Awake()
        {
            base.Awake();
            Application.targetFrameRate = 60;
        }
        void Start()
        {
            uiInput = new UIInput();
            uiInput.Enable();
            player = PlayerControl.Current;
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                player = playerObject != null ? playerObject.GetComponent<PlayerControl>() : null;
            }

            if (player == null)
            {
                Debug.LogError("GamePlay could not find the active PlayerControl.");
                enabled = false;
                return;
            }
            player.PresentationChanged += RefreshPlayerPresentation;
            player.StateChanged += OnPlayerStateChanged;
            RefreshPlayerPresentation(player);
            OnPlayerStateChanged(player);
            if (enemyRegistry == null && LevelContext.Current != null)
                enemyRegistry = LevelContext.Current.Enemies;
            if (enemyRegistry != null) enemyRegistry.RegisterExistingSceneEnemies();
            tipsUI.text = "";
            audioSource = GetComponent<AudioSource>();
            // New games do not enter the save restoration path, so the scene must
            // explicitly start with the loading overlay hidden.
            EndLoadingUI();
            //LoadReDestroyObject();
            // 读档由开始菜单或存档界面显式触发，关卡初始化不再重复加载场景。
        }

        // Update is called once per frame
        void Update()
        {
            if (uiInput.Input.Backpack.WasPressedThisFrame() && !colding)
            {
                ToggleBackpackUI();
                coldTime = StartCoroutine(ColdTime());
            }
            if (uiInput.Input.CloseItem.WasPressedThisFrame())
            {
                if (ItemUI.activeSelf)
                {
                    AudioManager.instance.PlaySound("Navigation", audioSource);
                    ItemUI.SetActive(false);
                    ItemName.gameObject.SetActive(false);
                    textAreaUI.SetActive(false);
                    Destroy(ShowPoint.GetChild(0).gameObject);
                    GamePause.Release(PauseReason.ItemInspect);
                    if (!GamePause.IsPaused)
                    {
                        player.playerInput.Enable();
                    }
                }
            }

        }
        private void ToggleBackpackUI()
        {
            if (backpackUI != null)
            {
                bool active = backpackUI.activeSelf;
                backpackUI.SetActive(!active);
                if (!active) // UI即将打开
                {
                    AudioManager.instance.PlaySound("Select", audioSource);
                    questUI.SetActive(true);
                    player.SetAnimaionSpeed(0);
                    player.playerInput.Disable();
                    GamePause.Acquire(PauseReason.Backpack);
                }
                else // UI即将关闭
                {
                    AudioManager.instance.PlaySound("Back", audioSource);
                    questUI.SetActive(false);
                    player.SetAnimaionSpeed(1);
                    ItemName.gameObject.SetActive(false);
                    GamePause.Release(PauseReason.Backpack);
                    if (!GamePause.IsPaused)
                    {
                        player.playerInput.Enable();
                    }
                }
            }
        }
        public void ShowTips(string tips, float time)
        {
            if (showTipsTime != null)
            {
                StopCoroutine(showTipsTime);
            }
            showTipsTime = StartCoroutine(ShowTipsTime(tips, time));
        }
        public void ShowItem(GameObject item, string name, string sound)
        {
            AudioManager.instance.PlaySound(sound, audioSource);
            player.playerInput.Disable();
            ItemUI.SetActive(true);
            ItemName.gameObject.SetActive(true);
            ItemName.text = name;
            GameObject obj = Instantiate(item, ShowPoint);
            obj.transform.parent = ShowPoint;
            GamePause.Acquire(PauseReason.ItemInspect);
        }
        public void ShowText(string text)
        {
            textAreaUI.SetActive(true);
            textArea.text = text;
        }
        public void ShowInteractUI(int index)
        {
            if (interactUI == null || index < 0 || index >= interactUI.Length || interactUI[index] == null)
            {
                return;
            }

            interactUI[index].SetActive(true);
        }
        public void HideInteractUI(int index)
        {
            if (interactUI != null && index >= 0 && index < interactUI.Length && interactUI[index] != null)
            {
                interactUI[index].SetActive(false);
            }

        }
        //public void DoSth(InputAction.CallbackContext context)
        //{
        //    if (context.performed)
        //    {
        //        ToggleBackpackUI();
        //        //coldTime = StartCoroutine(ColdTime());
        //    }
        //}
        private IEnumerator ShowTipsTime(string tips, float time)
        {
            tipsUI.gameObject.SetActive(true);
            tipsUI.text = tips;
            yield return new WaitForSecondsRealtime(time);
            tipsUI.gameObject.SetActive(false);
            tipsUI.text = "";
        }
        private IEnumerator ColdTime()
        {
            colding = true;
            yield return new WaitForSecondsRealtime(0.5f);
            colding = false;
        }
        public ResourceDirector Resources => resourceDirector != null ? resourceDirector : resourceDirector = GetComponent<ResourceDirector>();

        public bool TryRollResourceDrop(ResourceDropSource source, LootTable table, out LootResult result)
        {
            ResourceDirector director = Resources;
            if (director != null) return director.TryRoll(source, table, out result);
            result = default;
            Debug.LogError("GamePlay has no ResourceDirector.", this);
            return false;
        }

        public GameObject SpawnResourceDrop(ResourceDropSource source, LootTable table, Vector3 position, Transform parent = null)
        {
            if (!TryRollResourceDrop(source, table, out LootResult loot) || loot.prefab == null)
                return null;

            GameObject spawned = ObjectPool.Instance.GetObject(loot.prefab, position);
            if (spawned == null) return null;
            ItemBase pickup = spawned.GetComponentInChildren<ItemBase>();
            if (pickup != null) pickup.pickupAmount = loot.amount;
            if (parent != null) spawned.transform.SetParent(parent);
            return spawned;
        }
        public void SetIcon(Sprite icon)
        {
            if (wpIcon == null)
            {
                return;
            }

            var image = wpIcon.color;
            image.a = 1;
            wpIcon.color = image;
            wpIcon.sprite = icon;
        }
        public void CleanIcon()
        {
            if (wpIcon == null)
            {
                return;
            }

            wpIcon.sprite = null;
            var image = wpIcon.color;
            image.a = 0;
            wpIcon.color = image;
        }
        public void DieUIOpen()
        {
            if (dieUI == null || GamePause.Contains(PauseReason.PlayerDead))
            {
                return;
            }

            if (dieUITime != null)
            {
                StopCoroutine(dieUITime);
            }

            CanvasGroup canvasGroup = dieUI.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            GamePause.Acquire(PauseReason.PlayerDead);
            dieUI.SetActive(true);
            dieUITime = StartCoroutine(DieUITime());
        }
        public void DieUIClose()
        {
            if (dieUITime != null)
            {
                StopCoroutine(dieUITime);
                dieUITime = null;
            }

            if (dieUI != null)
            {
                CanvasGroup canvasGroup = dieUI.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
                dieUI.SetActive(false);
            }

            GamePause.Release(PauseReason.PlayerDead);
        }
        private IEnumerator DieUITime()
        {
            CanvasGroup canvasGroup = dieUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                dieUITime = null;
                yield break;
            }

            float duration = 1.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            dieUITime = null;
        }

        public void RegisterEnemy(EnemyBase enemy)
        {
            if (enemyRegistry != null) enemyRegistry.Register(enemy);
            else if (enemy != null && !legacyEnemies.Contains(enemy)) legacyEnemies.Add(enemy);
        }

        public void UnregisterEnemy(EnemyBase enemy)
        {
            if (enemyRegistry != null) enemyRegistry.Unregister(enemy);
            else if (enemy != null) legacyEnemies.Remove(enemy);
        }
        public void LoadReDestroyObject()
        {
            objs.Clear();
            GameObject[] obj = GameObject.FindGameObjectsWithTag("Interact");
            for(int i = 0; i < obj.Length; i++)
            {
                objs.Add(obj[i]);
            }
        }
        public void BeforeSceneReload()
        {
            if (objs != null) // 检查列表是否为 null
            {
                foreach (var obj in objs)
                {
                    if (obj != null) // 检查对象是否为 null
                    {
                        Destroy(obj);
                    }
                    destroyObj = StartCoroutine(DestroyObject());
                }
            }
        }
        public void StartLoadingUI()
        {
            if (loadingUI != null)
            {
                loadingUI.SetActive(true);
            }
        }
        public void EndLoadingUI()
        {
            if (loadingUI != null)
            {
                loadingUI.SetActive(false);
            }
        }
        private IEnumerator DestroyObject()
        {
            yield return new WaitForSecondsRealtime(0.1f);
        }

        protected override void OnDestroy()
        {
            if (player != null)
            {
                player.PresentationChanged -= RefreshPlayerPresentation;
                player.StateChanged -= OnPlayerStateChanged;
            }
            GamePause.Release(PauseReason.Backpack);
            GamePause.Release(PauseReason.ItemInspect);
            GamePause.Release(PauseReason.PlayerDead);
            uiInput?.Dispose();
            base.OnDestroy();
        }

        private void RefreshPlayerPresentation(PlayerControl source)
        {
            if (playerHp != null) playerHp.fillAmount = source.GetHp() / source.MaxHealth;
            if (knifeUse != null) knifeUse.fillAmount = source.MeleeWeapon != null && source.MeleeWeapon.bulletSize > 0
                ? source.currentUse / source.MeleeWeapon.bulletSize : 0f;
            if (playerKnife != null) playerKnife.SetActive(source.IsKnifeVisible);
            if (source.aimUI != null) source.aimUI.SetActive(source.IsAimVisible);
            if (source.qteUI != null)
            {
                source.qteUI.SetActive(source.IsQteVisible);
                Image image = source.qteUI.GetComponent<Image>();
                if (image != null)
                {
                    image.fillAmount = source.QteProgress;
                    image.color = source.QteColor;
                }
            }
            if (source.bulletCount != null) source.bulletCount.text = source.AmmoText;
            if (source.currentWp != null && source.currentWp.wpSimpleIcon != null) SetIcon(source.currentWp.wpSimpleIcon);
            else CleanIcon();
        }

        private void OnPlayerStateChanged(PlayerControl source)
        {
            if (source.currentState == PlayerState.Dead) DieUIOpen();
            else DieUIClose();
        }
    }
}
