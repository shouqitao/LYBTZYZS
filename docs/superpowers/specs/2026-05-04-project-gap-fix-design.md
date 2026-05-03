# 项目差距修复清单 — 设计文档

> **日期**: 2026-05-04
> **状态**: 设计阶段
> **策略**: 模块逐个推进 — 每模块端到端修复 (API对齐 → 代码质量 → 测试覆盖)

---

## 一、总体概况

### 1.1 差距来源

| 来源 | 文件 |
|------|------|
| API端点差距分析 | `docs/api-endpoint-gap-report.md` |
| 远程vs本地API差异 | `docs/remote-vs-local-api-gap-report.md` |
| WebAPI架构审查 | `docs/03-architecture/2026-03-31-webapi-architecture-review.md` |
| WebAPI代码审查 | `docs/code-review/webapi-code-review-report.md` |
| 本地API对齐计划 | `docs/local-api-alignment-plan.md` |
| 源码TODO/FIXME扫描 | `src/` 目录 grep |

### 1.2 模块优先级

| 阶段 | 模块 | Local Refit缺失 | 代码质量问题 | 理由 |
|------|------|----------------|-------------|------|
| Phase 1 | MedicalCase | 11 | MedicalCaseController 20+方法 | 核心临床流程，差距最大 |
| Phase 2 | Registration | 3 | — | 门诊核心（候诊/开始就诊） |
| Phase 3 | Patients | 3 | — | 患者管理基础 |
| Phase 4 | Herbs | 3 | — | 中药数据管理 |
| Phase 5 | Formula | 4 | — | 验方管理 |
| Phase 6 | Users | 0 | UserService 400+行 | 无缺失但需重构 |
| Phase 7 | Auth | 0 | AuthService 845行 | 无缺失但需重构 |
| Phase 8 | 跨模块收尾 | — | CORS、TODO、文档 | 全局问题统一处理 |

---

## 二、每阶段执行模板

每个模块阶段按以下三步执行：

### Step 1: API对齐

**目标**: 离线模式功能与在线模式一致。

**具体工作**:
1. 补齐 `ILocal{Module}Api.cs` 中缺失的 Refit 方法声明
2. 返回类型差异通过 Repository 层适配器处理（Remote 解包 `ApiResponse<T>.Data`，Local 直接使用），不改变 Local Controller 行为
3. 统一分页：Local 接口支持 `PagedResult<T>` 返回
4. 统一批量操作参数：Local 使用 `BatchDeleteInputDto`（与 Remote 一致）
5. 在 `LocalWebAPI/Controllers/{Module}Controller.cs` 中确保对应端点存在

**验收标准**:
- `dotnet build LYBT.Desktop.sln` 零错误
- 所有 Remote Refit 方法在 Local Refit 中有对应声明
- 返回类型签名一致

### Step 2: 代码质量

**目标**: 消除 God Class、清理 TODO、改善可维护性。

**具体工作**:
1. 对过大的服务类进行拆分（仅限本阶段模块）
2. 清理模块内的 TODO/FIXME/HACK 注释
3. 确保异步方法支持 CancellationToken
4. 检查控制器方法是否职责单一

**验收标准**:
- 拆分后的服务类不超过 300 行
- TODO 注释归零或关联到具体 Issue
- 编译通过，现有测试不回归

### Step 3: 测试覆盖

**目标**: 验证离线/在线行为一致。

**具体工作**:
1. 为补齐的 Local API 端点补充集成测试
2. 验证 Remote/Local 返回值结构一致
3. 补充边界条件测试

**验收标准**:
- `dotnet test` 全部通过
- 新增端点有对应测试覆盖

---

## 三、各阶段详细清单

### Phase 1: MedicalCase (医案)

**差距**: 11个 Local Refit 方法缺失

#### API对齐

| 缺失端点 | HTTP | Remote方法 | 需操作 |
|----------|------|-----------|--------|
| `/{id}/close` | PUT | `CloseCaseAsync` | 补齐 ILocalMedicalCaseApi + 验证 LocalWebAPI |
| `/{id}/suspend` | PUT | `SuspendAsync` | 同上 |
| `/{id}/cancel` | PUT | `CancelMedicalCaseAsync` | 同上 |
| `/{id}/status` | PUT | `UpdateStatusAsync` | 同上 |
| `/{id}/prescription-flag` | PUT | `SetPrescriptionFlagAsync` | 同上 |
| `/{id}/print-completed` | PUT | `RecordPrintCompletedAsync` | 同上 |
| `/{id}/permissions` | GET | `GetPermissionsAsync` | 同上 |
| `/query` | GET | `QueryMedicalCasesAsync` | 同上 |
| `/search` | GET | `SearchMedicalCasesAsync` | 同上 |
| `/batch-details` | POST | `GetBatchDetailsAsync` | 同上 |
| `/batch-delete` | POST | `BatchDeleteAsync` | 同上 |

**返回类型统一**: Local Controller 已直接返回 DTO（无 `ApiResponse<T>` 包装），差异在 Repository 层通过适配器处理，不改变 Local Controller 行为。具体：Remote Repository 解包 `response.Data`，Local Repository 直接使用返回值。

#### 代码质量

- MedicalCaseController 拆分评估（20+方法 → 按职责分组）
- 清理 `MedicalCaseCommandService.cs` 中 3 处价格刷新 TODO

#### 测试覆盖

- 为 11 个补齐端点补充 LocalWebAPI 集成测试

---

### Phase 2: Registration (挂号)

**差距**: 3个 Local Refit 方法缺失

#### API对齐

| 缺失端点 | HTTP | Remote方法 | 需操作 |
|----------|------|-----------|--------|
| `/queue` | GET | `GetQueueAsync` | 补齐 ILocalRegistrationApi |
| `/{id}/start-visit` | PUT | `StartVisitAsync` | 同上 |
| `/{id}/cancel` | PUT | `CancelAsync` | 同上 |

#### 测试覆盖

- 候诊队列、开始就诊、取消挂号的集成测试

---

### Phase 3: Patients (患者)

**差距**: 3个 Local Refit 方法缺失

#### API对齐

| 缺失端点 | HTTP | Remote方法 | 需操作 |
|----------|------|-----------|--------|
| `/{id}/toggle-status` | POST | `ToggleStatusAsync` | 补齐 ILocalPatientApi |
| `/export` | GET | `ExportPatientsAsync` | 同上 |
| `/import-template` | GET | `ExportTemplateAsync` | 同上 |

#### 代码质量

- 导出/导入功能评估是否需要离线支持

---

### Phase 4: Herbs (中药)

**差距**: 3个 Local Refit 方法缺失

#### API对齐

| 缺失端点 | HTTP | Remote方法 | 需操作 |
|----------|------|-----------|--------|
| `/{id}/toggle-status` | POST | `ToggleStatusAsync` | 补齐 ILocalHerbApi |
| `/export` | GET | `ExportHerbsAsync` | 同上 |
| `/import-template` | GET | `ExportTemplateAsync` | 同上 |

---

### Phase 5: Formula (验方)

**差距**: 4个 Local Refit 方法缺失

#### API对齐

| 缺失端点 | HTTP | Remote方法 | 需操作 |
|----------|------|-----------|--------|
| `/{id}/clone` | POST | `CloneFormulaAsync` | 补齐 ILocalFormulaApi |
| `/{id}/toggle-status` | POST | `ToggleStatusAsync` | 同上 |
| `/export` | GET | `ExportFormulasAsync` | 同上 |
| `/import-template` | GET | `ExportTemplateAsync` | 同上 |

---

### Phase 6: Users (用户)

**差距**: 无 API 缺失，需代码质量重构

#### 代码质量

- UserService (400+行) 拆分为 UserQueryService + UserCommandService
- 统一 Remote/Local 批量操作参数（`BatchDeleteInputDto` vs `List<Guid>`）
- 统一返回类型包装

#### 测试覆盖

- 重构后回归测试

---

### Phase 7: Auth (认证)

**差距**: 无 API 缺失，需代码质量重构

#### 代码质量

- AuthService (845行) 拆分为：
  - AuthLoginService（登录/登出/自动登录）
  - AuthTokenService（JWT/RefreshToken 管理）
  - AuthPolicyService（授权策略）
- 清理遗留注释和废弃端点

#### 测试覆盖

- 拆分后各子服务的单元测试

---

### Phase 8: 跨模块收尾

#### 安全修复

- **CORS 策略收紧**: `AllowAnyOrigin` → 白名单策略
  - 文件: `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs`

#### TODO 清理

| 位置 | TODO 内容 | 处理方式 |
|------|----------|----------|
| EnhancedNavigationService.cs | Implement state restoration | 评估是否需要，关联 Issue 或删除 |
| EnhancedNavigationService.cs | Subscribe to region navigation events | 同上 |
| EnhancedNavigationService.cs | Publish navigation event | 同上 |
| NavigationAnalyticsService.cs | Integrate with authentication service | 同上 |
| NavigationShortcuts.cs | Implement show history panel | 同上 |
| NavigationShortcuts.cs | Implement cycle through regions | 同上 |
| MenuManager.cs | Publish event to open/focus NavigationHistoryPanel | 同上 |
| MenuManager.cs | Implement region cycling logic | 同上 |
| MedicalCaseCommandService.cs | 价格刷新 (3处) | 关联 US-MC-016 |

#### 文档清理

- `docs/plans/` 下 50+ 文件，归档 2026-03 及更早的已完成计划
- 更新 `docs/README.md` 索引

---

## 四、执行约束

1. **每阶段独立可测试** — 完成一个阶段后 `dotnet build` + `dotnet test` 必须全部通过
2. **不破坏现有功能** — 重构使用 Extract Class 模式，保持原接口不变
3. **API对齐优先** — 先确保功能可用，再做代码质量改进
4. **测试先行** — 对重构目标先补充测试，再拆分代码

---

## 五、风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| God Class 拆分破坏现有调用 | 保持原接口为 Facade，内部委托给子服务 |
| Local API 返回类型统一影响 Repository 层 | 在 Repository 层增加适配器，不改 Controller |
| 测试回归 | 每步完成后运行完整测试套件 |

<!-- MANUAL: -->
