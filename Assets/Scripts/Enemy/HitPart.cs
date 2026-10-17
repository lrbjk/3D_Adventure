using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public enum HitPartType
    {
        Head,
        Body,
        LeftLeg,
        RightLeg,
        LeftArm,
        RightArm,
    }
    public class HitPart : MonoBehaviour, IDamage
    {
        public HitPartType hitPartType;
        public string hitName;
        public string hitBackName;
        public EnemyBase enemy;
        public int layer = 1;
        public float damageRate = 1;

        public void DamageName(string name,int layer)
        {

        }

        public void DamageVector(Vector3 pos, Quaternion rot)
        {

        }

        public bool InBlock()
        {
            return false;
        }

        public void TakeDamage(float damage)
        {
            enemy.playerChoesLayerAnimation(hitName, layer);
            enemy.DecreaseHp(damage * damageRate);
            enemy.DecreaseSpeed(0.1f);
            switch (hitPartType)
            {
                case HitPartType.LeftLeg:
                    enemy.DecreaseLeftLegValue(damage * damageRate);
                    break;
                case HitPartType.RightLeg:
                    enemy.DecreaseRightLegValue(damage * damageRate);
                    break;
                case HitPartType.LeftArm:
                    enemy.DecreaseLeftArmValue(damage * damageRate);
                    break;
                case HitPartType.RightArm:
                    enemy.DecreaseRightArmValue(damage * damageRate);
                    break;
            }
            
            AudioManager.instance.PlaySound(enemy.hitSound, enemy.audioSource);
        }

        public void TakeDamageStun(float stunValue)
        {
            enemy.DecreaseStunValue(stunValue * damageRate);
        }

        public void TakeTC(string name, Transform target, float damages,bool isHit)
        {
            enemy.dieAnimName = name;
            enemy.DecreaseHp(damages);
            AudioManager.instance.PlaySound(enemy.hitSound, enemy.audioSource);
            //enemy.SetDie(true);
        }
    }
}