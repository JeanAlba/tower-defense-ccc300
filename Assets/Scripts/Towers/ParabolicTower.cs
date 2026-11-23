using UnityEngine;
using HollowKeep.Audio;
using HollowKeep.Enemies;
using HollowKeep.Projectiles;
using HollowKeep.VFX;

namespace HollowKeep.Towers
{
    public class ParabolicTower : Tower
    {
        [Header("Projétil (Parábola + Área)")]
        [SerializeField] private ParabolicProjectile projectilePrefab;
        [SerializeField] private float splashRadius = 2.5f;
        [Tooltip("Compensação da predição ao longo da rota (1 = acompanha o tempo exato de voo).")]
        [Range(0f, 1.2f)]
        [SerializeField] private float leadFactor = 1f;

        [Header("Efeitos Visuais")]
        [SerializeField] private ParticleSystem muzzleVfxPrefab;

        public float SplashRadius => splashRadius;

        private void Reset()
        {
            towerName = "Torre Canhão";
            buildCost = 80;
            range = 5f;
            fireRate = 0.6f;
            damage = 35;
            splashRadius = 2.5f;
            leadFactor = 1f;
        }

        protected override void Fire(Enemy target)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[ParabolicTower] {gameObject.name} está sem projectilePrefab atribuído!", this);
                return;
            }

            Vector3 firePosition = GetFirePosition();
            Vector3 aimPosition = target.PredictPositionAfter(projectilePrefab.FlightDuration * leadFactor);
            Vector3 toTarget = aimPosition - firePosition;
            toTarget.y = 0f;
            Quaternion fireRotation = toTarget.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(toTarget.normalized)
                : (turretHead != null ? turretHead.rotation : transform.rotation);

            VfxSpawner.Spawn(muzzleVfxPrefab, firePosition, fireRotation);

            ParabolicProjectile projectile = Instantiate(projectilePrefab, firePosition, Quaternion.identity);
            projectile.Launch(aimPosition, damage, splashRadius);
            GameAudioManager.PlayCannonShoot();
        }
    }
}
