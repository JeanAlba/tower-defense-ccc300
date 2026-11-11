using UnityEngine;

namespace HollowKeep.Level
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public class Waypoint : MonoBehaviour
    {
        [Tooltip("Raio da esfera do Gizmo desenhada no editor.")]
        [SerializeField] private float gizmoRadius = 0.35f;

        public Vector3 Position => transform.position;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.8f, 1f);
            Gizmos.DrawSphere(transform.position, gizmoRadius * 1.2f);
        }
    }
}
