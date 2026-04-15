# Skyrim Islands 代码重构分析报告

> 本文档最后更新于 2026-04-15，基于当前 `main` 分支的最新代码状态。

## 一、已完成的重构与修复

### P0 — 已落地（架构层清理）

#### 1. `GameComponent_SkyIslandResearch` 尖塔研究泳道 Strategy 拆分（Stage 1~2）

**已完成。**

- 新增 `ISkyResearchCategory` 接口，定义泳道的生命周期方法（`IsProjectVisible`、`HasAvailableWork`、`AddProgress`、`CategoryTick` 等）。
- 新增 `SkyResearchCategoryRegistry`：持有并暴露尖塔、云海、节点观测三个 Category 实例。
- 新增 `SpireResearchCategory.cs`：将原 `GameComponent_SkyIslandResearch` 中约 300 行尖塔逻辑全部迁移至此，包括任务创建、阶段推进、手动操作状态机、存档兼容。
- 新增 `ISpireManualOperation` 与 4 个实现类（`BootstrapInvestigationOp`、`AdvanceToStage1Op`、`AdvanceToStage2Op`、`FinalRestartOp`）：彻底消除尖塔手动操作在 `GetCurrentOperationLabel` / `GetCurrentOperationDescription` / `NotifySpireManualOperationCompleted` 中的 switch 堆积。
- 新增 `ISkyResearchSource` 接口：`Building_FloatingEnergySpire` 与 `Building_WeatherMonitor` 均已实现该接口。`GameComponent_SkyIslandResearch` 不再硬编码引用具体建筑类型，研究可用性扫描改为通用的 `ISkyResearchSource` 遍历。
- `GameComponent_SkyIslandResearch` 已清理所有尖塔私有字段与方法，对外保留薄委托层（如 `GetCurrentManualOperation`、`ScheduleInitialSpireQuest` 等），旧存档通过 `MigrateFromLegacy` 在 `PostLoadInit` 时无损迁移。
- `CloudSeaResearchCategory` 与 `NodeObservationCategory` 已按命名空间分离（`SkyrimIslands.Research.Categories.CloudSea` / `.NodeObservation`）。

**验证结果：** 编译通过 0 错误，外部调用方（如 `Building_FloatingEnergySpire`）无需修改。

#### 2. 时间补丁层合并（原 15 个重复类 → 1 张配置表 + 2 个共享前缀）

**已完成。**

- 新增 `SkyIslandLocalTimeRedirectPrefix.cs`：利用 `MethodInfo __originalMethod` + `Dictionary<MethodInfo, Delegate>` 实现统一的类型分发前缀。
- 新增 `SkyIslandLocalTimePatchRegistry.cs`：在 Harmony 初始化阶段通过 `harmony.Patch()` 动态注册 13 个 `GenLocalDate` 重载 + `GenCelestial.CelestialSunGlow`。
- `SkyIslandLocalTimePatches.cs` 从 234 行缩减至约 15 行，仅保留 `DateReadout_DateOnGUI` 这一个无法复用泛型前缀的特殊补丁。
- `SkyrimIslandsHarmonyPatcher.cs` 在 `PatchAll()` 之后调用 `SkyIslandLocalTimePatchRegistry.Apply(harmony)`。

**验证结果：** 编译通过，游戏内空岛日期/时间/天空亮度随连续位置平滑过渡，无异常。

#### 3. 研究 UI 绘制逻辑从补丁层剥离

**已完成。**

- 新增 `Source/Research/SkyIslandResearchUIPresenter.cs`，将 `MainTabWindow_Research_DrawProjectInfo` 和 `ListProjects` 中约 270 行的绘制逻辑、反射缓存全部迁移至此。
- `SkyIslandResearchProjectDef_Patches.cs` 的对应补丁缩减为仅 2 行入口调用：判断 Tab → 调用 Presenter → return false。
- 其余 7 个简单行为补丁（`IsHidden`、`SetCurrentProject`、`StopProject` 等）保持不变。

**待修复小问题：** 研究项目节点框的底部边框在特定情况下存在轻微裁剪，正在排查中。

---

### 额外的用户体验修复（同期完成）

#### 3. 路径规划性能骤降

**问题：** 添加路径点后 FPS 急剧下降，退出规划界面后仍然卡顿。

**修复：**
- `SkyIslandMovementRenderUtility.cs`：`MaxArcSegments` 从 `48` 降至 `20`，`ArcSegmentAngle` 从 `0.08f` 放宽到 `0.14f`，减少约 60% 的 `Graphics.DrawMesh` 调用。
- `GameComponent_SkyIslandMovement.cs`：非规划模式下，只渲染**当前选中**天空岛的路径，避免所有玩家空岛同时每帧绘制。
- 移除了 `DrawRoutePreview` 中每帧冗余的 `EnsureWaypointProjectionCache()` 调用。

**验证结果：** 构建通过，路径预览视觉平滑度无显著损失。

#### 4. 地表视角快速选中空岛 Gizmo

**问题：** 大地图处于地面视角时，无法点击选中位于天空层的玩家空岛。

**修复：**
- 新增 `WorldGrid_SkyIslandSelectGizmoPatch.cs`：在地面视角下，Gizmo 栏增加"选中空岛"按钮。
- 点击后直接 `Find.WorldSelector.Select(island)` 并将镜头跳转到空岛的**地表投影位置**，但不会切换层级。
- 若存在多个玩家空岛，会弹出菜单供选择。

#### 5. 暂停时返回空岛层背景未同步

**问题：** 关闭大地图回到空岛殖民地时，若游戏暂停，背景世界层不会立即切回空岛层（原逻辑依赖 `GameComponentTick`）。

**修复：**
- 新增 `CameraJumper_SkyIslandLayerSyncPatch.cs`：在 `CameraJumper.TryHideWorld()` 的 Postfix 中，如果返回的殖民地地图属于空岛，则**同一帧**立即将 `PlanetLayer.Selected` 纠正为空岛层。
- 原 `GameComponentTick` 的兜底逻辑保留，作为运行时的二次保障。

---

## 二、核心问题总览（剩余待处理）

| 问题类型 | 严重程度 | 影响模块 | 状态 |
|---|---|---|---|
| **巨型 God Class** | 高 | `GameComponent_SkyIslandResearch` | **已完成 Stage 1~2** |
| **状态机用枚举 + 巨型 switch** | 中 | `SkyIslandMovementDriver`, `CompSkyIslandMovement` | 待重构 |
| **硬编码的跨建筑耦合** | 中 | `GameComponent_SkyIslandResearch` 直接引用具体建筑类 | **已解耦** |
| **纯透传代理层过厚** | 低 | `SkyIslandMapParent` | 可接受，暂缓 |
| **渲染与输入处理混杂** | 中 | `GameComponent_SkyIslandMovement` | 已部分缓解 |
| **UI 绘制细节待修复** | 低 | 研究项目节点底部边框裁剪 | 排查中 |

---

## 三、详细问题分析（保留用于后续参考）

### 3.1 GameComponent_SkyIslandResearch —— 典型的 God Class

> **注：** 以下"现状"描述反映重构前的状态。截至本文档更新时，**Stage 1~2 已完成**：尖塔泳道已迁移到 `SpireResearchCategory`，Command 模式已落地（`ISpireManualOperation`），硬编码建筑耦合已通过 `ISkyResearchSource` 解耦。剩余工作为 Stage 3（云海泳道收尾）和最终清理。

#### 重构前现状
- **650+ 行代码**，承担了以下 8 类职责：
  1. 尖塔任务生命周期（创建、完成、阶段推进）
  2. 尖塔手动操作状态机（调查、启动外环、重构稳压、完全重启）
  3. 云海研究管理（当前项目、监测仪存在性检查）
  4. 节点观测占位（预留但未实现）
  5. 研究工作可用性扫描（遍历地图找尖塔/监测仪）
  6. UI 文本工厂（操作标签、描述、任务名称、信件文本）
  7. 研究进度添加与 Mote 弹出
  8. 存档数据管理（多槽位研究项目列表）

#### 直接后果（已部分解决）
- 任何一条研究泳道的改动（如新增节点观测）都需要修改这个类。（✅ 尖塔线已分离，云海线待收尾）
- `Building_FloatingEnergySpire` 和 `Building_WeatherMonitor` 被硬编码引用，新增第三种数据来源必须继续往里加代码。（✅ 已通过 `ISkyResearchSource` 解耦）
- `GetQuestName` / `GetQuestDescription` 等方法用 switch 堆积阶段文本，扩展新阶段时极易遗漏分支。（✅ 已迁移到 `SpireResearchCategory` 和 4 个 `ISpireManualOperation` 实现类）

#### 设计模式改进方案

**方案 A：Strategy 模式 —— 拆分研究泳道处理器**

```csharp
public interface ISkyResearchLane
{
    SkyIslandDataTypeDef DataType { get; }
    bool IsProjectVisible(SkyIslandResearchProjectDef project);
    bool TryGetCurrentProject(out SkyIslandResearchProjectDef project);
    bool HasAvailableWork(Pawn pawn, SkyIslandResearchProjectDef project);
    string GetQuestName(int stage);
    string GetQuestDescription(int stage);
    // ...
}

public class SpireResearchLane : ISkyResearchLane { ... }
public class CloudSeaResearchLane : ISkyResearchLane { ... }
public class NodeObservationLane : ISkyResearchLane { ... }
```

`GameComponent_SkyIslandResearch` 只需维护一个 `List<ISkyResearchLane>`，所有泳道相关查询都委托给对应处理器。

**方案 B：Command 模式 —— 提取尖塔手动操作**

当前 `SpireManualOperationKind` 是裸枚举，操作逻辑散落在 `GetCurrentOperationLabel` / `GetCurrentOperationDescription` / `NotifySpireManualOperationCompleted` 三个方法中。

应改为：

```csharp
public interface ISpireManualOperation
{
    SpireManualOperationKind Kind { get; }
    string Label { get; }
    string Description { get; }
    JobDef JobDef { get; }
    int DurationTicks { get; }
    void OnCompleted(Building_FloatingEnergySpire spire, Pawn pawn);
}
```

每个阶段推进操作对应一个具体类，彻底消除 switch 堆积。

**方案 C：Observer / Event 模式 —— 解耦任务完成通知**

`CompleteBootstrapInvestigation`、`CompleteStageAdvance`、`CompleteFinalRestart` 中混合了：状态变更、信件发送、任务结束、新任务创建、科技解锁。

应拆分为事件：
- `OnSpireStageCompleted`
- `OnSpireSequenceFinished`

由专门的 `SpireQuestManager` 或事件处理器订阅处理，而不是全部内联在 God Class 中。

---

### 3.2 移动系统 —— 状态机与职责边界模糊

#### 现状
- `SkyIslandMapParent`：60 行，**纯透传代理**。所有移动相关属性直接 `=> MovementComp.XXX`。它本应是领域模型入口，但实际上只是壳。
- `CompSkyIslandMovement`：230 行，**协调器 + 路径管理器 + 信件发送器**。负责：路径点增删、WaypointPlanner 管理、Driver 启停、信件发送、存档。
- `SkyIslandMovementDriver`：460 行，**状态机 + 运动学 + 瓦片映射**。包含 `Accelerating` / `Cruising` / `Decelerating` / `Interrupting` 的 switch 逻辑，以及球面坐标插值、邻居瓦片爬山法。
- `GameComponent_SkyIslandMovement`：310 行，**输入处理器 + 渲染调度器 + 相机控制器**。处理鼠标右键、ESC、FloatMenu、世界地图渲染调用、相机跳转。

#### 直接后果
- `SkyIslandMovementDriver` 的 `AdvanceMovement` 方法超过 200 行，状态转换与运动计算混在一起。
- 新增一个状态（如 `Hovering` 悬停状态）时，需要同时修改 Driver 中的 switch、Comp 中的条件判断、UI 中的状态标签显示。
- `GameComponent_SkyIslandMovement` 直接操作 `Find.WorldCameraDriver` 和 `Find.WindowStack`，与"GameComponent"的持久化协调职责不符。

#### 设计模式改进方案

**方案 A：State 模式重构 SkyIslandMovementDriver**

```csharp
public interface IMovementState
{
    SkyIslandMapParent.SkyIslandMovementState State { get; }
    MovementTickOutput Tick(SkyIslandMovementContext ctx, int ticks);
    float CurrentSpeed { get; }
}

public class AcceleratingState : IMovementState { ... }
public class CruisingState : IMovementState { ... }
public class DeceleratingState : IMovementState { ... }
public class InterruptingState : IMovementState { ... }
```

`SkyIslandMovementDriver` 只维护 `currentState: IMovementState`，状态转换由具体状态对象内部决定或返回给 Driver。

**方案 B：职责分离 —— 拆分 KinematicsEngine 与 PathNavigator**

当前 Driver 同时处理：
1. "沿路径怎么走"（路径段切换、目标点管理）
2. "球面插值怎么算"（角速度、RotateTowards、瓦片映射）

应拆分为：
- `PathNavigator`：管理路径段序列、到达检测、下一段切换
- `KinematicsEngine`：给定起点/目标/速度曲线，计算新的方向和位置

**方案 C：MVC / 分层拆分 GameComponent_SkyIslandMovement**

当前它混合了 Model（planningIsland 状态）、View（DrawPlanningHintWindow, DrawWorldRoutePreviews）、Controller（ProcessInput, StartPlanning, StopPlanning）。

建议拆分为：
- `SkyIslandMovementPlanner`（Model）：只保存 planningIsland 和路径点操作逻辑
- `SkyIslandMovementInputHandler`（Controller）：处理鼠标/键盘事件
- `SkyIslandMovementRenderer`（View）：负责 `DrawPlanningOverlay`、`DrawRoutePreview`、`DrawPlanningHintWindow`
- `GameComponent_SkyIslandMovement` 只作为生命周期容器和外部入口。

> 注：经过路径渲染性能修复后，该模块的紧迫性已有所下降，可作为中长期目标逐步推进。

---

### 3.3 跨建筑硬编码耦合

#### 现状
`GameComponent_SkyIslandResearch` 中：
- `HasAvailableSpireResearchWork` 直接 `ThingsOfDef(SkyrimIslands_FloatingEnergySpire).OfType<Building_FloatingEnergySpire>`
- `HasAvailableCloudSeaResearchWork` 直接 `AllBuildingsColonistOfDef(SkyrimIslands_WeatherMonitor)`
- `NotifySpireManualOperationCompleted` 的参数类型是 `Building_FloatingEnergySpire`

#### 直接后果
- 新增第三种数据来源建筑时，必须修改 `GameComponent_SkyIslandResearch`。
- 建筑类的具体验证逻辑（`CanPerformResearch`、`CanPerformInvestigation`）无法统一抽象。

#### 设计模式改进方案

**方案：接口隔离 + 依赖倒置**

```csharp
public interface ISkyResearchSource
{
    bool CanPerformResearch(Pawn pawn, SkyIslandResearchProjectDef project, out string reason);
    void OnResearchCompleted(Pawn pawn, SkyIslandResearchProjectDef project);
}
```

`Building_FloatingEnergySpire` 和 `Building_WeatherMonitor` 都实现该接口。

`GameComponent_SkyIslandResearch` 的 `HasAvailableSkyResearchWork` 改为：

```csharp
public bool HasAvailableSkyResearchWork(Pawn pawn, SkyIslandDataTypeDef dataType)
{
    var sources = pawn.MapHeld!.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
        .OfType<ISkyResearchSource>();
    // ...
}
```

> 注：RimWorld 的 `Thing` 基类不支持接口快速检索，实际可用 `ThingDef` 白名单或维护一个 `CompSkyResearchSource` 的 ThingComp 来优化查找。

---

## 四、下一步计划

### 近期（1-2 周内，优先保障稳定性）

1. **修复研究 UI 底部边框裁剪**
   - 排查 `Widgets.CustomButtonText` 与 `ListProjects` 中 `rect` 高度计算的差异。
   - 必要时插入调试绘制代码定位是高度压缩还是材质绘制问题。

2. **Stage 3：迁移云海研究泳道逻辑到 `CloudSeaResearchCategory`**
   - 将 `Building_WeatherMonitor` 的调查进度添加、项目完成回调等逻辑从 `GameComponent_SkyIslandResearch` 迁移到 `CloudSeaResearchCategory`。
   - 实现云海研究的阶段门控（若后续需要）。
   - 目标：`GameComponent_SkyIslandResearch` 彻底退化为"槽位持有器 + Category 委托入口"。

### 中期（2-4 周内，架构扩展）

3. **MapTemperature 连续化补丁**
   - 实现 `MapTemperature.OutdoorTemp` / `SeasonalTemp` 的 Harmony 前缀拦截。
   - 基于 `SkyIslandLocalTimeUtility` 的连续地面投影经纬度，让空岛室外温度随移动平滑过渡。
   - 这是 `IMPLEMENTATION.md` 中"平滑本地时间系统"的最后一环。

### 长期（视进度安排）

4. **State 模式重构 `SkyIslandMovementDriver`**
   - 将 `Accelerating` / `Cruising` / `Decelerating` / `Interrupting` 提取为独立状态类。
   - 为后续添加 `Hovering`、`Docking` 等状态打下基础。

5. **拆分 `GameComponent_SkyIslandMovement` 的 MVC 职责**
   - 将输入、渲染、模型状态彻底分离。
   - 该工作可与高度系统、对地交互（ docking / trading ）一并设计。

---

## 五、快速验证重构质量的标准

每次重构后，可通过以下问题自测：

1. 新增一条研究泳道时，是否**不需要**修改 `GameComponent_SkyIslandResearch`？（✅ 已达成）
2. 新增一个 `GenLocalDate` 重载补丁时，是否**不需要**再新建一个类？（✅ 已达成）
3. 新增一个移动状态（如 `Hovering`）时，是否只需新增一个文件，而不改现有类的 switch 分支？
4. 修改研究 UI 布局时，是否**不需要**碰任何 `Patches/` 下的文件？（✅ 已达成）
5. 新增一种研究数据来源建筑时，是否只需实现一个接口并在 XML 中注册？（✅ 已达成）

如果以上 5 个问题有 3 个以上答案是"是"，则说明架构已经显著改善。目前已有 4 项达成。
