using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MistIsle.World
{
    /// <summary>Small authored changes at the ancient grove, restored from the earned Forest Seal.</summary>
    [DisallowMultipleComponent]
    public sealed class DesignPackWorldConsequences : MonoBehaviour
    {
        private const string HostName = "Forest recovery";
        private GameObject restoredGrove;
        public bool ForestRestored { get; private set; }

        public static DesignPackWorldConsequences Attach(Transform forestMapRoot)
        {
            if (forestMapRoot == null) throw new ArgumentNullException(nameof(forestMapRoot));
            var existing = forestMapRoot.Find(HostName);
            if (existing != null) return existing.GetComponent<DesignPackWorldConsequences>();
            var host = new GameObject(HostName);
            host.transform.SetParent(forestMapRoot, false);
            var result = host.AddComponent<DesignPackWorldConsequences>();
            result.BuildGrove();
            result.ApplyForestRecovery(false);
            return result;
        }

        private void BuildGrove()
        {
            restoredGrove = new GameObject("Living roots and returning seedlings");
            restoredGrove.transform.SetParent(transform, false);
            // These stay beside the supplied approach and root-bridge route. They add no collider or new boundary.
            var seedlings = new[] { new Vector2(42.4f, 42.8f), new Vector2(44.4f, 44.1f), new Vector2(49.7f, 43.8f), new Vector2(51.7f, 41.8f) };
            foreach (var point in seedlings)
            {
                AddProp(restoredGrove.transform, "Returning seedling", "Tree", point, 1.2f, Color.white);
                AddProp(restoredGrove.transform, "New forest flowers", "FlowerBlue", point + new Vector2(.6f, -.3f), .65f, Color.white);
            }
            foreach (var point in new[] { new Vector2(46f, 43.2f), new Vector2(48.5f, 41.1f), new Vector2(52.7f, 38.8f), new Vector2(56.7f, 37.9f) })
                AddProp(restoredGrove.transform, "Root bloom", "FlowerWhite", point, .7f, new Color(.75f, 1f, .77f));
            AddRootLight(new Vector2(47f, 42f), 4.4f);
            AddRootLight(new Vector2(56f, 38f), 3.8f);
        }

        private void AddRootLight(Vector2 point, float radius)
        {
            var host = new GameObject("Restored root glow");
            host.transform.SetParent(restoredGrove.transform, false);
            host.transform.localPosition = point;
            var light = host.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(.57f, 1f, .65f);
            light.intensity = .22f;
            light.pointLightInnerRadius = .4f;
            light.pointLightOuterRadius = radius;
        }

        public void ApplyForestRecovery(bool restored)
        {
            ForestRestored = restored;
            if (restoredGrove != null) restoredGrove.SetActive(restored);
        }

        /// <summary>Call on the actual interactable transform, so the record is visible at its reading point.</summary>
        public static void DecorateRecord(Transform interactionRoot, DesignPackNode node)
        {
            if (interactionRoot == null || node == null || !node.IsNarrativeRecord) return;
            if (interactionRoot.Find("Readable world record") != null) return;
            bool sign = node.SourceId == "M04_ASCENT";
            var record = new GameObject("Readable world record");
            record.transform.SetParent(interactionRoot, false);
            // StoneWall.png is a narrow ground strip in the existing import, not a readable upright tablet.
            AddProp(record.transform, sign ? "Old patrol sign" : "Island record board", "Signpost",
                Vector2.up * .12f, 1.6f, Color.white);
            if (!sign) AddProp(record.transform, "Record lamp", "Lantern", new Vector2(-1.4f, .15f), 1.4f, Color.white);
        }

        private static void AddProp(Transform parent, string name, string asset, Vector2 point, float height, Color color)
        {
            var sprite = Resources.Load<Sprite>("Art/Environment/" + asset);
            if (sprite == null) throw new InvalidOperationException("Missing approved environment sprite: " + asset);
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            float scale = height / Mathf.Max(.01f, sprite.bounds.size.y);
            host.transform.localScale = Vector3.one * scale;
            host.transform.localPosition = (Vector3)point - new Vector3(sprite.bounds.center.x * scale, sprite.bounds.min.y * scale, 0f);
            var renderer = host.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            var material = Resources.Load<Material>("Settings/MistIsleSpriteLit");
            if (material != null) renderer.sharedMaterial = material;
            renderer.sortingOrder = Mathf.RoundToInt(-(parent.position.y + point.y) * 100f);
        }
    }
}
