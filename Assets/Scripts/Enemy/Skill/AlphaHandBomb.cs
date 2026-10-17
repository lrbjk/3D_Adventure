using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class AlphaHandBomb : MonoBehaviour,IDamage
    {
        public float maxDamage;
        public bool canCameraShake;
        public GameObject destroyEffect;
        public ShakeData cameraData;
        public void DamageName(string name, int layer)
        {
            
        }

        public void DamageVector(Vector3 pos, Quaternion rot)
        {
            
        }

        public bool InBlock()
        {
            return false;
        }

        public virtual void TakeDamage(float damage)
        {
            if (damage > maxDamage)
            {
                GameObject effect = ObjectPool.Instance.GetObject(destroyEffect, transform.position, Quaternion.identity);

                if (canCameraShake)
                {
                    CameraShakerHandler.Shake(cameraData);
                }
                gameObject.SetActive(false);
            }
        }

        public void TakeDamageStun(float stunValue)
        {
            
        }

        public void TakeTC(string name, Transform transform, float damage, bool isHit)
        {
            
        }

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
