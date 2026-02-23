# Progress: Phase 2 - Desktop Architecture Optimization

## Session: 2026-02-23

### Setup
- Created branch: feature/phase2-architecture-optimization (from feature/phase1-dead-code-cleanup)
- Phase 1 uncommitted changes carried over (stash + pop)

### Phase 2.1: Contracts 接口改双泛型 [complete]

| Action | Files Modified |
|--------|---------------|
| IDataSourceBase `<TEntity>` -> `<TDetail, TInput>` | IDataSourceBase.cs |
| 5 接口改 DTO 泛型 | IHerbDataSource.cs, IPatientDataSource.cs, IFormulaDataSource.cs, IMedicalCaseDataSource.cs, IUserDataSource.cs |
| ILocalAuthService `User?` -> `UserDetailDto?` | ILocalAuthService.cs |
| 移除 Entities 项目引用 | LYBT.Desktop.Contracts.csproj |

Build Contracts: 0 errors, 0 warnings

### Phase 2.2: Remote DataSource 简化 [complete]

| Action | Files Modified |
|--------|---------------|
| 删除 Entity mapper + using Entities | 5 Remote DataSource files |
| 新增 ListToDetail Mapperly 内部 mapper | 每个文件底部附加 internal mapper class |
| Get 直接返回 response.Data | GetById, Restore, Clone, GetWithDetails |
| Create/Update 接收 InputDto | CreateAsync, UpdateAsync (+ SaveAsync for MC) |
| Paged 用 _listMapper.ToDetailDto | GetPaged, Search, Query, GetByPatientId |

文件列表:
- RemoteHerbDataSource.cs
- RemotePatientDataSource.cs
- RemoteFormulaDataSource.cs
- RemoteMedicalCaseDataSource.cs
- RemoteUserDataSource.cs

### Phase 2.3: Local DataSource Mapper + 实现 [complete]

**Task 3: 新建 5 个 LocalData Mapper**

| 文件 | 位置 |
|------|------|
| LocalHerbMapper.cs | LocalData/Mappers/ |
| LocalPatientMapper.cs | LocalData/Mappers/ |
| LocalFormulaMapper.cs | LocalData/Mappers/ |
| LocalMedicalCaseMapper.cs | LocalData/Mappers/ |
| LocalUserMapper.cs | LocalData/Mappers/ |

**Task 4: 5 个 Local DataSource 更新**

| 文件 | 核心变更 |
|------|---------|
| LocalHerbDataSource.cs | Get->ToDetailDto, Create/Update(InputDto) |
| LocalPatientDataSource.cs | 同上, Search 返回 List<PatientDetailDto> |
| LocalFormulaDataSource.cs | 同上, Clone 内部仍用 Entity, 返回 DTO |
| LocalMedicalCaseDataSource.cs | 最复杂: SaveAsync(InputDto), 从 InputDto 构建聚合 |
| LocalUserDataSource.cs | 同上, Create 保留 BCrypt 密码哈希 |
| LocalAuthService.cs | ValidateAsync 返回 UserDetailDto? |

修复:
- LocalFormulaMapper.TotalPrice: FormulaHerbItem 无 UnitPrice, 改为由 Service 计算
- LocalAuthService: 添加 LocalUserMapper, 返回 `_mapper.ToDetailDto(user)`

Build LocalData: 0 errors, 0 warnings

### Phase 2.4: Repository 简化 [complete]

**Task 5: 5 个 Repository 删除 DataSourceMapper 调用**

| 文件 | 核心变更 |
|------|---------|
| HerbRepository.cs | 移除 _mapper, DataSource 直接返回 DTO |
| PatientRepository.cs | 同上 |
| FormulaRepository.cs | 同上 + 修复 e.Indication -> e.Indications |
| MedicalCaseRepository.cs | 同上, SaveAsync 直接传 InputDto |
| UserRepository.cs | 同上 |

每个 Repository 变更模式:
- 移除 `XxxDataSourceMapper _mapper = new()` 和 `using Mappers;`
- Read 操作: DataSource 已返回 DetailDto, 移除 `_mapper.ToDetailDto()` 转换
- Write 操作: 直接传 InputDto 给 DataSource, 移除 `_mapper.ToEntity()` 转换

Build Modules: 0 errors (27 -> 0)

**Task 5b: 测试文件同步更新 (Entity -> DTO)**

| 测试文件 | 核心变更 |
|----------|---------|
| PatientRepositoryTests.cs | Mock 返回 DetailDto, CreateAsync 参数改 InputDto |
| UserRepositoryTests.cs | 同上 |
| LocalHerbDataSourceTests.cs | CreateAsync 参数改 InputDto, 移除 IsDeleted 断言 |
| LocalPatientDataSourceTests.cs | 同上 |
| LocalFormulaDataSourceTests.cs | 同上, Indication -> Indications |
| DataSourceIntegrationTests.cs (x2) | Entity 构造改 InputDto 构造 |
| BusinessFlowTests.cs | Entity -> InputDto |
| BusinessFlowE2ETests.cs | Entity -> InputDto + ConsultationInputDto |
| MedicalCaseDataSourceTests.cs | Entity 聚合 -> InputDto 聚合 |
| MedicalCaseAggregateE2ETests.cs | 同上 |
| PrescriptionE2ETests.cs | 同上 |

Build: 0 errors, 0 warnings
Tests: 561 (Server) + 58 (Arch) + 635 (Desktop) = 1254 全部通过

### Phase 2.5: 清理旧 Mapper + 依赖 [complete]

| Action | 变更 |
|--------|------|
| 删除旧 DataSourceMapper 源文件 | 5 files deleted |
| 删除对应测试文件 | 5 test files deleted |
| 移除 DI 注册 | DataSourceRegistrationExtensions.cs |
| 移除 Infrastructure.csproj Entities 依赖 | LYBT.Desktop.Infrastructure.csproj |

### Phase 2.6-2.8: 已完成/已解决

- Phase 2.6 ICrossModuleService: 已在之前提交中完成
- Phase 2.7 A-3 Models->Foundation: 无此依赖 (已解决)
- Phase 2.7 A-4 Patients->MedicalCase: 无此依赖 (已解决)
- Phase 2.8 代码位置调整: 低优先级，可独立处理

### Phase 2.9: 全量验证 [complete]

修复:
- LocalMedicalCaseMapper: 补充 Consultation/Prescription 嵌套 DTO 映射
- LocalMedicalCaseDataSource.CreateAsync: 补充 PatientName/DoctorName 数据库查找
- 3 个集成测试: 遵守业务规则 (同患者需先 Complete 才能创建新案例)

**最终结果**:

| 测试项目 | 结果 |
|----------|------|
| Build | 0 errors, 0 warnings |
| LYBT.Tests.Unit (Server) | 561 passed |
| LYBT.Tests.Architecture | 58 passed |
| LYBT.Tests.Desktop.Unit | 633 passed |
| LYBT.Tests.Desktop.Integration | 24 passed |
| **总计** | **1276 passed, 0 failed** |

### 修改文件汇总 (最终)

| 层级 | 新建 | 修改 | 删除 |
|------|------|------|------|
| Contracts | 0 | 7 (6接口 + csproj) | 0 |
| Infrastructure/Remote | 0 | 5 | 0 |
| Infrastructure/Mappers | 0 | 0 | 5 (旧 DataSourceMapper) |
| Infrastructure/csproj | 0 | 1 | 0 |
| LocalData/Mappers | 5 | 1 (MC mapper fix) | 0 |
| LocalData/DataSources | 0 | 5+1 (MC PatientName) | 0 |
| LocalData/Services | 0 | 1 (LocalAuthService) | 0 |
| Modules/Repositories | 0 | 5 | 0 |
| Shell/Extensions | 0 | 1 (DI cleanup) | 0 |
| Tests | 0 | 14 | 5 (旧 Mapper tests) |
| **总计** | **5** | **41** | **10** |
