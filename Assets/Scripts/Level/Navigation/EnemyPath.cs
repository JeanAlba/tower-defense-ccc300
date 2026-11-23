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

        /// <summary>
        /// Calcula o vetor unitário lateral no plano XZ perpendicular à tangente da rota no índice informado.
        /// </summary>
        public static Vector3 GetLateralDirection(IReadOnlyList<Waypoint> waypoints, int index)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Count || waypoints[index] == null)
            {
                return Vector3.right;
            }

            Vector3 currentPos = waypoints[index].Position;
            Vector3 prevDir = Vector3.zero;
            bool hasPrev = false;

            for (int i = index - 1; i >= 0; i--)
            {
                if (waypoints[i] == null) continue;
                Vector3 diff = currentPos - waypoints[i].Position;
                diff.y = 0f;
                if (diff.sqrMagnitude > 0.0001f)
                {
                    prevDir = diff.normalized;
                    hasPrev = true;
                }
                break;
            }

            Vector3 nextDir = Vector3.zero;
            bool hasNext = false;

            for (int i = index + 1; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;
                Vector3 diff = waypoints[i].Position - currentPos;
                diff.y = 0f;
                if (diff.sqrMagnitude > 0.0001f)
                {
                    nextDir = diff.normalized;
                    hasNext = true;
                }
                break;
            }

            Vector3 tangent;
            if (hasPrev && hasNext)
            {
                tangent = prevDir + nextDir;
                if (tangent.sqrMagnitude <= 0.0001f)
                {
                    tangent = nextDir;
                }
                else
                {
                    tangent.Normalize();
                }
            }
            else if (hasNext)
            {
                tangent = nextDir;
            }
            else if (hasPrev)
            {
                tangent = prevDir;
            }
            else
            {
                return Vector3.right;
            }

            Vector3 lateral = Vector3.Cross(Vector3.up, tangent);
            return lateral.sqrMagnitude > 0.0001f ? lateral.normalized : Vector3.right;
        }

        /// <summary>
        /// Retorna a posição do waypoint deslocada lateralmente conforme o offset normalizado [-1, 1].
        /// </summary>
        public static Vector3 GetOffsetPosition(IReadOnlyList<Waypoint> waypoints, int index, float normalizedLateralOffset)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Count)
            {
                return Vector3.zero;
            }

            Waypoint wp = waypoints[index];
            if (wp == null)
            {
                return Vector3.zero;
            }

            if (wp.SpreadRadius <= 0f || Mathf.Approximately(normalizedLateralOffset, 0f))
            {
                return wp.Position;
            }

            Vector3 lateral = GetLateralDirection(waypoints, index);
            return wp.Position + lateral * (Mathf.Clamp(normalizedLateralOffset, -1f, 1f) * wp.SpreadRadius);
        }

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

                    if (current.SpreadRadius > 0f)
                    {
                        Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.75f);
                        Gizmos.DrawLine(
                            GetOffsetPosition(waypoints, i, -1f),
                            GetOffsetPosition(waypoints, i, 1f)
                        );
                        Gizmos.color = baseColor;
                    }
                }

                // Desenha linha até o próximo ponto
                if (i < waypoints.Length - 1)
                {
                    Waypoint next = waypoints[i + 1];
                    if (next != null)
                    {
                        Vector3 nextPos = next.Position;
                        Gizmos.DrawLine(currentPos, nextPos);

                        if (current.SpreadRadius > 0f || next.SpreadRadius > 0f)
                        {
                            Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, isSelected ? 0.7f : 0.2f);
                            Gizmos.DrawLine(
                                GetOffsetPosition(waypoints, i, -1f),
                                GetOffsetPosition(waypoints, i + 1, -1f)
                            );
                            Gizmos.DrawLine(
                                GetOffsetPosition(waypoints, i, 1f),
                                GetOffsetPosition(waypoints, i + 1, 1f)
                            );
                            Gizmos.color = baseColor;
                        }

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
