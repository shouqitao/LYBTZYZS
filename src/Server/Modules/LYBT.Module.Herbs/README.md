# LYBT.Module.Herbs

> 中药材档案管理 | 传统三层 | Record-Only模式(无库存)

## 项目定位

- **层级**: Server端
- **架构模式**: 传统三层
- **跨模块通信**: 被Formula/Prescriptions模块引用

## 目录结构

```
LYBT.Module.Herbs/
├── HerbsModule.cs
├── Interfaces/
│   └── IHerbRepository.cs
├── Services/
│   └── HerbService.cs
├── Repositories/
│   └── HerbRepository.cs
├── Validators/
│   ├── HerbCreateDtoValidator.cs
│   └── HerbUpdateDtoValidator.cs
└── Mapping/
    └── HerbMappingProfile.cs
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IHerbService | 13 | CRUD/搜索/批量导入导出 |
| IHerbRepository | 2 | 按名称精确查询/拼音模糊查询 |

## 药材字段

| 字段 | 说明 |
|------|------|
| Name | 药材名称 |
| Category | 分类(补益药、清热药等) |
| Effects | 功效(补气养血等) |
| UnitPrice | 单价(元/克) |
| DefaultUnit | 默认计量单位(克、两) |
| DefaultDosage | 常用剂量(3-9g) |
| PinyinAbbreviation | 拼音首字母(快速检索) |

## 设计特点

| 特点 | 说明 |
|------|------|
| Record-Only模式 | 只管理药材档案，不涉及库存 |
| 拼音检索 | 输入"dg"可匹配"当归" |
| 批量导入 | Excel导入，自动去重验证 |

## 设计依据

- Record-Only 模式只管理药材档案信息，不涉及库存管理，符合小型诊所 MVP 定位
- 拼音首字母检索 (PinyinAbbreviation) 适配中医处方开具时的快速药材选择场景
- FluentValidation 在 Service 层做输入验证，BaseService 泛型基类复用错误处理逻辑
- 作为被引用最多的基础数据模块，通过 IHerbCrossModuleService 向 Formula/MedicalCase 暴露只读查询

## 依赖关系

### 依赖
- LYBT.Infrastructure (BaseRepository, AppDbContext)
- LYBT.Entities (HerbModel实体)
- LYBT.Shared.Models (HerbDto等)

### 被依赖
- LYBT.Module.Formula (验方药材组成)
- LYBT.Module.Prescriptions (处方药材配伍)
- LYBT.WebAPI (HerbsController)

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/herbs | GET | 分页查询药材 |
| /api/herbs/{id} | GET | 按ID查询药材详情 |
| /api/herbs | POST | 创建药材 |
| /api/herbs/{id} | PUT | 更新药材 |
| /api/herbs/{id} | DELETE | 删除药材 |
| /api/herbs/search | GET | 搜索药材(名称/拼音/功效) |
| /api/herbs/import | POST | Excel导入药材 |
| /api/herbs/export | GET | 导出药材到Excel |
| /api/herbs/template | GET | 下载导入模板 |
| /api/herbs/batch-delete | POST | 批量删除药材 |

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |

## 开发笔记

# LYBT.Module.Herbs 代码知识

药材模块 - 提供药材的CRUD、Excel导入导出、批量导入、引用检查、批量操作等业务功能。

## 代码文件结构

```
LYBT.Module.Herbs/
├── HerbsModule.cs                            # 模块DI注册
├── Interfaces/
│   ├── IHerbService.cs                       # 药材业务服务接口
│   └── IHerbRepository.cs                    # 药材仓储接口
├── Mapping/
│   └── HerbMapper.cs                         # Mapperly编译时映射器
├── Repositories/
│   └── HerbRepository.cs                     # 药材仓储实现
└── Services/
    └── HerbService.cs                        # 药材业务服务实现
```

### HerbsModule.cs
**HerbsModule** (static) | 模块DI注册入口

| 方法 | 说明 |
|------|------|
| AddHerbsModule(IServiceCollection, IConfiguration) | 注册 IHerbRepository, IHerbService, FluentValidation验证器 |
| UseHerbsModule(IApplicationBuilder) | 中间件配置 (当前空实现) |

DI注册清单:
- `IHerbRepository` -> `HerbRepository` (Scoped)
- `IHerbService` -> `HerbService` (Scoped)
- FluentValidation: `HerbInputDtoValidator` 所在程序集全部注册
- 被注释的注册: IHerbCategoryRepository, IHerbCategoryService

### Interfaces/IHerbService.cs
**IHerbService** | 药材业务服务接口

| 方法 | 说明 |
|------|------|
| GetPagedAsync(page, pageSize, keyword?, category?) | 分页查询，支持关键字+分类筛选 |
| GetByIdAsync(Guid id) | 获取药材详情 |
| CreateAsync(HerbInputDto dto) | 创建药材 (自动生成拼音码) |
| UpdateAsync(Guid id, HerbInputDto dto) | 更新药材 (名称变更时重新生成拼音码) |
| DeleteAsync(Guid id) | 软删除 (删除前强制引用检查) |
| SearchAsync(string keyword) | 按名称/拼音码搜索 |
| ImportFromExcelAsync(Stream, string? fileName) | 从Excel导入药材 (Server端解析) |
| ExportAsync(string? category) | 导出药材到Excel |
| GenerateImportTemplate() | 生成Excel导入模板 |
| BatchImportAsync(List\<HerbInputDto\>, DuplicateStrategy) | 批量导入DTO (Skip/Update/Error策略，上限10000条) |
| GetAllForExportAsync(string? category) | 获取全部药材用于Desktop端导出 |
| CheckReferenceAsync(Guid herbId) | 检查药材被处方引用情况 |
| BatchCheckReferenceAsync(List\<Guid\> herbIds) | 批量引用检查 (上限100条) |
| ToggleStatusAsync(Guid id) | 切换启用/禁用状态 |
| RestoreAsync(Guid id) | 恢复软删除的药材 |
| BatchUpdateStatusAsync(List\<Guid\> ids, CommonStatus) | 批量更新状态 |
| BatchDeleteAsync(List\<Guid\> ids) | 批量软删除 |

### Interfaces/IHerbRepository.cs
**IHerbRepository** : IRepository\<Herb\> | 药材仓储接口 (继承11个标准CRUD方法)

| 方法 | 说明 |
|------|------|
| GetByNameAsync(string name) | 精确名称查询 |
| GetByNameOrPinyinAsync(string searchTerm) | 名称/拼音码模糊匹配 (FormulaService引用) |
| ExistsByNameAsync(string name, Guid? excludeId) | 名称重复检查 (批量导入用) |
| GetPagedAsync(pageNumber, pageSize, keyword?, category?) | 分页查询含DB层分类筛选 |
| GetByIdIncludingDeletedAsync(Guid id) | 获取含已软删除实体 |

### Mapping/HerbMapper.cs
**HerbMapper** (partial, Mapperly) | 编译时映射器

| 映射方法 | 说明 |
|----------|------|
| ToListDto(Herb) -> HerbListDto | 列表DTO |
| ToListDtos(List\<Herb\>) -> List\<HerbListDto\> | 列表批量映射 |
| ToDetailDto(Herb) -> HerbDetailDto | 详情DTO (忽略Properties字段) |
| ToDetailDtos(List\<Herb\>) -> List\<HerbDetailDto\> | 详情批量映射 |
| ToEntity(HerbInputDto) -> Herb | 创建实体 (忽略Id/Status/审计字段) |
| UpdateEntity(HerbInputDto, Herb) | 更新实体 (忽略Id/Status/审计字段) |
| ToEntityFromImport(HerbImportItemDto) -> Herb | 从导入DTO创建实体 (忽略多个业务字段) |

### Repositories/HerbRepository.cs
**HerbRepository** (internal) : BaseRepository\<Herb\>, IHerbRepository | 药材仓储实现

| 方法 | 说明 |
|------|------|
| ApplyKeywordFilter(query, keyword) | 模板方法覆盖: 按名称/拼音码过滤 |
| ApplyDefaultOrdering(query) | 模板方法覆盖: 按名称升序 |
| GetPagedAsync(pageNumber, pageSize, keyword?, category?) | 分页查询，DB层关键字+分类筛选 |
| GetByNameAsync(string name) | 精确匹配，AsNoTracking |
| GetByNameOrPinyinAsync(string searchTerm) | 优先精确名称匹配，其次拼音码模糊匹配 |
| ExistsByNameAsync(string name, Guid? excludeId) | 名称存在性检查，支持排除指定ID |
| GetByIdIncludingDeletedAsync(Guid id) | IgnoreQueryFilters绕过软删除过滤器 |

### Services/HerbService.cs
**HerbService** : BaseService\<Herb\>, IHerbService | 药材业务服务

依赖注入: IHerbRepository, IValidator\<HerbInputDto\>, AppDbContext, ICacheInvalidationService, ILogger

| 方法 | 说明 |
|------|------|
| GetPagedAsync(...) | 分页查询，调用Repository DB层筛选 |
| GetByIdAsync(Guid id) | 获取详情 |
| CreateAsync(HerbInputDto dto) | FluentValidation验证 + 自动生成拼音码 + Mapper创建 |
| UpdateAsync(Guid id, HerbInputDto dto) | 验证 + 名称变更时重新生成拼音码 + Mapper更新 |
| DeleteAsync(Guid id) | 删除前调用CheckReferenceAsync强制引用检查 |
| SearchAsync(string keyword) | 使用Repository.FindAsync按名称/拼音码搜索 |
| ImportFromExcelAsync(Stream, string? fileName) | Server端EPPlus解析Excel，逐行导入 |
| ExportAsync(string? category) | 导出到Excel (EPPlus) |
| GenerateImportTemplate() | 生成含示例数据的Excel模板 |
| BatchImportAsync(List\<HerbInputDto\>, DuplicateStrategy) | 批量导入，支持Skip/Update/Error策略 |
| GetAllForExportAsync(string? category) | 获取全部药材DTO |
| CheckReferenceAsync(Guid herbId) | 查询PrescriptionItems引用计数 + 最近5条引用 |
| BatchCheckReferenceAsync(List\<Guid\> herbIds) | 逐项调用CheckReferenceAsync |
| ToggleStatusAsync(Guid id) | 切换Enabled/Disabled状态 |
| RestoreAsync(Guid id) | 恢复软删除 (GetByIdIncludingDeletedAsync) |
| BatchUpdateStatusAsync(List\<Guid\> ids, CommonStatus) | 逐项更新状态 |
| BatchDeleteAsync(List\<Guid\> ids) | 逐项软删除 |

CheckReferenceAsync 查询路径: PrescriptionItems JOIN Prescriptions JOIN MedicalCases JOIN Patients

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| HerbMapper.ToEntityFromImport(HerbImportItemDto) | [DEAD] 仅Mapper定义，无调用方 | BatchImportAsync使用ToEntity(HerbInputDto) | 可安全移除 |
| IHerbRepository.GetByNameAsync(string) | [DEAD] 仅接口+实现定义，无调用方 | ExistsByNameAsync / GetByNameOrPinyinAsync | 可安全移除 |
| HerbsModule.UseHerbsModule(IApplicationBuilder) | [DEAD] 仅定义，无调用方 | 无 (空实现) | 可安全移除 |
| HerbsModule 中被注释的 IHerbCategoryRepository/IHerbCategoryService | [DEAD] 已注释代码 | 无 | 清理注释 |

## 设计分析

| 文件/目录 | 问题 | 分析 | 建议 |
|-----------|------|------|------|
| HerbService.CheckReferenceAsync | 直接使用AppDbContext跨聚合查询 | 绕过Repository查询PrescriptionItems/Prescriptions/MedicalCases/Patients | 考虑通过CrossModuleService封装 |
| HerbService.ImportFromExcelAsync | Server端解析Excel | 与Formula模块设计不一致 (Formula由Client端解析) | 保留兼容，新功能使用BatchImportAsync |
| HerbService.BatchImportAsync | 逐项查重 + 逐项插入/更新 | 大批量(10000条)时N+1查询性能问题 | 可考虑批量查询优化 |
| HerbService.ExportAsync | GetAllAsync全量加载后内存过滤分类 | 未使用Repository的DB层分类过滤 | 考虑使用Repository.GetPagedAsync |
| IHerbRepository | 接口文档中提到GetByCategoryAsync | IHerbRepository.cs注释提到此方法但实际未定义 | 清理过期注释 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| GetByIdIncludingDeletedAsync 不能用 FindAsync | EF Core 8 中 FindAsync 受全局查询过滤器(IsDeleted)影响 | 使用 IgnoreQueryFilters() + FirstOrDefaultAsync |
| 删除药材前必须检查引用 | DeleteAsync 内部强制调用 CheckReferenceAsync，有引用则拒绝删除 | 前端应先调用 CheckReferenceAsync 提示用户 |
| PinYinCode 自动生成 | CreateAsync/UpdateAsync/BatchImportAsync 均自动生成拼音码 | 使用 PinYinHelper.GetPinYinCode，依赖 LYBT.Shared.Utilities.Text |
| HerbDetailDto.Properties 实体无对应字段 | Herb 实体没有 Properties 字段 | HerbMapper 使用 MapperIgnoreTarget 忽略该字段 |
| ImportFromExcelAsync vs BatchImportAsync 两套导入 | 历史遗留: ImportFromExcelAsync 是 Server端Excel解析，BatchImportAsync 是新架构Client端解析 | 新功能使用 BatchImportAsync，ImportFromExcelAsync 保持向后兼容 |
