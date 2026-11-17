using UnityEngine;

namespace HollowKeep.Level
{
    [CreateAssetMenu(fileName = "Path_New", menuName = "HollowKeep/Level/Path Identifier")]
    public class PathIdentifier : ScriptableObject
    {
        [Tooltip("Cor usada para desenhar os Gizmos e conexões deste caminho na Scene View.")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.2f, 1f);

        [TextArea(2, 4)]
        [Tooltip("Descrição ou anotações sobre a função desta rota (ex: Rota Principal, Flanco Esquerdo).")]
        [SerializeField] private string routeDescription;

        public Color GizmoColor => gizmoColor;
        public string RouteDescription => routeDescription;
    }
}
