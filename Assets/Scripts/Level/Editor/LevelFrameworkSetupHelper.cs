#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HollowKeep.Level;
using HollowKeep.Level.Data;
using HollowKeep.Level.Spawning;
using HollowKeep.Enemies;

namespace HollowKeep.Editor
{
    public static class LevelFrameworkSetupHelper
    {
        [MenuItem("HollowKeep/1. Gerar Assets de Exemplo (Paths, Inimigo, LevelData)")]
        public static void GenerateSampleAssets()
        {
            // Pastas
            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Data/Paths");
            EnsureFolder("Assets/Data/Levels");
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Enemies");

            // 1. PathIdentifier
            string pathAssetPath = "Assets/Data/Paths/Path_Main.asset";
            PathIdentifier pathId = AssetDatabase.LoadAssetAtPath<PathIdentifier>(pathAssetPath);
            if (pathId == null)
            {
                pathId = ScriptableObject.CreateInstance<PathIdentifier>();
                SerializedObject so = new SerializedObject(pathId);
                so.FindProperty("gizmoColor").colorValue = new Color(1f, 0.82f, 0.1f, 1f);
                so.FindProperty("routeDescription").stringValue = "Rota principal sinuosa até o castelo.";
                so.ApplyModifiedProperties();
                AssetDatabase.CreateAsset(pathId, pathAssetPath);
                Debug.Log($"[LevelSetup] Criado: {pathAssetPath}");
            }

            // 2. Prefab do Inimigo Greybox (TestMinion)
            string enemyPrefabPath = "Assets/Prefabs/Enemies/TestMinion.prefab";
            GameObject enemyPrefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath);
            if (enemyPrefabObj == null)
            {
                GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                temp.name = "TestMinion";
                temp.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

                temp.AddComponent<TestMinionEnemy>();

                // Material URP simples avermelhado
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader != null)
                {
                    Material mat = new Material(urpShader) { color = new Color(0.9f, 0.25f, 0.2f) };
                    AssetDatabase.CreateAsset(mat, "Assets/Data/Paths/M_TestMinion.mat");
                    temp.GetComponent<Renderer>().sharedMaterial = mat;
                }

                enemyPrefabObj = PrefabUtility.SaveAsPrefabAsset(temp, enemyPrefabPath);
                Object.DestroyImmediate(temp);
                Debug.Log($"[LevelSetup] Criado Prefab: {enemyPrefabPath}");
            }

            TestMinionEnemy minionPrefab = enemyPrefabObj.GetComponent<TestMinionEnemy>();

            // 3. LevelData (Level_01)
            string levelAssetPath = "Assets/Data/Levels/Level_01.asset";
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(levelAssetPath);
            if (levelData == null)
            {
                levelData = ScriptableObject.CreateInstance<LevelData>();
                SerializedObject levelSo = new SerializedObject(levelData);
                levelSo.FindProperty("levelId").stringValue = "Level_01";
                levelSo.FindProperty("startingGold").intValue = 150;
                levelSo.FindProperty("timeBetweenWaves").floatValue = 5.0f;

                SerializedProperty wavesProp = levelSo.FindProperty("waves");
                wavesProp.ClearArray();
                wavesProp.InsertArrayElementAtIndex(0);

                SerializedProperty wave0 = wavesProp.GetArrayElementAtIndex(0);
                wave0.FindPropertyRelative("duration").floatValue = 12f;

                SerializedProperty subwaves = wave0.FindPropertyRelative("subwaves");
                subwaves.ClearArray();

                // Subwave 0 (0% da onda: 3 minions a cada 1.2s)
                subwaves.InsertArrayElementAtIndex(0);
                SerializedProperty sub0 = subwaves.GetArrayElementAtIndex(0);
                sub0.FindPropertyRelative("wavePercentage").floatValue = 0f;
                SerializedProperty groups0 = sub0.FindPropertyRelative("spawnGroups");
                groups0.ClearArray();
                groups0.InsertArrayElementAtIndex(0);
                SerializedProperty g0 = groups0.GetArrayElementAtIndex(0);
                g0.FindPropertyRelative("enemyPrefab").objectReferenceValue = minionPrefab;
                g0.FindPropertyRelative("quantity").intValue = 3;
                g0.FindPropertyRelative("interval").floatValue = 1.2f;
                g0.FindPropertyRelative("targetPath").objectReferenceValue = pathId;

                // Subwave 1 (50% da onda: 4 minions mais rápidos a cada 0.8s)
                subwaves.InsertArrayElementAtIndex(1);
                SerializedProperty sub1 = subwaves.GetArrayElementAtIndex(1);
                sub1.FindPropertyRelative("wavePercentage").floatValue = 50f;
                SerializedProperty groups1 = sub1.FindPropertyRelative("spawnGroups");
                groups1.ClearArray();
                groups1.InsertArrayElementAtIndex(0);
                SerializedProperty g1 = groups1.GetArrayElementAtIndex(0);
                g1.FindPropertyRelative("enemyPrefab").objectReferenceValue = minionPrefab;
                g1.FindPropertyRelative("quantity").intValue = 4;
                g1.FindPropertyRelative("interval").floatValue = 0.8f;
                g1.FindPropertyRelative("targetPath").objectReferenceValue = pathId;

                levelSo.ApplyModifiedProperties();
                AssetDatabase.CreateAsset(levelData, levelAssetPath);
                Debug.Log($"[LevelSetup] Criado LevelData: {levelAssetPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelSetup] Assets de exemplo criados com sucesso em Assets/Data e Assets/Prefabs!");
        }

        [MenuItem("HollowKeep/2. Montar Fase de Teste na Cena Atual")]
        public static void SetupTestScene()
        {
            GenerateSampleAssets();

            PathIdentifier pathId = AssetDatabase.LoadAssetAtPath<PathIdentifier>("Assets/Data/Paths/Path_Main.asset");
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Levels/Level_01.asset");

            // Chão Greybox
            GameObject ground = GameObject.Find("Ground_Plane");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground_Plane";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(3f, 1f, 3f);
            }

            // Waypoints
            GameObject wpParent = GameObject.Find("Waypoints_Group");
            if (wpParent == null)
            {
                wpParent = new GameObject("Waypoints_Group");
            }

            Vector3[] wpPositions = new[]
            {
                new Vector3(-12f, 0.5f, -6f),
                new Vector3(-4f, 0.5f, 6f),
                new Vector3(4f, 0.5f, -6f),
                new Vector3(12f, 0.5f, 6f)
            };

            Waypoint[] waypoints = new Waypoint[wpPositions.Length];
            for (int i = 0; i < wpPositions.Length; i++)
            {
                string wpName = $"WP_{i + 1:D2}";
                Transform existing = wpParent.transform.Find(wpName);
                GameObject wpObj = existing != null ? existing.gameObject : new GameObject(wpName);
                wpObj.transform.SetParent(wpParent.transform);
                wpObj.transform.position = wpPositions[i];
                Waypoint wp = wpObj.GetComponent<Waypoint>();
                if (wp == null) wp = wpObj.AddComponent<Waypoint>();
                waypoints[i] = wp;
            }

            // EnemyPath
            GameObject pathObj = GameObject.Find("Path_Principal");
            if (pathObj == null)
            {
                pathObj = new GameObject("Path_Principal");
            }
            EnemyPath enemyPath = pathObj.GetComponent<EnemyPath>();
            if (enemyPath == null) enemyPath = pathObj.AddComponent<EnemyPath>();

            SerializedObject pathSo = new SerializedObject(enemyPath);
            pathSo.FindProperty("pathId").objectReferenceValue = pathId;
            SerializedProperty wpsProp = pathSo.FindProperty("waypoints");
            wpsProp.ClearArray();
            for (int i = 0; i < waypoints.Length; i++)
            {
                wpsProp.InsertArrayElementAtIndex(i);
                wpsProp.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
            }
            pathSo.ApplyModifiedProperties();

            // Objeto Mestre [LEVEL] com Registry e Spawner
            GameObject levelMaster = GameObject.Find("[LEVEL]");
            if (levelMaster == null)
            {
                levelMaster = new GameObject("[LEVEL]");
            }

            PathRegistry registry = levelMaster.GetComponent<PathRegistry>();
            if (registry == null) registry = levelMaster.AddComponent<PathRegistry>();
            SerializedObject registrySo = new SerializedObject(registry);
            SerializedProperty pathsProp = registrySo.FindProperty("registeredPaths");
            pathsProp.ClearArray();
            pathsProp.InsertArrayElementAtIndex(0);
            pathsProp.GetArrayElementAtIndex(0).objectReferenceValue = enemyPath;
            registrySo.ApplyModifiedProperties();
            registry.RebuildRegistry();

            WaveSpawner spawner = levelMaster.GetComponent<WaveSpawner>();
            if (spawner == null) spawner = levelMaster.AddComponent<WaveSpawner>();

            SerializedObject spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("levelData").objectReferenceValue = levelData;
            spawnerSo.FindProperty("pathRegistry").objectReferenceValue = registry;
            spawnerSo.FindProperty("autoStartOnPlay").boolValue = true;
            spawnerSo.ApplyModifiedProperties();

            // Ajuste da Câmera principal para visão isométrica
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 18f, -16f);
                cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[LevelSetup] Cena de teste montada com sucesso! Clique em Play para ver os monstros se movimentando.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
