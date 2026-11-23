using UnityEngine;

namespace HollowKeep.Towers
{
    /// <summary>
    /// Desenha um anel no chão indicando o alcance da torre selecionada ou em construção.
    /// </summary>
    public class TowerRangeIndicator : MonoBehaviour
    {
        [SerializeField] private int segments = 48;
        [SerializeField] private float lineWidth = 0.08f;
        [SerializeField] private float heightOffset = 0.14f;
        [SerializeField] private Color ringColor = new Color(0.25f, 0.85f, 1f, 0.72f);

        private LineRenderer _lineRenderer;
        private Material _runtimeMaterial;

        private void Awake()
        {
            EnsureLineRenderer();
            Hide();
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null)
            {
                Destroy(_runtimeMaterial);
            }
        }

        public void Show(Vector3 worldCenter, float radius)
        {
            if (radius <= 0f)
            {
                Hide();
                return;
            }

            EnsureLineRenderer();

            int count = Mathf.Max(16, segments);
            _lineRenderer.positionCount = count;

            float angleStep = (Mathf.PI * 2f) / count;
            float y = worldCenter.y + heightOffset;

            for (int i = 0; i < count; i++)
            {
                float angle = angleStep * i;
                Vector3 point = new Vector3(
                    worldCenter.x + Mathf.Cos(angle) * radius,
                    y,
                    worldCenter.z + Mathf.Sin(angle) * radius
                );
                _lineRenderer.SetPosition(i, point);
            }

            _lineRenderer.enabled = true;
        }

        public void Hide()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }

        private void EnsureLineRenderer()
        {
            if (_lineRenderer != null) return;

            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            _lineRenderer.useWorldSpace = true;
            _lineRenderer.loop = true;
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.startColor = ringColor;
            _lineRenderer.endColor = ringColor;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader != null)
            {
                _runtimeMaterial = new Material(shader);
                _runtimeMaterial.color = ringColor;
                _lineRenderer.sharedMaterial = _runtimeMaterial;
            }
        }
    }
}
