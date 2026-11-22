using System;
using System.Collections.Generic;
using HollowKeep.Towers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HollowKeep.UI
{
    /// <summary>
    /// Menu radial cartográfico modular para construir torres sobre um BuildSlot.
    /// </summary>
    public class BuildMenuUI : MonoBehaviour
    {
        [Serializable]
        public struct TowerBuildOption
        {
            public Tower towerPrefab;
            public string displayName;
            public Sprite icon;
        }

        [Header("Estrutura do Anel Radial")]
        [SerializeField] private RectTransform menuPanel;
        [SerializeField] private TowerBuildOptionButton optionButtonTemplate;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private float radialRadius = 112f;
        [SerializeField] private float cancelButtonOffsetY = -102f;
        [SerializeField] private Vector2 screenEdgePadding = new Vector2(175f, 175f);
        [SerializeField] private bool positionMenuAtSlot = true;
        [SerializeField] private Vector3 worldOffset = Vector3.zero;

        [Header("Opções Modulares de Torres")]
        [SerializeField] private List<TowerBuildOption> towerOptions = new();

        [Header("Fallback Legado (Opcional)")]
        [SerializeField] private DirectTower directTowerPrefab;
        [SerializeField] private ParabolicTower parabolicTowerPrefab;

        [Header("Detecção de Clique e HUD")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask slotLayerMask = ~0;
        [SerializeField] private GameHUDController hudController;

        private readonly List<TowerBuildOptionButton> _spawnedButtons = new();
        private readonly List<Tower> _optionPrefabs = new();
        private BuildSlot _selectedSlot;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (hudController == null)
            {
                hudController = FindFirstObjectByType<GameHUDController>();
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseMenu);
            }

            EnsureFallbackOptions();
            RebuildRadialOptions();
            CloseMenu();
        }

        private void OnEnable()
        {
            if (hudController != null)
            {
                hudController.OnGoldChanged += HandleGoldChanged;
            }
        }

        private void OnDisable()
        {
            if (hudController != null)
            {
                hudController.OnGoldChanged -= HandleGoldChanged;
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseMenu);
            }
        }

        private void Update()
        {
            if (hudController != null && hudController.IsPaused)
            {
                if (menuPanel != null && menuPanel.gameObject.activeSelf)
                {
                    CloseMenu();
                }

                return;
            }

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            HandleWorldClick();
        }

        /// <summary>
        /// Abre o anel radial centralizado no slot selecionado.
        /// </summary>
        public void OpenMenu(BuildSlot slot, Camera cam = null)
        {
            _selectedSlot = slot;
            if (menuPanel == null)
            {
                return;
            }

            menuPanel.gameObject.SetActive(true);
            RefreshTowerButtons();

            if (positionMenuAtSlot && slot != null)
            {
                Camera activeCam = cam != null ? cam : (targetCamera != null ? targetCamera : Camera.main);
                if (activeCam != null)
                {
                    Vector3 screenPos = activeCam.WorldToScreenPoint(slot.transform.position + worldOffset);
                    screenPos.x = Mathf.Clamp(screenPos.x, screenEdgePadding.x, Mathf.Max(screenEdgePadding.x, Screen.width - screenEdgePadding.x));
                    screenPos.y = Mathf.Clamp(screenPos.y, screenEdgePadding.y, Mathf.Max(screenEdgePadding.y, Screen.height - screenEdgePadding.y));
                    menuPanel.position = screenPos;
                }
            }
        }

        /// <summary>
        /// Fecha o anel radial de construção e limpa a seleção.
        /// </summary>
        public void CloseMenu()
        {
            _selectedSlot = null;
            if (menuPanel != null)
            {
                menuPanel.gameObject.SetActive(false);
            }
        }

        private void EnsureFallbackOptions()
        {
            if (towerOptions.Count > 0)
            {
                return;
            }

            if (directTowerPrefab != null)
            {
                towerOptions.Add(new TowerBuildOption
                {
                    towerPrefab = directTowerPrefab,
                    displayName = directTowerPrefab.TowerName
                });
            }

            if (parabolicTowerPrefab != null)
            {
                towerOptions.Add(new TowerBuildOption
                {
                    towerPrefab = parabolicTowerPrefab,
                    displayName = parabolicTowerPrefab.TowerName
                });
            }
        }

        private void RebuildRadialOptions()
        {
            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null)
                {
                    Destroy(_spawnedButtons[i].gameObject);
                }
            }
            _spawnedButtons.Clear();
            _optionPrefabs.Clear();

            if (optionButtonTemplate == null || menuPanel == null)
            {
                return;
            }

            optionButtonTemplate.gameObject.SetActive(false);

            List<TowerBuildOption> validOptions = new();
            for (int i = 0; i < towerOptions.Count; i++)
            {
                if (towerOptions[i].towerPrefab != null)
                {
                    validOptions.Add(towerOptions[i]);
                }
            }

            int count = validOptions.Count;
            for (int i = 0; i < count; i++)
            {
                TowerBuildOption option = validOptions[i];
                TowerBuildOptionButton instance = Instantiate(optionButtonTemplate, menuPanel);
                instance.name = $"Option_{option.towerPrefab.name}";
                instance.gameObject.SetActive(true);
                instance.Configure(option.towerPrefab, option.displayName, option.icon, TryBuild);

                RectTransform rect = instance.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = GetRadialPosition(i, count);
                }

                _spawnedButtons.Add(instance);
                _optionPrefabs.Add(option.towerPrefab);
            }

            if (closeButton != null)
            {
                RectTransform closeRect = closeButton.GetComponent<RectTransform>();
                if (closeRect != null)
                {
                    closeRect.anchoredPosition = new Vector2(0f, cancelButtonOffsetY);
                }
            }
        }

        private Vector2 GetRadialPosition(int index, int count)
        {
            if (count <= 1)
            {
                return new Vector2(0f, radialRadius);
            }

            // Mantém o fundo do anel reservado para o botão de cancelar (-90 graus)
            // e distribui as torres simetricamente no arco restante.
            float totalArc = count == 2 ? 120f : Mathf.Min(250f, 120f + (count - 2) * 55f);
            float startAngle = 90f + totalArc * 0.5f;
            float step = totalArc / (count - 1);
            float angleRad = (startAngle - step * index) * Mathf.Deg2Rad;

            return new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * radialRadius;
        }

        private void HandleWorldClick()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 200f, slotLayerMask))
            {
                BuildSlot slot = hit.collider.GetComponentInParent<BuildSlot>();
                if (slot != null && !slot.IsOccupied)
                {
                    OpenMenu(slot, cam);
                    return;
                }
            }

            CloseMenu();
        }

        private void TryBuild(Tower towerPrefab)
        {
            if (_selectedSlot == null || _selectedSlot.IsOccupied || towerPrefab == null)
            {
                return;
            }

            if (hudController != null && hudController.IsPaused)
            {
                return;
            }

            if (hudController != null && !hudController.CanAfford(towerPrefab.BuildCost))
            {
                return;
            }

            Tower builtTower = _selectedSlot.BuildTower(towerPrefab);
            if (builtTower == null)
            {
                return;
            }

            hudController?.TrySpendGold(towerPrefab.BuildCost);
            CloseMenu();
        }

        private void HandleGoldChanged(int _)
        {
            RefreshTowerButtons();
        }

        private void RefreshTowerButtons()
        {
            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                Tower prefab = _optionPrefabs[i];
                bool canAfford = prefab != null && (hudController == null || hudController.CanAfford(prefab.BuildCost));
                _spawnedButtons[i].SetAffordable(canAfford);
            }
        }
    }
}
