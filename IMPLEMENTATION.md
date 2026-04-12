# Skyrim Islands 实现记录

## 项目定位

`Skyrim Islands` 是一个强依赖 `Odyssey DLC` 的 `RimWorld 1.6` 模组。当前已经确定的主线目标，是把玩家从地表殖民地迁移到一个独立空岛层，并在该层上展开后续的地图、资源、任务和移动玩法。

本文件只保留当前仍有效的实现方案与已落地结果，不再记录已经废弃的尝试路线。

## 已确定的总体方案

### 1. 世界层方案

- 空岛使用独立的 `PlanetLayer`，不伪装成普通地表聚落。
- 空岛层在运行时注册，并与地表层、轨道层建立连接关系。
- 空岛世界对象使用 `SkyIslandMapParent`，作为后续地图生成、视角切换和世界层定位的唯一入口。
- 从空岛小地图切回世界时，背景世界视角应始终纠正到空岛层，而不是回到地表层。

对应实现：

- `Source/World/WorldComponent_SkyIslands.cs`
- `Source/World/SkyIslandMapParent.cs`
- `1.6/Defs/PlanetLayerDefs/PlanetLayerDefs_SkyrimIslands.xml`
- `1.6/Defs/WorldObjectDefs/WorldObjects_SkyrimIslands.xml`

### 2. 空岛地图方案

- 空岛地图继续使用“核心区 + 浮空平台 + 云海背景”三层结构。
- 核心区为固定可种植区域，使用自定义地形 `SkyrimIslands_IslandCore`。
- 外围可建造平台使用自定义地形 `SkyrimIslands_FloatingPlatform`。
- 云海作为不可通行、不可建造区域，视觉表现由背景层接管，不在地格本身重复渲染。
- 地图生成继续由专用 `MapGeneratorDef` 和 `GenStep` 驱动。

对应实现：

- `Source/MapGen/GenStep_SkyIslandBase.cs`
- `Source/MapGen/GenStep_SkyIslandCore.cs`
- `1.6/Defs/MapGeneration/MapGeneration_SkyrimIslands.xml`
- `1.6/Defs/TerrainDefs/Terrain_SkyrimIslands.xml`

### 3. Biome / Weather 方案

- 空岛使用独立 `BiomeDef` 与 `BiomeWorker`，不直接复用地表生物群系。
- 空岛天气使用独立 `WeatherDef`，由空岛 biome 控制局部表现，不改全局天气系统。
- 空岛世界云层使用独立 `WorldDrawLayer`，并禁用原版世界云层以避免叠层冲突。

对应实现：

- `Source/World/BiomeWorker_SkyIsland.cs`
- `Source/World/WorldDrawLayer_SkyIslandClouds.cs`
- `1.6/Defs/BiomeDefs/Biomes_SkyrimIslands.xml`
- `1.6/Defs/WeatherDefs/Weather_SkyrimIslands.xml`
- `1.6/Patches/Patch_DisableVanillaWorldClouds.xml`

## 开局任务链最终方案

### 1. 任务出现方式

- 新游戏开始后，不立即弹出独立提示信。
- 经过短暂延迟后，正式创建任务 `SkyrimIslands_MigrationQuest`。
- 任务使用绑定 quest 的标准 Letter 提醒玩家，信件打开后应先显示说明窗口，并可跳转到任务页。
- 任务无接受期限。

对应实现：

- `Source/Quests/Initial/GameComponent_SkyIslandFlow.cs`
- `1.6/Defs/QuestScriptDefs/QuestScripts_SkyrimIslands.xml`

### 2. 任务生效范围

- 该任务只负责“把未知穿梭机送到玩家初始殖民地附近”。
- 玩家接受任务后，任务穿梭机以 `ShuttleIncoming` 形式降落。
- 穿梭机真正完成降落后，任务立即成功结束。
- 如果穿梭机在降落完成前被摧毁，任务直接失败。
- 任务结束后，后续是否装载、是否起飞、是否放弃原殖民地，不再由 quest 状态承载，而由穿梭机自身组件处理。

对应实现：

- `Source/Quests/Initial/QuestPart_SkyIslandMigration.cs`
- `Source/Quests/Initial/Shuttle/CompSkyIslandMissionShuttleControl.cs`

### 3. 初始穿梭机落点规则

- 初始穿梭机优先落在距离地图正中心 `10-20` 格的环形区域内。
- 候选落点必须同时满足：
  - 非迷雾区域
  - 可实际降落
  - 至少有一名已出生玩家殖民者可到达
- 如果该环形区域内没有合法位置，再扩大到更宽的近中心区域继续搜索。
- 原版 `DropCellFinder.GetBestShuttleLandingSpot(...)` 仅作为最后 fallback。

对应实现：

- `Source/Quests/Initial/SkyIslandMigrationUtility.cs`

## 穿梭机迁移链最终方案

### 1. 装载与发射

- 初始任务穿梭机使用自定义 Def `SkyrimIslands_MissionShuttle`，不直接改原版 `Shuttle`。
- 装载逻辑建立在原版 `CompTransporter / CompShuttle / EnterTransporter` 之上。
- 穿梭机提供两个自定义入口：
  - `装载穿梭机 / 调整装载`
  - `启动发射程序`
- 至少需要装载一名殖民者，才能启动迁移。
- 发射前由玩家确认是否在迁移后删除原地表殖民地。

对应实现：

- `1.6/Defs/ThingDefs_Buildings/Buildings_SkyrimIslands_MissionShuttle.xml`
- `Source/Quests/Initial/Shuttle/CompSkyIslandMissionTransporter.cs`
- `Source/Quests/Initial/Shuttle/CompSkyIslandMissionShuttleControl.cs`
- `Source/Quests/Initial/Shuttle/Command_SkyIslandLoadToTransporter.cs`

### 2. 装载界面方案

- 装载界面保留原版 `Dialog_LoadTransporters` 的基本交互模型，但改为任务穿梭机专用版本。
- 人员页继续沿用原版分段展示逻辑。
- 物品页允许同类物品合并显示，并支持堆叠数量调整。
- 对同类物品，如果一部分可装、一部分不可装，则拆成两行：
  - 可装行：允许调整数量，并受剩余载重限制
  - 不可装行：仅展示，不允许调整，并显示 `已禁用`
- 打开窗口后只检测一次可达性，不在暂停状态下每帧重复计算。

对应实现：

- `Source/Quests/Initial/Shuttle/Dialog_SkyIslandLoadTransporters.cs`

### 3. 起飞、世界飞行与抵达

- 点击发射后，先播放地图内起飞动画。
- 起飞后进入自定义长转场。
- 长转场结束时切入世界地图，不立即切入目标空岛地图。
- 世界地图阶段播放穿梭机大地图飞行 cutscene，镜头跟随飞行中的世界对象。
- 目标空岛地图在飞行结束时才实际生成，避免提前触发不必要的“生成地图中”过场。
- 抵达空岛后，切入空岛小地图，播放 `Ship_ShuttleCrashing` 坠毁到达动画并卸载内容。
- 原起飞穿梭机本体不会在坠毁落地时再次吐出。

对应实现：

- `Source/Quests/Initial/SkyIslandMigrationUtility.cs`
- `Source/Quests/Initial/Screen_SkyIslandMigrationCinematics.cs`
- `Source/Quests/Initial/Screen_SkyIslandWorldFlightCutscene.cs`
- `Source/Quests/Initial/TransportersArrivalAction_SkyIslandMigration.cs`

## 当前已落地内容

- 模组入口、Harmony 初始化、`DefOf`、Debug 输出链已经稳定。
- 空岛层运行时注册、世界对象、Biome、Weather、云层绘制链已接通。
- 空岛地图生成器、核心区、平台、云海地形已接通。
- 初始任务链已经能完整跑通：
  1. 新游戏开始
  2. 延时生成可接取任务
  3. 玩家接受任务
  4. 自定义任务穿梭机在殖民地附近降落
  5. 任务完成，穿梭机转为玩家可操作载具
  6. 玩家装载殖民者与物资
  7. 玩家启动发射程序
  8. 播放起飞动画与长转场
  9. 切入大地图播放飞行 cutscene
  10. 生成目标空岛地图
  11. 坠毁穿梭机落地并卸载

## 当前仍需继续开发的部分

- 空岛层高度、背景世界偏移、世界与小地图之间的视角参数仍需继续调优。
- 空岛天气、云层速度、天空滤镜和整体视觉观感仍需继续定稿。
- 资源系统、空岛移动、核心建筑、平台规则、袭击平台、后续特殊任务尚未进入正式实现。
- 语言文件、贴图资源、发布整理仍未完成。

## 文档使用原则

- 本文件记录“当前有效的实现方案”和“已经落地的结果”。
- 若后续实现方向发生变化，应直接覆盖本文件对应段落，不保留历史备选路线。
