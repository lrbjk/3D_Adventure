using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astraparasitus
{
    /// <summary>
    /// 项目唯一的场景加载入口，负责校验场景并阻止重复加载。
    /// </summary>
    public static class SceneLoader
    {
        public static bool IsLoading { get; private set; }

        public static bool CanLoad(int buildIndex)
        {
            return buildIndex >= 0 &&
                   !string.IsNullOrWhiteSpace(SceneUtility.GetScenePathByBuildIndex(buildIndex));
        }

        public static bool LoadAsync(
            int buildIndex,
            Action<AsyncOperation> completed = null,
            LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (IsLoading)
            {
                Debug.LogWarning("A scene load is already in progress.");
                return false;
            }

            if (!CanLoad(buildIndex))
            {
                Debug.LogError($"Scene build index is not available: {buildIndex}");
                return false;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex, mode);
            if (operation == null)
            {
                Debug.LogError($"Unable to start scene load: {buildIndex}");
                return false;
            }

            IsLoading = true;
            operation.completed += FinishLoad;
            return true;

            void FinishLoad(AsyncOperation result)
            {
                operation.completed -= FinishLoad;
                IsLoading = false;
                completed?.Invoke(result);
            }
        }
    }
}
