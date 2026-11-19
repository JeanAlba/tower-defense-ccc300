using UnityEngine;
using HollowKeep.Enemies;

namespace HollowKeep.Projectiles
{
    public class ParabolicProjectile : Projectile
    {
        [Header("Trajetória Parabólica")]
        [SerializeField] private float flightDuration = 0.9f;
        [SerializeField] private float arcHeight = 3.5f;

        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private float _splashRadius;
        private float _elapsed;
        private bool _isLaunched;

        public void Launch(Vector3 targetPosition, int damage, float splashRadius)
        {
            InitializeDamage(damage);
            _startPosition = transform.position;
            _targetPosition = targetPosition;
            _splashRadius = Mathf.Max(0.1f, splashRadius);
            _elapsed = 0f;
            _isLaunched = true;
        }

        protected override void Update()
        {
            base.Update();

            if (!_isLaunched) return;

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
            // Percorre de trás para frente porque TakeDamage pode destruir o inimigo e removê-lo da lista.
            var enemies = Enemy.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || enemy.IsDead) continue;

                Vector3 diff = enemy.transform.position - _targetPosition;
                diff.y = 0f;

                if (diff.magnitude <= _splashRadius)
                {
                    enemy.TakeDamage(Damage);
                }
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
            Vector3 center = _isLaunched ? _targetPosition : transform.position;
            Gizmos.DrawWireSphere(center, _splashRadius > 0f ? _splashRadius : 2.5f);
        }
    }
}
