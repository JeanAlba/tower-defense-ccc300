#if UNITY_EDITOR
using System.IO;
using HollowKeep.Enemies;
using HollowKeep.Projectiles;
using HollowKeep.Towers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HollowKeep.Editor
{
    /// <summary>
    /// Cria materiais e prefabs de partículas URP (canhão, flecha e poeira) e conecta aos prefabs do jogo.
    /// </summary>
    [InitializeOnLoad]
    public static class VfxSetupHelper
    {
        private const string VfxFolder = "Assets/Prefabs/VFX";
        private const string VfxMatFolder = "Assets/Materials/VFX";
        private const string ExplosionPrefabPath = VfxFolder + "/VFX_CannonExplosion.prefab";
        private const string MuzzlePrefabPath = VfxFolder + "/VFX_CannonMuzzle.prefab";
        private const string ArrowHitPrefabPath = VfxFolder + "/VFX_ArrowHit.prefab";
        private const string BuildDustPrefabPath = VfxFolder + "/VFX_BuildDust.prefab";
        private const string DeathDustPrefabPath = VfxFolder + "/VFX_EnemyDeathDust.prefab";
        private const string UpgradePrefabPath = VfxFolder + "/VFX_TowerUpgrade.prefab";
        private const string LowPolyMeshMatPath = VfxMatFolder + "/M_VFX_LowPolyMesh.mat";
        private const string LowPolySphereMeshPath = VfxMatFolder + "/Mesh_LowPolySphere.asset";
        private const string LowPolyDiamondMeshPath = VfxMatFolder + "/Mesh_LowPolyDiamond.asset";
        private const string ImpactRingMeshPath = VfxMatFolder + "/Mesh_ImpactRing.asset";

        static VfxSetupHelper()
        {
            EditorApplication.delayCall += RunInitialSetupIfNeeded;
        }

        private static void RunInitialSetupIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            GameObject existingUpgrade = AssetDatabase.LoadAssetAtPath<GameObject>(UpgradePrefabPath);
            GameObject existingDust = AssetDatabase.LoadAssetAtPath<GameObject>(BuildDustPrefabPath);
            if (existingUpgrade != null && existingDust != null
                && existingUpgrade.TryGetComponent(out ParticleSystem upgradePs)
                && existingDust.TryGetComponent(out ParticleSystem dustPs))
            {
                var vel = upgradePs.velocityOverLifetime;
                bool velValid = vel.x.mode == vel.y.mode && vel.z.mode == vel.y.mode;
                Transform ringChild = existingUpgrade.transform.Find("UpgradeRing");
                bool ringUpdated = ringChild != null
                    && ringChild.TryGetComponent(out ParticleSystem ringPs)
                    && ringPs.main.startSize.constantMax >= 3.0f;
                bool dustUpdated = dustPs.shape.radius >= 0.7f;

                if (velValid && ringUpdated && dustUpdated)
                {
                    return;
                }
            }

            SetupAllParticles();
        }

        [MenuItem("HollowKeep/4. Configurar Particulas (Canhao, Flecha, Poeira)")]
        public static void SetupAllParticles()
        {
            EnsureFolder("Assets/Materials");
            EnsureFolder(VfxMatFolder);
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(VfxFolder);

            Material alphaMat = CreateOrLoadParticleMaterial(VfxMatFolder + "/M_VFX_Alpha.mat", additive: false);
            Material additiveMat = CreateOrLoadParticleMaterial(VfxMatFolder + "/M_VFX_Additive.mat", additive: true);
            Material lowPolyMat = CreateOrLoadLowPolyMeshMaterial(LowPolyMeshMatPath);
            Mesh lowPolySphere = CreateOrLoadLowPolySphereMesh(LowPolySphereMeshPath);
            Mesh lowPolyDiamond = CreateOrLoadLowPolyDiamondMesh(LowPolyDiamondMeshPath);
            Mesh impactRingMesh = CreateOrLoadImpactRingMesh(ImpactRingMeshPath);

            ParticleSystem explosionVfx = CreateCannonExplosionPrefab(lowPolyMat, lowPolySphere, alphaMat, impactRingMesh);
            ParticleSystem muzzleVfx = CreateCannonMuzzlePrefab(additiveMat, alphaMat);
            ParticleSystem arrowHitVfx = CreateArrowHitPrefab(additiveMat);
            ParticleSystem buildDustVfx = CreateLowPolyBuildDustPrefab(lowPolyMat, lowPolySphere);
            ParticleSystem upgradeVfx = CreateTowerUpgradePrefab(lowPolyMat, lowPolyDiamond, alphaMat, impactRingMesh);
            ParticleSystem deathDustVfx = CreateDustPrefab(DeathDustPrefabPath, "VFX_EnemyDeathDust", alphaMat, 6, 0.25f, 0.6f, 1.3f, 0.25f, 0.45f, new Color(0.55f, 0.55f, 0.55f, 0.5f));

            WireParabolicProjectile(explosionVfx, alphaMat);
            WireSingleVfxField<ParabolicTower>("Assets/Prefabs/Towers/ParabolicTower.prefab", "muzzleVfxPrefab", muzzleVfx);
            WireSingleVfxField<DirectProjectile>("Assets/Prefabs/Projectiles/DirectProjectile.prefab", "hitVfxPrefab", arrowHitVfx);
            WireSingleVfxField<BuildSlot>("Assets/Prefabs/Towers/BuildSlot.prefab", "buildDustVfxPrefab", buildDustVfx);
            WireSingleVfxField<BuildSlot>("Assets/Prefabs/Towers/BuildSlot.prefab", "upgradeVfxPrefab", upgradeVfx);
            WireSingleVfxField<Enemy>("Assets/Prefabs/Enemies/Esqueleto.prefab", "deathDustVfxPrefab", deathDustVfx);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VfxSetup] Partículas de canhão, flecha, construção/demolição e upgrade atualizadas.");
        }

        private static Material CreateOrLoadParticleMaterial(string path, bool additive)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            Material mat = new Material(shader);
            Material referenceUrpMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
            Texture particleTex = referenceUrpMat != null && referenceUrpMat.HasProperty("_BaseMap")
                ? referenceUrpMat.GetTexture("_BaseMap")
                : AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");

            if (particleTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", particleTex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", particleTex);
            }

            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", additive ? 2f : 0f);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Material CreateOrLoadLowPolyMeshMaterial(string path)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }

            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ColorMode", 0f);
            mat.SetFloat("_SpecularHighlights", 0f);
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_ReceiveShadows", 0f);
            mat.SetInt("_SrcBlend", (int)BlendMode.One);
            mat.SetInt("_DstBlend", (int)BlendMode.Zero);
            mat.SetFloat("_ZWrite", 1f);
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = (int)RenderQueue.Geometry;

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Mesh CreateOrLoadLowPolySphereMesh(string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] baseVerts =
            {
                new Vector3(-1f,  t,  0f).normalized * 0.5f,
                new Vector3( 1f,  t,  0f).normalized * 0.5f,
                new Vector3(-1f, -t,  0f).normalized * 0.5f,
                new Vector3( 1f, -t,  0f).normalized * 0.5f,
                new Vector3( 0f, -1f,  t).normalized * 0.5f,
                new Vector3( 0f,  1f,  t).normalized * 0.5f,
                new Vector3( 0f, -1f, -t).normalized * 0.5f,
                new Vector3( 0f,  1f, -t).normalized * 0.5f,
                new Vector3( t,  0f, -1f).normalized * 0.5f,
                new Vector3( t,  0f,  1f).normalized * 0.5f,
                new Vector3(-t,  0f, -1f).normalized * 0.5f,
                new Vector3(-t,  0f,  1f).normalized * 0.5f
            };

            int[] faces =
            {
                0, 11, 5,   0, 5, 1,   0, 1, 7,   0, 7, 10,  0, 10, 11,
                1, 5, 9,    5, 11, 4,  11, 10, 2, 10, 7, 6,  7, 1, 8,
                3, 9, 4,    3, 4, 2,   3, 2, 6,   3, 6, 8,   3, 8, 9,
                4, 9, 5,    2, 4, 11,  6, 2, 10,  8, 6, 7,   9, 8, 1
            };

            // Duplica vértices por triângulo para sombreamento flat low-poly.
            Vector3[] flatVerts = new Vector3[faces.Length];
            int[] flatTris = new int[faces.Length];
            for (int i = 0; i < faces.Length; i++)
            {
                flatVerts[i] = baseVerts[faces[i]];
                flatTris[i] = i;
            }

            Mesh mesh = new Mesh { name = "Mesh_LowPolySphere" };
            mesh.vertices = flatVerts;
            mesh.triangles = flatTris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Mesh CreateOrLoadLowPolyDiamondMesh(string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            Vector3 top = new(0f, 0.55f, 0f);
            Vector3 bottom = new(0f, -0.55f, 0f);
            Vector3 p0 = new(0.35f, 0f, 0f);
            Vector3 p1 = new(0f, 0f, 0.35f);
            Vector3 p2 = new(-0.35f, 0f, 0f);
            Vector3 p3 = new(0f, 0f, -0.35f);

            Vector3[] verts =
            {
                top, p1, p0,
                top, p2, p1,
                top, p3, p2,
                top, p0, p3,
                bottom, p0, p1,
                bottom, p1, p2,
                bottom, p2, p3,
                bottom, p3, p0
            };

            int[] tris = new int[verts.Length];
            for (int i = 0; i < tris.Length; i++)
            {
                tris[i] = i;
            }

            Mesh mesh = new Mesh { name = "Mesh_LowPolyDiamond" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Mesh CreateOrLoadImpactRingMesh(string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            const int segments = 24;
            const float innerRadius = 0.41f;
            const float outerRadius = 0.5f;
            const float yOffset = 0.06f;

            Vector3[] verts = new Vector3[segments * 2];
            Vector2[] uvs = new Vector2[segments * 2];
            int[] tris = new int[segments * 6];

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                int vi = i * 2;
                verts[vi] = new Vector3(cos * innerRadius, yOffset, sin * innerRadius);
                verts[vi + 1] = new Vector3(cos * outerRadius, yOffset, sin * outerRadius);
                uvs[vi] = new Vector2(0.5f, 0.5f);
                uvs[vi + 1] = new Vector2(0.5f, 0.5f);

                int nextVi = ((i + 1) % segments) * 2;
                int ti = i * 6;
                tris[ti] = vi;
                tris[ti + 1] = nextVi + 1;
                tris[ti + 2] = vi + 1;
                tris[ti + 3] = vi;
                tris[ti + 4] = nextVi;
                tris[ti + 5] = nextVi + 1;
            }

            Mesh mesh = new Mesh { name = "Mesh_ImpactRing" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static ParticleSystem CreateCannonExplosionPrefab(Material lowPolyMat, Mesh lowPolySphere, Material alphaMat, Mesh impactRingMesh)
        {
            GameObject root = new GameObject("VFX_CannonExplosion");
            ParticleSystem coreFire = ConfigureBaseSystem(root, 0.6f, 0.18f, 0.28f, 0.6f, 1.6f, 0.7f, 1.15f, 0f, lowPolyMat, destroyOnStop: true);
            SetBurst(coreFire, 6);
            var fireShape = coreFire.shape;
            fireShape.shapeType = ParticleSystemShapeType.Sphere;
            fireShape.radius = 0.2f;
            SetColorGradient(coreFire, new Color(1f, 0.88f, 0.28f, 1f), new Color(0.98f, 0.45f, 0.1f, 1f), new Color(0.85f, 0.25f, 0.06f, 1f));
            SetPopShrinkCurve(coreFire, 0.5f, 1.15f, 0.28f);
            SetMeshRenderMode(coreFire, lowPolySphere, ParticleSystemRenderSpace.World);

            GameObject smokeObj = new GameObject("DenseSmoke");
            smokeObj.transform.SetParent(root.transform, false);
            ParticleSystem smoke = ConfigureBaseSystem(smokeObj, 0.55f, 0.35f, 0.52f, 1.3f, 2.6f, 0.65f, 1.1f, -0.25f, lowPolyMat, destroyOnStop: false);
            var smokeEmission = smoke.emission;
            smokeEmission.rateOverTime = 0f;
            smokeEmission.SetBursts(new[] { new ParticleSystem.Burst(0.03f, (short)12) });
            var smokeShape = smoke.shape;
            smokeShape.shapeType = ParticleSystemShapeType.Hemisphere;
            smokeShape.radius = 0.35f;
            var smokeLimit = smoke.limitVelocityOverLifetime;
            smokeLimit.enabled = true;
            smokeLimit.limit = 0.7f;
            smokeLimit.dampen = 0.3f;
            var smokeRot = smoke.rotationOverLifetime;
            smokeRot.enabled = true;
            smokeRot.separateAxes = true;
            float maxRadPerSec = 90f * Mathf.Deg2Rad;
            smokeRot.x = new ParticleSystem.MinMaxCurve(-maxRadPerSec, maxRadPerSec);
            smokeRot.y = new ParticleSystem.MinMaxCurve(-maxRadPerSec, maxRadPerSec);
            smokeRot.z = new ParticleSystem.MinMaxCurve(-maxRadPerSec, maxRadPerSec);
            SetColorGradient(smoke, new Color(0.36f, 0.34f, 0.36f, 1f), new Color(0.28f, 0.27f, 0.29f, 1f), new Color(0.22f, 0.22f, 0.24f, 1f));
            SetPopShrinkCurve(smoke, 0.4f, 1.1f, 0.25f);
            SetMeshRenderMode(smoke, lowPolySphere, ParticleSystemRenderSpace.World);

            GameObject ringObj = new GameObject("ImpactRing");
            ringObj.transform.SetParent(root.transform, false);
            ParticleSystem ring = ConfigureBaseSystem(ringObj, 0.4f, 0.34f, 0.34f, 0f, 0f, 5.0f, 5.0f, 0f, alphaMat, destroyOnStop: false);
            SetBurst(ring, 1);
            var ringShape = ring.shape;
            ringShape.enabled = false;
            SetColorGradient(ring, new Color(1f, 0.62f, 0.18f, 0.85f), new Color(1f, 0.45f, 0.12f, 0.55f), new Color(0.95f, 0.32f, 0.08f, 0f));
            SetSizeCurve(ring, 0.15f, 1f);
            SetMeshRenderMode(ring, impactRingMesh, ParticleSystemRenderSpace.World);

            return SaveVfxPrefab(root, ExplosionPrefabPath);
        }

        private static void SetPopShrinkCurve(ParticleSystem ps, float startScale, float peakScale, float peakTime)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, startScale),
                new Keyframe(peakTime, peakScale),
                new Keyframe(1f, 0f)
            );
            sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static void SetMeshRenderMode(ParticleSystem ps, Mesh mesh, ParticleSystemRenderSpace alignment)
        {
            ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Mesh;
            psr.mesh = mesh;
            psr.alignment = alignment;
        }

        private static ParticleSystem CreateLowPolyBuildDustPrefab(Material lowPolyMat, Mesh lowPolySphere)
        {
            GameObject root = new GameObject("VFX_BuildDust");
            ParticleSystem dust = ConfigureBaseSystem(root, 0.7f, 0.42f, 0.62f, 2.4f, 4.0f, 0.45f, 0.78f, -0.35f, lowPolyMat, destroyOnStop: true);
            SetBurst(dust, 16);

            var shape = dust.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.75f;
            shape.radiusThickness = 0.25f;
            shape.position = new Vector3(0f, 0.22f, 0f);
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var limit = dust.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 1.4f;
            limit.dampen = 0.25f;

            var rot = dust.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            float maxRad = 80f * Mathf.Deg2Rad;
            rot.x = new ParticleSystem.MinMaxCurve(-maxRad, maxRad);
            rot.y = new ParticleSystem.MinMaxCurve(-maxRad, maxRad);
            rot.z = new ParticleSystem.MinMaxCurve(-maxRad, maxRad);

            SetColorGradient(dust, new Color(0.82f, 0.77f, 0.66f, 1f), new Color(0.7f, 0.65f, 0.56f, 1f), new Color(0.58f, 0.54f, 0.48f, 1f));
            SetPopShrinkCurve(dust, 0.55f, 1.15f, 0.25f);
            SetMeshRenderMode(dust, lowPolySphere, ParticleSystemRenderSpace.World);

            return SaveVfxPrefab(root, BuildDustPrefabPath);
        }

        private static ParticleSystem CreateTowerUpgradePrefab(Material lowPolyMat, Mesh diamondMesh, Material alphaMat, Mesh ringMesh)
        {
            GameObject root = new GameObject("VFX_TowerUpgrade");
            ParticleSystem shards = ConfigureBaseSystem(root, 0.85f, 0.48f, 0.7f, 0.2f, 0.5f, 0.24f, 0.36f, 0f, lowPolyMat, destroyOnStop: true);
            var emission = shards.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)12),
                new ParticleSystem.Burst(0.12f, (short)8)
            });

            var shape = shards.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.85f;
            shape.radiusThickness = 0f;
            shape.position = new Vector3(0f, 0.15f, 0f);
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var vel = shards.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(2.6f, 4.0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var rot = shards.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = 0f;
            rot.y = 180f * Mathf.Deg2Rad;
            rot.z = 0f;

            SetColorGradient(shards, new Color(1f, 0.92f, 0.3f, 1f), new Color(1f, 0.75f, 0.16f, 1f), new Color(0.95f, 0.55f, 0.1f, 1f));
            SetPopShrinkCurve(shards, 0.45f, 1.2f, 0.35f);
            SetMeshRenderMode(shards, diamondMesh, ParticleSystemRenderSpace.World);

            GameObject ringObj = new GameObject("UpgradeRing");
            ringObj.transform.SetParent(root.transform, false);
            ParticleSystem ring = ConfigureBaseSystem(ringObj, 0.55f, 0.48f, 0.48f, 0f, 0f, 3.6f, 3.6f, 0f, alphaMat, destroyOnStop: false);
            SetBurst(ring, 1);
            var ringShape = ring.shape;
            ringShape.enabled = false;

            var ringVel = ring.velocityOverLifetime;
            ringVel.enabled = true;
            ringVel.space = ParticleSystemSimulationSpace.World;
            ringVel.x = 0f;
            ringVel.y = 2.2f;
            ringVel.z = 0f;

            SetColorGradient(ring, new Color(1f, 0.9f, 0.32f, 0.9f), new Color(1f, 0.74f, 0.2f, 0.65f), new Color(1f, 0.6f, 0.12f, 0f));
            SetSizeCurve(ring, 0.55f, 1.15f);
            SetMeshRenderMode(ring, ringMesh, ParticleSystemRenderSpace.World);

            return SaveVfxPrefab(root, UpgradePrefabPath);
        }

        private static ParticleSystem CreateCannonMuzzlePrefab(Material additiveMat, Material alphaMat)
        {
            GameObject root = new GameObject("VFX_CannonMuzzle");
            ParticleSystem smoke = ConfigureBaseSystem(root, 0.45f, 0.22f, 0.38f, 2.5f, 4.5f, 0.35f, 0.6f, -0.15f, alphaMat, destroyOnStop: true);
            SetBurst(smoke, 8);
            var shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 22f;
            shape.radius = 0.08f;
            SetColorGradient(smoke, new Color(0.9f, 0.65f, 0.35f, 0.85f), new Color(0.35f, 0.35f, 0.35f, 0.55f), new Color(0.25f, 0.25f, 0.25f, 0f));
            SetSizeCurve(smoke, 0.6f, 1.4f);

            GameObject flashObj = new GameObject("Flash");
            flashObj.transform.SetParent(root.transform, false);
            ParticleSystem flash = ConfigureBaseSystem(flashObj, 0.2f, 0.08f, 0.14f, 1f, 2.2f, 0.35f, 0.5f, 0f, additiveMat, destroyOnStop: false);
            SetBurst(flash, 3);
            var flashShape = flash.shape;
            flashShape.shapeType = ParticleSystemShapeType.Cone;
            flashShape.angle = 15f;
            flashShape.radius = 0.05f;
            SetColorGradient(flash, new Color(1f, 0.9f, 0.4f, 1f), new Color(1f, 0.45f, 0.1f, 0.7f), new Color(1f, 0.2f, 0.05f, 0f));

            return SaveVfxPrefab(root, MuzzlePrefabPath);
        }

        private static ParticleSystem CreateArrowHitPrefab(Material additiveMat)
        {
            GameObject root = new GameObject("VFX_ArrowHit");
            ParticleSystem sparks = ConfigureBaseSystem(root, 0.35f, 0.15f, 0.28f, 2.5f, 5f, 0.08f, 0.16f, 0.8f, additiveMat);
            SetBurst(sparks, 7);
            var shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;
            SetColorGradient(sparks, new Color(1f, 0.94f, 0.65f, 1f), new Color(0.95f, 0.72f, 0.35f, 0.8f), new Color(0.8f, 0.5f, 0.2f, 0f));
            SetSizeCurve(sparks, 1f, 0.2f);

            return SaveVfxPrefab(root, ArrowHitPrefabPath);
        }

        private static ParticleSystem CreateDustPrefab(
            string prefabPath,
            string objectName,
            Material alphaMat,
            short count,
            float radius,
            float minSpeed,
            float maxSpeed,
            float minSize,
            float maxSize,
            Color startColor)
        {
            GameObject root = new GameObject(objectName);
            ParticleSystem dust = ConfigureBaseSystem(root, 0.65f, 0.3f, 0.55f, minSpeed, maxSpeed, minSize, maxSize, -0.1f, alphaMat);
            SetBurst(dust, count);

            var shape = dust.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var limit = dust.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.8f;
            limit.dampen = 0.25f;

            Color midColor = new Color(startColor.r, startColor.g, startColor.b, startColor.a * 0.55f);
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            SetColorGradient(dust, startColor, midColor, endColor);
            SetSizeCurve(dust, 0.75f, 1.25f);

            return SaveVfxPrefab(root, prefabPath);
        }

        private static void WireParabolicProjectile(ParticleSystem explosionVfx, Material alphaMat)
        {
            const string path = "Assets/Prefabs/Projectiles/ParabolicProjectile.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) return;

            try
            {
                Transform existingTrail = root.transform.Find("TrailVfx");
                if (existingTrail != null)
                {
                    Object.DestroyImmediate(existingTrail.gameObject);
                }

                GameObject trailObj = new GameObject("TrailVfx");
                trailObj.transform.SetParent(root.transform, false);
                ParticleSystem trail = trailObj.AddComponent<ParticleSystem>();
                var main = trail.main;
                main.duration = 1f;
                main.loop = true;
                main.playOnAwake = true;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.34f);
                main.startSpeed = 0.15f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.3f);

                var emission = trail.emission;
                emission.rateOverTime = 24f;

                var shape = trail.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.05f;

                SetColorGradient(trail, new Color(0.35f, 0.35f, 0.35f, 0.55f), new Color(0.25f, 0.25f, 0.25f, 0.25f), new Color(0.2f, 0.2f, 0.2f, 0f));
                SetSizeCurve(trail, 1f, 0.2f);

                ParticleSystemRenderer psr = trail.GetComponent<ParticleSystemRenderer>();
                psr.sharedMaterial = alphaMat;

                ParabolicProjectile proj = root.GetComponent<ParabolicProjectile>();
                if (proj != null)
                {
                    SerializedObject so = new SerializedObject(proj);
                    so.FindProperty("explosionVfxPrefab").objectReferenceValue = explosionVfx;
                    so.FindProperty("trailVfx").objectReferenceValue = trail;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireSingleVfxField<T>(string prefabPath, string fieldName, ParticleSystem vfxPrefab) where T : Component
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return;

            try
            {
                T component = root.GetComponent<T>();
                if (component != null)
                {
                    SerializedObject so = new SerializedObject(component);
                    SerializedProperty prop = so.FindProperty(fieldName);
                    if (prop != null)
                    {
                        prop.objectReferenceValue = vfxPrefab;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static ParticleSystem ConfigureBaseSystem(
            GameObject target,
            float duration,
            float minLifetime,
            float maxLifetime,
            float minSpeed,
            float maxSpeed,
            float minSize,
            float maxSize,
            float gravity,
            Material mat,
            bool destroyOnStop = true)
        {
            ParticleSystem ps = target.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = true;
            main.stopAction = destroyOnStop ? ParticleSystemStopAction.Destroy : ParticleSystemStopAction.None;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.gravityModifier = gravity;

            ParticleSystemRenderer psr = target.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;

            return ps;
        }

        private static void SetBurst(ParticleSystem ps, short count)
        {
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        }

        private static void SetColorGradient(ParticleSystem ps, Color start, Color mid, Color end)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(mid, 0.45f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(start.a, 0f),
                    new GradientAlphaKey(mid.a, 0.45f),
                    new GradientAlphaKey(end.a, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void SetSizeCurve(ParticleSystem ps, float startScale, float endScale)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, startScale, 1f, endScale));
        }

        private static ParticleSystem SaveVfxPrefab(GameObject root, string prefabPath)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return saved != null ? saved.GetComponent<ParticleSystem>() : null;
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
