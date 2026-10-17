using UnityEngine;

namespace Astraparasitus
{
    public class TubeEndTrigger : MonoBehaviour
    {
        public GameObject barry;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                barry.SetActive(true);
                gameObject.SetActive(false);
            }
            
        }
    }
}
