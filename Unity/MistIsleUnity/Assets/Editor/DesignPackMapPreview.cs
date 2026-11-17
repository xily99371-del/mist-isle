#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using MistIsle.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace MistIsle.Editor
{
    /// <summary>Offscreen map inspection; deliberately makes no claims about game UI or manual play.</summary>
    public static class DesignPackMapPreview
    {
        [MenuItem("Mist Isle/Preview Design Pack Maps (World Only)")]
        public static void Capture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Map preview requires Edit Mode.");
            var original = SceneManager.GetActiveScene(); Scene scene = default;
            var previousTiles = new HashSet<Tile>(Resources.FindObjectsOfTypeAll<Tile>());
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Captures", "DesignPack")); Directory.CreateDirectory(directory);
            try {
                scene = EditorSceneManager.NewPreviewScene();
                var root = new GameObject("DesignPackWorldOnlyPreview"); SceneManager.MoveGameObjectToScene(root, scene);
                var layout = DesignPackWorldBuilder.Build(root.transform, (_, __) => { });
                var daylight = new GameObject("PreviewDaylight", typeof(Light2D)); SceneManager.MoveGameObjectToScene(daylight, scene);
                daylight.GetComponent<Light2D>().lightType = Light2D.LightType.Global; daylight.GetComponent<Light2D>().intensity = 1f;
                var camera = new GameObject("MapPreviewCamera", typeof(Camera)).GetComponent<Camera>(); SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
                camera.scene = scene; camera.enabled = false; camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f, .16f, .21f);
                camera.transparencySortMode = TransparencySortMode.CustomAxis; camera.transparencySortAxis = Vector3.up;
                camera.GetUniversalAdditionalCameraData().SetRenderer(0);
                foreach (var map in DesignPackCatalog.Maps) {
                    camera.orthographicSize = map.Height * .5f; camera.aspect = (float)map.Width / map.Height;
                    camera.transform.position = new Vector3(map.Bounds.center.x, map.Bounds.center.y, -10f);
                    Save(camera, map.Width * 16, map.Height * 16, Path.Combine(directory, map.Id + "-world-only.png"));
                }
                foreach (string landmark in new[] { "campfire-approach", "shiguang-approach", "tutorial-viewpoint" }) {
                    Vector2 point = layout.GetLandmarkPosition(landmark); camera.orthographicSize = 5.6f; camera.aspect = 16f / 9f;
                    camera.transform.position = new Vector3(point.x, point.y, -10f);
                    Save(camera, 1600, 900, Path.Combine(directory, landmark + "-player-world-only.png"));
                }
                File.WriteAllText(Path.Combine(directory, "capture-mode.txt"), "WORLD ONLY: Camera.Render in isolated temporary preview scene. No HUD, session progression or keyboard verification.\n");
                Debug.Log("MIST_ISLE_DESIGN_PACK_PREVIEWS: " + directory);
            }
            finally {
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.ClosePreviewScene(scene);
                foreach (var tile in Resources.FindObjectsOfTypeAll<Tile>()) if (tile != null && !previousTiles.Contains(tile) && !AssetDatabase.Contains(tile)) UnityEngine.Object.DestroyImmediate(tile);
            }
        }
        private static void Save(Camera camera, int width, int height, string path)
        {
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active; var previousTarget = camera.targetTexture;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { camera.targetTexture = previousTarget; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
#endif
