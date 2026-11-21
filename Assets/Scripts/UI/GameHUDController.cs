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
        [SerializeField] private UnityEngine.UI.Button mainMenuButton;

        [Header("Configuração Inicial")]
        [Min(1)]
        [SerializeField] private int startingLives = 20;

        private readonly HashSet<Enemy> _trackedEnemies = new();
        private int _currentGold;
        private int _currentLives;
        private bool _isPaused;
        private bool _allWavesCompleted;

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

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
            }
        }

        private void Start()
        {
            _currentGold = (waveSpawner != null && waveSpawner.CurrentLevelData != null)
                ? waveSpawner.CurrentLevelData.StartingGold
                : 0;
            _currentLives = startingLives;
            _allWavesCompleted = false;

            SetPaused(false);
            UpdateWaveDisplay(0, waveSpawner != null ? waveSpawner.TotalWaves : 0);
            RefreshResourceDisplays();

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

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
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
        }

        public void SetLives(int amount)
        {
            _currentLives = Mathf.Max(0, amount);
            RefreshResourceDisplays();
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
            }
        }

        private void HandleEnemyReachedEnd(Enemy enemy)
        {
            UnsubscribeEnemy(enemy);
            if (enemy != null)
            {
                _currentLives = Mathf.Max(0, _currentLives - enemy.DamageToCastle);
                RefreshResourceDisplays();
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
            if (_isPaused || waveSpawner == null)
            {
                return;
            }

            waveSpawner.SkipIntermission();
        }

        private void HandlePauseClicked()
        {
            SetPaused(!_isPaused);
        }

        private void HandleResumeClicked()
        {
            SetPaused(false);
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
            _isPaused = paused;
            Time.timeScale = _isPaused ? 0f : 1f;

            if (pausePopup != null)
            {
                pausePopup.SetActive(_isPaused);
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
