# Design: DTO简化重构

## Context

当前项目DTO设计过于复杂，与业务复杂度不匹配。诊所管理系统是典型的CRUD应用，不需要企业级复杂DTO分层。

### 现状问题

| 问题 | 影响 |
|------|------|
| 继承链过深(3-4层) | 阅读一个DTO需追溯多层，增加认知负担 |
| 接口过度抽象(6+接口) | 大多数接口字段从未被多态调用 |
| 单文件多类(20+类/文件) | 难以定位、修改和维护 |
| 变体过多 | PrescriptionDto/DetailDto/InputDto/CreateDto/EditDto/QueryDto...功能重叠 |
| Desktop命名歧义 | Desktop本地Model命名为Dto(如PrescriptionPrintDto)，与Shared层DTO混淆 |

### 目标架构

```
Contracts/
├── Prescriptions/
│   ├── PrescriptionListDto.cs      # 列表视图
│   ├── PrescriptionDetailDto.cs    # 详情视图
│   ├── PrescriptionInputDto.cs     # 创建/编辑
│   └── PrescriptionItemInputDto.cs # 处方项输入
├── Formulas/
│   ├── FormulaListDto.cs
│   ├── FormulaDetailDto.cs
│   ├── FormulaInputDto.cs
│   └── FormulaHerbItemDto.cs
├── Herbs/
│   ├── HerbListDto.cs
│   ├── HerbDetailDto.cs
│   └── HerbInputDto.cs
├── Patients/
│   ├── PatientListDto.cs
│   ├── PatientDetailDto.cs
│   └── PatientInputDto.cs
└── MedicalCases/
    ├── MedicalCaseListDto.cs
    ├── MedicalCaseDetailDto.cs
    └── MedicalCaseInputDto.cs
```

## Goals / Non-Goals

### Goals
- 简化DTO设计，降低认知复杂度
- 一个DTO一个文件，便于维护
- 按模块组织，便于定位
- 扁平化设计，无继承链

### Non-Goals
- 不改变API契约(保持向后兼容)
- 不改变业务逻辑
- 不重构Entity层

## Decisions

### Decision 1: 标准DTO类型

每个实体最多4种DTO类型:

| 类型 | 用途 | 字段范围 |
|------|------|----------|
| ListDto | 列表视图 | Id + 显示必需字段(名称、日期、状态) |
| DetailDto | 详情视图 | 所有可读字段 |
| InputDto | 创建/编辑 | 所有可写字段 + 可选Id |
| ItemInputDto | 子项输入 | 子项的输入字段 |

**Rationale**: Microsoft官方推荐的模式(BookDto + BookDetailDto)，简单且满足所有CRUD需求。

### Decision 1.1: InputDto设计原则

**核心目标**: 防止Over-posting（过度提交）攻击

#### 字段包含规则

| 规则 | 说明 | 示例 |
|------|------|------|
| **只含可写字段** | 用户可以输入/修改的字段 | Name, PhoneNumber, Address |
| **排除系统字段** | 由系统自动管理的字段 | Id, CreatedAt, UpdatedAt, CreatedBy |
| **排除计算字段** | 由服务层计算的字段 | Age (从BirthDate计算) |
| **排除状态字段** | 通过专用API修改的字段 | Status (通过Enable/Disable API) |
| **带验证注解** | 使用DataAnnotation验证 | [Required], [StringLength] |
| **Create/Update合并** | 一个DTO同时用于创建和更新 | PatientInputDto |

#### 字段对比示例

```
Entity字段              InputDto    DetailDto    说明
─────────────────       ────────    ─────────    ─────────────
Id                      ✗           ✓            系统生成
Name                    ✓           ✓            用户输入
BirthDate               ✓           ✓            用户输入
Age                     ✗           ✓            计算字段(Service计算)
Status                  ✗           ✓            专用API修改
CreatedAt               ✗           ✓            系统管理
UpdatedAt               ✗           ✓            系统管理
RowVersion              ✗           ✓            乐观锁
```

**Rationale**: Microsoft官方安全最佳实践，防止恶意用户提交不应被修改的字段。

#### 各模块InputDto合规性分析

| 模块 | 合规状态 | 问题 | 修正措施 |
|------|----------|------|----------|
| **Patient** | ✅ 合规 | - | 最佳实践示例(移除Age字段) |
| **MedicalCase** | ✅ 合规 | - | 最佳实践示例(文档完善) |
| **User** | ⚠️ 部分合规 | 包含Status字段 | 移除Status，通过专用API修改 |
| **Consultation** | ⚠️ 部分合规 | 包含展示字段(PatientName/DoctorName) | 移至DetailDto |
| **Herb** | ⚠️ 部分合规 | 包含Status字段 | 移除Status，通过专用API修改 |
| **Formula** | ❌ 违规 | 继承IRemarkable接口 + Status字段 | 扁平化 + 移除Status |
| **Prescription** | ❌ 违规 | 继承PrescriptionInputBaseDto + IIdentifiable接口 | 扁平化设计 |

#### 修正优先级

1. **P1-立即修正**: Prescription模块(继承链最深)
2. **P2-优先修正**: Formula模块(接口继承)
3. **P3-后续修正**: User/Consultation/Herb模块(Status/展示字段)

### Decision 2: 扁平化设计

移除所有DTO继承:
```csharp
// 旧设计 ❌
public class PrescriptionDto : StatusDto { }
public class StatusDto : TimestampDto { }
public class TimestampDto : BaseDto { }

// 新设计 ✅
public class PrescriptionListDto
{
    public Guid Id { get; set; }
    public string? PrescriptionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    // ... 所有需要的字段直接声明
}
```

**Rationale**: 继承增加了理解成本，但没有带来实际的代码复用价值(大多数DTO字段各不相同)。

### Decision 3: 一文件一类

每个DTO单独一个文件，文件名与类名一致:
```
PrescriptionListDto.cs → public class PrescriptionListDto
PrescriptionDetailDto.cs → public class PrescriptionDetailDto
```

**Rationale**: 便于定位、便于Git diff、便于并行编辑。

### Decision 4: 统一命名规范

| 类型 | 命名模式 | 示例 |
|------|----------|------|
| 列表DTO | `{Entity}ListDto` | PrescriptionListDto |
| 详情DTO | `{Entity}DetailDto` | PrescriptionDetailDto |
| 输入DTO | `{Entity}InputDto` | PrescriptionInputDto |
| 子项DTO | `{Entity}ItemInputDto` | PrescriptionItemInputDto |

### Decision 5: 保留必要接口

保留部分接口用于泛型约束:
- `IIdentifiable<T>` - 用于Repository泛型方法
- 移除其他未使用接口

### Decision 6: Server-Shared-Client三层命名规范

**原则**: 形成从Server到Client一致的命名对应关系，便于代码阅读和维护

#### 三层命名对应表

| 层级 | 列表类型 | 详情类型 | 输入类型 | 子项类型 |
|------|----------|----------|----------|----------|
| **Server Entity** | - | `{Entity}Model` | - | `{Entity}ItemModel` |
| **Shared DTO** | `{Entity}ListDto` | `{Entity}DetailDto` | `{Entity}InputDto` | `{Entity}ItemInputDto` |
| **Desktop Model** | `{Entity}Item` | `{Entity}DetailModel` | (直接使用DTO) | `{Entity}HerbItem`等 |

#### 命名对应规则

```
Server层 (Entity)           Shared层 (DTO)              Desktop层 (Model)
─────────────────           ─────────────────           ─────────────────
PrescriptionModel    →      PrescriptionListDto    →    (直接使用DTO)
PrescriptionModel    →      PrescriptionDetailDto  →    (直接使用DTO)
                            PrescriptionInputDto   →    (直接使用DTO)
                            -                      →    PrescriptionPrintModel (打印专用)

PatientModel         →      PatientListDto         →    PatientItem
PatientModel         →      PatientDetailDto       →    PatientDetailModel
                            PatientInputDto        →    (直接使用DTO)

FormulaModel         →      FormulaListDto         →    FormulaItem
FormulaModel         →      FormulaDetailDto       →    FormulaDetailModel
FormulaHerbItemModel →      FormulaHerbItemDto     →    FormulaHerbItem
```

#### 后缀含义规范

| 后缀 | 层级 | 含义 |
|------|------|------|
| `Model` | Server | 数据库实体 |
| `Dto` | Shared | API数据传输契约 |
| `Item` | Desktop | 列表项UI绑定模型 |
| `DetailModel` | Desktop | 详情编辑UI绑定模型 |
| `PrintModel` | Desktop | 打印视图模型 |
| `ViewState` | Desktop | 视图状态管理 |
| `Context` | Desktop | 共享上下文 |

#### 需修正项

| 当前命名 | 修正后 | 原因 |
|----------|--------|------|
| `PrescriptionPrintDto` | `PrescriptionPrintModel` | Desktop本地模型不用Dto后缀 |
| `PrescriptionItemPrintDto` | `PrescriptionItemPrintModel` | 同上 |

**Rationale**:
- `Dto`后缀专属Shared层，表示API契约
- Desktop本地Model使用`Item`/`Model`后缀，明确区分用途
- 三层命名形成清晰对应，降低认知负担

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|----------|
| 迁移期间代码重复 | 使用Obsolete标记旧DTO，逐步迁移 |
| 遗漏引用导致编译错误 | 每阶段编译验证，增量迁移 |
| Desktop层大量改动 | 优先处理Server层，Desktop层后续迁移 |

## Migration Plan

### Step 1: 创建新DTO(不删除旧DTO)
- 在新文件夹创建简化DTO
- 新旧DTO并存

### Step 2: 迁移Controller/Service
- 更新Server层使用新DTO
- 保持API兼容性

### Step 3: 迁移Desktop层
- 更新ViewModel使用新DTO
- 验证UI功能

### Step 4: 清理旧DTO
- 标记Obsolete
- 确认无引用后删除

### Rollback
- Git revert到迁移前
- 旧DTO保留期间可快速回滚

## Resolved Questions

### Q1: Query/Search DTO处理方式
**决策**: 移除专用QueryDto/SearchDto，使用**方法参数**

**依据**: Microsoft官方ASP.NET Core MVC+EF Core教程推荐方式
```csharp
// 推荐 - 方法参数
public async Task<IActionResult> Index(
    string sortOrder, string searchString, int? pageNumber)

// 参数较多时 - 使用record
public record QueryParams(string? Keyword, int Page = 1, int PageSize = 20);
```

### Q2: Statistics DTO处理方式
**决策**: 保留，但简化为**record**

**理由**: Statistics是聚合计算结果，需要明确API契约
```csharp
public record PrescriptionStatistics(int TotalCount, int TodayCount, decimal TodayAmount);
```

### Q3: Desktop层重构范围
**决策**: Desktop层随Shared层同步更新引用，并**修正命名歧义**

**分析**:
- Desktop层141个文件引用Shared DTO（作为API契约）→ 随Shared层同步更新
- Desktop本地Model设计合理（{Entity}Item, {Entity}DetailModel模式）
- **命名歧义修正**: `PrescriptionPrintDto` → `PrescriptionPrintModel`（避免与Shared层DTO混淆）
- 迁移时使用IDE批量重构工具更新引用
