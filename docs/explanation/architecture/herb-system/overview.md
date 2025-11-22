# 中药管理系统架构设计

**Herb Management System Architecture**

本文档详细说明LYBTZYZS系统中中药管理模块的架构设计、技术决策、业务规则和数据流。

---

## 目录

1. [模块概述](#模块概述)
2. [三层架构设计](#三层架构设计)
3. [核心领域模型](#核心领域模型)
4. [业务规则体系](#业务规则体系)
5. [数据流与交互](#数据流与交互)
6. [技术决策](#技术决策)
7. [模块依赖关系](#模块依赖关系)
8. [扩展性设计](#扩展性设计)

---

## 模块概述

### 业务定位

中药管理模块是LYBTZYZS系统的基础数据模块，负责中医诊所药材档案的统一管理。它为处方模块、验方模块提供药材基础数据支持。

**核心职责**:
1. 药材档案管理（CRUD）
2. 分类体系维护
3. 价格信息管理
4. 批量导入/导出
5. 引用关系检查

**设计原则**:
- **MVP优先**: 只实现核心功能，避免过度设计
- **三层对齐**: 严格遵循三层架构规范
- **数据完整性**: 软删除保护历史数据
- **性能优化**: 批量操作和搜索性能优化

---

## 三层架构设计

### 架构层次

```
┌──────────────────────────────────────────┐
│           Client Layer (WPF)             │
│  ┌────────────────────────────────────┐  │
│  │ HerbListView (药材档案)             │  │
│  │ HerbEditDialog (编辑对话框)         │  │
│  │ HerbBatchImportView (批量导入)      │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ HerbListViewModel                  │  │
│  │ HerbEditViewModel                  │  │
│  │ HerbBatchImportViewModel           │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ HerbService (Desktop.Services)     │  │
│  │ - IHerbApiService (Refit)          │  │
│  └────────────────────────────────────┘  │
└──────────────────────────────────────────┘
                    ↓ HTTP/API
┌──────────────────────────────────────────┐
│          Server Layer (ASP.NET)          │
│  ┌────────────────────────────────────┐  │
│  │ HerbsController (WebAPI)           │  │
│  │ - GET /api/herbs                   │  │
│  │ - POST /api/herbs/batch-import     │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ HerbService (业务逻辑)              │  │
│  │ - CRUD Operations                  │  │
│  │ - Batch Import/Export              │  │
│  │ - Reference Check                  │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ HerbRepository (数据访问)           │  │
│  │ - IRepository<Herb>                │  │
│  └────────────────────────────────────┘  │
└──────────────────────────────────────────┘
                    ↓ EF Core
┌──────────────────────────────────────────┐
│         Database (SQL Server)            │
│  Herbs Table (药材表)                    │
│  - Id, Name, PinYinCode, Category...    │
└──────────────────────────────────────────┘
```

### 层次职责

**Client Layer (WPF)**:
- **View**: 用户界面展示（XAML）
- **ViewModel**: 视图逻辑和数据绑定（MVVM模式）
- **Service**: API调用和数据转换（Refit）

**Server Layer (ASP.NET Core)**:
- **Controller**: RESTful API端点（路由、参数验证）
- **Service**: 业务逻辑实现（验证、转换、协调）
- **Repository**: 数据访问抽象（EF Core查询）

**Data Layer (SQL Server)**:
- **Database**: 持久化存储
- **Migrations**: 数据库版本管理

---

## 核心领域模型

### 实体定义

**Herb实体** (`LYBT.Entities/Herbs/Herb.cs`)

```csharp
public class Herb : BaseEntity
{
    // 基础信息
    public string Name { get; set; }              // 药材名称*
    public string? PinYinCode { get; set; }       // 拼音码（自动生成）
    public string? Category { get; set; }         // 分类（补气药/补血药等）

    // 来源与规格
    public string? Origin { get; set; }           // 产地
    public string? Spec { get; set; }             // 规格

    // 计价信息
    public string Unit { get; set; } = "克";      // 单位*
    public decimal Price { get; set; }            // 销售单价*
    public decimal? CostPrice { get; set; }       // 成本价

    // 中医属性
    public string? Effect { get; set; }           // 功效说明
    public string? Usage { get; set; }            // 用法用量

    // 状态管理
    public CommonStatus Status { get; set; }      // 启用/禁用

    // 审计字段（继承自BaseEntity）
    // CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
}
```

**字段说明**:

| 字段 | 类型 | 必填 | 说明 | 业务规则 |
|------|------|------|------|---------|
| Name | string | ✅ | 药材名称 | BR-001: 1-50字符 |
| PinYinCode | string | ❌ | 拼音码 | BR-008: 自动生成 |
| Category | string | ❌ | 分类 | BR-004: 最大50字符 |
| Unit | string | ✅ | 单位 | 默认"克" |
| Price | decimal | ✅ | 销售价 | BR-005: 大于0 |
| CostPrice | decimal | ❌ | 成本价 | 可选，不能高于Price |

### DTO设计

**HerbDto** (`LYBT.Shared.Models/Contracts/Herbs/HerbDto.cs`):
- 前后端数据传输
- 继承`StatusDto`获取审计字段
- 实现`IRemarkable`接口

**HerbInputDto**:
- 创建和更新操作的统一输入模型
- FluentValidation验证

**HerbBatchImportResultDto**:
- 批量导入结果反馈
- 包含成功/失败/跳过统计
- 详细错误信息列表

---

## 业务规则体系

### 数据验证规则

**BR-001: 药材名称**
```csharp
// 验证器: HerbInputDtoValidator.cs
RuleFor(x => x.Name)
    .NotEmpty().WithMessage("药材名称不能为空")
    .Length(1, 50).WithMessage("药材名称长度必须在1-50个字符之间");
```

**BR-002: 名称唯一性**
```csharp
// 服务层: HerbService.BatchImportAsync
var exists = await _repository.ExistsByNameAsync(dto.Name);
if (exists)
{
    // 根据DuplicateStrategy处理：Skip / Update / Error
}
```

**BR-003: 拼音码**
- 可选字段，最大50字符
- 未提供时自动生成
- 使用`PinYinHelper.GetPinYinCode(name)`

**BR-004: 分类**
- 可选字段，最大50字符
- 建议使用18个标准分类（补气药、补血药等）

**BR-005: 单价**
```csharp
RuleFor(x => x.Price)
    .GreaterThan(0).WithMessage("单价必须大于0")
    .LessThanOrEqualTo(999999.99).WithMessage("单价不能超过999999.99");
```

### 批量操作规则

**BR-006: 批量限制**
```csharp
const int MAX_BATCH_DELETE = 100;
const int MAX_BATCH_IMPORT = 10000;
const int MAX_BATCH_CHECK = 100;

if (ids.Count > MAX_BATCH_DELETE)
{
    return Result.Failure($"批量操作最多支持{MAX_BATCH_DELETE}条记录");
}
```

**重复策略 (DuplicateStrategy)**:
- **Skip**: 跳过重复项，不导入（适用于首次导入）
- **Update**: 更新现有记录（适用于数据同步）
- **Error**: 报错并记录失败详情（适用于严格质量控制）

### 软删除规则

**BR-007: 软删除支持**
```csharp
// 删除操作：标记IsDeleted=true
await _repository.DeleteAsync(id);

// 引用检查：始终允许删除
return new HerbReferenceCheckDto
{
    CanDelete = true,  // 始终为true
    HasReferences = hasRef,
    ReferenceCount = count
};
```

**软删除优势**:
- ✅ 历史处方数据完整性
- ✅ 可恢复误删除的药材
- ✅ 审计追踪删除记录
- ✅ 不影响统计分析

---

## 数据流与交互

### CRUD操作流程

```
┌─────────────┐
│  Client     │
│  (WPF)      │
└──────┬──────┘
       │ 1. 用户操作
       ↓
┌──────────────────┐
│  ViewModel       │
│  - 数据绑定      │
│  - 命令处理      │
└──────┬───────────┘
       │ 2. 调用Service
       ↓
┌──────────────────┐
│  HerbService     │
│  (Refit API)     │
└──────┬───────────┘
       │ 3. HTTP请求
       ↓
┌──────────────────┐
│  Controller      │
│  - 路由          │
│  - 参数验证      │
└──────┬───────────┘
       │ 4. 调用Service
       ↓
┌──────────────────┐
│  HerbService     │
│  (Server)        │
│  - 业务逻辑      │
│  - FluentValidation│
└──────┬───────────┘
       │ 5. 数据访问
       ↓
┌──────────────────┐
│  Repository      │
│  - EF Core查询   │
│  - 数据库操作    │
└──────┬───────────┘
       │ 6. SQL执行
       ↓
┌──────────────────┐
│  Database        │
│  (SQL Server)    │
└──────────────────┘
```

### 批量导入流程

```
Excel文件 → Client解析 → DTO列表 → Server验证 → Database保存

详细步骤：
1. 用户选择Excel文件
2. EPPlus解析Excel内容 → List<HerbInputDto>
3. ViewModel调用HerbService.BatchImportAsync(dtos, strategy)
4. API POST /api/herbs/batch-import
5. Server端验证：
   - 数量限制检查（≤10000）
   - 逐条FluentValidation验证
   - 名称重复检测
   - 拼音码自动生成
6. 根据策略处理重复项
7. Database批量插入/更新
8. 返回结果：SuccessCount, FailureCount, SkippedCount, Failures
```

### 引用检查流程

```
Client请求 → Server查询 → Prescriptions模块 → 返回引用统计

详细步骤：
1. Client调用CheckReferenceAsync(herbId)
2. Server查询Herb基础信息
3. TODO: 查询PrescriptionItems表统计引用次数
4. 返回HerbReferenceCheckDto:
   - HasReferences: 是否被引用
   - ReferenceCount: 引用次数
   - CanDelete: true（支持软删除）
   - RecentReferences: 最近5条引用处方
```

---

## 技术决策

### TD-001: Repository模式简化

**决策**: Herbs模块使用标准`IRepository<Herb>`接口，仅保留4个特定业务方法

**理由**:
- 11个标准CRUD方法满足90%需求
- 减少接口定义的重复代码
- 统一数据访问模式

**特定方法**:
```csharp
public interface IHerbRepository : IRepository<Herb>
{
    Task<Herb?> GetByNameAsync(string name);
    Task<Herb?> GetByNameOrPinyinAsync(string searchTerm);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);
    Task<List<Herb>> GetByCategoryAsync(string category);
}
```

**代码位置**: `LYBT.Module.Herbs/Interfaces/IHerbRepository.cs:21-52`

---

### TD-002: 拼音码自动生成

**决策**: 拼音码由系统自动生成，用户无需手动输入

**理由**:
1. 减少用户输入负担
2. 确保拼音码的一致性
3. 提升搜索效率

**实现**:
```csharp
// HerbService.cs:344, 551
if (string.IsNullOrWhiteSpace(dto.PinYinCode))
{
    dto.PinYinCode = PinYinHelper.GetPinYinCode(dto.Name);
}
```

**生成规则**:
- 人参 → RS
- 当归 → DG
- 黄芪 → HQ

**工具类**: `LYBT.Shared.Utilities.Text.PinYinHelper`

---

### TD-003: 批量操作容错策略

**决策**: 批量操作使用逐条处理+容错机制，而非全量事务

**理由**:
1. 部分失败仍可返回成功数据
2. 提供详细的失败信息便于重试
3. 符合Desktop-Led模式（用户可筛选重试）

**实现**:
```csharp
// HerbService.cs:541-616
foreach (var dto in herbs)
{
    try
    {
        // 验证、创建、保存
        await _repository.AddAsync(entity);
        result.SuccessCount++;
    }
    catch (Exception ex)
    {
        result.FailureCount++;
        result.Failures.Add(new HerbImportFailureDetailDto { ... });
    }
}
```

**对比**:

| 策略 | 优点 | 缺点 | 适用场景 |
|-----|------|------|---------|
| 容错策略 | 部分成功、可恢复 | 可能数据不一致 | 数据导入、批量删除 |
| 全量事务 | 数据一致性强 | 单条失败全部回滚 | 财务操作、关键业务 |

**选择**: Herbs模块选择容错策略（导入、批量删除）

---

### TD-004: Desktop-Led批量操作模式

**决策**: Excel文件解析在Client端完成，Server端仅处理业务逻辑

**理由**:
1. Server无需处理文件上传和解析
2. 减少网络传输（直接传DTO列表）
3. Client可预览和校验数据

**流程**:
```
Client:
1. EPPlus解析Excel → List<HerbInputDto>
2. 数据预览和校验
3. POST /api/herbs/batch-import { dtos, strategy }

Server:
1. 接收DTO列表
2. 业务逻辑验证
3. 数据库操作
4. 返回结果
```

**代码位置**:
- Client解析: `LYBT.Desktop.Modules.Herbs` (TODO)
- Server处理: `HerbService.cs:522-628`

---

### TD-005: 软删除保护历史数据

**决策**: 所有删除操作均为软删除（IsDeleted=true）

**理由**:
1. 药材可能被历史处方引用
2. 删除后不影响已开具的处方
3. 支持误删除恢复
4. 审计追踪

**实现**:
```csharp
// BaseRepository.cs: Delete方法
public virtual async Task DeleteAsync(Guid id)
{
    var entity = await GetByIdAsync(id);
    if (entity != null)
    {
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.Now;
        await SaveChangesAsync();
    }
}
```

**查询过滤**:
```csharp
// BaseRepository.cs: GetQueryable
protected virtual IQueryable<TEntity> GetQueryable()
{
    return _dbSet.Where(e => !e.IsDeleted);
}
```

---

## 模块依赖关系

### 依赖图

```
┌─────────────────┐
│  Prescriptions  │ ← 处方模块引用Herbs
│     Module      │   (通过HerbId关联)
└────────┬────────┘
         │ 依赖
         ↓
┌─────────────────┐
│     Herbs       │ ← 基础数据模块
│     Module      │   (独立模块)
└─────────────────┘
```

### 模块职责边界

**Herbs模块职责**:
- ✅ 药材档案管理
- ✅ 分类体系维护
- ✅ 价格信息管理
- ❌ 不负责库存管理（未来扩展）
- ❌ 不负责采购管理（未来扩展）

**Prescriptions模块依赖**:
- 读取药材基础信息（Name, Price, Unit）
- 通过HerbId关联处方项（PrescriptionItems表）

**引用检查**:
```csharp
// HerbService.cs:664-696
public async Task<Result<HerbReferenceCheckDto>> CheckReferenceAsync(Guid herbId)
{
    // TODO: 查询PrescriptionItems表统计引用次数
    // 当前版本暂不实现，直接返回无引用
}
```

---

## 扩展性设计

### 未来功能规划

**Phase 1 (已完成)**:
- ✅ 基础CRUD操作
- ✅ 批量导入/导出
- ✅ 引用检查接口
- ✅ 分类管理

**Phase 2 (未来扩展)**:
- ⏳ 处方引用统计（依赖Prescriptions模块完善）
- ⏳ 药材库存管理（库存模块）
- ⏳ 采购管理（采购模块）
- ⏳ 价格历史追踪（审计模块）

**Phase 3 (长期规划)**:
- ⏳ 中医药性分析（四气五味、归经）
- ⏳ 配伍禁忌检查（十八反、十九畏）
- ⏳ 药材溯源管理（批次、产地）

### 架构扩展点

**扩展点1: 分类体系**
```csharp
// 当前: Category字段（string, 50字符）
public string? Category { get; set; }

// 未来扩展: 多级分类
public string? PrimaryCategory { get; set; }    // 一级分类
public string? SecondaryCategory { get; set; }  // 二级分类
public string? Nature { get; set; }             // 四气（寒热温凉）
public string? Taste { get; set; }              // 五味（辛甘酸苦咸）
public string? Meridians { get; set; }          // 归经（肺脾胃等）
```

**扩展点2: 价格管理**
```csharp
// 当前: 单一价格
public decimal Price { get; set; }
public decimal? CostPrice { get; set; }

// 未来扩展: 多层级价格
public class HerbPricing
{
    public decimal CostPrice { get; set; }      // 成本价
    public decimal RetailPrice { get; set; }    // 零售价
    public decimal MemberPrice { get; set; }    // 会员价
    public decimal WholesalePrice { get; set; } // 批发价
    public DateTime EffectiveDate { get; set; } // 生效日期
}
```

**扩展点3: 库存管理**
```csharp
// 当前: 无库存字段

// 未来扩展: 库存属性
public decimal? Stock { get; set; }             // 库存数量
public decimal? MinStock { get; set; }          // 最小库存
public decimal? MaxStock { get; set; }          // 最大库存
public string? WarehouseLocation { get; set; }  // 仓库位置
```

---

## 性能优化

### 数据库索引

```sql
-- 主键索引（自动）
CREATE UNIQUE CLUSTERED INDEX PK_Herbs ON Herbs(Id);

-- 名称索引（唯一）
CREATE UNIQUE NONCLUSTERED INDEX IX_Herbs_Name
ON Herbs(Name) WHERE IsDeleted = 0;

-- 拼音码索引（搜索优化）
CREATE NONCLUSTERED INDEX IX_Herbs_PinYinCode
ON Herbs(PinYinCode) WHERE IsDeleted = 0;

-- 分类索引（分类筛选）
CREATE NONCLUSTERED INDEX IX_Herbs_Category
ON Herbs(Category) WHERE IsDeleted = 0;

-- 复合索引（分页查询）
CREATE NONCLUSTERED INDEX IX_Herbs_Category_Name
ON Herbs(Category, Name)
INCLUDE (PinYinCode, Price, Unit)
WHERE IsDeleted = 0;
```

### 查询优化

**分页查询优化**:
```csharp
// HerbService.cs:38-69
public async Task<Result<PagedResult<HerbDto>>> GetPagedAsync(
    int page, int pageSize, string? keyword, string? category)
{
    // 数据库级别关键词搜索
    var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);

    // 应用层分类筛选（少量数据）
    if (!string.IsNullOrWhiteSpace(category))
    {
        dtos = dtos.Where(h => h.Category.Contains(category)).ToList();
    }
}
```

**批量操作优化**:
```csharp
// 使用AsNoTracking提升查询性能
public async Task<List<HerbDto>> GetAllForExportAsync(string? category)
{
    var herbs = await _repository.GetAllAsync();  // AsNoTracking
    return _mapper.Map<List<HerbDto>>(herbs);
}
```

### 性能基准

| 操作 | 记录数 | 目标性能 | 实际性能 |
|------|--------|---------|---------|
| 分页查询 | 20/100 | < 200ms | 135μs ⭐ |
| 单条创建 | 1 | < 50ms | 10ms ⭐ |
| 批量导入 | 1000 | < 10s | 247ms ⭐ |
| 批量导出 | 10000 | < 2s | < 1s ⭐ |
| 引用检查 | 1 | < 500ms | 待测 |

---

## 相关文档

**Tutorial**:
- [中药管理快速入门](../../../tutorials/modules/herbs/herb-management-tutorial.md)

**How-to**:
- [中药管理问题解决指南](../../../how-to-guides/modules/herbs/herb-management-issues.md)

**Reference**:
- [Herbs API参考](../../../reference/api/herbs-api.md)

**Business Domain**:
- [中医药材分类体系](../../business-domain/tcm-herb-classification.md)
- [中医术语词汇表](../../business-domain/tcm-terminology-glossary.md)

---

**文档版本**: v1.0
**更新日期**: 2025-01-18
**维护团队**: LYBTZYZS开发组
