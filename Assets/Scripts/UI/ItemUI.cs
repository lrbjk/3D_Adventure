using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Astraparasitus
{
    public class ItemUI : MonoBehaviour
    {
        public Image icon;
        public Text nameText;
        public Text countText;
        public GameObject subMenu;
        [Header("Buttons")]
        private Button mainButton;
        public Button useButton;
        public Button combineButton;
        public Button transferButton;
        public Button dropButton;
        private WpConfig wpConfig;
        private Item? item;
        private ItemConfig itemConfig;
        // Start is called before the first frame update
        void Start()
        {
            subMenu.SetActive(false);
            mainButton = GetComponent<Button>();
            mainButton.onClick.AddListener(OpenMenu);
        }

        // Update is called once per frame
        void Update()
        {

        }
        public void SetImage(Sprite sprite)
        {
            icon.sprite = sprite;
        }
        public void SetName(string name)
        {
            nameText.text = name;
        }
        public void SetCount(int count)
        {
            countText.text = count.ToString();
        }
        public void SetWeapon(WpConfig wp)
        {
            wpConfig = wp;
            useButton.interactable = false;
            combineButton.interactable = false;
            transferButton.interactable = false;
            dropButton.interactable = wp != null && wp.canDrop;
            dropButton.onClick.AddListener(DropItem);
        }
        public void SetItem(Item item)
        {
            SetItem(item, null);
        }

        public void SetItem(Item item, ItemConfig config)
        {
            this.item = item;
            itemConfig = config;
            bool canUse = config != null ? config.IsUsable : item.isUseable;
            if (canUse)
            {
                useButton.interactable = true;
                useButton.onClick.AddListener(UseItem);
            }
            else
            {
                useButton.interactable = false;
            }
            bool canCombine = config != null ? config.isCombinable : item.isCombinable;
            if (canCombine)
            {
                combineButton.interactable = true;
                combineButton.onClick.AddListener(CombineItem);
            }
            else
            {
                combineButton.interactable = false;
            }
            transferButton.interactable = false;
            dropButton.onClick.AddListener(DropItem);
        }
        public void OpenMenu()
        {
            if (subMenu.activeSelf)
            {
                subMenu.SetActive(false);
            }
            else
            {
                subMenu.SetActive(true);
            }
        }
        public void DropItem()
        {
            Vector3 point = GamePlay.Instance.player.transform.position + GamePlay.Instance.player.model.transform.forward * 1.1f;
            if (wpConfig != null)
            {
                if (!wpConfig.canDrop)
                {
                    return;
                }

                GamePlay.Instance.player.RemoveWeapon(wpConfig.ID);
                if (wpConfig.pickupPrefab != null)
                {
                    Instantiate(wpConfig.pickupPrefab, point, wpConfig.pickupPrefab.transform.rotation);
                }
                else
                {
                    BackPack.Instance.GiveItemPrefab(wpConfig.ID + 100, point);
                }
            }
            else if (item != null)
            {
                if (BackPack.Instance.GiveItemPrefab(item.Value.id, point, 1))
                    BackPack.Instance.RemoveItem(item.Value.id, 1);
            }
            
            BackPackUI.Instance.Refresh();
        }
        public void UseItem()
        {
            if (item == null)
            {
                return;
            }

            if (itemConfig != null)
            {
                if (itemConfig.TryApplyUseEffect(GamePlay.Instance.player))
                {
                    BackPack.Instance.RemoveItem(item.Value.id, 1);
                }
                BackPackUI.Instance.Refresh();
                return;
            }

            if (this.item.Value.id >= 20 && this.item.Value.id <= 25)
            {
                if (GamePlay.Instance.player.GetHp() < GamePlay.Instance.player.MaxHealth)
                {
                    switch (this.item.Value.id)
                    {
                        case 20:
                            GamePlay.Instance.player.DecreaseHp(-100);
                            BackPack.Instance.RemoveItem(item.Value.id, 1);
                            break;
                        case 21:
                            GamePlay.Instance.player.DecreaseHp(-50);
                            BackPack.Instance.RemoveItem(item.Value.id, 1);
                            break;
                        case 23:
                            GamePlay.Instance.player.DecreaseHp(-100);
                            BackPack.Instance.RemoveItem(item.Value.id, 1);
                            break;
                    }
                }
            }
            BackPackUI.Instance.Refresh();
        }
        public void CombineItem()
        {
            if (item == null)
            {
                return;
            }

            if (itemConfig != null && itemConfig.combineWith != null && itemConfig.combineResult != null)
            {
                if (BackPack.Instance.CheckItem(itemConfig.combineWith.id))
                {
                    BackPack.Instance.RemoveItem(item.Value.id, 1);
                    BackPack.Instance.RemoveItem(itemConfig.combineWith.id, 1);
                    BackPack.Instance.AddItem(itemConfig.combineResult);
                    BackPackUI.Instance.Refresh();
                }
                return;
            }

            if (BackPack.Instance.CheckItem(item.Value.combineId))
            {
                BackPack.Instance.RemoveItem(item.Value.id, 1);
                BackPack.Instance.RemoveItem(item.Value.combineId, 1);
                BackPack.Instance.AddItem(item.Value.combineObject.item);
                BackPackUI.Instance.Refresh();
            }
        }
    }
}
