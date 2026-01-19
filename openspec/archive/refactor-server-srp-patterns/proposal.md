# refactor-server-srp-patterns

## Why

Server层代码膨胀问题，部分核心组件严重违反单一职责原则(SRP)，需要进行架构优化。

### 发现的问题

| 优先级 | 位置 | 问题类型 | 当前状态 | 期望状态 |
|--------|------|----------|----------|----------|
| H1 | MedicalCaseController.cs | 代码膨胀 | 1204行，包含3个内联Mapping方法 | <400行，Mapping迁移到Mapper |
| H2 | MedicalCaseCommandService.cs | 职责混合 | 1079行，包含validation/audit职责 | <600行，职责拆分 |
| H3 | 5个Controller | 代码重复 | 批量操作代码重复(BatchDelete等) | 提取到BaseController |
| M1 | Consultation/Prescriptions模块 | 死代码 | 已注册但无Controller调用 | 确认后删除 |
| M2 | FormulaService.cs | 职责混合 | 792行，包含ImportExport职责 | 提取FormulaImportExportService |
| M3 | 多个Service | 警告阈值 | 700+行（HerbService, UserService等） | 监控，超800行拆分 |
| L1 | Server模块 | 文档缺失 | 无CLAUDE.md文件 | 添加模块说明文档 |
| L2 | 架构文档 | 记录缺失 | 无SRP拆分记录 | 添加Serena记忆 |

### 影响分析

**变更范围**: Server端
**影响模块**: MedicalCase, Formula, Consultation, Prescriptions, 共享Controller基础设施
**风险等级**: Medium-High（涉及核心业务Controller和Service）

### 架构优势(已有)

1. MedicalCase已拆分为5个服务(Query, Command, Permission, State, Audit)
2. Auth模块架构良好(AuthService, JwtService, SecurityAuditService, TokenRevocationService)
3. 使用Riok.Mapperly，Mapping逻辑清晰

## What Changes

### Phase 1: HIGH优先级修复 (H1-H3)

#### H1: MedicalCaseController Mapping提取

将Controller中的3个内联Mapping方法迁移到MedicalCaseMapper:
- `MapToPrescriptionDetailDto(Prescription, Guid)`
- `MapToMedicalCaseDto(MedicalCase)`
- `MapToMedicalCaseDetailDto(MedicalCase)`

#### H2: MedicalCaseCommandService评估

分析现有1079行代码，识别可提取职责:
- 评估Validation逻辑是否可提取到单独Validator
- 评估是否需要进一步拆分

#### H3: BatchOperationControllerBase创建

创建Controller基类，统一批量操作:
- `BatchDeleteAsync<TEntity>(ids)`
- `BatchEnableAsync<TEntity>(ids)`
- `BatchDisableAsync<TEntity>(ids)`
- `ToggleStatusAsync<TEntity>(id)`
- `RestoreAsync<TEntity>(id)`

5个Controller继承此基类: UsersController, FormulasController, HerbsController, PatientsController, MedicalCaseController

### Phase 2: MEDIUM优先级修复 (M1-M3)

#### M1: Consultation/Prescriptions模块清理

1. 验证模块确为死代码（无Controller引用）
2. 从ServiceCollectionExtensions.cs移除注册
3. 删除模块目录

#### M2: FormulaImportExportService提取

从FormulaService(792行)提取:
- `ImportFromDataAsync()`
- `ExportAsync()`
- `GenerateImportTemplate()`

创建独立的`IFormulaImportExportService`接口和实现。

#### M3: Service大小监控

- 记录当前警告阈值Service(700+行)
- 超过800行时优先拆分
- 建立监控机制

### Phase 3: LOW优先级改进 (L1-L2)

#### L1: Server模块CLAUDE.md

为以下模块添加CLAUDE.md:
- LYBT.Module.MedicalCase
- LYBT.Module.Formula
- LYBT.Module.Patients
- LYBT.Module.Herbs
- LYBT.Module.Users
- LYBT.Module.Auth

#### L2: Serena记忆更新

记录本次SRP重构:
- 架构决策记录
- 拆分模式说明
- 警告阈值标准

## Architecture

### 变更影响范围

```
src/Server/
├── Services/LYBT.WebAPI/
│   ├── Controllers/
│   │   ├── Base/
│   │   │   └── BatchOperationControllerBase.cs  [NEW]
│   │   ├── MedicalCaseController.cs              [MODIFY]
│   │   ├── UsersController.cs                    [MODIFY]
│   │   ├── FormulasController.cs                 [MODIFY]
│   │   ├── HerbsController.cs                    [MODIFY]
│   │   └── PatientsController.cs                 [MODIFY]
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs        [MODIFY - 移除死模块注册]
│
├── Modules/
│   ├── LYBT.Module.MedicalCase/
│   │   ├── Mapping/
│   │   │   └── MedicalCaseMapper.cs              [MODIFY - 添加Mapping方法]
│   │   ├── Services/
│   │   │   └── MedicalCaseCommandService.cs      [EVALUATE]
│   │   └── CLAUDE.md                             [NEW]
│   │
│   ├── LYBT.Module.Formula/
│   │   ├── Interfaces/
│   │   │   └── IFormulaImportExportService.cs    [NEW]
│   │   ├── Services/
│   │   │   ├── FormulaService.cs                 [MODIFY]
│   │   │   └── FormulaImportExportService.cs     [NEW]
│   │   └── CLAUDE.md                             [NEW]
│   │
│   ├── LYBT.Module.Consultation/                 [DELETE - 确认死代码后]
│   └── LYBT.Module.Prescriptions/                [DELETE - 确认死代码后]
```

### 新增接口

```csharp
// H3: 批量操作Controller基类
public abstract class BatchOperationControllerBase<TEntity, TService> : ControllerBase
    where TEntity : class
    where TService : IBatchOperationService<TEntity>
{
    [HttpPost("batch-delete")]
    public virtual async Task<IActionResult> BatchDeleteAsync([FromBody] List<Guid> ids);

    [HttpPost("batch-enable")]
    public virtual async Task<IActionResult> BatchEnableAsync([FromBody] List<Guid> ids);

    [HttpPost("batch-disable")]
    public virtual async Task<IActionResult> BatchDisableAsync([FromBody] List<Guid> ids);

    [HttpPost("{id}/toggle-status")]
    public virtual async Task<IActionResult> ToggleStatusAsync(Guid id);

    [HttpPost("{id}/restore")]
    public virtual async Task<IActionResult> RestoreAsync(Guid id);
}

// M2: 验方导入导出服务
public interface IFormulaImportExportService
{
    Task<ImportResult> ImportFromDataAsync(Stream data);
    Task<byte[]> ExportAsync(ExportOptions options);
    byte[] GenerateImportTemplate();
}
```

## Impact

- **文件变更**: 约20个文件
- **新增文件**: 8-10个(BaseController, Services, Interfaces, CLAUDE.md)
- **删除文件**: 约10个(Consultation/Prescriptions模块)
- **风险等级**: Medium-High
- **测试要求**:
  - 批量操作API端点测试
  - MedicalCase CRUD功能测试
  - Formula导入导出功能测试

## Risks

| 风险 | 缓解措施 |
|------|----------|
| Controller继承可能影响现有API | 保持API签名不变，仅重构内部实现 |
| 删除模块可能影响未发现的引用 | 使用Grep全面搜索确认无引用 |
| Service拆分可能引入新依赖问题 | 保持接口向后兼容，渐进式迁移 |
| Mapping迁移可能遗漏边界情况 | 对比测试确保输出一致 |

## Validation

### 编译验证
```bash
dotnet build LYBT.Server.sln -c Release --no-restore
```

### 功能验证
- MedicalCase创建/编辑/列表/详情
- 批量删除/启用/禁用操作
- Formula导入/导出功能
- 用户/患者/药材管理

## References

- 用户需求: Server层SRP架构重构，对标Desktop层`refactor-frontend-srp-patterns`标准
- 前序提案: `refactor-frontend-srp-patterns` (Desktop层SRP重构)
- 架构标准: 单一职责原则(SRP)，Controller<400行，Service<600行
