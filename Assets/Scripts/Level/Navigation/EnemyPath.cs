using System.Collections.Generic;
using UnityEngine;

namespace HollowKeep.Level
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public class EnemyPath : MonoBehaviour
    {
        [Tooltip("Identificador único tipado desta rota (arraste o asset PathIdentifier correspondente).")]
        [SerializeField] private PathIdentifier pathId;

        [Tooltip("Lista estritamente tipada e ordenada de Waypoints por onde os inimigos passarão.")]
        [SerializeField] private Waypoint[] waypoints;

        public PathIdentifier PathId => pathId;
        public IReadOnlyList<Waypoint> Waypoints => waypoints;
        public int WaypointCount => waypoints != null ? waypoints.Length : 0;
        public Waypoint StartWaypoint => (waypoints != null && waypoints.Length > 0) ? waypoints[0] : null;
        public Waypoint EndWaypoint => (waypoints != null && waypoints.Length > 0) ? waypoints[^1] : null;

        private void OnDrawGizmos()
        {
            DrawPathGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawPathGizmos(true);
        }

        private void DrawPathGizmos(bool isSelected)
        {
            if (waypoints == null || waypoints.Length == 0) return;

            Color baseColor = (pathId != null) ? pathId.GizmoColor : Color.yellow;
            if (!isSelected)
            {
                baseColor.a = 0.35f;
            }

            Gizmos.color = baseColor;

            for (int i = 0; i < waypoints.Length; i++)
            {
                Waypoint current = waypoints[i];
                if (current == null) continue;

                Vector3 currentPos = current.Position;

                // Desenha nó do caminho
                if (isSelected)
                {
                    Gizmos.DrawSphere(currentPos, 0.35f);
                }

                // Desenha linha até o próximo ponto
                if (i < waypoints.Length - 1)
                {
                    Waypoint next = waypoints[i + 1];
                    if (next != null)
                    {
                        Vector3 nextPos = next.Position;
                        Gizmos.DrawLine(currentPos, nextPos);

                        // Desenha um ponto intermediário para indicar direção
                        if (isSelected)
                        {
                            Vector3 midPoint = Vector3.Lerp(currentPos, nextPos, 0.6f);
                            Gizmos.DrawWireCube(midPoint, Vector3.one * 0.2f);
                        }
                    }
                }
            }
        }
    }
}
