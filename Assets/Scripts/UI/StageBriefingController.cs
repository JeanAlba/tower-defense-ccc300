using HollowKeep.Level.Data;
using HollowKeep.Level.Spawning;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HollowKeep.UI
{
    /// <summary>
    /// Controla o popup pré-batalha exibido sobre o mapa esmaecido antes de iniciar as ondas da fase.
    /// </summary>
    public class StageBriefingController : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private GameHUDController hudController;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private bool showOnStart = true;

        [Header("Textos e Miniatura")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text difficultySummaryText;
        [SerializeField] private GameObject mapPreviewLevel01;
        [SerializeField] private GameObject mapPreviewLevel02;

        [Header("Dificuldade e Ações")]
        [SerializeField] private DifficultyProfile normalDifficulty;
        [SerializeField] private DifficultyProfile hardDifficulty;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private Button battleButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image normalButtonGraphic;
        [SerializeField] private Image hardButtonGraphic;
        [SerializeField] private Color selectedButtonColor = new Color(1f, 0.89f, 0.62f, 1f);
        [SerializeField] private Color unselectedButtonColor = new Color(0.84f, 0.75f, 0.59f, 0.9f);

        [Header("Fallback de Texto")]
        [SerializeField] private string defaultTitle = "FASE 1";
        [TextArea(3, 5)]
        [SerializeField] private string defaultDescription =
            "Saudações, Comandante!\n\nEsqueletos e criaturas do cemitério marcham pela estrada antiga rumo a Hollow Keep. Posicione arqueiros e canhões antes que a noite caia!";

        private DifficultyProfile _selectedDifficulty;

        private void Awake()
        {
            if (popupRoot == null)
            {
                popupRoot = gameObject;
            }

            if (waveSpawner == null)
            {
                waveSpawner = FindFirstObjectByType<WaveSpawner>();
            }

            if (hudController == null)
            {
                hudController = FindFirstObjectByType<GameHUDController>();
            }

            if (showOnStart)
            {
                waveSpawner?.SuppressAutoStart();
                hudController?.SetBriefingActive(true);
                popupRoot.SetActive(true);
            }
        }

        private void OnEnable()
        {
            if (normalButton != null)
            {
                normalButton.onClick.AddListener(HandleNormalClicked);
            }

            if (hardButton != null)
            {
                hardButton.onClick.AddListener(HandleHardClicked);
            }

            if (battleButton != null)
            {
                battleButton.onClick.AddListener(HandleBattleClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleCloseClicked);
            }
        }

        private void Start()
        {
            if (!showOnStart)
            {
                popupRoot?.SetActive(false);
                return;
            }

            _selectedDifficulty = ResolveInitialDifficulty();
            PopulateStageInfo();
            RefreshDifficultyUI();
        }

        private void OnDisable()
        {
            if (normalButton != null)
            {
                normalButton.onClick.RemoveListener(HandleNormalClicked);
            }

            if (hardButton != null)
            {
                hardButton.onClick.RemoveListener(HandleHardClicked);
            }

            if (battleButton != null)
            {
                battleButton.onClick.RemoveListener(HandleBattleClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }
        }

        private DifficultyProfile ResolveInitialDifficulty()
        {
            if (GameSessionConfig.SelectedDifficulty != null)
            {
                return GameSessionConfig.SelectedDifficulty;
            }

            if (normalDifficulty != null)
            {
                return normalDifficulty;
            }

            return waveSpawner != null ? waveSpawner.ActiveDifficulty : hardDifficulty;
        }

        private void PopulateStageInfo()
        {
            LevelData levelData = waveSpawner != null ? waveSpawner.CurrentLevelData : null;
            bool isLevel02Fallback = levelData != null
                && !string.IsNullOrEmpty(levelData.LevelId)
                && levelData.LevelId.Contains("02");

            if (titleText != null)
            {
                if (levelData != null && !string.IsNullOrWhiteSpace(levelData.StageTitle))
                {
                    titleText.text = levelData.StageTitle.ToUpperInvariant();
                }
                else
                {
                    titleText.text = isLevel02Fallback ? "FASE 2" : defaultTitle;
                }
            }

            if (descriptionText != null)
            {
                descriptionText.text = (levelData != null && !string.IsNullOrWhiteSpace(levelData.StageDescription))
                    ? levelData.StageDescription
                    : defaultDescription;
            }

            int previewIndex = 0;
            if (levelData != null)
            {
                previewIndex = levelData.MapPreviewVariant > 0
                    ? levelData.MapPreviewVariant
                    : (isLevel02Fallback ? 1 : 0);
            }

            if (mapPreviewLevel01 != null)
            {
                mapPreviewLevel01.SetActive(previewIndex == 0);
            }

            if (mapPreviewLevel02 != null)
            {
                mapPreviewLevel02.SetActive(previewIndex != 0);
            }
        }

        private void HandleNormalClicked()
        {
            if (normalDifficulty == null) return;
            _selectedDifficulty = normalDifficulty;
            RefreshDifficultyUI();
        }

        private void HandleHardClicked()
        {
            if (hardDifficulty == null) return;
            _selectedDifficulty = hardDifficulty;
            RefreshDifficultyUI();
        }

        private void RefreshDifficultyUI()
        {
            bool isHard = hardDifficulty != null && _selectedDifficulty == hardDifficulty;

            if (normalButtonGraphic != null)
            {
                normalButtonGraphic.color = !isHard ? selectedButtonColor : unselectedButtonColor;
            }

            if (hardButtonGraphic != null)
            {
                hardButtonGraphic.color = isHard ? selectedButtonColor : unselectedButtonColor;
            }

            if (difficultySummaryText == null)
            {
                return;
            }

            if (_selectedDifficulty == null)
            {
                difficultySummaryText.text = string.Empty;
                return;
            }

            int baseGold = (waveSpawner != null && waveSpawner.CurrentLevelData != null)
                ? waveSpawner.CurrentLevelData.StartingGold
                : 150;
            int startGold = _selectedDifficulty.GetStartingGold(baseGold);
            int hpBonusPercent = Mathf.RoundToInt((_selectedDifficulty.EnemyHealthMultiplier - 1f) * 100f);
            string combatNote = hpBonusPercent > 0 ? $"+{hpBonusPercent}% HP inimigos" : "ritmo padrão";
            difficultySummaryText.text = $"{_selectedDifficulty.StartingLives} vidas · {startGold} ouro · {combatNote}";
        }

        private void HandleBattleClicked()
        {
            if (_selectedDifficulty != null)
            {
                GameSessionConfig.SetDifficulty(_selectedDifficulty);
            }

            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            hudController?.ApplySessionDifficulty();
            hudController?.SetBriefingActive(false);
            waveSpawner?.StartLevel();
        }

        private void HandleCloseClicked()
        {
            Time.timeScale = 1f;
            if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }
    }
}
