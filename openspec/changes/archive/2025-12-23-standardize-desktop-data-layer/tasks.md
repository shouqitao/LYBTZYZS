# Tasks: standardize-desktop-data-layer

## 执行概览

```
Phase 0 (基础设施)     ──→ Phase 1 (Shared层)
         ↓                       ↓
Phase 2 (独立实体)     ←── Phase 3 (聚合根)
         ↓                       ↓
Phase 4 (从属实体)     ──→ Phase 5 (验证测试)
```

---

## Phase 0: 基础设施准备

### Task 0.1: 创建Repository基类和接口
- **位置**: `LYBT.Desktop.Shared/Data/`
- **交付物**:
  - `IRepository<TDetail, TList, TInput>.cs` - 统一Repository接口
  - `RepositoryBase<TDetail, TList, TInput>.cs` - 抽象基类实现
- **验证**: 编译通过

### Task 0.2: 创建DataManager基类和接口
- **位置**: `LYBT.Desktop.Shared/Data/`
- **交付物**:
  - `IDataManager<TDetail>.cs` - DataManager接口
  - `DataManagerBase<TDetail>.cs` - 抽象基类
- **验证**: 编译通过

### Task 0.3: 创建CommandHandler基类
- **位置**: `LYBT.Desktop.Shared/Data/`
- **交付物**:
  - `ICommandHandler<TInput, TResult>.cs` - 命令处理器接口
- **验证**: 编译通过

---

## Phase 1: Shared层DTO规范

### Task 1.1: 审查现有DTO命名
- **范围**: `LYBT.Shared.DataTransfer/`
- **交付物**: DTO命名清单和待修改列表
- **验证**: 生成审查报告

### Task 1.2: 统一DTO命名规范
- **操作**: 重命名不符合规范的DTO
- **命名规则**:
  - `[Entity]ListDto` - 列表响应
  - `[Entity]DetailDto` - 详情响应
  - `[Entity]InputDto` - 创建/更新请求
  - `[Entity]SummaryDto` - 聚合内嵌入
- **验证**: 编译通过，API测试通过

---

## Phase 2: 独立实体模块标准化

### Task 2.1: Patients模块Repository增强
- **位置**: `LYBT.Desktop.Patients/`
- **操作**:
  - 确认`IPatientsRepository`继承统一接口
  - 确认`PatientsRepository`继承`RepositoryBase`
- **验证**: 编译通过

### Task 2.2: Patients模块Models规范化
- **位置**: `LYBT.Desktop.Patients/Models/`
- **操作**:
  - 统一命名为`PatientDetailModel.cs`
  - 创建`Items/PatientItem.cs` (如缺失)
- **验证**: 编译通过

### Task 2.3: Herbs模块Models规范化
- **位置**: `LYBT.Desktop.Herbs/Models/`
- **操作**:
  - 审查现有Models命名
  - 统一为`HerbDetailModel`/`HerbItem`/`HerbViewState`
- **验证**: 编译通过

### Task 2.4: Formula模块Models规范化
- **位置**: `LYBT.Desktop.Formula/Models/`
- **操作**:
  - 审查现有Models命名
  - 统一为`FormulaDetailModel`/`FormulaItem`/`FormulaViewState`
- **验证**: 编译通过

---

## Phase 3: 聚合根模块标准化

### Task 3.1: MedicalCase DataManager验证
- **位置**: `LYBT.Desktop.MedicalCase/Data/`
- **操作**:
  - 确认`IMedicalCaseDataManager`符合规范
  - 确认包含子实体访问方法
- **验证**: 接口契约审查

### Task 3.2: MedicalCase Models规范化
- **位置**: `LYBT.Desktop.MedicalCase/Models/`
- **操作**:
  - 统一命名规范
  - 确认`MedicalCaseDetailModel`/`MedicalCaseItem`
- **验证**: 编译通过

---

## Phase 4: 从属实体模块标准化

### Task 4.1: Consultation CommandHandler创建
- **位置**: `LYBT.Desktop.Consultation/Data/`
- **交付物**:
  - `IConsultationCommandHandler.cs`
  - `ConsultationCommandHandler.cs` - 通过`IMedicalCaseDataManager`
- **验证**: 编译通过

### Task 4.2: Consultation Models规范化
- **位置**: `LYBT.Desktop.Consultation/Models/`
- **操作**:
  - 统一为`ConsultationDetailModel`/`ConsultationItem`
- **验证**: 编译通过

### Task 4.3: Prescriptions CommandHandler创建
- **位置**: `LYBT.Desktop.Prescriptions/Data/`
- **交付物**:
  - `IPrescriptionCommandHandler.cs`
  - `PrescriptionCommandHandler.cs`
- **验证**: 编译通过

### Task 4.4: Prescriptions Models规范化
- **位置**: `LYBT.Desktop.Prescriptions/Models/`
- **操作**:
  - 统一为`PrescriptionDetailModel`/`PrescriptionItem`
- **验证**: 编译通过

---

## Phase 5: 验证与文档

### Task 5.1: 架构测试补充
- **位置**: `tests/UnitTests/Client/`
- **交付物**:
  - Repository继承关系测试
  - DTO命名规范测试
  - Models层结构测试
- **验证**: 所有架构测试通过

### Task 5.2: 完整性验证
- **操作**:
  - 全量编译验证
  - 运行现有单元测试
  - 运行架构测试
- **验证**: 零编译错误，测试全绿

### Task 5.3: 文档更新
- **交付物**:
  - 更新`DESKTOP_ARCHITECTURE_STANDARD.md`
  - 补充模块类型说明
  - 添加代码示例
- **验证**: 文档审查通过

---

## 任务依赖关系

```
Phase 0 ──→ Phase 1 ──→ Phase 2 ┐
                                 ├──→ Phase 5
Phase 0 ──→ Phase 3 ──→ Phase 4 ┘
```

| 任务 | 依赖 | 可并行 |
|-----|------|--------|
| 0.1-0.3 | 无 | 是 |
| 1.1-1.2 | Phase 0 | 否 |
| 2.1-2.4 | Phase 1 | 是 |
| 3.1-3.2 | Phase 0 | 是 |
| 4.1-4.4 | Phase 3 | 是 |
| 5.1-5.3 | Phase 2,4 | 否 |

---

## 验收标准

1. **编译通过**: `dotnet build LYBT.All.sln` 零错误
2. **测试通过**: 所有单元测试和架构测试通过
3. **规范符合**:
   - Repository/DataManager/CommandHandler模式100%统一
   - DTO命名100%符合规范
   - Models层结构100%规范化
4. **文档完整**: 架构文档更新完成

---

## 风险提示

| 风险 | 影响 | 缓解措施 |
|-----|------|---------|
| DTO重命名导致API不兼容 | 高 | Phase 1使用类型别名过渡 |
| CommandHandler引入增加复杂度 | 中 | 提供完整代码示例 |
| 跨模块引用可能遗漏 | 中 | 使用Grep全量搜索 |
