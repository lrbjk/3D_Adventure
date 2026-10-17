using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astraparasitus
{
    [Serializable]
    public struct InteractObj
    {
        public string id;
        public string name;
        public bool isActive;
    }
    public class SaveManager : SingletonMono<SaveManager>
    {
        [SerializeField, Tooltip("用于通过稳定 ID 恢复武器配置。建议放在 Resources/GameContentCatalog。")]
        private GameContentCatalog contentCatalog;
        private Coroutine saveCoroutine;
        public const int recordNum = 3;
        public const string NAME = "SaveData";
        private const int CurrentSaveVersion = 3;

        public string[] recordName = new string[recordNum];
        public int lastID;

        public List<InteractObj> interactObjs = new List<InteractObj>();
        public List<InteractObj> destroyObjs = new List<InteractObj>();
        private PlayerAtributeData currentData;
        protected override void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            Load();
            if (contentCatalog == null)
            {
                contentCatalog = Resources.Load<GameContentCatalog>("GameContentCatalog");
            }
        }
        private void Start()
        {
            RefreshObjs();
        }
        public void RefreshObjs()
        {
            interactObjs.Clear();
            destroyObjs.Clear();
            GameObject[] objs = GameObject.FindGameObjectsWithTag("Interact");
            for (int i = 0; i < objs.Length; i++)
            {
                var obj = new InteractObj();
                obj.id = GetSaveableId(objs[i]);
                obj.name = objs[i].name;
                obj.isActive = objs[i].activeSelf;
                interactObjs.Add(obj);
            }
            GameObject[] dobjs = GameObject.FindGameObjectsWithTag("Destructible");
            for(int i = 0; i < dobjs.Length; i++)
            {
                var obj = new InteractObj();
                obj.id = GetSaveableId(dobjs[i]);
                obj.name = dobjs[i].name;
                obj.isActive = dobjs[i].activeSelf;
                destroyObjs.Add(obj);
            }
        }
        [Serializable]
        class SaveData
        {
            public string[] recordName = new string[recordNum];
            public int lastID;
        }
        [Serializable]
        class PlayerAtributeData
        {
            public int version = CurrentSaveVersion;
            public float health;
            public float defence;
            public PlayerRuntimeState playerState;
            public List<string> weaponIds = new List<string>();
            public string currentWeaponId;
            public List<WeaponAmmoData> weaponAmmo = new List<WeaponAmmoData>();
            // 旧字段仅用于兼容已有存档。
            public List<WpConfig> wpConfigs = new List<WpConfig>();
            public WpConfig currentWp;
            public float currentUse;
            public Vector3 position;

            public int level;
            //物品
            public string items;
            public InventoryState inventory;
            public ResourceDirectorState resourceDirector;
            //场景物品状态
            public List<InteractObj> isActive = new List<InteractObj>();
            public List<InteractObj> destroyState = new List<InteractObj>();
        }
        #region 存档储存
        SaveData ForSave()
        {
            var savedata = new SaveData();
            for(int i = 0; i<recordNum; i++)
            {
                savedata.recordName[i] = recordName[i];
            }
            savedata.lastID = lastID;
            return savedata;
        }
        void ForLoad(SaveData saveData)
        {
            lastID = Mathf.Clamp(saveData.lastID, 0, recordNum - 1);
            if (saveData.recordName == null)
            {
                return;
            }

            for(int i = 0; i < recordNum && i < saveData.recordName.Length; i++)
            {
                recordName[i] = saveData.recordName[i];
            }
        }
        public void Save()
        {
            SAVE.JsonSave(NAME, ForSave());
        }
        public void Load()
        {
            var json = SAVE.JsonLoad<SaveData>(NAME);
            if (json == null)
                return;
            ForLoad(json);
        }
        #endregion
        #region 玩家基本信息储存
        PlayerAtributeData ForPlayerSave()
        {
            var playerData = new PlayerAtributeData();
            playerData.playerState = GamePlay.Instance.player.CaptureRuntimeState();
            playerData.health = GamePlay.Instance.player.GetCurrentHp();
            playerData.defence = GamePlay.Instance.player.GetCurrentDefence();
            playerData.wpConfigs = GamePlay.Instance.player.wpConfigs;
            playerData.currentWp = GamePlay.Instance.player.currentWp;
            foreach (WpConfig weapon in GamePlay.Instance.player.wpConfigs)
            {
                if (weapon != null)
                {
                    playerData.weaponIds.Add(weapon.StableId);
                }
            }
            playerData.currentWeaponId = GamePlay.Instance.player.currentWp != null
                ? GamePlay.Instance.player.currentWp.StableId
                : string.Empty;
            playerData.weaponAmmo = GamePlay.Instance.player.CaptureWeaponAmmo();
            playerData.currentUse = GamePlay.Instance.player.currentUse;
            playerData.position = GamePlay.Instance.player.transform.position;
            playerData.level = SceneManager.GetActiveScene().buildIndex;
            // 新存档只写纯数据 InventoryState；items 仅保留给旧存档读取。
            playerData.items = string.Empty;
            playerData.inventory = BackPack.Instance.CaptureState();
            playerData.resourceDirector = GamePlay.Instance.Resources?.CaptureState();
            GameObject[] objs = GameObject.FindGameObjectsWithTag("Interact");
            foreach (var interactObj in interactObjs)
            {
                var inObj = new InteractObj();
                inObj.id = interactObj.id;
                inObj.name = interactObj.name;
                inObj.isActive = false;
                foreach(var obj in objs)
                {
                    if (IsSameSaveable(obj, interactObj))
                    {
                        inObj.isActive = true;
                        break;
                    }
                }
                playerData.isActive.Add(inObj);
            }
            GameObject[] dobjs = GameObject.FindGameObjectsWithTag("Destructible");
            foreach (var destroyObj in destroyObjs)
            {
                var inObj = new InteractObj();
                inObj.id = destroyObj.id;
                inObj.name = destroyObj.name;
                inObj.isActive = false;
                foreach (var obj in dobjs)
                {
                    if (IsSameSaveable(obj, destroyObj))
                    {
                        inObj.isActive = true;
                        break;
                    }
                }
                playerData.destroyState.Add(inObj);
            }
            
            return playerData;
        }
        bool isSwitchingScene = false;

        void ForLoadPlayerData(PlayerAtributeData playerData)
        {
            if (playerData == null || isSwitchingScene)
            {
                return;
            }

            if (!SceneLoader.CanLoad(playerData.level))
            {
                Debug.LogError($"Save references an unavailable scene: {playerData.level}");
                return;
            }

            isSwitchingScene = true;
            GamePlay.Instance?.StartLoadingUI();
            GamePlay.Instance?.BeforeSceneReload();
            currentData = playerData;

            if (!SceneLoader.LoadAsync(playerData.level, RefreshItems))
            {
                isSwitchingScene = false;
                currentData = null;
                GamePlay.Instance?.EndLoadingUI();
            }
            
        }

        public void RefreshItems(AsyncOperation obj)
        {
            StartCoroutine(RestoreAfterSceneReady());
        }

        private IEnumerator RestoreAfterSceneReady()
        {
            // AsyncOperation.completed can run before Start on objects in the new scene.
            // Wait until the new scene has created and bound its player and coordinators.
            const int maxWaitFrames = 300;
            int waitFrames = 0;
            yield return null;
            while (currentData != null &&
                   (GamePlay.Instance == null || GamePlay.Instance.player == null || BackPack.Instance == null) &&
                   waitFrames < maxWaitFrames)
            {
                waitFrames++;
                yield return null;
            }

            if (currentData == null || GamePlay.Instance == null || GamePlay.Instance.player == null || BackPack.Instance == null)
            {
                Debug.LogError("Loaded scene did not initialize GamePlay, PlayerControl, or BackPack in time; save restoration was cancelled.");
                GamePlay.Instance?.EndLoadingUI();
                currentData = null;
                isSwitchingScene = false;
                yield break;
            }

            GamePlay.Instance.StartLoadingUI();
            try
            {
                GameObject[] objs = GameObject.FindGameObjectsWithTag("Interact");
                foreach (GameObject objItem in objs)
                {
                    foreach (InteractObj interactObj in currentData.isActive ?? new List<InteractObj>())
                    {
                        if (IsSameSaveable(objItem, interactObj))
                        {
                            objItem.SetActive(interactObj.isActive);
                            break; // 找到匹配项后跳出循环
                        }
                    }
                }
                GameObject[] dobjs = GameObject.FindGameObjectsWithTag("Destructible");
                foreach (GameObject objItem in dobjs)
                {
                    foreach (InteractObj interactObj in currentData.destroyState ?? new List<InteractObj>())
                    {
                        if (IsSameSaveable(objItem, interactObj))
                        {
                            objItem.SetActive(interactObj.isActive);
                            break; // 找到匹配项后跳出循环
                        }
                    }
                }
                PlayerRuntimeState playerState = currentData.playerState ?? new PlayerRuntimeState
                {
                    health = currentData.health,
                    defence = currentData.defence,
                    knifeUse = currentData.currentUse,
                    weaponAmmo = currentData.weaponAmmo
                };
                List<WpConfig> weapons = ResolveWeapons(currentData);
                WpConfig equippedWeapon = ResolveCurrentWeapon(currentData, weapons);
                if (currentData.inventory != null)
                {
                    BackPack.Instance.RestoreState(currentData.inventory);
                }
                else
                {
                    Dictionary<int, Item> legacyItems = string.IsNullOrWhiteSpace(currentData.items)
                        ? new Dictionary<int, Item>()
                        : JsonConvert.DeserializeObject<Dictionary<int, Item>>(currentData.items) ?? new Dictionary<int, Item>();
                    BackPack.Instance.RestoreLegacyItems(legacyItems);
                }
                GamePlay.Instance.Resources?.RestoreState(currentData.resourceDirector);
                GamePlay.Instance.player.RestoreFromSave(playerState, weapons, equippedWeapon, currentData.position);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Save restoration failed: {exception.Message}");
            }
            finally
            {
                GamePlay.Instance?.EndLoadingUI();
                currentData = null;
                isSwitchingScene = false;
            }
        }
        public void SavePlayerData(int id)
        {
            if (id < 0 || id >= recordNum || string.IsNullOrWhiteSpace(recordName[id]))
            {
                Debug.LogWarning($"Invalid save slot: {id}");
                return;
            }

            SAVE.JsonSave(recordName[id], ForPlayerSave());
            //GamePlay.Instance.autoSaveUI.SetActive(false);
        }
        public void LoadPlayerData(int id)
        {
            TryLoadPlayerData(id);
        }

        private bool TryLoadPlayerData(int id)
        {
            if (id < 0 || id >= recordNum || string.IsNullOrWhiteSpace(recordName[id]))
            {
                Debug.LogWarning($"Invalid save slot: {id}");
                return false;
            }

            var saveData = SAVE.JsonLoad<PlayerAtributeData>(recordName[id]);
            if (saveData == null)
            {
                Debug.LogWarning($"Save data is missing or invalid: {recordName[id]}");
                return false;
            }

            if (saveData.version <= 0 || saveData.version > CurrentSaveVersion)
            {
                Debug.LogWarning($"Unsupported save version: {saveData.version}");
                return false;
            }

            ForLoadPlayerData(saveData);
            return isSwitchingScene;
        }
        #endregion
        public void DeleteRecord(int id)
        {
            if (id < 0 || id >= recordNum)
                return;
            SAVE.JsonDelete(recordName[id]);
        }
        public void SavingTest()
        {
            if (recordName[0] != "")
            {
                DeleteRecord(0);
            }   
            recordName[0] = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString()+".save";
            lastID = 0;
            Save();
            SavePlayerData(0);
        }
        public void LoadingTest()
        {
            TryAutoLoading();
        }
        public void AutoSave()
        {
            if (GamePlay.Instance != null && GamePlay.Instance.autoSaveUI != null)
            {
                GamePlay.Instance.autoSaveUI.SetActive(true);
            }
            SavingTest();
        }
        public void AutoLoading()
        {
            TryAutoLoading();
        }

        public bool TryAutoLoading()
        {
            if (lastID < 0 || lastID >= recordNum)
            {
                Debug.LogWarning($"Invalid last save slot: {lastID}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(recordName[lastID]))
            {
                Debug.LogWarning($"Save slot {lastID} is empty.");
                return false;
            }

            if (!TryLoadPlayerData(lastID))
            {
                return false;
            }

            Save();
            return true;
        }
        private void Update()
        {
            //if (Input.GetKeyDown(KeyCode.M))
            //{
            //    SavingTest();
            //}
            //if (Input.GetKeyDown(KeyCode.N))
            //{
            //    LoadingTest();
            //}
            if (GamePlay.Instance != null && GamePlay.Instance.autoSaveUI != null && GamePlay.Instance.autoSaveUI.activeSelf)
            {
                if (saveCoroutine == null)
                {
                    saveCoroutine = StartCoroutine(savingUI());
                }
            }
        }
        private IEnumerator savingUI()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (GamePlay.Instance != null && GamePlay.Instance.autoSaveUI != null)
            {
                GamePlay.Instance.autoSaveUI.SetActive(false);
            }
            saveCoroutine = null;
        }

        private static string GetSaveableId(GameObject obj)
        {
            SaveableId saveableId = obj != null ? obj.GetComponent<SaveableId>() : null;
            return saveableId != null ? saveableId.Id : string.Empty;
        }

        private static bool IsSameSaveable(GameObject obj, InteractObj saved)
        {
            if (obj == null)
            {
                return false;
            }

            string objectId = GetSaveableId(obj);
            if (!string.IsNullOrWhiteSpace(saved.id) && !string.IsNullOrWhiteSpace(objectId))
            {
                return saved.id == objectId;
            }

            return obj.name == saved.name;
        }

        private List<WpConfig> ResolveWeapons(PlayerAtributeData data)
        {
            List<WpConfig> resolved = new List<WpConfig>();
            if (contentCatalog != null && data.weaponIds != null)
            {
                foreach (string weaponId in data.weaponIds)
                {
                    if (!string.IsNullOrWhiteSpace(weaponId) && contentCatalog.TryGetWeapon(weaponId, out WpConfig weapon))
                    {
                        resolved.Add(weapon);
                    }
                }
            }

            if (resolved.Count == 0 && data.wpConfigs != null)
            {
                resolved.AddRange(data.wpConfigs);
            }

            return resolved;
        }

        private WpConfig ResolveCurrentWeapon(PlayerAtributeData data, List<WpConfig> weapons)
        {
            if (contentCatalog != null && !string.IsNullOrWhiteSpace(data.currentWeaponId) &&
                contentCatalog.TryGetWeapon(data.currentWeaponId, out WpConfig currentWeapon))
            {
                return currentWeapon;
            }

            if (data.currentWp != null)
            {
                return data.currentWp;
            }

            return weapons.Count > 0 ? weapons[0] : null;
        }
    }
}
