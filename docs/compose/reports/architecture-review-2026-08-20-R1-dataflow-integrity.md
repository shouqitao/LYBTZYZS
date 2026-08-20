# 第1轮审查：数据流完整性验证

> **审查日期**: 2026-08-20  
> **审查人**: Hermes Agent (架构师视角, intended vs implemented)  
> **审查范围**: `docs/03-architecture/{00-summary,03-server,04-data-model,05-dual-mode,09-security}/11-business-flows` + `LYBT.Entities` + `Shared.Models` + `Mappers/Repositories/Controllers` 双栈  
> **基线规模**: `LYBTZYZS` `28,122节点 / 67,287边 / 1,832文件&模块 / 1,314类 / 169接口` (`codebase-memory` `get_architecture all`)

---

## 一、执行摘要

| 维度 | 结论 |
|------|------|
| **整体数据流** | `Controller → Service/CQRS Handler → Repository → AppDbContext` 三层完整，**但 MedicalCase 聚合保存绕过验证管线**，且 `Controller` 双栈（Remote/Local）端点不一致 |
| **Build 门禁** | ❌ **0/0 失败**：`dotnet build --no-incremental` **6 warnings CS0105**（`LYBT.Tests.Desktop` 重复 using）→ 违反 `AGENTS.md §0错误0警告` 硬门禁 |
| **严重度分布** | 🔴 CRITICAL 3 · 🟠 HIGH 5 · 🟡 MEDIUM 4 · 🟢 LOW 2  · ✅ PASS 6 |
| **最大风险** | `Patient` 实体被静默裁剪至 7 字段（文档定义 20+ 字段），敏感数据/审计字段在流中丢失；`MedicalCase.CreateFromInputDtoAsync` 直接映射 `Consultation` 不走 `ValidationBehavior` |

> **原则**：文档是设计态 SSOT，代码是当前态。以下每项均标注 `文档意图 → 代码证据 → 边界影响 → 修复`。

---

## 二、CRITICAL（阻断发布）

### C1 — Patient 实体的静默裁剪：文档↔代码失配导致数据流截断
- **文档意图** (`04-data-model.md:Patient`)：`Name/PinYinCode/Gender/MaritalStatus/BirthDate/IdType/IdNumber/PhoneNumber/Address/AllergyHistory/MedicalHistory/BloodType/EmergencyContact*/Status/DisableReason/LastVisitTime/VisitCount` 20+ 字段，其中 `Address/AllergyHistory/MedicalHistory` 等 6 字段标为**敏感数据**。
- **代码证据**：`src/Shared/LYBT.Entities/Patients/PatientModel.cs:14-61` 仅保留 `Name/PinYinCode/Gender/BirthDate/IdNumber/PhoneNumber/Status` 7 字段；`PatientInputDto`/`PatientDetailDto` 同步裁剪。`docs` 未更新，SSOT 背离。
- **边界影响**：前端 `PatientEditor` 若传入 `Address/Allergy` 将被 `Mapperly` 静默丢弃（`PatientMapper.ToEntity` 仅 7 参），历史数据迁移时字段丢失不可逆。
- **数据流**：`Desktop ViewModel → PatientInputDto(7字段) → PatientMapper.ToEntity → Patient(7字段) → AppDbContext → SQL` 全链路缺 13 字段。
- **修复**：P0 要么补回实体+迁移+DTO（若为需求裁剪则先改 `02-requirements/04-patients.md` 再改代码），要么更新 `04-data-model.md` 明确「v1.0 患者仅 7 字段」并归档裁剪 ADR。

### C2 — MedicalCase 聚合创建绕过 `ValidationBehavior` 管线
- **文档意图** (`03-server.md:FluentValidation集成`)：所有写操作经 `ValidationBehavior<TRequest,TResponse>` 自动校验，失败抛 `ValidationException → 400`。
- **代码证据**：`src/Server/Modules/LYBT.Module.MedicalCases/Services/MedicalCaseCommandService.cs:123-134` 注释自证：
  > `UpdateConsultationAsync 通过 FluentValidation Pipeline 验证，但 CreateFromInputDtoAsync 的 Consultation 数据是直接映射，不经过验证器`
  仅手写 `TcmDiagnosis` 非空检查，其余 `PresentIllness/TongueDiagnosis/PulseDiagnosis` 长度/必填规则（`ConsultationInputDtoValidator`）未触发。
- **数据流**：`MedicalCaseInputDto.Consultation → CreateFromInputDtoAsync 直接赋值 → Consultation(未校验) → MedicalCaseRepository.AddAsync → DB` — 可写入超长 `2000` 字符外数据或空诊断绕过 `BR-003`。
- **修复**：`CreateFromInputDtoAsync` 注入 `IValidator<ConsultationInputDto>` 手动 `ValidateAndThrowAsync`，或改为走 `MediatR` Command 复用管线。

### C3 — 双栈控制器端点覆盖率断层：数据流分叉
- **文档意图** (`05-dual-mode.md:端点覆盖率`)：`Remote ~104 vs Local ~99`，差异仅 `MedicalCases 62% / Reports 38%` 已知，其余模块 100%。
- **代码证据**：
  - `MedicalCases: Remote 13 endpoints vs Local 11`：Local 缺 `POST /batch-details, POST /(Create独立), PUT /{id}/prescription-flag, PUT /{id}/print-completed, GET /search, GET /{id}/permissions, GET /{id}/audit-logs`
  - `Reports: Remote 8 vs Local 3`：Local 缺 `trend/income, trend/consultations, doctor-performance, herbs/ranking, patient-flow` 5 趋势端点
  - `diff MedicalCasesController.cs` 显示 Local 继承 `BaseMedicalCasesController` 但重写并**丢弃**关键操作（含 `DoctorOnly` 打印权限）
- **数据流影响**：`Desktop MedicalCaseRepository → IApiClient → SwitchingApiClient(localhost?) → LocalWebAPI` 在本地模式下调用 `print-completed` 得到 `404`（文档已修正为 404，但前端未降级），`ReportsHomeView` 仅消费 3/8 导致趋势页空白。
- **修复**：P0 对齐 `Reports` 趋势端点（或显式在 `05-dual-mode.md §本地模式功能限制` 标注 5 端点 `501 NotImplemented` 并让 `ReportRepository` 降级）；`MedicalCases` Local 补回 `prescription-flag/print-completed` 或让 `BaseMedicalCasesController` 的 `virtual` 方法在 Local 不被 `override` 覆盖而直接复用基类实现。

---

## 三、HIGH（数据完整性受损）

### H1 — SensitiveData 脱敏链在 LocalWebAPI 断裂
- **意图** (`04-data-model.md:敏感数据` + `09-security.md:7.8`)：`Patient.IdNumber/PhoneNumber` 标记 `[SensitiveData]`，`SensitiveDataJsonConverterFactory` 在序列化时 `Partial` 脱敏，日志经 `SensitiveDataMasker.SerializeWithSanitization`。
- **证据**：`src/Server/Services/LYBT.WebAPI/Extensions/ServiceCollectionExtensions.cs:162` 注册 `SensitiveDataJsonConverterFactory`；`src/Client/Desktop/LocalWebAPI` 全仓仅 `PatientsController.cs:231` 一处注释提及，**无 `AddJsonOptions.Converters.Add(...)` 注册**（grep 零命中）。`grep -rn SensitiveData LocalWebAPI` 唯一命中为注释。
- **影响**：本地模式 `GET /api/v1/patients` 返回明文 `IdNumber/PhoneNumber`，`Desktop → LocalWebAPI` 环回虽低风险，但日志与内存 dump 仍泄露 PII，违反 `STD-04`。
- **修复**：`LocalWebApiProgram.cs` 复用 `ServiceCollectionExtensions.AddSensitiveDataMasking()`，并为 `BaseApiController.LogOperation` 的 `SerializeWithSanitization` 在 Local 同步生效。

### H2 — Prescription Discount 精度不一致：DTO ↔ Entity ↔ DB 三向漂移
- **意图** (`04-data-model.md:Prescription`): `Discount decimal(5,4)`，`1.0=无折扣`，公式 `TotalPrice = SingleDosePrice * DosageCount * Discount`。
- **证据**：`PrescriptionModel.cs:Discount HasPrecision(3,2)` (`PrescriptionConfiguration.cs:22`) vs 文档 `5,4`；`PrescriptionInputDto.Usage/Remark` 为 `string(500)` 但 `Herb.Price` 为 `18,2`，`Discount` 在 `MedicalCasePrescriptionService` 计算时 `18,2 * 3,2` 导致精度截断。
- **修复**：统一为 `HasPrecision(5,4)` 并加迁移。

### H3 — MedicalCaseDetailModel → MedicalCaseInputDto 映射静默忽略 6 关键字段
- **证据**：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseDetailModelMapper.cs:153-159` 显式 `MapperIgnoreTarget`：`Id/UserId/RegistrationId/EditReason/Consultation/Prescription/NeedsPrescription`。
- **数据流**：`MedicalCaseDetailDto → MedicalCaseDetailModel → ToInputDtoCore(丢6字段) → MedicalCaseInputDto(缺NeedsPrescription) → SaveAsync → Prescription 误删`（`NeedsPrescription=false` 触发软删）
- **修复**：`ToInputDto` 改为必填参数 `ToInputDto(model, consultation, prescription, needsPrescription, editReason)`，编译期强制。

### H4 — AppDbContext.SetAuditFields 的 HttpContext 依赖在非 HTTP 路径丢失审计
- **证据**：`AppDbContext.cs:133-172` 仅当 `IHttpContextAccessor.HttpContext` 有 `NameIdentifier` 才写 `CreatedBy/UpdatedBy`，否则 `null`。`MigrateAsync + Seed`、后台 `LogCleanupService`、`RegistrationLink` 跨模块调用均走 `null` 分支，导致 `MedicalCase.CreatedBy` 在 `StartVisit` 路径曾出现 `DB NOT NULL 500`（已用 `startvisit-createdby-fix` 补丁硬编码绕过）。
- **修复**：Service 层统一显式传 `operatorId` 并赋值，DbContext 仅作兜底；或引入 `ICurrentUserAccessor` 抽象隔离 Http。

### H5 — RegistrationFee 快照在 MedicalCase 创建链未原子化
- **意图** (`04-data-model.md:Registration.RegistrationFee`): 创建时从 `User.RegistrationFee` 快照，后续改价不影响已建挂号。
- **证据**：`MedicalCaseCommandService.CreateFromInputDtoAsync:152-158` 的 `LinkRegistrationToMedicalCaseAsync` 在 `AddAsync(medicalCase)` **之后**才执行，非同一 `SaveChanges` 事务（L2 聚合根隐式事务被拆为两次 `SaveChanges`），若第二步失败则 `MedicalCase` 已落库但 `Registration.MedicalCaseId` 仍 `null`。
- **修复**：纳入同一 `IDbContextTransaction`（L3 显式事务）或预先 `Attach` Registration。

---

## 四、MEDIUM（设计漂移）

### M1 — 乐观锁有效但错误映射缺失：RowVersion 未转为 409
- **证据**：`BaseEntityConfiguration: IsRowVersion()` 正确；但全仓 `grep DbUpdateConcurrencyException` 零命中，仅 `docs` 文字描述 `→ ConflictException → 409`，代码无 `IExceptionHandler` 将 `DbUpdateConcurrencyException` 映射为 `ErrorCode.ConcurrencyConflict`，实际冒泡为 `500`。
- **修复**：`UnifiedExceptionHandler` 加 `case DbUpdateConcurrencyException => Results.Conflict(ProblemDetails{errorCode=40901})`。

### M2 — 分页强制仅在 Controller 层，未下沉到 Repository
- **证据**：`BaseMedicalCasesController:ValidatePagination(page,pageSize)` 正确，但 `MedicalCaseQueryService.GetListDtoAsync` 未对 `pageSize>100` 限流，恶意 `?pageSize=10000` 可触发全表扫描。
- **修复**：`ValidationBehavior` 或 `BaseRepository.PagedQuery` 加 `Math.Min(pageSize, 100)` 硬上限。

### M3 — 命名冲突的 using 别名未统一
- **证据**：`CatalogModule` 需 `using FormulaEntity = LYBT.Entities.Formulas.Formula` 区分 `Contracts.Formula`，但 `CatalogDtoMapper` 仍有同名风险。
- **修复**：统一 `global using` 别名。

### M4 — Desktop 缓存与 Server 缓存不一致
- **证据**：`Server: OutputCache + IMemoryCache`，`Desktop: ApiService GET 缓存 + IMedicalCaseService.Cached*` 三字段内存缓存，但 `MedicalCaseCommandService:InvalidateAsync("medicalcases")` 仅清 Server 缓存，Desktop 的 `CachedConsultation/CachedPrescription` 未经 `SignalR` 或轮询失效。
- **修复**：`Desktop MedicalCaseService.ClearCache()` 在 `AggregateSave` 后强制调用。

---

## 五、LOW

- **L1 — Build 6× CS0105**：`tests/LYBT.Tests.Desktop/Unit/**/ViewModelTests.cs:15` 重复 `using`。修复：删除重复 using。
- **L2 — Prescription 计算属性未在 DTO 暴露**：`PrescriptionItem.Amount = UnitPrice × Dosage` 仅在 Entity 为计算属性，DTO 需前端重算，易因 `Discount` 精度漂移导致总额不一致。建议 DTO 加 `TotalPrice` 只读字段由 Server 计算后返回。

### ✅ PASS
1. `BaseRepository 5方法 + AppDbContext全局 IsDeleted过滤器 + IsRowVersion` 正确
2. `BaseApiController.Success/BusinessFail/HandleResult → ApiResponse<T> + RFC7807` 统一响应
3. `ValidationBehavior` 在 `Patients/Catalog/Identity/Registration` 4 模块 MediatR 管线已注册
4. `MedicalCase` 充血模型 `Complete/Suspend/SoftDelete` 域方法封装正确
5. `跨模块仅经 ICrossModuleService` 5 接口，grep 零违规
6. `Patient.PinYinCode` 搜索与 `EnsurePinYinCode` 回退链完整

---

## 六、修复路线（按数据流优先级）

| 优先级 | 项 | 文件 | 工作量 |
|--------|----|------|--------|
| **P0** | C1 患者实体裁剪决策 | `04-data-model.md` + `PatientModel.cs` + 迁移 | 1d |
| **P0** | C2 Validation 管线补齐 | `MedicalCaseCommandService.cs:69-160` | 0.5d |
| **P0** | C3 双栈对齐（Reports 5 趋势 + MedicalCases 6 端点） | `LocalWebAPI/Controllers/*` + `05-dual-mode.md` | 1d |
| **P0** | Build 0/0 | `tests/LYBT.Tests.Desktop/Unit/**/*.cs` | 0.2d |
| **P1** | H1 Local 脱敏 | `LocalWebApiProgram.cs` | 0.3d |
| **P1** | H4/H5 审计与事务原子化 | `AppDbContext.cs` + `MedicalCaseCommandService.cs` | 0.5d |
| **P2** | M1 409 映射 + M2 分页上限 | `UnifiedExceptionHandler.cs` | 0.3d |

> **验证**：每项修复后必跑 `dotnet build --no-incremental`（0/0）+ `dotnet test tests/LYBT.Tests.Architecture`（87/87 守卫：含 `P02_Repositories_Inherit_BaseRepository / P10_Services_NotInject_AppDbContext / P07_ModuleIsolation`）+ `SwitchingApiClient` 手动双模式冒烟（Remote `5000` ↔ Local `127.0.0.1:5300`）。

**结论**：数据流骨架完整，但 **C1/C2/C3 三处截断**使 `Patient → MedicalCase 聚合 → Prescription` 主链在「实体完整性」「验证完整性」「双模式一致性」三轴上均未闭环，建议按 P0 顺序修复后再进入第2轮（权限/安全）审查。
