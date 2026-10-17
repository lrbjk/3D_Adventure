using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Astraparasitus
{
    public class MenuUI : MonoBehaviour
    {
        public Button replayBtn;
        public Button quitBtn;
        private CanvasGroup canvasGroup;
        private bool restartRequested;
        public int sceneLayer;
        // Start is called before the first frame update
        void Start()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        private void OnEnable()
        {
            restartRequested = false;
            replayBtn.onClick.RemoveListener(ReplayBtn);
            replayBtn.onClick.AddListener(ReplayBtn);
        }

        private void OnDisable()
        {
            replayBtn.onClick.RemoveListener(ReplayBtn);
        }

        // Update is called once per frame
        void Update()
        {
            replayBtn.interactable = !restartRequested && canvasGroup != null && canvasGroup.alpha >= 1f;
        }
        public void ReplayBtn()
        {
            if (restartRequested)
            {
                return;
            }

            restartRequested = true;
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Load();
                if (SaveManager.Instance.TryAutoLoading())
                {
                    return;
                }
            }

            GamePlay.Instance?.StartLoadingUI();
            GamePlay.Instance?.BeforeSceneReload();
            if (!SceneLoader.LoadAsync(SceneManager.GetActiveScene().buildIndex))
            {
                GamePlay.Instance?.EndLoadingUI();
                restartRequested = false;
            }
        }
    }
}
