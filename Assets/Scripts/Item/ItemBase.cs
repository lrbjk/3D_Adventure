using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Astraparasitus
{
    public enum ItemType
    {
        Break,
        Open,
        Pickup,
    }
    public enum InteractionType
    {
        Door,
        Box,
        Item,
        None,
    }
    [RequireComponent(typeof(AudioSource))]
    public class ItemBase : MonoBehaviour, Iinteractable
    {
        [Header("基本属性")]
        public ItemType itemType;
        public ItemConfig itemConfig;
        [Min(0), Tooltip("本次拾取的数量；0 表示使用物品配置的默认数量。")]
        public int pickupAmount;
        public AudioSource audioSource;
        public string interactSound;
        [Header("UI")]
        public InteractionType interactionType;
        public GameObject itemInfo;
        public Text itemName;
        public Material highlightMaterial;
        // Start is called before the first frame update
        void Start()
        {
            if (itemConfig != null)
            {
                itemType = itemConfig.itemType;
            }
            if(highlightMaterial!= null)
            {
                highlightMaterial.SetFloat("_Scale", 0f);
            }
            audioSource = GetComponent<AudioSource>();
        }

        // Update is called once per frame
        void Update()
        {

        }
        public virtual void HideInfo()
        {
            if (highlightMaterial != null)
            {
                highlightMaterial.SetFloat("_Scale", 0f);
            }
        }

        public virtual void Interact()
        {
            if (itemConfig == null)
            {
                Debug.LogError("Pickup has no ItemConfig.", this);
                return;
            }

            GameObject obj = itemConfig.inspectPrefab;
            if (obj == null && !string.IsNullOrWhiteSpace(itemConfig.item.itemObject))
            {
                obj = Resources.Load<GameObject>(itemConfig.item.itemObject);
            }
            if (obj != null)
                GamePlay.Instance.ShowItem(obj, itemConfig.itemName, interactSound);
            BackPack.Instance.AddItem(itemConfig, pickupAmount);
            ObjectPool.Instance.PushObject(gameObject);
        }

        public virtual void ShowInfo()
        {
            if(highlightMaterial!= null)
            {
                highlightMaterial.SetFloat("_Scale", 1.05f);
            }
        }
        private void OnDisable()
        {
            GamePlay gamePlay = GamePlay.Instance;
            // 卸载旧场景时 Instance 可能已经指向新场景，旧物品不能再修改新场景 UI。
            if (gamePlay != null && gamePlay.gameObject.scene == gameObject.scene)
            {
                switch (interactionType)
                {
                    case InteractionType.Door:
                        gamePlay.HideInteractUI(0);
                        break;
                    case InteractionType.Box:
                        gamePlay.HideInteractUI(1);
                        break;
                    case InteractionType.Item:
                        gamePlay.HideInteractUI(2);
                        break;
                }
            }

            if(highlightMaterial!= null)
            {
                highlightMaterial.SetFloat("_Scale", 0f);
            }
        }
    }
}
