# A-06 Repository 泛型化重构实施计划（方案 B 收敛版）

> **For agentic workers:** REQUIRED SUB-SKILL: compose:execute 逐任务实施。Steps use checkbox (`- [ ]`) syntax.

**Goal:** 将 Patient/Formula/Herb/User 4 个 Desktop Repository 的标准 CRUD 样板收敛到泛型基类，消除 4 处复制粘贴。

**Architecture:** 新建泛型段接口 `IEntityApiSegment<TList,TDetail,TInput>`（5 个标准 CRUD 方法），4 个形状一致的段接口继承它并用手写默认接口方法（DIM）转发到现有实体命名方法（实现类零改动）；新建 `EntityApiClientRepositoryBase<TList,TDetail,TInput>` 派生自现有 2 参基类，用段实现标准 CRUD。

**Tech Stack:** .NET 8 / C# 12（默认接口方法 DIM，C# 8+）/ Refit 段适配器

## Global Constraints

- 边界：Registration/MedicalCase 两 Repository **必须保持原样**（它们继承现有 2 参 `ApiClientRepositoryBase<TList,TDetail>`，因此该基类本身不可改动——改 3 参会使它们编译失败）
- 段接口现有方法名（`GetPatientsAsync` 等）**不得重命名**——用 DIM 默认实现转发，8 个实现类（4 Refit 适配器 + 4 HttpApiClient 本地适配器）零改动
- 不得改动：IApiClient 聚合、SwitchingApiClient/RefitApiClient/HttpClientApiClient、任何测试文件、Server 侧代码、实体特有方法
- 标准 CRUD 语义与现状完全一致：GetPaged Data==null→空分页；GetById 直接返回 Data（失败返回 null，不抛）；Create/Update/Delete 失败抛 InvalidOperationException(msg??默认文案)
- 构造签名保持 `(IApiClient, ILogger<X>)`（DI 注册零改动）；`_apiClient` 字段保留供实体特有方法使用
- 验收：`dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告；架构测试通过；完成后 git commit（不 push），中文 message

---

### Task 1: 泛型接口 + 约束接口（T1）

**Files:**
- Create: `src/Shared/LYBT.Shared.Models/Contracts/Common/IEntityInputDto.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/ApiClient/IEntityApiSegment.cs`
- Modify: 4 个输入 DTO（PatientInputDto/FormulaInputDto/HerbInputDto/UserInputDto）声明实现 `IEntityInputDto`

- [ ] 新建 `IEntityInputDto.cs`（`Guid? Id { get; }`，供泛型基类提取更新用 ID）
- [ ] 新建 `IEntityApiSegment.cs`：5 个标准 CRUD 方法（签名按任务规格，含 `category=null` 默认参）
- [ ] 4 个 DTO 加 `using LYBT.Shared.Models.Contracts.Common;` + `: IEntityInputDto`

### Task 2: 4 个段接口继承 + DIM 转发（T2）

**Files:** Modify: `IApiClientPatients.cs` / `IApiClientFormulas.cs` / `IApiClientHerbs.cs` / `IApiClientUsers.cs`

- [ ] 每个接口改为 `: IEntityApiSegment<XListDto, XDetailDto, XInputDto>`
- [ ] 追加 5 个显式接口实现（DIM 默认体）转发到现有实体命名方法：
  - Patients: `GetPagedAsync→GetPatientsAsync(page,pageSize,keyword)`；Users 同理
  - Formulas/Herbs: `GetPagedAsync→GetXAsync(page,pageSize,keyword,category)`
  - GetById/Create/Update/Delete 一一对应转发
- [ ] 验证实现类（PatientApiClient 等 8 个）无需改动即可编译

### Task 3: 泛型基类（T3）

**Files:** Create: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Repositories/EntityApiClientRepositoryBase.cs`

- [ ] `abstract class EntityApiClientRepositoryBase<TListDto, TDetailDto, TInputDto> : ApiClientRepositoryBase<TListDto, TDetailDto> where TInputDto : IEntityInputDto`
- [ ] 构造 `(ILogger, IEntityApiSegment<...>)`，存 `protected IEntityApiSegment<...> Api`
- [ ] 5 个 `virtual` 标准 CRUD（语义与现状一致，见 Global Constraints）

### Task 4: 4 个 Repository 重构（T4）

**Files:** Modify: `PatientRepository.cs` / `FormulaRepository.cs` / `HerbRepository.cs` / `UserRepository.cs`

- [ ] 改继承 `EntityApiClientRepositoryBase<XListDto, XDetailDto, XInputDto>`，构造 `: base(logger, apiClient.Patients/Formulas/Herbs/Users)`
- [ ] 删除 5 个标准 CRUD 方法；Patient/User 因接口无 category 参数，保留 1 行薄包装 `GetPagedAsync`（4 参 → base 5 参，category 传 null）
- [ ] 实体特有方法（Search/Clone/ToggleStatus/Restore/Batch*/Export*/GetByIdNumber/GetByUsername/GetDoctors/Change*/密码等）与 Mapperly 保持原样

### Task 5: 验证（T5）

- [ ] `dotnet build LYBTZYZS.sln --no-incremental` → 0 错误 0 警告
- [ ] `dotnet test tests/LYBT.Tests.Architecture/`（含 A-09 Repository 基类守卫：新基类继承链含 `ApiClientRepositoryBase<,>` 且为抽象类，天然满足）
- [ ] `dotnet test tests/LYBT.Tests.Desktop/`（若环境允许；否则说明原因）

### Task 6: 收尾（T6）

- [ ] 更新 `docs/03-architecture/13-project-master-plan.md`：A-06 ⬜→✅ + Commit SHA（§八 状态跟踪表）
- [ ] `git add` 具体文件 + `git commit`（中文 message，不 push）
