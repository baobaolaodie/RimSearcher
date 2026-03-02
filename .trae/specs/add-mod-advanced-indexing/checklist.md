# Checklist

## 版本目录探测
- [x] 版本目录名称列表定义完整
- [x] 版本目录优先于根目录探测
- [x] 多版本目录同时被索引
- [x] 无版本目录时正确回退到根目录
- [x] `Patches/` 目录被正确探测

## XML Patch 索引
- [x] `PatchIndexer` 类实现完整
- [x] `PatchLocation` 记录类型定义正确
- [x] XML Patch 文件解析正确（Operation/Target）
- [x] 按目标 Def 查询功能正常
- [x] 快照导出/导入正常工作

## Harmony Patch 索引
- [x] `HarmonyPatchIndexer` 类实现完整
- [x] `HarmonyPatchLocation` 记录类型定义正确
- [x] `[HarmonyPatch]` 特性解析正确
- [x] 按目标方法查询功能正常
- [x] 快照导出/导入正常工作

## 程序集成
- [x] 新索引器正确初始化
- [x] Mod Patches 目录被扫描
- [x] 缓存快照结构更新正确
- [x] 缓存指纹包含新索引信息

## 工具实现
- [x] `list_patches` 工具注册成功
- [x] XML Patch 查询返回正确结果
- [x] Harmony Patch 查询返回正确结果
- [x] `inspect` 工具附加 Patch 提示

## 构建验证
- [x] 项目构建成功无错误
- [x] 向后兼容，无 Mod 配置时正常工作
