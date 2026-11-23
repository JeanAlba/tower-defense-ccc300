using UnityEngine;

namespace HollowKeep.Level
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public class Waypoint : MonoBehaviour
    {
        [Tooltip("Raio da esfera do Gizmo desenhada no editor.")]
        [SerializeField] private float gizmoRadius = 0.35f;

        [Tooltip("Raio lateral de dispersão dos inimigos neste waypoint (0 = centro exato).")]
        [Min(0f)]
        [SerializeField] private float spreadRadius = 0.8f;

        public Vector3 Position => transform.position;
        public float SpreadRadius => spreadRadius;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.8f, 1f);
            Gizmos.DrawSphere(transform.position, gizmoRadius * 1.2f);

            if (spreadRadius > 0f)
            {
                DrawSpreadCircleGizmo(20);
            }
        }

        private void DrawSpreadCircleGizmo(int segments)
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.85f);
            Vector3 center = transform.position;
            float angleStep = (Mathf.PI * 2f) / segments;
            Vector3 prev = center + new Vector3(spreadRadius, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * spreadRadius, 0f, Mathf.Sin(angle) * spreadRadius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
