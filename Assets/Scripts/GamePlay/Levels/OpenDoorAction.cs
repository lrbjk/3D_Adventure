using UnityEngine;
namespace Astraparasitus
{
    public class OpenDoorAction : LevelAction
    {
        [SerializeField] private Door door;
        [SerializeField] private bool openImmediately = true;
        public override Object Target => door;
        public override void Execute(LevelContext context) { if (door == null) return; door.UnLock(false); if (openImmediately && !door.GetOpenState()) door.Interact(); }
        public override bool IsConfigured(out string message) { message = door == null ? "Door 未配置" : string.Empty; return door != null; }
    }
}
