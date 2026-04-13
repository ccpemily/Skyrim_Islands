# Skyrim Islands 实现记录

## 项目定位

`Skyrim Islands` 是一个强依赖 `Odyssey DLC` 的 `RimWorld 1.6` 模组。当前已经确定的主线目标，是把玩家从地表殖民地迁移到一个独立空岛层，并在该层上展开后续的地图、资源、任务和移动玩法。

本文件只保留当前仍有效的实现方案与已落地结果，不再记录已经废弃的尝试路线。

## 已确定的总体方案

### 1. 世界层方案

- 空岛使用独立的 `PlanetLayer`，不伪装成普通地表聚落。
- 空岛层在运行时注册，并与地表层、轨道层建立连接关系。
- 世界缩放切层链固定为：`地表层 <-> 空岛层 <-> 轨道层`。
- 空岛世界对象使用 `SkyIslandMapParent`，作为后续地图生成、视角切换和世界层定位的唯一入口。
- 从空岛小地图切回世界时，背景世界视角应始终纠正到空岛层，而不是回到地表层。

对应实现：

- `Source/World/WorldComponent_SkyIslands.cs`
- `Source/World/SkyIslandMapParent.cs`
- `1.6/Defs/PlanetLayerDefs/PlanetLayerDefs_SkyrimIslands.xml`
- `1.6/Defs/WorldObjectDefs/WorldObjects_SkyrimIslands.xml`

### 2. 空岛地图方案

- 空岛地图继续使用“核心区 + 浮空平台 + 云海背景”三层结构。
- 核心区为半径 `6` 的圆形区域，使用自定义地形 `SkyrimIslands_IslandCore`。
- 核心区正中心固定生成 `3x3` 的核心建筑 `SkyrimIslands_FloatingEnergySpire`。
- 外围可建造平台使用自定义地形 `SkyrimIslands_FloatingPlatform`，围绕核心区形成一圈更厚的平台带。
- 外圈边缘使用按角度分段的随机半径扰动，形成轻微凹凸，不使用纯规则圆边。
- 云海作为不可通行、不可建造区域，视觉表现由背景层接管，不在地格本身重复渲染。
- 云海上的坠毁残骸类杂物应视为无效落点结果并立即清理。
- 地图生成继续由专用 `MapGeneratorDef` 和 `GenStep` 驱动。

对应实现：

- `Source/MapGen/GenStep_SkyIslandBase.cs`
- `Source/MapGen/GenStep_SkyIslandCore.cs`
- `Source/MapGen/SkyIslandShapeUtility.cs`
- `1.6/Defs/MapGeneration/MapGeneration_SkyrimIslands.xml`
- `1.6/Defs/TerrainDefs/Terrain_SkyrimIslands.xml`
- `1.6/Defs/ThingDefs_Buildings/Buildings_SkyrimIslands_Core.xml`

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

### 4. 空岛研究方案

- 空岛研究使用独立 `ResearchTabDef`，统一承载空岛专属科技。
- 当前研究页按三条纵向泳道组织：
  - 尖塔研究数据
  - 云海研究数据
  - 节点观测数据
- 每条泳道都维护自己的“当前项目”，不复用原版单一 `currentProj` 作为唯一状态来源。
- 左下详情区保留原版研究页结构，但改为同时显示三条当前研究进度，并用对应数据颜色提升辨识度。
- 空岛研究项目详情会在描述后补充“研究类型图标 + 研究类型名”。
- 普通研究台工作不会再无条件抢占空岛调查；只有当当前地图上确实存在可执行的空岛调查对象时，研究小人才会优先去做空岛调查。

对应实现：

- `Source/Research/GameComponent_SkyIslandResearch.cs`
- `Source/Research/SkyIslandResearchProjectDef.cs`
- `Source/Research/SkyIslandDataTypeDef.cs`
- `Source/Patches/SkyIslandResearchProjectDef_Patches.cs`
- `Source/Patches/CloudSeaFishing_Patches.cs`
- `1.6/Defs/ResearchTabDefs/ResearchTabs_SkyrimIslands.xml`
- `1.6/Defs/SkyIslandDataTypeDefs/SkyIslandDataTypes_SkyrimIslands.xml`
- `1.6/Defs/ResearchProjectDefs/ResearchProjects_SkyrimIslands.xml`

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
- 如果进入原版 fallback 分支，会打印一条 `Warning` 便于调试落点异常。

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
- 可装行的数量按钮遵循原版载重限制逻辑，支持 `>>M / M<` 这类“到最大载重”为止的快速调整。

对应实现：

- `Source/Quests/Initial/Shuttle/Dialog_SkyIslandLoadTransporters.cs`

### 3. 起飞、世界飞行与抵达

- 点击发射后，先播放地图内起飞动画。
- 起飞后进入自定义长转场。
- 长转场结束时切入世界地图，不立即切入目标空岛地图。
- 世界地图阶段播放穿梭机大地图飞行 cutscene，镜头跟随飞行中的世界对象。
- 黑屏长转场本身不暂停游戏模拟，避免阻塞穿梭机离图与世界对象生成。
- 进入世界飞行阶段前会短暂重试查找 `TravellingTransporters`，避免起飞时序导致空对象报错。
- 目标空岛地图在飞行结束时才实际生成，避免提前触发不必要的“生成地图中”过场。
- 抵达空岛后，切入空岛小地图，播放 `Ship_ShuttleCrashing` 坠毁到达动画并卸载内容。
- 原起飞穿梭机本体不会在坠毁落地时再次吐出。
- 世界飞行 cutscene 只在开头设置一次初始相机高度，后续允许玩家自行调整缩放视场。

对应实现：

- `Source/Quests/Initial/SkyIslandMigrationUtility.cs`
- `Source/Quests/Initial/Screen_SkyIslandMigrationCinematics.cs`
- `Source/Quests/Initial/Screen_SkyIslandWorldFlightCutscene.cs`
- `Source/Quests/Initial/TransportersArrivalAction_SkyIslandMigration.cs`
- `Source/Patches/CloudSeaDebrisPatch.cs`

## 当前已落地内容

- 模组入口、Harmony 初始化、`DefOf`、Debug 输出链已经稳定。
- 空岛层运行时注册、世界对象、Biome、Weather、云层绘制链已接通。
- 世界缩放切层顺序已经更新为 `地表 -> 空岛 -> 轨道` 与反向链路。
- 空岛地图生成器已经改成圆形核心、加厚外圈平台和随机凹凸边缘。
- 地形用途已经重新调整：
  - `IslandCore` 顶部使用机械族设施风格，非可耕种
  - `FloatingPlatform` 顶部使用肥沃泥土，可耕种
- 浮空能量尖塔已作为中心固定建筑接入地图生成，并使用专用贴图。
- 空岛科技页已经落地，支持三条研究数据泳道与并列进度显示。
- 尖塔研究线已经形成首轮可跑通闭环：
  1. 初步调查
  2. 尖塔阶段研究
  3. 阶段推进操作
  4. 最终重启
- 尖塔研究已接入专用工作对象、研究进度与阶段任务推进。
- 云海研究 demo 已接入：
  - 新研究 `云海垂钓`
  - 新建筑 `气象监测仪`
  - 监测仪会随时间积累数据，满后可被研究员调查并一次性提供云海研究数据
  - `云海垂钓` 研究完成后可在云海上使用原版钓鱼区设计器
- 云海上的坠毁钢渣与建筑碎屑会自动清理。
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
- 空岛研究目前只完成了尖塔线和云海 demo，节点观测泳道仍是占位框架。
- 云海研究目前只有最小可用闭环，尚未接入更多研究项目、更多数据来源建筑或更细的调查反馈。
- `气象监测仪` 目前使用原版 1x1 建筑贴图占位，尚未替换为专用美术资源。
- 资源系统、空岛移动、平台规则、袭击平台、后续特殊任务尚未进入正式实现。
- 语言文件、贴图资源、发布整理仍未完成。

## 下一步计划

下一阶段进入“空岛研究与后续资源系统扩展”，优先顺序如下：

1. 扩展云海研究链
- 在 `云海垂钓` 之外继续补充至少一到两项真正消耗云海研究数据的科技。
- 让云海泳道形成“数据积累 -> 调查 -> 研究解锁 -> 新玩法入口”的连续闭环。

2. 扩展研究数据来源
- 为云海、节点观测等泳道增加更多可调查对象，而不只依赖单一建筑。
- 明确不同数据来源之间的节奏差异，例如持续积累型、手动推进型、事件奖励型。

3. 完善空岛研究 UI
- 继续优化空岛科技页的可读性，包括泳道辨识、当前项目提示、类型标记和状态反馈。
- 评估是否需要在右侧项目卡或左下进度区继续补充更明确的视觉区分。

4. 把研究系统接入后续资源与剧情
- 为后续空岛资源链、特殊建筑、剧情推进和世界层玩法提供统一的研究数据入口。
- 不在这一阶段过度扩展所有玩法，而是优先保证研究系统继续扩展时结构稳定。

## 文档使用原则

- 本文件记录“当前有效的实现方案”和“已经落地的结果”。
- 若后续实现方向发生变化，应直接覆盖本文件对应段落，不保留历史备选路线。
