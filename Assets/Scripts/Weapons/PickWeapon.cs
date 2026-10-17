using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Astraparasitus
{
    public class PickWeapon : ItemBase, Iinteractable
    {
        public WpConfig weapon;
        //public override void HideInfo()
        //{
        //    if (itemInfo == null) return;
        //    itemInfo.SetActive(false);
        //}

        public override void Interact()
        {
            GamePlay.Instance.player.SetWeaponAmmo(weapon, 0);
            if (weapon.weaponPrefab != null)
                GamePlay.Instance.ShowItem(weapon.weaponPrefab, weapon.Name, interactSound);
            GamePlay.Instance.player.AddWeapon(weapon);
            ObjectPool.Instance.PushObject(gameObject);
        }

        //public override void ShowInfo()
        //{
        //    if (itemInfo == null) return;
        //    itemInfo.SetActive(true);
        //    itemName.text = weapon.Name;
        //}

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
