using System;
using System.Collections.Generic;
using HollowKeep.Enemies;
using HollowKeep.Level.Spawning;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowKeep.UI
{
    /// <summary>
    /// Controla a HUD em jogo: onda, ouro, vidas do castelo, avanço de onda e popup de pausa.
    /// </summary>
    public class GameHUDController : MonoBehaviour
    {
        [Header("Referências de Cena")]
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Indicadores (Superior Esquerdo)")]
        [SerializeField] private TMPro.TMP_Text waveText;
        [SerializeField] private TMPro.TMP_Text goldText;
        [SerializeField] private TMPro.TMP_Text livesText;

        [Header("Próxima Onda (Inferior Direito)")]
        [SerializeField] private UnityEngine.UI.Button advanceWaveButton;
        [SerializeField] private UnityEngine.UI.Image advanceWaveTimerFill;
        [SerializeField] private TMPro.TMP_Text advanceWaveLabelText;
        [SerializeField] private TMPro.TMP_Text advanceWaveTimerText;

        [Header("Pausa (Superior Direito e Popup)")]
        [SerializeField] private UnityEngine.UI.Button pauseButton;
        [SerializeField] private GameObject pausePopup;
        [SerializeField] private UnityEngine.UI.Button resumeButton;
        [SerializeField] private UnityEngine.UI.Button restartButton;
        [SerializeField] private UnityEngine.UI.Button mainMenuButton;

        [Header("Derrota (Game Over)")]
        [SerializeField] private GameObject gameOverPopup;
        [SerializeField] private UnityEngine.UI.Button gameOverRetryButton;
        [SerializeField] private UnityEngine.UI.Button gameOverExitButton;

        [Header("Configuração Inicial")]
        [Min(1)]
        [SerializeField] private int startingLives = 20;

        private readonly HashSet<Enemy> _trackedEnemies = new();
        private int _currentGold;
        private int _currentLives;
        private bool _isPaused;
        private bool _isGameOver;
        private bool _allWavesCompleted;

        public int CurrentGold => _currentGold;
        public bool IsPaused => _isPaused;
        public bool IsGameOver => _isGameOver;
        public bool IsInputBlocked => _isPaused || _isGameOver;

        public event Action<int> OnGoldChanged;

        private void Awake()
        {
            if (waveSpawner == null)
            {
                waveSpawner = FindFirstObjectByType<WaveSpawner>();
            }
        }

        private void OnEnable()
        {
            if (waveSpawner != null)
            {
                waveSpawner.OnWaveStarted += HandleWaveStarted;
                waveSpawner.OnIntermissionTick += HandleIntermissionTick;
                waveSpawner.OnIntermissionUpdated += HandleIntermissionUpdated;
                waveSpawner.OnAllWavesCompleted += HandleAllWavesCompleted;
                waveSpawner.OnEnemySpawned += HandleEnemySpawned;
            }

            if (advanceWaveButton != null)
            {
                advanceWaveButton.onClick.AddListener(HandleAdvanceWaveClicked);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(HandlePauseClicked);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(HandleResumeClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
            }

            if (gameOverRetryButton != null)
            {
                gameOverRetryButton.onClick.AddListener(HandleRestartClicked);
            }

            if (gameOverExitButton != null)
            {
                gameOverExitButton.onClick.AddListener(HandleMainMenuClicked);
            }
        }

        private void Start()
        {
            _currentGold = (waveSpawner != null && waveSpawner.CurrentLevelData != null)
                ? waveSpawner.CurrentLevelData.StartingGold
                : 0;
            _currentLives = startingLives;
            _isGameOver = false;
            _allWavesCompleted = false;

            if (gameOverPopup != null)
            {
                gameOverPopup.SetActive(false);
            }

            if (pauseButton != null)
            {
                pauseButton.interactable = true;
            }

            SetPaused(false);
            UpdateWaveDisplay(0, waveSpawner != null ? waveSpawner.TotalWaves : 0);
            RefreshResourceDisplays();
            OnGoldChanged?.Invoke(_currentGold);

            if (waveSpawner != null && waveSpawner.IsInIntermission)
            {
                SetAdvanceWaveState(true, "--");
            }
            else
            {
                SetAdvanceWaveState(false, "--");
            }
        }

        private void OnDisable()
        {
            if (waveSpawner != null)
            {
                waveSpawner.OnWaveStarted -= HandleWaveStarted;
                waveSpawner.OnIntermissionTick -= HandleIntermissionTick;
                waveSpawner.OnIntermissionUpdated -= HandleIntermissionUpdated;
                waveSpawner.OnAllWavesCompleted -= HandleAllWavesCompleted;
                waveSpawner.OnEnemySpawned -= HandleEnemySpawned;
            }

            if (advanceWaveButton != null)
            {
                advanceWaveButton.onClick.RemoveListener(HandleAdvanceWaveClicked);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(HandlePauseClicked);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(HandleResumeClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
            }

            if (gameOverRetryButton != null)
            {
                gameOverRetryButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (gameOverExitButton != null)
            {
                gameOverExitButton.onClick.RemoveListener(HandleMainMenuClicked);
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;

            foreach (Enemy enemy in _trackedEnemies)
            {
                if (enemy != null)
                {
                    enemy.OnDied -= HandleEnemyDied;
                    enemy.OnReachedEnd -= HandleEnemyReachedEnd;
                }
            }
            _trackedEnemies.Clear();
        }

        public void SetGold(int amount)
        {
            _currentGold = Mathf.Max(0, amount);
            RefreshResourceDisplays();
            OnGoldChanged?.Invoke(_currentGold);
        }

        public bool CanAfford(int amount)
        {
            return _currentGold >= Mathf.Max(0, amount);
        }

        public bool TrySpendGold(int amount)
        {
            if (amount < 0 || _currentGold < amount)
            {
                return false;
            }

            _currentGold -= amount;
            RefreshResourceDisplays();
            OnGoldChanged?.Invoke(_currentGold);
            return true;
        }

        public void SetLives(int amount)
        {
            _currentLives = Mathf.Max(0, amount);
            RefreshResourceDisplays();

            if (_currentLives <= 0 && !_isGameOver)
            {
                TriggerGameOver();
            }
        }

        private void HandleWaveStarted(int waveIndex, int totalWaves)
        {
            UpdateWaveDisplay(waveIndex + 1, totalWaves);
            SetAdvanceWaveState(false, "--");
        }

        private void HandleIntermissionTick(float secondsRemaining)
        {
            if (_allWavesCompleted)
            {
                return;
            }

            if (secondsRemaining > 0f)
            {
                SetAdvanceWaveState(true, $"{Mathf.CeilToInt(secondsRemaining)}s");
            }
            else
            {
                SetAdvanceWaveState(false, "--");
            }
        }

        private void HandleIntermissionUpdated(float secondsRemaining, float totalDuration)
        {
            if (_allWavesCompleted || advanceWaveTimerFill == null)
            {
                return;
            }

            advanceWaveTimerFill.fillAmount = (totalDuration > 0f)
                ? Mathf.Clamp01(secondsRemaining / totalDuration)
                : 0f;
        }

        private void HandleAllWavesCompleted()
        {
            _allWavesCompleted = true;

            if (advanceWaveButton != null)
            {
                advanceWaveButton.interactable = false;
            }

            if (advanceWaveLabelText != null)
            {
                advanceWaveLabelText.text = "CONCLUÍDAS";
            }

            if (advanceWaveTimerText != null)
            {
                advanceWaveTimerText.text = "OK";
            }
        }

        private void HandleEnemySpawned(Enemy enemy)
        {
            if (enemy == null || !_trackedEnemies.Add(enemy))
            {
                return;
            }

            enemy.OnDied += HandleEnemyDied;
            enemy.OnReachedEnd += HandleEnemyReachedEnd;
        }

        private void HandleEnemyDied(Enemy enemy)
        {
            UnsubscribeEnemy(enemy);
            if (enemy != null)
            {
                _currentGold += enemy.GoldReward;
                RefreshResourceDisplays();
                OnGoldChanged?.Invoke(_currentGold);
            }
        }

        private void HandleEnemyReachedEnd(Enemy enemy)
        {
            UnsubscribeEnemy(enemy);
            if (enemy != null)
            {
                _currentLives = Mathf.Max(0, _currentLives - enemy.DamageToCastle);
                RefreshResourceDisplays();

                if (_currentLives <= 0 && !_isGameOver)
                {
                    TriggerGameOver();
                }
            }
        }

        private void UnsubscribeEnemy(Enemy enemy)
        {
            if (enemy != null)
            {
                enemy.OnDied -= HandleEnemyDied;
                enemy.OnReachedEnd -= HandleEnemyReachedEnd;
                _trackedEnemies.Remove(enemy);
            }
        }

        private void HandleAdvanceWaveClicked()
        {
            if (_isPaused || _isGameOver || waveSpawner == null)
            {
                return;
            }

            waveSpawner.SkipIntermission();
        }

        private void HandlePauseClicked()
        {
            if (_isGameOver)
            {
                return;
            }

            SetPaused(!_isPaused);
        }

        private void HandleResumeClicked()
        {
            SetPaused(false);
        }

        private void HandleRestartClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void HandleMainMenuClicked()
        {
            Time.timeScale = 1f;

            if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        private void SetPaused(bool paused)
        {
            if (_isGameOver)
            {
                return;
            }

            _isPaused = paused;
            Time.timeScale = _isPaused ? 0f : 1f;

            if (pausePopup != null)
            {
                pausePopup.SetActive(_isPaused);
            }
        }

        private void TriggerGameOver()
        {
            if (_isGameOver)
            {
                return;
            }

            _isGameOver = true;
            _isPaused = false;

            if (pausePopup != null)
            {
                pausePopup.SetActive(false);
            }

            if (pauseButton != null)
            {
                pauseButton.interactable = false;
            }

            if (advanceWaveButton != null)
            {
                advanceWaveButton.interactable = false;
            }

            waveSpawner?.StopSpawning();
            Time.timeScale = 0f;

            if (gameOverPopup != null)
            {
                gameOverPopup.SetActive(true);
            }
        }

        private void UpdateWaveDisplay(int currentWave, int totalWaves)
        {
            if (waveText != null)
            {
                waveText.text = $"ONDA {currentWave} / {totalWaves}";
            }
        }

        private void RefreshResourceDisplays()
        {
            if (goldText != null)
            {
                goldText.text = _currentGold.ToString();
            }

            if (livesText != null)
            {
                livesText.text = _currentLives.ToString();
            }
        }

        private void SetAdvanceWaveState(bool canAdvance, string timerText)
        {
            if (advanceWaveButton != null)
            {
                advanceWaveButton.interactable = canAdvance;
            }

            if (advanceWaveLabelText != null)
            {
                advanceWaveLabelText.text = canAdvance ? "ADIANTAR ONDA" : "EM COMBATE";
            }

            if (advanceWaveTimerText != null)
            {
                advanceWaveTimerText.text = timerText;
            }

            if (!canAdvance && advanceWaveTimerFill != null)
            {
                advanceWaveTimerFill.fillAmount = 0f;
            }
        }
    }
}
