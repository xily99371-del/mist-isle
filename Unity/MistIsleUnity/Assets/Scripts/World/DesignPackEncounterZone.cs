using System;
using System.Collections.Generic;
using System.Globalization;
using MistIsle.Runtime;
using UnityEngine;

namespace MistIsle.World
{
    /// <summary>Rolls only after actual player travel inside an authored wild-grass rectangle.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class DesignPackEncounterZone : MonoBehaviour
    {
        private GameSession session;
        private DesignPackNode node;
        private EncounterZoneSettings settings;
        private Func<float> nextRandom;
        private Rigidbody2D playerBody;
        private readonly HashSet<Collider2D> contacts = new HashSet<Collider2D>();
        private Rect worldBounds;
        private Vector2 previousPosition;
        private float distanceSinceRoll;
        private float nextRollAt;
        private bool hasSample;
        private bool returningFromEncounter;

        public string NodeId => node == null ? "" : node.Id;
        public Rect WorldBounds => worldBounds;

        public static DesignPackEncounterZone[] Install(GameSession owner, Transform parent)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            var result = new List<DesignPackEncounterZone>();
            foreach (var map in DesignPackCatalog.Maps)
                foreach (var candidate in map.Nodes)
                {
                    if (!TryGetWorldBounds(candidate, out _)) continue;
                    DesignPackEncounterZone found = null;
                    foreach (var existing in parent.GetComponentsInChildren<DesignPackEncounterZone>(true))
                        if (existing.session == owner && existing.NodeId == candidate.Id) { found = existing; break; }
                    if (found == null)
                    {
                        var host = new GameObject("WildGrass — " + candidate.SourceId);
                        host.transform.SetParent(parent, false);
                        found = host.AddComponent<DesignPackEncounterZone>();
                        found.Configure(owner, candidate);
                    }
                    result.Add(found);
                }
            return result.ToArray();
        }

        public void Configure(GameSession owner, DesignPackNode source)
            => Configure(owner, source, new EncounterZoneSettings(), null);

        public void Configure(GameSession owner, DesignPackNode source, EncounterZoneSettings tuning, Func<float> randomSample)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (!TryGetWorldBounds(source, out Rect bounds))
                throw new ArgumentException("An encounter node with an authored rectangle is required.", nameof(source));
            session = owner; node = source; settings = tuning;
            nextRandom = randomSample ?? (() => UnityEngine.Random.value);
            worldBounds = bounds;
            transform.position = bounds.center;
            var shape = GetComponent<BoxCollider2D>();
            shape.isTrigger = true; shape.offset = Vector2.zero; shape.size = bounds.size;
            contacts.Clear(); playerBody = null; hasSample = false; distanceSinceRoll = 0f;
            returningFromEncounter = false;
            nextRollAt = Time.unscaledTime + settings.EntryGraceSeconds;
        }

        public static bool TryGetWorldBounds(DesignPackNode candidate, out Rect bounds)
        {
            bounds = Rect.zero;
            if (candidate == null || candidate.Kind != "encounter" || candidate.Id == "tutorial-coast-crab"
                || candidate.EncounterSpeciesIds == null || candidate.EncounterSpeciesIds.Length == 0
                || candidate.Raw == null || !candidate.Raw.TryGetValue("rect", out object source)
                || !(source is IList<object> rect) || rect.Count != 4) return false;
            float x = Convert.ToSingle(rect[0], CultureInfo.InvariantCulture);
            float y = Convert.ToSingle(rect[1], CultureInfo.InvariantCulture);
            float width = Convert.ToSingle(rect[2], CultureInfo.InvariantCulture);
            float height = Convert.ToSingle(rect[3], CultureInfo.InvariantCulture);
            if (!Finite(x) || !Finite(y) || !Finite(width) || !Finite(height) || width <= 0f || height <= 0f) return false;
            bounds = new Rect(DesignPackCatalog.GetMap(candidate.MapId).Origin + new Vector2(x, y), new Vector2(width, height));
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (session == null || session.PlayerTransform == null) return;
            var body = session.PlayerTransform.GetComponent<Rigidbody2D>();
            if (body == null || other.attachedRigidbody != body) return;
            bool firstContact = contacts.Count == 0;
            contacts.Add(other); playerBody = body;
            if (!firstContact) return;
            ResetWalking(body.position);
            nextRollAt = Mathf.Max(nextRollAt, Time.unscaledTime + settings.EntryGraceSeconds);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            contacts.Remove(other);
            if (contacts.Count != 0) return;
            playerBody = null; hasSample = false; distanceSinceRoll = 0f;
            // Keep nextRollAt: repeatedly leaving and re-entering grass cannot bypass its roll interval.
        }

        private void OnDisable()
        {
            contacts.Clear(); playerBody = null; hasSample = false; distanceSinceRoll = 0f;
        }

        private void FixedUpdate()
        {
            if (session == null || node == null || playerBody == null || contacts.Count == 0) return;
            Vector2 point = playerBody.position;
            if (session.Panel == GamePanel.Battle) returningFromEncounter = true;
            if (!CanRequestEncounter() || !worldBounds.Contains(point)) { ResetWalking(point); return; }
            if (returningFromEncounter)
            {
                returningFromEncounter = false;
                nextRollAt = Mathf.Max(nextRollAt, Time.unscaledTime + settings.AfterEncounterGrace);
                ResetWalking(point);
                return;
            }
            if (!hasSample) { ResetWalking(point); return; }
            float traveled = Vector2.Distance(previousPosition, point);
            previousPosition = point;
            if (traveled > settings.TeleportDistance) { distanceSinceRoll = 0f; return; }
            if (traveled < .0025f) return; // Idle body/contact jitter must not become walking.
            distanceSinceRoll = Mathf.Min(settings.StepDistance, distanceSinceRoll + traveled);
            if (distanceSinceRoll < settings.StepDistance || Time.unscaledTime < nextRollAt) return;

            distanceSinceRoll = 0f;
            nextRollAt = Time.unscaledTime + settings.MinimumRollInterval;
            if (settings.ChancePerRoll <= 0f) return;
            float sample = nextRandom();
            if (settings.ChancePerRoll < 1f && (!Finite(sample) || sample >= settings.ChancePerRoll)) return;
            session.HandleWorldAction("encounter", node.Id);
            // An escape still grants breathing room before another walking encounter can start.
            if (session.Panel == GamePanel.Battle)
            {
                returningFromEncounter = true;
                nextRollAt = Mathf.Max(nextRollAt, Time.unscaledTime + settings.AfterEncounterGrace);
            }
        }

        private bool CanRequestEncounter()
        {
            if (session.Panel != GamePanel.Explore || session.OpeningPhase != OpeningStage.Complete
                || session.Progress.roomId != node.RoomId || !session.NodeAvailable(node.Id)
                || session.Party == null || session.Catalog == null) return false;
            bool hasHealthyPartner = false;
            foreach (var partner in session.Party.Members) if (partner.currentHp > 0) { hasHealthyPartner = true; break; }
            if (!hasHealthyPartner) return false;
            foreach (string id in node.EncounterSpeciesIds)
                if (session.Catalog.TryGetValue(id, out var species) && species.contentReady) return true;
            return false;
        }

        private void ResetWalking(Vector2 point)
        { previousPosition = point; hasSample = true; distanceSinceRoll = 0f; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>Per-region tuning can be supplied without changing the source map JSON.</summary>
    public sealed class EncounterZoneSettings
    {
        public float StepDistance { get; }
        public float ChancePerRoll { get; }
        public float MinimumRollInterval { get; }
        public float EntryGraceSeconds { get; }
        public float TeleportDistance { get; }
        public float AfterEncounterGrace { get; }

        public EncounterZoneSettings(float stepDistance = 3f, float chancePerRoll = .18f, float minimumRollInterval = 4.5f,
            float entryGraceSeconds = .8f, float teleportDistance = 1.5f, float afterEncounterGrace = 8f)
        {
            if (!Valid(stepDistance) || stepDistance <= 0f) throw new ArgumentOutOfRangeException(nameof(stepDistance));
            if (!Valid(chancePerRoll) || chancePerRoll < 0f || chancePerRoll > 1f) throw new ArgumentOutOfRangeException(nameof(chancePerRoll));
            if (!Valid(minimumRollInterval) || minimumRollInterval < 0f) throw new ArgumentOutOfRangeException(nameof(minimumRollInterval));
            if (!Valid(entryGraceSeconds) || entryGraceSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(entryGraceSeconds));
            if (!Valid(teleportDistance) || teleportDistance <= 0f) throw new ArgumentOutOfRangeException(nameof(teleportDistance));
            if (!Valid(afterEncounterGrace) || afterEncounterGrace < 0f) throw new ArgumentOutOfRangeException(nameof(afterEncounterGrace));
            StepDistance = stepDistance; ChancePerRoll = chancePerRoll; MinimumRollInterval = minimumRollInterval;
            EntryGraceSeconds = entryGraceSeconds; TeleportDistance = teleportDistance; AfterEncounterGrace = afterEncounterGrace;
        }
        private static bool Valid(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
