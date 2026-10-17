using UnityEngine;

namespace Astraparasitus
{
    /// <summary>
    /// 显式声明对象池归属。同一 prefab 的实例应共享同一个 PoolId。
    /// </summary>
    [DisallowMultipleComponent]
    public class PoolIdentity : MonoBehaviour
    {
        [SerializeField] private string poolId;
        public string PoolId => poolId;

        internal void SetRuntimePoolId(string value)
        {
            if (string.IsNullOrWhiteSpace(poolId))
            {
                poolId = value;
            }
        }

        private void OnValidate()
        {
            poolId = poolId?.Trim();
        }
    }
}
