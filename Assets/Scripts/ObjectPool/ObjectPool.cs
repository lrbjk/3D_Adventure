using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class ObjectPool : Singleton<ObjectPool>
    {
        private readonly Dictionary<string, Queue<GameObject>> objectPool = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, GameObject> parentContainers = new Dictionary<string, GameObject>();
        private readonly HashSet<int> pooledObjects = new HashSet<int>();
        private readonly Dictionary<int, int> pickupDefaults = new Dictionary<int, int>();
        private GameObject poolRoot;

        public GameObject GetObject(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Prefab is null!");
                return null;
            }

            string poolKey = GetPoolKey(prefab);
            EnsureContainer(poolKey);
            GameObject obj = TakeAvailableObject(poolKey);

            if (obj == null)
            {
                obj = Instantiate(prefab);
                PoolIdentity identity = obj.GetComponent<PoolIdentity>();
                if (identity == null)
                {
                    identity = obj.AddComponent<PoolIdentity>();
                }
                identity.SetRuntimePoolId(poolKey);
                obj.name = prefab.name;
            }

            obj.transform.SetParent(parentContainers[poolKey].transform);
            int prefabId = prefab.GetInstanceID();
            if (!pickupDefaults.TryGetValue(prefabId, out int defaultAmount))
            {
                ItemBase prefabPickup = prefab.GetComponentInChildren<ItemBase>(true);
                defaultAmount = prefabPickup != null ? prefabPickup.pickupAmount : -1;
                pickupDefaults.Add(prefabId, defaultAmount);
            }
            if (defaultAmount >= 0)
            {
                ItemBase pickup = obj.GetComponentInChildren<ItemBase>(true);
                if (pickup != null) pickup.pickupAmount = defaultAmount;
            }
            obj.SetActive(true);
            return obj;
        }

        public GameObject GetObject(GameObject prefab, Vector3 position)
        {
            GameObject obj = GetObject(prefab);
            if (obj != null)
            {
                obj.transform.position = position;
            }
            return obj;
        }

        public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject obj = GetObject(prefab);
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            return obj;
        }

        public void PushObject(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            int instanceId = obj.GetInstanceID();
            if (!pooledObjects.Add(instanceId))
            {
                Debug.LogWarning($"Object is already in pool: {obj.name}", obj);
                return;
            }

            string poolKey = GetPoolKey(obj);
            PoolIdentity identity = obj.GetComponent<PoolIdentity>();
            if (identity == null)
            {
                identity = obj.AddComponent<PoolIdentity>();
            }
            identity.SetRuntimePoolId(poolKey);

            EnsureContainer(poolKey);
            if (!objectPool.TryGetValue(poolKey, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                objectPool.Add(poolKey, queue);
            }

            obj.SetActive(false);
            obj.transform.SetParent(parentContainers[poolKey].transform);
            queue.Enqueue(obj);
        }

        public void PushObject(GameObject obj, float duration)
        {
            if (obj != null)
            {
                StartCoroutine(ReturnToPoolAfterDelay(obj, duration));
            }
        }

        private IEnumerator ReturnToPoolAfterDelay(GameObject obj, float duration)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, duration));
            PushObject(obj);
        }

        public void ClearPool(string poolKey)
        {
            if (string.IsNullOrWhiteSpace(poolKey))
            {
                return;
            }

            if (objectPool.TryGetValue(poolKey, out Queue<GameObject> queue))
            {
                while (queue.Count > 0)
                {
                    GameObject obj = queue.Dequeue();
                    if (obj != null)
                    {
                        pooledObjects.Remove(obj.GetInstanceID());
                        Destroy(obj);
                    }
                }
                objectPool.Remove(poolKey);
            }

            if (parentContainers.TryGetValue(poolKey, out GameObject container))
            {
                if (container != null)
                {
                    Destroy(container);
                }
                parentContainers.Remove(poolKey);
            }
        }

        public void ClearAllPools()
        {
            foreach (string key in new List<string>(objectPool.Keys))
            {
                ClearPool(key);
            }
            pooledObjects.Clear();
        }

        private GameObject TakeAvailableObject(string poolKey)
        {
            if (!objectPool.TryGetValue(poolKey, out Queue<GameObject> queue))
            {
                return null;
            }

            while (queue.Count > 0)
            {
                GameObject obj = queue.Dequeue();
                if (obj != null)
                {
                    pooledObjects.Remove(obj.GetInstanceID());
                    return obj;
                }
            }

            return null;
        }

        private void EnsureContainer(string poolKey)
        {
            if (poolRoot == null)
            {
                poolRoot = new GameObject("ObjectPool");
                DontDestroyOnLoad(poolRoot);
            }

            if (!parentContainers.TryGetValue(poolKey, out GameObject container) || container == null)
            {
                container = new GameObject(poolKey + "_Container");
                container.transform.SetParent(poolRoot.transform);
                parentContainers[poolKey] = container;
            }
        }

        private static string GetPoolKey(GameObject obj)
        {
            PoolIdentity identity = obj.GetComponent<PoolIdentity>();
            if (identity != null && !string.IsNullOrWhiteSpace(identity.PoolId))
            {
                return identity.PoolId;
            }

            return obj.name.Replace("(Clone)", string.Empty).Trim();
        }
    }
}
