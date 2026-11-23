using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HollowKeep.Level;
using HollowKeep.VFX;

namespace HollowKeep.Enemies
{
    public class Enemy : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private EnemyStatsData stats;

        protected virtual int DefaultMaxHealth => 60;
        protected virtual float DefaultMoveSpeed => 3.5f;
        protected virtual int DefaultGoldReward => 10;
        protected virtual int DefaultDamageToCastle => 1;
        protected virtual float WaypointTolerance => 0.15f;

        public EnemyStatsData Stats => stats;
        public int MaxHealth => Mathf.Max(1, Mathf.RoundToInt((stats != null ? stats.MaxHealth : DefaultMaxHealth) * _healthMultiplier));
        public float MoveSpeed => Mathf.Max(0.1f, (stats != null ? stats.MoveSpeed : DefaultMoveSpeed) * _speedMultiplier);
        public int GoldReward => Mathf.Max(0, Mathf.RoundToInt((stats != null ? stats.GoldReward : DefaultGoldReward) * _goldMultiplier));
        public int DamageToCastle => stats != null ? stats.DamageToCastle : DefaultDamageToCastle;

        private float _healthMultiplier = 1f;
        private float _speedMultiplier = 1f;
        private float _goldMultiplier = 1f;

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
        private static readonly Queue<GameObject> _corpses = new();
        private static readonly int DieTriggerHash = Animator.StringToHash("Die");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static IReadOnlyList<Enemy> ActiveEnemies => _activeEnemies;

        public event Action<Enemy> OnDied;
        public event Action<Enemy> OnReachedEnd;
        public event Action<int, int> OnHealthChanged;

        [Header("Feedback Visual")]
        [SerializeField] private bool showHealthBar = true;
        [Min(0.2f)]
        [SerializeField] private float healthBarHeight = 2.1f;

        [Header("Animação e Cadáver")]
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystem deathDustVfxPrefab;
        [SerializeField] private bool keepCorpseOnDeath = true;
        [Tooltip("Segundos antes de destruir o inimigo quando 'keepCorpseOnDeath' estiver desmarcado.")]
        [Min(0f)]
        [SerializeField] private float deathDestroyDelay = 1.5f;
        [Tooltip("Tempo (em segundos) que o cadáver permanece visível no mapa antes de afundar.")]
        [Min(0f)]
        [SerializeField] private float corpseLifetime = 20f;
        [SerializeField] private Color corpseTintColor = new(0.38f, 0.38f, 0.42f, 1f);
        [Min(0f)]
        [SerializeField] private float corpseTintDuration = 0.5f;
        [Min(0.1f)]
        [SerializeField] private float corpseSinkDuration = 1.2f;
        [Min(0.1f)]
        [SerializeField] private float corpseSinkDepth = 1.2f;
        [Tooltip("Limite máximo de cadáveres no mapa ao mesmo tempo (0 = sem limite).")]
        [Min(0)]
        [SerializeField] private int maxCorpsesOnMap = 40;

        private IReadOnlyList<Waypoint> _waypoints;
        private int _currentWaypointIndex;
        private float _lateralOffset;
        private bool _isInitialized;
        private bool _hasDied;

        protected virtual void OnEnable()
        {
            if (_hasDied || IsDead) return;

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
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (showHealthBar)
            {
                EnsureHealthBar();
            }
        }

        private void EnsureHealthBar()
        {
            EnemyHealthBar existing = GetComponentInChildren<EnemyHealthBar>();
            if (existing == null)
            {
                GameObject barObj = new GameObject("HealthBar");
                barObj.transform.SetParent(transform, false);
                existing = barObj.AddComponent<EnemyHealthBar>();
            }

            existing.Initialize(this, healthBarHeight);
        }

        /// <summary>
        /// Aplica multiplicadores de dificuldade em runtime e sincroniza a vida atual.
        /// </summary>
        public void ApplyDifficulty(float healthMultiplier, float speedMultiplier, float goldMultiplier)
        {
            _healthMultiplier = Mathf.Max(0.1f, healthMultiplier);
            _speedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            _goldMultiplier = Mathf.Max(0f, goldMultiplier);
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// Prevê a posição futura deste inimigo ao longo da rota após 'seconds' segundos.
        /// </summary>
        public Vector3 PredictPositionAfter(float seconds)
        {
            if (seconds <= 0f || IsDead || _waypoints == null || _currentWaypointIndex >= _waypoints.Count)
            {
                return transform.position;
            }

            float remainingDistance = MoveSpeed * seconds;
            Vector3 simPos = transform.position;

            for (int i = _currentWaypointIndex; i < _waypoints.Count; i++)
            {
                if (_waypoints[i] == null) continue;

                Vector3 targetPos = GetWaypointTargetPosition(i);
                Vector3 toTarget = targetPos - simPos;
                toTarget.y = 0f;
                float segDist = toTarget.magnitude;

                if (segDist <= 0.0001f)
                {
                    simPos = targetPos;
                    continue;
                }

                if (segDist >= remainingDistance)
                {
                    Vector3 result = simPos + toTarget.normalized * remainingDistance;
                    result.y = Mathf.Lerp(simPos.y, targetPos.y, remainingDistance / segDist);
                    return result;
                }

                remainingDistance -= segDist;
                simPos = targetPos;
            }

            return simPos;
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
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            if (_hasDied) return;
            _hasDied = true;

            OnDied?.Invoke(this);
            DisableColliders();

            if (animator != null)
            {
                animator.SetTrigger(DieTriggerHash);
            }

            VfxSpawner.Spawn(deathDustVfxPrefab, transform.position, Quaternion.identity);

            // Desabilitar o script dispara OnDisable(), removendo de ActiveEnemies e parando o Update.
            enabled = false;

            if (!keepCorpseOnDeath)
            {
                Destroy(gameObject, deathDestroyDelay);
                return;
            }

            RegisterCorpse(gameObject, maxCorpsesOnMap);
            StartCoroutine(CorpseLifecycleRoutine());
        }

        private IEnumerator CorpseLifecycleRoutine()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            Color[] baseColors = CaptureBaseColors(renderers);
            MaterialPropertyBlock mpb = new();

            if (corpseTintDuration > 0f)
            {
                float tintElapsed = 0f;
                while (tintElapsed < corpseTintDuration)
                {
                    tintElapsed += Time.deltaTime;
                    ApplyCorpseTint(renderers, baseColors, mpb, Mathf.Clamp01(tintElapsed / corpseTintDuration));
                    yield return null;
                }
            }
            else
            {
                ApplyCorpseTint(renderers, baseColors, mpb, 1f);
            }

            float waitTime = Mathf.Max(0f, corpseLifetime - corpseTintDuration);
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }

            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.down * corpseSinkDepth;
            float sinkElapsed = 0f;

            while (sinkElapsed < corpseSinkDuration)
            {
                sinkElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(sinkElapsed / corpseSinkDuration);
                transform.position = Vector3.Lerp(startPos, endPos, t * t);
                yield return null;
            }

            Destroy(gameObject);
        }

        private static Color[] CaptureBaseColors(Renderer[] renderers)
        {
            Color[] colors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                Material mat = renderers[i].sharedMaterial;
                if (mat != null && mat.HasProperty(BaseColorId))
                {
                    colors[i] = mat.GetColor(BaseColorId);
                }
                else if (mat != null && mat.HasProperty(ColorId))
                {
                    colors[i] = mat.GetColor(ColorId);
                }
                else
                {
                    colors[i] = Color.white;
                }
            }
            return colors;
        }

        private void ApplyCorpseTint(Renderer[] renderers, Color[] baseColors, MaterialPropertyBlock mpb, float t)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (rend == null) continue;

                Color tinted = Color.Lerp(baseColors[i], corpseTintColor, t);
                rend.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, tinted);
                mpb.SetColor(ColorId, tinted);
                rend.SetPropertyBlock(mpb);
            }
        }

        private void DisableColliders()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private static void RegisterCorpse(GameObject corpse, int maxCorpses)
        {
            if (maxCorpses <= 0) return;

            while (_corpses.Count > 0 && _corpses.Peek() == null)
            {
                _corpses.Dequeue();
            }

            _corpses.Enqueue(corpse);

            while (_corpses.Count > maxCorpses)
            {
                GameObject oldest = _corpses.Dequeue();
                if (oldest != null)
                {
                    Destroy(oldest);
                }
            }
        }

        protected virtual void ReachEnd()
        {
            OnReachedEnd?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
