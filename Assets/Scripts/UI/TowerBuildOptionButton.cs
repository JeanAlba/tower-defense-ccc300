using System;
using HollowKeep.Towers;
using TMPro;
using UnityEngine;

namespace HollowKeep.UI
{
    /// <summary>
    /// Representa um botão modular de opção de torre no anel radial de construção.
    /// </summary>
    public class TowerBuildOptionButton : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private UnityEngine.UI.Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Color affordableCostColor = new Color(0.16f, 0.1f, 0.06f, 1f);
        [SerializeField] private Color unaffordableCostColor = new Color(0.62f, 0.14f, 0.12f, 1f);

        private Tower _towerPrefab;
        private Action<Tower> _onSelected;

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
        public void Configure(Tower towerPrefab, string displayName, Sprite icon, Action<Tower> onSelected)
        {
            _towerPrefab = towerPrefab;
            _onSelected = onSelected;

            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
            }

            if (nameText != null)
            {
                nameText.text = string.IsNullOrWhiteSpace(displayName) && towerPrefab != null
                    ? towerPrefab.TowerName.ToUpperInvariant()
                    : displayName.ToUpperInvariant();
            }

            if (costText != null && towerPrefab != null)
            {
                costText.text = towerPrefab.BuildCost.ToString();
            }
        }

        /// <summary>
        /// Atualiza interatividade e cor do custo conforme o ouro disponível.
        /// </summary>
        public void SetAffordable(bool canAfford)
        {
            if (button != null)
            {
                button.interactable = _towerPrefab != null && canAfford;
            }

            if (costText != null)
            {
                costText.color = canAfford ? affordableCostColor : unaffordableCostColor;
            }
        }

        private void HandleClicked()
        {
            if (_towerPrefab != null)
            {
                _onSelected?.Invoke(_towerPrefab);
            }
        }
    }
}
