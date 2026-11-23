#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using HollowKeep.Enemies;
using HollowKeep.Level.Data;
using HollowKeep.Level.Spawning;
using HollowKeep.Towers;
using HollowKeep.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HollowKeep.Level.Editor
{
    /// <summary>
    /// Ferramenta de reconstrução dos mapas sem grid (Unity Terrain normal-mapped em escala 1:1) e extração do núcleo LevelCore.
    /// </summary>
    public static class HollowKeepMapBuilder
    {
        private const string Level01ScenePath = "Assets/Scenes/Level_01.unity";
        private const string Level02ScenePath = "Assets/Scenes/Level_02.unity";
        private const string LevelCorePrefabPath = "Assets/Prefabs/LevelCore.prefab";

        private const string BuildSlotPrefabPath = "Assets/Prefabs/Towers/BuildSlot.prefab";
        private const string DirectTowerPrefabPath = "Assets/Prefabs/Towers/DirectTower.prefab";
        private const string ParabolicTowerPrefabPath = "Assets/Prefabs/Towers/ParabolicTower.prefab";

        private const string DifficultyNormalPath = "Assets/Data/Difficulty/Difficulty_Normal.asset";
        private const string VolumeProfilePath = "Assets/Settings/SampleSceneProfile.asset";

        private const string TerrainShaderName = "Universal Render Pipeline/Terrain/Lit";
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        private const string TerrainMaterialPath = "Assets/Materials/M_HollowKeep_Terrain.mat";
        private const string MiniForestMaterialPath = "Assets/Materials/M_Kenney_MiniForest.mat";
        private const string MiniForestTexturePath = "Assets/KenneyPack/textures/colormap_mini-forest.png";
        private const string SurvivalMaterialPath = "Assets/Materials/M_Kenney_Survival.mat";
        private const string SurvivalTexturePath = "Assets/KenneyPack/textures/colormap_survival.png";
        private const string GraveyardMaterialPath = "Assets/Materials/M_Kenney_Graveyard.mat";
        private const string CastleMaterialPath = "Assets/Materials/M_Kenney_Castle.mat";
        private const string MiniDungeonMaterialPath = "Assets/Materials/M_Kenney_MiniDungeon.mat";
        private const string GlowWarmFlameMatPath = "Assets/Materials/M_Glow_WarmFlame.mat";
        private const string GlowLanternAmberMatPath = "Assets/Materials/M_Glow_LanternAmber.mat";
        private const string GlowEerieSoulMatPath = "Assets/Materials/M_Glow_EerieSoul.mat";
        private const string VfxAdditiveMatPath = "Assets/Materials/VFX/M_VFX_Additive.mat";

        private const string TerrainTextureDir = "Assets/Textures/Terrain";
        private const string TerrainLayerDir = "Assets/Data/Terrains/Layers";
        private const string TerrainDataDir = "Assets/Data/Terrains";

        private const string PathMainAsset = "Assets/Data/Paths/Path_Main.asset";
        private const string PathNorthAsset = "Assets/Data/Paths/Path_North.asset";
        private const string PathSouthAsset = "Assets/Data/Paths/Path_South.asset";
        private const string Level01DataAsset = "Assets/Data/Levels/Level_01.asset";
        private const string Level02DataAsset = "Assets/Data/Levels/Level_02.asset";

        private const string EsqueletoPrefabPath = "Assets/Prefabs/Enemies/Esqueleto.prefab";
        private const string CorredorPrefabPath = "Assets/Prefabs/Enemies/Corredor.prefab";
        private const string CultistaPrefabPath = "Assets/Prefabs/Enemies/Cultista.prefab";
        private const string BrutamontesPrefabPath = "Assets/Prefabs/Enemies/Brutamontes.prefab";
        private const string CampeaoPrefabPath = "Assets/Prefabs/Enemies/Campeao.prefab";

        private const string CastleKitDir = "Assets/KenneyPack/kenney/kenney_castle-kit/Models/FBX format/";
        private const string GraveyardKitDir = "Assets/KenneyPack/kenney/kenney_graveyard-kit_5.0/Models/FBX format/";
        private const string MiniForestKitDir = "Assets/KenneyPack/kenney/kenney_mini-forest_1.0/Models/FBX format/";
        private const string MiniDungeonKitDir = "Assets/KenneyPack/kenney/kenney_mini-dungeon/Models/FBX format/";
        private const string SurvivalKitDir = "Assets/KenneyPack/kenney/kenney_survival-kit/Models/FBX format/";
        private const string TowerDefenseKitDir = "Assets/KenneyPack/kenney/kenney_tower-defense-kit/Models/FBX format/";

        private enum TerrainPatternMode
        {
            Grass,
            Dirt,
            Moss,
            Cobble,
            Flagstone
        }

        private readonly struct WaypointSpec
        {
            public readonly Vector3 Position;
            public readonly float SpreadRadius;

            public WaypointSpec(float x, float y, float z, float spreadRadius)
            {
                Position = new Vector3(x, y, z);
                SpreadRadius = spreadRadius;
            }
        }

        private readonly struct RoadPolyline
        {
            public readonly Vector3[] SmoothPoints;
            public readonly float[] SmoothSpreads;
            public readonly float[] BaseHalfWidths;

            public RoadPolyline(Vector3[] points, float[] spreads, float[] baseHalfWidths)
            {
                SmoothPoints = points;
                SmoothSpreads = spreads;
                BaseHalfWidths = baseHalfWidths;
            }
        }

        private readonly struct CobbleZone
        {
            public readonly Vector3 Center;
            public readonly float Radius;

            public CobbleZone(Vector3 center, float radius)
            {
                Center = center;
                Radius = radius;
            }
        }

        [MenuItem("HollowKeep/Maps/1. Create Terrain Assets, Glow Materials and Profile")]
        public static void CreateTerrainAssets()
        {
            EnsureFolder(TerrainTextureDir);
            EnsureFolder(TerrainLayerDir);
            EnsureFolder(TerrainDataDir);

            EnsureMatteColormapMaterial(MiniForestMaterialPath, MiniForestTexturePath, new Color(0.84f, 0.88f, 0.84f, 1f));
            EnsureMatteColormapMaterial(SurvivalMaterialPath, SurvivalTexturePath, new Color(0.80f, 0.84f, 0.79f, 1f));
            EnsureEmissiveGlowMaterial(GlowWarmFlameMatPath, new Color(1f, 0.65f, 0.18f, 1f), new Color(1.0f, 0.50f, 0.10f) * 3.8f);
            EnsureEmissiveGlowMaterial(GlowLanternAmberMatPath, new Color(1f, 0.85f, 0.45f, 1f), new Color(1.0f, 0.78f, 0.30f) * 3.4f);
            EnsureEmissiveGlowMaterial(GlowEerieSoulMatPath, new Color(0.25f, 0.95f, 0.88f, 1f), new Color(0.18f, 0.92f, 0.85f) * 3.2f);
            EnsureTerrainMaterial();
            TunePostProcessingProfile();

            EnsureTerrainTexturePairAndLayer(
                "T_Terrain_DarkGrass.png", "T_Terrain_DarkGrass_N.png", "TL_DarkGrass.terrainlayer",
                new Vector2(6f, 6f), 0.38f,
                new Color32(30, 62, 34, 1), new Color32(48, 86, 44, 1), new Color32(18, 42, 24, 1),
                11, 12f, 2.2f, TerrainPatternMode.Grass);

            EnsureTerrainTexturePairAndLayer(
                "T_Terrain_DirtRoad.png", "T_Terrain_DirtRoad_N.png", "TL_DirtRoad.terrainlayer",
                new Vector2(5.5f, 5.5f), 0.45f,
                new Color32(192, 154, 98, 1), new Color32(222, 184, 124, 1), new Color32(154, 118, 72, 1),
                29, 14f, 2.5f, TerrainPatternMode.Dirt);

            EnsureTerrainTexturePairAndLayer(
                "T_Terrain_MossBorder.png", "T_Terrain_MossBorder_N.png", "TL_MossBorder.terrainlayer",
                new Vector2(7f, 7f), 0.35f,
                new Color32(13, 27, 18, 1), new Color32(22, 42, 26, 1), new Color32(8, 17, 12, 1),
                47, 10f, 2.0f, TerrainPatternMode.Moss);

            EnsureTerrainTexturePairAndLayer(
                "T_Terrain_CobbleDetail.png", "T_Terrain_CobbleDetail_N.png", "TL_CobbleDetail.terrainlayer",
                new Vector2(3.2f, 3.2f), 0.65f,
                new Color32(134, 124, 108, 1), new Color32(164, 152, 132, 1), new Color32(66, 60, 54, 1),
                53, 8f, 3.4f, TerrainPatternMode.Cobble);

            EnsureTerrainTexturePairAndLayer(
                "T_Terrain_DarkStone.png", "T_Terrain_DarkStone_N.png", "TL_DarkStone.terrainlayer",
                new Vector2(6f, 6f), 0.48f,
                new Color32(28, 38, 48, 1), new Color32(42, 54, 66, 1), new Color32(18, 25, 34, 1),
                63, 10f, 2.6f, TerrainPatternMode.Flagstone);

            EnsureTerrainTexturePairAndLayer(
                "T_Terrain_CourtyardRoad.png", "T_Terrain_CourtyardRoad_N.png", "TL_CourtyardRoad.terrainlayer",
                new Vector2(5.5f, 5.5f), 0.50f,
                new Color32(174, 152, 116, 1), new Color32(202, 178, 140, 1), new Color32(138, 118, 88, 1),
                81, 14f, 2.6f, TerrainPatternMode.Dirt);

            EnsureTerrainTexturePairAndLayer(
                "T_Terrain_FortressRim.png", "T_Terrain_FortressRim_N.png", "TL_FortressRim.terrainlayer",
                new Vector2(7f, 7f), 0.40f,
                new Color32(16, 20, 29, 1), new Color32(26, 32, 44, 1), new Color32(10, 14, 21, 1),
                97, 9f, 2.2f, TerrainPatternMode.Moss);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HollowKeepMapBuilder] Materiais, glow emissivo, normal maps e camadas foscas de Terrain atualizados.");
        }

        [MenuItem("HollowKeep/Maps/2. Extract LevelCore and Normalize 1:1")]
        public static string ExtractLevelCoreAndNormalizePrefabs()
        {
            if (File.Exists(Level01ScenePath) && SceneManager.GetActiveScene().path != Level01ScenePath)
            {
                EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
            }

            if (!File.Exists(LevelCorePrefabPath))
            {
                GameObject levelObj = GameObject.Find("[LEVEL]");
                GameObject hudObj = GameObject.Find("GameHUDCanvas");
                GameObject buildMenuObj = GameObject.Find("BuildMenuCanvas");
                GameObject eventSystemObj = GameObject.Find("EventSystem");

                if (levelObj == null || hudObj == null || buildMenuObj == null || eventSystemObj == null)
                {
                    throw new InvalidOperationException(
                        $"Objetos obrigatórios ausentes em Level_01 para extrair LevelCore: " +
                        $"[LEVEL]={levelObj != null}, GameHUDCanvas={hudObj != null}, BuildMenuCanvas={buildMenuObj != null}, EventSystem={eventSystemObj != null}");
                }

                GameObject levelCoreRoot = new GameObject("LevelCore");
                levelCoreRoot.transform.position = Vector3.zero;
                levelCoreRoot.transform.rotation = Quaternion.identity;
                levelCoreRoot.transform.localScale = Vector3.one;

                levelObj.transform.SetParent(levelCoreRoot.transform, true);
                hudObj.transform.SetParent(levelCoreRoot.transform, true);
                buildMenuObj.transform.SetParent(levelCoreRoot.transform, true);
                eventSystemObj.transform.SetParent(levelCoreRoot.transform, true);

                DifficultyProfile normalDiff = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(DifficultyNormalPath);
                WaveSpawner spawner = levelObj.GetComponent<WaveSpawner>();
                PathRegistry registry = levelObj.GetComponent<PathRegistry>();
                GameHUDController hud = hudObj.GetComponent<GameHUDController>();
                BuildMenuUI buildMenu = buildMenuObj.GetComponent<BuildMenuUI>();

                if (registry != null)
                {
                    SerializedObject soReg = new SerializedObject(registry);
                    SerializedProperty prop = soReg.FindProperty("registeredPaths");
                    if (prop != null && prop.isArray)
                    {
                        prop.ClearArray();
                    }
                    soReg.ApplyModifiedPropertiesWithoutUndo();
                }

                if (spawner != null)
                {
                    SerializedObject soSp = new SerializedObject(spawner);
                    soSp.FindProperty("pathRegistry").objectReferenceValue = registry;
                    soSp.FindProperty("defaultDifficulty").objectReferenceValue = normalDiff;
                    soSp.ApplyModifiedPropertiesWithoutUndo();
                }

                if (hud != null)
                {
                    SerializedObject soHud = new SerializedObject(hud);
                    soHud.FindProperty("waveSpawner").objectReferenceValue = spawner;
                    soHud.FindProperty("defaultDifficulty").objectReferenceValue = normalDiff;
                    soHud.ApplyModifiedPropertiesWithoutUndo();
                }

                if (buildMenu != null)
                {
                    SerializedObject soBm = new SerializedObject(buildMenu);
                    soBm.FindProperty("hudController").objectReferenceValue = hud;
                    soBm.FindProperty("targetCamera").objectReferenceValue = null;
                    soBm.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAssetAndConnect(levelCoreRoot, LevelCorePrefabPath, InteractionMode.AutomatedAction);
            }

            VerifyLevelCorePrefabIntegrity();
            NormalizeBuildSlotAndGameplayStats();
            AssetDatabase.SaveAssets();

            Debug.Log("[HollowKeepMapBuilder] LevelCore.prefab validado e escala/stats 1:1 normalizados.");
            return "OK";
        }

        [MenuItem("HollowKeep/Maps/3. Build Level_01 (Terrain 1:1 + Glow-Up)")]
        public static void BuildLevel01()
        {
            if (!File.Exists(LevelCorePrefabPath) || !File.Exists(TerrainMaterialPath))
            {
                throw new InvalidOperationException("Execute CreateTerrainAssets e ExtractLevelCoreAndNormalizePrefabs antes de BuildLevel01.");
            }

            if (SceneManager.GetActiveScene().path != Level01ScenePath)
            {
                EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
            }
            ClearActiveSceneRoots();

            GameObject systemsRoot = new GameObject("01_Systems");
            GameObject camLightRoot = new GameObject("02_CameraAndLighting");
            GameObject terrainRoot = new GameObject("03_Terrain");
            GameObject envRoot = new GameObject("04_Environment");
            GameObject pathsRoot = new GameObject("05_Paths");
            GameObject slotsRoot = new GameObject("06_BuildSlots");

            // 1. Systems (LevelCore)
            GameObject levelCorePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelCorePrefabPath);
            GameObject levelCoreInst = (GameObject)PrefabUtility.InstantiatePrefab(levelCorePrefab);
            levelCoreInst.transform.SetParent(systemsRoot.transform, false);
            ConfigureLevelCoreInstance(
                levelCoreInst,
                AssetDatabase.LoadAssetAtPath<LevelData>(Level01DataAsset),
                "Level_02");

            // 2. Camera & Atmosphere Lighting (luar azulado + calor nas tochas/fogueira + spawn espectral)
            SetupCameraAndBaseLighting(
                camLightRoot.transform,
                new Vector3(0.2f, 15.6f, -10.9f),
                new Vector3(56f, 0f, 0f),
                new Vector3(50f, 322f, 0f),
                new Color(0.68f, 0.78f, 0.96f, 1f),
                0.95f,
                new Color(0.07f, 0.10f, 0.14f, 1f),
                0.0055f,
                new Color(0.24f, 0.28f, 0.34f, 1f));

            CreatePointLight(camLightRoot.transform, "Light_Spawn_Eerie", new Vector3(12.2f, 2.0f, 4.3f), new Color(0.22f, 0.90f, 0.85f), 5.0f, 8.0f);
            CreatePointLight(camLightRoot.transform, "Light_Castle_N", new Vector3(-7.4f, 1.55f, -3.5f), new Color(1.0f, 0.56f, 0.18f), 5.4f, 8.0f);
            CreatePointLight(camLightRoot.transform, "Light_Castle_S", new Vector3(-7.4f, 1.55f, -5.5f), new Color(1.0f, 0.56f, 0.18f), 5.4f, 8.0f);
            CreatePointLight(camLightRoot.transform, "Light_Campfire_Warm", new Vector3(-9.5f, 1.35f, 2.2f), new Color(1.0f, 0.52f, 0.16f), 4.8f, 7.5f);
            CreatePointLight(camLightRoot.transform, "Light_Shrine_SE", new Vector3(7.4f, 1.6f, -4.3f), new Color(1.0f, 0.65f, 0.25f), 4.0f, 7.0f);
            CreatePointLight(camLightRoot.transform, "Light_Crypt_NW", new Vector3(-6.2f, 1.4f, 6.2f), new Color(0.95f, 0.62f, 0.24f), 3.0f, 6.0f);

            // 3. Path_Main (~50m, curvas suaves estilo referência terminando no castelo no canto inferior esquerdo)
            WaypointSpec[] mainSpecs = new[]
            {
                new WaypointSpec(17.2f, 0.05f,  4.5f, 0.55f),
                new WaypointSpec(12.2f, 0.05f,  4.2f, 0.55f),
                new WaypointSpec( 8.8f, 0.05f,  3.7f, 0.55f),
                new WaypointSpec( 5.6f, 0.05f,  4.4f, 0.55f),
                new WaypointSpec( 4.6f, 0.05f,  7.6f, 0.55f),
                new WaypointSpec( 1.8f, 0.05f,  8.8f, 0.50f),
                new WaypointSpec(-0.8f, 0.05f,  7.4f, 0.50f),
                new WaypointSpec(-1.0f, 0.05f,  4.3f, 0.42f),
                new WaypointSpec(-4.6f, 0.05f,  3.7f, 0.55f),
                new WaypointSpec(-6.6f, 0.05f,  1.6f, 0.52f),
                new WaypointSpec(-4.8f, 0.05f, -0.5f, 0.55f),
                new WaypointSpec( 0.4f, 0.05f, -0.4f, 0.55f),
                new WaypointSpec( 3.0f, 0.05f, -2.0f, 0.48f),
                new WaypointSpec( 1.2f, 0.05f, -4.1f, 0.45f),
                new WaypointSpec(-3.6f, 0.05f, -4.5f, 0.35f),
                new WaypointSpec(-7.8f, 0.05f, -4.5f, 0.25f),
            };
            BuildPathObject(pathsRoot.transform, "Path_Main", PathMainAsset, mainSpecs);

            // 4. BuildSlots (9 posições fiéis aos bolsões da imagem + defesa do castelo)
            Vector3[] slotPositions = new[]
            {
                new Vector3( 1.8f, 0.03f,  6.5f), // U superior (alto)
                new Vector3( 1.8f, 0.03f,  4.3f), // U superior (baixo)
                new Vector3(-3.4f, 0.03f,  5.8f), // Bolsão esquerdo superior
                new Vector3(-4.0f, 0.03f,  1.6f), // Bolsão crescente oeste
                new WaypointSpec( 0.6f, 0.03f,  1.7f, 0f).Position, // Clareira central
                new Vector3( 0.2f, 0.03f, -2.3f), // Bolsão crescente leste-sul
                new Vector3(-2.8f, 0.03f, -2.4f), // Bolsão oeste inferior
                new Vector3( 5.4f, 0.03f, -2.0f), // Flanco direito inferior
                new Vector3(-5.0f, 0.03f, -6.2f), // Última linha sul-esquerda antes do portão
            };

            List<RoadPolyline> roads = new() { BuildRoadPolyline(mainSpecs, 1.0f) };
            Vector3 castleCenter = new Vector3(-8.8f, 0f, -4.5f);
            Vector3 spawnCenter = new Vector3(12.2f, 0f, 4.2f);
            Vector3[] spawns = new[] { spawnCenter };

            // Pré-cálculo dos bosques densos e pátios de paralelepípedo
            TerrainLayer[] layers01 = new[]
            {
                LoadTerrainLayer("TL_DarkGrass.terrainlayer"),
                LoadTerrainLayer("TL_DirtRoad.terrainlayer"),
                LoadTerrainLayer("TL_MossBorder.terrainlayer"),
                LoadTerrainLayer("TL_CobbleDetail.terrainlayer")
            };

            CobbleZone[] cobbleZones01 = new[]
            {
                new CobbleZone(castleCenter + new Vector3(1.5f, 0f, 0f), 2.4f),
                new CobbleZone(spawnCenter, 1.7f)
            };

            Vector3[] groveCenters01 = new[]
            {
                new Vector3(-11.5f, 0f, 9.5f),
                new Vector3(-6.2f, 0f, 9.6f),
                new Vector3( 9.5f, 0f, 9.4f),
                new Vector3(12.8f, 0f, 9.2f),
                new Vector3(10.8f, 0f, 0.5f),
                new Vector3(13.8f, 0f, 0.2f),
                new Vector3(12.8f, 0f, -4.5f),
                new Vector3( 2.0f, 0f, -8.2f),
                new Vector3( 8.5f, 0f, -8.0f),
                new Vector3( 3.8f, 0f,  1.6f)
            };

            Terrain terrain = BuildTerrain(
                terrainRoot.transform,
                "Terrain_Level_01",
                $"{TerrainDataDir}/TD_Level_01.asset",
                layers01,
                roads,
                slotPositions,
                castleCenter,
                spawns,
                groveCenters01,
                cobbleZones01,
                seed: 101);

            PlaceBuildSlots(slotsRoot.transform, slotPositions, terrain);
            PopulateLevel01Environment(envRoot.transform, terrain, roads, slotPositions, castleCenter, spawnCenter);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), Level01ScenePath);
            Debug.Log("[HollowKeepMapBuilder] Level_01.unity reconstruída com polish visual e glow-up.");
        }

        [MenuItem("HollowKeep/Maps/4. Create Level_02 Data Assets")]
        public static void CreateLevel02DataAssets()
        {
            EnsurePathIdentifier(PathNorthAsset, new Color(1.0f, 0.55f, 0.15f, 1f), "Rota norte pela península da muralha.");
            EnsurePathIdentifier(PathSouthAsset, new Color(0.35f, 0.85f, 1.0f, 1f), "Rota sul de assalto rápido convergente.");

            PathIdentifier pathNorth = AssetDatabase.LoadAssetAtPath<PathIdentifier>(PathNorthAsset);
            PathIdentifier pathSouth = AssetDatabase.LoadAssetAtPath<PathIdentifier>(PathSouthAsset);

            Enemy esqueleto = AssetDatabase.LoadAssetAtPath<Enemy>(EsqueletoPrefabPath);
            Enemy corredor = AssetDatabase.LoadAssetAtPath<Enemy>(CorredorPrefabPath);
            Enemy cultista = AssetDatabase.LoadAssetAtPath<Enemy>(CultistaPrefabPath);
            Enemy brutamontes = AssetDatabase.LoadAssetAtPath<Enemy>(BrutamontesPrefabPath);
            Enemy campeao = AssetDatabase.LoadAssetAtPath<Enemy>(CampeaoPrefabPath);

            LevelData level02 = AssetDatabase.LoadAssetAtPath<LevelData>(Level02DataAsset);
            if (level02 == null)
            {
                level02 = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(level02, Level02DataAsset);
            }

            SerializedObject so = new SerializedObject(level02);
            so.FindProperty("levelId").stringValue = "Level_02";
            so.FindProperty("stageTitle").stringValue = "FASE 2";
            so.FindProperty("stageDescription").stringValue =
                "Comandante!\n\nAs hordas marcham por duas brechas rumo às muralhas de Hollow Keep. Divida suas torres e segure o gargalo antes que o portão caia!";
            so.FindProperty("mapPreviewVariant").intValue = 1;
            so.FindProperty("startingGold").intValue = 180;
            so.FindProperty("timeBetweenWaves").floatValue = 12f;

            SerializedProperty wavesProp = so.FindProperty("waves");
            wavesProp.ClearArray();

            AddWave(wavesProp, 26f, new[]
            {
                (0f, new[] { (esqueleto, 8, 1.2f, pathNorth) }),
                (45f, new[] { (esqueleto, 4, 1.3f, pathSouth) })
            });

            AddWave(wavesProp, 35f, new[]
            {
                (0f, new[] { (esqueleto, 8, 1.0f, pathNorth), (esqueleto, 4, 1.2f, pathSouth) }),
                (45f, new[] { (corredor, 6, 0.75f, pathNorth), (corredor, 4, 0.85f, pathSouth) })
            });

            AddWave(wavesProp, 44f, new[]
            {
                (0f, new[] { (esqueleto, 10, 0.85f, pathNorth), (corredor, 6, 0.75f, pathSouth) }),
                (45f, new[] { (cultista, 4, 1.3f, pathSouth), (brutamontes, 3, 2.2f, pathNorth) })
            });

            AddWave(wavesProp, 52f, new[]
            {
                (0f, new[] { (corredor, 10, 0.65f, pathSouth), (cultista, 6, 1.1f, pathNorth) }),
                (40f, new[] { (brutamontes, 4, 2.0f, pathNorth), (cultista, 4, 1.2f, pathSouth) }),
                (72f, new[] { (brutamontes, 3, 2.2f, pathSouth) })
            });

            AddWave(wavesProp, 60f, new[]
            {
                (0f, new[] { (esqueleto, 12, 0.85f, pathNorth), (corredor, 8, 0.65f, pathSouth) }),
                (35f, new[] { (cultista, 6, 1.1f, pathNorth), (brutamontes, 4, 2.0f, pathSouth) }),
                (68f, new[] { (brutamontes, 2, 2.0f, pathNorth), (cultista, 4, 1.2f, pathSouth), (campeao, 1, 1.0f, pathNorth) })
            });

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(level02);
            AssetDatabase.SaveAssets();
            Debug.Log("[HollowKeepMapBuilder] Path_North, Path_South e Level_02.asset atualizados.");
        }

        [MenuItem("HollowKeep/Maps/5. Build Level_02 (Terrain 1:1)")]
        public static void BuildLevel02()
        {
            if (!File.Exists(LevelCorePrefabPath) || !File.Exists(TerrainMaterialPath) || !File.Exists(Level02DataAsset))
            {
                throw new InvalidOperationException("Execute as etapas anteriores antes de BuildLevel02.");
            }

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(newScene);

            GameObject systemsRoot = new GameObject("01_Systems");
            GameObject camLightRoot = new GameObject("02_CameraAndLighting");
            GameObject terrainRoot = new GameObject("03_Terrain");
            GameObject envRoot = new GameObject("04_Environment");
            GameObject pathsRoot = new GameObject("05_Paths");
            GameObject slotsRoot = new GameObject("06_BuildSlots");

            // 1. Systems (LevelCore)
            GameObject levelCorePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelCorePrefabPath);
            GameObject levelCoreInst = (GameObject)PrefabUtility.InstantiatePrefab(levelCorePrefab);
            levelCoreInst.transform.SetParent(systemsRoot.transform, false);
            ConfigureLevelCoreInstance(
                levelCoreInst,
                AssetDatabase.LoadAssetAtPath<LevelData>(Level02DataAsset),
                "");

            // 2. Camera & Lighting
            SetupCameraAndBaseLighting(
                camLightRoot.transform,
                new Vector3(0.4f, 16.2f, -11.3f),
                new Vector3(56f, 0f, 0f),
                new Vector3(52f, 325f, 0f),
                new Color(0.68f, 0.76f, 0.94f, 1f),
                0.90f,
                new Color(0.08f, 0.09f, 0.14f, 1f),
                0.006f,
                new Color(0.22f, 0.25f, 0.32f, 1f));

            CreatePointLight(camLightRoot.transform, "Light_Spawn_North", new Vector3(12.2f, 2.0f, 7.0f), new Color(0.35f, 0.85f, 0.95f), 4.2f, 7.5f);
            CreatePointLight(camLightRoot.transform, "Light_Spawn_South", new Vector3(12.2f, 2.0f, 2.1f), new Color(0.35f, 0.85f, 0.95f), 4.2f, 7.5f);
            CreatePointLight(camLightRoot.transform, "Light_Convergence", new Vector3(-1.4f, 1.6f, 0.8f), new Color(1.0f, 0.62f, 0.24f), 4.0f, 7.0f);
            CreatePointLight(camLightRoot.transform, "Light_Castle_N", new Vector3(-9.5f, 1.6f, -0.8f), new Color(1.0f, 0.56f, 0.20f), 5.4f, 8.0f);
            CreatePointLight(camLightRoot.transform, "Light_Castle_S", new Vector3(-9.5f, 1.6f, -2.8f), new Color(1.0f, 0.56f, 0.20f), 5.4f, 8.0f);

            // 3. Paths
            WaypointSpec[] northSpecs = new[]
            {
                new WaypointSpec( 17.2f, 0.05f,  7.0f, 0.55f),
                new WaypointSpec( 12.2f, 0.05f,  7.0f, 0.55f),
                new WaypointSpec(  7.0f, 0.05f,  7.1f, 0.55f),
                new WaypointSpec(  1.8f, 0.05f,  7.1f, 0.55f),
                new WaypointSpec( -2.6f, 0.05f,  6.7f, 0.50f),
                new WaypointSpec( -4.6f, 0.05f,  5.0f, 0.50f),
                new WaypointSpec( -4.6f, 0.05f,  2.8f, 0.45f),
                new WaypointSpec( -3.4f, 0.05f,  1.4f, 0.45f),
                new WaypointSpec( -1.4f, 0.05f,  0.8f, 0.45f),
                new WaypointSpec(  1.2f, 0.05f, -0.4f, 0.45f),
                new WaypointSpec(  4.4f, 0.05f, -1.8f, 0.45f),
                new WaypointSpec(  5.3f, 0.05f, -3.9f, 0.45f),
                new WaypointSpec(  2.8f, 0.05f, -5.4f, 0.45f),
                new WaypointSpec( -0.8f, 0.05f, -4.9f, 0.40f),
                new WaypointSpec( -3.8f, 0.05f, -3.2f, 0.35f),
                new WaypointSpec( -6.6f, 0.05f, -1.9f, 0.30f),
                new WaypointSpec(-10.2f, 0.05f, -1.8f, 0.25f),
            };
            BuildPathObject(pathsRoot.transform, "Path_North", PathNorthAsset, northSpecs);

            WaypointSpec[] southSpecs = new[]
            {
                new WaypointSpec( 17.2f, 0.05f,  2.1f, 0.55f),
                new WaypointSpec( 12.2f, 0.05f,  2.1f, 0.55f),
                new WaypointSpec(  8.6f, 0.05f,  2.3f, 0.55f),
                new WaypointSpec(  5.2f, 0.05f,  2.9f, 0.50f),
                new WaypointSpec(  1.8f, 0.05f,  3.1f, 0.50f),
                new WaypointSpec( -0.6f, 0.05f,  2.6f, 0.45f),
                new WaypointSpec( -1.6f, 0.05f,  1.9f, 0.45f),
                new WaypointSpec( -1.4f, 0.05f,  0.8f, 0.45f),
                new WaypointSpec(  1.2f, 0.05f, -0.4f, 0.45f),
                new WaypointSpec(  4.4f, 0.05f, -1.8f, 0.45f),
                new WaypointSpec(  5.3f, 0.05f, -3.9f, 0.45f),
                new WaypointSpec(  2.8f, 0.05f, -5.4f, 0.45f),
                new WaypointSpec( -0.8f, 0.05f, -4.9f, 0.40f),
                new WaypointSpec( -3.8f, 0.05f, -3.2f, 0.35f),
                new WaypointSpec( -6.6f, 0.05f, -1.9f, 0.30f),
                new WaypointSpec(-10.2f, 0.05f, -1.8f, 0.25f),
            };
            BuildPathObject(pathsRoot.transform, "Path_South", PathSouthAsset, southSpecs);

            Vector3[] slotPositions = new[]
            {
                new Vector3(-2.4f, 0.03f,  8.9f),
                new Vector3( 0.8f, 0.03f,  9.0f),
                new Vector3(-2.0f, 0.03f,  4.9f),
                new Vector3( 0.8f, 0.03f,  5.1f),
                new Vector3( 3.4f, 0.03f,  5.0f),
                new Vector3( 3.0f, 0.03f,  1.0f),
                new Vector3(-3.6f, 0.03f, -1.1f),
                new Vector3(-0.6f, 0.03f, -2.5f),
                new Vector3( 2.4f, 0.03f, -3.3f),
                new Vector3(-4.8f, 0.03f, -5.6f),
                new Vector3( 7.6f, 0.03f, -4.5f),
            };

            List<RoadPolyline> roads = new()
            {
                BuildRoadPolyline(northSpecs, 0.88f),
                BuildRoadPolyline(southSpecs, 0.86f)
            };

            Vector3 castleCenter = new Vector3(-10.8f, 0f, -1.8f);
            Vector3[] spawns = new[]
            {
                new Vector3(13.0f, 0f, 7.0f),
                new Vector3(13.0f, 0f, 2.1f)
            };

            TerrainLayer[] layers02 = new[]
            {
                LoadTerrainLayer("TL_DarkStone.terrainlayer"),
                LoadTerrainLayer("TL_CourtyardRoad.terrainlayer"),
                LoadTerrainLayer("TL_FortressRim.terrainlayer"),
                LoadTerrainLayer("TL_CobbleDetail.terrainlayer")
            };

            CobbleZone[] cobbleZones02 = new[]
            {
                new CobbleZone(castleCenter + new Vector3(1.6f, 0f, 0f), 2.5f),
                new CobbleZone(new Vector3(12.2f, 0f, 7.0f), 1.6f),
                new CobbleZone(new Vector3(12.2f, 0f, 2.1f), 1.6f),
                new CobbleZone(new Vector3(-1.4f, 0f, 0.8f), 1.85f)
            };

            Terrain terrain = BuildTerrain(
                terrainRoot.transform,
                "Terrain_Level_02",
                $"{TerrainDataDir}/TD_Level_02.asset",
                layers02,
                roads,
                slotPositions,
                castleCenter,
                spawns,
                groveCenters: null,
                cobbleZones: cobbleZones02,
                seed: 202);

            PlaceBuildSlots(slotsRoot.transform, slotPositions, terrain);
            PopulateLevel02Environment(envRoot.transform, terrain, roads, slotPositions, castleCenter, spawns);

            EditorSceneManager.SaveScene(newScene, Level02ScenePath);
            AddSceneToBuildSettings(Level02ScenePath);
            Debug.Log("[HollowKeepMapBuilder] Level_02.unity atualizada com Terrain normal-mapped e glow.");
        }

        [MenuItem("HollowKeep/Maps/Run Full Map Rebuild (All Steps)")]
        public static void BuildAll()
        {
            CreateTerrainAssets();
            ExtractLevelCoreAndNormalizePrefabs();
            BuildLevel01();
            CreateLevel02DataAssets();
            BuildLevel02();
            EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
        }

        private static void VerifyLevelCorePrefabIntegrity()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelCorePrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("LevelCore.prefab não foi salvo.");
            }

            WaveSpawner spawner = prefab.GetComponentInChildren<WaveSpawner>(true);
            PathRegistry registry = prefab.GetComponentInChildren<PathRegistry>(true);
            GameHUDController hud = prefab.GetComponentInChildren<GameHUDController>(true);
            BuildMenuUI buildMenu = prefab.GetComponentInChildren<BuildMenuUI>(true);

            if (spawner == null || registry == null || hud == null || buildMenu == null)
            {
                throw new InvalidOperationException("Componentes principais ausentes em LevelCore.prefab.");
            }

            SerializedObject soHud = new SerializedObject(hud);
            string[] requiredHudProps =
            {
                "waveSpawner", "waveText", "goldText", "livesText",
                "advanceWaveButton", "pauseButton", "pausePopup",
                "gameOverPopup", "gameOverRetryButton", "gameOverExitButton",
                "victoryPopup", "victoryNextLevelButton", "defaultDifficulty"
            };

            foreach (string propName in requiredHudProps)
            {
                SerializedProperty p = soHud.FindProperty(propName);
                if (p == null || p.objectReferenceValue == null)
                {
                    throw new InvalidOperationException($"Referência nula em LevelCore GameHUDController.{propName}");
                }
            }
        }

        private static void NormalizeBuildSlotAndGameplayStats()
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(BuildSlotPrefabPath))
            {
                GameObject root = scope.prefabContentsRoot;
                root.transform.localScale = Vector3.one;
                BoxCollider col = root.GetComponent<BoxCollider>();
                if (col != null)
                {
                    col.size = new Vector3(1.25f, 0.35f, 1.25f);
                    col.center = new Vector3(0f, 0.175f, 0f);
                }
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(DirectTowerPrefabPath))
            {
                Tower tower = scope.prefabContentsRoot.GetComponent<Tower>();
                SerializedObject so = new SerializedObject(tower);
                so.FindProperty("range").floatValue = 6.2f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(ParabolicTowerPrefabPath))
            {
                ParabolicTower tower = scope.prefabContentsRoot.GetComponent<ParabolicTower>();
                SerializedObject so = new SerializedObject(tower);
                so.FindProperty("range").floatValue = 4.5f;
                so.FindProperty("splashRadius").floatValue = 2.1f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            SetEnemySpeed("Assets/Data/Enemies/Stats_Esqueleto.asset", 3.1f);
            SetEnemySpeed("Assets/Data/Enemies/Stats_Corredor.asset", 4.9f);
            SetEnemySpeed("Assets/Data/Enemies/Stats_Cultista.asset", 2.7f);
            SetEnemySpeed("Assets/Data/Enemies/Stats_Brutamontes.asset", 1.85f);
            SetEnemySpeed("Assets/Data/Enemies/Stats_Campeao.asset", 1.55f);
        }

        private static void SetEnemySpeed(string assetPath, float moveSpeed)
        {
            EnemyStatsData stats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>(assetPath);
            if (stats == null) return;

            SerializedObject so = new SerializedObject(stats);
            so.FindProperty("moveSpeed").floatValue = moveSpeed;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stats);
        }

        private static void ConfigureLevelCoreInstance(GameObject levelCoreInst, LevelData levelData, string nextLevelSceneName)
        {
            DifficultyProfile normalDiff = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(DifficultyNormalPath);
            WaveSpawner spawner = levelCoreInst.GetComponentInChildren<WaveSpawner>(true);
            PathRegistry registry = levelCoreInst.GetComponentInChildren<PathRegistry>(true);
            GameHUDController hud = levelCoreInst.GetComponentInChildren<GameHUDController>(true);
            BuildMenuUI buildMenu = levelCoreInst.GetComponentInChildren<BuildMenuUI>(true);

            if (spawner != null)
            {
                SerializedObject so = new SerializedObject(spawner);
                so.FindProperty("levelData").objectReferenceValue = levelData;
                so.FindProperty("pathRegistry").objectReferenceValue = registry;
                so.FindProperty("defaultDifficulty").objectReferenceValue = normalDiff;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (hud != null)
            {
                SerializedObject so = new SerializedObject(hud);
                so.FindProperty("waveSpawner").objectReferenceValue = spawner;
                so.FindProperty("nextLevelSceneName").stringValue = nextLevelSceneName;
                so.FindProperty("defaultDifficulty").objectReferenceValue = normalDiff;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (buildMenu != null)
            {
                SerializedObject so = new SerializedObject(buildMenu);
                so.FindProperty("hudController").objectReferenceValue = hud;
                so.FindProperty("targetCamera").objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetupCameraAndBaseLighting(
            Transform parent,
            Vector3 camPos,
            Vector3 camEuler,
            Vector3 dirEuler,
            Color dirColor,
            float dirIntensity,
            Color fogColor,
            float fogDensity,
            Color ambientColor)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(parent, false);
            camGo.transform.position = camPos;
            camGo.transform.eulerAngles = camEuler;

            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.14f, 1f);
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;

            camGo.AddComponent<AudioListener>();
            UniversalAdditionalCameraData uac = camGo.AddComponent<UniversalAdditionalCameraData>();
            uac.renderPostProcessing = true;

            GameObject dirGo = new GameObject("Directional Light");
            dirGo.transform.SetParent(parent, false);
            dirGo.transform.position = new Vector3(0f, 12f, 0f);
            dirGo.transform.eulerAngles = dirEuler;
            Light dirLight = dirGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = dirColor;
            dirLight.intensity = dirIntensity;
            dirLight.shadows = LightShadows.Soft;

            GameObject volGo = new GameObject("Global Volume");
            volGo.transform.SetParent(parent, false);
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
        }

        private static void CreatePointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
        }

        private static void BuildPathObject(Transform parent, string pathName, string pathIdAssetPath, WaypointSpec[] specs)
        {
            GameObject pathGo = new GameObject(pathName);
            pathGo.transform.SetParent(parent, false);
            EnemyPath enemyPath = pathGo.AddComponent<EnemyPath>();

            Waypoint[] wpComponents = new Waypoint[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                GameObject wpGo = new GameObject($"WP_{(i + 1):00}");
                wpGo.transform.SetParent(pathGo.transform, false);
                wpGo.transform.position = specs[i].Position;

                Waypoint wp = wpGo.AddComponent<Waypoint>();
                SerializedObject soWp = new SerializedObject(wp);
                soWp.FindProperty("spreadRadius").floatValue = specs[i].SpreadRadius;
                soWp.ApplyModifiedPropertiesWithoutUndo();
                wpComponents[i] = wp;
            }

            PathIdentifier pathId = AssetDatabase.LoadAssetAtPath<PathIdentifier>(pathIdAssetPath);
            SerializedObject soPath = new SerializedObject(enemyPath);
            soPath.FindProperty("pathId").objectReferenceValue = pathId;
            SerializedProperty listProp = soPath.FindProperty("waypoints");
            listProp.ClearArray();
            for (int i = 0; i < wpComponents.Length; i++)
            {
                listProp.InsertArrayElementAtIndex(i);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = wpComponents[i];
            }
            soPath.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RoadPolyline BuildRoadPolyline(WaypointSpec[] specs, float widthScale)
        {
            const int samplesPerSeg = 16;
            int n = specs.Length;
            List<Vector3> pts = new();
            List<float> spreads = new();
            List<float> halfWidths = new();

            for (int i = 0; i < n - 1; i++)
            {
                Vector3 p0 = specs[Mathf.Max(0, i - 1)].Position;
                Vector3 p1 = specs[i].Position;
                Vector3 p2 = specs[i + 1].Position;
                Vector3 p3 = specs[Mathf.Min(n - 1, i + 2)].Position;

                float s1 = specs[i].SpreadRadius;
                float s2 = specs[i + 1].SpreadRadius;

                int steps = (i == n - 2) ? samplesPerSeg + 1 : samplesPerSeg;
                for (int k = 0; k < steps; k++)
                {
                    float t = k / (float)samplesPerSeg;
                    Vector3 pos = EvalCatmullRom(p0, p1, p2, p3, t);
                    float spread = Mathf.Lerp(s1, s2, t);
                    pts.Add(pos);
                    spreads.Add(spread);
                    // Estrada generosa e nítida comportando a dispersão com folga
                    halfWidths.Add(Mathf.Max(0.72f, spread * 0.78f + 0.38f) * widthScale);
                }
            }

            return new RoadPolyline(pts.ToArray(), spreads.ToArray(), halfWidths.ToArray());
        }

        private static Vector3 EvalCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private static Terrain BuildTerrain(
            Transform parent,
            string terrainName,
            string terrainDataPath,
            TerrainLayer[] layers,
            List<RoadPolyline> roads,
            Vector3[] slots,
            Vector3 castleCenter,
            Vector3[] spawns,
            Vector3[] groveCenters,
            CobbleZone[] cobbleZones,
            int seed)
        {
            const float width = 44f;
            const float maxHeight = 8f;
            const float length = 34f;
            Vector3 origin = new Vector3(-22f, 0f, -15f);

            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainDataPath);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, terrainDataPath);
            }

            data.heightmapResolution = 257;
            data.alphamapResolution = 1024;
            data.baseMapResolution = 512;
            data.size = new Vector3(width, maxHeight, length);
            data.terrainLayers = layers;

            int hRes = data.heightmapResolution;
            float[,] heights = new float[hRes, hRes];
            float offX = (seed * 17.3f) % 100f;
            float offZ = (seed * 31.7f) % 100f;

            for (int z = 0; z < hRes; z++)
            {
                float worldZ = origin.z + (z / (float)(hRes - 1)) * length;
                for (int x = 0; x < hRes; x++)
                {
                    float worldX = origin.x + (x / (float)(hRes - 1)) * width;
                    float hMeters = EvaluateOuterHillHeight(worldX, worldZ, roads, slots, castleCenter, spawns, offX, offZ);
                    heights[z, x] = Mathf.Clamp01(hMeters / maxHeight);
                }
            }
            data.SetHeights(0, 0, heights);

            int aRes = data.alphamapResolution;
            float[,,] alpha = new float[aRes, aRes, 4];
            for (int z = 0; z < aRes; z++)
            {
                float worldZ = origin.z + ((z + 0.5f) / aRes) * length;
                for (int x = 0; x < aRes; x++)
                {
                    float worldX = origin.x + ((x + 0.5f) / aRes) * width;
                    ComputeSplatWeights(
                        worldX, worldZ, origin, width, length, data,
                        roads, slots, groveCenters, cobbleZones, offX, offZ,
                        out float wBase, out float wRoad, out float wMossAO, out float wCobble);

                    alpha[z, x, 0] = wBase;
                    alpha[z, x, 1] = wRoad;
                    alpha[z, x, 2] = wMossAO;
                    alpha[z, x, 3] = wCobble;
                }
            }
            data.SetAlphamaps(0, 0, alpha);
            EditorUtility.SetDirty(data);

            GameObject terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = terrainName;
            terrainGo.transform.SetParent(parent, false);
            terrainGo.transform.position = origin;

            Terrain terrain = terrainGo.GetComponent<Terrain>();
            terrain.materialTemplate = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            terrain.drawInstanced = false;
            terrain.heightmapPixelError = 2f;

            return terrain;
        }

        private static float EvaluateOuterHillHeight(
            float wx,
            float wz,
            List<RoadPolyline> roads,
            Vector3[] slots,
            Vector3 castleCenter,
            Vector3[] spawns,
            float offX,
            float offZ)
        {
            float dRoad = DistanceToRoads(wx, wz, roads, out _);
            float dSlot = DistanceToPoints(wx, wz, slots);
            float dSpawn = DistanceToPoints(wx, wz, spawns);
            float dCastle = Vector2.Distance(new Vector2(wx, wz), new Vector2(castleCenter.x, castleCenter.z));

            float roadClear = Mathf.InverseLerp(2.3f, 4.9f, dRoad);
            float slotClear = Mathf.InverseLerp(2.1f, 4.4f, dSlot);
            float spawnClear = Mathf.InverseLerp(2.6f, 5.2f, dSpawn);
            float castleClear = Mathf.InverseLerp(4.0f, 6.8f, dCastle);

            float arenaX = Mathf.InverseLerp(11.4f, 15.6f, Mathf.Abs(wx - 0.3f));
            float arenaZ = wz > 1.5f
                ? Mathf.InverseLerp(9.2f, 12.6f, wz)
                : Mathf.InverseLerp(-6.2f, -9.4f, wz);
            float arenaBorder = Mathf.Max(arenaX, arenaZ);

            float mask = arenaBorder * roadClear * slotClear * spawnClear * castleClear;
            if (mask <= 0.001f) return 0f;

            float ridgeNoise = 0.65f + 0.85f * Mathf.PerlinNoise(wx * 0.09f + offX, wz * 0.09f + offZ);
            return mask * ridgeNoise * 2.2f;
        }

        private static void ComputeSplatWeights(
            float wx,
            float wz,
            Vector3 origin,
            float width,
            float length,
            TerrainData data,
            List<RoadPolyline> roads,
            Vector3[] slots,
            Vector3[] groveCenters,
            CobbleZone[] cobbleZones,
            float offX,
            float offZ,
            out float wBase,
            out float wRoad,
            out float wMossAO,
            out float wCobble)
        {
            float dRoad = DistanceToRoads(wx, wz, roads, out float halfWidth);
            float dSlot = DistanceToPoints(wx, wz, slots);

            // Borda orgânica de grama serrilhada estilo diorama ilustrado
            float macroWave = (Mathf.PerlinNoise(wx * 0.9f + offX, wz * 0.9f + offZ) - 0.5f) * 0.18f;
            float microTuft = (Mathf.PerlinNoise(wx * 2.8f + offX + 19f, wz * 2.8f + offZ + 7f) - 0.5f) * 0.09f;
            float roadSigned = dRoad + macroWave + microTuft - halfWidth;

            wRoad = 1f - SmoothStep01(-0.06f, 0.14f, roadSigned);

            // Pedestal circular de paralelepípedo sob cada BuildSlot + praças de castelo/spawn
            float slotCobble = 1f - SmoothStep01(0.56f, 0.74f, dSlot);
            float zoneCobble = 0f;
            if (cobbleZones != null)
            {
                Vector2 p = new Vector2(wx, wz);
                for (int i = 0; i < cobbleZones.Length; i++)
                {
                    float d = Vector2.Distance(p, new Vector2(cobbleZones[i].Center.x, cobbleZones[i].Center.z));
                    float zoneNoise = (Mathf.PerlinNoise(wx * 1.4f + offX, wz * 1.4f + offZ) - 0.5f) * 0.25f;
                    float w = 1f - SmoothStep01(cobbleZones[i].Radius * 0.72f, cobbleZones[i].Radius + 0.35f, d + zoneNoise);
                    if (w > zoneCobble) zoneCobble = w;
                }
            }

            wCobble = Mathf.Clamp01(Mathf.Max(slotCobble, zoneCobble * (1f - wRoad * 0.65f)));
            wRoad = Mathf.Clamp01(wRoad * (1f - wCobble * 0.88f));

            // Contorno escuro da estrada + colar dos slots + AO sob bosques e morros
            float roadShoulder = Mathf.Exp(-Mathf.Pow((roadSigned - 0.24f) / 0.22f, 2f)) * 0.72f * (1f - wRoad) * (1f - wCobble);
            float slotCollar = Mathf.Exp(-Mathf.Pow((dSlot - 0.80f) / 0.18f, 2f)) * 0.58f * (1f - wCobble);

            float normX = Mathf.Clamp01((wx - origin.x) / width);
            float normZ = Mathf.Clamp01((wz - origin.z) / length);
            float h = data.GetInterpolatedHeight(normX, normZ);
            float hillShade = SmoothStep01(0.10f, 1.15f, h) * 0.92f;

            float canopyAO = 0f;
            if (groveCenters != null && groveCenters.Length > 0)
            {
                float dGrove = DistanceToPoints(wx, wz, groveCenters);
                canopyAO = (1f - SmoothStep01(1.4f, 4.2f, dGrove)) * 0.62f * (1f - wRoad) * (1f - wCobble);
            }

            wMossAO = Mathf.Clamp01(Mathf.Max(Mathf.Max(roadShoulder, slotCollar), Mathf.Max(hillShade, canopyAO))) * (1f - wRoad) * (1f - wCobble);
            wBase = Mathf.Max(0f, 1f - wRoad - wMossAO - wCobble);

            float sum = wBase + wRoad + wMossAO + wCobble;
            if (sum > 0.0001f)
            {
                wBase /= sum;
                wRoad /= sum;
                wMossAO /= sum;
                wCobble /= sum;
            }
            else
            {
                wBase = 1f;
                wRoad = 0f;
                wMossAO = 0f;
                wCobble = 0f;
            }
        }

        private static float SmoothStep01(float edge0, float edge1, float x)
        {
            float t = Mathf.InverseLerp(edge0, edge1, x);
            return t * t * (3f - 2f * t);
        }

        private static float DistanceToRoads(float wx, float wz, List<RoadPolyline> roads, out float nearestHalfWidth)
        {
            float bestDist = float.MaxValue;
            nearestHalfWidth = 1.0f;
            Vector2 p = new Vector2(wx, wz);

            for (int r = 0; r < roads.Count; r++)
            {
                Vector3[] pts = roads[r].SmoothPoints;
                float[] hw = roads[r].BaseHalfWidths;
                for (int i = 0; i < pts.Length - 1; i++)
                {
                    Vector2 a = new Vector2(pts[i].x, pts[i].z);
                    Vector2 b = new Vector2(pts[i + 1].x, pts[i + 1].z);
                    Vector2 ab = b - a;
                    float lenSq = ab.sqrMagnitude;
                    float t = lenSq > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq) : 0f;
                    Vector2 proj = a + ab * t;
                    float d = Vector2.Distance(p, proj);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        nearestHalfWidth = Mathf.Lerp(hw[i], hw[i + 1], t);
                    }
                }
            }

            return bestDist;
        }

        private static float DistanceToPoints(float wx, float wz, Vector3[] points)
        {
            if (points == null || points.Length == 0) return float.MaxValue;
            float best = float.MaxValue;
            Vector2 p = new Vector2(wx, wz);
            for (int i = 0; i < points.Length; i++)
            {
                float d = Vector2.Distance(p, new Vector2(points[i].x, points[i].z));
                if (d < best) best = d;
            }
            return best;
        }

        private static void PlaceBuildSlots(Transform parent, Vector3[] slotPositions, Terrain terrain)
        {
            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildSlotPrefabPath);
            for (int i = 0; i < slotPositions.Length; i++)
            {
                Vector3 pos = slotPositions[i];
                pos.y = terrain.SampleHeight(pos) + 0.03f;
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab);
                inst.name = $"BuildSlot_{(i + 1):00}";
                inst.transform.SetParent(parent, false);
                inst.transform.position = pos;
                inst.transform.rotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;
            }
        }

        private static void PopulateLevel01Environment(
            Transform parent,
            Terrain terrain,
            List<RoadPolyline> roads,
            Vector3[] slots,
            Vector3 castleCenter,
            Vector3 spawnCenter)
        {
            Material matCastle = AssetDatabase.LoadAssetAtPath<Material>(CastleMaterialPath);
            Material matGraveyard = AssetDatabase.LoadAssetAtPath<Material>(GraveyardMaterialPath);
            Material matForest = AssetDatabase.LoadAssetAtPath<Material>(MiniForestMaterialPath);
            Material matSurvival = AssetDatabase.LoadAssetAtPath<Material>(SurvivalMaterialPath);
            Material matWarmGlow = AssetDatabase.LoadAssetAtPath<Material>(GlowWarmFlameMatPath);
            Material matAmberGlow = AssetDatabase.LoadAssetAtPath<Material>(GlowLanternAmberMatPath);
            Material matEerieGlow = AssetDatabase.LoadAssetAtPath<Material>(GlowEerieSoulMatPath);

            GameObject castleGroup = CreateChildGroup(parent, "Castle");
            GameObject spawnGroup = CreateChildGroup(parent, "Spawns");
            GameObject forestGroup = CreateChildGroup(parent, "ForestAndRocks");
            GameObject detailGroup = CreateChildGroup(parent, "Details");
            GameObject groundGroup = CreateChildGroup(parent, "GroundDressing");
            GameObject vfxGroup = CreateChildGroup(parent, "AmbientVFX");

            List<Vector3> occupiedDetails = new();
            void AddProp(Transform grp, string path, Material mat, Vector3 p, float yaw, float sc, bool registerClearance = true)
            {
                PlaceKenneyProp(grp, path, mat, terrain, p, yaw, sc);
                if (registerClearance) occupiedDetails.Add(p);
            }

            // A. Castelo Assombrado no canto inferior esquerdo (-8.8, 0, -4.5) voltado para +X
            BuildCastleGateAssembly(castleGroup.transform, terrain, matCastle, matGraveyard, matWarmGlow, matAmberGlow, castleCenter, 0f, 1.38f, addWallWings: true);
            AddProp(castleGroup.transform, SurvivalKitDir + "fence-fortified.fbx", matSurvival, castleCenter + new Vector3(1.1f, 0f, 3.1f), 90f, 2.1f);
            AddProp(castleGroup.transform, SurvivalKitDir + "fence-fortified.fbx", matSurvival, castleCenter + new Vector3(1.1f, 0f, -3.1f), 90f, 2.1f);
            AddProp(castleGroup.transform, SurvivalKitDir + "barrel.fbx", matSurvival, castleCenter + new Vector3(1.85f, 0f, 1.85f), 15f, 1.8f);
            AddProp(castleGroup.transform, SurvivalKitDir + "box.fbx", matSurvival, castleCenter + new Vector3(2.25f, 0f, 1.95f), 28f, 1.8f);
            CreateAmbientFire(vfxGroup.transform, "VFX_CastleFire_N", terrain, castleCenter + new Vector3(1.31f, 0.25f, 0.90f), new Color(1.0f, 0.55f, 0.14f, 0.85f), 0.28f);
            CreateAmbientFire(vfxGroup.transform, "VFX_CastleFire_S", terrain, castleCenter + new Vector3(1.31f, 0.25f, -0.90f), new Color(1.0f, 0.55f, 0.14f, 0.85f), 0.28f);

            // B. Origem do Spawn na direita + Placa indicadora + Cristais Espectrais
            AddProp(spawnGroup.transform, GraveyardKitDir + "iron-fence-border-gate.fbx", matGraveyard, spawnCenter, 90f, 1.38f);
            AddProp(spawnGroup.transform, GraveyardKitDir + "iron-fence-border.fbx", matGraveyard, new Vector3(12.2f, 0f, 5.45f), 90f, 1.3f);
            AddProp(spawnGroup.transform, GraveyardKitDir + "iron-fence-border.fbx", matGraveyard, new Vector3(12.2f, 0f, 2.95f), 90f, 1.3f);
            AddProp(spawnGroup.transform, GraveyardKitDir + "crypt-large.fbx", matGraveyard, new Vector3(13.4f, 0f, 6.2f), -130f, 1.15f);
            AddProp(spawnGroup.transform, GraveyardKitDir + "pillar-obelisk.fbx", matGraveyard, new Vector3(11.3f, 0f, 2.6f), 18f, 1.25f);
            AddProp(spawnGroup.transform, GraveyardKitDir + "lightpost-double.fbx", matGraveyard, new Vector3(11.2f, 0f, 5.75f), -35f, 1.3f);
            AddGlowBulb(spawnGroup.transform, terrain, new Vector3(11.2f, 1.35f, 5.75f), 0.11f, matEerieGlow);
            AddProp(spawnGroup.transform, TowerDefenseKitDir + "detail-crystal-large.fbx", matEerieGlow, new Vector3(11.5f, 0f, 2.95f), 25f, 1.15f);
            AddProp(spawnGroup.transform, TowerDefenseKitDir + "detail-crystal.fbx", matEerieGlow, new Vector3(10.8f, 0f, 5.4f), -18f, 1.25f);
            AddGlowingPumpkin(spawnGroup.transform, terrain, matGraveyard, matWarmGlow, GraveyardKitDir + "pumpkin-carved.fbx", new Vector3(11.1f, 0f, 3.1f), -35f, 1.2f);
            AddProp(spawnGroup.transform, SurvivalKitDir + "signpost.fbx", matSurvival, new Vector3(9.6f, 0f, 5.1f), -28f, 2.3f);
            CreateAmbientFire(vfxGroup.transform, "VFX_SpawnWisps", terrain, new Vector3(11.8f, 0.35f, 4.3f), new Color(0.22f, 0.92f, 0.86f, 0.72f), 0.45f, 1.8f);

            // C. Acampamento / Lenha / Plantação Assombrada no Centro-Esquerdo (ecoando as cabanas, toras e horta da referência)
            Vector3 campFirePos = new Vector3(-9.5f, 0f, 2.2f);
            AddProp(detailGroup.transform, SurvivalKitDir + "campfire-pit.fbx", matSurvival, campFirePos, 15f, 2.6f);
            AddGlowBulb(detailGroup.transform, terrain, new Vector3(campFirePos.x, 0.16f, campFirePos.z), 0.20f, matWarmGlow);
            CreateAmbientFire(vfxGroup.transform, "VFX_CampfireEmbers", terrain, campFirePos + new Vector3(0f, 0.22f, 0f), new Color(1.0f, 0.52f, 0.14f, 0.90f), 0.32f);

            // Tendas sombrias de vigia & bancada de lenhador
            AddProp(detailGroup.transform, SurvivalKitDir + "tent.fbx", matSurvival, new Vector3(-9.7f, 0f, 3.9f), 25f, 2.25f);
            AddProp(detailGroup.transform, SurvivalKitDir + "workbench.fbx", matSurvival, new Vector3(-9.2f, 0f, 3.3f), 40f, 2.1f);
            AddProp(detailGroup.transform, SurvivalKitDir + "tent-canvas.fbx", matSurvival, new Vector3(-11.2f, 0f, 2.8f), 45f, 2.25f);
            AddProp(detailGroup.transform, SurvivalKitDir + "barrel.fbx", matSurvival, new Vector3(-8.6f, 0f, 3.2f), 10f, 1.8f);
            AddProp(detailGroup.transform, SurvivalKitDir + "box.fbx", matSurvival, new Vector3(-8.4f, 0f, 3.7f), 25f, 1.75f);

            // Pilha de toras e tocos
            AddProp(detailGroup.transform, SurvivalKitDir + "tree-log.fbx", matSurvival, new Vector3(-11.4f, 0f, 4.6f), 68f, 1.65f);
            AddProp(detailGroup.transform, SurvivalKitDir + "tree-log.fbx", matSurvival, new Vector3(-11.2f, 0f, 5.05f), 74f, 1.65f);
            AddProp(detailGroup.transform, SurvivalKitDir + "resource-wood.fbx", matSurvival, new Vector3(-10.1f, 0f, 5.25f), 20f, 2.4f);
            AddProp(detailGroup.transform, SurvivalKitDir + "tree-trunk.fbx", matSurvival, new Vector3(-8.8f, 0f, 5.3f), 12f, 2.0f);

            // Cercado + Horta de Abóboras & Fardos de Feno
            AddProp(detailGroup.transform, SurvivalKitDir + "fence.fbx", matSurvival, new Vector3(-10.8f, 0f, 0.7f), 0f, 2.0f);
            AddProp(detailGroup.transform, SurvivalKitDir + "fence.fbx", matSurvival, new Vector3(-9.7f, 0f, 0.7f), 0f, 2.0f);
            AddProp(detailGroup.transform, SurvivalKitDir + "fence.fbx", matSurvival, new Vector3(-11.4f, 0f, -0.2f), 90f, 2.0f);
            AddProp(detailGroup.transform, GraveyardKitDir + "hay-bale-bundled.fbx", matGraveyard, new Vector3(-8.3f, 0f, -0.25f), 22f, 1.35f);
            AddProp(detailGroup.transform, GraveyardKitDir + "hay-bale.fbx", matGraveyard, new Vector3(-8.9f, 0f, -0.65f), -18f, 1.3f);
            AddGlowingPumpkin(detailGroup.transform, terrain, matGraveyard, matWarmGlow, GraveyardKitDir + "pumpkin-carved.fbx", new Vector3(-10.4f, 0f, 0.1f), 15f, 1.25f);
            AddProp(detailGroup.transform, GraveyardKitDir + "pumpkin.fbx", matGraveyard, new Vector3(-9.7f, 0f, 0.15f), -30f, 1.15f);
            AddGlowingPumpkin(detailGroup.transform, terrain, matGraveyard, matWarmGlow, GraveyardKitDir + "pumpkin-tall-carved.fbx", new Vector3(-10.5f, 0f, -0.75f), 40f, 1.3f);
            AddProp(detailGroup.transform, GraveyardKitDir + "pumpkin.fbx", matGraveyard, new Vector3(-9.7f, 0f, -0.65f), 12f, 1.15f);

            // Cripta / Cemitério Superior Esquerdo
            AddProp(detailGroup.transform, GraveyardKitDir + "crypt-small.fbx", matGraveyard, new Vector3(-6.2f, 0f, 6.2f), 28f, 1.15f);
            AddProp(detailGroup.transform, GraveyardKitDir + "gravestone-cross-large.fbx", matGraveyard, new Vector3(-5.2f, 0f, 6.6f), 12f, 1.15f);
            AddProp(detailGroup.transform, GraveyardKitDir + "gravestone-round.fbx", matGraveyard, new Vector3(-7.4f, 0f, 5.8f), -22f, 1.15f);
            AddProp(detailGroup.transform, GraveyardKitDir + "lightpost-single.fbx", matGraveyard, new Vector3(-4.5f, 0f, 7.6f), 42f, 1.25f);
            AddGlowBulb(detailGroup.transform, terrain, new Vector3(-4.42f, 1.28f, 7.68f), 0.10f, matAmberGlow);
            AddGlowingPumpkin(detailGroup.transform, terrain, matGraveyard, matWarmGlow, GraveyardKitDir + "pumpkin-carved.fbx", new Vector3(-4.15f, 0f, 7.35f), 20f, 1.2f);

            // D. Santuário / Ruína e Fenos no Canto Inferior Direito (no lugar dos moinhos alegres da referência)
            AddProp(detailGroup.transform, GraveyardKitDir + "crypt-a.fbx", matGraveyard, new Vector3(8.1f, 0f, -4.3f), -38f, 1.25f);
            AddProp(detailGroup.transform, GraveyardKitDir + "pillar-obelisk.fbx", matGraveyard, new Vector3(6.6f, 0f, -4.7f), 15f, 1.2f);
            AddProp(detailGroup.transform, GraveyardKitDir + "lightpost-single.fbx", matGraveyard, new Vector3(7.4f, 0f, -4.3f), -120f, 1.3f);
            AddGlowBulb(detailGroup.transform, terrain, new Vector3(7.35f, 1.32f, -4.25f), 0.10f, matAmberGlow);
            AddProp(detailGroup.transform, GraveyardKitDir + "hay-bale-bundled.fbx", matGraveyard, new Vector3(5.6f, 0f, -4.8f), 18f, 1.3f);
            AddProp(detailGroup.transform, GraveyardKitDir + "hay-bale.fbx", matGraveyard, new Vector3(6.1f, 0f, -5.2f), -22f, 1.25f);
            AddProp(detailGroup.transform, GraveyardKitDir + "bench-damaged.fbx", matGraveyard, new Vector3(9.1f, 0f, -3.5f), -65f, 1.15f);
            AddProp(detailGroup.transform, GraveyardKitDir + "coffin-old.fbx", matGraveyard, new Vector3(7.2f, 0f, -5.3f), 45f, 1.15f);
            AddProp(detailGroup.transform, SurvivalKitDir + "fence.fbx", matSurvival, new Vector3(8.2f, 0f, -5.6f), 12f, 1.95f);
            AddProp(detailGroup.transform, SurvivalKitDir + "fence.fbx", matSurvival, new Vector3(9.2f, 0f, -5.3f), 22f, 1.95f);
            AddGlowingPumpkin(detailGroup.transform, terrain, matGraveyard, matWarmGlow, GraveyardKitDir + "pumpkin-carved.fbx", new Vector3(6.7f, 0f, -4.25f), 32f, 1.2f);

            // E. Grupo Icônico de 3 Árvores na Curva Central-Direita (exatamente como a referência!)
            Vector3[] pocketCluster = new[]
            {
                new Vector3(3.6f, 0f, 1.85f),
                new Vector3(4.35f, 0f, 1.35f),
                new Vector3(3.25f, 0f, 1.05f),
            };
            for (int i = 0; i < pocketCluster.Length; i++)
            {
                Vector3 tp = pocketCluster[i];
                if (DistanceToRoads(tp.x, tp.z, roads, out _) >= 1.55f && DistanceToPoints(tp.x, tp.z, slots) >= 1.75f)
                {
                    string model = i == 1 ? SurvivalKitDir + "tree-autumn-tall.fbx" : SurvivalKitDir + "tree.fbx";
                    float scale = i == 1 ? 1.85f : 1.75f;
                    AddProp(forestGroup.transform, model, matSurvival, tp, i * 55f, scale);
                }
            }

            // F. Bosques Densos NW, NE, Península Meio-Direita e Margem Sul
            ScatterLushForestBorder(
                forestGroup.transform, terrain, matForest, matGraveyard, matSurvival,
                roads, slots, castleCenter, new[] { spawnCenter }, occupiedDetails.ToArray(), seed: 101);

            // G. Micro-detalhes de chão (tufos de grama, flores/plantas e pedrinhas ao redor de estradas e slots)
            ScatterGroundDressing(
                groundGroup.transform, terrain, matForest, matSurvival,
                roads, slots, castleCenter, new[] { spawnCenter }, occupiedDetails.ToArray(), seed: 301);

            // H. Vagalumes suaves sobre o bosque e campina
            CreateForestFireflies(vfxGroup.transform);
        }

        private static void PopulateLevel02Environment(
            Transform parent,
            Terrain terrain,
            List<RoadPolyline> roads,
            Vector3[] slots,
            Vector3 castleCenter,
            Vector3[] spawns)
        {
            Material matCastle = AssetDatabase.LoadAssetAtPath<Material>(CastleMaterialPath);
            Material matGraveyard = AssetDatabase.LoadAssetAtPath<Material>(GraveyardMaterialPath);
            Material matDungeon = AssetDatabase.LoadAssetAtPath<Material>(MiniDungeonMaterialPath);
            Material matForest = AssetDatabase.LoadAssetAtPath<Material>(MiniForestMaterialPath);
            Material matSurvival = AssetDatabase.LoadAssetAtPath<Material>(SurvivalMaterialPath);
            Material matWarmGlow = AssetDatabase.LoadAssetAtPath<Material>(GlowWarmFlameMatPath);
            Material matAmberGlow = AssetDatabase.LoadAssetAtPath<Material>(GlowLanternAmberMatPath);
            Material matEerieGlow = AssetDatabase.LoadAssetAtPath<Material>(GlowEerieSoulMatPath);

            GameObject castleGroup = CreateChildGroup(parent, "Castle");
            GameObject spawnGroup = CreateChildGroup(parent, "Spawns");
            GameObject forestGroup = CreateChildGroup(parent, "ForestAndRocks");
            GameObject detailGroup = CreateChildGroup(parent, "Details");
            GameObject groundGroup = CreateChildGroup(parent, "GroundDressing");
            GameObject vfxGroup = CreateChildGroup(parent, "AmbientVFX");

            List<Vector3> occupiedDetails = new();
            void AddDetailProp(Transform grp, string path, Material mat, Vector3 p, float yaw, float sc)
            {
                PlaceKenneyProp(grp, path, mat, terrain, p, yaw, sc);
                occupiedDetails.Add(p);
            }

            // Fortaleza Principal à esquerda (-10.8, 0, -1.8) voltada para +X
            BuildCastleGateAssembly(castleGroup.transform, terrain, matCastle, matGraveyard, matWarmGlow, matAmberGlow, castleCenter, 0f, 1.40f, addWallWings: true);
            AddDetailProp(castleGroup.transform, CastleKitDir + "wall.fbx", matCastle, castleCenter + new Vector3(0f, 0f, 2.8f), 0f, 1.40f);
            AddDetailProp(castleGroup.transform, CastleKitDir + "wall.fbx", matCastle, castleCenter + new Vector3(0f, 0f, -2.8f), 0f, 1.40f);
            CreateAmbientFire(vfxGroup.transform, "VFX_CastleFire_N", terrain, castleCenter + new Vector3(1.33f, 0.25f, 0.91f), new Color(1.0f, 0.55f, 0.14f, 0.85f), 0.28f);
            CreateAmbientFire(vfxGroup.transform, "VFX_CastleFire_S", terrain, castleCenter + new Vector3(1.33f, 0.25f, -0.91f), new Color(1.0f, 0.55f, 0.14f, 0.85f), 0.28f);

            // Duas brechas de entrada na borda direita com brilho espectral
            foreach (Vector3 sp in spawns)
            {
                AddDetailProp(spawnGroup.transform, CastleKitDir + "wall-doorway.fbx", matCastle, new Vector3(12.2f, 0f, sp.z), 0f, 1.35f);
                Vector3 northPillar = new Vector3(12.2f, 0f, sp.z + 1.55f);
                Vector3 southPillar = new Vector3(12.2f, 0f, sp.z - 1.55f);
                AddDetailProp(spawnGroup.transform, CastleKitDir + "tower-square-base.fbx", matCastle, northPillar, 0f, 1.2f);
                AddDetailProp(spawnGroup.transform, CastleKitDir + "tower-square-base.fbx", matCastle, southPillar, 0f, 1.2f);
                PlaceKenneyProp(spawnGroup.transform, CastleKitDir + "tower-square-top.fbx", matCastle, terrain, northPillar + new Vector3(0f, terrain.SampleHeight(northPillar) + 1.2f, 0f), 0f, 1.2f, snapToTerrain: false);
                PlaceKenneyProp(spawnGroup.transform, CastleKitDir + "tower-square-top.fbx", matCastle, terrain, southPillar + new Vector3(0f, terrain.SampleHeight(southPillar) + 1.2f, 0f), 0f, 1.2f, snapToTerrain: false);
                AddDetailProp(spawnGroup.transform, TowerDefenseKitDir + "detail-crystal.fbx", matEerieGlow, new Vector3(11.3f, 0f, sp.z + 1.15f), 15f, 1.15f);
            }

            // Escombros de cerco, fogueiras e área cercada de feno entre os dois braços
            AddDetailProp(detailGroup.transform, CastleKitDir + "siege-catapult-demolished.fbx", matCastle, new Vector3(9.2f, 0f, -1.6f), -35f, 1.0f);
            AddDetailProp(detailGroup.transform, CastleKitDir + "siege-ram-demolished.fbx", matCastle, new Vector3(-8.4f, 0f, 6.2f), 30f, 0.9f);
            AddDetailProp(detailGroup.transform, GraveyardKitDir + "fire-basket.fbx", matGraveyard, new Vector3(-5.8f, 0f, 1.4f), 0f, 1.3f);
            AddGlowBulb(detailGroup.transform, terrain, new Vector3(-5.8f, 0.22f, 1.4f), 0.14f, matWarmGlow);
            CreateAmbientFire(vfxGroup.transform, "VFX_JunctionFire", terrain, new Vector3(-5.8f, 0.26f, 1.4f), new Color(1.0f, 0.55f, 0.14f, 0.85f), 0.24f);
            AddDetailProp(detailGroup.transform, GraveyardKitDir + "fire-basket.fbx", matGraveyard, new Vector3(5.6f, 0f, 0.4f), 0f, 1.25f);
            AddGlowBulb(detailGroup.transform, terrain, new Vector3(5.6f, 0.22f, 0.4f), 0.13f, matWarmGlow);
            AddDetailProp(detailGroup.transform, GraveyardKitDir + "pillar-obelisk.fbx", matGraveyard, new Vector3(6.4f, 0f, 4.8f), 0f, 1.25f);
            AddDetailProp(detailGroup.transform, MiniDungeonKitDir + "barrel.fbx", matDungeon, new Vector3(6.0f, 0f, 4.4f), 25f, 1.2f);
            AddDetailProp(detailGroup.transform, GraveyardKitDir + "hay-bale-bundled.fbx", matGraveyard, new Vector3(7.4f, 0f, 4.7f), 18f, 1.3f);
            AddDetailProp(detailGroup.transform, GraveyardKitDir + "hay-bale.fbx", matGraveyard, new Vector3(8.4f, 0f, 4.5f), -20f, 1.25f);
            AddDetailProp(detailGroup.transform, SurvivalKitDir + "fence.fbx", matSurvival, new Vector3(7.6f, 0f, 5.3f), 10f, 1.9f);
            AddDetailProp(detailGroup.transform, SurvivalKitDir + "fence.fbx", matSurvival, new Vector3(8.8f, 0f, 5.1f), 20f, 1.9f);
            AddDetailProp(detailGroup.transform, MiniDungeonKitDir + "column.fbx", matDungeon, new Vector3(-7.4f, 0f, -5.6f), 0f, 1.2f);
            AddGlowingPumpkin(detailGroup.transform, terrain, matGraveyard, matWarmGlow, GraveyardKitDir + "pumpkin-carved.fbx", new Vector3(-8.2f, 0f, -5.1f), 30f, 1.2f);

            for (float x = -7.8f; x <= 9.1f; x += 1.3f)
            {
                Vector3 wallPos = new Vector3(x, 0f, 10.8f);
                if (DistanceToRoads(wallPos.x, wallPos.z, roads, out _) < 2.2f) continue;
                AddDetailProp(castleGroup.transform, CastleKitDir + "wall.fbx", matCastle, wallPos, 90f, 1.3f);
            }
            AddDetailProp(castleGroup.transform, CastleKitDir + "tower-square-base.fbx", matCastle, new Vector3(-8.8f, 0f, 10.8f), 0f, 1.35f);
            PlaceKenneyProp(castleGroup.transform, CastleKitDir + "tower-square-top.fbx", matCastle, terrain, new Vector3(-8.8f, terrain.SampleHeight(new Vector3(-8.8f, 0f, 10.8f)) + 1.35f, 10.8f), 0f, 1.35f, snapToTerrain: false);
            AddDetailProp(castleGroup.transform, CastleKitDir + "tower-square-base.fbx", matCastle, new Vector3(10.1f, 0f, 10.8f), 0f, 1.35f);
            PlaceKenneyProp(castleGroup.transform, CastleKitDir + "tower-square-top.fbx", matCastle, terrain, new Vector3(10.1f, terrain.SampleHeight(new Vector3(10.1f, 0f, 10.8f)) + 1.35f, 10.8f), 0f, 1.35f, snapToTerrain: false);

            ScatterLushForestBorder(
                forestGroup.transform, terrain, matForest, matGraveyard, matSurvival,
                roads, slots, castleCenter, spawns, occupiedDetails.ToArray(), seed: 202, fortressSparseMode: true);
            ScatterGroundDressing(
                groundGroup.transform, terrain, matForest, matSurvival,
                roads, slots, castleCenter, spawns, occupiedDetails.ToArray(), seed: 402);
        }

        private static void BuildCastleGateAssembly(
            Transform parent,
            Terrain terrain,
            Material matCastle,
            Material matGraveyard,
            Material matWarmGlow,
            Material matAmberGlow,
            Vector3 center,
            float yawDeg,
            float scale,
            bool addWallWings)
        {
            PlaceKenneyProp(parent, CastleKitDir + "wall-doorway.fbx", matCastle, terrain, center, yawDeg, scale);
            PlaceKenneyProp(parent, CastleKitDir + "metal-gate.fbx", matCastle, terrain, center + new Vector3(0.25f * scale, 0f, 0f), yawDeg, scale);

            float towerZ = 1.05f * scale;
            Vector3 leftTower = center + new Vector3(0f, 0f, towerZ);
            Vector3 rightTower = center + new Vector3(0f, 0f, -towerZ);

            PlaceStackedTower(parent, terrain, matCastle, leftTower, yawDeg, scale);
            PlaceStackedTower(parent, terrain, matCastle, rightTower, yawDeg, scale);

            if (addWallWings)
            {
                PlaceKenneyProp(parent, CastleKitDir + "wall.fbx", matCastle, terrain, center + new Vector3(0f, 0f, 2.0f * scale), yawDeg, scale);
                PlaceKenneyProp(parent, CastleKitDir + "wall.fbx", matCastle, terrain, center + new Vector3(0f, 0f, -2.0f * scale), yawDeg, scale);
            }

            PlaceKenneyProp(parent, CastleKitDir + "flag-banner-long.fbx", matCastle, terrain, center + new Vector3(0.45f * scale, 1.15f * scale, 0.72f * scale), yawDeg, scale * 0.9f, snapToTerrain: false);
            PlaceKenneyProp(parent, CastleKitDir + "flag-banner-long.fbx", matCastle, terrain, center + new Vector3(0.45f * scale, 1.15f * scale, -0.72f * scale), yawDeg, scale * 0.9f, snapToTerrain: false);

            // Braseiros acesos + lanternas do portão com emissão HDR
            Vector3 brazierN = center + new Vector3(0.95f * scale, 0f, 0.65f * scale);
            Vector3 brazierS = center + new Vector3(0.95f * scale, 0f, -0.65f * scale);
            PlaceKenneyProp(parent, GraveyardKitDir + "fire-basket.fbx", matGraveyard, terrain, brazierN, 0f, 1.25f);
            PlaceKenneyProp(parent, GraveyardKitDir + "fire-basket.fbx", matGraveyard, terrain, brazierS, 0f, 1.25f);
            AddGlowBulb(parent, terrain, brazierN + new Vector3(0f, 0.16f, 0f), 0.15f, matWarmGlow);
            AddGlowBulb(parent, terrain, brazierS + new Vector3(0f, 0.16f, 0f), 0.15f, matWarmGlow);
            AddGlowBulb(parent, terrain, center + new Vector3(0.56f * scale, 1.12f * scale, 0.42f * scale), 0.10f, matAmberGlow, snapToTerrain: false);
            AddGlowBulb(parent, terrain, center + new Vector3(0.56f * scale, 1.12f * scale, -0.42f * scale), 0.10f, matAmberGlow, snapToTerrain: false);

            AddGlowingPumpkin(parent, terrain, matGraveyard, matWarmGlow, GraveyardKitDir + "pumpkin-carved.fbx", center + new Vector3(1.1f * scale, 0f, 1.0f * scale), 35f, 1.15f);
        }

        private static void PlaceStackedTower(Transform parent, Terrain terrain, Material matCastle, Vector3 pos, float yawDeg, float scale)
        {
            float baseH = terrain.SampleHeight(pos);
            PlaceKenneyProp(parent, CastleKitDir + "tower-square-base.fbx", matCastle, terrain, new Vector3(pos.x, baseH, pos.z), yawDeg, scale, snapToTerrain: false);
            PlaceKenneyProp(parent, CastleKitDir + "tower-square-mid-windows.fbx", matCastle, terrain, new Vector3(pos.x, baseH + 1.0f * scale, pos.z), yawDeg, scale, snapToTerrain: false);
            PlaceKenneyProp(parent, CastleKitDir + "tower-square-top-roof-high.fbx", matCastle, terrain, new Vector3(pos.x, baseH + 2.0f * scale, pos.z), yawDeg, scale, snapToTerrain: false);
        }

        private static void PlaceStackedHut(Transform parent, Terrain terrain, Material matSurvival, Vector3 pos, float yawDeg, float scale)
        {
            float baseH = terrain.SampleHeight(pos);
            PlaceKenneyProp(parent, SurvivalKitDir + "structure.fbx", matSurvival, terrain, new Vector3(pos.x, baseH, pos.z), yawDeg, scale, snapToTerrain: false);
            PlaceKenneyProp(parent, SurvivalKitDir + "structure-roof.fbx", matSurvival, terrain, new Vector3(pos.x, baseH + 0.50f * scale, pos.z), yawDeg, scale, snapToTerrain: false);
        }

        private static void AddGlowingPumpkin(
            Transform parent,
            Terrain terrain,
            Material matGraveyard,
            Material matWarmGlow,
            string fbxPath,
            Vector3 pos,
            float yawDeg,
            float scale)
        {
            PlaceKenneyProp(parent, fbxPath, matGraveyard, terrain, pos, yawDeg, scale);
            AddGlowBulb(parent, terrain, pos + new Vector3(0f, 0.15f * scale, 0f), 0.10f * scale, matWarmGlow);
        }

        private static void AddGlowBulb(
            Transform parent,
            Terrain terrain,
            Vector3 pos,
            float radius,
            Material glowMat,
            bool snapToTerrain = true)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "GlowCore";
            sphere.transform.SetParent(parent, false);
            float baseY = (snapToTerrain && terrain != null) ? terrain.SampleHeight(pos) : 0f;
            sphere.transform.position = new Vector3(pos.x, baseY + pos.y, pos.z);
            sphere.transform.localScale = Vector3.one * (radius * 2f);

            Collider col = sphere.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            MeshRenderer mr = sphere.GetComponent<MeshRenderer>();
            if (mr != null && glowMat != null)
            {
                mr.sharedMaterial = glowMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static void ScatterLushForestBorder(
            Transform parent,
            Terrain terrain,
            Material matForest,
            Material matGraveyard,
            Material matSurvival,
            List<RoadPolyline> roads,
            Vector3[] slots,
            Vector3 castleCenter,
            Vector3[] spawns,
            Vector3[] occupiedDetails,
            int seed,
            bool fortressSparseMode = false)
        {
            System.Random rng = new(seed);
            string[] pinePrefabs =
            {
                GraveyardKitDir + "pine.fbx",
                GraveyardKitDir + "pine-crooked.fbx",
                GraveyardKitDir + "pine-fall.fbx"
            };
            string[] forestPrefabs =
            {
                MiniForestKitDir + "tree.fbx",
                MiniForestKitDir + "tree-high.fbx"
            };
            string[] survivalGreenPrefabs =
            {
                SurvivalKitDir + "tree.fbx",
                SurvivalKitDir + "tree-tall.fbx"
            };
            string[] survivalAutumnPrefabs =
            {
                SurvivalKitDir + "tree-autumn.fbx",
                SurvivalKitDir + "tree-autumn-tall.fbx"
            };
            string[] rockPrefabs =
            {
                MiniForestKitDir + "rocks-low.fbx",
                MiniForestKitDir + "stones.fbx"
            };

            float step = fortressSparseMode ? 1.65f : 1.18f;
            for (float gx = -16.5f; gx <= 17.5f; gx += step)
            {
                for (float gz = -9.6f; gz <= 13.0f; gz += step)
                {
                    float wx = gx + (float)(rng.NextDouble() - 0.5) * 0.65f;
                    float wz = gz + (float)(rng.NextDouble() - 0.5) * 0.65f;

                    // Em Level_01, além das bordas, cria a península densa meio-direita abaixo da entrada!
                    bool inMidRightGrove = !fortressSparseMode && (wx > 6.2f && wz > -1.3f && wz < 2.6f);
                    bool inOuterFrame = Mathf.Abs(wx) > 10.6f || wz > 8.9f || wz < -6.1f;
                    if (!inOuterFrame && !inMidRightGrove) continue;

                    float dRoad = DistanceToRoads(wx, wz, roads, out _);
                    float dSlot = DistanceToPoints(wx, wz, slots);
                    float dSpawn = DistanceToPoints(wx, wz, spawns);
                    float dDetails = DistanceToPoints(wx, wz, occupiedDetails);
                    float dCastle = Vector2.Distance(new Vector2(wx, wz), new Vector2(castleCenter.x, castleCenter.z));

                    if (dRoad < 1.85f || dSlot < 1.95f || dSpawn < 2.45f || dDetails < 1.85f || dCastle < 3.9f) continue;

                    double roll = rng.NextDouble();
                    float yaw = (float)(rng.NextDouble() * 360.0);
                    if (roll < 0.34)
                    {
                        string path = pinePrefabs[rng.Next(pinePrefabs.Length)];
                        float s = 1.0f + (float)rng.NextDouble() * 0.45f;
                        PlaceKenneyProp(parent, path, matGraveyard, terrain, new Vector3(wx, 0f, wz), yaw, s);
                    }
                    else if (roll < 0.60)
                    {
                        string path = forestPrefabs[rng.Next(forestPrefabs.Length)];
                        float s = 1.05f + (float)rng.NextDouble() * 0.45f;
                        PlaceKenneyProp(parent, path, matForest, terrain, new Vector3(wx, 0f, wz), yaw, s);
                    }
                    else if (roll < 0.78)
                    {
                        string path = survivalGreenPrefabs[rng.Next(survivalGreenPrefabs.Length)];
                        float s = 1.75f + (float)rng.NextDouble() * 0.55f;
                        PlaceKenneyProp(parent, path, matSurvival, terrain, new Vector3(wx, 0f, wz), yaw, s);
                    }
                    else if (roll < 0.90)
                    {
                        string path = survivalAutumnPrefabs[rng.Next(survivalAutumnPrefabs.Length)];
                        float s = 1.75f + (float)rng.NextDouble() * 0.50f;
                        PlaceKenneyProp(parent, path, matSurvival, terrain, new Vector3(wx, 0f, wz), yaw, s);
                    }
                    else
                    {
                        string path = rockPrefabs[rng.Next(rockPrefabs.Length)];
                        float s = 0.85f + (float)rng.NextDouble() * 0.45f;
                        PlaceKenneyProp(parent, path, matForest, terrain, new Vector3(wx, 0f, wz), yaw, s);
                    }
                }
            }
        }

        private static void ScatterGroundDressing(
            Transform parent,
            Terrain terrain,
            Material matForest,
            Material matSurvival,
            List<RoadPolyline> roads,
            Vector3[] slots,
            Vector3 castleCenter,
            Vector3[] spawns,
            Vector3[] occupiedDetails,
            int seed)
        {
            System.Random rng = new(seed);
            for (int i = 0; i < 140; i++)
            {
                float wx = Mathf.Lerp(-12.5f, 13.5f, (float)rng.NextDouble());
                float wz = Mathf.Lerp(-7.2f, 10.2f, (float)rng.NextDouble());

                float dRoad = DistanceToRoads(wx, wz, roads, out float hw);
                float dSlot = DistanceToPoints(wx, wz, slots);
                float dSpawn = DistanceToPoints(wx, wz, spawns);
                float dDetails = DistanceToPoints(wx, wz, occupiedDetails);
                float dCastle = Vector2.Distance(new Vector2(wx, wz), new Vector2(castleCenter.x, castleCenter.z));

                // Nunca coloca tufos/pedras em cima da estrada nem bloqueando slots/detalhes
                if (dRoad < hw + 0.30f || dSlot < 0.92f || dSpawn < 1.5f || dDetails < 0.9f || dCastle < 2.6f) continue;

                // Prioriza beirada da estrada ou contorno externo dos slots
                bool nearInterest = (dRoad < hw + 1.35f) || (dSlot < 1.65f);
                if (!nearInterest && rng.NextDouble() > 0.35) continue;

                float yaw = (float)(rng.NextDouble() * 360.0);
                double roll = rng.NextDouble();
                if (roll < 0.45)
                {
                    PlaceKenneyProp(parent, SurvivalKitDir + "grass.fbx", matSurvival, terrain, new Vector3(wx, 0f, wz), yaw, 1.45f + (float)rng.NextDouble() * 0.4f);
                }
                else if (roll < 0.70)
                {
                    PlaceKenneyProp(parent, SurvivalKitDir + "grass-large.fbx", matSurvival, terrain, new Vector3(wx, 0f, wz), yaw, 1.2f + (float)rng.NextDouble() * 0.3f);
                }
                else if (roll < 0.85)
                {
                    PlaceKenneyProp(parent, MiniForestKitDir + "plant.fbx", matForest, terrain, new Vector3(wx, 0f, wz), yaw, 0.9f + (float)rng.NextDouble() * 0.35f);
                }
                else
                {
                    PlaceKenneyProp(parent, SurvivalKitDir + "rock-a.fbx", matSurvival, terrain, new Vector3(wx, 0f, wz), yaw, 0.65f + (float)rng.NextDouble() * 0.35f);
                }
            }
        }

        private static void CreateAmbientFire(
            Transform parent,
            string name,
            Terrain terrain,
            Vector3 pos,
            Color color,
            float radius,
            float heightGain = 1.1f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            float baseH = terrain != null ? terrain.SampleHeight(pos) : 0f;
            go.transform.position = new Vector3(pos.x, baseH + pos.y, pos.z);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 2.0f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f * heightGain, 0.95f * heightGain);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
            main.startColor = color;
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 14f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            var colOverLife = ps.colorOverLifetime;
            colOverLife.enabled = true;
            Gradient g = new();
            g.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLife.color = g;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.enableGPUInstancing = false;
            psr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(VfxAdditiveMatPath);
        }

        private static void CreateForestFireflies(Transform parent)
        {
            GameObject go = new GameObject("VFX_ForestFireflies");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0.5f, 1.15f, 1.2f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 5.0f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
            main.startColor = new Color(1.0f, 0.85f, 0.32f, 0.72f);
            main.maxParticles = 45;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 10f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(24f, 1.4f, 16f);

            var colOverLife = ps.colorOverLifetime;
            colOverLife.enabled = true;
            Gradient g = new();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(1.0f, 0.88f, 0.38f), 0f), new GradientColorKey(new Color(0.95f, 0.75f, 0.28f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.45f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLife.color = g;

            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.enableGPUInstancing = false;
            psr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(VfxAdditiveMatPath);
        }

        private static GameObject PlaceKenneyProp(
            Transform parent,
            string fbxPath,
            Material material,
            Terrain terrain,
            Vector3 pos,
            float yawDeg,
            float scale,
            bool snapToTerrain = true)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[HollowKeepMapBuilder] Modelo não encontrado: {fbxPath}");
                return null;
            }

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetParent(parent, false);
            if (snapToTerrain && terrain != null)
            {
                pos.y = terrain.SampleHeight(pos);
            }
            inst.transform.position = pos;
            inst.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            inst.transform.localScale = Vector3.one * scale;

            if (material != null)
            {
                foreach (MeshRenderer mr in inst.GetComponentsInChildren<MeshRenderer>(true))
                {
                    Material[] mats = new Material[Mathf.Max(1, mr.sharedMaterials.Length)];
                    for (int i = 0; i < mats.Length; i++) mats[i] = material;
                    mr.sharedMaterials = mats;
                }
            }

            foreach (Collider col in inst.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(col);
            }

            return inst;
        }

        private static GameObject CreateChildGroup(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void ClearActiveSceneRoots()
        {
            Scene active = SceneManager.GetActiveScene();
            foreach (GameObject root in active.GetRootGameObjects())
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void EnsureMatteColormapMaterial(string matPath, string texPath, Color tint)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader lit = Shader.Find(UrpLitShaderName);
                mat = new Material(lit);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex);
            }

            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_SpecularHighlights", 0f);
            mat.SetFloat("_EnvironmentReflections", 0f);
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            EditorUtility.SetDirty(mat);
        }

        private static void EnsureEmissiveGlowMaterial(string matPath, Color baseColor, Color emissionColor)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader lit = Shader.Find(UrpLitShaderName);
                mat = new Material(lit);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", 0.1f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetColor("_EmissionColor", emissionColor);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(mat);
        }

        private static void TunePostProcessingProfile()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null) return;

            if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.42f);
            bloom.scatter.Override(0.64f);
            bloom.highQualityFiltering.Override(true);

            if (!profile.TryGet(out ColorAdjustments ca)) ca = profile.Add<ColorAdjustments>(true);
            ca.active = true;
            ca.postExposure.Override(0.16f);
            ca.contrast.Override(14f);
            ca.saturation.Override(8f);
            ca.colorFilter.Override(new Color(0.96f, 0.98f, 1.0f, 1f));

            if (!profile.TryGet(out Vignette vig)) vig = profile.Add<Vignette>(true);
            vig.active = true;
            vig.intensity.Override(0.26f);
            vig.smoothness.Override(0.35f);

            EditorUtility.SetDirty(profile);
        }

        private static void EnsureTerrainMaterial()
        {
            Shader terrainShader = Shader.Find(TerrainShaderName);
            if (terrainShader == null)
            {
                throw new InvalidOperationException($"Shader '{TerrainShaderName}' não encontrado.");
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            if (mat == null)
            {
                mat = new Material(terrainShader);
                AssetDatabase.CreateAsset(mat, TerrainMaterialPath);
            }
            else if (mat.shader != terrainShader)
            {
                mat.shader = terrainShader;
            }
            EditorUtility.SetDirty(mat);
        }

        private static void EnsureTerrainTexturePairAndLayer(
            string diffusePngName,
            string normalPngName,
            string layerName,
            Vector2 tileSize,
            float normalScale,
            Color32 baseCol,
            Color32 highCol,
            Color32 lowCol,
            int seed,
            float freq,
            float bumpStrength,
            TerrainPatternMode pattern)
        {
            string texPath = $"{TerrainTextureDir}/{diffusePngName}";
            string normPath = $"{TerrainTextureDir}/{normalPngName}";
            string layerPath = $"{TerrainLayerDir}/{layerName}";

            const int size = 512;
            float[] heightField = new float[size * size];
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float n1 = TileableNoise(u, v, freq, seed);
                    float n2 = TileableNoise(u, v, freq * 2f, seed + 13);
                    float n3 = TileableNoise(u, v, freq * 4f, seed + 41);
                    float val = n1 * 0.56f + n2 * 0.28f + n3 * 0.16f;

                    if (pattern == TerrainPatternMode.Grass)
                    {
                        // Traços finos orgânicos imitando tufos de relva
                        float blades = TileableNoise(u * 2.2f % 1f, v, freq * 3f, seed + 71);
                        val = val * 0.78f + blades * 0.22f;
                    }
                    else if (pattern == TerrainPatternMode.Dirt)
                    {
                        // Grãos sutis de cascalho / terra batida
                        float grit = TileableNoise(u, v, freq * 5f, seed + 89);
                        if (grit > 0.76f) val = Mathf.Clamp01(val + 0.16f);
                        else if (grit < 0.20f) val = Mathf.Clamp01(val - 0.14f);
                    }
                    else if (pattern == TerrainPatternMode.Cobble || pattern == TerrainPatternMode.Flagstone)
                    {
                        float rows = pattern == TerrainPatternMode.Cobble ? 5f : 6f;
                        float row = Mathf.Floor(v * rows);
                        float cellU = (u * rows + (row % 2f) * 0.5f) % 1f;
                        float cellV = (v * rows) % 1f;
                        float jitter = (TileableNoise(u, v, rows * 2f, seed + 55) - 0.5f) * 0.08f;
                        float edge = Mathf.Min(Mathf.Min(cellU, 1f - cellU), Mathf.Min(cellV, 1f - cellV)) + jitter;
                        float seamFactor = SmoothStep01(0.02f, 0.13f, edge);
                        float stoneTint = Hash01(Mathf.FloorToInt(u * rows), Mathf.FloorToInt(row), seed + 9) * 0.18f;
                        val = (val * 0.65f + stoneTint + 0.15f) * Mathf.Lerp(0.28f, 1.0f, seamFactor);
                    }

                    heightField[y * size + x] = val;

                    Color32 c = val < 0.45f
                        ? Color32.Lerp(lowCol, baseCol, val / 0.45f)
                        : Color32.Lerp(baseCol, highCol, (val - 0.45f) / 0.55f);
                    // Evita perda de canais RGB mantendo smoothness quase nula quando não há MaskMap
                    c.a = 1;
                    pixels[y * size + x] = c;
                }
            }

            Texture2D tex = new(size, size, TextureFormat.RGBA32, true, false);
            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            // Gera Normal Map tileable via gradiente Sobel
            Color32[] normalPixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                int ym = (y - 1 + size) % size;
                int yp = (y + 1) % size;
                for (int x = 0; x < size; x++)
                {
                    int xm = (x - 1 + size) % size;
                    int xp = (x + 1) % size;
                    float dx = (heightField[y * size + xp] - heightField[y * size + xm]) * bumpStrength;
                    float dy = (heightField[yp * size + x] - heightField[ym * size + x]) * bumpStrength;
                    Vector3 n = new Vector3(-dx, -dy, 1f).normalized;
                    normalPixels[y * size + x] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f), 0, 255),
                        255);
                }
            }

            Texture2D normTex = new(size, size, TextureFormat.RGBA32, true, true);
            normTex.SetPixels32(normalPixels);
            normTex.Apply(true, false);
            File.WriteAllBytes(normPath, normTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(normTex);

            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
            TextureImporter diffuseImp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (diffuseImp != null)
            {
                diffuseImp.wrapMode = TextureWrapMode.Repeat;
                diffuseImp.filterMode = FilterMode.Bilinear;
                diffuseImp.sRGBTexture = true;
                diffuseImp.mipmapEnabled = true;
                diffuseImp.alphaIsTransparency = false;
                diffuseImp.SaveAndReimport();
            }

            AssetDatabase.ImportAsset(normPath, ImportAssetOptions.ForceUpdate);
            TextureImporter normImp = AssetImporter.GetAtPath(normPath) as TextureImporter;
            if (normImp != null)
            {
                normImp.textureType = TextureImporterType.NormalMap;
                normImp.wrapMode = TextureWrapMode.Repeat;
                normImp.filterMode = FilterMode.Bilinear;
                normImp.mipmapEnabled = true;
                normImp.SaveAndReimport();
            }

            Texture2D importedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Texture2D importedNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(normPath);
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, layerPath);
            }

            layer.diffuseTexture = importedTex;
            layer.normalMapTexture = importedNorm;
            layer.normalScale = normalScale;
            layer.tileSize = tileSize;
            layer.tileOffset = Vector2.zero;
            layer.smoothness = 0f;
            layer.metallic = 0f;
            layer.specular = Color.black;
            EditorUtility.SetDirty(layer);
        }

        private static float TileableNoise(float u, float v, float period, int seed)
        {
            int p = Mathf.Max(2, Mathf.RoundToInt(period));
            float gx = u * p;
            float gy = v * p;
            int x0 = Mathf.FloorToInt(gx) % p;
            int y0 = Mathf.FloorToInt(gy) % p;
            int x1 = (x0 + 1) % p;
            int y1 = (y0 + 1) % p;
            float tx = gx - Mathf.Floor(gx);
            float ty = gy - Mathf.Floor(gy);
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);

            float v00 = Hash01(x0, y0, seed);
            float v10 = Hash01(x1, y0, seed);
            float v01 = Hash01(x0, y1, seed);
            float v11 = Hash01(x1, y1, seed);

            return Mathf.Lerp(Mathf.Lerp(v00, v10, tx), Mathf.Lerp(v01, v11, tx), ty);
        }

        private static float Hash01(int x, int y, int seed)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0x00FFFFFFu) / (float)0x01000000u;
        }

        private static TerrainLayer LoadTerrainLayer(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<TerrainLayer>($"{TerrainLayerDir}/{fileName}");
        }

        private static void EnsurePathIdentifier(string assetPath, Color gizmoColor, string description)
        {
            PathIdentifier pid = AssetDatabase.LoadAssetAtPath<PathIdentifier>(assetPath);
            if (pid == null)
            {
                pid = ScriptableObject.CreateInstance<PathIdentifier>();
                AssetDatabase.CreateAsset(pid, assetPath);
            }

            SerializedObject so = new SerializedObject(pid);
            so.FindProperty("gizmoColor").colorValue = gizmoColor;
            so.FindProperty("routeDescription").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pid);
        }

        private static void AddWave(
            SerializedProperty wavesProp,
            float duration,
            (float pct, (Enemy prefab, int qty, float interval, PathIdentifier path)[] groups)[] subwaves)
        {
            int wIdx = wavesProp.arraySize;
            wavesProp.InsertArrayElementAtIndex(wIdx);
            SerializedProperty waveProp = wavesProp.GetArrayElementAtIndex(wIdx);
            waveProp.FindPropertyRelative("duration").floatValue = duration;

            SerializedProperty subsProp = waveProp.FindPropertyRelative("subwaves");
            subsProp.ClearArray();

            for (int s = 0; s < subwaves.Length; s++)
            {
                subsProp.InsertArrayElementAtIndex(s);
                SerializedProperty subProp = subsProp.GetArrayElementAtIndex(s);
                subProp.FindPropertyRelative("wavePercentage").floatValue = subwaves[s].pct;

                SerializedProperty groupsProp = subProp.FindPropertyRelative("spawnGroups");
                groupsProp.ClearArray();
                var groups = subwaves[s].groups;
                for (int g = 0; g < groups.Length; g++)
                {
                    groupsProp.InsertArrayElementAtIndex(g);
                    SerializedProperty gProp = groupsProp.GetArrayElementAtIndex(g);
                    gProp.FindPropertyRelative("enemyPrefab").objectReferenceValue = groups[g].prefab;
                    gProp.FindPropertyRelative("quantity").intValue = groups[g].qty;
                    gProp.FindPropertyRelative("interval").floatValue = groups[g].interval;
                    gProp.FindPropertyRelative("targetPath").objectReferenceValue = groups[g].path;
                }
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> list = new(EditorBuildSettings.scenes);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].path == scenePath)
                {
                    list[i].enabled = true;
                    EditorBuildSettings.scenes = list.ToArray();
                    return;
                }
            }

            list.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            string[] parts = assetFolderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
