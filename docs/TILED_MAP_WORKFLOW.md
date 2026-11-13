# Tiled 地图编辑与验收流程

## 结论

当前四张地图由仓库中的 `maps/*.tmx` 作为唯一地图源文件。我可以直接修改这些文件，不需要你手动打开 Tiled 摆放地块或物件。

Tiled 仍然保留为可视化编辑器：你可以用它检查地图、拖动树木或建筑，也可以自己做小幅调整。你不需要为了本轮重做地图额外安装或操作 Tiled。

## 地图结构

| 文件 / 图层 | 用途 |
|---|---|
| `maps/coastal_camp.tmx` | 海岸营地 |
| `maps/glowglade_grove.tmx` | 辉光林地 |
| `maps/tidal_ruins.tmx` | 潮汐遗迹 |
| `maps/lighthouse_courtyard.tmx` | 灯塔庭院 |
| `maps/terrain.tsx` | 56px 地形 Tileset |
| `Ground` | 草地、沙地、石地和水域 |
| `Collision` | 建筑、墙体等矩形阻挡 |
| `Objects` | 树木、建筑、桥梁、花草和场景物件 |
| `Markers` | 出生点、返回点、区域入口、机关和 BOSS 点 |

地图尺寸固定为 **22 × 18 格**，每格 **56 × 56 px**。

## 在 Tiled 中查看

1. 打开 Tiled。
2. 选择 `File → Open File`。
3. 打开仓库里的 `maps/coastal_camp.tmx`。
4. Tileset 会通过相对路径自动加载；另外三张地图也可以按相同方式打开。

不建议直接修改生成目录里的 C 头文件。它们是构建产物，不是地图源文件。

## 编辑约定

- 不要随意改动 `Ground`、`Collision`、`Objects`、`Markers` 图层名称。
- `Objects` 中使用项目已识别的类型，例如 `tree0`、`tree1`、`tree2`、`prop_28`、`prop_32`、`prop_500` 和 `campfire`。
- 场景物件可通过 `scale` 浮点属性调整大小。
- 树木的基础阻挡由地图编译器自动生成；大型建筑和墙体需要在 `Collision` 中添加矩形。
- `prop_32` 是桥梁。编译器会将桥面覆盖的水域格设为可通行。
- `Markers` 必须保留 `spawn`、`return`、`grove_gate`、`ruin_gate`、`shrine`、`boss` 六个名称。所有故事标记必须从出生点可达。

## 预览与构建

在仓库根目录运行：

```bash
python tools/generate_ground_atlas.py
python tools/preview_tiled_maps.py --output build/map-previews
python tools/validate_project.py
```

正常构建时，CMake 会自动生成地形图集和地图 C 数据。运行时只读取编译好的数据和 PNG 素材，不需要 Tiled、Python 或 TMX 解析器。

## 地形过渡

每个地图格最多生成北、东、南、西四个方向的透明过渡遮罩。这样水道弯折、沙滩边缘和不同地表相接时，不再只依赖单一方向的直线贴片。地图仍以 Tiled 中的基础地块为准，过渡层由构建工具自动计算。