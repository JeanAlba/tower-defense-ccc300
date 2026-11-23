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

        [Header("Gerenciamento da Torre (Evoluir / Demolir)")]
        [SerializeField] private Sprite upgradeIcon;
        [SerializeField] private Sprite demolishIcon;
        [SerializeField] private Color demolishIconColor = new Color(0.62f, 0.14f, 0.12f, 1f);
        [SerializeField] private Color demolishRefundColor = new Color(0.14f, 0.45f, 0.18f, 1f);

        [Header("Fallback Legado (Opcional)")]
        [SerializeField] private DirectTower directTowerPrefab;
        [SerializeField] private ParabolicTower parabolicTowerPrefab;

        [Header("Detecção de Clique e HUD")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask slotLayerMask = ~0;
        [SerializeField] private GameHUDController hudController;
        [SerializeField] private TowerRangeIndicator rangeIndicator;

        private readonly List<TowerBuildOptionButton> _spawnedButtons = new();
        private readonly List<Tower> _optionPrefabs = new();
        private BuildSlot _selectedSlot;
        private bool _isManageMode;
        private TowerBuildOptionButton _upgradeButton;
        private TowerBuildOptionButton _demolishButton;

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

            EnsureRangeIndicator();

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
            if (hudController != null && hudController.IsInputBlocked)
            {
                if (menuPanel != null && menuPanel.gameObject.activeSelf)
                {
                    CloseMenu();
                }
                else
                {
                    HideRangeIndicator();
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
        /// Abre o anel radial centralizado no slot selecionado (construção se vazio, evoluir/demolir se ocupado).
        /// </summary>
        public void OpenMenu(BuildSlot slot, Camera cam = null)
        {
            _selectedSlot = slot;
            if (menuPanel == null || slot == null)
            {
                return;
            }

            _isManageMode = slot.IsOccupied && slot.BuiltTower != null;
            if (_isManageMode)
            {
                RebuildManageOptions(slot.BuiltTower);
                ShowRangeIndicator(slot.BuiltTower.transform.position, slot.BuiltTower.Range);
            }
            else
            {
                RebuildBuildOptions();
                HideRangeIndicator();
            }

            menuPanel.gameObject.SetActive(true);
            RefreshActiveButtons();

            if (positionMenuAtSlot)
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
        /// Fecha o anel radial e limpa a seleção.
        /// </summary>
        public void CloseMenu()
        {
            _selectedSlot = null;
            _isManageMode = false;
            if (menuPanel != null)
            {
                menuPanel.gameObject.SetActive(false);
            }

            HideRangeIndicator();
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
            RebuildBuildOptions();
        }

        private void ClearSpawnedButtons()
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
            _upgradeButton = null;
            _demolishButton = null;
        }

        private void RebuildBuildOptions()
        {
            ClearSpawnedButtons();

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
                TowerBuildOptionButton instance = InstantiateOptionButton($"Option_{option.towerPrefab.name}", i, count);
                instance.Configure(option.towerPrefab, option.displayName, option.icon, TryBuild, HandleOptionHoverChanged);

                _spawnedButtons.Add(instance);
                _optionPrefabs.Add(option.towerPrefab);
            }

            PositionCloseButton();
        }

        private void RebuildManageOptions(Tower tower)
        {
            ClearSpawnedButtons();

            if (optionButtonTemplate == null || menuPanel == null || tower == null)
            {
                return;
            }

            optionButtonTemplate.gameObject.SetActive(false);

            _upgradeButton = InstantiateOptionButton("Option_Upgrade", 0, 2);
            string upgradeLabel = tower.CanUpgrade ? $"EVOLUIR (NV.{tower.Level + 1})" : "NÍVEL MÁX.";
            string upgradeCostLabel = tower.CanUpgrade ? tower.UpgradeCost.ToString() : "MÁX";
            _upgradeButton.ConfigureAction(
                upgradeLabel,
                upgradeIcon,
                upgradeCostLabel,
                TryUpgradeSelectedTower,
                HandleUpgradeHoverChanged
            );
            _spawnedButtons.Add(_upgradeButton);

            _demolishButton = InstantiateOptionButton("Option_Demolish", 1, 2);
            _demolishButton.ConfigureAction(
                "DEMOLIR",
                demolishIcon,
                $"+{tower.SellRefund}",
                DemolishSelectedTower,
                null,
                demolishRefundColor,
                demolishIconColor
            );
            _spawnedButtons.Add(_demolishButton);

            PositionCloseButton();
        }

        private TowerBuildOptionButton InstantiateOptionButton(string objName, int index, int count)
        {
            TowerBuildOptionButton instance = Instantiate(optionButtonTemplate, menuPanel);
            instance.name = objName;
            instance.gameObject.SetActive(true);

            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = GetRadialPosition(index, count);
            }

            return instance;
        }

        private void PositionCloseButton()
        {
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
                if (slot != null)
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

            if (hudController != null && hudController.IsInputBlocked)
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

        private void TryUpgradeSelectedTower()
        {
            if (_selectedSlot == null || !_selectedSlot.IsOccupied || _selectedSlot.BuiltTower == null)
            {
                return;
            }

            if (hudController != null && hudController.IsInputBlocked)
            {
                return;
            }

            Tower tower = _selectedSlot.BuiltTower;
            if (!tower.CanUpgrade)
            {
                return;
            }

            int cost = tower.UpgradeCost;
            if (hudController != null && !hudController.CanAfford(cost))
            {
                return;
            }

            if (!_selectedSlot.TryUpgradeTower())
            {
                return;
            }

            hudController?.TrySpendGold(cost);
            CloseMenu();
        }

        private void DemolishSelectedTower()
        {
            if (_selectedSlot == null || !_selectedSlot.IsOccupied)
            {
                return;
            }

            if (hudController != null && hudController.IsInputBlocked)
            {
                return;
            }

            int refund = _selectedSlot.DemolishTower();
            if (refund > 0)
            {
                hudController?.AddGold(refund);
            }

            CloseMenu();
        }

        private void HandleOptionHoverChanged(Tower towerPrefab, bool isHovering)
        {
            if (_isManageMode || _selectedSlot == null || _selectedSlot.IsOccupied)
            {
                return;
            }

            if (isHovering && towerPrefab != null)
            {
                ShowRangeIndicator(_selectedSlot.transform.position, towerPrefab.Range);
            }
            else if (!isHovering)
            {
                HideRangeIndicator();
            }
        }

        private void HandleUpgradeHoverChanged(bool isHovering)
        {
            if (!_isManageMode || _selectedSlot == null || _selectedSlot.BuiltTower == null)
            {
                return;
            }

            Tower tower = _selectedSlot.BuiltTower;
            float previewRange = isHovering ? tower.UpgradedRange : tower.Range;
            ShowRangeIndicator(tower.transform.position, previewRange);
        }

        private void EnsureRangeIndicator()
        {
            if (rangeIndicator != null) return;

            rangeIndicator = FindFirstObjectByType<TowerRangeIndicator>();
            if (rangeIndicator == null)
            {
                GameObject indicatorObj = new GameObject("TowerRangeIndicator");
                rangeIndicator = indicatorObj.AddComponent<TowerRangeIndicator>();
            }
        }

        private void ShowRangeIndicator(Vector3 worldCenter, float range)
        {
            EnsureRangeIndicator();
            rangeIndicator?.Show(worldCenter, range);
        }

        private void HideRangeIndicator()
        {
            rangeIndicator?.Hide();
        }

        private void HandleGoldChanged(int _)
        {
            RefreshActiveButtons();
        }

        private void RefreshActiveButtons()
        {
            if (_isManageMode)
            {
                Tower tower = _selectedSlot != null ? _selectedSlot.BuiltTower : null;
                if (_upgradeButton != null)
                {
                    bool canAffordUpgrade = tower != null && tower.CanUpgrade
                        && (hudController == null || hudController.CanAfford(tower.UpgradeCost));
                    _upgradeButton.SetAffordable(canAffordUpgrade);
                }

                if (_demolishButton != null)
                {
                    _demolishButton.SetAffordable(tower != null);
                }

                return;
            }

            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                Tower prefab = i < _optionPrefabs.Count ? _optionPrefabs[i] : null;
                bool canAfford = prefab != null && (hudController == null || hudController.CanAfford(prefab.BuildCost));
                _spawnedButtons[i].SetAffordable(canAfford);
            }
        }
    }
}
