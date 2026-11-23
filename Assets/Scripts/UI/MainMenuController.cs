using HollowKeep.Audio;
using HollowKeep.Level.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowKeep.UI
{
    /// <summary>
    /// Controla as ações da tela inicial (iniciar jogo, exibir informações sobre o jogo/ranking/créditos e sair).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Referências de UI")]
        [SerializeField] private UnityEngine.UI.Button playButton;
        [SerializeField] private UnityEngine.UI.Button creditsButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;

        [Header("Sobre o Jogo, Ranking e Créditos (Popup)")]
        [SerializeField] private GameObject creditsPopup;
        [SerializeField] private UnityEngine.UI.Button closeCreditsButton;
        [SerializeField] private TMP_Text creditsTitleText;
        [SerializeField] private TMP_Text creditsBodyText;

        [Header("Navegação")]
        [Tooltip("Nome da cena de jogo carregada ao clicar em Jogar.")]
        [SerializeField] private string gameplaySceneName = "Level_01";

        private void Awake()
        {
            if (playButton == null)
            {
                Debug.LogWarning("[MainMenuController] playButton não atribuído no Inspector.", this);
            }

            if (creditsButton == null)
            {
                Debug.LogWarning("[MainMenuController] creditsButton não atribuído no Inspector.", this);
            }

            if (quitButton == null)
            {
                Debug.LogWarning("[MainMenuController] quitButton não atribuído no Inspector.", this);
            }

            if (creditsPopup == null)
            {
                Debug.LogWarning("[MainMenuController] creditsPopup não atribuído no Inspector.", this);
            }

            if (closeCreditsButton == null)
            {
                Debug.LogWarning("[MainMenuController] closeCreditsButton não atribuído no Inspector.", this);
            }
        }

        private void Start()
        {
            ConfigureAboutPopupLayout();
            SetCreditsOpen(false);
        }

        private void OnEnable()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(HandlePlayClicked);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.AddListener(HandleCreditsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(HandleQuitClicked);
            }

            if (closeCreditsButton != null)
            {
                closeCreditsButton.onClick.AddListener(HandleCloseCreditsClicked);
            }
        }

        private void OnDisable()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(HandlePlayClicked);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.RemoveListener(HandleCreditsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(HandleQuitClicked);
            }

            if (closeCreditsButton != null)
            {
                closeCreditsButton.onClick.RemoveListener(HandleCloseCreditsClicked);
            }
        }

        public void HandlePlayClicked()
        {
            GameAudioManager.PlayUiClick();
            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                Debug.LogError("[MainMenuController] gameplaySceneName está vazio.", this);
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void HandleCreditsClicked()
        {
            GameAudioManager.PlayUiClick();
            SetCreditsOpen(true);
        }

        public void HandleCloseCreditsClicked()
        {
            GameAudioManager.PlayUiClick();
            SetCreditsOpen(false);
        }

        private void SetCreditsOpen(bool isOpen)
        {
            if (isOpen)
            {
                RefreshAboutAndRankingText();
            }

            if (creditsPopup != null)
            {
                creditsPopup.SetActive(isOpen);
            }
        }

        private void ConfigureAboutPopupLayout()
        {
            if (creditsButton != null)
            {
                TMP_Text btnLabel = creditsButton.GetComponentInChildren<TMP_Text>(true);
                if (btnLabel != null)
                {
                    btnLabel.text = "SOBRE";
                }
            }

            if (creditsPopup == null)
            {
                return;
            }

            Transform card = creditsPopup.transform.Find("CreditsCard");
            if (card != null && card.TryGetComponent(out RectTransform cardRect))
            {
                cardRect.sizeDelta = new Vector2(760f, 600f);
            }

            if (creditsTitleText == null && card != null)
            {
                Transform title = card.Find("CreditsTitle");
                if (title != null)
                {
                    creditsTitleText = title.GetComponent<TMP_Text>();
                    if (title.TryGetComponent(out RectTransform titleRect))
                    {
                        titleRect.anchoredPosition = new Vector2(0f, 228f);
                        titleRect.sizeDelta = new Vector2(680f, 54f);
                    }
                }
            }

            if (card != null)
            {
                Transform subtitle = card.Find("CreditsSubtitle");
                if (subtitle != null && subtitle.TryGetComponent(out RectTransform subRect))
                {
                    subRect.anchoredPosition = new Vector2(0f, 184f);
                    subRect.sizeDelta = new Vector2(680f, 30f);
                }
            }

            if (creditsBodyText == null && card != null)
            {
                Transform body = card.Find("CreditsBody");
                if (body != null)
                {
                    creditsBodyText = body.GetComponent<TMP_Text>();
                    if (body.TryGetComponent(out RectTransform bodyRect))
                    {
                        bodyRect.anchoredPosition = new Vector2(0f, -8f);
                        bodyRect.sizeDelta = new Vector2(680f, 330f);
                    }
                }
            }

            if (closeCreditsButton != null && closeCreditsButton.TryGetComponent(out RectTransform closeRect))
            {
                closeRect.anchoredPosition = new Vector2(0f, -225f);
            }

            RefreshAboutAndRankingText();
        }

        private void RefreshAboutAndRankingText()
        {
            if (creditsTitleText != null)
            {
                creditsTitleText.text = "SOBRE O JOGO & RANKING";
            }

            if (creditsBodyText != null)
            {
                creditsBodyText.fontSize = 18.5f;
                creditsBodyText.richText = true;
                string rankingSummary = MatchRankingService.FormatMenuLeaderboardSummary();
                creditsBodyText.text =
                    "<b>MISSÃO:</b> Defenda Hollow Keep das hordas antes que as vidas do castelo cheguem a 0 ou o tempo máximo da fase acabe.\n" +
                    "<b>CONTROLES (MOUSE):</b> Clique em um slot vazio para construir (<b>Torre Arqueira</b>: longo alcance/rápida · <b>Torre Canhão</b>: dano em área). Clique em uma torre para <b>Evoluir (Nv.2)</b> ou <b>Demolir</b>. Adiante ondas para ganhar ouro bônus.\n\n" +
                    $"<b>RANKING (MELHORES TEMPOS):</b>\n{rankingSummary}\n\n" +
                    "<size=88%>Dupla: Jean L. Alba & Ricardo Zancan · CCC300 Computação Gráfica (UPF 2026/2) · Assets: Kenney (CC0)</size>";
            }
        }

        public void HandleQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
