using BehaviorDesigner.Runtime.Tasks.Unity.UnityAnimator;
using DG.Tweening;
using Knife.Effects;
using Knife.Effects.SimpleController;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace Astraparasitus
{
    public class BezierCurve : MonoBehaviour
    {
        //public Transform startTran;
        public Transform endTran;
        private Vector3 centerPoint;
        public float height;
        public float speed;
        public int resolution;
        [Header("-----------------")]
        public string targetTag;
        public int hitlayer = 0;
        public float damage;
        public float decreaseKnifeUse;
        public string hitName;
        public GameObject bloodEffect;
        public GameObject stayEffect;
        //private Vector3[] path;
        // Start is called before the first frame update
        private void OnEnable()
        {
            
        }
        void Start()
        {
            //startTran = transform;
            
            
        }

        // Update is called once per frame
        void Update()
        {

        }
        public void SetCenterPoint()
        {
            centerPoint = (transform.position + endTran.position) * 0.5f + (Vector3.up * height);
        }
        public void DoAnim()
        {
            Vector3[] pathvec = Bezier2Path(transform.position, centerPoint, endTran.position);
            transform.DOPath(pathvec, speed).SetEase(Ease.Linear);
        }
        private Vector3[] Bezier2Path(Vector3 startPos, Vector3 controlPos, Vector3 endPos)
        {
            Vector3[] path = new Vector3[resolution];
            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)(resolution-1);
                path[i] = GetBezierPoint(t,startPos, controlPos, endPos);
            }
            return path;
        }
        /// <param name="t">0��1��ֵ��0��ȡ���ߵ���㣬1������ߵ��յ�</param>
        /// <param name="start">���ߵ���ʼλ��</param>
        /// <param name="center">����������״�Ŀ��Ƶ�</param>
        /// <param name="end">���ߵ��յ�</param>
        public static Vector3 GetBezierPoint(float t, Vector3 start, Vector3 center, Vector3 end)
        {
            return (1 - t) * (1 - t) * start + 2 * t * (1 - t) * center + t * t * end;
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(targetTag))
            {
                if (other.TryGetComponent(out IDamage damage))
                {
                    if (!damage.InBlock())
                    {
                        damage.TakeDamage(this.damage);
                        damage.TakeTC(hitName, gameObject.transform,0,true);
                        Vector3 hitPosition = other.ClosestPoint(transform.position);
                        ObjectPool.Instance.GetObject(bloodEffect, hitPosition, Quaternion.identity);
                    }
                    else
                    {
                        GamePlay.Instance.player.DescreaseKnifeUse(decreaseKnifeUse);
                        damage.TakeTC("Hit_defence", gameObject.transform, 0, true);
                    }
                }
                ObjectPool.Instance.PushObject(gameObject);
            }
            else if(!other.CompareTag("Enemy") && !other.CompareTag("Untagged"))
            {
                GameObject obj = ObjectPool.Instance.GetObject(stayEffect);
                obj.transform.position = gameObject.transform.position;
                ObjectPool.Instance.PushObject(gameObject);
            }
        }
    }
}
