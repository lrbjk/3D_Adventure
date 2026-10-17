using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public enum PauseReason
    {
        Backpack,
        ItemInspect,
        SaveMenu,
        PlayerDead,
    }

    public static class GamePause
    {
        private static readonly HashSet<PauseReason> Reasons = new HashSet<PauseReason>();

        public static bool IsPaused => Reasons.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Reset()
        {
            Reasons.Clear();
            Time.timeScale = 1f;
        }

        public static void Acquire(PauseReason reason)
        {
            if (Reasons.Add(reason))
            {
                Apply();
            }
        }

        public static void Release(PauseReason reason)
        {
            if (Reasons.Remove(reason))
            {
                Apply();
            }
        }

        public static bool Contains(PauseReason reason)
        {
            return Reasons.Contains(reason);
        }

        private static void Apply()
        {
            Time.timeScale = IsPaused ? 0f : 1f;
            Cursor.lockState = IsPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = IsPaused;
        }
    }
}
