using UnityEngine;
using HollowKeep.Enemies;
using HollowKeep.Projectiles;

namespace HollowKeep.Towers
{
    public class DirectTower : Tower
    {
        [Header("Projétil (Mira Direta)")]
        [SerializeField] private DirectProjectile projectilePrefab;

        private void Reset()
        {
            towerName = "Torre Arqueira";
            buildCost = 50;
            range = 8f;
            fireRate = 2f;
            damage = 12;
        }

        protected override void Fire(Enemy target)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[DirectTower] {gameObject.name} está sem projectilePrefab atribuído!", this);
                return;
            }

            DirectProjectile projectile = Instantiate(projectilePrefab, GetFirePosition(), Quaternion.identity);
            projectile.Launch(target, damage);
        }
    }
}
