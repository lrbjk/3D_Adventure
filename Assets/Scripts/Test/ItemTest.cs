using UnityEngine;
namespace Astraparasitus
{
    public class ItemTest : MonoBehaviour
    {
        public ItemConfig[] itemConfigs;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                ReloadScene();
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                ReloadSceneNew();
            }
        }
        public void AddKey1()
        {
            BackPack.Instance.AddItem(itemConfigs[0].item);
        }
        public void ReloadScene()
        {
            SceneLoader.LoadAsync(1);
        }
        public void ReloadSceneNew()
        {
            GamePlay.Instance.BeforeSceneReload();
            SceneLoader.LoadAsync(1);
        }
    }
}
