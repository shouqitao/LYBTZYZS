# Desktop 层重构优化设计文档

**日期**: 2026-03-14
**目标**: 建立可持续演进的 Desktop 层架构基础
**范围**: Desktop 层全部代码（~11,150 CS 文件，73 XAML）

---

## 执行摘要

### 当前状态
Desktop 层经过 Sprint 6 双模式架构改造后，整体架构良好，但存在以下关键问题：
- 5 个 ViewModel 过于臃肿（>400 行或注入>6服务）
- 启动流程存在 3 处同步阻塞
- XAML 样式重复定义，硬编码严重
- ViewModel 层测试覆盖率 < 20%

### 重构目标
1. **ViewModel 瘦身**: 将复杂 ViewModel 拆分为单一职责组件
2. **启动性能**: 冷启动时间从当前 >5s 优化到 <3s
3. **UI 规范化**: 建立统一设计系统，消除重复样式
4. **测试覆盖**: ViewModel 测试覆盖率达到 60%+

---

## 问题严重性排序

### P0 - 阻塞级（立即修复）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| 1 | PatientMasterDetailViewModel 注入 9 个服务 | `Patients/ViewModels/PatientMasterDetailViewModel.cs` | 严重违反 SRP |
| 2 | DatabaseInitializer 同步阻塞启动 | `DataSourceRegistrationExtensions.cs:201-207` | 首次启动阻塞 UI |
| 3 | API 健康检查阻塞启动 10 秒 | `ApiHealthCheckStartupStep.cs:44` | WebAPI 未启动时延迟 10s |
| 4 | SyncViewModel 597 行代码 | `Sync/ViewModels/SyncViewModel.cs` | 同步工作流逻辑臃肿 |
| 5 | MedicalCaseCommandsViewModel 514 行 | `MedicalCase/ViewModels/Workspace/*.cs` | 9 个命令职责过重 |

### P1 - 高危级（本周修复）

| # | 问题 | 位置 |
|---|------|------|
| 6 | LoginViewModel 509 行无测试 | `Auth/ViewModels/LoginViewModel.cs` |
| 7 | XAML 颜色硬编码 37+ 处 | 多个 XAML 文件 |
| 8 | 按钮样式重复定义 4+ 次 | `Controls.xaml`, `ButtonStyles.xaml`, `MedicalCaseStyles.xaml` |
| 9 | FontFamily 硬编码 37 处 | 多个 XAML 文件 |
| 10 | MedicalCaseMasterDetailViewModel 无测试 | 无测试文件 |
| 11 | PatientMasterDetailViewModel 无测试 | 无测试文件 |
| 12 | 模块同步初始化阻塞 | `App.xaml.cs:339-367` |

### P2 - 中危级（本月修复）

| # | 问题 | 位置 |
|---|------|------|
| 13 | Patients->MedicalCase 循环依赖 | `Patients.csproj:81` |
| 14 | UserMasterDetailViewModel 注入 7 服务 | `Users/ViewModels/*.cs` |
| 15 | XAML 资源字典合并 8 个 | `App.xaml:13-39` |
| 16 | 预热服务空实现 | `StartupOptimizationService.cs:49-74` |
| 17 | 异常处理器重复注册 | `ErrorHandlingStartupStep.cs` |
| 18 | ViewModel 测试覆盖率 < 20% | 所有 MasterDetailViewModel |
| 19 | 缺少 User Journey 测试 | 无测试目录 |
| 20 | 硬编码尺寸 117 处 | 多个 XAML |

---

## 详细设计方案

### 1. ViewModel 拆分方案

#### 1.1 PatientMasterDetailViewModel 拆分

**当前**: 418 行，注入 9 个服务
```csharp
// 当前注入
IViewModelServices, IMasterDetailServices, PatientService,
IPatientRepository, IPatientStatusHandler, IPatientImportExportHandler,
ICardReaderService, IPatientCardReaderIntegration, IDesktopCacheManager
```

**拆分后**:
```
PatientMasterDetailViewModel (核心 CRUD)
  ├── 5 个服务: IViewModelServices, IMasterDetailServices,
  │             PatientService, IPatientRepository, IDesktopCacheManager
  │
  ├── PatientCardReaderViewModel (读卡器功能，Child VM)
  │   └── 2 个服务: ICardReaderService, IPatientCardReaderIntegration
  │
  └── PatientImportExportViewModel (导入导出，Child VM)
      └── 1 个服务: IPatientImportExportHandler
```

#### 1.2 SyncViewModel 拆分

**当前**: 597 行，包含完整同步工作流

**拆分后**:
```
SyncViewModel (状态管理 + 命令绑定)
  ├── SyncDifferenceChecker (差异检查逻辑)
  ├── SyncConflictResolver (冲突处理逻辑)
  └── SyncExecutor (同步执行逻辑)
```

#### 1.3 MedicalCaseCommandsViewModel 拆分

**当前**: 514 行，9 个命令

**拆分后**:
```
MedicalCaseCommandsViewModel (核心命令: 保存/暂存/完成)
  ├── PrescriptionPrintViewModel (打印功能，Child VM)
  ├── PrescriptionImportViewModel (导入验方/历史，Child VM)
  └── MedicalCaseExportViewModel (导出 PDF，Child VM)
```

---

### 2. 启动性能优化方案

#### 2.1 延迟数据库初始化

**当前问题**:
```csharp
// DatabaseInitializer 以 Singleton 注册，立即创建 LocalDbContext
containerRegistry.RegisterSingleton<DatabaseInitializer>(resolver =>
{
    var context = resolver.Resolve<LocalDbContext>();  // 立即创建！
    ...
});
```

**优化方案**:
```csharp
// 改为延迟初始化
containerRegistry.RegisterSingleton<DatabaseInitializer>(resolver =>
{
    var loggerFactory = resolver.Resolve<ILoggerFactory>();
    return new DatabaseInitializer(
        () => resolver.Resolve<LocalDbContext>(),  // 延迟工厂
        loggerFactory.CreateLogger<DatabaseInitializer>());
});
```

#### 2.2 异步 API 健康检查

**当前**: 同步阻塞 10 秒
**优化**: 后台异步执行，不阻塞启动流程

#### 2.3 模块按需加载

**当前**: 11 个模块 `WhenAvailable` 同时加载
**优化**: 使用 `OnDemand` 按需加载，登录后根据角色动态加载

---

### 3. XAML 规范化方案

#### 3.1 消除重复样式

| 样式 | 保留 | 删除 |
|------|------|------|
| PrimaryButton | `ButtonStyles.xaml` | `Controls.xaml`, `MedicalCaseStyles.xaml`, Dialog 本地定义 |
| FormLabel | `Typography.xaml` | `DetailLabelStyle`, `FieldLabel` 等 |

#### 3.2 替换硬编码

| 硬编码类型 | 数量 | 替换为 |
|-----------|------|--------|
| FontFamily="Microsoft YaHei" | 37 | `{StaticResource PrimaryFontFamily}` |
| Foreground="#424242" 等 | 37+ | `{StaticResource SecondaryTextBrush}` |
| FontSize="14" | 117 | `{StaticResource FontSizeBody}` |
| Margin="20" | 多处 | `{StaticResource SpacingLarge}` |

#### 3.3 统一控件

提取重复 UI 模式：
- `FormField` 控件（标签+输入框+验证错误）
- `FormSection` 控件（带标题的卡片容器）

---

### 4. 测试覆盖方案

#### 4.1 ViewModel 测试框架

**新增测试文件**:
```
tests/LYBT.Tests.Desktop/
├── ViewModels/
│   ├── LoginViewModelTests.cs
│   ├── MedicalCaseMasterDetailViewModelTests.cs
│   ├── PatientMasterDetailViewModelTests.cs
│   └── SyncViewModelTests.cs
└── UserJourneys/
    ├── LoginToMedicalCaseJourneyTests.cs
    ├── ModeSwitchJourneyTests.cs
    └── SyncWorkflowJourneyTests.cs
```

#### 4.2 测试策略

- **单元测试**: 使用 NSubstitute mock 依赖
- **集成测试**: SQLite InMemory + 真实 Repository
- **User Journey**: 端到端关键流程测试

---

## 实施阶段规划

### Phase 1: 紧急修复（1-2 周）

**目标**: 修复 P0 级问题，解决启动阻塞和关键 ViewModel 臃肿

| 任务 | 负责人 | 预估工时 |
|------|--------|----------|
| PatientMasterDetailViewModel 拆分 | TBD | 2 天 |
| 延迟数据库初始化 | TBD | 1 天 |
| 异步 API 健康检查 | TBD | 1 天 |
| SyncViewModel 拆分 | TBD | 2 天 |
| MedicalCaseCommandsViewModel 拆分 | TBD | 2 天 |

**验收标准**:
- 冷启动时间 < 3 秒
- PatientMasterDetailViewModel 注入服务 < 6 个
- SyncViewModel 行数 < 300 行

### Phase 2: 测试覆盖（2-4 周）

**目标**: 核心 ViewModel 测试覆盖，建立 User Journey 框架

| 任务 | 负责人 | 预估工时 |
|------|--------|----------|
| LoginViewModel 测试 | TBD | 2 天 |
| MedicalCaseMasterDetailViewModel 测试 | TBD | 3 天 |
| PatientMasterDetailViewModel 测试 | TBD | 3 天 |
| User Journey 框架搭建 | TBD | 3 天 |
| 关键用户旅程测试 | TBD | 4 天 |

**验收标准**:
- 3 个核心 ViewModel 测试覆盖率 > 80%
- 3 个关键用户旅程测试通过

### Phase 3: UI 规范化（2-3 周）

**目标**: 消除样式重复，建立统一设计系统

| 任务 | 负责人 | 预估工时 |
|------|--------|----------|
| 按钮样式统一 | TBD | 2 天 |
| 颜色硬编码替换 | TBD | 3 天 |
| 字体硬编码替换 | TBD | 2 天 |
| 间距硬编码替换 | TBD | 2 天 |
| FormField 控件提取 | TBD | 3 天 |

**验收标准**:
- 硬编码颜色/字体减少 90%
- 样式重复定义消除
- 所有新代码使用 DesignTokens

### Phase 4: 架构完善（3-4 周）

**目标**: 解决架构债务，建立长期可维护性

| 任务 | 负责人 | 预估工时 |
|------|--------|----------|
| 循环依赖解决 | TBD | 3 天 |
| 模块按需加载 | TBD | 4 天 |
| 剩余 ViewModel 拆分 | TBD | 5 天 |
| 性能监控和回归测试 | TBD | 3 天 |

**验收标准**:
- Patients->MedicalCase 循环依赖消除
- 模块加载时间减少 50%
- 启动性能回归测试通过

---

## 风险与缓解

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| ViewModel 拆分引入回归 Bug | 中 | 高 | 每个拆分配套完整测试；灰度发布 |
| 启动优化影响功能 | 低 | 高 | 延迟初始化需确保首次使用正常；充分测试 |
| UI 规范化覆盖不全 | 中 | 低 | 分阶段进行；每阶段视觉回归测试 |
| 测试编写耗时超预期 | 高 | 中 | 优先核心路径；使用测试生成辅助工具 |

---

## 成功指标

| 指标 | 当前值 | 目标值 | 测量方式 |
|------|--------|--------|----------|
| 冷启动时间 | >5s | <3s | StartupPerformanceMonitor |
| ViewModel 最大注入服务 | 9 | <=6 | 代码审查 |
| XAML 硬编码颜色 | 37+ | <5 | 静态扫描 |
| ViewModel 测试覆盖率 | <20% | >60% | 测试报告 |
| 循环依赖数量 | 1 | 0 | 架构扫描 |

---

## 附录

### A. 关键文件路径

| 组件 | 路径 |
|------|------|
| 应用入口 | `src/Client/Desktop/Shell/App.xaml.cs` |
| DI 注册 | `src/Client/Desktop/Shell/Extensions/DataSourceRegistrationExtensions.cs` |
| 启动管道 | `src/Client/Desktop/Shell/Services/Startup/` |
| 样式定义 | `src/Client/Desktop/Shell/Styles/`, `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/` |
| ViewModels | `src/Client/Desktop/Modules/*/ViewModels/` |
| 测试项目 | `tests/LYBT.Tests.Desktop/` |

### B. 相关文档

- [双模式架构文档](docs/03-architecture/dual-mode.md)
- [Testing Trophy 架构](docs/05-development/testing-architecture.md)
- [Sprint 6 完成总结](docs/06-operations/sprint-6-completion.md)

---

**设计确认**: 待确认
**下一步**: 使用 superpowers:writing-plans 创建详细实施计划
