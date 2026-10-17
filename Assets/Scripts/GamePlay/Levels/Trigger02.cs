using UnityEngine;

namespace Astraparasitus
{
    public class Trigger02 : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                gameObject.SetActive(false);
            }
        }
    }
}
