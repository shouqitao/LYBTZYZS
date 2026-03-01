# LYBT.Desktop.LocalData 代码知识

本地数据层 - SQLite EF Core 实现，提供离线模式的数据存取、同步和本地认证服务。

## 代码文件结构

```
LYBT.Desktop.LocalData/
├── Context/
│   └── LocalDbContext.cs           # SQLite 数据库上下文
├── DataSources/
│   ├── LocalUserDataSource.cs      # 本地用户数据源
│   ├── LocalPatientDataSource.cs   # 本地患者数据源
│   ├── LocalHerbDataSource.cs      # 本地药材数据源
│   ├── LocalFormulaDataSource.cs   # 本地验方数据源
│   └── LocalMedicalCaseDataSource.cs # 本地医案数据源 (聚合根)
├── Helpers/
│   └── ChecksumHelper.cs          # SHA256 校验和计算 (同步用)
├── Initialization/
│   ├── DatabaseInitializer.cs      # SQLite 数据库初始化
│   └── SeedData.cs                 # 默认管理员种子数据
├── Mappers/
│   ├── LocalFormulaMapper.cs       # 验方 Entity <-> DTO 映射
│   ├── LocalHerbMapper.cs          # 药材 Entity <-> DTO 映射
│   ├── LocalMedicalCaseMapper.cs   # 医案 Entity <-> DTO 映射
│   ├── LocalPatientMapper.cs       # 患者 Entity <-> DTO 映射
│   └── LocalUserMapper.cs          # 用户 Entity <-> DTO 映射
└── Services/
    ├── LocalAuthService.cs         # 本地认证 (BCrypt 密码验证)
    └── SyncService.cs              # 本地-服务器数据同步服务
```

### Context/LocalDbContext.cs
**LocalDbContext** : DbContext | SQLite 数据库上下文，10 个 DbSet，自动审计字段

| 方法 | 说明 |
|------|------|
| OnModelCreating(ModelBuilder) | 配置软删除过滤器、RowVersion 忽略、decimal 转换、实体关系 |
| ApplySoftDeleteFilter(ModelBuilder) | 遍历 ISoftDeletable 实体应用全局查询过滤器 |
| IgnoreRowVersion(ModelBuilder) | SQLite 不支持 RowVersion，忽略该字段 |
| ApplyDecimalConversion(ModelBuilder) | SQLite decimal->double 值转换器 |
| ConfigureRelationships(ModelBuilder) | MedicalCase 聚合根关系: 1:1 Consultation(共享主键), 1:0..1 Prescription, 1:N PrintLog |
| SaveChangesAsync(CancellationToken) | 重写保存，自动设置 CreatedAt/UpdatedAt/CreatedBy/UpdatedBy |
| SaveChanges() | 同步版自动审计 |

### DataSources/LocalUserDataSource.cs
**LocalUserDataSource** : IUserDataSource | 本地用户数据源，含角色保护逻辑

| 方法 | 说明 |
|------|------|
| GetByIdAsync(Guid, CancellationToken) | 按 ID 查询用户 |
| GetByUsernameAsync(string, CancellationToken) | 按用户名查询 |
| GetPagedAsync(int, int, string?, CancellationToken) | 分页查询，支持关键词搜索 |
| CreateAsync(UserInputDto, CancellationToken) | 创建用户，BCrypt 密码哈希 |
| UpdateAsync(UserInputDto, CancellationToken) | 更新用户，保留密码哈希不被覆盖 |
| DeleteAsync(Guid, CancellationToken) | 软删除，SuperAdmin/最后管理员保护 |
| ChangePasswordAsync(Guid, string, string, CancellationToken) | 修改密码，验证旧密码 |
| ToggleStatusAsync(Guid, CancellationToken) | 切换启用/禁用，SuperAdmin/最后管理员保护 |
| UpdateLastLoginTimeAsync(Guid, CancellationToken) | 更新最后登录时间 |
| ResetFailedLoginCountAsync(Guid, CancellationToken) | 重置失败登录计数 |
| IncrementFailedLoginCountAsync(Guid, CancellationToken) | 递增失败登录计数 |
| RestoreAsync(Guid, CancellationToken) | 恢复软删除用户 (IgnoreQueryFilters) |
| BatchDeleteAsync(List\<Guid\>, CancellationToken) | 批量软删除 |
| ResetPasswordAsync(Guid, CancellationToken) | 重置为默认密码 "Lybt@2026" |
| BatchToggleStatusAsync(List\<Guid\>, bool, CancellationToken) | 批量切换状态 |
| GetCurrentUserAsync(CancellationToken) | 获取当前登录用户 |

### DataSources/LocalPatientDataSource.cs
**LocalPatientDataSource** : IPatientDataSource | 本地患者数据源

| 方法 | 说明 |
|------|------|
| GetByIdAsync(Guid, CancellationToken) | 按 ID 查询患者 |
| GetPagedAsync(int, int, string?, CancellationToken) | 分页查询，支持姓名/电话/身份证/拼音码搜索 |
| CreateAsync(PatientInputDto, CancellationToken) | 创建患者 |
| UpdateAsync(PatientInputDto, CancellationToken) | 更新患者信息 |
| DeleteAsync(Guid, CancellationToken) | 软删除 |
| SearchAsync(string, CancellationToken) | 搜索患者 (限100条) |
| GetByIdNumberAsync(string, CancellationToken) | 按身份证号查询 |
| RestoreAsync(Guid, CancellationToken) | 恢复软删除患者 |
| BatchDeleteAsync(List\<Guid\>, CancellationToken) | 批量软删除 |
| BatchImportAsync(List\<PatientInputDto\>, CancellationToken) | 批量导入患者 |
| GetAllForExportAsync(string?, CancellationToken) | 导出全量患者数据 |
| HasMedicalCasesAsync(Guid, CancellationToken) | 检查患者是否有关联医案 |
| BatchCheckReferencesAsync(List\<Guid\>, CancellationToken) | 批量检查引用关系 |

### DataSources/LocalHerbDataSource.cs
**LocalHerbDataSource** : IHerbDataSource | 本地药材数据源

| 方法 | 说明 |
|------|------|
| GetByIdAsync(Guid, CancellationToken) | 按 ID 查询药材 |
| GetPagedAsync(int, int, string?, string?, CancellationToken) | 分页查询，支持关键词+分类过滤 |
| CreateAsync(HerbInputDto, CancellationToken) | 创建药材 |
| UpdateAsync(HerbInputDto, CancellationToken) | 更新药材信息 |
| DeleteAsync(Guid, CancellationToken) | 软删除 |
| ToggleStatusAsync(Guid, CancellationToken) | 切换启用/禁用 |
| RestoreAsync(Guid, CancellationToken) | 恢复软删除药材 |
| GetCategoriesAsync(CancellationToken) | 获取所有药材分类 |
| BatchDeleteAsync(List\<Guid\>, CancellationToken) | 批量软删除 |
| BatchToggleStatusAsync(List\<Guid\>, bool, CancellationToken) | 批量切换状态 |
| BatchImportAsync(List\<HerbInputDto\>, CancellationToken) | 批量导入药材 |
| GetAllForExportAsync(string?, CancellationToken) | 导出全量药材数据 |
| HasReferencesAsync(Guid, CancellationToken) | 检查验方/处方引用 |
| GetImportTemplateColumns() | 返回导入模板列定义 |

### DataSources/LocalFormulaDataSource.cs
**LocalFormulaDataSource** : IFormulaDataSource | 本地验方数据源

| 方法 | 说明 |
|------|------|
| GetByIdAsync(Guid, CancellationToken) | 按 ID 查询验方 |
| GetWithHerbsAsync(Guid, CancellationToken) | 查询验方含药材子项 (Include Herbs) |
| GetPagedAsync(int, int, string?, string?, CancellationToken) | 分页查询，支持关键词+分类过滤 |
| CreateAsync(FormulaInputDto, CancellationToken) | 创建验方及药材项 |
| UpdateAsync(FormulaInputDto, CancellationToken) | 更新验方 (删除旧药材项+添加新的) |
| DeleteAsync(Guid, CancellationToken) | 软删除 |
| CloneAsync(Guid, CancellationToken) | 克隆验方 (名称加"副本"后缀) |
| ToggleStatusAsync(Guid, CancellationToken) | 切换启用/禁用 |
| RestoreAsync(Guid, CancellationToken) | 恢复软删除验方 |
| BatchImportAsync(List\<FormulaImportItemDto\>, CancellationToken) | 批量导入 (延迟绑定模式) |
| GetPendingValidationAsync(CancellationToken) | 获取待验证验方 (Draft 状态) |
| GetAllForExportAsync(string?, CancellationToken) | 导出全量验方数据 |
| BatchToggleStatusAsync(List\<Guid\>, bool, CancellationToken) | 批量切换状态 |
| GetImportTemplateColumns() | 主表导入列定义 |
| GetImportTemplateHerbColumns() | 药材明细导入列定义 |
| BatchDeleteAsync(List\<Guid\>, CancellationToken) | 批量软删除 |
| ValidateHerbBindingsAsync(Guid, CancellationToken) | 验证药材绑定有效性 |

### DataSources/LocalMedicalCaseDataSource.cs
**LocalMedicalCaseDataSource** : IMedicalCaseDataSource | 本地医案数据源 (聚合根)

| 方法 | 说明 |
|------|------|
| GetByIdAsync(Guid, CancellationToken) | 按 ID 查询医案 |
| GetWithDetailsAsync(Guid, CancellationToken) | 查询医案含 Consultation + Prescription + Items |
| GetPagedAsync(int, int, string?, CancellationToken) | 分页查询 |
| QueryAsync(Guid?, Guid?, MedicalCaseStatus?, DateTime?, DateTime?, int, int, CancellationToken) | 多条件查询 |
| GetByPatientIdAsync(Guid, CancellationToken) | 按患者 ID 查询医案历史 |
| CreateAsync(MedicalCaseInputDto, CancellationToken) | 创建医案 + Consultation + Prescription (聚合创建) |
| UpdateAsync(MedicalCaseInputDto, CancellationToken) | 更新医案 + 子实体 |
| SaveAsync(MedicalCaseInputDto, CancellationToken) | 统一入口: 根据 ID 存在决定创建或更新 |
| DeleteAsync(Guid, CancellationToken) | 软删除 |
| CompleteAsync(Guid, CancellationToken) | 完成医案 (设置 Completed 状态) |
| CancelAsync(Guid, string?, CancellationToken) | 取消医案 (软删除+记录原因) |
| AddPrintLogAsync(Guid, bool, PrintType, string?, string?, CancellationToken) | 添加打印日志，成功时更新打印管理字段 |
| BatchDeleteAsync(List\<Guid\>, CancellationToken) | 批量软删除 |
| GenerateCaseNumber() | 生成医案编号 (MC + yyyyMMdd + 3位序号) |

### Helpers/ChecksumHelper.cs
**ChecksumHelper** (static) | SHA256 校验和计算，用于同步差异比对

| 方法 | 说明 |
|------|------|
| ComputeHerbChecksum(Herb) | 计算药材 Checksum (排除审计字段) |
| ComputePatientChecksum(Patient) | 计算患者 Checksum |
| ComputeFormulaChecksum(Formula) | 计算验方 Checksum (含排序后的药材子项) |
| ComputeChecksum(object, string) | 按实体类型分发计算 |
| ComputeHash(object) | 序列化为 JSON 后计算 SHA256 |

### Initialization/DatabaseInitializer.cs
**DatabaseInitializer** | SQLite 数据库初始化器

| 方法 | 说明 |
|------|------|
| InitializeAsync(CancellationToken) | 创建目录、EnsureCreated、种子数据 |
| DatabaseExists() | 检查数据库文件是否存在 (static) |
| GetConnectionString() | 获取连接字符串 (static) |
| DatabasePath | AppData/LYBTZYZS/lybtzyzs.db (static) |

### Initialization/SeedData.cs
**SeedData** (static) | 种子数据，创建默认管理员

| 方法 | 说明 |
|------|------|
| SeedAsync(LocalDbContext, ILogger, CancellationToken) | 初始化种子数据 |
| SeedAdminUserAsync(LocalDbContext, ILogger, CancellationToken) | 创建 admin/Admin@123 SuperAdmin 账户 |

### Mappers/LocalFormulaMapper.cs
**LocalFormulaMapper** (internal, partial) : Riok.Mapperly [Mapper] | 验方 Entity <-> DTO

| 方法 | 说明 |
|------|------|
| ToDetailDto(Formula) | Formula -> FormulaDetailDto (含计算属性 HerbCount) |
| ToEntity(FormulaInputDto) | FormulaInputDto -> Formula |
| ToDto(FormulaHerbItem) | FormulaHerbItem -> FormulaHerbItemDto |
| ToEntity(FormulaHerbItemInputDto) | FormulaHerbItemInputDto -> FormulaHerbItem |

### Mappers/LocalHerbMapper.cs
**LocalHerbMapper** (internal, partial) : Riok.Mapperly [Mapper] | 药材 Entity <-> DTO

| 方法 | 说明 |
|------|------|
| ToDetailDto(Herb) | Herb -> HerbDetailDto |
| ToEntity(HerbInputDto) | HerbInputDto -> Herb |

### Mappers/LocalPatientMapper.cs
**LocalPatientMapper** (internal, partial) : Riok.Mapperly [Mapper] | 患者 Entity <-> DTO

| 方法 | 说明 |
|------|------|
| ToDetailDto(Patient) | Patient -> PatientDetailDto |
| ToEntity(PatientInputDto) | PatientInputDto -> Patient |

### Mappers/LocalUserMapper.cs
**LocalUserMapper** (internal, partial) : Riok.Mapperly [Mapper] | 用户 Entity <-> DTO

| 方法 | 说明 |
|------|------|
| ToDetailDto(User) | User -> UserDetailDto (排除 PasswordHash) |
| ToEntity(UserInputDto) | UserInputDto -> User (排除 Password/ConfirmPassword) |

### Mappers/LocalMedicalCaseMapper.cs
**LocalMedicalCaseMapper** (internal, partial) : Riok.Mapperly [Mapper] | 医案聚合根映射

| 方法 | 说明 |
|------|------|
| ToDetailDto(MedicalCase) | MedicalCase -> MedicalCaseDetailDto (含 Consultation/Prescription 嵌套) |
| ToConsultationDetailDto(Consultation, MedicalCase) | Consultation -> ConsultationDetailDto (需要父实体补充 PatientId 等) |
| ToPrescriptionDetailDto(Prescription) | Prescription -> PrescriptionDetailDto |
| ToPrescriptionItemDto(PrescriptionItem) | PrescriptionItem -> PrescriptionItemDto |
| ToEntity(MedicalCaseInputDto) | MedicalCaseInputDto -> MedicalCase |

### Services/LocalAuthService.cs
**LocalAuthService** : ILocalAuthService | 本地认证服务

| 方法 | 说明 |
|------|------|
| ValidateAsync(string, string, CancellationToken) | 验证用户名密码，含账户锁定机制 (5次失败锁定15分钟) |
| ChangePasswordAsync(Guid, string, string, CancellationToken) | 修改密码，验证旧密码 |

### Services/SyncService.cs
**SyncService** : ISyncService | 数据同步服务，协调本地与服务器之间的同步

| 方法 | 说明 |
|------|------|
| GetSupportedEntityTypesAsync(CancellationToken) | 获取支持的实体类型 (Herb/Patient/Formula) |
| CheckDifferencesAsync(string, CancellationToken) | Checksum 比对，生成 LocalOnly/ServerOnly/Conflicts 三类差异 |
| UploadAsync(string, List\<Guid\>, CancellationToken) | 上传本地实体到服务器 |
| DownloadAsync(string, List\<Guid\>, CancellationToken) | 从服务器下载实体到本地 |
| DeleteAsync(string, List\<Guid\>, CancellationToken) | 请求服务器删除实体 |
| ExecuteSyncAsync(string, SyncResolution, CancellationToken) | 执行完整同步流程 (上传+下载+冲突解决) |

## 死代码与废弃标记

- Mappers (5个) 均为 `internal` 访问级别，仅在 LocalData 项目内部被对应的 DataSource 和 Service 使用，无外部引用 -- 属于正常设计 (internal 封装)
- `SeedData` 仅被 `DatabaseInitializer` 调用，测试项目通过 `DatabaseInitializer` 间接使用 -- 非死代码
- 所有 DataSource 类在 `DataSourceRegistrationExtensions.cs` 和 `ServiceCollectionExtensions.cs` 中注册 -- 非死代码

## 设计分析

1. **DataSource 模式**: 每个领域实体对应一个 DataSource 类，实现 Contracts 层定义的接口 (IUserDataSource 等)，与远程 API DataSource 形成对称结构，由 Shell 层根据运行模式选择注册
2. **Mapperly 编译期映射**: 使用 Riok.Mapperly 源生成器替代运行时反射映射，5 个 Mapper 均为 `internal partial class`，编译器自动生成映射实现
3. **聚合根模式**: LocalMedicalCaseDataSource 管理 MedicalCase + Consultation + Prescription 三实体的完整生命周期，CreateAsync 方法一次性创建所有关联实体
4. **Checksum 同步**: ChecksumHelper 计算实体业务字段的 SHA256 哈希 (排除审计字段)，必须与服务器端 `LYBT.Module.Sync.Services.ChecksumHelper` 保持完全一致
5. **角色保护逻辑**: LocalUserDataSource 实现 SuperAdmin 不可删除/不可禁用、最后管理员保护等业务规则
6. **SQLite 适配**: LocalDbContext 处理三个 SQLite 限制: 忽略 RowVersion (不支持)、decimal->double 转换、软删除全局过滤

## 已知陷阱

- ChecksumHelper 的 JSON 序列化选项 (camelCase, WhenWritingNull) 必须与服务器端完全一致，否则相同数据会产生不同的 Checksum 导致同步误判
- SeedData 中硬编码了默认管理员密码 "Admin@123"，仅用于首次初始化，不应在生产环境保留
- LocalUserDataSource 的 DefaultResetPassword "Lybt@2026" 是 OpenSpec SYNC-D02 过渡态设计
- LocalMedicalCaseDataSource.GenerateCaseNumber() 使用 IgnoreQueryFilters 计数，包含软删除记录，确保编号不重复
- LocalDbContext 的 decimal->double 转换存在精度损失风险，适用于药材价格等对精度要求不极端的场景
- FormulaDataSource.UpdateAsync 采用 "删除旧项+添加新项" 策略更新药材列表，必须先 ToList() 避免迭代时修改集合
- MedicalCaseDataSource.CreateAsync 会查询 MedicalCases 表检查业务规则 (患者同时只能有一个活跃医案)

---
最后更新: 2026-03-01
