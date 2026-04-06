# LYBT.Module.Patients

> 患者信息管理 | 传统三层 | 独立模块

## 项目定位

- **层级**: Server端
- **架构模式**: 传统三层
- **跨模块通信**: 无(被MedicalCase模块通过IPatientCrossModuleService查询)

## 目录结构

```
LYBT.Module.Patients/
├── PatientsModule.cs
├── Interfaces/
│   ├── IPatientService.cs
│   ├── IPatientServiceOptimized.cs
│   └── IPatientRepository.cs
├── Services/
│   └── PatientService.cs
├── Repositories/
│   └── PatientRepository.cs
└── Mapping/
    └── PatientMappingProfile.cs
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IPatientService | 9 | CRUD、搜索、批量导入/导出 |
| IPatientServiceOptimized | - | Entity直接返回策略(性能优化) |
| IPatientRepository | 4+ | 继承BaseRepository，按名字/电话/日期范围查询 |

## 设计依据

- 传统三层架构，患者管理属于标准 CRUD 场景，架构简单直接
- IPatientServiceOptimized 提供 Entity 直接返回模式，跨模块查询时避免不必要的 DTO 映射开销
- 通过 IPatientCrossModuleService 向 MedicalCase 模块暴露只读查询，保持模块边界清晰
- Mapperly 编译时映射替代 AutoMapper，消除运行时反射开销
- 继承 BaseService 复用统一错误处理和 FluentValidation 验证逻辑

## 依赖关系

### 依赖
- LYBT.Infrastructure (BaseRepository, AppDbContext)
- LYBT.Entities (Patient实体)
- LYBT.Shared.Models (PatientDto, CreatePatientRequest等)

### 被依赖
- LYBT.WebAPI (PatientController)
- LYBT.Module.MedicalCase (通过IPatientCrossModuleService查询PatientBasicDto)

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/patients | GET | 分页查询患者列表 |
| /api/patients/{id} | GET | 按ID获取患者详情 |
| /api/patients | POST | 创建新患者 |
| /api/patients/{id} | PUT | 更新患者信息 |
| /api/patients/{id} | DELETE | 软删除患者 |
| /api/patients/search | GET | 按关键字搜索 |
| /api/patients/import | POST | 批量导入患者(Excel) |
| /api/patients/export | GET | 导出患者数据(Excel) |
| /api/patients/template | GET | 下载导入模板 |
| /api/patients/statistics | GET | 获取患者统计信息 |

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-11-19 | 修复DI注册缺失导致的500错误(IPatientServiceOptimized) |
| 2025-10-29 | 初始版本 |

## 开发笔记

# LYBT.Module.Patients 代码知识

服务端患者管理模块，提供患者CRUD、Excel批量导入/导出、引用检查、状态切换、Entity直接返回等功能。

## 代码文件结构

```
LYBT.Module.Patients/
├── PatientsModule.cs                      # 模块DI注册入口
├── Interfaces/
│   ├── IPatientService.cs                 # 患者服务接口
│   └── IPatientRepository.cs             # 患者仓储接口
├── Mapping/
│   └── PatientMapper.cs                   # Mapperly编译时映射器
├── Repositories/
│   └── PatientRepository.cs               # 患者仓储实现 (internal)
└── Services/
    └── PatientService.cs                  # 患者服务实现（987行）
```

### PatientsModule.cs
**PatientsModule** (static) | 模块DI注册

| 方法 | 说明 |
|------|------|
| AddPatientsModule(IServiceCollection, IConfiguration) | 注册IPatientRepository(Scoped)、IPatientService(Scoped)、FluentValidation验证器 |
| UsePatientsModule(IApplicationBuilder) | 中间件配置(当前为空占位) |

注: 注释中有被注释掉的IMedicalRecordRepository和IMedicalRecordService注册，属于历史遗留。

### Interfaces/IPatientService.cs
**IPatientService** | 患者服务统一接口，包含DTO和Entity两种返回模式

DTO返回方法:
| 方法 | 说明 |
|------|------|
| GetPagedAsync(int page, int pageSize, string? keyword, bool filterDisabled) | 分页查询患者，T5-P2-27支持过滤禁用患者 |
| GetPagedListAsync(int page, int pageSize, string? keyword, bool filterDisabled) | 分页查询患者列表(返回PatientListDto) |
| GetByIdAsync(Guid id) | 根据ID获取患者详情 |
| CreateAsync(PatientInputDto) | 创建患者 |
| UpdateAsync(Guid id, PatientInputDto) | 更新患者 |
| DeleteAsync(Guid id) | 软删除患者(含引用检查) |
| SearchAsync(string keyword) | 搜索患者 |
| BatchImportAsync(Stream, string? fileName) | Excel批量导入患者(Epic #1934) |
| ExportTemplateAsync(ExportTemplateDto) | 导出导入模板Excel |
| ExportPatientsAsync(string? keyword) | 导出患者数据到Excel |
| ToggleStatusAsync(Guid id) | 切换启用/禁用状态 |
| RestoreAsync(Guid id) | 恢复软删除的患者 |
| BatchDeleteAsync(List\<Guid\> ids) | 批量删除患者(含逐个引用检查) |
| CheckReferenceAsync(Guid patientId) | 检查患者是否被医案引用 |
| BatchCheckReferenceAsync(List\<Guid\> patientIds) | 批量检查患者引用关系(最多100条) |

Entity直接返回方法:
| 方法 | 说明 |
|------|------|
| GetPagedEntityAsync(int page, int pageSize, string? keyword) | 分页查询，直接返回Patient Entity |
| GetByIdEntityAsync(Guid id) | 根据ID获取，直接返回Patient Entity |
| CreateEntityAsync(PatientInputDto) | 创建患者，直接返回Patient Entity |
| UpdateEntityAsync(Guid id, PatientInputDto) | 更新患者，直接返回Patient Entity |

### Interfaces/IPatientRepository.cs
**IPatientRepository** : IRepository\<Patient\> | 患者仓储接口，继承11个标准CRUD方法

自定义方法:
| 方法 | 说明 |
|------|------|
| GetByNameAsync(string name) | 根据姓名模糊查询患者列表 |
| ExistsAsync(string name, Guid? excludeId) | 检查患者姓名唯一性(支持排除ID) |
| GetByPhoneNumberAsync(string phoneNumber) | 根据手机号查询患者(Epic #1934 BR-004重复检查) |
| GetByIdNumberAsync(string idNumber) | 根据身份证号查询患者(T5-P2-24唯一性检查) |
| GetPagedWithStatusFilterAsync(int page, int pageSize, string? keyword, CommonStatus status) | 分页查询(支持状态过滤，T5-P2-27角色过滤) |
| GetByIdIncludingDeletedAsync(Guid id) | 获取包含已软删除的实体(用于Restore) |

### Mapping/PatientMapper.cs
**PatientMapper** (partial, Mapperly) | 编译时映射器

| 方法 | 说明 |
|------|------|
| ToListDto(Patient) | Patient实体转PatientListDto |
| ToListDtos(List\<Patient\>) | 批量转换 |
| ToDetailDto(Patient) | Patient实体转PatientDetailDto |
| ToDetailDtos(List\<Patient\>) | 批量转换 |
| ToEntity(PatientInputDto) | PatientInputDto转Patient实体(创建)，忽略Id/Age/PinYinCode/Status/审计字段 |
| UpdateEntity(PatientInputDto, Patient) | 更新现有实体，忽略Id/Age/PinYinCode/Status/审计字段 |
| UpdateEntityFromDetail(PatientDetailDto, Patient) | DetailDto更新到实体 |

### Repositories/PatientRepository.cs
**PatientRepository** (internal) : BaseRepository\<Patient\>, IPatientRepository | 患者仓储实现

模板方法覆盖:
| 方法 | 说明 |
|------|------|
| ApplyKeywordFilter(IQueryable, string) | 按姓名、拼音码过滤 |
| ApplyDefaultOrdering(IQueryable) | 按姓名升序排序 |

自定义方法:
| 方法 | 说明 |
|------|------|
| GetByNameAsync(string name) | 姓名模糊查询，AsNoTracking |
| ExistsAsync(string name, Guid? excludeId) | AnyAsync检查姓名是否存在 |
| GetByPhoneNumberAsync(string phoneNumber) | 手机号查询，AsNoTracking |
| GetByIdNumberAsync(string idNumber) | 身份证号查询，AsNoTracking |
| GetPagedWithStatusFilterAsync(...) | 带状态过滤的分页查询，复用模板方法 |
| GetByIdIncludingDeletedAsync(Guid) | 使用IgnoreQueryFilters绕过全局软删除过滤器 |

### Services/PatientService.cs
**PatientService** : BaseService\<Patient\>, IPatientService | 患者服务实现 (987行，模块内最大文件)

依赖: IPatientRepository, IValidator\<PatientInputDto\>, PatientMapper, AppDbContext (引用检查), ICacheInvalidationService

创建/更新流程: FluentValidation -> 手机号唯一性 -> 身份证号唯一性 -> Mapperly映射 -> 拼音码生成 -> 保存 -> 缓存失效

删除流程: 引用检查(CheckReferenceAsync) -> 有引用则拒绝 -> 软删除 -> 缓存失效

批量导入(BatchImportAsync): Excel解析(EPPlus) -> 逐行FluentValidation -> 批量内手机号/身份证号去重 -> DB重复检查 -> 批量AddRangeAsync保存，支持部分成功模式

导出: EPPlus生成Excel文件流，模板支持示例数据，数据导出最大10000条

引用检查(CheckReferenceAsync): 查询MedicalCases表关联计数，返回最近5条引用详情

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| PatientMapper.UpdateEntityFromDetail | [DEAD] 定义但从未被调用 | 当前业务仅使用UpdateEntity(PatientInputDto, Patient) | 可安全删除 |
| IPatientService.GetPagedListAsync | [SUSPECT] 仅接口+实现，无外部调用者 | 与GetPagedAsync功能几乎完全重复 | 确认是否有未来使用计划，否则删除 |
| IPatientService.GetPagedEntityAsync | [SUSPECT] 仅接口+实现，无外部调用者 | GetPagedAsync已满足需求 | 确认是否有未来使用计划，否则删除 |

## 设计分析

| 文件/目录 | 问题 | 分析 | 建议 |
|-----------|------|------|------|
| Services/PatientService.cs | 文件过大(987行) | 包含CRUD、Excel导入导出、引用检查、批量操作、Entity返回等多个职责 | 拆分为：PatientService(CRUD) + PatientImportExportService(Excel操作) + PatientReferenceService(引用检查) |
| Services/PatientService.cs | GetPagedAsync与GetPagedListAsync近乎重复 | 两个方法逻辑几乎相同，都返回PagedResult\<PatientListDto\> | 保留一个，删除另一个 |
| Services/PatientService.cs | Entity直接返回方法与DTO方法重复 | Create/Update/GetPaged各有DTO和Entity两个版本，增加维护负担 | 如果Entity方法仅供Desktop端使用且无外部调用者，考虑移除 |
| Services/PatientService.cs | Age属性手动复制 | 多处出现dto.Age = entity.Age手动复制，因Age是实体上的计算属性，Mapperly无法自动映射 | 考虑在Mapperly配置中添加自定义映射，或在DTO层实现Age计算 |
| Services/PatientService.cs | 直接依赖AppDbContext | 引用检查(CheckReferenceAsync)直接通过DbContext查询MedicalCases表 | 应通过跨模块查询服务(CrossModuleService)访问MedicalCase数据 |
| PatientsModule.cs | 注释掉的注册代码 | 有被注释的IMedicalRecordRepository和IMedicalRecordService | 确认是否为历史遗留，清理注释代码 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| Age是计算属性，Mapperly不自动映射 | Patient.Age是基于BirthDate的只读计算属性，Mapper忽略了它 | 每次DTO转换后手动设置dto.Age = entity.Age |
| 批量导入最大1000行限制 | BR-003硬编码限制，T5-P3-11修复了表头行off-by-one | 如需调整限制，修改BatchImportAsync中的常量 |
| 批量导入内去重基于HashSet | 手机号和身份证号在本次导入批次内用HashSet去重 | 仅同批次内去重，不同批次的重复由DB查询检查 |
| EF Core 8 FindAsync与软删除 | FindAsync在实体不在ChangeTracker中时会应用全局查询过滤器(IsDeleted) | Restore操作使用GetByIdIncludingDeletedAsync(IgnoreQueryFilters) |
| 删除前必须通过引用检查 | 有MedicalCase关联的患者不可删除(X7规则) | DeleteAsync和BatchDeleteAsync都先查询MedicalCases计数 |
| EPPlus需要LicenseContext设置 | EPPlus 5.x+ 需要显式设置LicenseContext | 每个使用EPPlus的方法入口都设置LicenseContext = NonCommercial |
