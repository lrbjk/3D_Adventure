using System;
using System.Collections.Generic;

namespace Astraparasitus
{
    [Serializable]
    public struct WeaponAmmoData
    {
        public string weaponId;
        public int legacyWeaponId;
        public int ammo;
    }

    [Serializable]
    public class PlayerRuntimeState
    {
        public float health;
        public float defence;
        public float knifeUse;
        public List<WeaponAmmoData> weaponAmmo = new List<WeaponAmmoData>();
    }

    [Serializable]
    public struct InventoryItemData
    {
        public int id;
        public string itemId;
        public int amount;

        // 兼容版本 3 及更早的存档。新存档只保留其中的纯值字段。
        public Item item;
    }

    [Serializable]
    public class InventoryState
    {
        public List<InventoryItemData> items = new List<InventoryItemData>();
    }
}
