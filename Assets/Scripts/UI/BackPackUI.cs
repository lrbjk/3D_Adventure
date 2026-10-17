using UnityEngine;
namespace Astraparasitus
{
    public class BackPackUI : SingletonMono<BackPackUI>
    {
        public Transform weaponSlot;
        public Transform itemSlot;
        public GameObject itemIcon;
        // Start is called before the first frame update
        private void OnEnable()
        {
            Refresh();
        }
        private void OnDisable()
        {
            foreach (Transform child in weaponSlot)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in itemSlot)
            {
                Destroy(child.gameObject);
            }
        }
        public void Close()
        {
            //GamePlay.Instance.CloseBackpackUI();
        }
        public void Refresh()
        {
            foreach (Transform child in weaponSlot)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in itemSlot)
            {
                Destroy(child.gameObject);
            }

            foreach (var wp in GamePlay.Instance.player.wpConfigs)
            {
                GameObject icon = Instantiate(itemIcon, weaponSlot);
                if (wp.wpIcon != null)
                    icon.GetComponent<ItemUI>().SetImage(wp.wpIcon);
                icon.GetComponent<ItemUI>().SetName(wp.Name);
                icon.GetComponent<ItemUI>().SetCount(GamePlay.Instance.player.GetWeaponAmmo(wp, wp.bulletTotalNum));
                icon.GetComponent<ItemUI>().SetWeapon(wp);
            }
            foreach (var item in BackPack.Instance.items)
            {
                GameObject icon = Instantiate(itemIcon, itemSlot);
                BackPack.Instance.TryGetItemConfig(item.Key, out ItemConfig config);
                Sprite sprite = config != null ? config.icon : Resources.Load<Sprite>(item.Value.itemIcon);
                if (sprite != null)
                    icon.GetComponent<ItemUI>().SetImage(sprite);
                icon.GetComponent<ItemUI>().SetName(config != null ? config.itemName : item.Value.itemName);
                icon.GetComponent<ItemUI>().SetCount(item.Value.itemNum);
                icon.GetComponent<ItemUI>().SetItem(item.Value, config);
            }
        }
        public void BackToMenu()
        {
#if UNITY_EDITOR
            // 在编辑器模式下停止运行
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // 在打包后的应用中退出程序
            Application.Quit();
#endif
        }
    }
}
