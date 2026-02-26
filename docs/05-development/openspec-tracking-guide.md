# OpenSpec 标记追踪指南

> Sprint3-DOC3-16: 建立 OpenSpec 标记的追踪、管理和清理机制

## 概述

OpenSpec 标记用于标注代码中与特定提案/变更相关的临时代码、兼容设计或待重构区域。每个标记关联一个 `change-id`，指向 `openspec/changes/{change-id}/` 下的提案文档。

## 标记格式规范

```csharp
// OpenSpec: {change-id} - {描述}
// OpenSpec: {change-id} - 兼容设计，待{目标提案}完成后移除
```

示例:
```csharp
// OpenSpec: simplify-medicalcase-dataflow - DoctorId 到 UserId 迁移
// OpenSpec: optimize-module-list-ui - 恢复功能支持
```

## 当前统计 (2026-02-26)

- **总标记数**: ~1100 处
- **涉及提案数**: 40+ 个 change-id

### Top 10 高密度提案

| 提案 (change-id) | 标记数 | 说明 |
|-------------------|--------|------|
| simplify-workspace-architecture | 45 | 工作区架构简化 |
| simplify-desktop-data-layer | 33 | Desktop 数据层简化 |
| refactor-viewmodel-composition | 33 | ViewModel 组合重构 |
| refactor-login-authentication | 33 | 登录认证重构 |
| simplify-medicalcase-dataflow | 31 | 医案数据流简化 |
| refactor-frontend-srp-patterns | 30 | 前端 SRP 模式重构 |
| unify-navigation-architecture | 29 | 导航架构统一 |
| enhance-viewmodel-architecture | 28 | ViewModel 架构增强 |
| refactor-dto-simplification | 27 | DTO 简化重构 |
| implement-local-mode | 26 | 本地模式实现 |

## 查询方法

### 统计所有标记
```bash
grep -r "// OpenSpec:" src/ --include="*.cs" | wc -l
```

### 按提案分组统计
```bash
grep -r "// OpenSpec:" src/ --include="*.cs" | \
  sed 's/.*OpenSpec: //' | sed 's/ .*//' | \
  sort | uniq -c | sort -rn
```

### 查找特定提案的所有标记
```bash
grep -rn "// OpenSpec: {change-id}" src/ --include="*.cs"
```

### 按文件统计密度
```bash
grep -rc "// OpenSpec:" src/ --include="*.cs" | \
  grep -v ":0$" | sort -t: -k2 -rn | head -20
```

## 清理流程

### 1. 确认提案完成
- 检查 `openspec/changes/{change-id}/` 下的提案状态
- 确认所有任务已关闭

### 2. 验证标记可移除
- 逐个检查标记处的代码是否已稳定
- 兼容代码确认无调用方依赖
- 临时方案确认已被正式实现替代

### 3. 批量清理
```bash
# 预览将移除的行
grep -rn "// OpenSpec: {change-id}" src/ --include="*.cs"

# 使用 sed 移除整行注释
grep -rl "// OpenSpec: {change-id}" src/ --include="*.cs" | \
  xargs sed -i '/\/\/ OpenSpec: {change-id}/d'
```

### 4. 验证
- 编译通过: `dotnet build LYBT.All.sln`
- 测试通过: `dotnet test LYBT.All.sln --filter "FullyQualifiedName~LYBT.Tests"`
- 代码审查确认无副作用

## 管理规则

1. **新标记必须关联提案**: 禁止无 change-id 的 OpenSpec 标记
2. **清理 deadline**: 提案完成后 30 天内清理所有关联标记
3. **Sprint 审查**: 每个 Sprint 结束时审查高密度提案，评估清理优先级
4. **禁止长期保留**: 超过 90 天未清理的标记视为技术债务，强制清理

---

最后更新: 2026-02-26
