# Findings: Desktop 层架构分析

> **分析日期**: 2026-03-14
> **分析范围**: Desktop 层全部代码（~11,150 CS 文件，73 XAML）

---

## Summary

通过 6 个维度的全面分析，识别出 **25 个架构问题**，按严重性分为：
- P0 (阻塞级): 5 项
- P1 (高危级): 7 项
- P2 (中危级): 8 项
- P3 (低危级): 5 项

---

## Research Findings

### 1. ViewModel 复杂度分析

**总计 17 个 ViewModel，分布如下**：

| 模块 | ViewModel 数量 |
|------|---------------|
| MedicalCase | 6 |
| Formula | 2 |
| Herbs | 1 |
| Patients | 1 |
| Users | 1 |
| Auth | 1 |
| Registration | 1 |
| Sync | 2 |

**Top 5 复杂 ViewModel**：

| ViewModel | 行数 | 注入服务 | 优先级 |
|-----------|------|----------|--------|
| SyncViewModel | 597 | 4 | P0 |
| MedicalCaseCommandsViewModel | 514 | 6 | P0 |
| LoginViewModel | 509 | 6 | P1 |
| PatientMasterDetailViewModel | 418 | **9** | P0 |
| UserMasterDetailViewModel | 409 | 7 | P1 |

### 2. 启动性能分析

**启动管道步骤**：

| 步骤 | 名称 | 耗时风险 |
|------|------|----------|
| 1 | 错误处理初始化 | 低 |
| 2 | 模块协调器初始化 | 低 |
| 3 | 核心服务初始化 | **中** (预热) |
| 4 | API 健康检查 | **高** (10s 超时) |
| 5 | 应用预热 | **中** (空实现) |

**关键阻塞点**：
1. DatabaseInitializer 同步创建数据库
2. API 健康检查 10 秒阻塞
3. 11 个模块 WhenAvailable 同步加载

### 3. 模块依赖分析

**项目数量**：
- Modules: 8 个
- Core: 8 个
- Shell: 1 个
- Roles: 2 个

**发现的循环依赖**：
```
Patients -> MedicalCase (csproj:81)
```

**架构合规性**：总体良好，Contracts 层正确解耦

### 4. 双模式架构评估

**结论**：实现质量 **优秀**

| 检查项 | 状态 |
|--------|------|
| Repository 工厂注册 | 通过 |
| 无硬编码模式判断 | 通过 |
| 单例服务不直接依赖 Repository | 通过 |
| 本地基础设施始终注册 | 通过 |
| 模式切换事件通知 | 通过 |

### 5. XAML 分析

**统计**：
- XAML 文件总数: 73 个
- Converter 文件: 19 个

**硬编码问题**：
- FontFamily="Microsoft YaHei": 37 处
- 颜色硬编码: 37+ 处
- FontSize="14": 117 处

**重复样式**：
- PrimaryButton: 4+ 处定义
- FormLabel: 3 种命名

### 6. 测试覆盖分析

**测试数量对比**：

| 测试项目 | 测试方法数 | 策略 |
|---------|-----------|------|
| LYBT.Tests.Desktop | ~548 | SQLite + NSubstitute |
| LYBT.Tests.Server | ~485 | SQL Server + Respawn |

**测试缺口**：
- MedicalCaseMasterDetailViewModel: 无测试
- PatientMasterDetailViewModel: 无测试
- LoginViewModel: 无测试
- SyncViewModel: 无测试

**ViewModel 层测试覆盖率**: < 20%

---

## Technical Decisions

| 决策 | 理由 |
|------|------|
| 优先拆分 ViewModel | 解决最严重的 SRP 违反 |
| 延迟初始化数据库 | 避免首次启动阻塞 |
| 统一 ButtonStyles.xaml | 消除样式重复的最小侵入方案 |
| 保留双模式架构 | 当前实现已优秀，无需改动 |

---

## Issues Encountered

### P0 级问题（立即修复）

1. **PatientMasterDetailViewModel 注入 9 个服务** - 严重违反 SRP
2. **DatabaseInitializer 同步阻塞启动** - 首次启动阻塞 UI
3. **API 健康检查阻塞启动 10 秒** - WebAPI 未启动时延迟
4. **SyncViewModel 597 行代码** - 同步工作流逻辑臃肿
5. **MedicalCaseCommandsViewModel 514 行** - 9 个命令职责过重

### P1 级问题（本周修复）

6. **LoginViewModel 509 行无测试** - 核心登录逻辑无测试
7. **XAML 颜色硬编码 37+ 处** - 主题切换困难
8. **按钮样式重复定义 4+ 次** - 维护成本高
9. **FontFamily 硬编码 37 处** - 未使用 DesignTokens
10. **MedicalCaseMasterDetailViewModel 无测试**
11. **PatientMasterDetailViewModel 无测试**
12. **模块同步初始化阻塞** - 11 个模块同时加载

---

## References

- 详细问题清单: 见 `docs/plans/2026-03-14-desktop-refactoring-design.md`
