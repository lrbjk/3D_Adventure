using UnityEngine;
namespace Astraparasitus
{
    public class ShowTipAction : LevelAction
    {
        [SerializeField, TextArea(2, 4)] private string message;
        [SerializeField, Min(0.1f)] private float duration = 1.5f;
        public override void Execute(LevelContext context) => context?.GamePlay?.ShowTips(message, duration);
        public override bool IsConfigured(out string validationMessage) { validationMessage = string.IsNullOrWhiteSpace(message) ? "提示文本为空" : string.Empty; return !string.IsNullOrWhiteSpace(message); }
    }
}
