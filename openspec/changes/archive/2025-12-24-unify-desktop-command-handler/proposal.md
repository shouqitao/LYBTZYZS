# unify-desktop-command-handler

## Summary

统一Desktop层数据处理架构，消除碎片化模式。所有ViewModel统一通过CommandHandler处理数据操作，禁止直接依赖Repository。

## Motivation

### 问题现状

Desktop层数据访问模式高度碎片化，存在以下问题：

#### 问题1: 混合模式（ViewModel同时依赖Repository和Handler）

| ViewModel | Repository | Handler |
|-----------|------------|---------|
| UserMasterDetailViewModel | IUserRepository | UserCommandHandler |
| PatientMasterDetailViewModel | IPatientRepository | PatientCommandHandler |
| FormulaMasterDetailViewModel | IFormulaRepository | IFormulaCommandHandler |
| HerbMasterDetailViewModel | IHerbRepository | IHerbCommandHandler |

#### 问题2: 直接调用Repository（绕过数据处理层）

| ViewModel | 直接依赖的Repository |
|-----------|---------------------|
| ChangePasswordViewModel | IUserRepository |
| PatientDetailViewModel | IPatientRepository |
| HerbDetailViewModel | IHerbRepository |
| MedicalCaseMasterDetailViewModel | IMedicalCaseRepository |
| PrescriptionPanelViewModel | IMedicalCaseRepository, IHerbRepository |
| ConsultationPanelViewModel | IMedicalCaseRepository |

#### 问题3: 命名不一致

- CommandHandler vs DataManager（功能相似但命名不同）
- 接口(IXxxCommandHandler) vs 具体类(XxxCommandHandler)

#### 问题4: 双处理层

- FormulaDetailViewModel: 同时依赖CommandHandler和DataManager
- ConsultationFormViewModel: 同时依赖CommandHandler和DataManager

### 目标

1. **统一数据处理层**: 全部使用CommandHandler模式，废弃DataManager
2. **统一ViewModel依赖**: ViewModel只依赖IXxxCommandHandler接口
3. **消除碎片化**: 禁止ViewModel直接依赖Repository
4. **统一命名规范**: 接口用IXxxCommandHandler，日志前缀[CMD]

## Design

### 统一架构

```
┌─────────────────────────────────────────────────────────────┐
│                      ViewModel Layer                        │
│  (只依赖 IXxxCommandHandler，禁止直接依赖 Repository)        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   CommandHandler Layer                      │
│  - 无状态设计                                                │
│  - 统一返回 (bool success, T? data, string? error)          │
│  - 统一日志前缀 [CMD]                                        │
│  - 统一异常处理                                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     Repository Layer                        │
│  (HTTP调用，ViewModel不可直接访问)                           │
└─────────────────────────────────────────────────────────────┘
```

### 模块重构清单

| 模块 | 当前状态 | 目标状态 | 工作量 |
|------|----------|----------|--------|
| **Herbs** | DataManager模式 | CommandHandler | 高 |
| **Users** | 混合模式 | 纯CommandHandler | 中 |
| **Patients** | 混合模式 | 纯CommandHandler | 中 |
| **Formula** | 双处理层+混合 | 纯CommandHandler | 高 |
| **MedicalCase** | DataManager+直接调用 | CommandHandler | 高 |
| **Consultation** | 双处理层 | 纯CommandHandler | 中 |
| **Auth** | Coordinator模式 | 保持现状 | 无 |
| **Prescriptions** | PrintService | 保持现状 | 无 |

### CommandHandler规范

#### 接口命名

```csharp
// 统一使用 IXxxCommandHandler 接口
public interface IUserCommandHandler { }
public interface IPatientCommandHandler { }
public interface IFormulaCommandHandler { }
public interface IHerbCommandHandler { }
public interface IMedicalCaseCommandHandler { }
public interface IConsultationCommandHandler { }
```

#### 返回类型规范

```csharp
// CRUD操作返回tuple
Task<(bool success, TDetailDto? data, string? error)> CreateAsync(TInputDto input);
Task<(bool success, TDetailDto? data, string? error)> UpdateAsync(Guid id, TInputDto input);
Task<(bool success, string? error)> DeleteAsync(Guid id);
Task<(bool success, TDetailDto? data, string? error)> GetByIdAsync(Guid id);

// 列表查询可返回PagedResult（只读操作）
Task<PagedResult<TListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null);
```

#### 实现规范

```csharp
public class XxxCommandHandler : IXxxCommandHandler
{
    private readonly IXxxRepository _repository;
    private readonly ILogger<XxxCommandHandler> _logger;

    // 禁止: Current, HasChanges, _original* 等状态属性/字段

    public async Task<(bool success, XxxDetailDto? data, string? error)> CreateAsync(XxxInputDto input)
    {
        try
        {
            _logger.LogInformation("[CMD] CreateXxx started: {Key}", input.Key);
            var result = await _repository.CreateAsync(input);
            _logger.LogInformation("[CMD] CreateXxx completed: {Id}", result.Id);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CMD] CreateXxx failed: {Key}", input.Key);
            return (false, null, "创建失败，请重试");
        }
    }
}
```

### ViewModel依赖规范

```csharp
// 正确: 只依赖CommandHandler接口
public class XxxMasterDetailViewModel
{
    private readonly IXxxCommandHandler _commandHandler;
    // 禁止: private readonly IXxxRepository _repository;
}

// 跨模块只读查询: 通过CommandHandler的GetPagedAsync
public class FormulaMasterDetailViewModel
{
    private readonly IFormulaCommandHandler _commandHandler;
    private readonly IHerbCommandHandler _herbCommandHandler; // 用于获取药材列表
}
```

### 只读查询规范（基于CQRS最佳实践）

根据Microsoft CQRS指南："对于只读查询，将多个对象作为单一聚合处理只会增加复杂性而无实际收益"。

#### 设计决策

| 场景 | 处理方式 | 理由 |
|------|----------|------|
| **CRUD操作** | 必须通过CommandHandler | 需要统一日志、异常处理、事务管理 |
| **本模块列表查询** | CommandHandler.GetPagedAsync | 保持一致性，便于添加缓存/日志 |
| **跨模块只读查询** | 目标模块的CommandHandler.GetPagedAsync | 保持依赖方向正确 |

#### 实现方式

```csharp
// 正确: 跨模块查询通过目标模块的CommandHandler
public class FormulaDetailViewModel
{
    private readonly IFormulaCommandHandler _commandHandler;
    private readonly IHerbCommandHandler _herbCommandHandler; // 用于获取药材列表

    private async Task LoadHerbsAsync()
    {
        var herbs = await _herbCommandHandler.GetPagedAsync(1, 100);
        // ...
    }
}

// 禁止: 直接依赖其他模块的Repository
public class FormulaDetailViewModel
{
    private readonly IHerbRepository _herbRepository; // ❌ 禁止
}

// 禁止: 使用ServiceLocator模式
public class FormulaDetailViewModel
{
    private readonly IContainerProvider _container;
    private async Task LoadHerbsAsync()
    {
        var repo = _container.Resolve<IHerbRepository>(); // ❌ ServiceLocator反模式
    }
}
```

#### CommandHandler查询方法规范

```csharp
public interface IHerbCommandHandler
{
    // CRUD操作（返回tuple）
    Task<(bool success, HerbDetailDto? data, string? error)> CreateAsync(...);
    Task<(bool success, HerbDetailDto? data, string? error)> UpdateAsync(...);
    Task<(bool success, string? error)> DeleteAsync(...);
    Task<(bool success, HerbDetailDto? data, string? error)> GetByIdAsync(...);

    // 只读查询（直接返回结果，无需tuple包装）
    Task<PagedResult<HerbListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null);
    Task<IReadOnlyList<HerbListDto>> GetAllAsync(); // 可选，用于下拉列表等
}
```

### 变更清单

#### Phase 1: Herbs模块（已完成）
- [x] 创建IHerbCommandHandler接口
- [x] 创建HerbCommandHandler实现
- [x] 重构HerbMasterDetailViewModel
- [x] 删除IHerbDataManager, HerbDataManager
- [x] 删除IDataManager基接口

#### Phase 2: Users模块
- [ ] 重构UserMasterDetailViewModel移除IUserRepository依赖
- [ ] 重构ChangePasswordViewModel使用UserCommandHandler
- [ ] 接口化: IUserCommandHandler

#### Phase 3: Patients模块
- [ ] 重构PatientMasterDetailViewModel移除IPatientRepository依赖
- [ ] 重构PatientDetailViewModel使用PatientCommandHandler
- [ ] 接口化: IPatientCommandHandler

#### Phase 4: Formula模块
- [ ] 删除IFormulaDataManager, FormulaDataManager
- [ ] 重构FormulaDetailViewModel移除DataManager依赖
- [ ] 重构FormulaMasterDetailViewModel移除IFormulaRepository依赖
- [ ] 补充IHerbCommandHandler.GetPagedAsync用于药材列表

#### Phase 5: MedicalCase模块
- [ ] 创建IMedicalCaseCommandHandler接口
- [ ] 重构MedicalCaseDataManager为MedicalCaseCommandHandler
- [ ] 重构所有ViewModel移除IMedicalCaseRepository直接依赖
- [ ] 删除IMedicalCaseDataManager接口

#### Phase 6: Consultation模块
- [ ] 删除IConsultationDataManager
- [ ] 重构ConsultationFormViewModel移除DataManager依赖
- [ ] 接口化: IConsultationCommandHandler

## Risks

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| 大范围重构引入Bug | 高 | 分Phase执行，每Phase编译验证 |
| 跨模块依赖复杂 | 中 | 允许CommandHandler间调用获取只读数据 |
| 遗漏依赖引用 | 低 | 全局搜索Repository/DataManager关键词 |

## Success Criteria

- [ ] 编译通过，零错误零警告
- [ ] 所有DataManager接口及实现已删除
- [ ] 所有ViewModel只依赖IXxxCommandHandler
- [ ] 无ViewModel直接依赖Repository
- [ ] 所有CommandHandler日志包含[CMD]前缀
- [ ] 全模块CRUD功能正常

## References

- 现有CommandHandler实现: `UserCommandHandler.cs`
- 日志规范: `enhance-dataflow-logging` OpenSpec提案
- 聚合根设计: MedicalCase包含Consultation+Prescription

---

**Created**: 2025-12-24
**Updated**: 2025-12-24
**Author**: Claude Code
**Status**: In Progress
