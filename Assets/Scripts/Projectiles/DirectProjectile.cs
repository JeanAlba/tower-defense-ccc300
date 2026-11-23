using UnityEngine;
using HollowKeep.Audio;
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
        private bool _hasHit;

        private void Awake()
        {
            EnsurePhysicsComponents();
        }

        private void EnsurePhysicsComponents()
        {
            if (!TryGetComponent(out Collider col))
            {
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = Mathf.Max(0.2f, hitDistance);
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
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        public void Launch(Enemy target, int damage)
        {
            InitializeDamage(damage);
            _target = target;
            _lastTargetPosition = GetTargetAimPoint(target);
            _isLaunched = true;
            _hasHit = false;
        }

        protected override void Update()
        {
            base.Update();

            if (!_isLaunched || _hasHit) return;

            if (_target != null && !_target.IsDead)
            {
                _lastTargetPosition = GetTargetAimPoint(_target);
            }

            Vector3 toTarget = _lastTargetPosition - transform.position;
            float distanceThisFrame = speed * Time.deltaTime;

            if (toTarget.magnitude <= Mathf.Max(hitDistance, distanceThisFrame))
            {
                OnHit(_target);
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * distanceThisFrame;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isLaunched || _hasHit || other == null) return;

            Enemy hitEnemy = other.GetComponentInParent<Enemy>();
            if (hitEnemy == null || hitEnemy.IsDead) return;

            if (_target == null || _target.IsDead || hitEnemy == _target)
            {
                OnHit(hitEnemy);
            }
        }

        private void OnHit(Enemy hitEnemy)
        {
            if (_hasHit) return;
            _hasHit = true;

            Enemy victim = (hitEnemy != null && !hitEnemy.IsDead) ? hitEnemy : _target;
            if (victim != null && !victim.IsDead)
            {
                victim.TakeDamage(Damage);
                Quaternion hitRotation = transform.forward.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(-transform.forward)
                    : Quaternion.identity;
                VfxSpawner.Spawn(hitVfxPrefab, transform.position, hitRotation);
                GameAudioManager.PlayArrowHit();
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
