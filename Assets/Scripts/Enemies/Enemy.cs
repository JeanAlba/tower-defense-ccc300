using System;
using System.Collections.Generic;
using UnityEngine;
using HollowKeep.Level;

namespace HollowKeep.Enemies
{
    public abstract class Enemy : MonoBehaviour
    {
        [Header("Atributos")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private int goldReward = 15;
        [SerializeField] private int damageToCastle = 1;
        [SerializeField] private float waypointTolerance = 0.15f;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public int GoldReward => goldReward;
        public int DamageToCastle => damageToCastle;
        public bool IsDead => CurrentHealth <= 0;

        public event Action<Enemy> OnDied;
        public event Action<Enemy> OnReachedEnd;

        private IReadOnlyList<Waypoint> _waypoints;
        private int _currentWaypointIndex;
        private bool _isInitialized;

        protected virtual void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public virtual void Initialize(IReadOnlyList<Waypoint> waypoints)
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                Debug.LogError($"[Enemy] {gameObject.name} inicializado com lista de waypoints vazia ou nula!");
                return;
            }

            _waypoints = waypoints;
            _currentWaypointIndex = 0;
            transform.position = _waypoints[0].Position;
            _isInitialized = true;
        }

        protected virtual void Update()
        {
            if (!_isInitialized || IsDead) return;

            MoveAlongPath();
        }

        protected virtual void MoveAlongPath()
        {
            if (_waypoints == null || _currentWaypointIndex >= _waypoints.Count)
            {
                ReachEnd();
                return;
            }

            Waypoint target = _waypoints[_currentWaypointIndex];
            if (target == null)
            {
                _currentWaypointIndex++;
                return;
            }

            Vector3 targetPos = target.Position;
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f; // Mantém no plano XZ se for terreno plano

            float step = moveSpeed * Time.deltaTime;
            if (toTarget.magnitude <= waypointTolerance)
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex >= _waypoints.Count)
                {
                    ReachEnd();
                    return;
                }
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, step);
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, step * 10f);
                }
            }
        }

        public virtual void TakeDamage(int damage)
        {
            if (IsDead) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            OnDied?.Invoke(this);
            Destroy(gameObject);
        }

        protected virtual void ReachEnd()
        {
            OnReachedEnd?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
