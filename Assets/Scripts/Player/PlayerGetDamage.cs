using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Astraparasitus
{
    public class PlayerGetDamage : MonoBehaviour, IDamage
    {
        public PlayerControl player;
        public ShakeData shakeData;
        public void DamageName(string name,int layer)
        {
            CameraShakerHandler.Shake(shakeData);
            if(name == "Hit_defence")
            {
                player.ChangeState(PlayerState.Parry);
            }
            else
            {
                player.playerChoesLayerAnimation(name, layer);
            }
            //player.HitSpeed(0.05f);
            //player.rb.AddForce(-player.model.transform.forward*1000f,ForceMode.Impulse);
        }

        public void DamageVector(Vector3 pos, Quaternion rot)
        {

        }

        public bool InBlock()
        {
            if(player.currentState == PlayerState.Block || player.currentState == PlayerState.Parry)
            {
                return true;
            }
            return false;
        }

        public void TakeDamage(float damage)
        {
            player.DecreaseDefence(damage);
            player.DecreaseHp(damage);
        }

        public void TakeDamageStun(float stunValue)
        {
            if (stunValue == 0)
            {
                player.SetTCKnife(false);
            }
            else if (stunValue == 1)
            {
                player.SetTCKnife(true);
            }
        }

        public void TakeTC(string name, Transform transform, float damage,bool isHit)
        {
            if (isHit)
            {
                if (name == "Hit_defence")
                {
                    player.ChangeState(PlayerState.Parry);
                }
                else
                {
                    player.currentTCName = name;
                    player.enemyPosition = transform;
                    player.ChangeState(PlayerState.Hit);
                }
            }
            else
            {
                player.currentTCName = name;
                player.enemyPosition = transform;
                player.ChangeState(PlayerState.TC);
            }
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