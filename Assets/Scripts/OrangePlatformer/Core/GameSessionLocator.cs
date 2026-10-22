using System;

namespace OrangePlatformer.Core
{
    public static class GameSessionLocator
    {
        public static IGameSession Current { get; private set; }
        public static event Action<IGameSession> Registered;

        public static void Register(IGameSession s)
        {
            Current = s;
            Registered?.Invoke(s);
        }

        public static void Unregister(IGameSession s)
        {
            if (Current == s) Current = null;
        }
    }
}
