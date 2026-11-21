using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowKeep.UI
{
    /// <summary>
    /// Controla as ações da tela inicial (iniciar jogo e sair da aplicação).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Referências de UI")]
        [SerializeField] private UnityEngine.UI.Button playButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;

        [Header("Navegação")]
        [Tooltip("Nome da cena de jogo carregada ao clicar em Jogar.")]
        [SerializeField] private string gameplaySceneName = "SampleScene";

        private void Awake()
        {
            if (playButton == null)
            {
                Debug.LogWarning("[MainMenuController] playButton não atribuído no Inspector.", this);
            }

            if (quitButton == null)
            {
                Debug.LogWarning("[MainMenuController] quitButton não atribuído no Inspector.", this);
            }
        }

        private void OnEnable()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(HandlePlayClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(HandleQuitClicked);
            }
        }

        private void OnDisable()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(HandlePlayClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(HandleQuitClicked);
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
