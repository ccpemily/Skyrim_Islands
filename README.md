# Skyrim Islands

`Skyrim Islands` 是一个面向 `RimWorld 1.6` 的模组工程，当前仓库已经整理为可直接在 `VS Code` 中开发、构建与调试的结构。

## 仓库结构

```text
Skyrim_Islands/
├─ 1.6/                      # 按 RimWorld 1.6 加载的游戏内容
│  ├─ Assemblies/            # 编译输出 DLL / PDB
│  ├─ Defs/                  # XML Def
│  ├─ Patches/               # XML Patch
│  ├─ Textures/              # 贴图资源
│  └─ Languages/             # 本地化文本
├─ About/                    # About.xml 等模组元数据
├─ Source/                   # C# 源码
├─ .vscode/                  # VS Code 任务与启动配置
├─ LoadFolders.xml           # 版本加载入口
├─ Skyrim_Islands.csproj     # 主工程
└─ global.json               # 固定 .NET SDK 版本
```

## 命名空间规则

- `Source/` 根目录下的代码使用命名空间 `SkyrimIslands`
- `Source` 子目录下的代码使用 `SkyrimIslands.*`
- 文件名应与主类型同名，例如 `WorldComponent_SkyrimIslands.cs`

## C# 编码规范

参考上级项目 `MiliraXian_Characters` 的现有代码习惯，建议保持以下约定：

- 使用 4 个空格缩进，不使用 Tab
- 大括号独占一行
- 类型、方法、属性使用 `PascalCase`
- 局部变量、参数、私有字段使用 `camelCase`
- 优先提前返回，减少多层嵌套
- 循环中优先使用 `for` 处理 `List<T>`，避免无意义分配
- 判空、越界、销毁态检查尽量前置
- 仅在逻辑不直观时补充简短注释，避免解释性废话

## 开发说明

推荐安装以下 VS Code 扩展：

- `C# Dev Kit`
- `C#`
- `Unity for Visual Studio Code`
- `XML`

当前工程配置：

- 目标框架：`.NET Standard 2.1`
- C# 语言版本：`latest`
- Harmony：`Lib.Harmony 2.4.2`
- 调试符号：默认生成 `portable pdb`

## 构建方法

首次还原依赖：

```powershell
dotnet restore .\Skyrim_Islands.csproj
```

常规构建：

```powershell
dotnet build .\Skyrim_Islands.csproj -c Debug --no-restore
```

也可以直接在 VS Code 中使用：

- `Ctrl+Shift+B`：执行默认任务 `build mod`
- `Terminal > Run Task > launch RimWorld`：先构建，再启动开发副本的 `RimWorld`

编译输出位于：

```text
1.6/Assemblies/Skyrim_Islands.dll
1.6/Assemblies/Skyrim_Islands.pdb
```

## 调试运行方法

本仓库当前采用 `Unity Player` 调试流程，不再使用 Mono 直连方案。

1. 确认开发副本已经替换为可调试的 Debug `RimWorld`
2. 在 VS Code 中执行 `Launch RimWorld`，或手动启动开发副本
3. 当游戏提示可以附加调试器后，按 `Ctrl+Shift+P`
4. 执行 `Attach Unity Debugger`
5. 从列表中选择当前运行的 `RimWorld player`

说明：

- Unity 调试端口由 player 运行时动态分配，不要依赖固定端口
- 如果断点未命中，先重新构建，确认最新 `dll/pdb` 已写入 `1.6/Assemblies/`
- 初始化代码执行很早，建议在游戏继续运行前尽快附加调试器

## 开始前建议

1. 先完善 `About/About.xml` 中的 `author`、`packageId` 和 `description`
2. 新增 XML 内容时，按用途放入 `1.6/Defs`、`1.6/Patches`、`1.6/Languages`
3. 新增 C# 文件直接放入 `Source/` 或其子目录，工程会自动包含这些 `.cs` 文件
