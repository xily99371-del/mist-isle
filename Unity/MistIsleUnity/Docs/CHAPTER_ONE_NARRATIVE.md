# 第一章叙事与地图接入

本文对应 `Assets/Scripts/Runtime/DesignPackNarrative.cs` 的可调用台词。服务只返回对白和目标文案；奖励、战斗、地图开关与存档由会话层执行。

## 资料与适用边界

已完整阅读原始 `MistIsle_Complete_Design_And_Assets_v01.zip` 的 28 份 Markdown/JSON，并与工程 `Resources/Data/MapDesign` 的对应文本逐份核对 SHA256，一致。已查看五张 Blueprint；原压缩包 `M01_TidewindPath/Blueprint.png` 实际为 M02 林地画面，`M02_Glowglade/Blueprint.png` 实际为 M01 小径画面。地图与事件身份采用 WorldConnections、ProgressionGraph 和各图 Layout/Interactions 的稳定 ID。

设计包定义五图、M01 分流、森/潮/晶三印及灯塔终局。本轮沿用用户已经确认的四守卫和最终阶段：M04 三印之门接受三处区域见证，灯塔前庭由回响观测者进行第四试炼，之后在最终战场面对同一观测者的封闭协议。第四试炼不增加另一名 Boss，也不把三印之门改成四印门。

晶守卫 ID 保留 `crystal-warden`，对白采用用户最新确认名称“棱晶蛇王”；外形和像素资源沿用现有批准形象。`DesignPackNarrative` 不修改任何物种资产。

“求援航单没有回执”“保护协议把风险管理变为封闭”是此次为地图和战斗补充的串联文本。旅人主动回应求援，在亲眼见到岛上生活之后作出继续冒险的承诺；没有增加亲属关系、失踪亲人或命定身份。

## 冒险承诺

外海驿站留着一张长期没有回执的雾屿求援航单。旅人前来确认补给与归航为什么中断，在 Watcher 的照护屋看见等待的实际后果。第一次帮助的对象是 M01 木桥上游溪边的受伤潮壳蟹；它从戒备到自愿跟随，把“恢复归路”落实到一位具体生命身上。

回营交接时，旅人明确承诺：接回采集路、水路和航路，让等待的人与伙伴能往返。拾光说明从 M01 向北去林地、向东去遗迹，两条路线均可先走。离开营地以后，地图对白主要由旅人与场景读物承担，不让仍在营地的拾光或 Watcher 无故在远处发言。

旅人、拾光与 Watcher 的语气延续开场：旅人好奇、谨慎并愿意行动；拾光先观察伙伴的回应，再指导实践；Watcher 用照护、物资和归处支持旅途。三只初始伙伴的独立初见与同行回应保留原文，其他伙伴也以自愿建立信任的关系加入。

## 守卫职责、试炼和结果

| 战斗 ID | 守护的实际事物 | 战斗理由 | 获认可后的意义 |
|---|---|---|---|
| `glowglade-warden` | 根系、幼苗与林间采集路 | 确认来者使用力量时仍能照看同行者与脚下生灵 | 森印记录生长的见证，根系消息接续；采集与回营的路需要共同照料 |
| `tidal-warden` | 潮头、水道和浅湾栖地 | 确认来者能承受冲击、留出喘息空间，也理解调水对生命的影响 | 潮印记录来往的见证；水路恢复流动，栖地与通行都保有余地 |
| `crystal-warden` | 导能核心、信息线路和承载限度 | 确认来者懂得集中与节制力量，能维持网络 | 晶印记录传递的见证；岛上各处的消息重新送向灯塔 |
| `lighthouse-warden` | 进入灯芯的同行联系 | 三印见证之后，核验旅人与伙伴能否在强回响中相互回应 | 第四印认可联系，通向最终战场；观测者仍需重新面对旧保护结论 |
| `final-boss` | 同一观测者对全岛保护的解释 | 它认为外海风险未归零，继续封闭所有出入；旅人必须让被压住的求援和选择重新被听见 | 灯塔保留风浪预警并解除封闭，恢复航路与求援回应 |

区域守卫通过动作、光纹和场景叙述回应，没有新增其人形立绘或额外口头性格。观测者的声音使用 `narrator` 页面明确引用，现有对白 UI 可以直接显示，不依赖新增头像或 speaker 类型。

终局保留已有战斗顺序：明显蓄能预警 → 下一轮冲击 → 出战伙伴倒下、静默 → 玩家呼唤初始伙伴 → 自愿回应与超进化、队伍恢复 → 玩家继续指挥并可使用“归航回响”完成反击。叙事服务不自动执行最后行动，也不绕过真实战斗结果。

## API 和调用时机

```csharp
OpeningStory DesignPackNarrative.CreateOpeningStory();
StoryLine[] DesignPackNarrative.BeforeGuardian(string guardianId);
StoryLine[] DesignPackNarrative.AfterGuardian(string guardianId);
StoryLine[] DesignPackNarrative.Observe(string stablePoiId);
string DesignPackNarrative.ChapterObjective(SealProgression seals, bool completed);
```

- `CreateOpeningStory()` 每次创建独立运行时故事。全部原 beat ID 保留；替换 `arrival`、`watcher`、`guide`、`gather`、`ingredients`、`food-ready`、`crab`、`captured`、`report` 九组文本，保留三位初始伙伴的六组初见/同行文本。`{partner}` 仍由 `OpeningStory.Get` 替换。调用方负责销毁运行时 SO；原 `OpeningStory.asset` 保留。
- 新开场任务地理采用营地采莓/露水、M01 溪边救助，仍保留原捕获教学 beat 和阶段回调。地图逻辑、教程目标和拾光路线的地理用词应与此同步。
- `BeforeGuardian` 在真正开始战斗之前展示；完整读完后再启动遭遇。支持四个守卫 ID 及 `final-boss`。
- `AfterGuardian` 只在实际胜利且奖励/封印状态已落地后展示。读完才记录该段已读；读到一半退出，读档后仍需有机会接续或重播，不重新发奖励。
- `Observe` 覆盖原 Layout/Interactions 全部 43 个唯一 POI ID。未知 ID 返回空数组，由调用方正常继续，不弹无关的通用剧情。
- `ChapterObjective` 以封印集合判断缺项，不给林地/遗迹强加先后。三处齐全提示灯塔第四试炼，第四印后提示最终战场，通关后提示自由探索与回营。

一次性状态建议由会话层使用 `guardian-before:{id}`、`guardian-after:{id}`、`observe:{poiId}` 等稳定键记录。核心对白完成回调再标记已读；读物允许重读。战斗进度、机关完成和对白已读是不同状态，保存时应保留尚未读完的成功交接。

## POI 对白职责

| 地图 | 已覆盖的稳定 ID | 内容与触发说明 |
|---|---|---|
| M00 | `M00_SPAWN_DOCK`、`M00_CAMPFIRE`、`M00_SUPPLY_SHOP`、`M00_TRAVELER_HUT`、`M00_SHIGUANG_STATION`、`M00_STORAGE`、`M00_NOTICE`、`M00_GATHER_SHELL` | 求援回执、归处、材料与补给的生活用途，以及对伙伴和采集地的照料 |
| M01 | `M01_WEST_ENTRY`、`M01_BRIDGE`、`M01_GATHER_MEADOW`、`M01_ENCOUNTER_GRASS`、`M01_LOOKOUT`、`M01_TREASURE`、`M01_NORTH_EXIT`、`M01_EAST_EXIT` | 交通枢纽、两支线自由选择、巡路者的物资和维护痕迹、观察野生伙伴 |
| M02 | `M02_SOUTH_ENTRY`、`M02_FIREFLY_MEADOW`、`M02_ANCIENT_GROVE`、`M02_ROOT_BRIDGE`、`M02_WATERFALL_LOOKOUT`、`M02_HIDDEN_CLEARING`、`M02_SEAL_GATE`、`M02_GUARDIAN`、`M02_CHEST` | 根系与水网相连、采集需要留根、森林守护并未因试炼失败而消失 |
| M03 | `M03_WEST_ENTRY`、`M03_TIDE_TEMPLE`、`M03_WATER_CHANNEL`、`M03_FLOATING_PLATFORMS`、`M03_TIDE_GUARDIAN`、`M03_CRYSTAL_ARCHIVE`、`M03_CONDUIT`、`M03_CRYSTAL_GUARDIAN`、`M03_NORTH_EXIT` | 双翼自由寻访、渠道对栖地的影响、晶能承载、档案揭示求援被保护协议搁置 |
| M04 | `M04_SOUTH_ENTRY`、`M04_ASCENT`、`M04_FORECOURT`、`M04_CAMP`、`M04_RESONANCE_PLAZA`、`M04_THREE_SEAL_GATE`、`M04_LIGHTHOUSE_FORECOURT`、`M04_FINAL_ARENA`、`M04_BOSS` | 旧值守约定、协议失去反馈的过程、三印汇流、第四试炼与最终回应 |

以下五个 `Observe` 返回的是**成功后的文本**，调用方应先实际完成条件/世界变更，再展示：

1. `M02_ROOT_BRIDGE`：伙伴能力成功接续根桥/开放路线后。
2. `M03_WATER_CHANNEL`：调水成功、对应通路状态改变后。
3. `M03_CONDUIT`：导能成功、导流状态改变后。
4. `M04_RESONANCE_PLAZA`：三印条件满足并完成共鸣交接后。
5. `M04_THREE_SEAL_GATE`：门实际解除碰撞后。

条件不足时继续使用会话层的缺能力/缺封印提示，不能提前展示成功描述。其余读物不推断当前持印顺序；守卫 POI 的观察页复用对应战前对白，实际战斗仍由会话入口控制。

## 已接入的剧情入口与战后回报

0.4.1补齐档案馆、登塔阶道和前庭回廊的可见阅读点；古树环庭先呈现根系线索，再检查存活伙伴能力。森印实际胜利后，古树周围幼苗、花丛和局部灯光恢复，开放森息药草：采花粉×2、存活伙伴各恢复4HP，120秒刷新时间持续保存。告示板和营地NPC回应已获得的封印；相同进度重复找Watcher直接进入照护。

隔离Unity验证已实际运行：上述剧情流程4项、相关开场/世界会话19项通过；记录中断重读、真实守卫胜利即时变化、药草锁定/采集/读档及重复交谈均有覆盖。集成工程还会结合最新场景与美术做总体验证。`Observe`返回43个POI的台词是内容覆盖，不意味着43处都弹出对白；出口、服务与记录使用各自实际入口。

## 验证边界

对白不自行模拟生态变化；地图开关、光效、水道表现、胜利后的可通行状态须由世界逻辑实际提供。当前森印的可见回报已实现；潮印、晶印和最终归航仍需继续扩展独立的战后环境变化。完整第一章人工通关、自由分支叙事节奏与时长未验收，不以文档或单项流程通过代替整章完成。
