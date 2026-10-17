using System.Collections;
using UnityEngine;

namespace Astraparasitus
{
    /// <summary>
    /// 放在启动场景。自动读档只允许由该入口发起一次。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool loadLastSaveOnStart;
        private static bool hasBootstrapped;

        private IEnumerator Start()
        {
            if (hasBootstrapped)
            {
                yield break;
            }

            hasBootstrapped = true;
            if (!loadLastSaveOnStart)
            {
                yield break;
            }

            yield return null;
            if (SaveManager.Instance == null)
            {
                Debug.LogError("GameBootstrap could not find SaveManager.");
                yield break;
            }

            SaveManager.Instance.TryAutoLoading();
        }
    }
}
