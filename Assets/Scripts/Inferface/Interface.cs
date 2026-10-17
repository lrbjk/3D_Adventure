using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public enum DamageType
    {
        ImpactDamage,
        ExplosionDamage,

    }
    public interface IDamage
    {
        /// <summary>
        /// 伤害
        /// </summary>
        /// <param name="damage"></param>
        public void TakeDamage(float damage);
        /// <summary>
        /// 硬直伤害
        /// </summary>
        /// <param name="stunValue"></param>
        public void TakeDamageStun(float stunValue);
        /// <summary>
        /// 受到伤害的位置
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        public void DamageVector(Vector3 pos, Quaternion rot);
        /// <summary>
        /// 受到伤害的名字
        /// </summary>
        /// <param name="name"></param>
        public void DamageName(string name,int layer);
        /// <summary>
        /// 受到投技
        /// </summary>
        public void TakeTC(string name, Transform transform, float damage,bool isHit);
        /// <summary>
        /// 在防御
        /// </summary>
        /// <returns></returns>
        public bool InBlock();
    }
    public interface Iinteractable
    {
        /// <summary>
        /// 交互逻辑
        /// </summary>
        public void Interact();
        /// <summary>
        /// 展示UI
        /// </summary>
        public void ShowInfo();
        /// <summary>
        /// 隐藏UI
        /// </summary>
        public void HideInfo();
    }
}