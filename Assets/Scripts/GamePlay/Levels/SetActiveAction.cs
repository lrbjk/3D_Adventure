using UnityEngine;
namespace Astraparasitus
{
    public class SetActiveAction : LevelAction
    {
        [SerializeField] private GameObject target;
        [SerializeField] private bool active = true;
        public override Object Target => target;
        public override void Execute(LevelContext context) => target?.SetActive(active);
        public override bool IsConfigured(out string message) { message = target == null ? "目标对象未配置" : string.Empty; return target != null; }
    }
}
