using UnityEngine;

namespace HollowKeep.Level.Data
{
    [CreateAssetMenu(fileName = "Difficulty_New", menuName = "HollowKeep/Level/Difficulty Profile")]
    public class DifficultyProfile : ScriptableObject
    {
        [Header("Identificação")]
        [SerializeField] private string difficultyId = "Normal";
        [SerializeField] private string displayName = "Normal";
        [TextArea(2, 3)]
        [SerializeField] private string description;

        [Header("Economia e Castelo")]
        [Min(1)]
        [SerializeField] private int startingLives = 20;

        [Min(0.1f)]
        [SerializeField] private float startingGoldMultiplier = 1f;

        [Min(0f)]
        [SerializeField] private float goldRewardMultiplier = 1f;

        [Header("Inimigos e Ondas")]
        [Min(0.1f)]
        [SerializeField] private float enemyHealthMultiplier = 1f;

        [Min(0.1f)]
        [SerializeField] private float enemySpeedMultiplier = 1f;

        [Min(0.1f)]
        [SerializeField] private float spawnQuantityMultiplier = 1f;

        [Min(0.1f)]
        [SerializeField] private float spawnIntervalMultiplier = 1f;

        [Min(0.1f)]
        [SerializeField] private float timeBetweenWavesMultiplier = 1f;

        public string DifficultyId => difficultyId;
        public string DisplayName => displayName;
        public string Description => description;
        public int StartingLives => startingLives;
        public float StartingGoldMultiplier => startingGoldMultiplier;
        public float GoldRewardMultiplier => goldRewardMultiplier;
        public float EnemyHealthMultiplier => enemyHealthMultiplier;
        public float EnemySpeedMultiplier => enemySpeedMultiplier;
        public float SpawnQuantityMultiplier => spawnQuantityMultiplier;
        public float SpawnIntervalMultiplier => spawnIntervalMultiplier;
        public float TimeBetweenWavesMultiplier => timeBetweenWavesMultiplier;

        public int GetStartingGold(int baseStartingGold)
        {
            return Mathf.Max(0, Mathf.RoundToInt(baseStartingGold * startingGoldMultiplier));
        }

        public int GetScaledQuantity(int baseQuantity)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseQuantity * spawnQuantityMultiplier));
        }

        public float GetScaledInterval(float baseInterval)
        {
            return Mathf.Max(0.05f, baseInterval * spawnIntervalMultiplier);
        }

        public float GetScaledTimeBetweenWaves(float baseTime)
        {
            return Mathf.Max(0f, baseTime * timeBetweenWavesMultiplier);
        }
    }
}
