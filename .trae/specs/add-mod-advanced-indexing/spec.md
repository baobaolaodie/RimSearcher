# Mod 高级索引支持 Spec

## Why
当前 Mod 索引仅支持基础的源码和 Defs 目录探测，但 Mod 的实际结构更为复杂：存在多版本目录、XML Patch 文件、Harmony 补丁等。这些信息对于开发者理解 Mod 行为至关重要，但静态分析难以完全模拟运行时效果。本规格旨在以"知情但不模拟"的策略，让 LLM 能够感知这些 Patch 的存在，按需深入了解。

## What Changes
- 扩展 `ModPathResolver` 支持版本目录探测（`1.3/`、`1.4/`、`1.5/` 等）
- 新增 `PatchIndexer` 索引 XML Patch 文件（仅索引，不尝试合并）
- 新增 `HarmonyPatchIndexer` 索引 Harmony Patch 声明
- 在 `inspect` 工具返回时附加 Patch 提示信息
- 新增 `list_patches` 工具查询相关 Patch

## Impact
- Affected specs: Mod 路径解析、索引构建、工具层
- Affected code:
  - `ModPathResolver.cs` - 版本目录探测
  - `Program.cs` - 新索引器初始化
  - 新增 `PatchIndexer.cs`
  - 新增 `HarmonyPatchIndexer.cs`
  - 新增 `ListPatchesTool.cs`
  - `InspectTool.cs` - 附加 Patch 提示

## ADDED Requirements

### Requirement: Mod 版本目录探测
系统 SHALL 自动探测 Mod 的版本目录结构。

#### Scenario: 版本目录优先
- **GIVEN** 一个 Mod 根目录
- **WHEN** 该目录下存在版本子目录（如 `1.5/`、`1.4/`）
- **THEN** 系统应优先索引版本目录下的内容

#### Scenario: 多版本共存
- **GIVEN** 一个 Mod 同时存在多个版本目录
- **WHEN** 进行索引
- **THEN** 系统应索引所有存在的版本目录

#### Scenario: 根目录回退
- **GIVEN** 一个 Mod 没有版本目录
- **WHEN** 进行索引
- **THEN** 系统应回退到根目录探测

### Requirement: XML Patch 文件索引
系统 SHALL 索引 XML Patch 文件但不尝试合并。

#### Scenario: Patch 目录探测
- **GIVEN** 一个 Mod 目录
- **WHEN** 存在 `Patches/` 目录
- **THEN** 系统应索引其中的 XML Patch 文件

#### Scenario: Patch 元数据提取
- **GIVEN** 一个 XML Patch 文件
- **WHEN** 解析该文件
- **THEN** 系统应提取目标 Def、操作类型（Add/Remove/Replace）、来源文件

#### Scenario: Patch 查询
- **GIVEN** 用户查询某个 Def
- **WHEN** 存在针对该 Def 的 Patch
- **THEN** 系统应返回 Patch 列表（但不模拟合并结果）

### Requirement: Harmony Patch 索引
系统 SHALL 索引 Harmony Patch 声明。

#### Scenario: Harmony 特性解析
- **GIVEN** 一个 C# 文件包含 `[HarmonyPatch]` 特性
- **WHEN** 解析该文件
- **THEN** 系统应提取目标方法、Patch 类型（Prefix/Postfix/Transpiler）、来源信息

#### Scenario: Patch 查询
- **GIVEN** 用户查询某个方法
- **WHEN** 存在针对该方法的 Harmony Patch
- **THEN** 系统应返回 Patch 列表

### Requirement: Inspect 工具增强
系统 SHALL 在 inspect 返回时附加 Patch 提示。

#### Scenario: Def Patch 提示
- **GIVEN** 用户 inspect 一个 Def
- **WHEN** 存在针对该 Def 的 XML Patch
- **THEN** 返回结果应包含 Patch 数量提示

#### Scenario: 方法 Patch 提示
- **GIVEN** 用户 inspect 一个 C# 类型
- **WHEN** 该类型的方法存在 Harmony Patch
- **THEN** 返回结果应包含 Patch 数量提示

## MODIFIED Requirements

### Requirement: ModPathResolver
扩展路径解析逻辑，支持版本目录探测。

#### 解析优先级
1. 显式配置的路径（最高优先级）
2. 版本目录下的子目录（`1.5/Defs/`、`1.5/Source/` 等）
3. 根目录下的子目录（`Defs/`、`Source/` 等）

## REMOVED Requirements
无移除的需求。

## 设计原则

### Token 消耗控制
- Patch 信息采用"提示+按需查询"模式，不主动返回完整内容
- 索引仅存储元数据，不存储完整 Patch 内容
- LLM 可通过 `list_patches` 工具按需获取详情

### 功能边界
- ✅ 索引 Patch 文件位置和目标
- ✅ 索引 Harmony Patch 声明
- ✅ 提供 Patch 查询能力
- ❌ 不模拟 Patch 合并结果
- ❌ 不分析 Transpiler 的实际效果
- ❌ 不反编译 DLL 文件
