# Tasks

- [x] Task 1: 扩展 ModPathResolver 支持版本目录探测
  - [x] SubTask 1.1: 定义版本目录名称列表（`1.5`、`1.4`、`1.3`、`1.2`、`1.1`）
  - [x] SubTask 1.2: 实现版本目录探测逻辑，优先使用版本目录
  - [x] SubTask 1.3: 支持版本目录下的 `Defs/`、`Source/`、`Patches/` 探测
  - [x] SubTask 1.4: 更新 `ResolvedModPaths` 记录类型，增加 `PatchesPaths` 属性

- [x] Task 2: 实现 PatchIndexer 索引 XML Patch 文件
  - [x] SubTask 2.1: 创建 `PatchIndexer.cs` 类
  - [x] SubTask 2.2: 定义 `PatchLocation` 记录类型（目标Def、操作类型、来源文件、Mod名称）
  - [x] SubTask 2.3: 实现 XML Patch 文件解析逻辑
  - [x] SubTask 2.4: 实现按目标 Def 查询 Patch 的方法
  - [x] SubTask 2.5: 支持快照导出/导入

- [x] Task 3: 实现 HarmonyPatchIndexer 索引 Harmony Patch
  - [x] SubTask 3.1: 创建 `HarmonyPatchIndexer.cs` 类
  - [x] SubTask 3.2: 定义 `HarmonyPatchLocation` 记录类型
  - [x] SubTask 3.3: 扩展 `RoslynHelper` 解析 `[HarmonyPatch]` 特性
  - [x] SubTask 3.4: 实现按目标方法查询 Patch 的方法
  - [x] SubTask 3.5: 支持快照导出/导入

- [x] Task 4: 更新 Program.cs 集成新索引器
  - [x] SubTask 4.1: 初始化 `PatchIndexer` 和 `HarmonyPatchIndexer`
  - [x] SubTask 4.2: 扫描 Mod 的 Patches 目录
  - [x] SubTask 4.3: 更新缓存快照结构
  - [x] SubTask 4.4: 更新缓存指纹计算

- [x] Task 5: 实现 list_patches 工具
  - [x] SubTask 5.1: 创建 `ListPatchesTool.cs`
  - [x] SubTask 5.2: 支持按 Def 名称查询 XML Patch
  - [x] SubTask 5.3: 支持按方法名查询 Harmony Patch
  - [x] SubTask 5.4: 注册工具到服务器

- [x] Task 6: 增强 InspectTool 添加 Patch 提示
  - [x] SubTask 6.1: 在 inspect Def 时附加 XML Patch 数量提示
  - [x] SubTask 6.2: 在 inspect C# 类型时附加 Harmony Patch 数量提示

# Task Dependencies
- [Task 2] depends on [Task 1]
- [Task 3] depends on [Task 1]
- [Task 4] depends on [Task 2, Task 3]
- [Task 5] depends on [Task 4]
- [Task 6] depends on [Task 4]
