using UnityEngine;

namespace HollowKeep.Enemies
{
    [CreateAssetMenu(fileName = "EnemyStats_New", menuName = "HollowKeep/Enemies/Enemy Stats")]
    public class EnemyStatsData : ScriptableObject
    {
        [Header("Identidade")]
        [SerializeField] private string enemyName = "Inimigo";

        [Header("Atributos Base")]
        [Min(1)]
        [SerializeField] private int maxHealth = 60;

        [Min(0.1f)]
        [SerializeField] private float moveSpeed = 3.5f;

        [Min(0)]
        [SerializeField] private int goldReward = 10;

        [Min(1)]
        [SerializeField] private int damageToCastle = 1;

        public string EnemyName => enemyName;
        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public int GoldReward => goldReward;
        public int DamageToCastle => damageToCastle;
    }
}
