using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class SavinggTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                SaveManager.Instance.AutoSave();
                GamePlay.Instance.player.spotlight.SetActive(true);
                gameObject.SetActive(false);
            }
        }
    }
}
