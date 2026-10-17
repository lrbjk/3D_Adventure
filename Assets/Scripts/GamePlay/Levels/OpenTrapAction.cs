using UnityEngine;

namespace Astraparasitus
{
    public class OpenTrapAction : LevelAction
    {
        [SerializeField, Tooltip("要播放开门动画的 Trap 组件。")]
        private Trap trap;

        public override Object Target => trap;
        public override void Execute(LevelContext context) => trap?.OpenTrap();
        public override bool IsConfigured(out string message)
        {
            message = trap == null ? "Trap 未配置" : string.Empty;
            return trap != null;
        }
    }
}
