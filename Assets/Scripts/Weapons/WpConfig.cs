using FirstGearGames.SmoothCameraShaker;
using Astraparasitus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Weapons", menuName = "Weapon Config")]
public class WpConfig : ScriptableObject, IGameContent
{
    [SerializeField, HideInInspector] private string stableId;
    public string StableId => stableId;

    [Header("基础信息")]
    public int ID;
    public string Name;
    [TextArea(2, 4)] public string description;
    public WeaponType weaponType;

    [Header("表现资源")]
    [Tooltip("关闭后，背包中的丢弃按钮不可用。用于损坏状态或剧情专用武器。")]
    public bool canDrop = true;
    [Tooltip("场景中可拾取、丢弃的武器 Prefab。")]
    public GameObject pickupPrefab;
    [Tooltip("拾取武器时在查看界面展示的 Prefab。")]
    public GameObject weaponPrefab;
    [Tooltip("背包中的大图标。")]
    public Sprite wpIcon;
    [Tooltip("HUD 当前武器图标。")]
    public Sprite wpSimpleIcon;

    [Header("战斗参数")]
    [Min(0f)]
    public float damage;
    [Min(0f)]
    public float stunRate;
    [Min(0f)]
    public float fireRate;
    [Min(0f)]
    public float range;
    [Min(0f)]
    public float reloadTime;

    [Header("弹药与弹道")]
    [Tooltip("远程武器消耗的物品配置。配置后会自动同步旧版 Bullet ID。")]
    public ItemConfig ammoItem;
    [HideInInspector]
    public int bulletId;
    [Min(0f)]
    public float bulletSpeed;
    [Min(0)]
    public int bulletSize;
    [Min(0)]
    public int bulletTotalNum;
    [Min(0f)]
    public float bulletSpread;

    [Header("弹丸与特效资源")]
    public GameObject bulletPrefab;
    public GameObject muzzleFirePrefab;
    public GameObject impactEffectPrefab;
    public GameObject[] hitEffectPrefab;
    public GameObject[] hitImpacts;

    [Header("动画状态")]
    public string toIdle;
    public string IdleAnim;
    public string onHandAnim;
    public string AimAnim;
    public string AimCloseAnim;
    public string reloadAnim;
    public ShakeData shakeData;

    [Header("音效事件")]
    public string shootSound;
    public string reloadSound;
    public string emptySound;

    public bool UsesAmmo => weaponType != WeaponType.Knife;
    public int AmmoItemId => ammoItem != null ? ammoItem.id : bulletId;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(stableId))
        {
            stableId = GameContentId.Create();
        }

        damage = Mathf.Max(0f, damage);
        fireRate = Mathf.Max(0f, fireRate);
        range = Mathf.Max(0f, range);
        reloadTime = Mathf.Max(0f, reloadTime);
        bulletSpeed = Mathf.Max(0f, bulletSpeed);
        bulletSize = Mathf.Max(0, bulletSize);
        bulletTotalNum = Mathf.Max(0, bulletTotalNum);
        bulletSpread = Mathf.Max(0f, bulletSpread);
        if (ammoItem != null)
        {
            bulletId = ammoItem.id;
        }
        else if (!UsesAmmo)
        {
            bulletId = -1;
        }
    }
}
public enum WeaponType
{
    Knife = 0,
    Pistol = 1,
    Rifle = 2,
    Shotgun = 3,
    Heavy = 4,
}
