using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class Box : ItemBase, IDamage
    {
        public GameObject itemPrefab;
        public bool isRandom = true;
        public GameObject breakPrefab;
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

        }

        public void TakeDamageStun(float stunValue)
        {

        }

        public void TakeTC(string name, Transform transform, float damage,bool isHit)
        {
            ObjectPool.Instance.PushObject(gameObject);
            GameObject obj = ObjectPool.Instance.GetObject(breakPrefab);
            obj.transform.position = this.transform.position;
            if (isRandom)
            {
                GamePlay.Instance?.SpawnResourceDrop(ResourceDropSource.Container, null, transform.position);
            }
            else
            {
                GameObject obj2 = ObjectPool.Instance.GetObject(itemPrefab);
                obj2.transform.position = this.transform.position;
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
