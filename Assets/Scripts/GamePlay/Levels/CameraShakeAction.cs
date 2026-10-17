using FirstGearGames.SmoothCameraShaker;
using UnityEngine;
namespace Astraparasitus
{
    public class CameraShakeAction : LevelAction
    {
        [SerializeField] private ShakeData shakeData;
        public override Object Target => shakeData;
        public override void Execute(LevelContext context) { if (shakeData != null) CameraShakerHandler.Shake(shakeData); }
        public override bool IsConfigured(out string message) { message = shakeData == null ? "ShakeData 未配置" : string.Empty; return shakeData != null; }
    }
}
