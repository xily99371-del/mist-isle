using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MistIsle.World
{
    /// <summary>Applies the physical world change owned by an authored field gate, independently of dialogue and rewards.</summary>
    [ExecuteAlways]
    public sealed class DesignPackFieldMechanism : MonoBehaviour
    {
        private readonly List<CellState> cells = new List<CellState>();
        private Transform mapRoot;
        private Tilemap water;
        private Tilemap collision;
        private Tilemap ground;
        private Tilemap deck;
        private TileBase passage;
        private bool configured;
        private bool quitting;
        public string GateId { get; private set; }
        public bool IsOpen { get; private set; }
        public int ControlledWaterCells => cells.Count;
        public Rect LocalFootprint { get; private set; }

        public void Configure(DesignPackNode node, Transform ownerMap, TileBase passageTile)
        {
            if (configured) throw new InvalidOperationException("A field mechanism may only be configured once.");
            if (node == null || ownerMap == null) throw new ArgumentNullException(node == null ? nameof(node) : nameof(ownerMap));
            GateId = node.Id; mapRoot = ownerMap; passage = passageTile;
            water = mapRoot.Find("Water").GetComponent<Tilemap>();
            collision = mapRoot.Find("Collision").GetComponent<Tilemap>();
            ground = mapRoot.Find("Ground").GetComponent<Tilemap>();
            deck = mapRoot.Find("Objects").GetComponent<Tilemap>();
            Vector2 centre = node.Position - DesignPackCatalog.GetMap(node.MapId).Origin;
            LocalFootprint = new Rect(centre - node.Footprint * .5f, node.Footprint);
            if (GateId == "M02_ROOT_BRIDGE" || GateId == "M03_WATER_CHANNEL")
            {
                Physics2D.SyncTransforms();
                var tileCollider = collision.GetComponent<TilemapCollider2D>();
                var composite = collision.GetComponent<CompositeCollider2D>();
                for (int y = Mathf.FloorToInt(LocalFootprint.yMin); y < Mathf.CeilToInt(LocalFootprint.yMax); y++)
                    for (int x = Mathf.FloorToInt(LocalFootprint.xMin); x < Mathf.CeilToInt(LocalFootprint.xMax); x++)
                    {
                        var cell = new Vector3Int(x, y, 0);
                        Vector2 localCentre = new Vector2(x + .5f, y + .5f);
                        if (!LocalFootprint.Contains(localCentre) || !water.HasTile(cell)) continue;
                        // Drain only actual water cells, never a building/trunk/other independent footprint.
                        bool occupied = false;
                        foreach (var body in Physics2D.OverlapBoxAll(water.GetCellCenterWorld(cell), new Vector2(.94f, .94f), 0f))
                            if (!body.isTrigger && body != tileCollider && body != composite && body.gameObject != gameObject && body.transform.IsChildOf(mapRoot))
                            { occupied = true; break; }
                        if (occupied) continue;
                        cells.Add(new CellState { Cell = cell, Water = water.GetTile(cell), Collision = collision.GetTile(cell),
                            Ground = ground.GetTile(cell), Deck = deck.GetTile(cell) });
                    }
            }
            configured = true;
        }

        private void OnEnable()
        {
            if (configured && !quitting) Apply(false);
        }

        private void OnDisable()
        {
            // A gate opens by disabling its own barrier. Unloading a map or destroying the world is not a puzzle action.
            if (!configured || quitting || gameObject.activeSelf || mapRoot == null || !mapRoot.gameObject.activeInHierarchy) return;
            Apply(true);
        }

        private void OnApplicationQuit() { quitting = true; }

        private void Apply(bool open)
        {
            if (IsOpen == open || mapRoot == null) return;
            foreach (var state in cells)
            {
                water.SetTile(state.Cell, open ? null : state.Water);
                collision.SetTile(state.Cell, open ? null : state.Collision);
                if (GateId == "M02_ROOT_BRIDGE") deck.SetTile(state.Cell, open ? passage : state.Deck);
                else ground.SetTile(state.Cell, open ? passage : state.Ground);
            }
            IsOpen = open;
            if (cells.Count > 0)
            {
                collision.GetComponent<TilemapCollider2D>()?.ProcessTilemapChanges();
                collision.GetComponent<CompositeCollider2D>()?.GenerateGeometry();
                Physics2D.SyncTransforms();
            }
            var atmosphere = mapRoot.GetComponentInChildren<DesignPackAtmosphere>(true);
            if (GateId == "M03_CONDUIT") atmosphere?.SetConduitPowered(open);
            if (cells.Count > 0) atmosphere?.RefreshWaterBanks();
        }

        private sealed class CellState
        {
            public Vector3Int Cell;
            public TileBase Water, Collision, Ground, Deck;
        }
    }
}
