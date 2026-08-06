# 架构测试重构计划

**日期**: 2026-08-06
**目标**: 清理、升级、完善 `tests/LYBT.Tests.Architecture/` 项目

## 问题清单

| # | 问题 | 严重度 | 状态 |
|---|------|:---:|:---:|
| 1 | `ArchTests.Assemblies` 和 `AggregateRootArchTests.ServerAssemblies` 缺少 `LYBT.Module.Reports` 和 `LYBT.Module.Registration` | P0 | ⬜ |
| 2 | `ArchTests` 和 `ServerArchTests` 的 API 版本测试重复 | P1 | ⬜ |
| 3 | 授权相关测试分散（3处） | P1 | ⬜ |
| 4 | 命名前缀混乱（P07_/Batch2_/AR001_/A-09:） | P1 | ⬜ |
| 5 | UltraThink 残留测试（Workflow/Bus/Engine 命名检查） | P2 | ⬜ |
| 6 | Batch2_ 前缀标记应替换为规则编号 | P2 | ⬜ |
| 7 | 缺少规则映射表（测试方法↔架构规则↔文档） | P2 | ⬜ |

## 重构步骤

### Step 1: 统一程序集清单
- 创建 `TestAssemblies.cs` 公共常量类
- 包含所有 Server + Desktop + Shared 程序集
- 所有测试文件引用此公共清单

### Step 2: 消除重复测试
- 删除 `ArchTests.ApiVersionTests_Controllers_Should_Use_V1_Routes_Only`（保留 ServerArchTests 版本）
- 合并 3 个授权测试为 1 个 `Authorization_Rules`

### Step 3: 规范化命名
- 统一格式：`规则编号_描述`
- 示例：`P07_ModuleIsolation`、`P10_ServiceNoDirectDbContext`、`P06_NoReverseDependencies`

### Step 4: 按职责整理文件
| 文件 | 职责 |
|------|------|
| `ArchTests.cs` | 通用分层依赖 + 命名规范 + 防回潮 |
| `ServerArchTests.cs` | Server 端专属规则 |
| `DesktopLayerArchTests.cs` | Desktop 层专属规则 |
| `AggregateRootArchTests.cs` | DDD 聚合根模式 |
| `LocalWebApiPatternTests.cs` | LocalWebAPI 模式 |
| `AntiMockRuleTests.cs` | 测试质量守卫 |
| `CustomControlArchTests.cs` | 自定义控件规范 |

### Step 5: 清理过时测试
- 删除 `NamingConventionTests_Should_Not_Contain_Pipeline_Names`（UltraThink 残留）
- 删除 `NamingConventionTests_Should_Not_Have_Workflow_Namespaces`（UltraThink 残留）
- 移除所有 `Batch2_` 前缀，改为规则编号

### Step 6: 新增规则映射表
- 创建 `ARCHITECTURE-RULES.md`
- 列出：规则编号 → 测试方法 → 文档引用 → 简述

### Step 7: 验证
- `dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告
- `dotnet test tests/LYBT.Tests.Architecture/` 全部通过
- 测试数从 92 调整为 ~85（删除重复/过时，覆盖不减）

## 验收标准
- [ ] 程序集清单统一，Reports/Registration 在所有测试文件中覆盖
- [ ] 无重复测试
- [ ] 命名统一为 `规则编号_描述` 格式
- [ ] 所有测试有对应的架构规则映射
- [ ] build 0 错误 0 警告
- [ ] 架构测试全部通过
