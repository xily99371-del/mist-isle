using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace MistIsle.World
{
    // Layout is authored here, while inventory, dialogue and progression live in the session.
    public static class CoastalWorldBuilder
    {
        public const float RoomWidth = 32f;
        public const float RoomHeight = 20f;
        private static readonly string[] Regions = { "camp", "coast", "glowglade", "tidal", "crystal", "lighthouse" };
        private static readonly Dictionary<string, Sprite> MarkerSprites = new Dictionary<string, Sprite>();
        private static readonly string[,] Names = {
            { "海岸营地 · 归航之火", "营地后坡 · 旧果园", "观潮栈桥 · 远海来信" },
            { "潮风海岸 · 漂流驿站", "贝壳浅湾 · 潮线小径", "风蚀石台 · 林地前哨" },
            { "辉光林地 · 萤火驿站", "古木回廊 · 藤蔓支路", "根系圣所 · 林地封印" },
            { "潮汐遗迹 · 避雨石庭", "沉水回廊 · 双桥水道", "潮心祭坛 · 潮汐封印" },
            { "鸣晶高地 · 勘探营地", "共鸣矿道 · 晶簇岔路", "星晶祭台 · 晶能封印" },
            { "灯塔山麓 · 最后的篝火", "静默阶庭 · 四印之门", "灯芯圣殿 · 雾海回响" }
        };

        public static CoastalWorldLayout Build(Transform parent, Action<string, string> onInteract,
            Func<string, bool> wasCollected = null)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            var previous = parent.Find("ChapterOneWorld");
            if (previous != null) throw new InvalidOperationException("World has already been built for this session.");
            var root = new GameObject("ChapterOneWorld").transform;
            root.SetParent(parent, false);
            var art = new WorldArt();
            var layout = new CoastalWorldLayout();
            for (int region = 0; region < Regions.Length; region++)
                for (int room = 0; room < 3; room++)
                {
                    string id = Regions[region] + "-" + room;
                    Vector2 center = new Vector2((region * 3 + room) * 48f, 0f);
                    var roomRoot = new GameObject(id + " — " + Names[region, room]).transform;
                    roomRoot.SetParent(root, false);
                    roomRoot.position = center;
                    Vector2 arrival = region == 0 && room == 0 ? new Vector2(-10.5f,-6.5f) :
                        region == 1 && room == 0 ? new Vector2(-13f,1.2f) : new Vector2(0f,-5.5f);
                    layout.AddRoom(id, Names[region, room], center + arrival,
                        new Rect(center.x - 16f, -10f, RoomWidth, RoomHeight));
                    BuildGround(roomRoot, art, region, room);
                    BuildBoundaries(roomRoot);
                    BuildLandmarks(roomRoot, art, region, room);
                    Populate(roomRoot, art, layout, region, room, onInteract, wasCollected);
                }
            ConnectRooms(root, art, layout, onInteract);
            layout.Spawn = layout.GetSpawn("camp-0");
            layout.CameraBounds = layout.GetBounds("camp-0");
            return layout;
        }

        private static void BuildGround(Transform parent, WorldArt art, int region, int room)
        {
            if (room == 0 && region <= 1)
            {
                BuildOpeningGround(parent, art, region == 0);
                return;
            }
            var gridObject = new GameObject("GroundGrid", typeof(Grid));
            gridObject.transform.SetParent(parent, false);
            var groundObject = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
            groundObject.transform.SetParent(gridObject.transform, false);
            var map = groundObject.GetComponent<Tilemap>();
            var groundRenderer = groundObject.GetComponent<TilemapRenderer>();
            groundRenderer.sortingOrder = -30000;
            if (art.LitMaterial != null) groundRenderer.sharedMaterial = art.LitMaterial;
            map.tileAnchor = new Vector3(.5f, 0f, 0f);
            Color[] tints = { new Color(.76f,.88f,.67f), new Color(.97f,.89f,.68f), new Color(.54f,.77f,.62f),
                new Color(.61f,.78f,.80f), new Color(.73f,.69f,.84f), new Color(.61f,.68f,.78f) };
            var floor = MakeTile(art.Floor(region), tints[region]);
            var path = MakeTile(art.Floor(region == 0 || region == 2 ? 1 : 4), Color.white);
            var shore = MakeTile(art.Ground(36), new Color(.70f,.86f,.95f));
            for (int x = -16; x < 16; x++)
                for (int y = -10; y < 10; y++)
                {
                    bool border = x == -16 || x == 15 || y == -10 || y == 9;
                    // Every room has a readable central route, with a different side branch.
                    int branchY = room == 0 ? -3 : room == 1 ? 3 : 1;
                    bool trail = Math.Abs(y) <= 1 || (Math.Abs(x) <= 1 && y >= -6 && y <= 5) ||
                        (y == branchY && x >= -10 && x <= 10);
                    map.SetTile(new Vector3Int(x, y, 0), border ? shore : trail ? path : floor);
                }
            if (region == 3 && room == 1)
            {
                // Two water channels with two real crossings; bank collision leaves bridges open.
                for (int x = -12; x <= 12; x++)
                    if ((x < -6 || x > -3) && (x < 8 || x > 10))
                        for (int y = 3; y <= 4; y++) map.SetTile(new Vector3Int(x, y, 0), shore);
                AddBox(parent, "NorthCanalWest", new Vector2(-10f, 4f), new Vector2(5f, 1.6f));
                AddBox(parent, "NorthCanalMiddle", new Vector2(2.5f, 4f), new Vector2(10f, 1.6f));
                AddBox(parent, "NorthCanalEast", new Vector2(11.75f, 4f), new Vector2(1.5f, 1.6f));
            }
        }

        private static void BuildOpeningGround(Transform parent, WorldArt art, bool camp)
        {
            var sea = CreateMap(parent,"BayWater",-30020,art.LitMaterial);
            var substrate = CreateMap(parent,"CoastalSubstrate",-30015,art.LitMaterial);
            var land = CreateMap(parent,"CoastalGround",-30010,art.LitMaterial);
            var trail = CreateMap(parent,"WornFootpaths",-30000,art.LitMaterial);
            trail.transform.localScale=new Vector3(.5f,.5f,1f);
            var water = new Tile[8];
            var solidWater = new Tile[8];
            var grass = new Tile[8];
            var sand = new Tile[8];
            var grassEdge = new Tile[4];
            var sandEdge = new Tile[4];
            var fineSand = new Tile[32];
            for (int i=0;i<8;i++)
            {
                water[i] = MakeTile(art.Ground(36+i),Color.white);
                solidWater[i] = MakeTile(art.Ground(36+i),Color.white);
                solidWater[i].colliderType = Tile.ColliderType.Grid;
                grass[i] = MakeTile(art.Ground(i),Color.white);
                sand[i] = MakeTile(art.Ground(12+i),Color.white);
                for(int quarter=0;quarter<4;quarter++) fineSand[i*4+quarter]=MakeFinePathTile(art.Ground(12+i),quarter);
            }
            for (int i=0;i<4;i++)
            {
                grassEdge[i] = MakeTile(art.Ground(8+i),Color.white);
                sandEdge[i] = MakeTile(art.Ground(20+i),Color.white);
            }
            for (int x=-16;x<16;x++)
                for (int y=-10;y<10;y++)
                {
                    int variant = ((x+48)*13+(y+24)*7)%8;
                    int shoreline = Shoreline(x,camp);
                    bool waterCell = y < shoreline;
                    bool dock = camp && x>=-12 && x<=-10 && y>=-9 && y<=-3;
                    sea.SetTile(new Vector3Int(x,y,0),waterCell&&!dock ? solidWater[variant] : water[variant]);
                    if (waterCell) continue;
                    if(y>shoreline) substrate.SetTile(new Vector3Int(x,y,0),sand[variant]);
                    Tile tile = y==shoreline ? sandEdge[(x+32)%4] : y<shoreline+3 ? sand[variant] :
                        y==shoreline+3 ? grassEdge[(x+32)%4] : grass[variant];
                    land.SetTile(new Vector3Int(x,y,0),tile);
                }
            var body = sea.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var composite = sea.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            var waterCollider = sea.gameObject.AddComponent<TilemapCollider2D>();
            waterCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            waterCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();

            Vector2[][] paths = camp ? new[] {
                new[] {new Vector2(-10.5f,-3.5f),new Vector2(-8.5f,-1.4f),new Vector2(-5f,-1f),new Vector2(-2f,-1.1f),new Vector2(1.5f,0f),new Vector2(5.6f,-.3f),new Vector2(9.3f,.7f),new Vector2(13.5f,1.3f)},
                new[] {new Vector2(-6.7f,-1f),new Vector2(-6.7f,2.8f)},
                new[] {new Vector2(1.5f,0f),new Vector2(.3f,3.8f)},
                new[] {new Vector2(4.2f,.1f),new Vector2(4.2f,2.5f),new Vector2(9.2f,2.5f)},
                new[] {new Vector2(5.3f,-.3f),new Vector2(5f,-3.2f),new Vector2(4f,-6.5f)},
                new[] {new Vector2(-8.5f,-1.4f),new Vector2(-12f,-1f)}
            } : new[] {
                new[] {new Vector2(-13.5f,1.2f),new Vector2(-9f,0f),new Vector2(-5f,1f),new Vector2(-2f,2.8f),new Vector2(2f,2.5f),new Vector2(5f,.8f),new Vector2(7f,-1.7f),new Vector2(10f,-.7f),new Vector2(13.5f,0f)},
                new[] {new Vector2(7f,-1.7f),new Vector2(7.5f,-3.2f)}
            };
            for (int x=-30;x<30;x++)
                for (int y=-18;y<18;y++)
                {
                    Vector2 cell = new Vector2((x+.5f)*.5f,(y+.5f)*.5f);
                    if (cell.y<Shoreline(Mathf.FloorToInt(cell.x),camp)) continue;
                    bool onTrail = camp && Vector2.Distance(cell,new Vector2(-1.6f,-1.2f))<2.25f;
                    foreach (var path in paths)
                        for (int i=1;i<path.Length&&!onTrail;i++)
                            onTrail = DistanceToSegment(cell,path[i-1],path[i]) < .70f;
                    if (onTrail) trail.SetTile(new Vector3Int(x,y,0),fineSand[((x+64)*3+(y+32)*5)%32]);
                }
        }

        private static Tile MakeFinePathTile(Sprite source,int quarter)
        {
            // Quarter-crops retain the original ground pixels at the same density. They allow
            // half-cell bends without shrinking a complete tile's texture or painting new art.
            Rect rect=source.rect;
            float width=rect.width*.5f,height=rect.height*.5f;
            var crop=Sprite.Create(source.texture,new Rect(rect.x+(quarter%2)*width,rect.y+(quarter/2)*height,width,height),
                new Vector2(.5f,0f),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
            return MakeTile(crop,Color.white);
        }

        private static Tilemap CreateMap(Transform parent,string name,int sorting,Material material)
        {
            var grid = parent.Find("OpeningGrid");
            if (grid==null)
            {
                grid=new GameObject("OpeningGrid",typeof(Grid)).transform;
                grid.SetParent(parent,false);
            }
            var obj=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));
            obj.transform.SetParent(grid,false);
            var renderer=obj.GetComponent<TilemapRenderer>();
            renderer.sortingOrder=sorting;
            if(material!=null) renderer.sharedMaterial=material;
            var map=obj.GetComponent<Tilemap>();
            map.tileAnchor=new Vector3(.5f,0f,0f);
            return map;
        }

        private static int Shoreline(int x,bool camp)
        {
            if(camp) return x < -13 ? -1 : x < -9 ? -4 : x < -5 ? -6 : x < 3 ? -7 : x < 10 ? -8 : -7;
            return x < -10 ? -5 : x < -4 ? -6 : x < 2 ? -5 : x < 7 ? -6 : x < 11 ? -5 : -4;
        }

        private static float DistanceToSegment(Vector2 point,Vector2 from,Vector2 to)
        {
            Vector2 delta=to-from;
            float t=Mathf.Clamp01(Vector2.Dot(point-from,delta)/Mathf.Max(.001f,delta.sqrMagnitude));
            return Vector2.Distance(point,from+delta*t);
        }

        private static void BuildOpeningLandmarks(Transform parent,WorldArt art,bool camp)
        {
            BuildNorthernForestEdge(parent,art,camp);
            if(camp)
            {
                BuildHouse(parent,art,new Vector2(-7f,3f),5.8f);
                Prop(parent,"SupplyTent",art.Tent,new Vector2(.6f,4.4f),4.4f);
                AddBox(parent,"TentFootprint",new Vector2(.6f,4.85f),new Vector2(3.2f,.85f));
                for(int i=0;i<5;i++)
                {
                    var deck=Prop(parent,"ArrivalDockDeck",art.Dock??art.Fence,new Vector2(-10.5f,-8.4f+i*1.1f),3f);
                    if(deck!=null) deck.GetComponent<SpriteRenderer>().sortingOrder=-29000;
                }
                Prop(parent,"MooredBoat",art.Boat,new Vector2(-13.3f,-7.9f),2.2f);
                Prop(parent,"ArrivalLantern",art.Lantern,new Vector2(-11.9f,-5.1f),.6f);
                Prop(parent,"DockRopeCrate",art.Crate,new Vector2(-9.4f,-3.2f),.8f);
                Prop(parent,"HouseSupplies",art.Crate,new Vector2(-9.1f,2.1f),.9f);
                Prop(parent,"WaterBarrel",art.Barrel,new Vector2(-4.9f,2.7f),.8f);
                Prop(parent,"WorkbenchCrate",art.Crate,new Vector2(-3.4f,-2.7f),1f);
                Prop(parent,"CookingBarrel",art.Barrel,new Vector2(-4.4f,-2.2f),.8f);
                Prop(parent,"WorkAreaLantern",art.Lantern,new Vector2(-3.4f,-.25f),.65f);
                for(int i=0;i<4;i++) OpeningTimberFence(parent,art,new Vector2(4.7f+i*2f,5.1f));
                OpeningTimberFence(parent,art,new Vector2(9.8f,-.2f));
                FenceColumn(parent,art,new Vector2(11.5f,3.8f),2.6f);
                Tree(parent,art,new Vector2(14f,3.8f),3.1f);
                Tree(parent,art,new Vector2(-14f,2.4f),3.1f);
                ScatterOpeningPlants(parent,art,new[] {new Vector2(-11f,2.2f),new Vector2(-3.7f,3.9f),
                    new Vector2(3.4f,5.6f),new Vector2(10.7f,5.6f),new Vector2(-7.6f,-4.5f),new Vector2(-5.6f,-5.3f),
                    new Vector2(1.8f,-5.8f),new Vector2(6.7f,-6.5f),new Vector2(11.3f,-5.7f),
                    new Vector2(-8.8f,.15f),new Vector2(-5.1f,-2.6f),new Vector2(3.4f,-1.7f),new Vector2(12f,2.8f)});
                Prop(parent,"CompanionGardenFlowers",art.FlowerWhite,new Vector2(5.8f,4.4f),1f);
                Prop(parent,"CompanionGardenBush",art.BushPink,new Vector2(10.4f,4.4f),1.4f);
                Prop(parent,"CampShoreRock",art.RockLarge,new Vector2(-13.4f,-2.7f),1.8f);
                Prop(parent,"CampTideStone",art.RockSmall,new Vector2(9.9f,-6.7f),1f);
                ScatterOpeningPlants(parent,art,new[] {new Vector2(-8.5f,-.05f),new Vector2(-7.8f,-2.4f),
                    new Vector2(.05f,-2.9f),new Vector2(2.8f,1.1f),new Vector2(4.5f,-1.5f),new Vector2(10.9f,2.0f)});
            }
            else
            {
                Tree(parent,art,new Vector2(13f,3.9f),3.1f);
                ScatterOpeningPlants(parent,art,new[] {new Vector2(-13f,3f),new Vector2(-9.4f,-1.8f),
                    new Vector2(-7.1f,3.2f),new Vector2(-4.7f,-2.5f),new Vector2(-3.5f,4.2f),
                    new Vector2(.5f,4.7f),new Vector2(2.8f,-2.4f),new Vector2(5.1f,3.2f),
                    new Vector2(8.3f,2.3f),new Vector2(12.1f,1.9f),new Vector2(-7f,-4.4f)});
                Vector2[] rocks={new Vector2(-12f,-3.8f),new Vector2(-6f,-4.8f),new Vector2(-1f,-4.1f),
                    new Vector2(3f,-4.9f),new Vector2(9.1f,-3.6f),new Vector2(12.7f,-3f)};
                for(int i=0;i<rocks.Length;i++)
                {
                    Prop(parent,"CoastalRock",i%2==0?art.RockLarge:art.RockSmall,rocks[i],i%2==0?1.4f:.9f);
                    AddBox(parent,"RockFootprint",rocks[i]+Vector2.up*.1f,new Vector2(.55f,.3f));
                }
                Prop(parent,"AbandonedSupplyCrate",art.Crate,new Vector2(-9f,2.1f),.9f);
                Prop(parent,"TrailLantern",art.Lantern,new Vector2(-10.1f,2.3f),.65f);
                Prop(parent,"CrabShelterRock",art.RockSmall,new Vector2(6.1f,-3.7f),1.1f);
                Prop(parent,"WashedUpTimber",art.Log??art.Dock,new Vector2(7.7f,-4.25f),2.1f);
                ScatterOpeningPlants(parent,art,new[] {new Vector2(-10.4f,1.2f),new Vector2(-6.4f,-.2f),
                    new Vector2(-3.1f,1.1f),new Vector2(-.5f,3.9f),new Vector2(3.6f,1.1f),
                    new Vector2(5.6f,-.7f),new Vector2(8.8f,.25f)});
            }
        }

        private static void BuildNorthernForestEdge(Transform parent,WorldArt art,bool camp)
        {
            float[] ledges=camp?new[] {6.3f,6.5f,6.9f,7.1f,6.7f,6.4f,6.3f,6.5f}:
                new[] {6.2f,6.4f,6.7f,6.8f,6.5f,6.3f,6.4f,6.2f};
            for(int i=0;i<16;i++)
            {
                float x=-15f+i*2f,ledge=ledges[i/2];
                Prop(parent,"NorthernGrassCliff",i%5==2?art.GrassCliffCorner:art.GrassCliff,new Vector2(x,ledge),2.1f);
                AddBox(parent,"NorthernCliffFootprint",new Vector2(x,ledge+.38f),new Vector2(2.1f,.76f));
                if(i%2==0)
                {
                    Prop(parent,"CliffFootFern",art.BushWhite,new Vector2(x+.6f,ledge-.13f),1.05f);
                    Prop(parent,"CliffFootStone",art.RockSmall,new Vector2(x-.65f,ledge-.2f),.7f);
                }
            }
            Vector2[] clusters=camp?new[] {new Vector2(-14.5f,5.9f),new Vector2(-12.8f,6.5f),new Vector2(-10.5f,6.2f),
                new Vector2(-3.4f,6.5f),new Vector2(-1.6f,7.0f),new Vector2(2.8f,6.8f),new Vector2(5.8f,6.1f),
                new Vector2(8.2f,6.8f),new Vector2(10.7f,6.0f),new Vector2(13f,6.4f)}:
                new[] {new Vector2(-13.7f,5.8f),new Vector2(-11.8f,6.4f),new Vector2(-10f,6.0f),
                new Vector2(-5.7f,6.2f),new Vector2(-3.6f,6.8f),new Vector2(-1.4f,6.1f),
                new Vector2(4.1f,6f),new Vector2(6.3f,6.7f),new Vector2(8.7f,6.1f),new Vector2(11.4f,6.4f),new Vector2(14.1f,5.6f)};
            for(int i=0;i<clusters.Length;i++)
            {
                Tree(parent,art,clusters[i],i%3==0?3.35f:i%3==1?2.65f:3f);
                if(i%2==1) Prop(parent,"ForestUnderstory",i%4==1?art.BushPink:art.BushWhite,clusters[i]+new Vector2(.95f,-.2f),1.3f);
            }
        }

        private static void OpeningTimberFence(Transform parent,WorldArt art,Vector2 position)
        {
            Prop(parent,"CompanionCourtyardTimberFence",art.FenceTimber,position,2.2f);
            AddBox(parent,"CompanionCourtyardFenceBody",position+Vector2.up*.15f,new Vector2(2.2f,.35f));
        }

        private static void BuildHouse(Transform parent,WorldArt art,Vector2 position,float width)
        {
            var house=Prop(parent,"ShelterFootprint",art.House,position,width);
            if(house==null) return;
            Vector2 size=art.House.bounds.size;
            var polygon=house.AddComponent<PolygonCollider2D>();
            polygon.points=new[] {new Vector2(-size.x*.40f,size.y*.05f),new Vector2(size.x*.40f,size.y*.05f),
                new Vector2(size.x*.44f,size.y*.21f),new Vector2(size.x*.31f,size.y*.36f),
                new Vector2(-size.x*.32f,size.y*.36f),new Vector2(-size.x*.44f,size.y*.22f)};
        }

        private static void ScatterOpeningPlants(Transform parent,WorldArt art,Vector2[] positions)
        {
            Sprite[] plants={art.BushPink,art.BushWhite,art.FlowerBlue,art.FlowerWhite,art.RockSmall};
            for(int i=0;i<positions.Length;i++)
            {
                Prop(parent,"CoastalPlantCluster",plants[i%plants.Length],positions[i],i%5<2?1.3f:.85f);
                if(i%3==0) Prop(parent,"TrailFlowerAccent",art.FlowerWhite,positions[i]+new Vector2(.65f,-.35f),.55f);
            }
        }

        private static void FenceColumn(Transform parent,WorldArt art,Vector2 position,float height)
        {
            AddBox(parent,"GardenSideFence",position,new Vector2(.3f,height));
            for(int i=0;i<2;i++)
            {
                var segment=Prop(parent,"GardenSideTimberFence",art.FenceTimber,
                    position+new Vector2(.4f,-height*.25f+i*height*.5f),1.4f);
                if(segment!=null) segment.transform.localRotation=Quaternion.Euler(0f,0f,90f);
            }
        }

        private static Tile MakeTile(Sprite sprite, Color color)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = Tile.ColliderType.None;
            if (sprite != null)
                tile.transform = Matrix4x4.Scale(new Vector3(1f / sprite.bounds.size.x, 1f / sprite.bounds.size.y, 1f));
            return tile;
        }

        private static void BuildBoundaries(Transform parent)
        {
            AddBox(parent, "WestBoundary", new Vector2(-15.8f, 0f), new Vector2(.7f, 20f));
            AddBox(parent, "EastBoundary", new Vector2(15.8f, 0f), new Vector2(.7f, 20f));
            AddBox(parent, "NorthBoundary", new Vector2(0f, 9.6f), new Vector2(32f, .8f));
            AddBox(parent, "SouthBoundary", new Vector2(0f, -9.6f), new Vector2(32f, .8f));
        }

        private static void BuildLandmarks(Transform parent, WorldArt art, int region, int room)
        {
            if(room==0&&region<=1)
            {
                BuildOpeningLandmarks(parent,art,region==0);
                return;
            }
            if (room == 0)
            {
                var house = Prop(parent, "ShelterFootprint", art.House, new Vector2(-7f, 3f), 6.8f);
                if (house != null)
                {
                    Vector2 size = art.House.bounds.size;
                    var polygon = house.AddComponent<PolygonCollider2D>();
                    polygon.points = new[] { new Vector2(-size.x*.40f,size.y*.05f), new Vector2(size.x*.40f,size.y*.05f),
                        new Vector2(size.x*.44f,size.y*.21f), new Vector2(size.x*.31f,size.y*.36f),
                        new Vector2(-size.x*.32f,size.y*.36f), new Vector2(-size.x*.44f,size.y*.22f) };
                }
                for (int i = 0; i < 3; i++) Fence(parent, art, new Vector2(4.5f + i * 2f, 5f));
            }
            else if (room == 1)
            {
                // Two offset groves form optional loops, leaving the main east/west path open.
                for (int i = 0; i < 4; i++)
                {
                    Tree(parent, art, new Vector2(-9f + i * 2.4f, 5.5f + (i % 2) * .5f));
                    Tree(parent, art, new Vector2(3f + i * 2.4f, -6.5f + (i % 2) * .6f));
                }
                if (region == 0 || region == 5)
                {
                    Fence(parent, art, new Vector2(7f, 3f));
                    Fence(parent, art, new Vector2(9f, 3f));
                }
            }
            else
            {
                // The guardian court has approach lanes around a ring of trees and masonry.
                Tree(parent, art, new Vector2(-9f, 6f));
                Tree(parent, art, new Vector2(9f, 6f));
                Fence(parent, art, new Vector2(-5f, 4f));
                Fence(parent, art, new Vector2(5f, 4f));
                Fence(parent, art, new Vector2(-5f, -4f));
                Fence(parent, art, new Vector2(5f, -4f));
            }
            Tree(parent, art, new Vector2(-12f, -5.5f));
            Tree(parent, art, new Vector2(11.5f, 6.8f));
            if (region == 2)
            {
                Tree(parent, art, new Vector2(-5f, 7.4f));
                Tree(parent, art, new Vector2(4f, 7f));
            }
            if (region == 1 && room == 1)
            {
                Fence(parent, art, new Vector2(-6f, -6f));
                Fence(parent, art, new Vector2(-4f, -6f));
            }
            if (region == 4 && room == 1)
            {
                for (int i = 0; i < 3; i++)
                    Fence(parent, art, new Vector2(-5f + i * 2f, 3.4f));
                for (int i = 0; i < 3; i++)
                    Fence(parent, art, new Vector2(1f + i * 2f, -3.4f));
            }
            if (region == 5 && room == 1)
                for (int i = -4; i <= 4; i++)
                    if (Math.Abs(i) > 1) Fence(parent, art, new Vector2(i * 2f, 4.7f));
        }

        private static void Populate(Transform parent, WorldArt art, CoastalWorldLayout layout, int region,
            int room, Action<string, string> onInteract, Func<string, bool> wasCollected)
        {
            if(room==0&&region<=1)
            {
                PopulateOpening(parent,art,layout,region==0,onInteract,wasCollected);
                return;
            }
            string regionId = Regions[region];
            string roomId = regionId + "-" + room;
            if (room == 0)
            {
                var fire = Prop(parent, "Campfire", art.Campfire, new Vector2(0f, 2f), 1.5f);
                var fireBody=AddBox(parent, "CampfireFootprint", new Vector2(0f, 2.1f), new Vector2(.8f,.45f));
                var fireInteraction=AddInteraction(parent, layout, "campfire", "campfire-" + roomId, "篝火 · 烹饪、合成与休息",
                    new Vector2(0f, 1.8f), 1f, onInteract, fire);
                fireInteraction.RegisterInteractionBody(fireBody);
                var lightObject = new GameObject("CampfireLight", typeof(Light2D));
                lightObject.transform.SetParent(parent, false);
                lightObject.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                var light = lightObject.GetComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = new Color(1f, .64f, .28f);
                light.intensity = 1.35f;
                light.pointLightOuterRadius = 4.5f;
                light.pointLightInnerRadius = .5f;
            }
            string[] materialIds = region == 4 ? new[] { "crystal", "dew", "crystal" } :
                region == 2 ? new[] { "pollen", "berry", "dew" } : new[] { "berry", "dew", "pollen" };
            Vector2[] resourcePositions = { new Vector2(-8f,-3.5f), new Vector2(7.5f,2f), new Vector2(5f,-4.5f) };
            for (int i = 0; i < resourcePositions.Length; i++)
            {
                string id = materialIds[i] + "-" + roomId + "-" + i;
                var resource = Prop(parent, materialIds[i], art.Resource(materialIds[i]), resourcePositions[i], .7f);
                var interaction = AddInteraction(parent, layout, "resource", id, "采集" + MaterialName(materialIds[i]),
                    resourcePositions[i], .5f, onInteract, resource);
                if (wasCollected != null && wasCollected(id)) interaction.Collect();
            }
            if (region > 0 && room != 2)
                for (int i = 0; i < 2; i++)
                {
                    Vector2 position = i == 0 ? new Vector2(-5.5f, -5f) : new Vector2(8.5f, -2.5f);
                    string id = "wild-" + roomId + "-" + i;
                    var wild = Prop(parent, "WildCreature-" + i, art.Wild(region, i), position, 1.2f);
                    AddInteraction(parent, layout, "encounter", id, "观察野生伙伴 · 战斗 / 投喂", position, .8f, onInteract, wild);
                }
            if ((region >= 2 && region <= 4 && room == 2) || (region == 5 && room == 1))
            {
                string guardianId = regionId + "-warden";
                var guardian = Prop(parent, guardianId, art.Guardian(region), new Vector2(0f, 3f), 3f);
                AddInteraction(parent, layout, "guardian", guardianId, "挑战" + RegionName(region) + "封印守卫",
                    new Vector2(0f, 3f), 1.8f, onInteract, guardian);
            }
            if (region == 5 && room == 2)
            {
                var boss = Prop(parent, "LighthouseFinalBoss", art.FinalBoss, new Vector2(0f, 3f), 4f);
                AddInteraction(parent, layout, "boss", "final-boss", "灯塔核心 · 最终共鸣", new Vector2(0f, 3f), 2f, onInteract, boss);
            }
            if (room == 1 && region > 0 && region < 5)
            {
                BuildFieldRoute(parent, art, layout, region, roomId, onInteract, wasCollected);
            }
            string hint = room == 0 ? "篝火可以恢复队伍、制作食物与共鸣道具。沿石径探索下一片区域。" :
                room == 1 ? "支路藏着资源与野生伙伴。伙伴能力解锁额外奖励，主路始终可以通行。" :
                region == 5 ? "回响观测者守护最后一枚封印。试炼结束后，与初始伙伴一同面对灯塔核心的最终共鸣。" :
                region == 1 ? "穿过海岸前哨就是辉光林地。四枚封印分别位于林地、潮汐、晶能和灯塔。" :
                "先做好准备，再挑战守卫。解除区域封印后可以向更深处前进。";
            var sign = Prop(parent, "路牌", art.Signpost != null ? art.Signpost : Marker("sign"), new Vector2(-1.8f, -3f), .7f);
            AddInteraction(parent, layout, "sign", hint, "阅读路牌", new Vector2(-1.8f, -3f), .55f, onInteract, sign);
        }

        private static void PopulateOpening(Transform parent,WorldArt art,CoastalWorldLayout layout,bool camp,
            Action<string,string> onInteract,Func<string,bool> wasCollected)
        {
            if(camp)
            {
                CreateActor(parent,layout,"watcher",art.WatcherFrames,new Vector2(-6.7f,1.3f),1.8f,
                    "npc","与 Watcher 交谈 · 伙伴照护",onInteract);
                CreateActor(parent,layout,"shiguang",art.ShiguangFrames,new Vector2(4.2f,1.3f),1.8f,
                    "npc","与拾光交谈 · 岛屿与伙伴",onInteract);
                string[] species={"moss-lumi","moatail","crystal-gecko"};
                Vector2[] positions={new Vector2(6.2f,2.8f),new Vector2(9f,2.8f),new Vector2(7.6f,.8f)};
                string[] petNames={"苔藓灵","沫尾兽","鸣晶蜥"};
                for(int i=0;i<species.Length;i++)
                    CreateActor(parent,layout,species[i],new[] {art.Starter(i)},positions[i],.95f,
                        "starter","认识"+petNames[i]+" · 初始伙伴",onInteract);
                var fire=Prop(parent,"Campfire",art.Campfire,new Vector2(-1.6f,-1.9f),1.35f);
                var fireBody=AddBox(parent,"CampfireFootprint",new Vector2(-1.6f,-1.75f),new Vector2(.7f,.4f));
                var fireInteraction=AddInteraction(parent,layout,"campfire","campfire-camp-0","篝火工作台 · 烹饪与合成",
                    new Vector2(-1.6f,-1.5f),.8f,onInteract,fire);
                fireInteraction.RegisterInteractionBody(fireBody);
                AddCampfireLight(parent,new Vector2(-1.6f,-1.5f));
                AddOpeningResource(parent,art,layout,"berry-camp-0-0","berry",new Vector2(-4.9f,-.3f),onInteract,wasCollected);
                AddOpeningResource(parent,art,layout,"dew-camp-0-1","dew",new Vector2(4.6f,-4.9f),onInteract,wasCollected);
                AddOpeningResource(parent,art,layout,"pollen-camp-0-2","pollen",new Vector2(10.5f,3.9f),onInteract,wasCollected);
                AddOpeningSign(parent,art,layout,new Vector2(-9f,-2f),
                    "海湾码头 · 沿沙地小径进入营地。Watcher 的照护屋在北边，拾光与三只伙伴在东侧小院。",onInteract);
                layout.AddLandmark("dock-arrival",parent.TransformPoint(new Vector2(-10.5f,-6.5f)));
                layout.AddLandmark("watcher-home",parent.TransformPoint(new Vector2(-6.7f,1.3f)));
                layout.AddLandmark("shiguang-home",parent.TransformPoint(new Vector2(4.2f,1.3f)));
                layout.AddLandmark("dock-end",parent.TransformPoint(new Vector2(-10.5f,-3.2f)));
                layout.AddLandmark("house-door",parent.TransformPoint(new Vector2(-7f,2.6f)));
                layout.AddLandmark("watcher-approach",parent.TransformPoint(new Vector2(-6.7f,.25f)));
                layout.AddLandmark("shiguang-approach",parent.TransformPoint(new Vector2(4.2f,0f)));
                layout.AddLandmark("campfire-approach",parent.TransformPoint(new Vector2(-1.6f,-.6f)));
                layout.AddLandmark("starter-meeting",parent.TransformPoint(new Vector2(5.3f,.2f)));
                layout.AddLandmark("guide-camp-waypoint",parent.TransformPoint(new Vector2(9f,1.2f)));
                layout.AddLandmark("guide-exit-wait",parent.TransformPoint(new Vector2(10.8f,2.2f)));
                layout.AddLandmark("coast-exit",parent.TransformPoint(new Vector2(13f,1.3f)));
            }
            else
            {
                AddOpeningResource(parent,art,layout,"berry-coast-0-0","berry",new Vector2(-7f,1.8f),onInteract,wasCollected);
                AddOpeningResource(parent,art,layout,"dew-coast-0-1","dew",new Vector2(2.8f,3.2f),onInteract,wasCollected);
                AddOpeningResource(parent,art,layout,"pollen-coast-0-2","pollen",new Vector2(6f,-1.8f),onInteract,wasCollected);
                CreateActor(parent,layout,"tutorial-coast-crab",new[] {art.TutorialCrab},new Vector2(7.5f,-3.2f),1.1f,
                    "encounter","受困的潮壳蟹 · 观察它的状态",onInteract);
                layout.AddLandmark("coast-arrival",parent.TransformPoint(new Vector2(-13f,1.2f)));
                layout.AddLandmark("coast-waypoint-1",parent.TransformPoint(new Vector2(-9f,0f)));
                layout.AddLandmark("coast-waypoint-2",parent.TransformPoint(new Vector2(-2f,2.8f)));
                layout.AddLandmark("coast-waypoint-3",parent.TransformPoint(new Vector2(5f,.8f)));
                layout.AddLandmark("tutorial-viewpoint",parent.TransformPoint(new Vector2(6.5f,-1.6f)));
                layout.AddLandmark("tutorial-coast-crab",parent.TransformPoint(new Vector2(7.5f,-3.2f)));
            }
        }

        private static void AddCampfireLight(Transform parent,Vector2 position)
        {
            var obj=new GameObject("LocalCampfireGlow",typeof(Light2D));
            obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;
            var light=obj.GetComponent<Light2D>();
            light.lightType=Light2D.LightType.Point;
            light.color=new Color(1f,.76f,.49f);
            light.intensity=.48f;
            light.pointLightOuterRadius=2.1f;
            light.pointLightInnerRadius=.18f;
        }

        private static void AddOpeningResource(Transform parent,WorldArt art,CoastalWorldLayout layout,string id,
            string material,Vector2 position,Action<string,string> onInteract,Func<string,bool> wasCollected)
        {
            // Pickup glyphs sit among authored plants/containers, so resources belong to the place.
            Sprite context=material=="berry"?art.BushPink:material=="dew"?art.Barrel:art.FlowerWhite;
            Prop(parent,"ResourceContext-"+id,context,position+new Vector2(0f,.08f),material=="berry"?1.15f:.8f);
            var item=Prop(parent,"Resource-"+id,art.Resource(material),position,.38f);
            var interaction=AddInteraction(parent,layout,"resource",id,"采集"+MaterialName(material),position,.45f,onInteract,item);
            if(wasCollected!=null&&wasCollected(id)) interaction.Collect();
        }

        private static void AddOpeningSign(Transform parent,WorldArt art,CoastalWorldLayout layout,Vector2 position,
            string text,Action<string,string> onInteract)
        {
            var sign=Prop(parent,"TrailSign",art.Signpost,position,.65f);
            AddInteraction(parent,layout,"sign",text,"阅读路牌",position,.4f,onInteract,sign);
        }

        private static void CreateActor(Transform parent,CoastalWorldLayout layout,string id,Sprite[] frames,
            Vector2 position,float height,string kind,string prompt,Action<string,string> onInteract)
        {
            if(frames==null||frames.Length==0||frames[0]==null)
                throw new InvalidOperationException("Approved world sprite is missing for actor "+id);
            var actor=new GameObject(id);
            actor.transform.SetParent(parent,false);
            actor.transform.localPosition=position;
            var spriteObject=new GameObject("Sprite",typeof(SpriteRenderer));
            spriteObject.transform.SetParent(actor.transform,false);
            var renderer=spriteObject.GetComponent<SpriteRenderer>();
            renderer.sprite=frames[0];
            renderer.sharedMaterial=Resources.Load<Material>("Settings/MistIsleSpriteLit");
            spriteObject.transform.localScale=Vector3.one*(height/Mathf.Max(.01f,frames[0].bounds.size.y));
            var footprint=actor.AddComponent<CapsuleCollider2D>();
            footprint.size=kind=="starter"?new Vector2(.34f,.24f):new Vector2(.45f,.32f);
            footprint.offset=new Vector2(0f,footprint.size.y*.5f);
            footprint.direction=CapsuleDirection2D.Horizontal;
            var presentation=actor.AddComponent<WorldActorPresentation>();
            presentation.Configure(renderer,frames);
            var interaction=AddInteraction(actor.transform,layout,kind,id,prompt,new Vector2(0f,.15f),kind=="starter"?.45f:.65f,onInteract);
            interaction.RegisterInteractionBody(footprint);
            layout.RegisterActor(id,actor.transform);
        }

        private static void BuildFieldRoute(Transform parent, WorldArt art, CoastalWorldLayout layout, int region,
            string roomId, Action<string, string> onInteract, Func<string, bool> wasCollected)
        {
            string fieldId = "field-" + Regions[region];
            // A fully enclosed optional courtyard has one solid, ability-operated entrance.
            // Its cache is farther than interaction range from every outside wall.
            FieldWall(parent, art, "FieldWest", new Vector2(6f,6.5f), new Vector2(.35f,5f));
            FieldWall(parent, art, "FieldEast", new Vector2(13f,6.5f), new Vector2(.35f,5f));
            FieldWall(parent, art, "FieldNorth", new Vector2(9.5f,9f), new Vector2(7.3f,.35f));
            FieldWall(parent, art, "FieldSouthLeft", new Vector2(7.2f,4f), new Vector2(2.4f,.4f));
            FieldWall(parent, art, "FieldSouthRight", new Vector2(11.8f,4f), new Vector2(2.4f,.4f));
            var barrier = new GameObject("FieldBarrier-" + fieldId, typeof(BoxCollider2D));
            barrier.transform.SetParent(parent, false);
            barrier.transform.localPosition = new Vector2(9.5f,4f);
            barrier.GetComponent<BoxCollider2D>().size = new Vector2(2.2f,.45f);
            var gate = Prop(barrier.transform, "ClosedAbilityGate", region == 2 ? art.Tree : art.Fence, Vector2.zero, 2.2f);
            if (gate != null) gate.GetComponent<SpriteRenderer>().color = region == 2 ? new Color(.62f,.86f,.49f) :
                region == 4 ? new Color(.78f,.58f,1f) : new Color(.50f,.78f,1f);
            var fieldMarker = Prop(parent, "伙伴能力支路", Marker("pollen"), new Vector2(9.5f,2.6f), .7f);
            string ability = region == 2 ? "藤蔓生长" : region == 4 ? "晶体共鸣" : "引水";
            var interaction = AddInteraction(parent, layout, "field", fieldId, "支路机关 · " + ability,
                new Vector2(9.5f,2.6f), .75f, onInteract, fieldMarker);
            var openMarker = Prop(parent, "OpenedField-" + fieldId, Marker("opened"), new Vector2(9.5f,3.7f), .7f);
            if (openMarker != null) openMarker.SetActive(false);
            layout.RegisterField(fieldId, barrier, interaction, openMarker);

            string material = region == 2 ? "pollen" : region == 4 ? "crystal" : "dew";
            string cacheId = material + "-" + roomId + "-field-cache";
            var cache = Prop(parent, "支路遗留物资", art.Resource(material), new Vector2(9.5f,6.5f), 1f);
            var pickup = AddInteraction(parent, layout, "resource", cacheId, "拾取支路宝藏 · " + MaterialName(material),
                new Vector2(9.5f,6.5f), .45f, onInteract, cache);
            if (wasCollected != null && wasCollected(cacheId)) pickup.Collect();
        }

        private static void FieldWall(Transform parent, WorldArt art, string name, Vector2 center, Vector2 size)
        {
            AddBox(parent, name, center, size);
            if (size.x > size.y)
            {
                int count = Mathf.CeilToInt(size.x / 2f);
                for (int i = 0; i < count; i++)
                    Prop(parent, name + "Fence", art.Fence,
                        center + new Vector2(-size.x*.5f + (i+.5f)*size.x/count,0f), size.x/count+.1f);
            }
            else
                for (int i = 0; i < 3; i++)
                    Prop(parent, name + "Fence", art.Fence, center + new Vector2(0f,-1.7f+i*1.7f), .8f);
        }

        private static void ConnectRooms(Transform root, WorldArt art, CoastalWorldLayout layout, Action<string, string> onInteract)
        {
            Connect(root, layout, "camp-0", "camp-1", new Vector2(-12f, -1f), new Vector2(12f, 0f), onInteract,art);
            Connect(root, layout, "camp-0", "camp-2", new Vector2(4f, -6.5f), new Vector2(-12f, 0f), onInteract,art);
            Connect(root, layout, "camp-0", "coast-0", new Vector2(13f, 1.3f), new Vector2(-13f, 1.2f), onInteract,art);
            for (int region = 1; region < Regions.Length; region++)
            {
                Connect(root, layout, Regions[region] + "-0", Regions[region] + "-1", new Vector2(12f, 0f), new Vector2(-12f, 0f), onInteract,art);
                Connect(root, layout, Regions[region] + "-1", Regions[region] + "-2", new Vector2(12f, 0f), new Vector2(-12f, 0f), onInteract,art);
                if (region + 1 < Regions.Length)
                    Connect(root, layout, Regions[region] + "-2", Regions[region + 1] + "-0", new Vector2(12f, 0f), new Vector2(-12f, 0f), onInteract,art);
            }
        }

        private static void Connect(Transform root, CoastalWorldLayout layout, string a, string b,
            Vector2 pointA, Vector2 pointB, Action<string, string> onInteract,WorldArt art)
        {
            Transform roomA = FindRoom(root, a);
            Transform roomB = FindRoom(root, b);
            bool openingA=a=="camp-0"||a=="coast-0",openingB=b=="camp-0"||b=="coast-0";
            var arrowA = Prop(roomA, "出口 · " + b,openingA?art.Signpost:Marker("exit"),pointA+(openingA?Vector2.up:Vector2.zero),openingA?.8f:1f);
            var arrowB = Prop(roomB, "出口 · " + a,openingB?art.Signpost:Marker("exit"),pointB+(openingB?Vector2.up:Vector2.zero),openingB?.8f:1f);
            AddInteraction(roomA, layout, "exit", b, "前往 " + layout.GetRoomName(b), pointA, 1.15f, onInteract, arrowA);
            AddInteraction(roomB, layout, "exit", a, "返回 " + layout.GetRoomName(a), pointB, 1.15f, onInteract, arrowB);
        }

        private static Transform FindRoom(Transform parent, string id)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name.StartsWith(id + " ", StringComparison.Ordinal)) return parent.GetChild(i);
            throw new InvalidOperationException("Missing room " + id);
        }

        private static WorldInteraction AddInteraction(Transform parent, CoastalWorldLayout layout, string kind,
            string id, string prompt, Vector2 position, float radius, Action<string, string> onInteract, GameObject visual = null)
        {
            var obj = new GameObject(kind + ":" + id, typeof(CircleCollider2D), typeof(WorldInteraction));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer >= 0) obj.layer = layer;
            var collider = obj.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = radius;
            var target = obj.GetComponent<WorldInteraction>();
            target.Initialize(kind, id, prompt, onInteract, visual);
            layout.Register(target);
            return target;
        }

        private static GameObject Prop(Transform parent, string name, Sprite sprite, Vector2 position, float width)
        {
            if (sprite == null) return null;
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            float scale = width / Mathf.Max(.001f, sprite.bounds.size.x);
            obj.transform.localScale = Vector3.one * scale;
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            var material = Resources.Load<Material>("Settings/MistIsleSpriteLit");
            if (material != null) renderer.sharedMaterial = material;
            renderer.sortingOrder = Mathf.RoundToInt(-obj.transform.position.y * 100f);
            return obj;
        }

        private static void Tree(Transform parent, WorldArt art, Vector2 position, float width = 2.6f)
        {
            var tree = Prop(parent, "TreeTrunkCollider", art.Tree, position, width);
            if (tree == null) return;
            Vector2 size = art.Tree.bounds.size;
            var collider = tree.AddComponent<PolygonCollider2D>();
            collider.points = new[] { new Vector2(-size.x*.10f,0f), new Vector2(size.x*.10f,0f),
                new Vector2(size.x*.13f,size.y*.13f), new Vector2(0f,size.y*.19f), new Vector2(-size.x*.13f,size.y*.13f) };
        }

        private static void Fence(Transform parent, WorldArt art, Vector2 position)
        {
            Prop(parent, "Fence", art.Fence, position, 2.1f);
            AddBox(parent, "FenceFootprint", position + new Vector2(0f,.15f), new Vector2(2.1f,.35f));
        }

        private static BoxCollider2D AddBox(Transform parent, string name, Vector2 point, Vector2 size)
        {
            var obj = new GameObject(name, typeof(BoxCollider2D));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = point;
            var collider=obj.GetComponent<BoxCollider2D>();
            collider.size = size;
            return collider;
        }

        private static string RegionName(int region) => new[] { "营地", "海岸", "林地", "潮汐", "晶能", "灯塔" }[region];
        private static string MaterialName(string id) => id == "berry" ? "野果" : id == "dew" ? "露水" : id == "crystal" ? "晶尘" : "花粉";

        // Small map symbols are code-authored UI glyphs, never character or creature substitutes.
        private static Sprite Marker(string kind)
        {
            if (MarkerSprites.TryGetValue(kind, out Sprite cached) && cached != null) return cached;
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false) { name = "MapGlyph-" + kind, filterMode = FilterMode.Point };
            var pixels = new Color[16 * 16];
            Color color = kind == "opened" ? new Color(.42f,.96f,.62f) : kind == "berry" ? new Color(.91f,.24f,.26f) : kind == "dew" ? new Color(.40f,.82f,1f) :
                kind == "crystal" ? new Color(.79f,.51f,1f) : kind == "sign" ? new Color(.65f,.42f,.22f) : new Color(1f,.83f,.32f);
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    bool filled = kind == "sign" ? ((x >= 2 && x <= 13 && y >= 7 && y <= 13) || (x >= 7 && x <= 9 && y <= 8)) :
                        kind == "exit" ? (y >= 3 && y <= 12 && Math.Abs(x - 7) <= Math.Max(1, (y - 3) / 2)) :
                        Math.Abs(x - 7) + Math.Abs(y - 7) <= 5;
                    if (filled) pixels[y * 16 + x] = color;
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0,0,16,16), new Vector2(.5f,0f), 16f);
            MarkerSprites[kind] = sprite;
            return sprite;
        }

        private sealed class WorldArt
        {
            public readonly Sprite House = Load("Environment/House");
            public readonly Sprite Tree = Load("Environment/Tree");
            public readonly Sprite Fence = Load("Environment/Fence");
            public readonly Sprite FenceTimber = Load("Environment/FenceTimber");
            public readonly Sprite GrassCliff = Load("Environment/GrassCliff");
            public readonly Sprite GrassCliffCorner = Load("Environment/GrassCliffCorner");
            public readonly Sprite Campfire = Load("Environment/Campfire");
            public readonly Sprite Dock = Load("Environment/Dock");
            public readonly Sprite Boat = Load("Environment/Boat");
            public readonly Sprite Tent = Load("Environment/Tent");
            public readonly Sprite BushPink = Load("Environment/BushPink");
            public readonly Sprite BushWhite = Load("Environment/BushWhite");
            public readonly Sprite FlowerBlue = Load("Environment/FlowerBlue");
            public readonly Sprite FlowerWhite = Load("Environment/FlowerWhite");
            public readonly Sprite RockLarge = Load("Environment/RockLarge");
            public readonly Sprite RockSmall = Load("Environment/RockSmall");
            public readonly Sprite Crate = Load("Environment/Crate");
            public readonly Sprite Barrel = Load("Environment/Barrel");
            public readonly Sprite Lantern = Load("Environment/Lantern");
            public readonly Sprite Log = Load("Environment/Log");
            public readonly Sprite Signpost = Load("Environment/Signpost");
            public readonly Sprite[] WatcherFrames = Frames("Actors/Watcher");
            public readonly Sprite[] ShiguangFrames = Frames("Actors/ShiguangPixel");
            public readonly Sprite TutorialCrab = Load("Creatures/PixelWildCrab");
            public readonly Sprite FinalBoss = Load("Creatures/GuardianLighthouse");
            public readonly Material LitMaterial = Resources.Load<Material>("Settings/MistIsleSpriteLit");
            private readonly Sprite[] ground = Resources.LoadAll<Sprite>("Art/Tiles/Ground");
            public WorldArt()
            {
                Array.Sort(ground, (a,b) => SpriteIndex(a).CompareTo(SpriteIndex(b)));
                if (ground.Length < 48) throw new InvalidOperationException("Approved Ground sprite tiles have not been imported (48 required).");
            }
            public Sprite Ground(int index) => ground[Mathf.Clamp(index,0,ground.Length-1)];
            public Sprite Floor(int region) => Ground(region == 1 ? 12 : region == 0 || region == 2 ? 0 : 24);
            public Sprite Resource(string id) => Load("Items/" + (id == "berry" ? "Berry" : id == "dew" ? "Dew" : id == "crystal" ? "Crystal" : "Pollen")) ?? Marker(id);
            public Sprite Starter(int index) => Load("Creatures/"+new[] {"PixelMossLumi","PixelMoatail","PixelCrystalGecko"}[index]);
            public Sprite Wild(int region, int index) => Load("Creatures/" + (region == 1 ? (index == 0 ? "PixelWildCrab" : "WildBird") :
                region == 2 ? "MossLumi" : region == 3 ? "Moatail" : "CrystalGecko"));
            public Sprite Guardian(int region) => Load("Creatures/Guardian" + (region == 5 ? "Lighthouse" : region == 2 ? "Grove" : region == 3 ? "Tide" : "Crystal"));
            private static int SpriteIndex(Sprite sprite)
            {
                string name = sprite.name;
                return int.TryParse(name.Substring(name.LastIndexOf('_')+1),out int index) ? index : 0;
            }
            private static Sprite Load(string path) => Resources.Load<Sprite>("Art/" + path);
            private static Sprite[] Frames(string path)
            {
                var sprites=Resources.LoadAll<Sprite>("Art/"+path);
                Array.Sort(sprites,(a,b)=>SpriteIndex(a).CompareTo(SpriteIndex(b)));
                return sprites;
            }
        }
    }

    public sealed class CoastalWorldLayout
    {
        private readonly Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        private readonly List<WorldInteraction> interactions = new List<WorldInteraction>();
        private readonly Dictionary<string, FieldRoute> fields = new Dictionary<string, FieldRoute>();
        private readonly Dictionary<string, Transform> actors = new Dictionary<string, Transform>();
        private readonly Dictionary<string, Vector2> landmarks = new Dictionary<string, Vector2>();
        public Vector2 Spawn { get; internal set; }
        public Rect CameraBounds { get; internal set; }
        public IReadOnlyList<WorldInteraction> Interactions => interactions;
        public IEnumerable<string> RoomIds => rooms.Keys;
        internal void AddRoom(string id, string name, Vector2 spawn, Rect bounds) => rooms.Add(id, new Room { Name = name, Spawn = spawn, Bounds = bounds });
        internal void Register(WorldInteraction interaction)
        {
            interactions.Add(interaction);
            if(interaction.Kind!="exit"&&interaction.Kind!="sign") landmarks[interaction.Id]=interaction.transform.position;
        }
        internal void RegisterActor(string id,Transform actor) { actors.Add(id,actor); landmarks[id]=actor.position; }
        internal void AddLandmark(string id,Vector2 position) => landmarks[id]=position;
        internal void RegisterField(string id, GameObject barrier, WorldInteraction interaction, GameObject openMarker)
            => fields.Add(id, new FieldRoute { Barrier = barrier, Interaction = interaction, OpenMarker = openMarker });
        public Vector2 GetSpawn(string id) => rooms.TryGetValue(id ?? "", out var room) ? room.Spawn : rooms["camp-0"].Spawn;
        public Rect GetBounds(string id) => rooms.TryGetValue(id ?? "", out var room) ? room.Bounds : rooms["camp-0"].Bounds;
        public string GetRoomName(string id) => rooms.TryGetValue(id ?? "", out var room) ? room.Name : "海岸营地";
        public string GetRoomId(Vector2 point)
        {
            foreach (var pair in rooms) if (pair.Value.Bounds.Contains(point)) return pair.Key;
            return "camp-0";
        }
        public Transform GetActor(string id) => actors.TryGetValue(id??"",out var actor)?actor:null;
        public void SetActorVisible(string id,bool visible)
        {
            if(actors.TryGetValue(id??"",out var actor)) actor.gameObject.SetActive(visible);
        }
        public Vector2 GetLandmarkPosition(string id)
        {
            if(actors.TryGetValue(id??"",out var actor)) return actor.position;
            if(landmarks.TryGetValue(id??"",out var position)) return position;
            throw new ArgumentOutOfRangeException(nameof(id),id,"Unknown world landmark.");
        }
        public void MoveActorFacing(string id,Vector2 position,Vector2 facing)
        {
            if(!actors.TryGetValue(id??"",out var actor)) return;
            actor.position=position;
            actor.GetComponent<WorldActorPresentation>()?.Face(facing);
        }
        public void SetCollected(string id)
        {
            foreach (var target in interactions) if (target.Id == id) target.Collect();
        }
        public void ResetCollected(Func<string, bool> wasCollected)
        {
            foreach (var target in interactions)
                if (target.Kind == "resource") target.SetAvailable(wasCollected == null || !wasCollected(target.Id));
        }
        public bool OpenField(string id)
        {
            if (!fields.TryGetValue(id ?? "", out var route)) return false;
            SetFieldState(route, true);
            return true;
        }
        public bool IsFieldOpen(string id) => fields.TryGetValue(id ?? "", out var route) && route.Open;
        public void ResetFields(Func<string, bool> isOpen)
        {
            foreach (var pair in fields) SetFieldState(pair.Value, isOpen != null && isOpen(pair.Key));
        }
        private static void SetFieldState(FieldRoute route, bool open)
        {
            route.Open = open;
            route.Barrier.SetActive(!open);
            route.Interaction.SetAvailable(!open);
            if (route.OpenMarker != null) route.OpenMarker.SetActive(open);
        }
        private sealed class FieldRoute { public GameObject Barrier; public WorldInteraction Interaction; public GameObject OpenMarker; public bool Open; }
        private sealed class Room { public string Name; public Vector2 Spawn; public Rect Bounds; }
    }

    public sealed class WorldActorPresentation : MonoBehaviour
    {
        private SpriteRenderer sprite;
        private Sprite[] frames;
        private int direction;
        private Vector3 previousPosition;
        private float walkTime;
        public void Configure(SpriteRenderer renderer,Sprite[] animationFrames)
        { sprite=renderer; frames=animationFrames; previousPosition=transform.position; }
        public void Face(Vector2 facing)
        {
            if(facing.sqrMagnitude<.001f) return;
            direction=Mathf.Abs(facing.x)>Mathf.Abs(facing.y)?facing.x<0f?1:2:facing.y>0f?3:0;
        }
        private void LateUpdate()
        {
            if(sprite==null||frames==null||frames.Length==0) return;
            Vector2 movement=transform.position-previousPosition;
            bool moving=movement.sqrMagnitude>.00001f;
            if(moving) { Face(movement); walkTime+=Time.deltaTime; } else walkTime=0f;
            int perDirection=frames.Length>=12?frames.Length/4:1;
            int frame=moving?(int)(walkTime*9f)%perDirection:0;
            sprite.sprite=frames.Length>=12?frames[Mathf.Clamp(direction*perDirection+frame,0,frames.Length-1)]:frames[0];
            sprite.sortingOrder=Mathf.RoundToInt(-transform.position.y*100f);
            previousPosition=transform.position;
        }
    }

    public sealed class WorldInteraction : MonoBehaviour, IWorldInteractable
    {
        private Action<string, string> callback;
        private GameObject visual;
        private readonly HashSet<Collider2D> interactionBodies=new HashSet<Collider2D>();
        public string Kind { get; private set; }
        public string Id { get; private set; }
        public string Prompt { get; private set; }
        public string InteractionPrompt => Prompt;
        public bool CanInteract { get; private set; } = true;
        internal void Initialize(string kind, string id, string prompt, Action<string, string> onInteract, GameObject representation)
        { Kind = kind; Id = id; Prompt = prompt; callback = onInteract; visual = representation; }
        public void Interact() { if (CanInteract) callback?.Invoke(Kind, Id); }
        public void RegisterInteractionBody(Collider2D collider) { if(collider!=null) interactionBodies.Add(collider); }
        public bool OwnsCollider(Collider2D collider) => collider!=null &&
            (interactionBodies.Contains(collider)||collider.transform==transform||collider.transform.IsChildOf(transform));
        public void Collect() => SetAvailable(false);
        public void SetAvailable(bool available)
        {
            CanInteract = available;
            var shape = GetComponent<Collider2D>();
            if (shape != null) shape.enabled = available;
            if (visual != null) visual.SetActive(available);
        }
    }
}
