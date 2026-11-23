using System;
using System.Collections.Generic;
using HollowKeep.Audio;
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
        [SerializeField] private TMPro.TMP_Text matchTimerText;

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
        [SerializeField] private TMPro.TMP_Text gameOverTitleText;

        [Header("Vitória e Ranking")]
        [SerializeField] private GameObject victoryPopup;
        [SerializeField] private UnityEngine.UI.Button victoryNextLevelButton;
        [SerializeField] private TMPro.TMP_Text victoryRankingText;
        [Tooltip("Nome da próxima cena a carregar ao vencer. Se vazio ou indisponível, avança no Build Settings ou volta ao menu.")]
        [SerializeField] private string nextLevelSceneName = "";

        [Header("Configuração Inicial")]
        [Min(1)]
        [SerializeField] private int startingLives = 20;
        [Min(30f)]
        [SerializeField] private float defaultMatchTimeLimitSeconds = 300f;

        [Header("Dificuldade")]
        [Tooltip("Perfil padrão usado quando a cena inicia sem seleção prévia no menu.")]
        [SerializeField] private DifficultyProfile defaultDifficulty;

        public DifficultyProfile ActiveDifficulty => GameSessionConfig.Resolve(
            defaultDifficulty != null ? defaultDifficulty : (waveSpawner != null ? waveSpawner.ActiveDifficulty : null)
        );

        private readonly HashSet<Enemy> _trackedEnemies = new();
        private int _currentGold;
        private int _currentLives;
        private float _matchTimeLimitSeconds;
        private float _remainingMatchSeconds;
        private float _elapsedMatchSeconds;
        private int _lastReportedMatchSecond = -1;
        private bool _isTimeoutDefeat;
        private bool _isPaused;
        private bool _isGameOver;
        private bool _isVictory;
        private bool _isBriefingOpen;
        private bool _isFastForward;
        private bool _hasStartedFirstWave;
        private bool _allWavesCompleted;

        public int CurrentGold => _currentGold;
        public float MatchTimeLimitSeconds => _matchTimeLimitSeconds;
        public float RemainingMatchSeconds => _remainingMatchSeconds;
        public float ElapsedMatchSeconds => _elapsedMatchSeconds;
        public bool IsTimeoutDefeat => _isTimeoutDefeat;
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
            _isTimeoutDefeat = false;
            _isFastForward = false;
            _hasStartedFirstWave = false;
            _allWavesCompleted = false;
            _elapsedMatchSeconds = 0f;
            _lastReportedMatchSecond = -1;

            EnsureMatchTimerIndicator();
            EnsureVictoryRankingDisplay();
            EnsureGameOverTitleReference();
            RefreshVictoryNextButtonLabel();

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
            RefreshMatchTimerVisibility();

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

        private void Update()
        {
            if (_isGameOver || _isVictory || _isPaused || _isBriefingOpen)
            {
                return;
            }

            if (_matchTimeLimitSeconds <= 0f)
            {
                return;
            }

            _elapsedMatchSeconds += Time.deltaTime;
            _remainingMatchSeconds = Mathf.Max(0f, _matchTimeLimitSeconds - _elapsedMatchSeconds);

            int currentSecond = Mathf.CeilToInt(_remainingMatchSeconds);
            if (currentSecond != _lastReportedMatchSecond)
            {
                _lastReportedMatchSecond = currentSecond;
                RefreshMatchTimerDisplay();
            }

            if (_remainingMatchSeconds <= 0f)
            {
                TriggerGameOver(isTimeout: true);
            }
        }

        /// <summary>
        /// Recarrega ouro inicial, vidas do castelo e tempo máximo com base na dificuldade ativa da sessão.
        /// </summary>
        public void ApplySessionDifficulty()
        {
            DifficultyProfile difficulty = ActiveDifficulty;
            LevelData levelData = waveSpawner != null ? waveSpawner.CurrentLevelData : null;
            int baseGold = levelData != null ? levelData.StartingGold : 0;
            float baseTimeLimit = levelData != null ? levelData.MatchTimeLimitSeconds : defaultMatchTimeLimitSeconds;

            _currentGold = difficulty != null ? difficulty.GetStartingGold(baseGold) : baseGold;
            _currentLives = difficulty != null ? difficulty.StartingLives : startingLives;
            _matchTimeLimitSeconds = difficulty != null
                ? difficulty.GetScaledMatchTimeLimit(baseTimeLimit)
                : Mathf.Max(30f, baseTimeLimit);
            _elapsedMatchSeconds = 0f;
            _remainingMatchSeconds = _matchTimeLimitSeconds;
            _lastReportedMatchSecond = -1;

            RefreshResourceDisplays();
            RefreshMatchTimerDisplay();
            RefreshMatchTimerVisibility();
            OnGoldChanged?.Invoke(_currentGold);
        }

        /// <summary>
        /// Bloqueia ou libera a HUD e o tempo de jogo enquanto o popup pré-fase estiver aberto.
        /// </summary>
        public void SetBriefingActive(bool active)
        {
            _isBriefingOpen = active;
            RefreshMatchTimerVisibility();

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
            GameAudioManager.PlayWaveStart();
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
                GameAudioManager.PlayCastleHit();

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

        private void TriggerGameOver(bool isTimeout = false)
        {
            if (_isGameOver || _isVictory)
            {
                return;
            }

            _isGameOver = true;
            _isTimeoutDefeat = isTimeout;
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

            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = isTimeout ? "TEMPO ESGOTADO" : "DERROTA";
            }

            waveSpawner?.StopSpawning();
            GameAudioManager.PlayDefeat();
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

            RecordAndDisplayVictoryRanking();
            RefreshVictoryNextButtonLabel();
            GameAudioManager.PlayVictory();
            ApplyActiveTimeScale();

            if (victoryPopup != null)
            {
                victoryPopup.SetActive(true);
            }
        }

        private void RecordAndDisplayVictoryRanking()
        {
            LevelData levelData = waveSpawner != null ? waveSpawner.CurrentLevelData : null;
            string levelId = levelData != null && !string.IsNullOrWhiteSpace(levelData.LevelId)
                ? levelData.LevelId
                : SceneManager.GetActiveScene().name;
            string stageTitle = levelData != null && !string.IsNullOrWhiteSpace(levelData.StageTitle)
                ? levelData.StageTitle
                : levelId;
            DifficultyProfile difficulty = ActiveDifficulty;
            string difficultyName = difficulty != null && !string.IsNullOrWhiteSpace(difficulty.DisplayName)
                ? difficulty.DisplayName
                : "Normal";

            int rankPosition = MatchRankingService.RecordVictory(
                levelId,
                _elapsedMatchSeconds,
                difficultyName,
                _currentLives
            );

            EnsureVictoryRankingDisplay();
            if (victoryRankingText != null)
            {
                victoryRankingText.text = MatchRankingService.FormatVictorySummary(
                    levelId,
                    stageTitle,
                    _elapsedMatchSeconds,
                    rankPosition,
                    3
                );
            }
        }

        private bool HasNextLevelConfigured()
        {
            if (!string.IsNullOrWhiteSpace(nextLevelSceneName) && Application.CanStreamedLevelBeLoaded(nextLevelSceneName))
            {
                return true;
            }

            int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
            return nextBuildIndex > 0 && nextBuildIndex < SceneManager.sceneCountInBuildSettings;
        }

        private void RefreshVictoryNextButtonLabel()
        {
            if (victoryNextLevelButton == null)
            {
                return;
            }

            TMPro.TMP_Text buttonLabel = victoryNextLevelButton.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (buttonLabel != null)
            {
                buttonLabel.text = HasNextLevelConfigured() ? "PRÓXIMA FASE" : "MENU PRINCIPAL";
            }
        }

        private void EnsureMatchTimerIndicator()
        {
            if (matchTimerText != null)
            {
                return;
            }

            Transform canvasRoot = transform;
            if (waveText != null && waveText.canvas != null)
            {
                canvasRoot = waveText.canvas.transform;
            }

            GameObject timerObj = new("MatchTimerTopCenter", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            timerObj.transform.SetParent(canvasRoot, false);

            // Mantém atrás dos popups (briefing, pausa, vitória, derrota)
            if (canvasRoot.childCount > 1)
            {
                timerObj.transform.SetSiblingIndex(1);
            }

            RectTransform rect = timerObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -18f);
            rect.sizeDelta = new Vector2(380f, 76f);

            TMPro.TextMeshProUGUI tmp = timerObj.GetComponent<TMPro.TextMeshProUGUI>();
            TMPro.TMP_FontAsset cartographyFont = waveText != null
                ? waveText.font
                : (advanceWaveLabelText != null ? advanceWaveLabelText.font : null);

            if (cartographyFont != null)
            {
                tmp.font = cartographyFont;
                Material baseMat = (waveText != null && waveText.fontSharedMaterial != null)
                    ? waveText.fontSharedMaterial
                    : cartographyFont.material;

                if (baseMat != null)
                {
                    Material matInstance = new(baseMat);
                    if (matInstance.HasProperty(TMPro.ShaderUtilities.ID_FaceDilate))
                    {
                        matInstance.SetFloat(TMPro.ShaderUtilities.ID_FaceDilate, 0.28f);
                    }
                    if (matInstance.HasProperty(TMPro.ShaderUtilities.ID_OutlineWidth))
                    {
                        matInstance.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0.26f);
                    }
                    if (matInstance.HasProperty(TMPro.ShaderUtilities.ID_OutlineColor))
                    {
                        matInstance.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, new Color(0.05f, 0.03f, 0.02f, 0.98f));
                    }
                    tmp.fontMaterial = matInstance;
                }
            }

            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.fontSize = 50f;
            tmp.characterSpacing = 4f;
            tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;

            UnityEngine.UI.Outline uiOutline = timerObj.AddComponent<UnityEngine.UI.Outline>();
            uiOutline.effectColor = new Color(0.06f, 0.03f, 0.02f, 0.95f);
            uiOutline.effectDistance = new Vector2(1.5f, -1.5f);

            UnityEngine.UI.Shadow shadow = timerObj.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2.5f, -2.5f);

            matchTimerText = tmp;
            RefreshMatchTimerVisibility();
        }

        private void RefreshMatchTimerVisibility()
        {
            if (matchTimerText != null)
            {
                matchTimerText.gameObject.SetActive(!_isBriefingOpen);
            }
        }

        private void EnsureVictoryRankingDisplay()
        {
            if (victoryRankingText != null || victoryPopup == null)
            {
                return;
            }

            Transform victoryCard = victoryPopup.transform.Find("VictoryCard");
            if (victoryCard == null)
            {
                return;
            }

            RectTransform cardRect = victoryCard.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.sizeDelta = new Vector2(540f, 430f);
            }

            Transform titleTransform = victoryCard.Find("VictoryTitle");
            if (titleTransform != null && titleTransform.TryGetComponent(out RectTransform titleRect))
            {
                titleRect.anchoredPosition = new Vector2(0f, 152f);
            }

            if (victoryNextLevelButton != null && victoryNextLevelButton.TryGetComponent(out RectTransform btnRect))
            {
                btnRect.anchoredPosition = new Vector2(0f, -148f);
            }

            GameObject rankingObj = new("VictoryRankingText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            rankingObj.transform.SetParent(victoryCard, false);

            RectTransform rankingRect = rankingObj.GetComponent<RectTransform>();
            rankingRect.anchorMin = new Vector2(0.5f, 0.5f);
            rankingRect.anchorMax = new Vector2(0.5f, 0.5f);
            rankingRect.pivot = new Vector2(0.5f, 0.5f);
            rankingRect.anchoredPosition = new Vector2(0f, 6f);
            rankingRect.sizeDelta = new Vector2(470f, 210f);

            TMPro.TextMeshProUGUI tmp = rankingObj.GetComponent<TMPro.TextMeshProUGUI>();
            TMPro.TMP_Text templateText = advanceWaveLabelText != null
                ? advanceWaveLabelText
                : (titleTransform != null ? titleTransform.GetComponent<TMPro.TMP_Text>() : waveText);

            if (templateText != null)
            {
                tmp.font = templateText.font;
                tmp.fontSharedMaterial = templateText.fontSharedMaterial;
            }

            tmp.fontSize = 23f;
            tmp.color = new Color(0.16f, 0.10f, 0.06f, 1f);
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.richText = true;
            tmp.raycastTarget = false;
            victoryRankingText = tmp;
        }

        private void EnsureGameOverTitleReference()
        {
            if (gameOverTitleText != null || gameOverPopup == null)
            {
                return;
            }

            Transform card = gameOverPopup.transform.Find("GameOverCard");
            Transform title = card != null ? card.Find("GameOverTitle") : null;
            if (title != null)
            {
                gameOverTitleText = title.GetComponent<TMPro.TMP_Text>();
            }
        }

        private void RefreshMatchTimerDisplay()
        {
            if (matchTimerText == null)
            {
                return;
            }

            string formatted = MatchRankingService.FormatTime(_remainingMatchSeconds);
            matchTimerText.text = formatted;
            matchTimerText.color = _remainingMatchSeconds <= 30f
                ? new Color(1f, 0.32f, 0.28f, 1f)
                : Color.white;
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
