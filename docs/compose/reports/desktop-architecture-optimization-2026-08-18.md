# Desktop 架构优化分析报告（2026-08-18）

> **分析范围**: LYBTZYZS Desktop 项目全部 15 个项目（6 Core + 6 Modules + 2 Roles + 1 Shell）
> **分析依据**: 蓝图 §3.5 分层规则（View←VM→Service→Repository→IApiClient→SwitchingApiClient）
> **分析方法**: 代码扫描 + 设计文档对比 + 模式一致性检查

---

## 架构全景

```
┌─────────────────────────────────────────────────────────────────┐
│ L0: View (XAML) ← binding → ViewModel                           │
│     47 ViewModels, 6 种基类, 1 个 BindableBase 遗留              │
├─────────────────────────────────────────────────────────────────┤
│ L1: Service 层                                                  │
│     ~20 Services (5 Remote*Service + 6 MedicalCase*Service +    │
│     9 Infrastructure Services)                                  │
├─────────────────────────────────────────────────────────────────┤
│ L2: Repository 层                                               │
│     6 Repositories (全部注入 IApiClient 整体)                    │
├─────────────────────────────────────────────────────────────────┤
│ L3: IApiClient (统一契约面)                                      │
│     10 个子接口: IApiClientPatients/Herbs/Formulas/...           │
├─────────────────────────────────────────────────────────────────┤
│ L4: SwitchingApiClient (URL 路由)                               │
│     ├─ Remote → RefitApiClient → 10 个 Refit 适配器              │
│     └─ Local → HttpClientApiClient → 10 个 HttpClient 适配器     │
└─────────────────────────────────────────────────────────────────┘
```

---

## P0: 严重偏差（需立即修复）

### P0-1: Repository 生命周期与蓝图不一致

| 维度 | 当前状态 | 设计要求 | 偏差 |
|------|----------|----------|------|
| **注册方式** | `containerRegistry.Register<IRepo>(resolver => ...)` (Transient) | `RegisterSingleton<IRepo, Repo>()` (Singleton) | 生命周期不符 |
| **影响** | 每次解析创建新实例，无状态复用 | 单例复用，减少 GC 压力 | 性能浪费 |
| **涉及文件** | `DataSourceRegistrationExtensions.cs` 6 处 | 蓝图 §3.2「Repository \| Singleton」 | — |

**建议修复方案**:
```csharp
// 当前（Transient）
containerRegistry.Register<IPatientRepository>(resolver =>
    new PatientRepository(resolver.Resolve<IApiClient>(), ...));

// 修复为（Singleton）
containerRegistry.RegisterSingleton<IPatientRepository>(resolver =>
    new PatientRepository(resolver.Resolve<IApiClient>(), ...));
```

> **注意**: Repository 是无状态的（仅持有 IApiClient 引用），Singleton 安全。

---

### P0-2: Service 层注入模式不一致

| 模式 | 服务 | 注入 | 蓝图要求 |
|------|------|------|----------|
| **模式 A** | RemotePatientService, RemoteHerbService, RemoteFormulaService, RemoteUserService, RemoteRegistrationService | `IXxxRepository` | ✅ VM→Service→Repository→IApiClient |
| **模式 B** | MedicalCaseQueryService, MedicalCaseCommandService, MedicalCaseLifecycleService, MedicalCaseService, AuditLogService, ReportService | `IApiClient`（整体） | ⚠️ 跳过 Repository 层 |

**偏差分析**:
- 模式 B 的服务直接注入 `IApiClient`，通过 `_apiClient.Xxx.GetXxxAsync(...)` 调用
- 蓝图 §3.5 要求 `VM→Service→Repository→IApiClient`
- MedicalCase 模块的 Service 绕过了 Repository 层

**建议修复方案**:
1. **短期**: 为 MedicalCase 域创建 `MedicalCaseRepository`（已有）和 `AuditLogRepository`、`ReportRepository`，让 Service 注入 Repository
2. **中期**: 统一所有 Service 注入 Repository，Repository 注入 IApiClient 子接口
3. **长期**: Repository 注入具体的 `IApiClientXxx` 而非整个 `IApiClient`

> **例外**: MedicalCase 模块是 Service 化模块（非 CQRS），可能有特殊设计考量。需确认蓝图是否允许例外。

---

## P1: 重要偏差（应尽快修复）

### P1-1: Repository 注入整个 IApiClient 而非子接口

| 维度 | 当前状态 | 最佳实践 | 偏差 |
|------|----------|----------|------|
| **注入方式** | `IApiClient _apiClient` | `IApiClientPatients _patients` | 过度宽泛 |
| **调用方式** | `_apiClient.Patients.GetPatientsAsync(...)` | `_patients.GetPatientsAsync(...)` | 多一层间接 |
| **涉及文件** | 6 个 Repository | — | — |

**影响**:
- Repository 可以访问所有域的 API（违反最小权限原则）
- 编译时无法发现不必要的域依赖

**建议修复方案**:
```csharp
// 当前
public PatientRepository(IApiClient apiClient, ILogger<PatientRepository> logger)
    : base(logger, apiClient.Patients) { _apiClient = apiClient; }

// 优化为
public PatientRepository(IApiClientPatients patients, ILogger<PatientRepository> logger)
    : base(logger, patients) { }
```

> **阻断因素**: 需要同时修改 DI 注册（从 IApiClient 解析子接口）。SwitchingApiClient 已支持子接口属性访问，技术上可行。

---

### P1-2: Service 层 Remote*Service 命名不一致

| 服务 | 命名模式 | 说明 |
|------|----------|------|
| RemotePatientService | `Remote*` | ✅ 表示 HTTP 数据服务 |
| RemoteHerbService | `Remote*` | ✅ |
| RemoteFormulaService | `Remote*` | ✅ |
| RemoteUserService | `Remote*` | ✅ |
| RemoteRegistrationService | `Remote*` | ✅ |
| MedicalCaseService | 无前缀 | ⚠️ 与其他模块不一致 |
| MedicalCaseQueryService | 无前缀 | ⚠️ |
| MedicalCaseCommandService | 无前缀 | ⚠️ |
| AuditLogService | 无前缀 | ⚠️ |
| ReportService | 无前缀 | ⚠️ |

**偏差分析**:
- MedicalCase 模块的 Service 无 `Remote*` 前缀
- 其他模块的 Service 有 `Remote*` 前缀
- 原因可能是 MedicalCase 是 Service 化模块（非标准 CRUD）

**建议修复方案**:
- **选项 A**: 统一加 `Remote*` 前缀（破坏性变更，需更新所有引用）
- **选项 B**: 统一去掉 `Remote*` 前缀（推荐，更简洁）
- **选项 C**: 保持现状，但更新文档说明命名约定

---

### P1-3: PrescriptionItemViewModel 使用 Prism BindableBase

| 维度 | 当前状态 | 设计要求 | 偏差 |
|------|----------|----------|------|
| **基类** | `BindableBase` (Prism) | `ObservableObject` (CommunityToolkit) | 遗留技术 |
| **位置** | `MedicalCase/ViewModels/Items/` | — | — |
| **文档** | `02-desktop.md` 记录为历史代码 | 新代码禁止使用 BindableBase | — |

**影响**:
- 与 ADR-0012（CommunityToolkit.Mvvm 采纳）不一致
- Mapperly 兼容性是历史原因，现在可能已解决

**建议修复方案**:
1. 检查 Mapperly 是否仍需要 BindableBase（查看 `PrescriptionMapper`）
2. 如果不需要，迁移到 `ObservableObject` + `[ObservableProperty]`
3. 如果需要，保持现状但添加注释说明原因

---

### P1-4: HerbItemControlViewModel 和 HerbListControlViewModel 未使用 VM 基类

| 维度 | 当前状态 | 设计要求 | 偏差 |
|------|----------|----------|------|
| **基类** | `ObservableObject` (直接) | 应使用 VM 基类 | 缺少 IsBusy/Logger/Events |
| **位置** | `Controls/HerbItem/` 和 `Controls/HerbList/` | — | — |

**影响**:
- 无法使用 `IsBusy` 属性（需手动实现）
- 无法使用 `Logger`（需手动注入）
- 无法使用 `Events`（EventSubscriptionManager）

**建议修复方案**:
- 如果是轻量控件 ViewModel，`ObservableObject` 可能足够
- 如果需要日志/忙碌状态，应继承 `CoreViewModelBase`

---

## P2: 改进建议（可择机优化）

### P2-1: L4 适配器合并空间评估

| 适配器对 | Refit (Remote) | HttpClient (Local) | 合并可能性 |
|----------|----------------|-------------------|-----------|
| Patient | PatientApiClient | PatientsHttpApiClient | ❌ 不可合并 |
| Herb | HerbApiClient | HerbsHttpApiClient | ❌ 不可合并 |
| Formula | FormulaApiClient | FormulasHttpApiClient | ❌ 不可合并 |
| MedicalCase | MedicalCaseApiClient | MedicalCasesHttpApiClient | ❌ 不可合并 |
| Identity | IdentityApiClient | IdentityHttpApiClient | ❌ 不可合并 |
| Registration | RegistrationApiClient | RegistrationsHttpApiClient | ❌ 不可合并 |
| Reports | ReportsApiClient | ReportsHttpApiClient | ❌ 不可合并 |
| Configuration | ConfigurationApiClient | ConfigurationHttpApiClient | ❌ 不可合并 |
| Deploy | DeployApiClient | DeployHttpApiClient | ❌ 不可合并 |
| Diagnostics | DiagnosticsApiClient | DiagnosticsHttpApiClient | ❌ 不可合并 |

**结论**: 20 个适配器**不可合并**，原因：
1. **技术栈不同**: Refit 使用源生成 HTTP 客户端，HttpClient 使用手动 URL 构建
2. **序列化不同**: Remote 用 camelCase，Local 用 PascalCase
3. **Handler 链不同**: Remote 有 TokenRefresh + Auth + Logging Handler，Local 无
4. **设计依据**: ADR-0009 URL 驱动双模式架构明确要求双轨实现

**潜在优化**:
- 可以提取 Refit 适配器的共性到基类（如 `RefitApiClientBase<TApi, TListDto, TDetailDto>`）
- 但收益有限（每个适配器仅 ~50 行，共性不大）

---

### P2-2: TODO/FIXME 清理

| 位置 | 内容 | 优先级 | 建议 |
|------|------|--------|------|
| `ReportsModule.cs:12` | `// TODO: 后续迭代完善报表功能` | P3 | 保留，功能待实现 |
| `PrescriptionItemViewModel.cs:26` | `// TODO: 静态mapper与DI风格不一致` | P2 | 评估是否可迁移 |
| `ClinicalHomeViewModel.cs:218` | `// TODO: US-SHELL-005 - 从服务获取今日统计数据` | P3 | 保留，功能待实现 |
| `MedicalCaseWorkspaceViewModel.cs:35` | `// TODO: 超大类型，建议拆分` | P1 | 评估是否需要拆分 |

---

### P2-3: MedicalCaseWorkspaceViewModel 类型过大

| 维度 | 当前状态 | 设计要求 | 偏差 |
|------|----------|----------|------|
| **行数** | 未精确统计，但 TODO 标记为超大类型 | VM 大小限制：≤600 行 | 可能超标 |
| **拆分建议** | TODO 提到 `docs/compose/reports/code-review-duplicates.md` | Components 拆分模式 | 待评估 |

**建议修复方案**:
1. 统计 `MedicalCaseWorkspaceViewModel` 行数
2. 如果 >600 行，按 Components 模式拆分：
   - `MedicalCaseWorkspaceViewModel`（协调器）
   - `Components/PatientSelectionHandler.cs`
   - `Components/PendingQueueHandler.cs`
   - `Components/CardReaderHandler.cs`

---

### P2-4: 配置管理双路径

| 配置文件 | 位置 | 用途 | 设计依据 |
|----------|------|------|----------|
| `appsettings.json` | Shell/bin/ | 应用默认配置 | 标准 .NET 配置 |
| `user-settings.json` | %LOCALAPPDATA%/LYBTZYZS/ | 用户偏好（连接 URL 等） | 构建不覆盖 |
| `clinic-settings.json` | Shell/bin/ | 诊所特定配置 | 业务配置 |
| `feature-toggles.json` | Shell/bin/ | 功能开关 | US-CFG-004 |

**评估**: 双配置路径是**设计意图**（避免 `--no-incremental` 构建覆盖用户配置），无问题。

---

## P3: 低优先级（可长期跟踪）

### P3-1: CrossModule 接口位置

| 接口 | 位置 | 设计要求 | 偏差 |
|------|------|----------|------|
| `IHerbSearchProvider` | `Contracts/Services/CrossModule/` | 应在 Infrastructure/Services/CrossModule | ⚠️ 位置不符 |
| `IFormulaSearchProvider` | `Contracts/Services/CrossModule/` | 同上 | ⚠️ 位置不符 |

**说明**: 这些是 Desktop 侧的跨模块接口，与 Server 侧的 `ICrossModuleService` 体系不同。位置在 Contracts 是合理的（供其他模块引用）。

---

### P3-2: ViewModel 基类使用统计

| 基类 | 使用数量 | 占比 | 状态 |
|------|----------|------|------|
| NavigableViewModelBase | ~25 | 53% | ✅ 主流 |
| DialogViewModelBase | 6 | 13% | ✅ |
| MasterDetailViewModelBase | 5 | 11% | ✅ |
| ChildViewModelBase | 5 | 11% | ✅ |
| EditorViewModelBase | 4 | 9% | ✅ |
| ConnectionTestViewModelBase | 2 | 4% | ✅ |
| ObservableObject (直接) | 2 | 4% | ⚠️ 无基类功能 |
| BindableBase (Prism) | 1 | 2% | ⚠️ 遗留 |

**评估**: 基类使用**基本一致**，仅 3 个例外（2 个 ObservableObject + 1 个 BindableBase）。

---

### P3-3: 空 catch 块

| 位置 | 数量 | 说明 |
|------|------|------|
| Foundation 层 | ~15 | 多数有日志记录 |
| Infrastructure 层 | ~10 | 多数有日志记录 |
| Modules 层 | ~3 | 需逐个检查 |

**评估**: 需要逐个检查是否真正为空（无日志/无注释）。初步扫描显示大部分有 `Logger.LogError`，不是真正空 catch。

---

## 总结

### 关键发现

1. **L4 适配器层设计合理**: 20 个适配器（10 Refit + 10 HttpClient）是 ADR-0009 双模式架构的必然产物，**不可合并**
2. **Repository 生命周期偏差**: Transient vs Singleton（蓝图要求 Singleton）
3. **Service 层注入不一致**: 5 个 Service 注入 Repository，6 个 Service 注入 IApiClient（跳过 Repository）
4. **ViewModel 基类基本一致**: 仅 3 个例外（2 ObservableObject + 1 BindableBase）
5. **配置管理双路径是设计意图**: 无问题
6. **TODO/FIXME 可控**: 4 个 TODO，无 FIXME

### 修复优先级

| 优先级 | 问题 | 工作量 | 影响 |
|--------|------|--------|------|
| **P0-1** | Repository Transient→Singleton | 小（改 6 行） | 性能优化 |
| **P0-2** | Service 层注入不一致 | 大（需创建 Repository + 重构 Service） | 架构一致性 |
| **P1-1** | Repository 注入整个 IApiClient | 中（改 DI + Repository 构造函数） | 最小权限 |
| **P1-2** | Service 命名不一致 | 小（重命名 + 更新引用） | 代码可读性 |
| **P1-3** | PrescriptionItemViewModel BindableBase | 中（需验证 Mapperly 兼容性） | 技术栈统一 |
| **P1-4** | HerbItem/HerbList 未用 VM 基类 | 小（继承 CoreViewModelBase） | 功能完整性 |

### 建议执行顺序

1. **第一批（P0-1）**: 修复 Repository 生命周期（1 天）
2. **第二批（P1-2）**: 统一 Service 命名（1 天）
3. **第三批（P1-1）**: Repository 注入子接口（2 天）
4. **第四批（P0-2）**: Service 层注入统一（3-5 天，需设计评审）
5. **第五批（P1-3 + P1-4）**: ViewModel 基类统一（2 天）

---

## 附录：代码统计

| 指标 | 数值 |
|------|------|
| Desktop 项目数 | 15 |
| Core 项目 | 6 (Contracts/Foundation/Infrastructure/Controls/Printing/LocalWebAPI) |
| Module 项目 | 6 (Auth/Catalog/MedicalCase/Patients/Registrations/Users) |
| Role 项目 | 2 (Admin/Clinical) |
| Shell 项目 | 1 |
| L4 适配器 | 20 (10 Refit + 10 HttpClient) |
| Repository | 6 |
| Service | ~20 |
| ViewModel | 47 |
| DI 注册点 | ~75 (Shell 60 + Modules 15) |
| TODO/FIXME | 4 |
| 空 catch 块 | ~28 (大部分有日志) |

---

*报告生成时间: 2026-08-18 20:00 CST*
*分析工具: 代码扫描 + 设计文档对比*
*下次审查建议: P0 修复完成后*
