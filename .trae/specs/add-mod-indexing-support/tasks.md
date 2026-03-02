# Tasks

- [x] Task 1: 扩展 AppConfig 配置结构
  - [x] SubTask 1.1: 创建 `ModConfig` 记录类型，包含 `name`、`path`、`enabled`、`csharpPaths`、`xmlPaths` 字段
  - [x] SubTask 1.2: 在 `AppConfig` 中添加 `Mods` 属性（`List<ModConfig>`）
  - [x] SubTask 1.3: 确保 JSON 反序列化正确处理新增字段（向后兼容）

- [x] Task 2: 实现 Mod 目录自动探测逻辑
  - [x] SubTask 2.1: 创建 `ModPathResolver` 类，负责解析 Mod 路径配置
  - [x] SubTask 2.2: 实现自动探测 `Source/` 目录逻辑
  - [x] SubTask 2.3: 实现自动探测 `Defs/` 目录逻辑
  - [x] SubTask 2.4: 支持显式配置覆盖自动探测
  - [x] SubTask 2.5: 处理相对路径转换为绝对路径

- [x] Task 3: 更新 Program.cs 索引逻辑
  - [x] SubTask 3.1: 遍历 Mod 配置列表并收集有效路径
  - [x] SubTask 3.2: 将 Mod 的 C# 路径加入 `existingCsharpPaths`
  - [x] SubTask 3.3: 将 Mod 的 XML 路径加入 `existingXmlPaths`
  - [x] SubTask 3.4: 更新 PathSecurity 初始化以包含 Mod 路径

- [x] Task 4: 更新缓存指纹计算
  - [x] SubTask 4.1: 修改 `ComputeConfigFingerprint` 方法签名，接受 Mod 配置
  - [x] SubTask 4.2: 将 Mod 配置信息纳入指纹计算
  - [x] SubTask 4.3: 更新 `Program.cs` 中的调用

- [x] Task 5: 增强启动日志输出
  - [x] SubTask 5.1: 输出每个 Mod 的加载状态
  - [x] SubTask 5.2: 输出 Mod 路径探测结果
  - [x] SubTask 5.3: 输出 Mod 索引统计信息

# Task Dependencies
- [Task 2] depends on [Task 1]
- [Task 3] depends on [Task 2]
- [Task 4] depends on [Task 1]
- [Task 5] depends on [Task 3]
