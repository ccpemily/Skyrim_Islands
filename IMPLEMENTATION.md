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
- **空岛移动系统已经实现并落地：**
  - `CompSkyIslandMovement` 挂载于 `SkyIslandMapParent`，负责连续移动状态
  - 使用基于 tiles/h 的轨迹规划与 `Vector3.Slerp` 球面插值实现平滑移动
  - 状态机包含 `Idle / Accelerating / Cruising / Decelerating / Docking / Interrupting`，并附加纵向状态 `Ascending / Descending / Holding`
  - 横向支持 4 个档位（前进一至前进四），各有独立的 `最大速度` 与 `加速度`
  - 纵向高度以 `1 km/h` 匀速变化；横纵向独立计算时间后以较长者为总时间，非瓶颈轴速度被压缩，保证两轴同时完成
  - 泊入段规则：横向剩余距离 `≤ 1 tile` 时进入泊入，固定以 `0.5 tiles/h` 走完最后 1 tile，耗时 2 小时
  - 加速/巡航段支持动态换挡：以当前位置和当前速度为新的起点重新规划剩余轨迹；减速/泊入段禁止换挡
  - 路径规划基于地表层选点，内部映射到 sky tile，最多支持 10 个路径点
  - `GameComponent_SkyIslandMovement` 提供路径规划模式，支持添加/删除路径点
  - 空岛图标启用 `useDynamicDrawer`，`DrawPos` 与 `WorldCameraPosition` 随连续位置实时更新
  - `SkyIslandMovementRenderUtility` 在世界地图上绘制路径预览、地面投影标记和天空层连线
  - 空岛总控界面 `Window_SkyIslandControl` 已接入：支持查看状态、规划路径、中断移动与档位切换
  - 删除独立的“启动引擎”按钮：空闲状态下点击档位横条中的非停止档位，若已有规划路线则按该档位直接启动引擎；若无路线则提示拒绝
  - 选中档位高亮为绿色；停止/中断档位为红色
  - 总控界面小地图位于移动面板左侧，地块显示中心下移到靠下四分之一；兴趣点按可见距离衰减透明度，支持悬停高亮与提示
  - 档位控制改为小地图下方的横条：左侧粗红线为中断，右侧 4 条白线对应 4 个档位，停止时默认选中中断位，选中档位高亮放大
  - 浮动按钮 `Window_SkyIslandControlButton` 已修复殖民地选中物体后的无响应问题
- **平滑本地时间系统已经实现并落地：**
  - `SkyIslandLocalTimeUtility` 提供基于连续地面投影经纬度的全套本地时间计算
  - Harmony 补丁覆盖了 `GenLocalDate` 的所有常用 `Map` 重载、`GenCelestial.CelestialSunGlow(Map, int)` 以及 `DateReadout.DateOnGUI`
  - 空岛小地图内的日期、时钟、季节显示和天空亮度均随连续位置平滑过渡，不再随 tile 切换跳变

## 当前仍需继续开发的部分

- 空岛层高度、背景世界偏移、世界与小地图之间的视角参数仍需继续调优。
- 空岛天气、云层速度、天空滤镜和整体视觉观感仍需继续定稿。
- 空岛研究目前只完成了尖塔线和云海 demo，节点观测泳道仍是占位框架。
- 云海研究目前只有最小可用闭环，尚未接入更多研究项目、更多数据来源建筑或更细的调查反馈。
- `气象监测仪` 目前使用原版 1x1 建筑贴图占位，尚未替换为专用美术资源。
- **空岛移动系统待完善：**
  - 对地交互（贸易、任务接入、袭击/ docked 交互）尚未接入
  - 移动的成本与资源消耗尚未设计
  - 空岛在世界地图上与其他世界对象的碰撞/规避规则尚未实现
- **平滑本地时间系统待完善：**
  - 室外温度连续化（`MapTemperature` 补丁）尚未实现
  - 植物生长季、动物适温、天气权重等更深层生态系统仍按逻辑 tile 离散运作
- 资源系统、平台规则、袭击平台、后续特殊任务尚未进入正式实现。
- 语言文件、贴图资源、发布整理仍未完成。

## 下一步计划

下一阶段进入“空岛移动完善与资源系统扩展”，优先顺序如下：

1. 完善空岛移动系统的剩余模块
- 实现离散高度状态（高空巡航 / 低空悬停 / 交互高度）。
- 基于“地面投影 tile + 高度门槛”接入对地交互（贸易、 docking、任务接入等）。
- 为移动补充资源/燃料消耗规则与成本模型。

2. 完成平滑本地时间的最后一环
- 实现 `MapTemperature.OutdoorTemp` / `SeasonalTemp` 的连续化补丁，让空岛地图的室外温度随连续位置平滑过渡。
- 评估是否需要把植物生长季、动物适温等系统逐步纳入连续位置体系。

3. 扩展云海研究链
- 在 `云海垂钓` 之外继续补充至少一到两项真正消耗云海研究数据的科技。
- 让云海泳道形成“数据积累 -> 调查 -> 研究解锁 -> 新玩法入口”的连续闭环。

4. 扩展研究数据来源
- 为云海、节点观测等泳道增加更多可调查对象，而不只依赖单一建筑。
- 明确不同数据来源之间的节奏差异，例如持续积累型、手动推进型、事件奖励型。

5. 把研究系统接入后续资源与剧情
- 为后续空岛资源链、特殊建筑、剧情推进和世界层玩法提供统一的研究数据入口。
- 不在这一阶段过度扩展所有玩法，而是优先保证研究系统继续扩展时结构稳定。

## 文档使用原则

- 本文件记录“当前有效的实现方案”和“已经落地的结果”。
- 若后续实现方向发生变化，应直接覆盖本文件对应段落，不保留历史备选路线。

## 空岛移动系统

### 总体架构

- 空岛继续使用唯一的 `SkyIslandMapParent` 作为真实世界对象，不拆分成“固定殖民地 + 另一个移动图标”两套所有权。
- 移动能力通过 `CompSkyIslandMovement`（`WorldObjectComp`）挂载到 `SkyIslandMapParent` 上，并在 `WorldObjects_SkyrimIslands.xml` 中通过 `CompProperties_SkyIslandMovement` 注册。
- 地图、派系、任务引用、存档引用都继续挂在 `SkyIslandMapParent` 上；`CompSkyIslandMovement` 只负责序列化移动状态。
- 核心驱动 `SkyIslandMovementDriver` 完全重写为基于**tiles/h 的轨迹规划**体系，取代旧的单一角速度模型。

### 连续移动机制（新版物理模型）

- 移动不使用离散 tile 跳跃，而是基于**球面坐标插值**的连续位置系统。
- 核心状态字段包括：
  - `departureSkyDir / targetSkyDir`：起点与终点的天空方向
  - `departureAltitude / targetAltitude`：起点与目标高度
  - `gearIndex`：当前横向档位（0 ~ 3）
  - `totalDurationHours / elapsedHours`：总移动时间与已用时间
  - `scaledMaxSpeedH / scaledAccelH`：经瓶颈压缩后的横向最大速度与加速度
  - `verticalSpeed`：经压缩后的纵向速度
- 横向支持 4 个档位，各有 `最大速度 (tiles/h)` 与 `加速度 (tiles/h²)`：
  - 前进一：`2 / 1`
  - 前进二：`5 / 1.5`
  - 前进三：`10 / 2`
  - 前进四：`20 / 3`
- 纵向速度：固定 `1 km/h = 1 高度单位/h`，无独立加减速；若横向所需时间更长，则纵向速度按压缩比例降低，保证两轴同时完成。
- 瓶颈与同步机制：
  - 分别计算横向独立时间 `T_h`（含正常加减速段 + 固定 2h 泊入段）与纵向独立时间 `T_v = |Δaltitude| / 1`
  - 总时间 `T_total = max(T_h, T_v)`
  - 若 `T_v > T_h`，横向速度与加速度按 `scale = T_h / T_v` 压缩；否则纵向速度被压缩为 `|Δaltitude| / T_h`
- 泊入段：当横向剩余距离 `≤ 1 tile` 时进入 `Docking` 状态，以固定 `0.5 tiles/h` 匀速走完最后 1 tile，耗时 2 小时。
- 移动状态机：
  - `Idle`：待命，无移动
  - `Accelerating`：向目标加速
  - `Cruising`：达到巡航速度
  - `Decelerating`：接近目标时减速
  - `Docking`：泊入最后 1 tile
  - `Interrupting`：玩家手动中断，减速回最近的锚定位置后停止
- 纵向状态：
  - `Holding`：高度不变
  - `Ascending`：高度上升
  - `Descending`：高度下降
- 逻辑 `Tile` 随当前天空方向通过邻域爬山法实时更新，用于原版兼容、事件触发和存档；视觉位置则由 `Vector3.Slerp` 直接插值计算。

### 动态换挡

- 当前档位为全局状态，不绑定具体路径点，默认“前进二”。
- 启动引擎时按当前档位计算初始轨迹。
- 在 `Accelerating` 或 `Cruising` 状态下，玩家可在总控界面切换档位：
  - 以当前位置、当前速度和剩余目标为新的起点重新调用 `CalculateMovementProfile(...)`
  - 状态平滑切换，不跳跃、不重置
- 在 `Decelerating` 或 `Docking` 状态下，换挡按钮置灰，禁止切换。
- 中断移动后回到 `Idle`，可再次自由换挡。

### 路径规划

- `GameComponent_SkyIslandMovement` 作为高层协调器，管理“规划模式”。
- 玩家通过空岛总控界面的“进入规划模式”按钮，或在空岛层选中空岛后操作，切换到大地的 `Surface` 层进行选点。
- 规划模式下：
  - 右键地面 tile 添加路径点
  - 右键已有路径点可删除或在重叠位置新建
  - ESC 退出规划并返回空岛层视角
- 路径点存储在地表层，内部通过 `GetSkyProjectionTile` 映射到最近的天空层 tile。
- 每个路径点携带高度信息（当前通过 `SkyIslandAltitude.DefaultAltitude`，后续可扩展为交互时选择）。
- 启动引擎后，空岛按顺序逐个抵达路径点，每到一个点自动停止并发送信件提示。

### 世界图标与背景板

- `WorldObjectDef` 启用了 `useDynamicDrawer`，空岛世界图标不再依赖静态网格 `Print`。
- `SkyIslandMapParent.DrawPos` 和 `WorldCameraPosition` 都返回 `CompSkyIslandMovement.CurrentSkyWorldPosition`，因此：
  - 世界地图上的空岛图标会随连续位置平滑移动
  - 小地图背景世界的视角会随空岛移动实时变化
- `WorldObject_SkyLayerVisibilityPatch` 确保当地表层被选中时，空岛世界对象仍然可见。

### 渲染与界面

- `SkyIslandMovementRenderUtility` 负责在世界地图上绘制：
  - 当前位置与天空投影的配对标记
  - 已规划路径点的配对标记
  - 地面投影与天空层之间的连线
  - 天空层路径点之间的弧形航线
- `Window_SkyIslandControl`（空岛总控界面）提供“总控面板”和“移动状态”两个标签页：
  - 左栏显示当前坐标、地面投影、移动状态（横向 + 纵向组合显示）、速度、ETA
  - 移动状态页左侧为小地图与档位横条，右侧为操作按钮与状态摘要
  - 小地图中地块显示中心下移到靠下四分之一，兴趣点按距离衰减透明度，支持悬停高亮与原版 tooltip
  - 档位横条位于小地图下方：左侧粗红线为中断，右侧 4 条白线对应前进一至前进四；停止时默认选中中断位，选中档位高亮为绿色并放大
  - 空闲状态下点击档位：若已有路线则按该档位直接启动引擎；若无路线则弹出提示并拒绝操作
  - 支持进入规划模式、清空路线、中断移动与动态换挡
  - 下方路线摘要以列表形式展示当前位置与各路径点的地面对应与高度
- `Window_SkyIslandControlButton` 是一个常驻的右上角浮动按钮，用于快速打开/关闭总控界面。
- 浮动按钮修复了殖民地视角下选中物体后无响应的问题：将窗口层级从 `WindowLayer.GameUI` 提升到 `WindowLayer.Super`，并改用手动 `MouseDown` 事件处理。

### Tooltip 稳定性处理

- 所有动态数值 tooltip（高度条、日期读数、小地图兴趣点）统一采用原版的 `new TipSignal(Func<string>, stableUniqueId)` 模式。
- 稳定 `uniqueId` 确保 `TooltipHandler` 在内容变化时不会销毁并重建提示框实例，从而彻底消除数值更新时的闪烁。
- 该处理方式与 `MiliraXian_Characters`（清荷 Status Gizmo）中的 tooltip 实践一致。

### 对应实现

- `Source/World/CompSkyIslandMovement.cs`
- `Source/World/CompProperties_SkyIslandMovement.cs`
- `Source/World/GameComponent_SkyIslandMovement.cs`
- `Source/World/SkyIslandMovementDriver.cs`
- `Source/World/SkyIslandMovementConstants.cs`
- `Source/World/SkyIslandWaypointPlanner.cs`
- `Source/World/SkyIslandMovementRenderUtility.cs`
- `Source/UI/Window_SkyIslandControl.cs`
- `Source/UI/Window_SkyIslandControlButton.cs`
- `Source/UI/SkyIslandControlWindowUtility.cs`
- `Source/UI/SkyIslandControlButtonDrawer.cs`
- `Source/UI/SkyIslandMinimapUtility.cs`
- `Source/Patches/WorldInterface_SkyIslandMovementPatches.cs`
- `Source/Patches/UIRoot_Play_SkyIslandControlButtonPatch.cs`
- `Source/Patches/WorldObject_SkyLayerVisibilityPatch.cs`
- `Source/SkyrimIslandsTextureCache.cs`
- `1.6/Defs/WorldObjectDefs/WorldObjects_SkyrimIslands.xml`

## 平滑本地时间系统

### 设计目标

- 空岛在穿越不同时区时，小地图中的本地时间、日期、季节和天空亮度应表现为连续平滑过渡，而不是随 `map.Tile` 的离散切换发生跳变。
- 方案把“逻辑 tile”（用于原版兼容、事件、存档）与“连续时间位置”（用于显示和环境演算）明确拆分。

### 连续位置来源

- `CompSkyIslandMovement` 在移动过程中维护 `currentDirection`，并基于该方向计算 `CurrentSurfaceWorldPosition` 和 `CurrentSurfaceLongLat`。
- `SkyIslandMapParent` 将这些连续经纬度暴露给外部系统。
- `SkyIslandLocalTimeUtility.TryGetContinuousSurfaceLocation(Map, out Vector2 longLat)` 是统一入口：如果地图的父对象是 `SkyIslandMapParent`，则返回其连续地面投影经纬度；否则返回 false，让调用方回退到原版逻辑。

### 已接入的原版系统

- `GenLocalDate` 的常用 `Map` 重载全部被 Harmony Prefix 拦截，在命中空岛地图时路由到 `SkyIslandLocalTimeUtility`：
  - `DayOfYear`、`HourOfDay`、`DayOfTwelfth`、`Twelfth`、`Season`、`Year`
  - `DayOfSeason`、`DayOfQuadrum`、`DayTick`、`DayPercent`、`YearPercent`
  - `HourInteger`、`HourFloat`
- `GenCelestial.CelestialSunGlow(Map, int)` 同样被拦截，使用连续经纬度计算太阳光照强度。
- `DateReadout.DateOnGUI(...)` 被完全接管，使用连续经纬度重绘日期、时钟和季节文本，其余地图继续走原版。
- 由于 `SkyManager` 每帧都会读取 `GenLocalDate.DayPercent` 和 `GenCelestial.CurCelestialSunGlow`，天空亮度、阴影方向、水面受光都会自动随连续位置平滑变化，无需额外修改 `SkyManager`。

### 尚未实现的扩展

- `MapTemperature.OutdoorTemp` / `SeasonalTemp` 的连续化补丁尚未落地。原版温度不仅依赖经纬度，还包含 `tile.temperature`、季节振幅和按 tile 哈希的日随机扰动，需要专门设计插值策略。
- 植物生长季、动物适温、天气权重等更深层生态逻辑仍基于离散 `map.Tile` 运作，后续按需评估是否扩展。

### 对应实现

- `Source/World/SkyIslandLocalTimeUtility.cs`
- `Source/Patches/SkyIslandLocalTimePatches.cs`
