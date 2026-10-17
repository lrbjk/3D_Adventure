using System;
using UnityEngine;
using UnityEngine.UI;

namespace Astraparasitus
{
    public class SaveUI : MonoBehaviour
    {
        public Text time;
        private void OnEnable()
        {
            GamePlay.Instance.player.SetAnimaionSpeed(0);
            GamePlay.Instance.player.playerInput.Disable();
            GamePause.Acquire(PauseReason.SaveMenu);
        }
        public void SaveGame()
        {
            SaveManager.Instance.SavingTest();
            time.text = DateTime.Now.ToString();
        }
        public void CloseUI()
        {
            gameObject.SetActive(false);
        }
        private void OnDisable()
        {
            //AudioManager.instance.PlaySound("Back", audioSource);
            GamePause.Release(PauseReason.SaveMenu);
            if (!GamePause.IsPaused && GamePlay.Instance != null && GamePlay.Instance.player != null)
            {
                GamePlay.Instance.player.SetAnimaionSpeed(1);
                GamePlay.Instance.player.playerInput.Enable();
            }
        }
    }
}
