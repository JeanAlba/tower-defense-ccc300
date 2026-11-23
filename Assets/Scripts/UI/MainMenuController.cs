using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowKeep.UI
{
    /// <summary>
    /// Controla as ações da tela inicial (iniciar jogo, exibir créditos e sair da aplicação).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Referências de UI")]
        [SerializeField] private UnityEngine.UI.Button playButton;
        [SerializeField] private UnityEngine.UI.Button creditsButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;

        [Header("Créditos (Popup)")]
        [SerializeField] private GameObject creditsPopup;
        [SerializeField] private UnityEngine.UI.Button closeCreditsButton;

        [Header("Navegação")]
        [Tooltip("Nome da cena de jogo carregada ao clicar em Jogar.")]
        [SerializeField] private string gameplaySceneName = "SampleScene";

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
            SetCreditsOpen(true);
        }

        public void HandleCloseCreditsClicked()
        {
            SetCreditsOpen(false);
        }

        private void SetCreditsOpen(bool isOpen)
        {
            if (creditsPopup != null)
            {
                creditsPopup.SetActive(isOpen);
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
