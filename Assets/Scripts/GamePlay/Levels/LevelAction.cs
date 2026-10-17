using UnityEngine;

namespace Astraparasitus
{
    public abstract class LevelAction : MonoBehaviour
    {
        public virtual Object Target => null;
        public abstract void Execute(LevelContext context);
        public abstract bool IsConfigured(out string message);
    }
}
