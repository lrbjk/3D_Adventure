using FirstGearGames.SmoothCameraShaker;
using UnityEngine;

namespace Astraparasitus
{
    public class Trigger01 : MonoBehaviour
    {
        public GameObject target;
        public GameObject targetEffect;
        public ShakeData shakeData;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                target.SetActive(false);
                CameraShakerHandler.Shake(shakeData);
                gameObject.SetActive(false);
                GameObject effect = ObjectPool.Instance.GetObject(targetEffect);
                effect.transform.position = target.transform.position;
            }
        }
    }
}
