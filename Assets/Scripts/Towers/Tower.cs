using UnityEngine;
using HollowKeep.Enemies;

namespace HollowKeep.Towers
{
    public abstract class Tower : MonoBehaviour
    {
        [Header("Identidade e Economia")]
        [SerializeField] protected string towerName = "Torre";
        [SerializeField] protected int buildCost = 50;

        [Header("Evolução e Demolição")]
        [Tooltip("Custo para evoluir para o Nível 2 (0 = 80% do custo base).")]
        [Min(0)]
        [SerializeField] protected int upgradeCost = 0;
        [Min(1f)]
        [SerializeField] protected float upgradeDamageMultiplier = 1.45f;
        [Min(1f)]
        [SerializeField] protected float upgradeFireRateMultiplier = 1.2f;
        [Min(0f)]
        [SerializeField] protected float upgradeRangeBonus = 0.75f;
        [Range(0.1f, 1f)]
        [SerializeField] protected float sellRefundRatio = 0.7f;
        [SerializeField] protected float upgradedScaleMultiplier = 1.12f;

        [Header("Combate")]
        [SerializeField] protected float range = 6f;
        [Tooltip("Disparos por segundo.")]
        [SerializeField] protected float fireRate = 1f;
        [SerializeField] protected int damage = 15;

        [Header("Referências Visuais")]
        [Tooltip("Parte da torre que gira para mirar no alvo (opcional).")]
        [SerializeField] protected Transform turretHead;
        [Tooltip("Ponto de origem do projétil. Se vazio, usa a posição da torre + 1m no eixo Y.")]
        [SerializeField] protected Transform firePoint;
        [SerializeField] private float rotationSpeed = 10f;

        public string TowerName => towerName;
        public int BuildCost => buildCost;
        public float Range => range;
        public float FireRate => fireRate;
        public int Damage => damage;
        public int Level { get; private set; } = 1;
        public int MaxLevel => 2;
        public bool CanUpgrade => Level < MaxLevel;
        public int UpgradeCost => upgradeCost > 0 ? upgradeCost : Mathf.Max(10, Mathf.RoundToInt(buildCost * 0.8f));
        public float UpgradedRange => CanUpgrade ? range + upgradeRangeBonus : range;
        public int TotalInvestedGold { get; private set; }
        public int SellRefund => Mathf.Max(0, Mathf.RoundToInt(TotalInvestedGold * sellRefundRatio));

        private Enemy _currentTarget;
        private float _fireCooldown;

        protected virtual void Awake()
        {
            Level = 1;
            TotalInvestedGold = Mathf.Max(0, buildCost);
        }

        public virtual bool TryUpgrade()
        {
            if (!CanUpgrade)
            {
                return false;
            }

            TotalInvestedGold += UpgradeCost;
            Level++;
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * upgradeDamageMultiplier));
            fireRate *= upgradeFireRateMultiplier;
            range += upgradeRangeBonus;
            transform.localScale *= Mathf.Max(1f, upgradedScaleMultiplier);
            return true;
        }

        protected virtual void Update()
        {
            if (_fireCooldown > 0f)
            {
                _fireCooldown -= Time.deltaTime;
            }

            if (!IsValidTarget(_currentTarget))
            {
                _currentTarget = FindTarget();
            }

            if (_currentTarget == null) return;

            AimAt(_currentTarget);

            if (_fireCooldown <= 0f)
            {
                Fire(_currentTarget);
                _fireCooldown = 1f / Mathf.Max(0.01f, fireRate);
            }
        }

        protected virtual Enemy FindTarget()
        {
            Enemy bestTarget = null;
            var enemies = Enemy.ActiveEnemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy candidate = enemies[i];
                if (!IsValidTarget(candidate)) continue;

                if (bestTarget == null || IsFurtherAlongPath(candidate, bestTarget))
                {
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        protected bool IsValidTarget(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
            {
                return false;
            }

            Vector3 diff = enemy.transform.position - transform.position;
            diff.y = 0f;
            return diff.sqrMagnitude <= range * range;
        }

        private bool IsFurtherAlongPath(Enemy candidate, Enemy currentBest)
        {
            if (candidate.CurrentWaypointIndex != currentBest.CurrentWaypointIndex)
            {
                return candidate.CurrentWaypointIndex > currentBest.CurrentWaypointIndex;
            }

            return candidate.DistanceToNextWaypoint < currentBest.DistanceToNextWaypoint;
        }

        private void AimAt(Enemy target)
        {
            if (turretHead == null || target == null) return;

            Vector3 direction = target.transform.position - turretHead.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
                turretHead.rotation = Quaternion.Slerp(
                    turretHead.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        protected Vector3 GetFirePosition()
        {
            return firePoint != null
                ? firePoint.position
                : transform.position + Vector3.up;
        }

        protected abstract void Fire(Enemy target);

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
