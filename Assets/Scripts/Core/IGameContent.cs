using System;

namespace Astraparasitus
{
    public interface IGameContent
    {
        string StableId { get; }
    }

    public static class GameContentId
    {
        public static string Create()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
