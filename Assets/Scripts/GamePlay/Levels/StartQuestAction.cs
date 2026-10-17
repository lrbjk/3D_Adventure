using UnityEngine;
namespace Astraparasitus
{
    public class StartQuestAction : LevelAction
    {
        [SerializeField] private QuestInfo quest;
        public override Object Target => quest;
        public override void Execute(LevelContext context) => QuestManager.Instance?.RequestStartQuest(quest);
        public override bool IsConfigured(out string message) { message = quest == null ? "QuestInfo 未配置" : string.Empty; return quest != null; }
    }
}
