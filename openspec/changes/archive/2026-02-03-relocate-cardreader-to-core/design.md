# relocate-cardreader-to-core 设计文档

## 概述

基于 [proposal.md](./proposal.md) 的详细技术设计。将CardReader模块从Modules目录迁移到Core目录。

## 架构决策

### ADR-1: 使用git mv保留历史

**状态**: 已采纳

**背景**: 目录迁移需要保留Git提交历史

**决策**: 使用`git mv`命令而非手动删除+创建

**后果**:
- 正面: 保留完整Git历史，便于追溯
- 负面: 无

## 实现策略

### 策略选择

分两步执行：先移动目录，再更新所有引用路径。

### 关键实现点

1. 目录迁移使用`git mv`
2. 引用路径更新需要计算正确的相对路径

## 变更清单

### 移动目录

| 原路径 | 新路径 |
|--------|--------|
| `src/Client/Desktop/Modules/LYBT.Desktop.CardReader/` | `src/Client/Desktop/Core/LYBT.Desktop.CardReader/` |

### 修改文件

| 文件路径 | 修改内容 |
|----------|----------|
| `LYBT.Desktop.sln:40` | `Modules/` → `Core/` |
| `LYBT.All.sln:166` | `Modules/` → `Core/` |
| `Shell/LYBT.Desktop.Shell.csproj:96` | `..\Modules\` → `..\Core\` |
| `Clinical/LYBT.Desktop.Clinical.csproj:91` | `..\..\Modules\` → `..\..\Core\` |
| `Patients/LYBT.Desktop.Patients.csproj:90` | `..\LYBT.Desktop.CardReader\` → `..\..\Core\LYBT.Desktop.CardReader\` |

### 路径变更详情

```
Shell.csproj (位于 src/Client/Desktop/Shell/)
  当前: ..\Modules\LYBT.Desktop.CardReader\...
  修改: ..\Core\LYBT.Desktop.CardReader\...

Clinical.csproj (位于 src/Client/Desktop/Roles/LYBT.Desktop.Clinical/)
  当前: ..\..\Modules\LYBT.Desktop.CardReader\...
  修改: ..\..\Core\LYBT.Desktop.CardReader\...

Patients.csproj (位于 src/Client/Desktop/Modules/LYBT.Desktop.Patients/)
  当前: ..\LYBT.Desktop.CardReader\...
  修改: ..\..\Core\LYBT.Desktop.CardReader\...
```

## 依赖关系

### 变更顺序

```
git mv 目录 ──► 更新 .sln ──► 更新 .csproj ──► 编译验证
```

Phase 1必须先完成目录移动，否则引用路径更新后会找不到文件。

## 测试策略

### 编译验证

- `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- `dotnet build LYBT.All.sln -c Release --no-restore`

### 功能验证

- 应用启动正常
- CardReaderModule加载成功（检查日志）

## 风险缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 相对路径计算错误 | 低 | 中 | 逐个验证路径正确性 |
| 遗漏引用 | 低 | 中 | Grep搜索确保无遗漏 |

## 回滚计划

如果变更失败:
1. `git reset --hard HEAD~1` 回退提交
2. 或手动`git mv`回原位置

---

**设计者**: Claude Code
**日期**: 2026-01-20
**状态**: 待审批
