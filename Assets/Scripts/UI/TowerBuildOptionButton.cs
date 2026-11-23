using System;
using HollowKeep.Towers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HollowKeep.UI
{
    /// <summary>
    /// Representa um botão modular de opção de torre no anel radial de construção.
    /// </summary>
    public class TowerBuildOptionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private UnityEngine.UI.Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Color defaultIconColor = new Color(0.16f, 0.1f, 0.06f, 1f);
        [SerializeField] private Color affordableCostColor = new Color(0.16f, 0.1f, 0.06f, 1f);
        [SerializeField] private Color unaffordableCostColor = new Color(0.62f, 0.14f, 0.12f, 1f);

        private Tower _towerPrefab;
        private Action _onSelected;
        private Action<bool> _onHoverChanged;
        private Color? _costColorOverride;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        /// <summary>
        /// Configura os dados visuais e callback de construção para uma torre.
        /// </summary>
        public void Configure(
            Tower towerPrefab,
            string displayName,
            Sprite icon,
            Action<Tower> onSelected,
            Action<Tower, bool> onHoverChanged = null)
        {
            _towerPrefab = towerPrefab;
            string resolvedName = string.IsNullOrWhiteSpace(displayName) && towerPrefab != null
                ? towerPrefab.TowerName
                : displayName;
            string costLabel = towerPrefab != null ? towerPrefab.BuildCost.ToString() : "-";

            ConfigureAction(
                resolvedName,
                icon,
                costLabel,
                () =>
                {
                    if (towerPrefab != null)
                    {
                        onSelected?.Invoke(towerPrefab);
                    }
                },
                isHovering =>
                {
                    if (towerPrefab != null)
                    {
                        onHoverChanged?.Invoke(towerPrefab, isHovering);
                    }
                }
            );
        }

        /// <summary>
        /// Configura o botão para qualquer ação radial (construir, evoluir ou demolir).
        /// </summary>
        public void ConfigureAction(
            string displayName,
            Sprite icon,
            string costLabel,
            Action onSelected,
            Action<bool> onHoverChanged = null,
            Color? costColorOverride = null,
            Color? iconColorOverride = null)
        {
            _onSelected = onSelected;
            _onHoverChanged = onHoverChanged;
            _costColorOverride = costColorOverride;

            if (iconImage != null)
            {
                if (icon != null)
                {
                    iconImage.sprite = icon;
                }

                iconImage.color = iconColorOverride ?? defaultIconColor;
            }

            if (nameText != null)
            {
                nameText.text = string.IsNullOrWhiteSpace(displayName)
                    ? string.Empty
                    : displayName.ToUpperInvariant();
            }

            if (costText != null)
            {
                costText.text = costLabel;
            }
        }

        /// <summary>
        /// Atualiza interatividade e cor do custo conforme disponibilidade.
        /// </summary>
        public void SetAffordable(bool canAfford)
        {
            if (button != null)
            {
                button.interactable = canAfford;
            }

            if (costText != null)
            {
                costText.color = _costColorOverride ?? (canAfford ? affordableCostColor : unaffordableCostColor);
            }
        }

        private void HandleClicked()
        {
            _onSelected?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _onHoverChanged?.Invoke(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _onHoverChanged?.Invoke(false);
        }

        private void OnDisable()
        {
            _onHoverChanged?.Invoke(false);
        }
    }
}
