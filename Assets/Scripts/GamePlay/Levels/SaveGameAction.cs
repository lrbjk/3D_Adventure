namespace Astraparasitus
{
    public class SaveGameAction : LevelAction
    {
        public override void Execute(LevelContext context) => SaveManager.Instance?.AutoSave();
        public override bool IsConfigured(out string message) { message = string.Empty; return true; }
    }
}
