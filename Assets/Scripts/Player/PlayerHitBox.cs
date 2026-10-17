using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class PlayerHitBox : MonoBehaviour
    {
        public PlayerControl player;
        public string hitName;
        public float damageNum;
        //private bool isHit = false;
        public float freezeAnimaTime;
        public string layerName = "Enemy";
        public ShakeData shakeData;
        private Collider currentCollider;
        public string hitSound = "KickHit";
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(layerName) && currentCollider != other)
            {
                //if (!isHit)
                //{
                if (other.TryGetComponent(out IDamage damage))
                {
                    if (shakeData != null)
                    {
                        CameraShakerHandler.Shake(shakeData);
                    }
                    damage.TakeTC(hitName, transform, damageNum,true);
                    if(hitSound!= "")
                    {
                        AudioManager.instance.PlaySound(hitSound, player.model.audioSource);
                    }
                    player.model.FreezeFrame(freezeAnimaTime);
                    currentCollider = other;
                    //isHit = true;
                }
                //}

            }
        }
        private void OnDisable()
        {
            currentCollider = null;
            //isHit = false;
        }
    }
}