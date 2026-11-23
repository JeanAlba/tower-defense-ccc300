#if UNITY_EDITOR
using System.IO;
using HollowKeep.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HollowKeep.Editor
{
    /// <summary>
    /// Configura o FBX do esqueleto, cria o AnimatorController base (Walk + Die) e atualiza Esqueleto.prefab.
    /// </summary>
    [InitializeOnLoad]
    public static class EnemyAnimationSetupHelper
    {
        private const string SkeletonFbxPath = "Assets/KenneyPack/characters/enemies/character-skeleton.fbx";
        private const string SkeletonPrefabPath = "Assets/Prefabs/Enemies/Esqueleto.prefab";
        private const string ControllerPath = "Assets/Animations/Enemies/AC_EnemyBase.controller";
        private const string GraveyardMaterialPath = "Assets/Materials/M_Kenney_Graveyard.mat";
        private const string MiniDungeonMaterialPath = "Assets/Materials/M_Kenney_MiniDungeon.mat";

        private static readonly (string prefabPath, string fbxPath, string materialPath)[] VariantEnemyPrefabs =
        {
            ("Assets/Prefabs/Enemies/Corredor.prefab", "Assets/KenneyPack/characters/enemies/character-ghost.fbx", GraveyardMaterialPath),
            ("Assets/Prefabs/Enemies/Cultista.prefab", "Assets/KenneyPack/characters/enemies/character-vampire.fbx", GraveyardMaterialPath),
            ("Assets/Prefabs/Enemies/Brutamontes.prefab", "Assets/KenneyPack/characters/enemies/character-orc.fbx", MiniDungeonMaterialPath),
            ("Assets/Prefabs/Enemies/Campeao.prefab", "Assets/KenneyPack/characters/enemies/character-zombie.fbx", GraveyardMaterialPath)
        };

        static EnemyAnimationSetupHelper()
        {
            EditorApplication.delayCall += RunInitialSetupIfNeeded;
        }

        private static void RunInitialSetupIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) == null)
            {
                SetupSkeletonAnimations();
                return;
            }

            if (NeedsVariantAvatarSetup())
            {
                SetupVariantEnemyAnimations();
            }
        }

        private static bool NeedsVariantAvatarSetup()
        {
            for (int i = 0; i < VariantEnemyPrefabs.Length; i++)
            {
                var (_, fbxPath, materialPath) = VariantEnemyPrefabs[i];
                ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                if (importer == null) continue;

                if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    return true;
                }

                Material targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (targetMaterial != null)
                {
                    AssetImporter.SourceAssetIdentifier id = new(typeof(Material), "colormap");
                    var externalMap = importer.GetExternalObjectMap();
                    if (!externalMap.TryGetValue(id, out Object currentObj) || currentObj != targetMaterial)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [MenuItem("HollowKeep/3. Configurar Animacoes Esqueleto (FBX)")]
        public static void SetupSkeletonAnimations()
        {
            if (!ConfigureSkeletonFbxImport(SkeletonFbxPath, out AnimationClip walkClip, out AnimationClip dieClip, out Avatar avatar))
            {
                return;
            }

            AnimatorController controller = CreateOrUpdateBaseController(walkClip, dieClip);
            if (controller == null) return;

            UpdateSkeletonPrefab(controller, avatar);
            SetupVariantEnemyAnimations(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EnemyAnimationSetup] Prefabs de inimigos atualizados para FBX com animações Walk + Die.");
        }

        [MenuItem("HollowKeep/4. Configurar Animacoes Variantes (FBX)")]
        public static void SetupVariantEnemyAnimations()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                SetupSkeletonAnimations();
                return;
            }

            SetupVariantEnemyAnimations(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SetupVariantEnemyAnimations(AnimatorController controller)
        {
            for (int i = 0; i < VariantEnemyPrefabs.Length; i++)
            {
                var (prefabPath, fbxPath, materialPath) = VariantEnemyPrefabs[i];
                if (!ConfigureSkeletonFbxImport(fbxPath, out _, out _, out Avatar avatar, materialPath))
                {
                    continue;
                }

                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefabRoot == null) continue;

                try
                {
                    Animator anim = prefabRoot.GetComponentInChildren<Animator>();
                    if (anim != null)
                    {
                        anim.runtimeAnimatorController = controller;
                        anim.avatar = avatar;
                        anim.applyRootMotion = false;
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }

        private static bool ConfigureSkeletonFbxImport(
            string fbxPath,
            out AnimationClip walkClip,
            out AnimationClip dieClip,
            out Avatar avatar,
            string materialPath = null)
        {
            walkClip = null;
            dieClip = null;
            avatar = null;

            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[EnemyAnimationSetup] FBX não encontrado em: {fbxPath}");
                return false;
            }

            bool needsReimport = false;

            if (!string.IsNullOrEmpty(materialPath))
            {
                Material targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (targetMaterial != null)
                {
                    AssetImporter.SourceAssetIdentifier id = new(typeof(Material), "colormap");
                    var externalMap = importer.GetExternalObjectMap();
                    if (!externalMap.TryGetValue(id, out Object currentObj) || currentObj != targetMaterial)
                    {
                        importer.AddRemap(id, targetMaterial);
                        needsReimport = true;
                    }
                }
            }

            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                needsReimport = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                needsReimport = true;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;

            for (int i = 0; i < clips.Length; i++)
            {
                bool shouldLoop = clips[i].name == "walk" || clips[i].name == "idle" || clips[i].name == "sprint";
                if (clips[i].loopTime != shouldLoop)
                {
                    clips[i].loopTime = shouldLoop;
                    needsReimport = true;
                }
            }

            if (needsReimport)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }

            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] is Avatar foundAvatar)
                {
                    avatar = foundAvatar;
                }
                else if (subAssets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    if (clip.name == "walk") walkClip = clip;
                    else if (clip.name == "die") dieClip = clip;
                }
            }

            if (walkClip == null || dieClip == null || avatar == null)
            {
                Debug.LogError($"[EnemyAnimationSetup] Sub-assets ausentes no FBX (walk={walkClip != null}, die={dieClip != null}, avatar={avatar != null}).");
                return false;
            }

            return true;
        }

        private static AnimatorController CreateOrUpdateBaseController(AnimationClip walkClip, AnimationClip dieClip)
        {
            EnsureFolder("Assets/Animations");
            EnsureFolder("Assets/Animations/Enemies");

            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine rootSm = controller.layers[0].stateMachine;

            AnimatorState walkState = rootSm.AddState("Walk");
            walkState.motion = walkClip;
            rootSm.defaultState = walkState;

            AnimatorState dieState = rootSm.AddState("Die");
            dieState.motion = dieClip;

            AnimatorStateTransition anyToDie = rootSm.AddAnyStateTransition(dieState);
            anyToDie.AddCondition(AnimatorConditionMode.If, 0f, "Die");
            anyToDie.hasExitTime = false;
            anyToDie.duration = 0.08f;
            anyToDie.canTransitionToSelf = false;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void UpdateSkeletonPrefab(AnimatorController controller, Avatar avatar)
        {
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonFbxPath);
            if (fbxAsset == null)
            {
                Debug.LogError($"[EnemyAnimationSetup] Não foi possível carregar GameObject do FBX: {SkeletonFbxPath}");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(SkeletonPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[EnemyAnimationSetup] Prefab não encontrado: {SkeletonPrefabPath}");
                return;
            }

            try
            {
                for (int i = prefabRoot.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = prefabRoot.transform.GetChild(i);
                    if (child.name.Contains("character-skeleton"))
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                }

                GameObject modelInstance = PrefabUtility.InstantiatePrefab(fbxAsset, prefabRoot.transform) as GameObject;
                if (modelInstance == null)
                {
                    Debug.LogError("[EnemyAnimationSetup] Falha ao instanciar FBX dentro de Esqueleto.prefab.");
                    return;
                }

                modelInstance.name = "character-skeleton";
                modelInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                modelInstance.transform.localScale = Vector3.one;

                Animator anim = modelInstance.GetComponent<Animator>();
                if (anim == null) anim = modelInstance.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
                anim.avatar = avatar;
                anim.applyRootMotion = false;

                Enemy enemy = prefabRoot.GetComponent<Enemy>();
                if (enemy != null)
                {
                    SerializedObject so = new SerializedObject(enemy);
                    so.FindProperty("animator").objectReferenceValue = anim;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, SkeletonPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
