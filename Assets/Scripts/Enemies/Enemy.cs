using System;
using System.Collections.Generic;
using UnityEngine;
using HollowKeep.Level;

namespace HollowKeep.Enemies
{
    public abstract class Enemy : MonoBehaviour
    {
        public abstract int MaxHealth { get; }
        public abstract float MoveSpeed { get; }
        public abstract int GoldReward { get; }
        public abstract int DamageToCastle { get; }
        protected virtual float WaypointTolerance => 0.15f;

        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;
        public int CurrentWaypointIndex => _currentWaypointIndex;

        public float DistanceToNextWaypoint
        {
            get
            {
                if (_waypoints == null || _currentWaypointIndex >= _waypoints.Count || _waypoints[_currentWaypointIndex] == null)
                {
                    return 0f;
                }

                Vector3 diff = GetWaypointTargetPosition(_currentWaypointIndex) - transform.position;
                diff.y = 0f;
                return diff.magnitude;
            }
        }

        private static readonly List<Enemy> _activeEnemies = new();
        public static IReadOnlyList<Enemy> ActiveEnemies => _activeEnemies;

        public event Action<Enemy> OnDied;
        public event Action<Enemy> OnReachedEnd;

        private IReadOnlyList<Waypoint> _waypoints;
        private int _currentWaypointIndex;
        private float _lateralOffset;
        private bool _isInitialized;

        protected virtual void OnEnable()
        {
            if (!_activeEnemies.Contains(this))
            {
                _activeEnemies.Add(this);
            }
        }

        protected virtual void OnDisable()
        {
            _activeEnemies.Remove(this);
        }

        protected virtual void Awake()
        {
            CurrentHealth = MaxHealth;
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
            _lateralOffset = UnityEngine.Random.Range(-1f, 1f);
            transform.position = GetWaypointTargetPosition(0);
            _isInitialized = true;
        }

        private Vector3 GetWaypointTargetPosition(int index)
        {
            return EnemyPath.GetOffsetPosition(_waypoints, index, _lateralOffset);
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

            Vector3 targetPos = GetWaypointTargetPosition(_currentWaypointIndex);
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f; // Mantém no plano XZ se for terreno plano

            float step = MoveSpeed * Time.deltaTime;
            if (toTarget.magnitude <= WaypointTolerance)
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
