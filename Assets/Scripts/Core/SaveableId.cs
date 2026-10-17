using UnityEngine;

namespace Astraparasitus
{
    [DisallowMultipleComponent]
    public class SaveableId : MonoBehaviour
    {
        [SerializeField, Tooltip("存档使用的稳定 ID。复制场景对象后请运行内容校验。")]
        private string id;

        public string Id => id;

        private void Awake()
        {
            EnsureId();
        }

        private void OnValidate()
        {
            EnsureId();
        }

        [ContextMenu("生成新的存档 ID")]
        public void RegenerateId()
        {
            id = GameContentId.Create();
        }

        private void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = GameContentId.Create();
            }
        }
    }
}
