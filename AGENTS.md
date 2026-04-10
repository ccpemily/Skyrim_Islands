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

## 测试与验证
仓库当前没有独立测试工程。提交前至少完成一次 Debug 构建，并在游戏内验证模组能正常加载。若改动涉及初始化、Harmony 补丁或游戏行为，请附上简短的人工验证说明，例如“进入主菜单后确认日志只输出一次初始化消息”。

## 提交与合并请求
当前工作区不包含 Git 历史，提交信息请使用简短的祈使句，例如 `Add island world bootstrap`、`Fix debug startup logging`。每次提交只处理一个主题。PR 应说明改动目的、影响范围、人工验证步骤；涉及 UI、贴图或文本展示变化时再附截图。

## 配置提示
工程引用默认指向 `..\..\RimWorldWin64_Data\Managed\`。如果本地无法构建，请先确认开发副本路径没有变化，再考虑调整项目引用。
