using UnityEngine;

namespace HollowKeep.Towers
{
    [RequireComponent(typeof(Collider))]
    public class BuildSlot : MonoBehaviour
    {
        [Header("Configuração do Slot")]
        [Tooltip("Ponto exato onde a torre será instanciada. Se vazio, usa a posição deste objeto.")]
        [SerializeField] private Transform buildPoint;

        [Tooltip("Visual exibido enquanto o slot está vazio (opcional).")]
        [SerializeField] private GameObject emptyVisual;

        private Tower _builtTower;

        public bool IsOccupied => _builtTower != null;
        public Tower BuiltTower => _builtTower;

        public Tower BuildTower(Tower towerPrefab)
        {
            if (IsOccupied)
            {
                Debug.LogWarning($"[BuildSlot] {gameObject.name} já possui uma torre construída.", this);
                return null;
            }

            if (towerPrefab == null)
            {
                Debug.LogError("[BuildSlot] Prefab de torre nulo!", this);
                return null;
            }

            Vector3 spawnPosition = buildPoint != null ? buildPoint.position : transform.position;
            _builtTower = Instantiate(towerPrefab, spawnPosition, Quaternion.identity, transform);

            if (emptyVisual != null)
            {
                emptyVisual.SetActive(false);
            }

            return _builtTower;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied
                ? new Color(1f, 0.3f, 0.3f, 0.5f)
                : new Color(0.2f, 1f, 0.4f, 0.6f);

            Vector3 center = buildPoint != null ? buildPoint.position : transform.position;
            Gizmos.DrawWireCube(center + Vector3.up * 0.1f, new Vector3(1.2f, 0.2f, 1.2f));
        }
    }
}
