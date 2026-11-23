using UnityEngine;

namespace HollowKeep.Projectiles
{
    public abstract class Projectile : MonoBehaviour
    {
        [Header("Segurança")]
        [Tooltip("Tempo máximo de vida (em segundos) antes de destruir o projétil automaticamente.")]
        [SerializeField] private float maxLifetime = 6f;

        protected int Damage { get; private set; }

        private float _lifeTimer;

        protected void InitializeDamage(int damage)
        {
            Damage = Mathf.Max(0, damage);
            _lifeTimer = 0f;
        }

        protected virtual void Update()
        {
            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= maxLifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
