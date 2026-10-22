namespace OrangePlatformer.Core
{
    public readonly struct RunResult
    {
        public readonly bool Won;
        public readonly float ElapsedSeconds;
        public readonly int OrangesCollected;
        public readonly int OrangesRequired;
        public readonly int HeartsLeft;

        public RunResult(bool won, float elapsedSeconds, int orangesCollected, int orangesRequired, int heartsLeft)
        {
            Won = won; ElapsedSeconds = elapsedSeconds; OrangesCollected = orangesCollected;
            OrangesRequired = orangesRequired; HeartsLeft = heartsLeft;
        }
    }
}
