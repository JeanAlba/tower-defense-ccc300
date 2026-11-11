using System.Collections.Generic;
using UnityEngine;

namespace HollowKeep.Level
{
    [DisallowMultipleComponent]
    public class PathRegistry : MonoBehaviour
    {
        [Tooltip("Lista de caminhos na cena. Se deixado vazio, buscará automaticamente nos filhos em Awake.")]
        [SerializeField] private List<EnemyPath> registeredPaths = new();

        private readonly Dictionary<PathIdentifier, EnemyPath> _pathMap = new();

        private void Awake()
        {
            RebuildRegistry();
        }

        public void RebuildRegistry()
        {
            _pathMap.Clear();

            if (registeredPaths.Count == 0)
            {
                GetComponentsInChildren(true, registeredPaths);
                if (registeredPaths.Count == 0)
                {
                    registeredPaths.AddRange(FindObjectsByType<EnemyPath>(FindObjectsInactive.Include, FindObjectsSortMode.None));
                }
            }

            foreach (EnemyPath path in registeredPaths)
            {
                if (path == null) continue;

                if (path.PathId == null)
                {
                    Debug.LogWarning($"[PathRegistry] Caminho no GameObject '{path.gameObject.name}' não possui um PathIdentifier atribuído!", path);
                    continue;
                }

                if (_pathMap.ContainsKey(path.PathId))
                {
                    Debug.LogError($"[PathRegistry] Identificador duplicado '{path.PathId.name}' detectado em '{path.gameObject.name}'.", path);
                }
                else
                {
                    _pathMap[path.PathId] = path;
                }
            }
        }

        public bool TryGetPath(PathIdentifier id, out EnemyPath path)
        {
            if (id == null)
            {
                path = null;
                return false;
            }

            return _pathMap.TryGetValue(id, out path);
        }

        public EnemyPath GetPath(PathIdentifier id)
        {
            if (TryGetPath(id, out EnemyPath path))
            {
                return path;
            }

            Debug.LogError($"[PathRegistry] Caminho com identificador '{(id != null ? id.name : "null")}' não foi encontrado no registro da cena!");
            return null;
        }

        public IReadOnlyCollection<EnemyPath> GetAllPaths() => registeredPaths;
    }
}
