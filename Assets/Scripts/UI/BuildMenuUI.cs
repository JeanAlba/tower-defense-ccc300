using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using HollowKeep.Towers;

namespace HollowKeep.UI
{
    public class BuildMenuUI : MonoBehaviour
    {
        [Header("UI do Menu de Construção")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button directTowerButton;
        [SerializeField] private Button parabolicTowerButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private bool positionMenuAtSlot = true;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Prefabs das Torres")]
        [SerializeField] private DirectTower directTowerPrefab;
        [SerializeField] private ParabolicTower parabolicTowerPrefab;

        [Header("Detecção de Clique")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask slotLayerMask = ~0;

        private BuildSlot _selectedSlot;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (directTowerButton != null)
            {
                directTowerButton.onClick.AddListener(() => TryBuild(directTowerPrefab));
            }

            if (parabolicTowerButton != null)
            {
                parabolicTowerButton.onClick.AddListener(() => TryBuild(parabolicTowerPrefab));
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseMenu);
            }

            CloseMenu();
        }

        private void Update()
        {
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

        public void OpenMenu(BuildSlot slot, Camera cam = null)
        {
            _selectedSlot = slot;

            if (menuPanel == null) return;

            menuPanel.SetActive(true);

            if (positionMenuAtSlot && slot != null)
            {
                Camera activeCam = cam != null ? cam : (targetCamera != null ? targetCamera : Camera.main);
                if (activeCam != null)
                {
                    menuPanel.transform.position = activeCam.WorldToScreenPoint(slot.transform.position + worldOffset);
                }
            }
        }

        public void CloseMenu()
        {
            _selectedSlot = null;
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }
        }

        private void TryBuild(Tower towerPrefab)
        {
            if (_selectedSlot == null || _selectedSlot.IsOccupied || towerPrefab == null)
            {
                return;
            }

            _selectedSlot.BuildTower(towerPrefab);
            CloseMenu();
        }
    }
}
