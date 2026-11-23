namespace HollowKeep.Level.Data
{
    /// <summary>
    /// Armazena a configuração de sessão escolhida entre cenas (ex.: Menu -> Fase).
    /// </summary>
    public static class GameSessionConfig
    {
        public static DifficultyProfile SelectedDifficulty { get; private set; }

        public static void SetDifficulty(DifficultyProfile difficulty)
        {
            SelectedDifficulty = difficulty;
        }

        public static DifficultyProfile Resolve(DifficultyProfile fallback)
        {
            return SelectedDifficulty != null ? SelectedDifficulty : fallback;
        }

        public static void Reset()
        {
            SelectedDifficulty = null;
        }
    }
}
