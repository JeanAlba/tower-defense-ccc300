using System.Collections.Generic;
using UnityEngine;
using HollowKeep.Audio;
using HollowKeep.Enemies;
using HollowKeep.VFX;

namespace HollowKeep.Projectiles
{
    public class ParabolicProjectile : Projectile
    {
        [Header("Trajetória Parabólica")]
        [SerializeField] private float flightDuration = 0.6f;
        [SerializeField] private float arcHeight = 2.8f;

        [Header("Efeitos Visuais")]
        [SerializeField] private ParticleSystem explosionVfxPrefab;
        [SerializeField] private ParticleSystem trailVfx;

        public float FlightDuration => Mathf.Max(0.05f, flightDuration);

        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private float _splashRadius;
        private float _elapsed;
        private bool _isLaunched;
        private bool _hasExploded;

        private void Awake()
        {
            EnsurePhysicsComponents();
        }

        private void EnsurePhysicsComponents()
        {
            if (!TryGetComponent(out Collider col))
            {
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = 0.3f;
                sphere.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }

            if (!TryGetComponent(out Rigidbody rb))
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        public void Launch(Vector3 targetPosition, int damage, float splashRadius)
        {
            InitializeDamage(damage);
            _startPosition = transform.position;
            _targetPosition = targetPosition;
            _splashRadius = Mathf.Max(0.1f, splashRadius);
            _elapsed = 0f;
            _isLaunched = true;
            _hasExploded = false;
        }

        protected override void Update()
        {
            base.Update();

            if (!_isLaunched || _hasExploded) return;

            _elapsed += Time.deltaTime;
            float duration = Mathf.Max(0.05f, flightDuration);
            float t = Mathf.Clamp01(_elapsed / duration);

            Vector3 previousPos = transform.position;
            Vector3 nextPos = Vector3.Lerp(_startPosition, _targetPosition, t);
            nextPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            transform.position = nextPos;

            Vector3 velocityDir = nextPos - previousPos;
            if (velocityDir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(velocityDir.normalized);
            }

            if (t >= 1f)
            {
                Explode();
            }
        }

        private void Explode()
        {
            if (_hasExploded) return;
            _hasExploded = true;

            VfxSpawner.Spawn(explosionVfxPrefab, _targetPosition, Quaternion.identity);
            GameAudioManager.PlayCannonExplode();
            DetachAndStopTrail();

            HashSet<Enemy> damagedEnemies = new();

            // Detecção de colisão física via Physics.OverlapSphere nos Colliders dos inimigos
            Collider[] hitColliders = Physics.OverlapSphere(_targetPosition, _splashRadius);
            for (int i = 0; i < hitColliders.Length; i++)
            {
                Enemy hitEnemy = hitColliders[i] != null ? hitColliders[i].GetComponentInParent<Enemy>() : null;
                if (hitEnemy != null && !hitEnemy.IsDead && damagedEnemies.Add(hitEnemy))
                {
                    hitEnemy.TakeDamage(Damage);
                }
            }

            // Fallback por distância no plano XZ para garantir consistência
            var enemies = Enemy.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || enemy.IsDead || damagedEnemies.Contains(enemy)) continue;

                Vector3 diff = enemy.transform.position - _targetPosition;
                diff.y = 0f;

                if (diff.magnitude <= _splashRadius)
                {
                    damagedEnemies.Add(enemy);
                    enemy.TakeDamage(Damage);
                }
            }

            Destroy(gameObject);
        }

        private void DetachAndStopTrail()
        {
            if (trailVfx == null) return;

            trailVfx.transform.SetParent(null, true);
            trailVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(trailVfx.gameObject, 1.5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
            Vector3 center = _isLaunched ? _targetPosition : transform.position;
            Gizmos.DrawWireSphere(center, _splashRadius > 0f ? _splashRadius : 2.5f);
        }
    }
}
