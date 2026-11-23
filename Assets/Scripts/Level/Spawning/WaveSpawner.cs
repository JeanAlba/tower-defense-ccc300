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

        [Tooltip("Tempo de preparação (em segundos) antes da primeira onda iniciar automaticamente caso o jogador não clique para iniciar.")]
        [Min(0f)]
        [SerializeField] private float initialPreparationDuration = 10f;

        [Tooltip("Porcentagem (0.5 a 1.0) da onda a partir da qual o botão de adiantar próxima onda é liberado (estilo Kingdom Rush).")]
        [Range(0.5f, 1f)]
        [SerializeField] private float earlyCallWaveThreshold = 0.8f;

        [Tooltip("Quando todos os inimigos da tela morrem antes da próxima subonda, reduz a espera restante para no máximo este tempo (em segundos).")]
        [Min(0.1f)]
        [SerializeField] private float emptyFieldAdvanceLeadSeconds = 0.75f;

        [Header("Dificuldade")]
        [Tooltip("Perfil padrão usado quando a cena é iniciada diretamente sem seleção prévia no menu.")]
        [SerializeField] private DifficultyProfile defaultDifficulty;

        [Header("Debug")]
        [SerializeField] private int currentWaveIndex = -1;
        [SerializeField] private int aliveEnemiesCount = 0;
        [SerializeField] private bool isWaveActive = false;

        public LevelData CurrentLevelData => levelData;
        public DifficultyProfile ActiveDifficulty => GameSessionConfig.Resolve(defaultDifficulty);
        public int CurrentWaveNumber => currentWaveIndex + 1;
        public int TotalWaves => (levelData != null) ? levelData.TotalWaves : 0;
        public int AliveEnemiesCount => aliveEnemiesCount;
        public bool IsWaveActive => isWaveActive;
        public bool IsInIntermission => _isInIntermission;
        public float IntermissionRemainingSeconds => _intermissionRemaining;

        public event Action<int, int> OnWaveStarted;          // (waveIndex, totalWaves)
        public event Action<int> OnWaveCompleted;             // (waveIndex)
        public event Action OnAllWavesCompleted;
        public event Action<float> OnIntermissionTick;         // segundos restantes no intervalo
        public event Action<float, float> OnIntermissionUpdated; // (segundos restantes, duração total)
        public event Action<Enemy> OnEnemySpawned;

        private sealed class WaveRunState
        {
            public int ActiveSpawns;
        }

        private Coroutine _levelRoutine;
        private bool _isInIntermission;
        private bool _skipIntermissionRequested;
        private bool _suppressAutoStart;
        private float _intermissionRemaining;

        private void Awake()
        {
            if (pathRegistry == null)
            {
                pathRegistry = FindFirstObjectByType<PathRegistry>();
            }
        }

        private void Start()
        {
            if (autoStartOnPlay && !_suppressAutoStart && levelData != null)
            {
                StartLevel();
            }
        }

        /// <summary>
        /// Impede o início automático das ondas no Start quando há uma tela pré-fase aguardando confirmação.
        /// </summary>
        public void SuppressAutoStart()
        {
            _suppressAutoStart = true;
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
            _isInIntermission = false;
            _skipIntermissionRequested = false;
            _intermissionRemaining = 0f;
            isWaveActive = false;
        }

        private IEnumerator RunLevelRoutine()
        {
            currentWaveIndex = -1;

            if (levelData.Waves.Count > 0)
            {
                yield return StartCoroutine(RunInitialPreparationRoutine(initialPreparationDuration));
            }

            for (int w = 0; w < levelData.Waves.Count; w++)
            {
                currentWaveIndex = w;
                WaveData wave = levelData.Waves[w];
                bool hasNextWave = (w + 1) < levelData.Waves.Count;

                yield return StartCoroutine(ExecuteWaveRoutine(wave, w, hasNextWave));

                OnWaveCompleted?.Invoke(w);
                Debug.Log($"[WaveSpawner] Onda {w + 1}/{levelData.TotalWaves} finalizada!");
            }

            // Garante que todos os inimigos (inclusive sobrepostos) morreram ou alcançaram o castelo antes da vitória
            while (aliveEnemiesCount > 0)
            {
                yield return new WaitForSeconds(0.25f);
            }

            isWaveActive = false;
            OnAllWavesCompleted?.Invoke();
            Debug.Log("[WaveSpawner] Todas as ondas do nível foram concluídas com sucesso!");
        }

        private IEnumerator RunInitialPreparationRoutine(float duration)
        {
            float intermission = duration;
            _isInIntermission = intermission > 0f;
            _skipIntermissionRequested = false;
            _intermissionRemaining = Mathf.Max(0f, intermission);

            int lastReportedSecond = -1;
            while (intermission > 0f && !_skipIntermissionRequested)
            {
                _intermissionRemaining = Mathf.Max(0f, intermission);
                int currentSecond = Mathf.CeilToInt(intermission);
                if (currentSecond != lastReportedSecond)
                {
                    lastReportedSecond = currentSecond;
                    OnIntermissionTick?.Invoke(currentSecond);
                }

                OnIntermissionUpdated?.Invoke(_intermissionRemaining, duration);
                yield return null;
                intermission -= Time.deltaTime;
            }

            ClearIntermissionState(duration);
        }

        private IEnumerator ExecuteWaveRoutine(WaveData wave, int waveIdx, bool hasNextWave)
        {
            isWaveActive = true;
            OnWaveStarted?.Invoke(waveIdx, levelData.TotalWaves);
            Debug.Log($"[WaveSpawner] Iniciando Onda {waveIdx + 1}/{levelData.TotalWaves} (Duração: {wave.Duration}s)");

            WaveRunState waveState = new();
            float waveTimer = 0f;

            // Ordena subondas por porcentagem
            List<SubWaveData> sortedSubwaves = new(wave.Subwaves);
            sortedSubwaves.Sort((a, b) => a.WavePercentage.CompareTo(b.WavePercentage));

            DifficultyProfile difficulty = ActiveDifficulty;
            float intermissionDuration = difficulty != null
                ? difficulty.GetScaledTimeBetweenWaves(levelData.TimeBetweenWaves)
                : levelData.TimeBetweenWaves;
            float earlyCallStartTime = wave.Duration * earlyCallWaveThreshold;

            int nextSubwaveIdx = 0;
            bool earlyIntermissionStarted = false;
            float intermissionRemaining = 0f;
            int lastReportedSecond = -1;

            while (true)
            {
                float currentPercentage = (wave.Duration > 0f) ? (waveTimer / wave.Duration) * 100f : 100f;

                while (nextSubwaveIdx < sortedSubwaves.Count && sortedSubwaves[nextSubwaveIdx].WavePercentage <= currentPercentage)
                {
                    LaunchSubwave(sortedSubwaves[nextSubwaveIdx], waveState);
                    nextSubwaveIdx++;
                }

                bool allSubwavesTriggered = nextSubwaveIdx >= sortedSubwaves.Count;
                bool waveFieldClear = waveState.ActiveSpawns == 0 && aliveEnemiesCount == 0;

                // Se todos os inimigos morreram antes da próxima subonda, evita tela vazia prolongada
                if (!allSubwavesTriggered && waveFieldClear)
                {
                    float nextTriggerTime = (sortedSubwaves[nextSubwaveIdx].WavePercentage / 100f) * wave.Duration;
                    float fastForwardTime = nextTriggerTime - emptyFieldAdvanceLeadSeconds;
                    if (fastForwardTime > waveTimer)
                    {
                        waveTimer = fastForwardTime;
                    }
                }

                // Estilo Kingdom Rush: libera botão de adiantar onda aos ~80% da duração (ou quando limpar tudo)
                if (hasNextWave && !earlyIntermissionStarted && (waveTimer >= earlyCallStartTime || (allSubwavesTriggered && waveFieldClear)))
                {
                    earlyIntermissionStarted = true;
                    _skipIntermissionRequested = false;
                    intermissionRemaining = intermissionDuration;
                    _intermissionRemaining = Mathf.Max(0f, intermissionRemaining);
                    _isInIntermission = intermissionDuration > 0f;
                    lastReportedSecond = -1;

                    if (intermissionDuration <= 0f)
                    {
                        LaunchRemainingSubwaves(sortedSubwaves, ref nextSubwaveIdx, waveState);
                        ClearIntermissionState(intermissionDuration);
                        break;
                    }
                }

                if (earlyIntermissionStarted)
                {
                    if (_skipIntermissionRequested || intermissionRemaining <= 0f)
                    {
                        LaunchRemainingSubwaves(sortedSubwaves, ref nextSubwaveIdx, waveState);
                        ClearIntermissionState(intermissionDuration);
                        break;
                    }

                    _intermissionRemaining = Mathf.Max(0f, intermissionRemaining);
                    int currentSecond = Mathf.CeilToInt(intermissionRemaining);
                    if (currentSecond != lastReportedSecond)
                    {
                        lastReportedSecond = currentSecond;
                        OnIntermissionTick?.Invoke(currentSecond);
                    }

                    OnIntermissionUpdated?.Invoke(_intermissionRemaining, intermissionDuration);
                    intermissionRemaining -= Time.deltaTime;
                }
                else if (!hasNextWave && allSubwavesTriggered && waveFieldClear)
                {
                    break;
                }

                yield return null;
                waveTimer += Time.deltaTime;
            }
        }

        private void LaunchSubwave(SubWaveData subwave, WaveRunState waveState)
        {
            foreach (SpawnGroup group in subwave.SpawnGroups)
            {
                waveState.ActiveSpawns++;
                StartCoroutine(SpawnGroupRoutine(group, waveState));
            }
        }

        private void LaunchRemainingSubwaves(List<SubWaveData> sortedSubwaves, ref int nextSubwaveIdx, WaveRunState waveState)
        {
            while (nextSubwaveIdx < sortedSubwaves.Count)
            {
                LaunchSubwave(sortedSubwaves[nextSubwaveIdx], waveState);
                nextSubwaveIdx++;
            }
        }

        private void ClearIntermissionState(float totalDuration)
        {
            _isInIntermission = false;
            _skipIntermissionRequested = false;
            _intermissionRemaining = 0f;
            OnIntermissionTick?.Invoke(0f);
            OnIntermissionUpdated?.Invoke(0f, totalDuration);
        }

        private IEnumerator SpawnGroupRoutine(SpawnGroup group, WaveRunState waveState)
        {
            try
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
                DifficultyProfile difficulty = ActiveDifficulty;
                int quantity = difficulty != null ? difficulty.GetScaledQuantity(group.Quantity) : group.Quantity;
                float interval = difficulty != null ? difficulty.GetScaledInterval(group.Interval) : group.Interval;

                for (int i = 0; i < quantity; i++)
                {
                    Enemy enemy = Instantiate(group.EnemyPrefab, startWaypoint.Position, Quaternion.identity);
                    aliveEnemiesCount++;

                    if (difficulty != null)
                    {
                        enemy.ApplyDifficulty(
                            difficulty.EnemyHealthMultiplier,
                            difficulty.EnemySpeedMultiplier,
                            difficulty.GoldRewardMultiplier
                        );
                    }

                    enemy.Initialize(path.Waypoints);

                    // Subscreve nos eventos do inimigo para decrementar a contagem
                    enemy.OnDied += HandleEnemyRemoved;
                    enemy.OnReachedEnd += HandleEnemyRemoved;

                    OnEnemySpawned?.Invoke(enemy);

                    if (i < quantity - 1)
                    {
                        yield return new WaitForSeconds(interval);
                    }
                }
            }
            finally
            {
                if (waveState != null)
                {
                    waveState.ActiveSpawns = Mathf.Max(0, waveState.ActiveSpawns - 1);
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
