using System;
using System.Collections.Generic;
using HollowKeep.Enemies;
using HollowKeep.Level.Data;
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
        [SerializeField] private UnityEngine.UI.Image advanceWaveIcon;
        [SerializeField] private Sprite advanceWaveActiveSprite;
        [SerializeField] private Sprite advanceWaveDisabledSprite;
        [SerializeField] private GameObject advanceWaveDisabledBar;
        [SerializeField] private TMPro.TMP_Text advanceWaveLabelText;
        [SerializeField] private TMPro.TMP_Text advanceWaveTimerText;
        [SerializeField] private bool animateInitialPrepTimerFill = false;
        [Min(0)]
        [SerializeField] private int earlyWaveGoldPerSecond = 1;

        [Header("Velocidade e Pausa (Superior Direito e Popup)")]
        [SerializeField] private bool showSpeedButton = true;
        [SerializeField] private UnityEngine.UI.Button speedButton;
        [SerializeField] private TMPro.TMP_Text speedButtonText;
        [Min(1f)]
        [SerializeField] private float fastForwardScale = 2f;
        [SerializeField] private UnityEngine.UI.Button pauseButton;
        [SerializeField] private GameObject pausePopup;
        [SerializeField] private UnityEngine.UI.Button resumeButton;
        [SerializeField] private UnityEngine.UI.Button restartButton;
        [SerializeField] private UnityEngine.UI.Button mainMenuButton;

        [Header("Derrota (Game Over)")]
        [SerializeField] private GameObject gameOverPopup;
        [SerializeField] private UnityEngine.UI.Button gameOverRetryButton;
        [SerializeField] private UnityEngine.UI.Button gameOverExitButton;

        [Header("Vitória")]
        [SerializeField] private GameObject victoryPopup;
        [SerializeField] private UnityEngine.UI.Button victoryNextLevelButton;
        [Tooltip("Nome da próxima cena a carregar ao vencer. Se vazio ou indisponível, avança no Build Settings ou volta ao menu.")]
        [SerializeField] private string nextLevelSceneName = "";

        [Header("Configuração Inicial")]
        [Min(1)]
        [SerializeField] private int startingLives = 20;

        [Header("Dificuldade")]
        [Tooltip("Perfil padrão usado quando a cena inicia sem seleção prévia no menu.")]
        [SerializeField] private DifficultyProfile defaultDifficulty;

        public DifficultyProfile ActiveDifficulty => GameSessionConfig.Resolve(
            defaultDifficulty != null ? defaultDifficulty : (waveSpawner != null ? waveSpawner.ActiveDifficulty : null)
        );

        private readonly HashSet<Enemy> _trackedEnemies = new();
        private int _currentGold;
        private int _currentLives;
        private bool _isPaused;
        private bool _isGameOver;
        private bool _isVictory;
        private bool _isBriefingOpen;
        private bool _isFastForward;
        private bool _hasStartedFirstWave;
        private bool _allWavesCompleted;

        public int CurrentGold => _currentGold;
        public bool IsPaused => _isPaused;
        public bool IsGameOver => _isGameOver;
        public bool IsVictory => _isVictory;
        public bool IsBriefingOpen => _isBriefingOpen;
        public bool IsFastForward => _isFastForward;
        public bool IsInputBlocked => _isPaused || _isGameOver || _isVictory || _isBriefingOpen;

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

            if (speedButton != null)
            {
                speedButton.onClick.AddListener(HandleSpeedClicked);
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

            if (victoryNextLevelButton != null)
            {
                victoryNextLevelButton.onClick.AddListener(HandleNextLevelClicked);
            }
        }

        private void Start()
        {
            _isGameOver = false;
            _isVictory = false;
            _isFastForward = false;
            _hasStartedFirstWave = false;
            _allWavesCompleted = false;

            if (gameOverPopup != null)
            {
                gameOverPopup.SetActive(false);
            }

            if (victoryPopup != null)
            {
                victoryPopup.SetActive(false);
            }

            if (speedButton != null)
            {
                speedButton.gameObject.SetActive(showSpeedButton);
                speedButton.interactable = !_isBriefingOpen;
            }

            if (pauseButton != null)
            {
                pauseButton.interactable = !_isBriefingOpen;
            }

            ApplySessionDifficulty();
            SetPaused(false);
            RefreshSpeedButtonDisplay();
            UpdateWaveDisplay(0, waveSpawner != null ? waveSpawner.TotalWaves : 0);

            if (waveSpawner != null && waveSpawner.IsInIntermission)
            {
                int secondsRemaining = Mathf.CeilToInt(waveSpawner.IntermissionRemainingSeconds);
                SetAdvanceWaveState(true, secondsRemaining > 0 ? FormatIntermissionCounter(secondsRemaining) : "--");
            }
            else
            {
                SetAdvanceWaveState(false, "--");
            }
        }

        /// <summary>
        /// Recarrega ouro inicial e vidas do castelo com base na dificuldade ativa da sessão.
        /// </summary>
        public void ApplySessionDifficulty()
        {
            DifficultyProfile difficulty = ActiveDifficulty;
            int baseGold = (waveSpawner != null && waveSpawner.CurrentLevelData != null)
                ? waveSpawner.CurrentLevelData.StartingGold
                : 0;
            _currentGold = difficulty != null ? difficulty.GetStartingGold(baseGold) : baseGold;
            _currentLives = difficulty != null ? difficulty.StartingLives : startingLives;
            RefreshResourceDisplays();
            OnGoldChanged?.Invoke(_currentGold);
        }

        /// <summary>
        /// Bloqueia ou libera a HUD e o tempo de jogo enquanto o popup pré-fase estiver aberto.
        /// </summary>
        public void SetBriefingActive(bool active)
        {
            _isBriefingOpen = active;

            if (active)
            {
                _isPaused = false;
                if (pausePopup != null)
                {
                    pausePopup.SetActive(false);
                }

                if (speedButton != null)
                {
                    speedButton.interactable = false;
                }

                if (pauseButton != null)
                {
                    pauseButton.interactable = false;
                }

                if (advanceWaveButton != null)
                {
                    advanceWaveButton.interactable = false;
                }
            }
            else if (!_isGameOver && !_isVictory)
            {
                if (speedButton != null)
                {
                    speedButton.interactable = true;
                }

                if (pauseButton != null)
                {
                    pauseButton.interactable = true;
                }
            }

            ApplyActiveTimeScale();
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

            if (speedButton != null)
            {
                speedButton.onClick.RemoveListener(HandleSpeedClicked);
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

            if (victoryNextLevelButton != null)
            {
                victoryNextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);
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

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _currentGold += amount;
            RefreshResourceDisplays();
            OnGoldChanged?.Invoke(_currentGold);
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
            _hasStartedFirstWave = true;
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
                SetAdvanceWaveState(true, FormatIntermissionCounter(Mathf.CeilToInt(secondsRemaining)));
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

            bool shouldAnimate = (_hasStartedFirstWave || animateInitialPrepTimerFill) && totalDuration > 0f;
            advanceWaveTimerFill.fillAmount = shouldAnimate
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

            if (advanceWaveDisabledBar != null)
            {
                advanceWaveDisabledBar.SetActive(true);
            }

            if (advanceWaveIcon != null && advanceWaveDisabledSprite != null)
            {
                advanceWaveIcon.sprite = advanceWaveDisabledSprite;
            }

            if (!_isGameOver && _currentLives > 0)
            {
                TriggerVictory();
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
            if (IsInputBlocked || waveSpawner == null)
            {
                return;
            }

            int secondsRemaining = Mathf.CeilToInt(waveSpawner.IntermissionRemainingSeconds);
            int earlyGoldBonus = _hasStartedFirstWave ? CalculateEarlyWaveGoldBonus(secondsRemaining) : 0;

            if (waveSpawner.SkipIntermission() && earlyGoldBonus > 0)
            {
                _currentGold += earlyGoldBonus;
                RefreshResourceDisplays();
                OnGoldChanged?.Invoke(_currentGold);
            }
        }

        public void SetSpeedButtonVisible(bool visible)
        {
            showSpeedButton = visible;
            if (speedButton != null)
            {
                speedButton.gameObject.SetActive(visible);
            }

            if (!visible && _isFastForward)
            {
                _isFastForward = false;
                ApplyActiveTimeScale();
                RefreshSpeedButtonDisplay();
            }
        }

        private void HandleSpeedClicked()
        {
            if (IsInputBlocked || !showSpeedButton)
            {
                return;
            }

            _isFastForward = !_isFastForward;
            ApplyActiveTimeScale();
            RefreshSpeedButtonDisplay();
        }

        private void HandlePauseClicked()
        {
            if (_isGameOver || _isVictory || _isBriefingOpen)
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

        private void HandleNextLevelClicked()
        {
            Time.timeScale = 1f;

            if (!string.IsNullOrWhiteSpace(nextLevelSceneName) && Application.CanStreamedLevelBeLoaded(nextLevelSceneName))
            {
                SceneManager.LoadScene(nextLevelSceneName);
                return;
            }

            int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextBuildIndex > 0 && nextBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextBuildIndex);
                return;
            }

            HandleMainMenuClicked();
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
            if (_isGameOver || _isVictory)
            {
                return;
            }

            _isPaused = !_isBriefingOpen && paused;
            ApplyActiveTimeScale();

            if (pausePopup != null)
            {
                pausePopup.SetActive(_isPaused);
            }
        }

        private void TriggerGameOver()
        {
            if (_isGameOver || _isVictory)
            {
                return;
            }

            _isGameOver = true;
            _isPaused = false;

            if (pausePopup != null)
            {
                pausePopup.SetActive(false);
            }

            if (speedButton != null)
            {
                speedButton.interactable = false;
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
            ApplyActiveTimeScale();

            if (gameOverPopup != null)
            {
                gameOverPopup.SetActive(true);
            }
        }

        private void TriggerVictory()
        {
            if (_isVictory || _isGameOver)
            {
                return;
            }

            _isVictory = true;
            _isPaused = false;

            if (pausePopup != null)
            {
                pausePopup.SetActive(false);
            }

            if (speedButton != null)
            {
                speedButton.interactable = false;
            }

            if (pauseButton != null)
            {
                pauseButton.interactable = false;
            }

            if (advanceWaveButton != null)
            {
                advanceWaveButton.interactable = false;
            }

            ApplyActiveTimeScale();

            if (victoryPopup != null)
            {
                victoryPopup.SetActive(true);
            }
        }

        private void ApplyActiveTimeScale()
        {
            if (_isPaused || _isGameOver || _isVictory || _isBriefingOpen)
            {
                Time.timeScale = 0f;
                return;
            }

            Time.timeScale = _isFastForward ? Mathf.Max(1f, fastForwardScale) : 1f;
        }

        private void RefreshSpeedButtonDisplay()
        {
            if (speedButtonText != null)
            {
                int multiplierLabel = _isFastForward ? Mathf.RoundToInt(Mathf.Max(1f, fastForwardScale)) : 1;
                speedButtonText.text = $"{multiplierLabel}X";
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

            if (advanceWaveDisabledBar != null)
            {
                advanceWaveDisabledBar.SetActive(!canAdvance);
            }

            if (advanceWaveIcon != null)
            {
                Sprite targetSprite = canAdvance ? advanceWaveActiveSprite : advanceWaveDisabledSprite;
                if (targetSprite != null)
                {
                    advanceWaveIcon.sprite = targetSprite;
                }
            }

            if (advanceWaveLabelText != null)
            {
                if (!canAdvance)
                {
                    advanceWaveLabelText.text = "EM COMBATE";
                }
                else
                {
                    advanceWaveLabelText.text = _hasStartedFirstWave ? "ADIANTAR ONDA" : "INICIAR FASE";
                }
            }

            if (advanceWaveTimerText != null)
            {
                advanceWaveTimerText.text = timerText;
            }

            bool shouldClearFill = !canAdvance || (!_hasStartedFirstWave && !animateInitialPrepTimerFill);
            if (shouldClearFill && advanceWaveTimerFill != null)
            {
                advanceWaveTimerFill.fillAmount = 0f;
            }
        }

        private int CalculateEarlyWaveGoldBonus(int secondsRemaining)
        {
            if (secondsRemaining <= 0 || earlyWaveGoldPerSecond <= 0)
            {
                return 0;
            }

            float goldMult = ActiveDifficulty != null ? ActiveDifficulty.GoldRewardMultiplier : 1f;
            return Mathf.Max(0, Mathf.RoundToInt(secondsRemaining * earlyWaveGoldPerSecond * goldMult));
        }

        private string FormatIntermissionCounter(int secondsRemaining)
        {
            if (!_hasStartedFirstWave)
            {
                return $"{secondsRemaining}s";
            }

            int goldBonus = CalculateEarlyWaveGoldBonus(secondsRemaining);
            return $"+{goldBonus} OURO";
        }
    }
}
