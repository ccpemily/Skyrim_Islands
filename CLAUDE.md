# CLAUDE.md

本文件为 Claude Code（claude.ai/code）在处理本仓库代码时提供指导。

## 前置说明

Thinking过程请使用中文。


## 构建、运行与调试

- **还原依赖：** `dotnet restore .\Skyrim_Islands.csproj`
- **构建：** `dotnet build .\Skyrim_Islands.csproj -c Debug --no-restore`
- **VS Code 任务：** 按 `Ctrl+Shift+B` 运行默认的 `build mod` 任务；`launch RimWorld` 会先构建再启动 RimWorld 开发版。
- **输出位置：** `1.6/Assemblies/Skyrim_Islands.dll` 和 `.pdb`
- **调试流程：** 项目使用 Unity Player 调试。先启动 RimWorld，然后在 VS Code 中使用 `Attach Unity Debugger`（或 `Ctrl+Shift+P → Attach Unity Debugger`）并选择正在运行的 `RimWorld player`。Unity 调试端口是动态的。
- **没有测试项目。** 请通过 Debug 构建和游戏中的手动测试来验证修改。

## 项目背景

`Skyrim Islands` 是一款需要 **Odyssey DLC** 的 RimWorld 1.6 Mod。它增加了一个自定义的天空岛殖民地层级，包含地图生成、研究系统、任务和世界地图移动。该 Mod 大量使用了 Harmony（`Lib.Harmony 2.4.2`）。

## 高层架构

### 天空岛世界层级

本 Mod 在运行时注册了一个自定义的 `PlanetLayerDef`（`SkyrimIslands_SkyLayer`）。缩放链固定为：地表（Surface）<-> 天空岛（Sky Island）<-> 轨道（Orbit）。

- `WorldComponent_SkyIslands` 管理层级注册、去重和连接规范化。它还通过 `CreateStartingSkyIslandAt` 创建初始天空岛。
- `SkyIslandLayerBootstrap` 处理运行时层级创建和缩放链的绑定。
- `SkyIslandMapParent`（继承自 `SpaceMapParent`）是玩家天空岛的唯一标准世界对象。所有地图绑定、派系所有权和存档引用都通过它进行。

### 地图生成

天空岛地图使用自定义的 `MapGeneratorDef` 和 GenSteps：

- `GenStep_SkyIslandBase` 构建岛屿形状。
- `GenStep_SkyIslandCore` 在中心放置固定的 3x3 `FloatingEnergySpire`。
- `SkyIslandShapeUtility` 提供径向形状数学计算（核心半径 6，加厚的平台边缘带有角度扰动）。

### 研究系统（3 线并行）

本 Mod 用自定义的 3 线 UI（尖塔、云海、节点观测）替代了原版天空岛科技的研究标签页体验。

- `SkyIslandResearchProjectDef` 扩展了 `ResearchProjectDef`，增加了 `skyIslandDataType` 和阶段字段。
- `GameComponent_SkyIslandResearch` 持有所有运行时研究状态：每种数据类型的当前项目、尖塔任务阶段推进，以及手动操作的门控（调查 -> 阶段研究 -> 手动推进 -> 下一阶段）。
- `SkyIslandResearchProjectDef_Patches.cs` 中的补丁拦截原版研究 UI，以渲染自定义标签页和多线进度显示。
- 研究进度通过 `GameComponent_SkyIslandResearch.AddSkyResearchProgress` 添加，完成时会弹出带标签的 Mote。

### 天空岛移动

天空岛在世界地图上移动，但不会离开天空层级。为了视觉上的平滑，移动使用**连续球面坐标**而非离散的瓦片跳跃。

- `CompSkyIslandMovement`（挂载在 `SkyIslandMapParent` 上）存储规划好的地表路径点、天空路径点、当前方向向量、角速度和移动状态（空闲/加速/巡航/减速/中断）。
- 玩家在地表层选择目的地；该组件内部将其投影到最近的天空层瓦片。
- `SkyIslandMapParent.DrawPos` 和 `WorldCameraPosition` 从 `CompSkyIslandMovement.CurrentSkyWorldPosition` 读取数据，因此随着岛屿移动，世界图标和地图背景会持续更新。
- `GameComponent_SkyIslandMovement` 作为更高层的协调器，在需要时可用。

### 连续本地时间

由于岛屿连续跨越经度移动，原版的 `GenLocalDate`（依赖于离散的 `map.Tile`）会导致一天中的时间和季节 UI 出现跳变。

- `SkyIslandLocalTimeUtility` 根据岛屿当前的连续经度/纬度计算连续的日期/时间值。
- `SkyIslandLocalTimePatches.cs` 为几乎所有 `GenLocalDate` 的 `Map` 重载以及 `GenCelestial.CelestialSunGlow` 添加了前缀补丁，将天空岛地图路由到上述工具类。
- `DateReadout.DateOnGUI` 也被补丁修改，以使用连续值绘制天空岛的日期读数。
- 温度连续性已规划但尚未完全接入；温度插值策略请参阅 `IMPLEMENTATION.md`。

### 任务组织

任务代码按剧情线分组存放在 `Source/Quests/` 下：

- `Initial/` — 移民穿梭机任务、着陆点逻辑、运输装载 UI、发射动画和世界飞行过场动画。
- `Spire/` — 浮空能量尖塔调查、研究工作和手动阶段推进操作。
- `CloudSea/WeatherMonitor/` — 气象监测建筑、调查工作和云海研究数据源。

### 补丁策略

大部分玩法改动通过 `Source/Patches/` 中的 Harmony 补丁实现：

- 层级引导：`WorldGrid_CreateRequiredLayers_SkyIslandsPatch`
- 研究 UI：`SkyIslandResearchProjectDef_Patches`
- 云海清理：`CloudSeaDebrisPatch`
- 云海钓鱼：`CloudSeaFishing_Patches`、`FishShadowComponentPatch`
- 移民时机：`TravellingTransporters_SkyIslandMigrationPatch`
- 移动渲染 / UI：`WorldInterface_SkyIslandMovementPatches`、`UIRoot_Play_SkyIslandControlButtonPatch`、`WorldObject_SkyLayerVisibilityPatch`
- 本地时间：`SkyIslandLocalTimePatches`

## 代码规范

- **缩进：** 4 个空格，大括号独占一行。
- **命名：** 类型/方法/属性使用 PascalCase；局部变量/参数/私有字段使用 camelCase。
- **命名空间：**
  - `Source/` 根目录文件：`SkyrimIslands`
  - `Source/*` 子目录：`SkyrimIslands.*`
  - 建筑：`SkyrimIslands.Buildings.$BUILDING_NAME$`
  - 任务：`SkyrimIslands.Quests.$QUEST_NAME$` 和 `SkyrimIslands.Quests.$QUEST_NAME$.$OBJECT_NAME$`
- **文件名：** 应与主要类型名称一致。
- **Thing/Building 代码位置：**
  - 若一个 `Thing`/`Building` **只在任务过程中出现，且不影响任务之外的任何游戏部分**（如未知穿梭机），可放在 `Source/Quests/$QUEST_NAME$/` 下。
  - 其余所有新增建筑（如浮空能量尖塔、气象监测仪）统一放在 `Source/Buildings/$BUILDING_NAME$/` 下。
- **纹理缓存：** 所有静态 `Texture2D` 缓存必须放在 `SkyrimIslandsTextureCache` 中，并带有 `[StaticConstructorOnStartup]`。
- **反射：** 在引入新的反射代码（如 `AccessTools`、`System.Reflection`）前请先询问用户。
- **RimWorld 源码查找：** 优先使用 `rimsearcher MCP` 而非普通文件搜索。如果它返回 `Transport closed`，请停止并向用户报告。
- **原版 Def 文件位置：** 相对于项目根目录为 `../../Data`，可用于查询原版 XML 定义（如 `PlanetLayerDef`、`WorldObjectDef` 等）。
- **文本编码：** `.cs`、`.xml`、`.md` 和本地化文件均使用 UTF-8。
- **Def 交叉引用：** 修改 XML 或 C# 后，请验证 `defName`、`Class`、`worldObjectClass`、`workerClass`、`compClass`、`layerType` 和 `DefOf` 字段在 C# 与 XML 之间保持一致。XML 中引用的 Mod 自有类型必须使用完全限定名（例如 `SkyrimIslands.World.SkyIslandMapParent`）。

## 任务与计划管理

- **当一条指令包含多个不同的 Bug 或改动时，请为每个独立项创建单独的 Task。** 不要把多个无关的修复合并成一个模糊的 Task。
- **计划必须拆分为详细、可执行的要点。** 每个要点应对应一个具体的文件或行为变更。

## 重要文件

- `IMPLEMENTATION.md` — 当前有效的设计决策和下一步计划（移动、研究、时间连续性等）
- `AGENTS.md` — 仓库协作规则和其他技术护栏
- `FEATURES.md` — 功能草案和范围
- `SkyrimIslandsDefOf.cs` — 集中式的 `DefOf` 类
- `SkyrimIslandsTextureCache.cs` — 集中式的纹理/材质缓存
