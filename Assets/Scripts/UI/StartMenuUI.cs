using UnityEngine;

namespace Astraparasitus
{
    public class StartMenuUI : MonoBehaviour
    {
        public GameObject tutorialUI;
        public void GameStart()
        {
            SceneLoader.LoadAsync(1);
        }
        public void OpenTutorial()
        {
            tutorialUI.SetActive(true);
        }
        public void ExitGame()
        {
            #if UNITY_EDITOR
            // 在编辑器模式下停止运行
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            // 在打包后的应用中退出程序
            Application.Quit();
            #endif
        }
    }
}
