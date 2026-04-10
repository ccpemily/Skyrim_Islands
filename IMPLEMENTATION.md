# Skyrim Islands 实现记录

## 目标概述

`Skyrim Islands` 的核心目标，是把玩家的殖民地从普通地表地图迁移到一个可扩建、可移动、资源获取方式独立的空岛地图中，并围绕这个空岛建立新的任务、资源与袭击逻辑。

当前仓库还处在非常早期的脚手架阶段，功能设想主要记录在 `FEATURES.md` 中，代码层面目前只完成了最小模组入口。

## 当前开发策略

在做出第一个稳定可运行版本之前，暂不考虑向前兼容、旧存档迁移或旧实现兜底。当前所有实现都以“先做出干净、可运行的第一版”为目标。

## 前提约束

### 强依赖 Odyssey

空岛 Mod 明确强依赖 `Odyssey DLC`，后续实现直接建立在 Odyssey 已提供的系统之上，不再考虑“无 DLC 兼容”的分支方案。这样可以明确利用以下现成能力：

- 独立 `PlanetLayerDef`
- 轨道 / 太空相关 `MapParent`
- `Space` 地形与相关地图生成思路
- `Zone_Fishing`、水体与鱼群系统
- 穿梭机、运输到达方式、轨道地图交互

这意味着实现时应优先复用 Odyssey 的世界层和地图层概念，而不是把空岛伪装成普通地表殖民地。

## 当前已实现内容

### 已落地

- 模组主入口 `Source/SkyrimIslandsMod.cs`
- Harmony 初始化与防重复补丁逻辑
- Debug 构建输出到 `1.6/Assemblies/`
- VS Code 下的构建、启动与 Unity Debugger 附加流程
- `portable pdb` 调试符号输出
- `DefOf` 入口 `Source/SkyrimIslandsDefOf.cs`
- 空岛世界对象骨架 `Source/World/SkyIslandMapParent.cs`
- 世界级状态组件 `Source/World/WorldComponent_SkyIslands.cs`
- 新游戏初始化流程 `Source/World/GameComponent_SkyIslandFlow.cs`
- 基础地图生成步骤：
  - `Source/MapGen/GenStep_SkyIslandBase.cs`
  - `Source/MapGen/GenStep_SkyIslandCore.cs`
- 最小 Def 集合已经接入：
  - `1.6/Defs/WorldObjectDefs/WorldObjects_SkyrimIslands.xml`
  - `1.6/Defs/MapGeneration/MapGeneration_SkyrimIslands.xml`
  - `1.6/Defs/TerrainDefs/Terrain_SkyrimIslands.xml`
  - `1.6/Defs/BiomeDefs/Biomes_SkyrimIslands.xml`
  - `1.6/Defs/PlanetLayerDefs/PlanetLayerDefs_SkyrimIslands.xml`
  - `1.6/Defs/WeatherDefs/Weather_SkyrimIslands.xml`
- `About/About.xml` 已加入 `Odyssey` 依赖声明
- 云海地形已调整为不自行渲染，视觉上由地图背景接管
- 云海鱼影特效已屏蔽，但鱼群数据与鼠标提示保留，便于后续继续做钓鱼系统
- 空岛专属 `PlanetLayerDef` 已接入，并在运行时注册
- 空岛专属 `BiomeWorker` 已接入：`Source/World/BiomeWorker_SkyIsland.cs`
- 空岛专属天气 Def 已接入：
  - `SkyrimIslands_Clear`
  - `SkyrimIslands_Windy`
- 空岛专用世界云层已接入：
  - `Source/World/WorldDrawLayer_SkyIslandClouds.cs`
  - 当前使用独立云层半径偏移 `+2.8`
  - 当前云层动画仍通过加速 `_GameTime` 近似控制速度
- 核心地格已从占位的 `SoilRich` 替换为自定义 `SkyrimIslands_IslandCore`
  - 表面贴图使用肥沃泥土
  - 边缘和下方侧面复用机械族平台 `MechanoidPlatform`
- 外围浮空平台已替换为自定义 `SkyrimIslands_FloatingPlatform`
  - 顶部贴图使用重型桥梁风格
  - 边缘和下方侧面同样复用机械族平台 `MechanoidPlatform`

### 当前缺失

- 目前空岛世界层正在向独立 `PlanetLayer` 结构收束，但还没有完成完整的游戏内验证
- 需要继续验证空岛专属 `PlanetLayer` 与小地图背景世界渲染高度是否完全一致
- 目前空岛 biome 已接入独立层，但还需要继续验证实际世界生成与显示行为
- 目前空岛专属天气已接入，但还需要继续验证晨昏颜色和天空观感
- 还没有核心建筑、平台规则、任务、研究、资源自动化与移动系统
- 还没有语言文件、正式贴图和可发布层面的内容整理
- 空岛专用云层速度目前是通过加速 `_GameTime` 实现的，尚未拆分成独立“平移速度”控制
- 空岛层高度与小地图背景渲染高度仍需要继续联动调试

### 当前最小闭环状态

当前版本已经能跑通一个“可进入”的最小空岛闭环：

1. 新游戏开始
2. `WorldComponent_SkyIslands` 运行时注册空岛专属图层
3. `GameComponent_SkyIslandFlow` 在空岛专属层创建空岛世界对象
4. 通过自定义地图生成器生成空岛地图
5. 起始 Pawn 与附近开局物资迁移到空岛
6. 当前视角切换到空岛地图

这说明“空岛作为可访问地图存在”这件事已经成立，后续重点已经不是从零搭入口，而是继续打磨空岛层与背景渲染、专属天气和后续玩法系统。

## 当前暂缓项

以下问题已经确认存在，但当前阶段暂不继续处理：

- 空岛层高度、独立 `PlanetLayer` 的相机高度与小地图背景渲染高度的完全一致性问题
  当前功能已经基本可用，后续需要时再集中调节相关参数组

- 空岛专属天气与天空颜色表现
  目前专属天气 Def 已接入，但视觉风格暂不继续细调

- 地块边缘上色问题
  已确认这是 `Camera + Mod 改变相机视场` 导致的现象，原版轨道地图在相同条件下也会出现，因此当前不视为空岛专属 bug，不继续处理

## 原版参考：超凡枢纽任务流

已经阅读过原版超凡枢纽（Archonexus）相关代码，当前可直接借鉴的结构如下：

- `QuestNode_Root_ArchonexusVictory`
  - 使用 `QuestPart_SubquestGenerator_ArchonexusVictory` 串起三轮子任务
- `QuestNode_Root_ArchonexusVictory_Cycle`
  - 负责每一轮的共同逻辑：接受条件、提醒信、奖励、玩家殖民地列表写入 slate
- `QuestPart_NewColony`
  - 真正负责“选择带走的人和物、播放过场、选新地点、启动搬迁”
- `Dialog_ChooseThingsForNewColony`
  - 负责选择殖民者、动物、遗物和有限数量物品
- `MoveColonyUtility.MoveColonyAndReset(...)`
  - 真正执行殖民地搬迁、生成新殖民地、投放选中的 Pawn/物资，并清理旧殖民地
- `Building_ArchonexusCore` + `ArchonexusCountdown`
  - 负责最终激活和结局收尾

对空岛项目而言，最值得直接复用的不是“超凡枢纽三轮胜利结构”，而是下面这条迁移链：

1. 任务触发
2. 打开“选择带走的殖民者/物资”窗口
3. 打开世界地图选点
4. 用长事件执行迁移
5. 在新地点生成玩家新据点并投放已选内容

后续空岛启程任务建议不要继续用当前的强制自动转换，而是改为：

- 自定义 `QuestScriptDef`
- 自定义 `QuestNode` 或 `QuestPart`
- 第一版直接复用或参考 `QuestPart_NewColony` / `Dialog_ChooseThingsForNewColony` / `MoveColonyUtility` 的工作流

需要注意的是，原版 `MoveColonyUtility.MoveColonyAndReset(...)` 带有明显的“超凡枢纽重置世界状态”语义，例如：

- 清理旧殖民地
- 重置部分研究与派系关系
- 处理遗物、装备、奴隶、运输队等特殊逻辑

因此空岛项目不应该直接照搬这个方法本体，而应优先参考它的流程拆分，自己实现一个更轻量的“迁移到空岛殖民地”版本。

## 实现架构建议

### 1. 世界层与空岛入口

空岛地图不适合仅做成普通地表殖民地替皮，更适合做成独立 `MapParent` / `WorldObjectDef` 驱动的世界对象。RimWorld 的地图生成入口本身就是围绕 `MapParent` 和 `GetOrGenerateMapUtility` 组织的，因此建议空岛核心世界对象单独建类，例如：

- `SkyrimIslands.World.SkyIslandMapParent`
- `SkyrimIslands.World.WorldComponent_SkyIslands`
- `SkyrimIslands.World.GameComponent_SkyIslandFlow`

当前实现已经切到独立空岛层路线。由于暂不考虑向前兼容，这一阶段直接围绕空岛专属 `PlanetLayerDef`、空岛专属 biome 和专属 weather 做验证与修整即可。

建议世界层拆成四部分：

- `Def`: 空岛专用 `PlanetLayerDef`
- `Def`: 空岛专用 `WorldObjectDef`
- `Code`: `SkyIslandMapParent` 负责地图、Gizmo、进入逻辑
- `Code`: `WorldComponent_SkyIslands` 负责全局生成、查找、移动与跨地图状态

这里的关键决定是：空岛应被视为“玩家拥有的一类轨道殖民地”，而不是“漂浮在地表上的特殊地图”。

### 2. 开局迁移与初始任务

初始任务最适合由 `GameComponent.StartedNewGame()` 或剧本 `ScenPart_CreateQuest` 驱动。第一版建议不要一开始就做复杂任务树，而是：

1. 新游戏开始后发送说明信件或任务
2. 玩家确认后触发“迁移到空岛”
3. 生成新的空岛世界对象与地图
4. 把起始 Pawn、基础物资和轨道穿梭机转移过去
5. 清理旧殖民地或将其标记为已废弃

这一部分需要优先保证流程稳定，先跑通“从普通开局进入空岛殖民地”的主线。

既然有 Odyssey，可进一步把“迁移到空岛”做成真正的世界层切换，而不是简单删除旧地图后传送 Pawn。更推荐的顺序是：

1. 正常完成原版开局初始化
2. 立刻发送空岛启程任务或确认信件
3. 生成空岛层上的 `SkyIslandMapParent`
4. 调用地图生成入口创建空岛地图
5. 使用穿梭机 / 运输到达动作，把起始 Pawn 与物资送入空岛
6. 回收或废弃地表起始点

这样后续的任务叙事、日志、运输和世界对象状态会更自然。

### 3. 空岛地图生成

空岛地图应拆成“地图生成器 + 若干 GenStep / PostMapGenerate 初始化”两层：

- 地图底层全部生成为“云海”
- 中央刷出 `8x8` 空岛核心
- 核心外刷一圈初始浮空石平台
- 固定刷出能量尖塔、损坏的链接节点、扫描仪等起始结构

这里最关键的设计点有两个：

- 云海如果只是普通不可通行地形，不能直接支持原版 `Zone_Fishing`
- RimWorld 的钓鱼区要求地形同时满足 `IsWater`、`waterBodyType`，并由 `WaterBodyTracker` 维护鱼群

因此如果要保留“云海可垂钓”的设想，云海地形不能只是“像深水一样不可通行”，而应该真正做成一种特殊水体地形，再通过贴图、移动性和建造限制把它伪装成云海。

由于强依赖 Odyssey，地图生成可以明确参考轨道 / 太空地图的处理方式：

- 用 Odyssey 的空间背景表现做底
- 用自定义地形把“云海”表现成非真空、可钓鱼的特殊水域
- 保留地图外观上的悬空感，而不是做成普通海洋地图

推荐把地形至少拆成三类：

- `Terrain_Sky`: 云海，不可通行，不可建造，可作为钓鱼水域
- `Terrain_FloatingPlatform`: 浮空石平台，可建造，可扩建
- `Terrain_IslandCore`: 核心地面，不可摧毁，可耕种，可刷出核心建筑

如果后续需要“平台未连通则坠毁”的判定，这三类地形的区分会很重要。

当前这三类地形里，已经实际落地的是：

- `SkyrimIslands_CloudSea`
- `SkyrimIslands_FloatingPlatform`
- `SkyrimIslands_IslandCore`

其中核心与平台已经开始做出明确的视觉分工：

- 核心：肥沃泥土表面 + 机械族平台边缘
- 平台：重型桥梁风格表面 + 机械族平台边缘

### 4. 气候与发电逻辑

地图气候建议分两层实现：

- 地图所在世界地块继续决定基础温度区间
- 空岛专用 `BiomeDef` / `MapComponent` 再对天气与温度做二次修正

RimWorld 的天气选择来自 `BiomeDef.baseWeatherCommonalities` 和 `WeatherDecider`。如果需求是“基本无天气变化”，最稳妥的第一版方案是：

- 自定义空岛 biome
- 让其天气表几乎恒定为 `Clear`
- 通过 biome 常量温度或 `MapComponent` 做额外降温

发电则不建议一开始就 Harmony 全局改原版逻辑。更稳的做法是：

- 给空岛专用风机、太阳能板单独做 `ThingDef`
- 通过自定义 `Comp` 或 Harmony 局部判定“当前地图是否为空岛”
- 风机输出在原版风速计算基础上加倍率，并设定保底值
- 太阳能在原版 `skyGlow` 基础上给固定加成

强依赖 Odyssey 后，这里还可以进一步明确：

- 空岛 biome 应挂到空岛专属层上
- 该 biome 的天气表可以近似锁死为晴朗，避免传统地表天气干扰
- 若想保留“高空有风、日照强，但无降雨”的感觉，最好直接在 biome 和电力组件层处理，而不是修改全局天气系统

换句话说，空岛环境应当是“基于所在地块温区的高空特化 biome”，而不是“普通地表 biome 的一组 Harmony 修补”。

当前状态下，`SkyrimIslands_SkyBiome` 已经挂到空岛独立层，`SkyrimIslands_Clear / SkyrimIslands_Windy` 也已经接入。下一阶段的重点变成：

1. 继续验证空岛层的大地图高度与小地图背景渲染高度
2. 校准专属天气对晨昏颜色、天空饱和度和风速的实际表现
3. 决定是否需要把云层速度从 `_GameTime` 加速改成独立平移控制

### 5. 资源获取系统

资源获取建议拆成三条线：

- 农业线：核心土地区与后续温室
- 扫描线：广域扫描仪生成资源地点
- 链接线：链接节点远程开采资源

扫描仪可直接参考 `CompScanner` / `CompLongRangeMineralScanner` 的工作流：Pawn 工作积累进度，触发后生成任务、地点或资源信号。第一版建议先做“扫描后生成世界地点 + 穿梭机前往采集”，等这条链稳定后，再加“链接节点远程采矿”。

链接节点本质上更像一个带库存和工作项目的建筑，建议拆成：

- 建筑本体 `Building_LinkNode`
- 存档数据 `Comp_LinkNode`
- 执行工作 `JobDriver_OperateLinkNode`
- 资源地点数据 `WorldObject` 或 `Site`

在 Odyssey 前提下，资源线建议优先使用“轨道地图 -> 穿梭机 -> 资源地点”的闭环，因为这与 DLC 现有运输体系最兼容。也就是说：

- 第一版不要急着做复杂的远程自动采矿 UI
- 先让扫描仪生成可访问的资源点
- 再用穿梭机完成运输与采集
- 链接节点则作为第二阶段的自动化升级内容

### 6. 袭击、商队与平台机制

袭击平台是后续所有外来访问逻辑的落点，因此它应该是中期优先级很高的系统。建议第一版只处理两件事：

- 用地图扫描或连通性检查确认平台是否与核心连通
- 把空投袭击、友方商队等特殊到达方式落在平台区域

在此之前，可以先暂时关闭常规地表袭击，避免 AI 直接从地图边缘无意义地刷在“云海”上。

既然空岛运行在 Odyssey 风格的独立层上，这部分建议不要沿用普通地图边缘来客逻辑，而应尽量统一成“平台着陆”：

- 袭击：空投 / 空降 / 穿梭机降落到平台
- 商队：友方飞船或穿梭机停靠平台
- 特殊任务目标：同样优先落在平台或平台附近

这样平台会成为真正的“空岛港口”，后续玩法才会统一。

### 7. 特殊研究与能量尖塔

能量尖塔建议分成两个功能：

- 电网核心：接管连接平台的供电
- 调查核心：提供“云海测量数据”

“云海测量数据”第一版不必急着复刻异象研究点的完整系统。更实际的做法是先把它实现成一种专用资源或计数器，再让空岛科技以研究前置或消耗条件读取它。等主线系统稳定后，再考虑做真正独立的研究点体系。

不过既然已经明确依赖 Odyssey，这里可以把“异象研究点式资源”定位得更清楚：

- 第一版先做成 `GameComponent` 或 `WorldComponent` 计数器
- 尖塔调查工作完成后增加该计数器
- 空岛专属科技或建筑检查该计数器是否达标
- 后续若体验良好，再考虑做成更完整的独立研究页签或研究修正系统

## 建议开发顺序

### 第一阶段：跑通最小可玩闭环

- 自定义空岛地图生成
- 开局迁移到空岛
- 基础起始资源与核心建筑刷出
- 稳定的构建、加载、进入地图流程

### 第二阶段：做基础生存循环

- 核心土地区与温室
- 云海地形与浮空石平台
- 风机、太阳能强化版
- 扫描仪生成资源地点

### 第三阶段：做外部交互

- 袭击平台
- 空投袭击与访客逻辑
- 机械师任务替换
- 异象信标与相关任务

### 第四阶段：做成长系统

- 链接节点升级与远程采矿
- 空岛控制终端与移动
- 能量尖塔调查与科技解锁
- 空岛专属招募事件

## 当前最推荐的下一步

优先把当前已经搭好的“独立 PlanetLayer + 专属 Biome / Weather”结构继续调到视觉和行为都稳定。

理由是：

- “空岛地图 + 开局迁移 + 独立层 + 专属天气”这条主链已经基本连起来了
- 现在最大的工作重心转向：
  - 视觉一致性
  - 层间渲染一致性
  - 云层与天气观感
- 只有这一层稳定下来，后续做空岛移动、平台机制和资源系统才不会反复返工

建议下一步按这个顺序推进：

- 校准空岛层高度相关的整组参数：
  - `SkyLayerRadius`
  - `extraCameraAltitude`
  - `backgroundWorldCameraOffset`
  - `backgroundWorldCameraParallaxDistancePer100Cells`
- 继续调整空岛专属云层：
  - 高度
  - 可见性
  - 速度控制方式
- 验证专属天气在空岛层上的实际显示结果
- 然后再进入核心建筑、平台规则、任务与资源系统

这一阶段完成后，空岛的第一版世界层表现才算稳定，可以放心往玩法层推进。

等这一层视觉和结构稳定下来，再继续推进平台规则、专属建筑、资源点和任务链会更顺手。
