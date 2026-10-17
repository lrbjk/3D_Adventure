using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class WeaponControl : MonoBehaviour
    {
        public Transform shootPoint;
        public Transform impactEffect;
        public PlayerControl playerControl;
        public WpConfig weapon;
        public Animator animator;
        public Transform aim;
        private float shootDelay = 0f;
        private bool canShoot = true;
        public float currentSpread;
        private float maxSpread;
        public float addForce;
        public Animator aimAnimator;
        private int currentBulletNum;
        private Coroutine coroutine;
        private bool isShoot;
        public int ammoIndex;
        private int listIndex;
        public AudioSource audioSource;
        public int wpIndex;
        // Start is called before the first frame update
        void Start()
        {
            //if(playerControl.playerConfig.wps.Count > wpIndex)
            //{
            //    weapon = playerControl.playerConfig.wps[wpIndex];
            //}
            //maxSpread = weapon.bulletSpread;
            //currentSpread = maxSpread;
            //currentBulletNum = weapon.bulletTotalNum;
            //listIndex = playerControl.playerConfig.items.FindIndex(x => x.id == ammoIndex);
            //if (listIndex != -1)
            //{
            //    if (playerControl.playerConfig.items[listIndex].Item.itemNum < currentBulletNum)
            //    {
            //        currentBulletNum = playerControl.playerConfig.items[listIndex].Item.itemNum;
            //    }
            //    BackPack.Instance.RemoveItem(ammoIndex, currentBulletNum);
            //    //playerControl.bulletCount.text = currentBulletNum + " / " + playerControl.playerConfig.items[listIndex].Item.itemNum;
            //}
        }
        private void OnEnable()
        {
            if (weapon == null && playerControl.wpConfigs.Count > wpIndex)
            {
                weapon = playerControl.wpConfigs[wpIndex];
            }
            if (weapon == null)
            {
                enabled = false;
                return;
            }
            maxSpread = weapon.bulletSpread;
            currentSpread = maxSpread;
            currentBulletNum = playerControl.GetWeaponAmmo(weapon, weapon.bulletTotalNum);
            int ammoItemId = GetAmmoItemId();
            listIndex = BackPack.Instance != null && ammoItemId >= 0 && BackPack.Instance.CheckItem(ammoItemId) ? 0 : -1;
        }
        // Update is called once per frame
        void Update()
        {
            CheckAmmo();


            if (currentBulletNum <= 0)
            {
                //canShoot = false;
                animator.SetBool("NoAmmo", true);
            }
            else
            {
                animator.SetBool("NoAmmo", false);
            }
            if (playerControl.playerInput.input.Reload.triggered && currentBulletNum < weapon.bulletSize && playerControl.currentState != PlayerState.Parry)
            {
                if (playerControl.currentState == PlayerState.TC)
                {
                    return;
                }
                int ammoItemId = GetAmmoItemId();
                listIndex = BackPack.Instance != null && ammoItemId >= 0 && BackPack.Instance.CheckItem(ammoItemId) ? 0 : -1;
                if (listIndex == -1)
                {
                    return;
                }
                playerControl.ChangeState(PlayerState.Reload);
                AudioManager.instance.PlaySound(weapon.reloadSound, audioSource);
                animator.SetTrigger("Reload");
            }

            if (!canShoot)
            {
                if (shootDelay >= weapon.fireRate)
                {
                    canShoot = true;
                    shootDelay = 0f;
                }
                else
                {
                    shootDelay += Time.deltaTime;
                }
            }

            if (playerControl.currentState == PlayerState.Idle)
            {
                aimAnimator.SetFloat("size", 0.2f);
                currentSpread = maxSpread * 0.2f;
            }
            else if (playerControl.currentState == PlayerState.Move)
            {
                aimAnimator.SetFloat("size", 1f);
                currentSpread = maxSpread;
            }
            else if (playerControl.currentState == PlayerState.Aim && !isShoot)
            {
                if (playerControl.cameraInput.Camera.CameraLook.ReadValue<Vector2>().magnitude < 0.5f)
                {
                    //coroutine = StartCoroutine(Delay(1));
                    aimAnimator.SetFloat("size", 0f);
                    currentSpread = maxSpread * 0f;
                }
                else
                {
                    // StopCoroutine(coroutine);
                    aimAnimator.SetFloat("size", 0.5f);
                    currentSpread = maxSpread * 0.5f;
                }

            }
            else if (playerControl.currentState == PlayerState.AimMove)
            {
                aimAnimator.SetFloat("size", 0.7f);
                currentSpread = maxSpread * 0.7f;
            }
            else if (isShoot)
            {
                aimAnimator.SetFloat("size", 0.5f);
                currentSpread = maxSpread * 0.5f;
            }
        }
        private void LateUpdate()
        {
            if (playerControl.playerInput.input.Shoot.triggered && playerControl.playerInput.input.Aim.IsPressed() && canShoot)
            {
                if (playerControl.currentState == PlayerState.TC)
                {
                    return;
                }
                if (currentBulletNum <= 0)
                {
                    AudioManager.instance.PlaySound(weapon.emptySound, audioSource);
                    return;
                }
                canShoot = false;
                Shooting();
                playerControl.model.animator.SetTrigger("Shoot");
                animator.SetTrigger("Shoot");

            }
        }
        public void Shooting()
        {
            AudioManager.instance.PlaySound(weapon.shootSound, audioSource);
            isShoot = true;
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            coroutine = StartCoroutine(Delay(1.5f));
            currentBulletNum--;
            //animator.SetBool("Shoot", true);
            CameraShakerHandler.Shake(weapon.shakeData);
            GameObject muzzleFire = ObjectPool.Instance.GetObject(weapon.muzzleFirePrefab, shootPoint.position, shootPoint.rotation);
            muzzleFire.transform.SetParent(shootPoint);
            muzzleFire.GetComponent<ParticleSystem>().Play();
            GameObject bulletImpact = ObjectPool.Instance.GetObject(weapon.impactEffectPrefab, impactEffect.position, impactEffect.rotation);
            bulletImpact.transform.SetParent(impactEffect);
            bulletImpact.GetComponent<ParticleSystem>().Play();
            Vector3 directionToAim = (aim.position - shootPoint.position).normalized;
            Vector3 randomOffset = Random.insideUnitSphere * currentSpread;
            Vector3 finalDirection = (directionToAim + randomOffset).normalized;
            //Vector3 bulletSpawnPosition = shootPoint.parent.TransformPoint(shootPoint.localPosition);
            GameObject bullet = ObjectPool.Instance.GetObject(playerControl.currentWp.bulletPrefab, shootPoint.position, Quaternion.identity);
            bullet.GetComponent<Bullet>().SetupBullet(shootPoint.position, finalDirection, weapon.bulletSpeed, weapon.damage, weapon.stunRate, addForce, weapon.hitEffectPrefab, weapon.hitImpacts);
        }
        public void Reload()
        {
            
            int ammoItemId = GetAmmoItemId();
            listIndex = BackPack.Instance != null && ammoItemId >= 0 && BackPack.Instance.CheckItem(ammoItemId) ? 0 : -1;
            int num = weapon.bulletSize - currentBulletNum;
            if (listIndex != -1)
            {
                int reloadAmount = Mathf.Min(num, BackPack.Instance.GetItemAmount(ammoItemId));
                currentBulletNum += reloadAmount;
                BackPack.Instance.RemoveItem(ammoItemId, reloadAmount);

            }

        }
        public void DetectAmmo()
        {
            if (currentBulletNum <= 0)
            {
                canShoot = false;
                animator.SetBool("NoAmmo", true);
            }
        }
        private IEnumerator Delay(float time)
        {
            yield return new WaitForSecondsRealtime(time);
            isShoot = false;
            coroutine = null;
        }

        private void OnDisable()
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }
            isShoot = false;
        }
        public void TestSpawnPosition()
        {
            // 在当前位置生成一个红色小球用于可视化
            GameObject testSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            testSphere.transform.position = shootPoint.position;
            testSphere.transform.localScale = Vector3.one * 0.1f;
            testSphere.GetComponent<Renderer>().material.color = Color.red;


            // 5秒后销毁测试球
            Destroy(testSphere, 5f);
        }
        public void CheckAmmo()
        {
            int ammoItemId = GetAmmoItemId();
            int reserveAmmo = BackPack.Instance != null && ammoItemId >= 0 ? BackPack.Instance.GetItemAmount(ammoItemId) : 0;
            playerControl.SetAmmoText(currentBulletNum + " / " + reserveAmmo);
            playerControl.SetWeaponAmmo(weapon, currentBulletNum);
        }

        private int GetAmmoItemId()
        {
            if (weapon != null && weapon.AmmoItemId >= 0)
            {
                return weapon.AmmoItemId;
            }

            return ammoIndex;
        }
        void OnDrawGizmos()
        {
            if (shootPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(shootPoint.position, 0.05f);
                Gizmos.DrawRay(shootPoint.position, shootPoint.forward * 2f);
            }
        }
    }
}

