using UnityEngine;
using HollowKeep.Enemies;
using HollowKeep.Projectiles;

namespace HollowKeep.Towers
{
    public class ParabolicTower : Tower
    {
        [Header("Projétil (Parábola + Área)")]
        [SerializeField] private ParabolicProjectile projectilePrefab;
        [SerializeField] private float splashRadius = 2.5f;

        public float SplashRadius => splashRadius;

        private void Reset()
        {
            towerName = "Torre Canhão";
            buildCost = 80;
            range = 5f;
            fireRate = 0.6f;
            damage = 35;
            splashRadius = 2.5f;
        }

        protected override void Fire(Enemy target)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[ParabolicTower] {gameObject.name} está sem projectilePrefab atribuído!", this);
                return;
            }

            ParabolicProjectile projectile = Instantiate(projectilePrefab, GetFirePosition(), Quaternion.identity);
            projectile.Launch(target.transform.position, damage, splashRadius);
        }
    }
}
