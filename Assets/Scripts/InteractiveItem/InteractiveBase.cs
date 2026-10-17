using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Astraparasitus
{
    public class InteractiveBase : MonoBehaviour, IDamage
    {
        public string itemName;
        public float maxDamage;
        public bool canCameraShake;
        public GameObject destroyEffect;
        public ShakeData cameraData;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

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
                ObjectPool.Instance.PushObject(gameObject);
            }
        }

        public virtual void DamageVector(Vector3 pos, Quaternion rot)
        {

        }

        public void TakeDamageStun(float stunValue)
        {

        }

        public void DamageName(string name,int layer)
        {

        }

        public void TakeTC(string name, Transform transform, float damage,bool isHit)
        {

        }

        public bool InBlock()
        {
            return false;
        }
    }
}