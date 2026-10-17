using UnityEngine;

namespace Astraparasitus
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LevelContext))]
    public class LevelInstaller : MonoBehaviour
    {
        [SerializeField] private LevelContext context;
        public LevelContext Context => context;

        private void Awake()
        {
            if (context == null) context = GetComponent<LevelContext>();
            string message = string.Empty;
            if (context == null || !context.ValidateReferences(out message))
            {
                Debug.LogError(string.IsNullOrEmpty(message) ? "LevelInstaller has no LevelContext." : message, this);
                enabled = false;
                return;
            }
            context.Install();
        }

        private void OnDestroy() => context?.Uninstall();
        private void Start() => context?.GamePlay?.LoadReDestroyObject();
        private void OnValidate()
        {
            if (context == null) context = GetComponent<LevelContext>();
        }
    }
}
