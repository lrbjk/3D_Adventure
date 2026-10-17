using UnityEngine;

namespace Astraparasitus
{
    public class BossRoomTrigger : MonoBehaviour
    {
        public AlphaSpawn alphaSpawn;
        public GameObject brokenRoof;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                alphaSpawn.SpawnEnemy();
                //alphaSpawn.SetBombPoint();
                brokenRoof.SetActive(true);
                gameObject.SetActive(false);
            }
        }
    }
}
