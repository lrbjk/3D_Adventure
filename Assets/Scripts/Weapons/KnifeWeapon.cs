using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class KnifeWeapon : MonoBehaviour
    {
        public Vector3 size;
        public float damage;
        public WpConfig wpConfig;
        public PlayerControl playerControl;
        public float knockbackForce;
        public float currentUse;
        public Transform hitPoint;
        public LayerMask hitLayer;
        public CapsuleCollider capsuleCollider;
        private bool isAttacking;
        public float freezingTime;
        public GameObject bloodEffect;
        public string hitName;
        public AudioSource audioSource;
        private void Awake()
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
            capsuleCollider.enabled = false;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (wpConfig != null)
            {
                damage = wpConfig.damage;
            }
        }
        private void OnEnable()
        {
            playerControl?.SetKnifeVisible(true);
            playerControl?.DescreaseKnifeUse(0);
            playerControl?.SetAmmoText(string.Empty);
        }
        private void OnDisable()
        {
            if (capsuleCollider != null)
            {
                capsuleCollider.enabled = false;
            }
            isAttacking = false;
            playerControl?.SetKnifeVisible(false);
        }
        // Update is called once per frame
        void Update()
        {
            if (playerControl.model.openCheck)
            {
                //DamageCheck();
                OpenCheck();
            }
            else
            {
                CloseCheck();
            }
        }
        public void OpenCheck()
        {
            if (!capsuleCollider.enabled)
            {
                isAttacking = false;
            }
            capsuleCollider.enabled = true;
        }
        public void CloseCheck()
        {
            capsuleCollider.enabled = false;
            isAttacking = false;
        }
        //public void DamageCheck()
        //{
        //    Collider[] colliders = new Collider[1];
        //    int count = Physics.OverlapBoxNonAlloc(hitPoint.position, size, colliders, hitPoint.rotation,hitLayer);
        //    if (count > 0)
        //    {
        //        for (int i = 0; i < count; i++)
        //        {
        //            if (colliders[i].GetComponent<Rigidbody>() != null)
        //            {
        //                colliders[i].GetComponent<Rigidbody>().AddForce(transform.forward * knockbackForce, ForceMode.Impulse);
        //            }
        //            if (colliders[i].TryGetComponent(out IDamage damagable))
        //            {
        //                damagable.TakeDamage(damage);
        //            }
        //        }
        //    }
        //}
        //private void OnDrawGizmos()
        //{
        //    Gizmos.color = Color.red;
        //    Gizmos.DrawWireCube(hitPoint.position,size);
        //}
        private void OnTriggerEnter(Collider other)
        {
            if (isAttacking || other == null)
            {
                return;
            }

            HitPart hitPart = other.GetComponent<HitPart>();
            if (hitPart == null)
            {
                hitPart = other.GetComponentInParent<HitPart>();
            }
            if (hitPart == null)
            {
                EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
                if (enemy != null)
                {
                    hitPart = enemy.GetComponentInChildren<HitPart>();
                }
            }
            if (hitPart == null)
            {
                return;
            }

            hitPart.TakeTC(hitName, playerControl.transform, damage, true);
            playerControl.model.FreezeFrame(freezingTime);
            Vector3 hitPosition = other.ClosestPoint(transform.position);
            AudioManager.instance.PlaySound("knifeHit", audioSource);
            if (ObjectPool.Instance != null && bloodEffect != null)
            {
                ObjectPool.Instance.GetObject(bloodEffect, hitPosition, Quaternion.identity);
            }
            playerControl.DescreaseKnifeUse(5);
            isAttacking = true;
        }
    }
}
