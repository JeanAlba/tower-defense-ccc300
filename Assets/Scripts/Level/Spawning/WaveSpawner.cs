using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HollowKeep.Enemies;
using HollowKeep.Level.Data;

namespace HollowKeep.Level.Spawning
{
    [DisallowMultipleComponent]
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Configurações")]
        [Tooltip("Configuração do nível e das ondas a serem executadas.")]
        [SerializeField] private LevelData levelData;

        [Tooltip("Referência ao registro de caminhos da cena. Se nulo, buscará na cena.")]
        [SerializeField] private PathRegistry pathRegistry;

        [Tooltip("Se verdadeiro, inicia a contagem da primeira onda automaticamente no Start.")]
        [SerializeField] private bool autoStartOnPlay = true;

        [Header("Debug")]
        [SerializeField] private int currentWaveIndex = -1;
        [SerializeField] private int aliveEnemiesCount = 0;
        [SerializeField] private bool isWaveActive = false;

        public LevelData CurrentLevelData => levelData;
        public int CurrentWaveNumber => currentWaveIndex + 1;
        public int TotalWaves => (levelData != null) ? levelData.TotalWaves : 0;
        public int AliveEnemiesCount => aliveEnemiesCount;
        public bool IsWaveActive => isWaveActive;
        public bool IsInIntermission => _isInIntermission;

        public event Action<int, int> OnWaveStarted;          // (waveIndex, totalWaves)
        public event Action<int> OnWaveCompleted;             // (waveIndex)
        public event Action OnAllWavesCompleted;
        public event Action<float> OnIntermissionTick;         // segundos restantes no intervalo
        public event Action<float, float> OnIntermissionUpdated; // (segundos restantes, duração total)
        public event Action<Enemy> OnEnemySpawned;

        private Coroutine _levelRoutine;
        private readonly List<Coroutine> _activeSpawnCoroutines = new();
        private bool _isInIntermission;
        private bool _skipIntermissionRequested;

        private void Awake()
        {
            if (pathRegistry == null)
            {
                pathRegistry = FindFirstObjectByType<PathRegistry>();
            }
        }

        private void Start()
        {
            if (autoStartOnPlay && levelData != null)
            {
                StartLevel();
            }
        }

        public void SetLevelData(LevelData newLevelData)
        {
            levelData = newLevelData;
        }

        public void StartLevel()
        {
            if (levelData == null)
            {
                Debug.LogError("[WaveSpawner] Nenhum LevelData atribuído!", this);
                return;
            }

            if (pathRegistry == null)
            {
                pathRegistry = FindFirstObjectByType<PathRegistry>();
                if (pathRegistry == null)
                {
                    Debug.LogError("[WaveSpawner] Nenhum PathRegistry encontrado na cena!", this);
                    return;
                }
            }

            if (_levelRoutine != null)
            {
                StopCoroutine(_levelRoutine);
            }

            _levelRoutine = StartCoroutine(RunLevelRoutine());
        }

        /// <summary>
        /// Adianta o início da próxima onda caso um intervalo esteja ativo.
        /// </summary>
        public bool SkipIntermission()
        {
            if (!_isInIntermission)
            {
                return false;
            }

            _skipIntermissionRequested = true;
            return true;
        }

        /// <summary>
        /// Interrompe imediatamente o fluxo de ondas e rotinas de spawn em andamento.
        /// </summary>
        public void StopSpawning()
        {
            if (_levelRoutine != null)
            {
                StopCoroutine(_levelRoutine);
                _levelRoutine = null;
            }

            StopAllCoroutines();
            _activeSpawnCoroutines.Clear();
            _isInIntermission = false;
            _skipIntermissionRequested = false;
            isWaveActive = false;
        }

        private IEnumerator RunLevelRoutine()
        {
            currentWaveIndex = -1;

            for (int w = 0; w < levelData.Waves.Count; w++)
            {
                currentWaveIndex = w;
                WaveData wave = levelData.Waves[w];

                // Intervalo de descanso antes da onda (se configurado)
                float intermissionDuration = (w == 0) ? 3f : levelData.TimeBetweenWaves;
                float intermission = intermissionDuration;
                _isInIntermission = intermission > 0f;
                _skipIntermissionRequested = false;

                int lastReportedSecond = -1;
                while (intermission > 0f && !_skipIntermissionRequested)
                {
                    int currentSecond = Mathf.CeilToInt(intermission);
                    if (currentSecond != lastReportedSecond)
                    {
                        lastReportedSecond = currentSecond;
                        OnIntermissionTick?.Invoke(currentSecond);
                    }

                    OnIntermissionUpdated?.Invoke(Mathf.Max(0f, intermission), intermissionDuration);
                    yield return null;
                    intermission -= Time.deltaTime;
                }

                _isInIntermission = false;
                _skipIntermissionRequested = false;
                OnIntermissionTick?.Invoke(0f);
                OnIntermissionUpdated?.Invoke(0f, intermissionDuration);

                // Execução da onda
                yield return StartCoroutine(ExecuteWaveRoutine(wave, w));

                OnWaveCompleted?.Invoke(w);
                Debug.Log($"[WaveSpawner] Onda {w + 1}/{levelData.TotalWaves} finalizada!");
            }

            isWaveActive = false;
            OnAllWavesCompleted?.Invoke();
            Debug.Log("[WaveSpawner] Todas as ondas do nível foram concluídas com sucesso!");
        }

        private IEnumerator ExecuteWaveRoutine(WaveData wave, int waveIdx)
        {
            isWaveActive = true;
            OnWaveStarted?.Invoke(waveIdx, levelData.TotalWaves);
            Debug.Log($"[WaveSpawner] Iniciando Onda {waveIdx + 1}/{levelData.TotalWaves} (Duração: {wave.Duration}s)");

            _activeSpawnCoroutines.Clear();
            float waveTimer = 0f;

            // Ordena subondas por porcentagem
            List<SubWaveData> sortedSubwaves = new(wave.Subwaves);
            sortedSubwaves.Sort((a, b) => a.WavePercentage.CompareTo(b.WavePercentage));

            int nextSubwaveIdx = 0;

            // Linha do tempo da onda
            while (waveTimer < wave.Duration || nextSubwaveIdx < sortedSubwaves.Count)
            {
                float currentPercentage = (wave.Duration > 0f) ? (waveTimer / wave.Duration) * 100f : 100f;

                while (nextSubwaveIdx < sortedSubwaves.Count && sortedSubwaves[nextSubwaveIdx].WavePercentage <= currentPercentage)
                {
                    SubWaveData subwave = sortedSubwaves[nextSubwaveIdx];
                    foreach (SpawnGroup group in subwave.SpawnGroups)
                    {
                        Coroutine c = StartCoroutine(SpawnGroupRoutine(group));
                        _activeSpawnCoroutines.Add(c);
                    }
                    nextSubwaveIdx++;
                }

                yield return null;
                waveTimer += Time.deltaTime;
            }

            // Aguarda o término de todas as rotinas de instanciação
            foreach (Coroutine c in _activeSpawnCoroutines)
            {
                if (c != null)
                {
                    yield return c;
                }
            }
            _activeSpawnCoroutines.Clear();

            // Aguarda todos os inimigos instanciados morrerem ou alcançarem o final
            while (aliveEnemiesCount > 0)
            {
                yield return new WaitForSeconds(0.25f);
            }

            isWaveActive = false;
        }

        private IEnumerator SpawnGroupRoutine(SpawnGroup group)
        {
            if (group == null || group.EnemyPrefab == null)
            {
                Debug.LogWarning("[WaveSpawner] SpawnGroup ignorado: prefab de inimigo nulo!");
                yield break;
            }

            if (group.TargetPath == null)
            {
                Debug.LogError("[WaveSpawner] SpawnGroup ignorado: TargetPath não atribuído!", this);
                yield break;
            }

            if (!pathRegistry.TryGetPath(group.TargetPath, out EnemyPath path) || path == null || path.WaypointCount == 0)
            {
                Debug.LogError($"[WaveSpawner] Rota '{group.TargetPath.name}' não encontrada ou sem waypoints!", this);
                yield break;
            }

            Waypoint startWaypoint = path.StartWaypoint;

            for (int i = 0; i < group.Quantity; i++)
            {
                Enemy enemy = Instantiate(group.EnemyPrefab, startWaypoint.Position, Quaternion.identity);
                aliveEnemiesCount++;

                enemy.Initialize(path.Waypoints);

                // Subscreve nos eventos do inimigo para decrementar a contagem
                enemy.OnDied += HandleEnemyRemoved;
                enemy.OnReachedEnd += HandleEnemyRemoved;

                OnEnemySpawned?.Invoke(enemy);

                if (i < group.Quantity - 1)
                {
                    yield return new WaitForSeconds(group.Interval);
                }
            }
        }

        private void HandleEnemyRemoved(Enemy enemy)
        {
            if (enemy != null)
            {
                enemy.OnDied -= HandleEnemyRemoved;
                enemy.OnReachedEnd -= HandleEnemyRemoved;
            }

            aliveEnemiesCount = Mathf.Max(0, aliveEnemiesCount - 1);
        }

        private void OnDestroy()
        {
            if (_levelRoutine != null)
            {
                StopCoroutine(_levelRoutine);
            }
            StopAllCoroutines();
        }
    }
}
