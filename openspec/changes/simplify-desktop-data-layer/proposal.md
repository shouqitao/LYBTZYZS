# simplify-desktop-data-layer

## 架构师分析摘要

经过对当前Desktop架构的深入分析，发现**层级架构**和**业务设计**两方面都有优化空间。

### 层级架构问题

**实际调用链**：
```
ViewModel → Service → Repository → Refit API → HttpClient
```

**关键发现**：
1. MedicalCaseService有实际业务逻辑（聚合根模式）- 保留
2. 简单Service（如HerbService）仅做日志+错误处理 - 可合并
3. ApiService基础设施完善（缓存、重试、去重）

### 业务设计问题（MedicalCaseService）

**701行代码分析**：

| 区域 | 行数 | 问题 |
|------|------|------|
| 简单CRUD转发 | 110行 | 仅Repository调用+日志，无业务价值 |
| API-based命令 | 130行 | 直接用`_api`绕过Repository，违反单一数据访问点 |
| 深拷贝方法 | 50行 | 手写克隆，应用Mapperly优化 |
| 冗余方法 | 多处 | 3种保存方法可统一为1种（通过CaseStatus区分） |

**核心问题**：
1. **数据访问混乱**：同时注入`_repository`和`_api`
2. **保存方法冗余**：3种保存方法可统一为1种（通过CaseStatus区分）
3. **手写克隆代码**：50行变更检测用克隆代码应改用Mapperly自动生成

### 过期设计清理（MedicalCaseItem）

**背景**：医案现在在单个View中完成，无需分步工作流。

**需要清理的属性**：
```csharp
// MedicalCaseItem.cs 中的过期计算属性
public bool CanStartConsultation => ...;    // ← 删除
public bool CanCreatePrescription => ...;   // ← 删除
public bool CanCompleteMedicalCase => ...;  // ← 删除（如存在）
```

**设计原则**：以Entity实例字段为第一参考，Item类仅保留必要的UI绑定属性。

### 架构决策

**综合层级优化和业务优化**：

1. **简单模块**：合并Service到Repository
2. **复杂模块**：保留Service但精简业务逻辑
3. **统一数据访问**：Service仅通过Repository访问，不直接用Api
4. **统一保存方法**：删除SaveDraftAsync，通过CaseStatus区分保存逻辑
5. **Mapperly优化**：克隆逻辑（变更检测用）改用源生成器自动生成
6. **清理过期设计**：删除分步工作流相关属性

## Why

### 变更动机

**层级问题**：
1. 职责边界不清晰：Service层职责在不同模块间不一致
2. 为本地存储做准备：需要Repository层支持本地/远程数据源切换
3. 异常处理分散：各模块异常处理方式各异

**业务问题**：
1. MedicalCaseService过于臃肿（701行）
2. 数据访问路径不统一（Repository+Api混用）
3. 方法命名混乱，职责重叠

**过期设计**：
1. 分步工作流属性已无用（CanStartConsultation等）
2. 医案在单View中完成，无需分步状态检查

### 发现的问题

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| `HerbService` | 职责冗余 | 仅日志+错误处理 | 合并到Repository |
| `MedicalCaseService` | 数据访问混乱 | 同时用Repository+Api | 仅用Repository |
| `MedicalCaseService` | 方法冗余 | 3种保存方法 | 统一为1种（通过CaseStatus区分） |
| `MedicalCaseService` | 手写克隆 | 50行手写代码（变更检测用） | Mapperly自动生成 |
| `MedicalCaseService` | 简单CRUD | 110行转发代码 | 删除，ViewModel直接用Repository |
| `MedicalCaseItem` | 过期属性 | CanStartConsultation等 | 删除 |

### 影响分析

**架构层面**：
- 简化简单模块的调用链（Herbs: ViewModel→Repository）
- 统一MedicalCaseService的数据访问路径（仅通过Repository）
- 净减少代码量约285行

**代码层面**：
- 删除HerbService类（合并到Repository）
- 精简FormulaService（删除CRUD转发）
- MedicalCaseService精简到~450行
- 新增MedicalCaseCloneMapper（Mapperly）
- MedicalCaseRepository新增5个API方法
- 删除MedicalCaseItem过期属性（CanStartConsultation等）

## What Changes

### Phase 1: MedicalCaseService业务精简

**1.1 统一数据访问**
- 移除`_api`字段，全部通过`_repository`访问
- Repository新增以下API方法封装：

```csharp
// MedicalCaseRepository新增方法（从Service迁移）
Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request);
Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id);
Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request);
Task<ApiResponse> DeleteMedicalCaseAsync(Guid id);
Task<ApiResponse<MedicalCaseDetailDto>> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request);
```

**1.2 删除冗余方法**
```csharp
// 删除简单CRUD转发（ViewModel直接用Repository）
- GetByIdSimpleAsync  → IRepository.GetByIdAsync
- UpdateSimpleAsync   → IRepository.UpdateAsync
- CreateAsync         → IRepository.CreateAsync
- GetPagedAsync       → IRepository.GetPagedAsync
- QueryAsync          → IRepository.QueryAsync
- SearchAsync         → IRepository.SearchAsync
```

**1.3 统一保存方法（通过状态区分）**
```csharp
// 当前：3种保存方法
SaveAsync()            // IDataManager实现，保存当前状态
SaveDraftAsync(Guid)   // 暂存医案
SaveDraftViaApiAsync() // 直接调API

// 优化后：1种保存方法
SaveAsync()            // 保存聚合根状态，Server根据CaseStatus区分处理
                       // CaseStatus=Draft → 暂存逻辑
                       // CaseStatus=Active → 正常保存逻辑

// 删除方法：
- SaveDraftAsync()
- SaveDraftViaApiAsync()
```

**设计原理**：Server端已有逻辑根据`CaseStatus`区分处理，客户端无需维护多个保存入口。

**1.3.1 生命周期方法精简**

统一保存后，以下生命周期方法需要调整：

| 方法 | 当前实现 | 优化后 |
|------|----------|--------|
| `CreateMedicalCaseAsync` | 调用CreateAsync | 保留，职责清晰 |
| `SaveDraftAsync` | 调用SaveDraftViaApiAsync | **删除**，通过SaveAsync+CaseStatus=Draft |
| `CancelMedicalCaseAsync` | 调用CancelMedicalCaseViaApiAsync | 保留，改调Repository |
| `CompleteMedicalCaseAsync` | 调用UpdateStatusAsync | 保留，改调Repository |
| `ResumeDraftAsync` | 调用UpdateStatusAsync | 保留，改调Repository |

**1.4 Mapperly优化克隆（变更检测用）**

**设计目的**：Clone方法用于**变更检测（Dirty Tracking）**：
- 初始化时：`_originalDetail = Clone(_currentDetail)` 保存原始状态
- 用户编辑：`_currentDetail`被修改，`_originalDetail`保持不变
- `HasChanges`：比较两者差异，决定是否启用保存按钮
- 保存后：`_originalDetail = Clone(_currentDetail)` 重置基准

**为什么保留**：聚合根需要追踪变更状态，其他简单CRUD模块不需要。

```csharp
// 新增 MedicalCaseCloneMapper.cs（替代50行手写克隆代码）
[Mapper]
public partial class MedicalCaseCloneMapper
{
    public partial MedicalCaseDetailDto Clone(MedicalCaseDetailDto source);
    public partial ConsultationDetailDto Clone(ConsultationDetailDto source);
    public partial PrescriptionDetailDto Clone(PrescriptionDetailDto source);
}
```

### Phase 2: 清理过期分步设计

**2.1 删除MedicalCaseItem过期属性**
```csharp
// 删除以下计算属性
- CanStartConsultation
- CanCreatePrescription
- CanCompleteMedicalCase（如存在）
```

**2.2 更新依赖这些属性的UI绑定**
- 检查XAML中的绑定引用
- 更新或删除相关触发器/转换器

### Phase 3: 模块Service分类处理

**模块分类分析**：

| 模块 | Service | 方法数 | 业务逻辑 | 处理策略 |
|------|---------|--------|----------|----------|
| Herbs | HerbService | 5 | 无（纯CRUD转发） | **合并到Repository** |
| Formula | FormulaService | 12 | 有（Copy/Print/Validate） | **保留，精简CRUD** |
| Consultation | ConsultationService | 8 | 有（命令模式/UI逻辑） | **保留，职责不变** |
| Users | 无独立Service | - | - | 维持现状 |
| Patients | ExcelParserService | - | 业务逻辑 | 维持现状 |

**3.1 HerbService合并到HerbRepository**
- Repository添加`[REPO]`前缀日志
- 统一错误处理返回格式
- 更新DI注册，ViewModel直接注入IHerbRepository

**3.2 FormulaService精简**
- 删除简单CRUD转发（GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, GetPagedAsync）
- 保留业务方法（CopyFormulaAsync, PrintFormulaAsync, ValidateFormulaHerbAsync等）

**3.3 ConsultationService保留**
- 职责为UI命令协调，不是数据访问层
- 不在本次重构范围内

### Phase 4: 统一异常处理

1. **Repository层异常标准化**
   - 使用现有的`ClientErrorMessageMapper`
   - 异常分类（网络错误、业务错误、系统错误）

2. **统一返回格式**
   ```csharp
   public record OperationResult<T>(bool Success, T? Data, string? Error);
   ```

### Phase 5: 引入缓存策略

**缓存配置**（设计阶段细化TTL）：

| 数据类型 | 缓存策略 | 建议TTL |
|----------|----------|---------|
| 药材分类列表 | 长缓存 | 30分钟 |
| 药材列表（分页） | 中缓存 | 5分钟 |
| 验方分类列表 | 长缓存 | 30分钟 |
| 配置数据 | 长缓存 | 60分钟 |
| 医案详情 | 不缓存 | - |

**实现方式**：
- 扩展现有`ApiService`缓存机制
- Repository层添加`[Cached(TTL)]`特性（可选）

### Phase 6: Repository抽象（为本地存储准备）

**当前版本**：仅定义接口，不实现本地存储。

```csharp
// 接口层次定义
IRepository<T>
  ├─ IRemoteRepository<T>  // 当前实现（Refit）
  └─ ILocalRepository<T>   // 预留接口（当前版本不实现）

// 未来版本：Service负责数据源路由
public class MedicalCaseService
{
    private readonly IRemoteRepository<MedicalCase> _remote;
    private readonly ILocalRepository<MedicalCase> _local;  // 未来添加
    
    // 未来：根据网络状态选择数据源
}
```

**本阶段目标**：
- 定义ILocalRepository<T>接口
- 不实现具体逻辑
- 为下一版本做准备

## Architecture

### 改进前MedicalCaseService结构

```
MedicalCaseService (701行)
├── 属性区 (15行)              ✓ 保留
├── IDataManager实现 (60行)    ✓ 保留
├── 简单CRUD转发 (110行)       ✗ 删除
├── API-based命令 (130行)      ⚡ 改用Repository
├── 聚合根方法 (60行)          ✓ 保留
├── 变更检测 (25行)            ✓ 保留
├── 深拷贝 (50行)              ⚡ Mapperly替代
└── 生命周期管理 (140行)       ✓ 保留
```

### 改进后MedicalCaseService结构

```
MedicalCaseService (~450行)
├── 属性区 (15行)              ✓ 保留
├── IDataManager实现 (60行)    ✓ 保留
├── 聚合根方法 (60行)          ✓ 保留
├── 变更检测 (25行)            ✓ 保留
├── 生命周期管理 (100行)       ✓ 精简（删除SaveDraftAsync）
├── [删除] 简单CRUD转发        ✗ -110行
├── [删除] API-based命令       ✗ -130行（改调Repository）
└── [删除] 深拷贝方法          ✗ -50行（Mapperly替代）
```

### MedicalCaseItem精简

```
MedicalCaseItem (改进前)
├── 核心属性                   ✓ 保留
├── UI绑定属性                 ✓ 保留
├── CanStartConsultation       ✗ 删除（过期）
├── CanCreatePrescription      ✗ 删除（过期）
└── CanCompleteMedicalCase     ✗ 删除（过期，如存在）

MedicalCaseItem (改进后)
├── 核心属性                   ✓ 保留
└── UI绑定属性                 ✓ 保留
```

### 改进后数据流

```
┌────────────────────────────────────────────────────────────┐
│                        ViewModel                            │
└────────────────────────────────────────────────────────────┘
          │                                    │
          ▼ (简单模块)                          ▼ (复杂模块)
┌──────────────────────┐           ┌───────────────────────┐
│    IRepository       │           │       Service         │
│   (日志+错误处理)     │           │  (聚合根/业务逻辑)     │
└──────────────────────┘           │  仅通过Repository访问  │
          │                        └───────────────────────┘
          │                                    │
          ▼                                    ▼
┌────────────────────────────────────────────────────────────┐
│                    Repository (统一数据访问)                 │
│                  (Refit API + 缓存 + 日志)                   │
└────────────────────────────────────────────────────────────┘
```

### 变更影响范围

```
src/Client/Desktop/Modules/
├── LYBT.Desktop.MedicalCase/
│   ├── Services/
│   │   └── MedicalCaseService.cs    # Phase 1: 精简到~500行
│   ├── Mappers/
│   │   └── MedicalCaseCloneMapper.cs # Phase 1: 新增
│   ├── Models/Items/
│   │   └── MedicalCaseItem.cs        # Phase 2: 删除过期属性
│   └── Repositories/
│       └── MedicalCaseRepository.cs  # Phase 1: 新增API方法
├── LYBT.Desktop.Herbs/
│   ├── Services/                     # Phase 3: 删除
│   └── Repositories/                 # Phase 3: 增强
└── 其他模块...                        # Phase 6: 推广
```

## Impact

### 代码变更统计

| 模块 | 变更类型 | 预估行数 |
|------|----------|----------|
| MedicalCaseService | 精简 | -250行（701→~450） |
| MedicalCaseRepository | 新增API方法 | +50行 |
| MedicalCaseCloneMapper | 新增 | +20行 |
| MedicalCaseItem | 删除过期属性 | -15行 |
| HerbService | 删除 | -80行 |
| HerbRepository | 增强 | +30行 |
| FormulaService | 精简CRUD | -40行 |

**净减少**：约 **285行**

### 文件变更

- **文件变更**: 预估 20-25 个文件
- **风险等级**: Medium（分阶段实施）

### 测试要求

| 测试类型 | 覆盖范围 |
|----------|----------|
| 编译验证 | Desktop解决方案0错误0警告 |
| 功能测试 | 医案创建/编辑/暂存/完成/取消 |
| UI验证 | 单View医案流程 |
| DI验证 | ViewModel依赖注入 |
| 回归测试 | 药材管理、验方管理 |

## Risks

| 风险 | 缓解措施 |
|------|----------|
| 删除Service方法可能影响ViewModel | 逐个检查ViewModel依赖，更新注入 |
| Mapperly克隆可能遗漏字段 | 添加单元测试验证克隆完整性 |
| Repository增强导致代码膨胀 | 使用基类封装通用逻辑 |
| 数据访问路径变更可能引入bug | 每阶段编译验证，增量提交 |
| 删除过期属性影响UI绑定 | 全局搜索属性引用，更新相关XAML |

## Success Criteria

1. **代码精简**: MedicalCaseService从701行减少到~450行
2. **统一数据访问**: Service全部通过Repository访问数据
3. **统一保存**: 仅保留1个SaveAsync方法，通过CaseStatus区分逻辑
4. **自动生成**: 变更检测用克隆逻辑使用Mapperly
5. **过期清理**: 删除分步工作流相关属性
6. **编译通过**: 全量编译0错误0警告
7. **功能正常**: 所有业务功能回归测试通过

## References

### 项目代码分析

**MedicalCaseService问题示例**：
```csharp
// 问题：同时注入Repository和Api
private readonly IMedicalCaseRepository _repository;
private readonly IMedicalCaseApi _api;  // ← 应删除

// 问题：直接用Api绕过Repository
var result = await _api.CloseCaseAsync(medicalCaseId);  // ← 应改为Repository
```

**优化方向**：
```csharp
// 优化：仅通过Repository访问
private readonly IMedicalCaseRepository _repository;

// 优化：统一通过Repository
var result = await _repository.CloseCaseAsync(medicalCaseId);
```

### Entity定义参考

**MedicalCase Entity（聚合根）**：
```csharp
public class MedicalCase : BaseEntity
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; }  // 冗余-读优化
    public Guid UserId { get; set; }
    public string DoctorName { get; set; }   // 冗余-读优化
    public string? CaseNumber { get; set; }
    public MedicalCaseStatus CaseStatus { get; set; }
    public bool? NeedsPrescription { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Remark { get; set; }
    public virtual Consultation? Consultation { get; set; }
    public virtual Prescription? Prescription { get; set; }
}
```

**设计说明**：Entity字段精简，DTO/Item冗余字段是有意设计（优化读取和UI绑定）。

### 过期设计示例

**MedicalCaseItem中的过期属性**：
```csharp
// 过期：分步工作流设计，医案现在单View完成
public bool CanStartConsultation => IsActive && !ConsultationId.HasValue;
public bool CanCreatePrescription => IsActive && ConsultationId.HasValue && !PrescriptionId.HasValue;
```
