# Repository Guidelines

## 项目结构与模块组织
本仓库是一个 `RimWorld 1.6` 模组工程。`Source/` 存放 C# 代码，`About/` 存放模组元数据，`1.6/Assemblies/` 存放最终加载的 `dll/pdb`，其余 XML Def、Patch、贴图和语言文件应分别放入 `1.6/Defs/`、`1.6/Patches/`、`1.6/Textures/`、`1.6/Languages/`。`LoadFolders.xml` 负责把 `1.6/` 目录映射给游戏版本。

## 构建、运行与调试
- `dotnet restore .\Skyrim_Islands.csproj`：首次还原 NuGet 依赖到 `.nuget/packages`
- `dotnet build .\Skyrim_Islands.csproj -c Debug --no-restore`：构建模组并输出到 `1.6/Assemblies/`
- `Ctrl+Shift+B`：执行默认任务 `build mod`
- `Terminal > Run Task > launch RimWorld`：构建后启动开发副本 `RimWorld`
- 调试时使用 `Ctrl+Shift+P -> Attach Unity Debugger` 手动附加到当前 `RimWorld player`

## C# 编码与命名规范
使用 4 空格缩进，大括号独占一行，保持 `Nullable` 警告清晰。类型、方法、属性使用 `PascalCase`，局部变量和参数使用 `camelCase`。参考上级项目 `MiliraXian_Characters`，优先采用提前返回、前置判空、对 `List<T>` 使用 `for` 遍历，并尽量把通用逻辑抽到明确的工具类中。文件名应与主类型一致。

命名空间遵循当前项目约定：
- `Source/` 根目录：`SkyrimIslands`
- `Source/*` 子目录：`SkyrimIslands.*`
- 新增任务时，任务主逻辑统一放在 `Source/Quests/$QUEST_NAME$/`，命名空间使用 `SkyrimIslands.Quests.$QUEST_NAME$`
- 任务相关的专用物体、组件、命令、窗口等，统一放在 `Source/Quests/$QUEST_NAME$/$OBJECT_NAME$/`，命名空间使用 `SkyrimIslands.Quests.$QUEST_NAME$.$OBJECT_NAME$`

## 测试与验证
仓库当前没有独立测试工程。提交前至少完成一次 Debug 构建，并在游戏内验证模组能正常加载。若改动涉及初始化、Harmony 补丁或游戏行为，请附上简短的人工验证说明，例如“进入主菜单后确认日志只输出一次初始化消息”。

## 提交与合并请求
当前工作区不包含 Git 历史，提交信息请使用简短的祈使句，例如 `Add island world bootstrap`、`Fix debug startup logging`。每次提交只处理一个主题。PR 应说明改动目的、影响范围、人工验证步骤；涉及 UI、贴图或文本展示变化时再附截图。

## 配置提示
工程引用默认指向 `..\..\RimWorldWin64_Data\Managed\`。如果本地无法构建，请先确认开发副本路径没有变化，再考虑调整项目引用。

## Agent 说明
查看 RimWorld 源码时，优先使用 `rimsearcher MCP` 进行检索、定位和阅读；不要默认改用普通文件搜索，除非 `rimsearcher MCP` 当前不可用。
如果发现 `rimsearcher MCP` 返回 `Transport closed`，应立即停止当前任务，并第一时间向用户报告；不要在同一任务中默默切换到普通文件搜索继续推进。
读取和写入文本文件时，默认使用 `UTF-8` 编码，尤其是包含中文的 `md`、`xml`、`cs` 文件；不要在未确认编码的情况下直接按控制台默认编码处理。
如果任务需要使用反射（如 `Harmony AccessTools`、`System.Reflection`、字段/方法反射访问等），应立即暂停实现，并先向用户说明用途后征求确认；不要在未征求用户同意的情况下引入新的反射代码。
如果需要缓存 `Texture2D` 等 Unity 资源的静态字段，不要直接放在普通业务类中；应统一放入单独的贴图缓存类（当前固定为 `Source/SkyrimIslandsTextureCache.cs` 中的 `SkyrimIslandsTextureCache`），并为该类添加 `StaticConstructorOnStartup`，确保资源在主线程初始化。后续所有新的静态 `Texture2D` 缓存都必须继续放进这个类，而不是分散到别的类里。
如需查找某个具体模组的本地路径，优先使用以下顺序：
- 先读取用户目录下的 `ModsConfig.xml`，确认目标模组当前启用时使用的 `packageId`
- 再在常见本地模组目录和 Steam Workshop `294100` 目录中检索 `About/About.xml`
- 以 `About.xml` 中的 `<packageId>` 为准确认模组根目录，不要只靠文件夹名判断
- 如果多个模组只是依赖或补丁关系，应明确区分主模组与其附属模组，例如 `Ancot.MiliraRace`、`Ancot.MiliraRaceGenePatch`、`Ancot.MiliraRaceFacialAnimation`

修改 Def 或新增代码后，应检查 XML 与 C# 的交叉引用是否一致，包括：
- `defName` 与 `DefOf` 字段名是否一致
- XML 中引用的 `Class`、`worldObjectClass`、`workerClass`、`compClass`、`layerType` 等类型名是否存在
- C# 中通过 `DefOf`、`DefDatabase`、字符串或反射引用的 Def 名称是否与 XML 一致

对 XML 本身的改动，还应额外检查 XML 之间的引用是否正确，包括：
- `ParentName`、`MayRequire`、`Name` 是否有效
- 各类 Def 节点中引用的其他 `defName` 是否存在，例如 `mapGenerator`、`terrain`、`biome`、`thingDef`、`worldObjectDef`、`researchPrerequisites`、`terrainAffordanceNeeded`
- 新增 Def 是否放在合适目录，并与 `LoadFolders.xml`、版本目录结构保持一致

XML 中如果引用 C# 类型，遵循以下规则：
- 原版 RimWorld / Verse / DLC 已有类型，可直接使用类名，不必写限定名
- Mod 自己实现的类型，必须使用全限定名，例如 `SkyrimIslands.World.SkyIslandMapParent`
