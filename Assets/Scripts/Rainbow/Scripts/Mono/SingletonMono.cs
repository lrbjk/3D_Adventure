using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
{
    public static T Instance;
    protected virtual void Awake()
    {
        // LoadSceneMode.Single 期间新旧场景可能短暂重叠，新场景实例需要立即接管。
        // 同一场景内如果误放了多个实例，继续保留最先初始化的实例，便于暴露配置问题。
        if (Instance == null || Instance == this || Instance.gameObject.scene != gameObject.scene)
        {
            Instance = (T)this;
        }
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
