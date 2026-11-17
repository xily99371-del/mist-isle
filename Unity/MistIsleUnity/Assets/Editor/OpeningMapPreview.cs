#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MistIsle.World;
using MistIsle.Player;
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
                traveler.GetComponentInChildren<SpriteRenderer>().sortingOrder=Mathf.RoundToInt(-traveler.position.y*100f);
                SetActorDepth(world);
                SaveRoom(camera,world.GetBounds("camp-0"),Path.Combine(directory,"OpeningMap-camp0-world-only.png"));
                traveler.position=world.GetLandmarkPosition("starter-meeting")+Vector2.down*.8f;
                SetTravelerDepth(traveler);
                SavePlayerView(camera,world.GetBounds("camp-0"),world.GetLandmarkPosition("shiguang-approach"),
                    Path.Combine(directory,"OpeningMap-courtyard-player-world-only.png"));
                traveler.position=world.GetLandmarkPosition("campfire-approach");
                SetTravelerDepth(traveler);
                SavePlayerView(camera,world.GetBounds("camp-0"),traveler.position,
                    Path.Combine(directory,"OpeningMap-hearth-player-world-only.png"));

                world.MoveActorFacing("shiguang",world.GetLandmarkPosition("tutorial-viewpoint"),Vector2.down);
                traveler.position=world.GetLandmarkPosition("tutorial-viewpoint")+Vector2.left*1.2f;
                traveler.GetComponentInChildren<SpriteRenderer>().sortingOrder=Mathf.RoundToInt(-traveler.position.y*100f);
                SetActorDepth(world);
                SaveRoom(camera,world.GetBounds("coast-0"),Path.Combine(directory,"OpeningMap-coast0-world-only.png"));
                SavePlayerView(camera,world.GetBounds("coast-0"),world.GetLandmarkPosition("tutorial-viewpoint"),
                    Path.Combine(directory,"OpeningMap-cove-player-world-only.png"));
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
            var traveler=new GameObject("PreviewTraveler");
            traveler.transform.SetParent(parent,false);
            var sprite=new GameObject("Sprite",typeof(SpriteRenderer));
            sprite.transform.SetParent(traveler.transform,false);
            var renderer=sprite.GetComponent<SpriteRenderer>();
            renderer.sprite=frames[0];
            renderer.sharedMaterial=Resources.Load<Material>("Settings/MistIsleSpriteLit");
            ActorVisualSizing.ApplyHuman(renderer,frames[0]);
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
            camera.orthographicSize=10.4f;
            camera.aspect=1.6f;
            camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-10f);
            SaveFrame(camera,1600,1000,path);
        }

        private static void SetTravelerDepth(Transform traveler)
            => traveler.GetComponentInChildren<SpriteRenderer>().sortingOrder=Mathf.RoundToInt(-traveler.position.y*100f);

        private static void SavePlayerView(Camera camera,Rect bounds,Vector2 focus,string path)
        {
            camera.orthographicSize=5.6f;
            camera.aspect=16f/9f;
            float halfWidth=camera.orthographicSize*camera.aspect;
            camera.transform.position=new Vector3(Mathf.Clamp(focus.x,bounds.xMin+halfWidth,bounds.xMax-halfWidth),
                Mathf.Clamp(focus.y,bounds.yMin+camera.orthographicSize,bounds.yMax-camera.orthographicSize),-10f);
            SaveFrame(camera,1600,900,path);
        }

        private static void SaveFrame(Camera camera,int width,int height,string path)
        {
            var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var previousActive=RenderTexture.active;
            var previousTarget=camera.targetTexture;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;
                camera.Render();
                RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,width,height),0,0);
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
