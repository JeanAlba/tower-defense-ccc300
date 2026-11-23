using UnityEngine;
using UnityEngine.UI;

namespace HollowKeep.Enemies
{
    /// <summary>
    /// Barra de vida flutuante em World Space criada automaticamente acima do inimigo.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private float heightOffset = 2.1f;
        [SerializeField] private Vector2 barWorldSize = new Vector2(1.1f, 0.14f);
        [SerializeField] private bool hideWhenFull = true;
        [SerializeField] private Color fullHealthColor = new Color(0.25f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color midHealthColor = new Color(0.95f, 0.78f, 0.2f, 1f);
        [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.22f, 0.2f, 1f);

        private Enemy _enemy;
        private Canvas _canvas;
        private RectTransform _fillRect;
        private Image _fillImage;
        private Camera _cachedCamera;
        private bool _isBuilt;

        public void Initialize(Enemy enemy, float customHeightOffset = -1f)
        {
            if (enemy == null) return;

            if (_enemy != null)
            {
                _enemy.OnHealthChanged -= HandleHealthChanged;
                _enemy.OnDied -= HandleEnemyDied;
            }

            _enemy = enemy;
            if (customHeightOffset > 0f)
            {
                heightOffset = customHeightOffset;
            }

            BuildUIIfNeeded();
            ApplyWorldTransform();

            _enemy.OnHealthChanged += HandleHealthChanged;
            _enemy.OnDied += HandleEnemyDied;

            UpdateVisual(_enemy.CurrentHealth, _enemy.MaxHealth);
        }

        private void Awake()
        {
            if (_enemy == null)
            {
                Enemy parentEnemy = GetComponentInParent<Enemy>();
                if (parentEnemy != null)
                {
                    Initialize(parentEnemy);
                }
            }
        }

        private void OnDestroy()
        {
            if (_enemy != null)
            {
                _enemy.OnHealthChanged -= HandleHealthChanged;
                _enemy.OnDied -= HandleEnemyDied;
            }
        }

        private void LateUpdate()
        {
            if (_canvas == null || !_canvas.enabled) return;

            if (_cachedCamera == null)
            {
                _cachedCamera = Camera.main;
            }

            if (_cachedCamera != null)
            {
                transform.rotation = _cachedCamera.transform.rotation;
            }
        }

        private void BuildUIIfNeeded()
        {
            if (_isBuilt) return;
            _isBuilt = true;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 10;

            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100f, 14f);

            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = bgObj.GetComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.1f, 0.82f);
            bgImage.raycastTarget = false;

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(bgObj.transform, false);
            _fillRect = fillObj.GetComponent<RectTransform>();
            _fillRect.anchorMin = Vector2.zero;
            _fillRect.anchorMax = Vector2.one;
            _fillRect.pivot = new Vector2(0f, 0.5f);
            _fillRect.offsetMin = new Vector2(3f, 2f);
            _fillRect.offsetMax = new Vector2(-3f, -2f);

            _fillImage = fillObj.GetComponent<Image>();
            _fillImage.color = fullHealthColor;
            _fillImage.raycastTarget = false;
        }

        private void ApplyWorldTransform()
        {
            if (_enemy == null) return;

            Vector3 parentScale = _enemy.transform.lossyScale;
            float sx = Mathf.Max(0.1f, Mathf.Abs(parentScale.x));
            float sy = Mathf.Max(0.1f, Mathf.Abs(parentScale.y));
            float sz = Mathf.Max(0.1f, Mathf.Abs(parentScale.z));

            transform.localPosition = new Vector3(0f, heightOffset, 0f);
            transform.localScale = new Vector3(
                (barWorldSize.x / 100f) / sx,
                (barWorldSize.y / 14f) / sy,
                0.01f / sz
            );
        }

        private void HandleHealthChanged(int current, int max)
        {
            UpdateVisual(current, max);
        }

        private void HandleEnemyDied(Enemy _)
        {
            if (_canvas != null)
            {
                _canvas.enabled = false;
            }
        }

        private void UpdateVisual(int current, int max)
        {
            if (_canvas == null || _fillRect == null || _fillImage == null) return;

            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            bool shouldShow = current > 0 && (!hideWhenFull || current < max);
            _canvas.enabled = shouldShow;

            _fillRect.localScale = new Vector3(ratio, 1f, 1f);
            _fillImage.color = ratio > 0.5f
                ? Color.Lerp(midHealthColor, fullHealthColor, (ratio - 0.5f) * 2f)
                : Color.Lerp(lowHealthColor, midHealthColor, ratio * 2f);
        }
    }
}
