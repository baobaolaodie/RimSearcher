# Mod 数据索引支持 Spec

## Why
当前 RimSearcher 仅支持配置原版 RimWorld 数据路径，无法索引 Mod 的 C# 源码和 XML Defs。在实际开发中，开发者经常需要基于其他 Mod 进行开发，需要能够搜索和查看 Mod 的源码与定义，目前这一功能的缺失导致开发效率低下。

## What Changes
- 扩展 `AppConfig` 配置结构，新增 `Mods` 配置项支持多个 Mod 路径
- Mod 配置支持自动探测标准目录结构（`Source/`、`Defs/` 等）
- 索引结果支持追踪来源（原版或具体 Mod 名称）
- 缓存指纹计算包含 Mod 配置信息
- 更新启动日志，显示 Mod 索引状态

## Impact
- Affected specs: 配置加载、索引构建、缓存管理
- Affected code:
  - `AppConfig.cs` - 配置结构扩展
  - `Program.cs` - Mod 路径处理与索引逻辑
  - `IndexCacheService.cs` - 缓存指纹计算
  - `SourceIndexer.cs` - 可选：来源追踪（本次暂不实现）
  - `DefIndexer.cs` - 可选：来源追踪（本次暂不实现）

## ADDED Requirements

### Requirement: Mod 配置支持
系统 SHALL 支持在 `config.json` 中配置多个 Mod 路径。

#### Scenario: 配置结构
- **GIVEN** 用户需要索引多个 Mod 的数据
- **WHEN** 用户在 `config.json` 中配置 `mods` 数组
- **THEN** 系统应正确解析每个 Mod 的路径配置

#### Scenario: Mod 路径配置格式
- **GIVEN** 一个 Mod 配置项
- **WHEN** 配置包含 `name` 和 `path` 字段
- **THEN** 系统应将该路径作为 Mod 根目录进行索引

#### Scenario: Mod 启用控制
- **GIVEN** 一个 Mod 配置项
- **WHEN** 配置 `enabled` 字段为 `false`
- **THEN** 系统应跳过该 Mod 的索引

### Requirement: Mod 目录自动探测
系统 SHALL 自动探测 Mod 的标准目录结构。

#### Scenario: C# 源码目录探测
- **GIVEN** 一个 Mod 根目录
- **WHEN** 该目录下存在 `Source/` 子目录
- **THEN** 系统应自动将 `Source/` 目录加入 C# 索引路径

#### Scenario: XML Defs 目录探测
- **GIVEN** 一个 Mod 根目录
- **WHEN** 该目录下存在 `Defs/` 子目录
- **THEN** 系统应自动将 `Defs/` 目录加入 XML 索引路径

#### Scenario: 多源码目录支持
- **GIVEN** 一个 Mod 配置显式指定了 `csharpPaths` 或 `xmlPaths`
- **WHEN** 这些路径存在
- **THEN** 系统应使用显式配置的路径而非自动探测

### Requirement: 缓存兼容性
系统 SHALL 在 Mod 配置变化时正确处理缓存。

#### Scenario: 缓存失效
- **GIVEN** 已有索引缓存
- **WHEN** Mod 配置列表发生变化（增删改）
- **THEN** 系统应重新构建索引而非使用旧缓存

#### Scenario: 配置指纹计算
- **GIVEN** 计算配置指纹时
- **WHEN** 包含 Mod 路径信息
- **THEN** 指纹应能唯一标识当前配置状态

### Requirement: 日志输出
系统 SHALL 在启动时输出 Mod 索引状态。

#### Scenario: Mod 加载日志
- **GIVEN** 配置了 Mod 路径
- **WHEN** 系统启动索引
- **THEN** 应输出每个 Mod 的加载状态（成功/路径不存在/已禁用）

## MODIFIED Requirements

### Requirement: 配置加载
原有配置结构保持向后兼容，新增 `mods` 字段为可选配置。

#### 配置示例
```json
{
  "csharpSourcePaths": ["D:/RimWorld/Source"],
  "xmlSourcePaths": ["D:/RimWorld/Data"],
  "mods": [
    {
      "name": "HugsLib",
      "path": "D:/RimWorld/Mods/HugsLib"
    },
    {
      "name": "JecsTools",
      "path": "D:/RimWorld/Mods/JecsTools",
      "enabled": false
    },
    {
      "name": "CustomMod",
      "path": "D:/RimWorld/Mods/CustomMod",
      "csharpPaths": ["Source", "AdditionalSource"],
      "xmlPaths": ["Defs", "MyCustomDefs"]
    }
  ],
  "skipPathSecurity": false,
  "checkUpdates": true
}
```

## REMOVED Requirements
无移除的需求。
