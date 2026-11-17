#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MistIsle.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MistIsle.Editor
{
    /// <summary>World-art inspection only. This does not run a session or verify any UI.</summary>
    public static class OpeningMapPreview
    {
        // Explicit batch entry for an isolated QA copy, keeping backups of derived artwork.
        public static void CaptureWithRefreshedOriginalArt()
        {
            ApprovedArtImporter.RebuildOriginalPixelActorsWithBackup();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Capture();
        }

        [MenuItem("Mist Isle/Preview Opening Maps (World Only)")]
        public static void Capture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("OpeningMapPreview requires edit mode; it never interrupts an active play session.");
            Scene original = SceneManager.GetActiveScene();
            Scene preview = default;
            var previousTiles = new HashSet<UnityEngine.Tilemaps.Tile>(Resources.FindObjectsOfTypeAll<UnityEngine.Tilemaps.Tile>());
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath,"..","Captures"));
            Directory.CreateDirectory(directory);
            try
            {
                // Preview scenes are isolated even when the user's active scene is unsaved.
                // They cannot be made active, so move the new root in before building its
                // children; parenting places all newly created world objects in this scene.
                preview = EditorSceneManager.NewPreviewScene();
                var worldRoot = new GameObject("OpeningMapWorldOnlyPreview");
                SceneManager.MoveGameObjectToScene(worldRoot,preview);
                var world = CoastalWorldBuilder.Build(worldRoot.transform,(_,__)=>{});
                // Reuse any already loaded global daylight to avoid creating duplicate URP lights.
                if (!UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None)
                    .Any(light=>light.enabled&&light.lightType==Light2D.LightType.Global))
                {
                    var ambient = new GameObject("PreviewDaylight",typeof(Light2D)).GetComponent<Light2D>();
                    SceneManager.MoveGameObjectToScene(ambient.gameObject,preview);
                    ambient.lightType=Light2D.LightType.Global;
                    ambient.color=new Color(.87f,.94f,1f);
                    ambient.intensity=.92f;
                }
                var camera = new GameObject("WorldOnlyPreviewCamera",typeof(Camera)).GetComponent<Camera>();
                SceneManager.MoveGameObjectToScene(camera.gameObject,preview);
                camera.enabled=false;
                camera.orthographic=true;
                camera.orthographicSize=10.4f;
                camera.aspect=1.6f;
                camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.09f,.15f,.19f);
                camera.nearClipPlane=.1f;
                camera.farClipPlane=100f;
                camera.transparencySortMode=TransparencySortMode.CustomAxis;
                camera.transparencySortAxis=Vector3.up;
                camera.scene=preview;
                camera.GetUniversalAdditionalCameraData().SetRenderer(0);

                var traveler = CreatePreviewTraveler(worldRoot.transform);
                traveler.position=world.GetSpawn("camp-0");
                traveler.GetComponent<SpriteRenderer>().sortingOrder=Mathf.RoundToInt(-traveler.position.y*100f);
                SetActorDepth(world);
                SaveRoom(camera,world.GetBounds("camp-0"),Path.Combine(directory,"OpeningMap-camp0-world-only.png"));

                world.MoveActorFacing("shiguang",world.GetLandmarkPosition("tutorial-viewpoint"),Vector2.down);
                traveler.position=world.GetLandmarkPosition("tutorial-viewpoint")+Vector2.left*1.2f;
                traveler.GetComponent<SpriteRenderer>().sortingOrder=Mathf.RoundToInt(-traveler.position.y*100f);
                SetActorDepth(world);
                SaveRoom(camera,world.GetBounds("coast-0"),Path.Combine(directory,"OpeningMap-coast0-world-only.png"));
                File.WriteAllText(Path.Combine(directory,"OpeningMap-preview-kind.txt"),
                    "WORLD ONLY. Isolated temporary preview scene rendered through Camera.Render to a RenderTexture.\n"+
                    "No GameSession, no IMGUI/HUD, no story progression or real keyboard interaction was tested.\n"+
                    "Coast arrangement places the guide and traveler near the tutorial viewpoint solely for visual scale inspection.\n");
                Debug.Log("MIST_ISLE_OPENING_WORLD_PREVIEWS: "+directory);
            }
            finally
            {
                if (original.IsValid()&&original.isLoaded) SceneManager.SetActiveScene(original);
                if (preview.IsValid()&&preview.isLoaded) EditorSceneManager.ClosePreviewScene(preview);
                foreach(var tile in Resources.FindObjectsOfTypeAll<UnityEngine.Tilemaps.Tile>())
                    if(tile!=null&&!previousTiles.Contains(tile)&&!AssetDatabase.Contains(tile)) UnityEngine.Object.DestroyImmediate(tile);
            }
        }

        private static Transform CreatePreviewTraveler(Transform parent)
        {
            var frames=Resources.LoadAll<Sprite>("Art/Actors/ApprovedTraveler");
            Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));
            if(frames.Length==0) throw new InvalidOperationException("Original approved traveler frames have not been imported.");
            var traveler=new GameObject("PreviewTraveler",typeof(SpriteRenderer));
            traveler.transform.SetParent(parent,false);
            var renderer=traveler.GetComponent<SpriteRenderer>();
            renderer.sprite=frames[0];
            renderer.sharedMaterial=Resources.Load<Material>("Settings/MistIsleSpriteLit");
            traveler.transform.localScale=Vector3.one*(1.8f/Mathf.Max(.01f,frames[0].bounds.size.y));
            return traveler.transform;
        }

        private static void SetActorDepth(CoastalWorldLayout world)
        {
            foreach(string id in new[] {"watcher","shiguang","moss-lumi","moatail","crystal-gecko","tutorial-coast-crab"})
            {
                Transform actor=world.GetActor(id);
                if(actor==null) continue;
                foreach(var renderer in actor.GetComponentsInChildren<SpriteRenderer>())
                    renderer.sortingOrder=Mathf.RoundToInt(-actor.position.y*100f);
            }
        }

        private static void SaveRoom(Camera camera,Rect bounds,string path)
        {
            camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-10f);
            var target=RenderTexture.GetTemporary(1600,1000,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var previousActive=RenderTexture.active;
            var previousTarget=camera.targetTexture;
            var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;
                camera.Render();
                RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1600,1000),0,0);
                texture.Apply();
                File.WriteAllBytes(path,texture.EncodeToPNG());
                Debug.Log("MIST_ISLE_WORLD_ONLY_CAPTURE: "+path);
            }
            finally
            {
                camera.targetTexture=previousTarget;
                RenderTexture.active=previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
#endif
