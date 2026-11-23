using UnityEngine;
using HollowKeep.Enemies;
using HollowKeep.VFX;

namespace HollowKeep.Projectiles
{
    public class DirectProjectile : Projectile
    {
        [Header("Movimento Direto")]
        [SerializeField] private float speed = 18f;
        [SerializeField] private float hitDistance = 0.25f;
        [SerializeField] private float targetHeightOffset = 0.5f;

        [Header("Efeitos Visuais")]
        [SerializeField] private ParticleSystem hitVfxPrefab;

        private Enemy _target;
        private Vector3 _lastTargetPosition;
        private bool _isLaunched;

        public void Launch(Enemy target, int damage)
        {
            InitializeDamage(damage);
            _target = target;
            _lastTargetPosition = GetTargetAimPoint(target);
            _isLaunched = true;
        }

        protected override void Update()
        {
            base.Update();

            if (!_isLaunched) return;

            if (_target != null && !_target.IsDead)
            {
                _lastTargetPosition = GetTargetAimPoint(_target);
            }

            Vector3 toTarget = _lastTargetPosition - transform.position;
            float distanceThisFrame = speed * Time.deltaTime;

            if (toTarget.magnitude <= Mathf.Max(hitDistance, distanceThisFrame))
            {
                OnHit();
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * distanceThisFrame;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void OnHit()
        {
            if (_target != null && !_target.IsDead)
            {
                _target.TakeDamage(Damage);
                Quaternion hitRotation = transform.forward.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(-transform.forward)
                    : Quaternion.identity;
                VfxSpawner.Spawn(hitVfxPrefab, transform.position, hitRotation);
            }

            Destroy(gameObject);
        }

        private Vector3 GetTargetAimPoint(Enemy target)
        {
            if (target == null) return transform.position;
            return target.transform.position + Vector3.up * targetHeightOffset;
        }
    }
}
