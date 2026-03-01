# LYBT.Module.Formula 代码知识

验方(经验方)模块 - 提供验方的CRUD、导入导出、药材验证、批量操作等业务功能。

## 代码文件结构

```
LYBT.Module.Formula/
├── FormulaModule.cs                          # 模块DI注册
├── Interfaces/
│   ├── IFormulaService.cs                    # 验方业务服务接口
│   ├── IFormulaImportExportService.cs        # 导入导出服务接口 (SRP拆分)
│   └── IFormulaRepository.cs                 # 验方仓储接口
├── Mapping/
│   └── FormulaMapper.cs                      # Mapperly编译时映射器
├── Repositories/
│   └── FormulaRepository.cs                  # 验方仓储实现
└── Services/
    ├── FormulaService.cs                     # 验方业务服务实现
    └── FormulaImportExportService.cs         # 导入导出服务实现
```

### FormulaModule.cs
**FormulaModule** (static) | 模块DI注册入口

| 方法 | 说明 |
|------|------|
| AddFormulaModule(IServiceCollection) | 注册 IFormulaRepository, IFormulaService, IFormulaImportExportService, FluentValidation验证器 |

DI注册清单:
- `IFormulaRepository` -> `FormulaRepository` (Scoped)
- `IFormulaService` -> `FormulaService` (Scoped)
- `IFormulaImportExportService` -> `FormulaImportExportService` (Scoped)
- FluentValidation: `FormulaInputDtoValidator` 所在程序集全部注册

### Interfaces/IFormulaService.cs
**IFormulaService** | 验方业务服务接口

| 方法 | 说明 |
|------|------|
| GetPagedAsync(page, pageSize, keyword?, category?, currentUserId?, isAdmin) | 分页查询，支持分类筛选和角色过滤 |
| GetByIdAsync(Guid id) | 获取验方详情 |
| CreateAsync(FormulaInputDto dto, Guid? creatorId) | 创建验方，creatorId用于所有权设置 |
| UpdateAsync(Guid id, FormulaInputDto dto) | 更新验方 |
| DeleteAsync(Guid id) | 软删除验方 |
| SearchAsync(string keyword) | 多条件搜索，返回前100条 |
| ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId) | 手动绑定药材到系统药材库 |
| GetPendingValidationFormulasAsync() | 获取 ValidationStatus=Draft 的待验证验方 |
| ToggleStatusAsync(Guid id) | 切换启用/禁用状态 |
| RestoreAsync(Guid id) | 恢复软删除的验方 |
| BatchDeleteAsync(List\<Guid\> ids) | 批量软删除 |
| BatchUpdateStatusAsync(List\<Guid\> ids, CommonStatus status) | 批量更新状态 |

### Interfaces/IFormulaImportExportService.cs
**IFormulaImportExportService** | 导入导出服务接口 (OpenSpec: refactor-server-srp-patterns 从FormulaService拆分)

| 方法 | 说明 |
|------|------|
| ImportFromDataAsync(List\<FormulaImportItemDto\>, string? fileName) | 批量导入结构化DTO (Server不解析Excel) |
| ExportAsync(string? category) | 导出验方到Excel (含药材组成Sheet) |
| GenerateImportTemplate() | 生成主-从Excel导入模板 |

### Interfaces/IFormulaRepository.cs
**IFormulaRepository** : IRepository\<Formula\> | 验方仓储接口

| 方法 | 说明 |
|------|------|
| GetTemplatesAsync() | 获取模板验方列表 |
| GetByIdWithHerbsAsync(Guid id) | 获取验方含药材配伍 |
| GetPagedWithDetailsAsync(pageNumber, pageSize, keyword?) | 分页查询含药材 (内部用于SearchAsync) |
| GetPagedWithDetailsAsync(pageNumber, pageSize, keyword?, category?, userId?, isAdmin) | 分页查询含分类/角色筛选 (DB层) |
| GetByUserIdAsync(Guid userId) | 按用户ID获取 (自己的+共享的) |
| GetAllWithHerbsAsync() | 获取全部含药材，用于导出 |
| GetByIdIncludingDeletedAsync(Guid id) | 获取含已软删除实体 (IgnoreQueryFilters) |

### Mapping/FormulaMapper.cs
**FormulaMapper** (partial, Mapperly) | 编译时映射器

| 映射方法 | 说明 |
|----------|------|
| ToListDto(Formula) -> FormulaListDto | 列表DTO (忽略HerbCount/TotalPrice) |
| ToListDtos(List\<Formula\>) -> List\<FormulaListDto\> | 列表批量映射 |
| ToDetailDto(Formula) -> FormulaDetailDto | 详情DTO (忽略HerbCount/TotalPrice/Description/Source/Contraindications) |
| ToDetailDtos(List\<Formula\>) -> List\<FormulaDetailDto\> | 详情批量映射 |
| ToHerbItemDto(FormulaHerbItem) -> FormulaHerbItemDto | 药材项映射 (忽略多个Service层填充字段) |
| ToHerbItemDtos(List\<FormulaHerbItem\>) -> List\<FormulaHerbItemDto\> | 药材项批量映射 |
| ToEntity(FormulaInputDto) -> Formula | 创建实体 (忽略审计字段/Status/Herbs等) |
| UpdateEntity(FormulaInputDto, Formula) | 更新实体 (忽略审计字段/Status/Herbs等) |

映射特殊配置: `Indication` <-> `Indications` 属性名映射 (MapProperty)

### Repositories/FormulaRepository.cs
**FormulaRepository** (internal) : BaseRepository\<Formula\>, IFormulaRepository | 验方仓储实现

| 方法 | 说明 |
|------|------|
| ApplyKeywordFilter(query, keyword) | 模板方法覆盖: 按名称/功效过滤 |
| GetBaseQuery() | 内部统一查询基础 (Include Herbs + 排除软删除) |
| GetTemplatesAsync() | 获取全部验方按名称排序 |
| GetByIdWithHerbsAsync(Guid id) | 按ID查询含药材 |
| GetPagedWithDetailsAsync(pageNumber, pageSize, keyword?) | 分页查询 (使用模板方法) |
| GetPagedWithDetailsAsync(pageNumber, pageSize, keyword?, category?, userId?, isAdmin) | 分页查询含DB层分类/角色过滤 |
| GetByUserIdAsync(Guid userId) | 按用户ID + IsShared过滤 |
| GetAllWithHerbsAsync() | 全量查询用于导出 |
| GetByIdIncludingDeletedAsync(Guid id) | IgnoreQueryFilters绕过软删除过滤器 |

### Services/FormulaService.cs
**FormulaService** : BaseService, IFormulaService | 验方业务服务

依赖注入: IFormulaRepository, IHerbCrossModuleService, ICacheInvalidationService, ILogger

| 方法 | 说明 |
|------|------|
| GetPagedAsync(...) | 分页查询，调用Repository DB层筛选 |
| GetByIdAsync(Guid id) | 获取详情，使用GetByIdWithHerbsAsync |
| CreateAsync(FormulaInputDto, Guid? creatorId) | 手动创建entity (不依赖Mapper处理Herbs集合) |
| UpdateAsync(Guid id, FormulaInputDto) | 粗粒度全量替换Herbs (Formula-Design-Decision-002) |
| SearchAsync(string keyword) | 复用分页查询取前100条 |
| DeleteAsync(Guid id) | 软删除，成功后InvalidateCache("formulas") |
| ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId) | 通过IHerbCrossModuleService验证药材，全部验证后更新ValidationStatus |
| GetPendingValidationFormulasAsync() | FindAsync过滤Draft状态 |
| ToggleStatusAsync(Guid id) | 切换Enabled/Disabled状态 |
| RestoreAsync(Guid id) | 恢复软删除 (使用GetByIdIncludingDeletedAsync) |
| BatchDeleteAsync(List\<Guid\> ids) | 逐项软删除，保留项级错误隔离 |
| BatchUpdateStatusAsync(List\<Guid\> ids, CommonStatus) | 逐项更新状态，批量SaveChanges |

### Services/FormulaImportExportService.cs
**FormulaImportExportService** : IFormulaImportExportService | 导入导出服务

依赖注入: IFormulaRepository, IHerbCrossModuleService, ICacheInvalidationService, ILogger

| 方法 | 说明 |
|------|------|
| ImportFromDataAsync(formulas, fileName?) | 逐条导入，自动匹配药材(TryMatchHerbAsync)，自动判断ValidationStatus |
| ExportAsync(string? category) | 导出两个Sheet: 验方列表 + 药材组成 (EPPlus) |
| GenerateImportTemplate() | 生成主-从模板: 验方信息Sheet + 药材明细Sheet (EPPlus) |
| TryMatchHerbAsync(string herbName) | (private) 通过IHerbCrossModuleService按名称/拼音匹配药材 |

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| IFormulaRepository.GetTemplatesAsync() | [DEAD] 仅接口+实现定义，无调用方 | GetPagedWithDetailsAsync | 可安全移除 |
| IFormulaRepository.GetByUserIdAsync(Guid) | [DEAD] 仅接口+实现定义，无调用方 | GetPagedWithDetailsAsync(含role筛选) | 可安全移除 |

## 设计分析

| 文件/目录 | 问题 | 分析 | 建议 |
|-----------|------|------|------|
| FormulaService.CreateAsync | 未使用FormulaMapper.ToEntity | 因Herbs集合需手动构建，直接new Formula | 可接受，Mapper不适合处理子集合 |
| FormulaService.UpdateAsync | 实体变更直接赋值 (mutation) | Herbs使用Clear()+Add()全量替换 (Formula-Design-Decision-002) | 符合DDD聚合根模式 |
| FormulaService.BatchDeleteAsync | 逐项查询+更新，无批量SQL | N+1查询问题，但保留了项级错误隔离 | 数据量小可接受，大批量考虑ExecuteUpdateAsync |
| FormulaMapper | Indication/Indications字段名不一致 | 实体用Indication(单数)，DTO用Indications(复数)，需MapProperty配置 | 统一命名需跨层修改，暂保持 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| GetByIdIncludingDeletedAsync 不能用 FindAsync | EF Core 8 中 FindAsync 受全局查询过滤器(IsDeleted)影响 | 使用 IgnoreQueryFilters() + FirstOrDefaultAsync |
| CreateAsync/UpdateAsync 中 Herbs 不能用 Mapper | FormulaHerbItem 子集合需要设置 OriginalHerbName/IsValidated 等业务字段 | 手动构建 FormulaHerbItem 集合 |
| Indication vs Indications 映射 | 实体字段为 Indication(单数)，DTO为 Indications(复数) | FormulaMapper 使用 MapProperty 显式映射 |
| BatchUpdateStatusAsync 先逐项更新后统一 SaveChanges | UpdateAsync 内部可能已调用 SaveChanges | 确认 BaseRepository.UpdateAsync 是否自动 SaveChanges |
