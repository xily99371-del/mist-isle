#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MistIsle.Editor
{
    // Explicit editor commands: importing this file never replaces a scene or starts a build.
    public static class MistIsleProjectBuilder
    {
        public const string MainScene = "Assets/Scenes/CoastalCamp.unity";
        public const string RendererPath = "Assets/Settings/MistIsleRenderer2D.asset";
        public const string PipelinePath = "Assets/Settings/MistIsleURP.asset";

        [MenuItem("Mist Isle/Build Chapter One Content")]
        public static void BuildContent()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Resources/Settings");
            ImportApprovedArtIfAvailable();
            ConfigureRendering();
            ConfigureProject();
            BakeCatalog();
            if (AssetDatabase.LoadAssetAtPath<MistIsle.Runtime.OpeningStory>("Assets/Resources/Data/OpeningStory.asset") == null)
                AssetDatabase.CreateAsset(MistIsle.Runtime.OpeningStory.CreateDefault(), "Assets/Resources/Data/OpeningStory.asset");
            if (!File.Exists(MainScene)) CreateMainScene();
            else EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MIST_ISLE_CONTENT_READY: " + MainScene);
        }

        private static void ImportApprovedArtIfAvailable()
        {
            Type importer = FindType("MistIsle.Editor.ApprovedArtImporter");
            var method = importer?.GetMethod("ImportApprovedArt", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, null);
            importer?.GetMethod("ValidateImportedArt", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.Invoke(null, null);
            importer?.GetMethod("ValidateOriginalPixelActors", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.Invoke(null, null);
            importer?.GetMethod("ValidateApprovedPortraits", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.Invoke(null, null);
        }

        private static void ConfigureRendering()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }
            var rendererSettings = new SerializedObject(renderer);
            var sortMode = rendererSettings.FindProperty("m_TransparencySortMode");
            var sortAxis = rendererSettings.FindProperty("m_TransparencySortAxis");
            if (sortMode != null) sortMode.intValue = (int)TransparencySortMode.CustomAxis;
            if (sortAxis != null) sortAxis.vector3Value = Vector3.up;
            rendererSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            else
            {
                var settings = new SerializedObject(pipeline);
                var rendererList = settings.FindProperty("m_RendererDataList");
                if (rendererList != null)
                {
                    rendererList.arraySize = 1;
                    rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                }
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            pipeline.msaaSampleCount = 1;
            pipeline.renderScale = 1f;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int previousQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(previousQuality, false);
            EditorUtility.SetDirty(pipeline);
            const string spriteMaterialPath = "Assets/Resources/Settings/MistIsleSpriteLit.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(spriteMaterialPath) == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                if (shader == null) throw new InvalidOperationException("URP 2D sprite shader was not imported.");
                AssetDatabase.CreateAsset(new Material(shader), spriteMaterialPath);
            }
        }

        private static void ConfigureProject()
        {
            PlayerSettings.companyName = "MistIsle";
            PlayerSettings.productName = "Mist Isle — 雾屿回响";
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.mistisle.echoes");
            EditorSettings.serializationMode = SerializationMode.ForceText;

            var playerSettingsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            if (playerSettingsAsset != null)
            {
                var playerSettings = new SerializedObject(playerSettingsAsset);
                var input = playerSettings.FindProperty("activeInputHandler");
                if (input != null) input.intValue = 0;
                playerSettings.ApplyModifiedPropertiesWithoutUndo();
            }
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
            if (tagManager != null)
            {
                var tags = new SerializedObject(tagManager);
                var layers = tags.FindProperty("layers");
                SetLayer(layers, 8, "Player");
                SetLayer(layers, 9, "World");
                SetLayer(layers, 10, "Interactable");
                tags.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BakeCatalog()
        {
            EnsureFolder("Assets/Resources/Data/Species");
            EnsureFolder("Assets/Resources/Data/Recipes");
            var catalog = MistIsle.Creatures.GameCatalog.CreateSpecies();
            var persistent = new System.Collections.Generic.Dictionary<string, MistIsle.Creatures.CreatureSpecies>();
            var created = new System.Collections.Generic.HashSet<string>();
            foreach (var pair in catalog)
            {
                string path = "Assets/Resources/Data/Species/" + pair.Key + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<MistIsle.Creatures.CreatureSpecies>(path);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(pair.Value, path);
                    persistent[pair.Key] = pair.Value;
                    created.Add(pair.Key);
                }
                else
                {
                    persistent[pair.Key] = existing;
                    if (existing.designApprovalPending && !existing.contentReady && pair.Value.contentReady
                        && Resources.Load<Sprite>(existing.spriteResourcePath) != null)
                    {
                        existing.contentReady = true;
                        EditorUtility.SetDirty(existing);
                    }
                }
            }
            foreach (var pair in catalog)
            {
                if (created.Contains(pair.Key) && pair.Value.evolutionTarget != null)
                    persistent[pair.Key].evolutionTarget = persistent[pair.Value.evolutionTarget.speciesId];
                if (created.Contains(pair.Key)) EditorUtility.SetDirty(persistent[pair.Key]);
                else UnityEngine.Object.DestroyImmediate(pair.Value);
            }
            foreach (var recipe in MistIsle.Creatures.GameCatalog.CreateRecipes())
            {
                string path = "Assets/Resources/Data/Recipes/" + recipe.recipeId + ".asset";
                if (AssetDatabase.LoadAssetAtPath<MistIsle.Items.CampfireRecipe>(path) == null) AssetDatabase.CreateAsset(recipe, path);
                else UnityEngine.Object.DestroyImmediate(recipe);
            }
        }

        private static void SetLayer(SerializedProperty layers, int index, string name)
        {
            if (layers == null || layers.arraySize <= index) return;
            string existing = layers.GetArrayElementAtIndex(index).stringValue;
            if (string.IsNullOrEmpty(existing) || existing == name)
                layers.GetArrayElementAtIndex(index).stringValue = name;
            else if (LayerMask.NameToLayer(name) < 0)
                Debug.LogWarning("Layer " + index + " already belongs to " + existing + "; preserving it.");
        }

        private static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CoastalCamp";
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, -1f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.10f, .16f, .20f);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100f;
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = Vector3.up;
            camera.GetUniversalAdditionalCameraData().SetRenderer(0);

            var ambient = new GameObject("Coastal daylight", typeof(Light2D));
            var light = ambient.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = new Color(.87f, .94f, 1f);
            light.intensity = .92f;

            Type sessionType = FindType("MistIsle.Runtime.GameSession");
            if (sessionType == null || !typeof(MonoBehaviour).IsAssignableFrom(sessionType))
                throw new InvalidOperationException("MistIsle.Runtime.GameSession must compile before building the playable scene.");
            new GameObject("Mist Isle — Chapter One").AddComponent(sessionType);
            if (!EditorSceneManager.SaveScene(scene, MainScene))
                throw new IOException("Could not save playable scene: " + MainScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };
            SceneManager.SetActiveScene(scene);
        }

        [MenuItem("Mist Isle/Build Windows Playable")]
        public static void BuildWindows()
        {
            if (!File.Exists(MainScene)) BuildContent();
            string path = Environment.GetEnvironmentVariable("MIST_ISLE_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(path)) path = "Builds/Windows/MistIsle.exe";
            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            Directory.CreateDirectory(directory);
            var options = new BuildPlayerOptions {
                scenes = new[] { MainScene },
                locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("MIST_ISLE_BUILD_RESULT: " + report.summary.result + ", " + report.summary.totalErrors + " errors, " + report.summary.totalSize + " bytes");
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
        }

        [MenuItem("Mist Isle/Play Opening")]
        public static void PlayOpening()
        {
            EditorSceneManager.OpenScene(MainScene);
            EditorApplication.isPlaying = true;
        }

        private static Type FindType(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name, false);
                if (type != null) return type;
            }
            return null;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string name = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
