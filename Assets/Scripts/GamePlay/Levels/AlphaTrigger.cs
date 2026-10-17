using UnityEngine;

namespace Astraparasitus
{
    public class AlphaTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                ObjectPool.Instance.PushObject(other.gameObject);
            }
        }
    }
}
