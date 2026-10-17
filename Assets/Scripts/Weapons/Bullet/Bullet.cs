using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class Bullet : MonoBehaviour
    {
        public float speed;
        public float damage;
        public float stunRate;
        public float addForce;
        public float range;
        public Vector3 direction;
        public GameObject[] hitEffect;
        public GameObject[] hitImpacts;
        private Vector3 pos;
        private Quaternion rot;
        public Vector3 previousPosition;
        private Vector3 tempDistance;
        [SerializeField] private int index;

        // 记录初始位置，用于距离计算
        private Vector3 startPosition;
        private bool isActive = false;

        // 调试信息
        [Header("Debug")]
        public bool showDebug;
        public string bulletID;

        void Start()
        {
            // 确保只在第一次启用时初始化
            if (!isActive)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            // 生成唯一ID用于调试
            bulletID = "Bullet_" + Time.time + "_" + UnityEngine.Random.Range(1000, 9999);

            if (showDebug)
            {
            }
        }

        private void OnDisable()
        {
            // 重置状态但保持位置（对象池可能重用）
            isActive = false;
            speed = 0;

            if (showDebug)
            {
            }
        }

        /// <summary>
        /// 初始化子弹状态
        /// </summary>
        private void Initialize()
        {
            previousPosition = transform.position;
            startPosition = transform.position;
            isActive = true;

            if (showDebug)
            {
            }
        }

        /// <summary>
        /// 从对象池取出后的初始化方法
        /// </summary>
        public void SetupBullet(Vector3 position, Vector3 direction, float speed, float damage, float stunRate, float addForce, GameObject[] hitEffect, GameObject[] hitImpacts)
        {
            if (showDebug)
            {
            }

            transform.position = position;
            this.direction = direction.normalized;
            this.speed = speed;
            this.damage = damage;
            this.stunRate = stunRate;
            this.addForce = addForce;
            this.hitEffect = hitEffect;
            this.hitImpacts = hitImpacts;

            // 设置正确的旋转方向
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // 重新初始化
            Initialize();

            if (showDebug)
            {
            }
        }

        void FixedUpdate()
        {
            if (!isActive) return;

            // 检查射程
            float distanceTraveled = Vector3.Distance(startPosition, transform.position);
            if (distanceTraveled >= range)
            {
                if (showDebug)
                {
                }
                ObjectPool.Instance.PushObject(gameObject);
                return;
            }

            // 计算移动
            MoveBullet();
        }

        /// <summary>
        /// 子弹移动逻辑
        /// </summary>
        private void MoveBullet()
        {
            Vector3 movement = direction * speed * Time.deltaTime;
            Vector3 newPosition = previousPosition + movement;

            // 使用射线检测路径上的碰撞
            RaycastHit hit;
            Vector3 rayDirection = newPosition - previousPosition;
            float distance = rayDirection.magnitude;

            if (showDebug)
            {
                Debug.DrawRay(previousPosition, rayDirection, Color.red, 0.1f);
            }

            if (Physics.Raycast(previousPosition, rayDirection, out hit, distance))
            {
                if (showDebug)
                {
                }

                if (!IsValidCollision(hit.collider.gameObject.layer))
                {
                    // 无效碰撞，继续移动
                    transform.position = newPosition;
                    previousPosition = newPosition;
                    return;
                }

                // 处理有效碰撞
                OnHit(hit);
            }
            else
            {
                // 没有碰撞，正常移动
                transform.position = newPosition;
                previousPosition = newPosition;

                if (showDebug && Time.frameCount % 30 == 0) // 每30帧输出一次位置
                {
                }
            }
        }

        // 其余方法保持不变...
        private bool IsValidCollision(int layer)
        {
            return layer != gameObject.layer;
        }

        void OnHit(RaycastHit hit)
        {
            if (!isActive) return;

            if (showDebug)
            {
            }

            // 停止子弹
            isActive = false;
            speed = 0;

            // 移动子弹到碰撞点
            transform.position = hit.point;

            // 根据层级设置特效索引
            SetEffectIndexByLayer(hit.collider.gameObject.layer);

            // 创建碰撞效果
            SpawnHitEffects(hit.collider, hit.point, hit.normal);

            // 施加物理力
            ApplyForceToHitObject(hit.collider);

            // 造成伤害
            ApplyDamage(hit.collider);

            // 回收到对象池
            ObjectPool.Instance.PushObject(gameObject);
        }

        private void SetEffectIndexByLayer(int layer)
        {
            switch (layer)
            {
                case 7: index = 0; break;
                case 8: index = 1; break;
                case 9: index = 2; break;
                case 10: index = 3; break;
                case 11: index = 4; break;
                default: index = 0; break;
            }
        }

        private void SpawnHitEffects(Collider collider, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (hitEffect == null || hitEffect.Length == 0) return;
            if (index < 0 || index >= hitEffect.Length) return;

            Quaternion effectRotation = Quaternion.LookRotation(hitNormal);
            var hitVFX = ObjectPool.Instance.GetObject(hitEffect[index], hitPoint, effectRotation);

            if (hitVFX != null)
            {
                var psHit = hitVFX.GetComponent<ParticleSystem>();
                if (psHit != null)
                {
                    ObjectPool.Instance.PushObject(hitVFX, psHit.main.duration);
                }
                else
                {
                    ObjectPool.Instance.PushObject(hitVFX, 2f);
                }
            }

            if (hitImpacts != null && hitImpacts.Length > index && hitImpacts[index] != null)
            {
                var hitImpact = ObjectPool.Instance.GetObject(hitImpacts[index], hitPoint, effectRotation);
                if (hitImpact != null && collider != null)
                {
                    hitImpact.transform.SetParent(collider.transform);
                    ObjectPool.Instance.PushObject(hitImpact, 5f);
                }
            }
        }

        private void ApplyForceToHitObject(Collider collider)
        {
            if (collider != null && collider.attachedRigidbody != null)
            {
                collider.attachedRigidbody.AddForce(direction * addForce, ForceMode.Impulse);
            }
        }

        private void ApplyDamage(Collider collider)
        {
            if (collider.TryGetComponent(out IDamage damagable))
            {
                damagable.TakeDamage(damage);
                damagable.TakeDamageStun(damage * stunRate);
                damagable.DamageVector(transform.position, transform.rotation);
            }
        }

        private void OnDrawGizmos()
        {
            if (!isActive || !showDebug) return;

            // 绘制移动方向
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(previousPosition, previousPosition + direction * 2f);

            // 绘制起始位置
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPosition, 0.1f);

            // 绘制当前位置
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.05f);
        }
    }
}
