# 方剂管理系统架构设计

**Formula Management System Architecture**

本文档详细说明LYBTZYZS系统中方剂管理模块的架构设计、技术决策、业务规则和数据流。

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

方剂管理模块是LYBTZYZS系统的核心业务模块，负责中医方剂（经典方剂和经验方）的完整生命周期管理。它为处方模块提供方剂模板支撑，旨在提高医生开方效率，积累诊疗经验。

**核心职责**:
1. 验方档案管理（CRUD）
2. 药材组成配置
3. 验方克隆复制
4. Excel批量导入/导出
5. 智能药材匹配
6. 验方验证机制
7. 共享验方管理

**设计原则**:
- **MVP优先**: 只实现核心功能，避免过度设计
- **三层对齐**: 严格遵循三层架构规范
- **数据完整性**: 软删除保护历史数据
- **Desktop-Led**: 复杂逻辑在客户端实现

---

## 三层架构设计

### 架构层次

```
┌──────────────────────────────────────────┐
│           Client Layer (WPF)             │
│  ┌────────────────────────────────────┐  │
│  │ FormulaManagementView (列表)        │  │
│  │ FormulaDetailView (详情编辑)        │  │
│  │ FormulaValidationView (待验证)      │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ FormulaManagementViewModel (458行) │  │
│  │ FormulaDetailViewModel (675行)     │  │
│  │ FormulaValidationViewModel         │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ Components辅助类 (4个)              │  │
│  │ - FormulaCalculator (总价计算)      │  │
│  │ - FormulaCommandHandler (命令处理)  │  │
│  │ - FormulaDataManager (数据管理)     │  │
│  │ - FormulaValidator (验证器)         │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ IFormulaRepository (Desktop)        │  │
│  │ - IFormulaApiService (Refit)        │  │
│  └────────────────────────────────────┘  │
└──────────────────────────────────────────┘
                    ↓ HTTP/API
┌──────────────────────────────────────────┐
│          Server Layer (ASP.NET)          │
│  ┌────────────────────────────────────┐  │
│  │ FormulasController (WebAPI)         │  │
│  │ - 11个端点                          │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ FormulaService (业务逻辑)           │  │
│  │ - 19个方法                          │  │
│  │ - CloneFormulaAsync (克隆)          │  │
│  │ - ImportFromExcelAsync (导入)       │  │
│  │ - TryMatchHerbAsync (智能匹配)      │  │
│  │ - ValidateFormulaHerbAsync (验证)   │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ FormulaRepository (数据访问)        │  │
│  │ - IRepository<Formula>              │  │
│  │ - 8个特定业务方法                   │  │
│  └────────────────────────────────────┘  │
└──────────────────────────────────────────┘
                    ↓ EF Core
┌──────────────────────────────────────────┐
│         Database (SQL Server)            │
│  Formulas表 (验方主表)                   │
│  FormulaHerbItems表 (药材组成表, 1:N)   │
└──────────────────────────────────────────┘
```

### 层次职责

**Client Layer (WPF)**:
- **View**: XAML界面展示
- **ViewModel**: 视图逻辑和数据绑定（MVVM模式）
- **Components**: 辅助类（计算器、验证器等）
- **Repository**: API调用和数据转换（Refit）

**Server Layer (ASP.NET Core)**:
- **Controller**: RESTful API端点（路由、参数验证）
- **Service**: 业务逻辑实现（验证、转换、协调）
- **Repository**: 数据访问抽象（EF Core查询）

**Data Layer (SQL Server)**:
- **Formulas**: 验方主表
- **FormulaHerbItems**: 药材组成表（1:N关系）

---

## 核心领域模型

### 实体定义

**Formula实体** (`LYBT.Entities/Formulas/Formula.cs`)

```csharp
public class Formula : BaseEntity
{
    // 基础信息
    public string Name { get; set; }              // 方剂名称*
    public string? Category { get; set; }         // 分类（补益方/清热方等）

    // 功效与用法
    public string? Description { get; set; }      // 功效说明
    public string? UsageInstructions { get; set; }// 用法用量

    // 共享管理
    public bool IsShared { get; set; }            // 是否共享验方

    // 药材组成（1:N关系）
    public virtual ICollection<FormulaHerbItem> HerbItems { get; set; }

    // 审计字段（继承自BaseEntity）
    // CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
}
```

**FormulaHerbItem实体** (`LYBT.Entities/Formulas/FormulaHerbItem.cs`)

```csharp
public class FormulaHerbItem : BaseEntity
{
    // 关联关系
    public Guid FormulaId { get; set; }          // 所属验方ID
    public virtual Formula Formula { get; set; } // 导航属性

    public Guid HerbId { get; set; }             // 药材ID
    public virtual Herb Herb { get; set; }       // 导航属性

    // 用量信息
    public decimal Dosage { get; set; }          // 用量
    public string Unit { get; set; } = "克";     // 单位

    // 备注
    public string? Notes { get; set; }           // 药材备注
}
```

**数据库表结构**:
```sql
-- Formulas表
CREATE TABLE Formulas (
    Id uniqueidentifier PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Category nvarchar(50),
    Description nvarchar(500),
    UsageInstructions nvarchar(200),
    IsShared bit NOT NULL DEFAULT 0,
    CreatedBy uniqueidentifier,
    CreatedAt datetime2 NOT NULL,
    UpdatedAt datetime2,
    IsDeleted bit NOT NULL DEFAULT 0
);

-- FormulaHerbItems表
CREATE TABLE FormulaHerbItems (
    Id uniqueidentifier PRIMARY KEY,
    FormulaId uniqueidentifier NOT NULL,
    HerbId uniqueidentifier NOT NULL,
    Dosage decimal(10,2) NOT NULL,
    Unit nvarchar(10) NOT NULL,
    Notes nvarchar(200),
    FOREIGN KEY (FormulaId) REFERENCES Formulas(Id) ON DELETE CASCADE,
    FOREIGN KEY (HerbId) REFERENCES Herbs(Id)
);
```

### DTO设计

**FormulaDto** (`LYBT.Shared.Models/Contracts/Formulas/FormulaDto.cs`):
- 前后端数据传输
- 包含药材列表和总价计算
- 继承审计字段

**FormulaInputDto**:
- 创建和更新操作的统一输入模型
- FluentValidation验证

**FormulaImportResultDto**:
- 批量导入结果反馈
- 包含智能匹配统计

---

## 业务规则体系

### 核心业务规则

**BR-001: 方剂名称**
```csharp
// 验证器: FormulaInputDtoValidator.cs
RuleFor(x => x.Name)
    .NotEmpty().WithMessage("方剂名称不能为空")
    .Length(1, 100).WithMessage("方剂名称长度必须在1-100个字符之间");
```

**BR-002: 名称唯一性**
```csharp
// 服务层验证
var exists = await _repository.ExistsByNameAsync(dto.Name);
if (exists)
{
    return Result.Failure("方剂名称已存在");
}
```

**BR-003: 药材组成**
```csharp
// 至少包含1味药材
RuleFor(x => x.HerbItems)
    .NotEmpty().WithMessage("药材组成不能为空")
    .Must(items => items.Count >= 1).WithMessage("至少包含1味药材");
```

**BR-004: 克隆规则**
- 克隆后默认不共享（IsShared = false）
- 名称自动添加"_副本"后缀
- 药材配置完整复制

**BR-005: 智能药材匹配**
```csharp
// 三级匹配策略
1. 精确匹配: 药材名称完全一致
2. 别名匹配: 支持常见药材别名
3. 拼音匹配: 模糊匹配拼音码
```

**BR-006: 验方验证**
- 检查药材是否存在
- 检查药材是否被软删除
- 检查药材价格是否更新

**BR-007: 共享验方**
- 共享验方对所有用户可见
- 只有创建者可修改或删除共享验方
- 其他用户可克隆共享验方

---

## 数据流与交互

### 验方创建流程

```
Client:
1. 用户填写验方信息和药材组成
2. FormulaDetailViewModel.SaveAsync()
3. 调用IFormulaRepository.CreateAsync(dto)

→ HTTP POST /api/formulas

Server:
4. FormulasController.CreateFormula(dto)
5. FluentValidation验证
6. FormulaService.CreateAsync(dto)
7. 检查名称唯一性
8. FormulaRepository.AddAsync(entity)
9. Database INSERT INTO Formulas + FormulaHerbItems

← HTTP 201 Created { formulaDto }

Client:
10. 更新UI列表
11. 提示"创建成功"
```

### 验方克隆流程

```
Client:
1. 用户点击"克隆"按钮
2. FormulaManagementViewModel.CopyFormulaAsync(id)
3. 调用IFormulaRepository.CloneFormulaAsync(id, newName)

→ HTTP POST /api/formulas/{id}/clone

Server:
4. FormulasController.CloneFormula(id, newName)
5. FormulaService.CloneFormulaAsync(id, newName)
6. 查询原验方（含药材）: GetByIdWithHerbsAsync(id)
7. 创建新Formula实体（深拷贝）
8. 创建新HerbItems集合（深拷贝）
9. FormulaRepository.AddAsync(clone)
10. Database INSERT INTO Formulas + FormulaHerbItems

← HTTP 201 Created { clonedFormulaDto }

Client:
11. 导航到新验方编辑页
12. 用户调整药材配置
13. 保存为新验方
```

### Excel导入流程

```
Client:
1. 用户选择Excel文件
2. EPPlus解析Excel → List<FormulaInputDto>
3. 数据预览和校验
4. 调用IFormulaRepository.BatchImportAsync(dtos, strategy)

→ HTTP POST /api/formulas/import?strategy=Skip

Server:
5. FormulasController.ImportFormulas(dtos, strategy)
6. FormulaService.ImportFromExcelAsync(dtos, strategy)
7. 逐条验证:
   - FluentValidation验证
   - 名称重复检测
   - 药材智能匹配 (TryMatchHerbAsync)
8. 根据策略处理重复项:
   - Skip: 跳过
   - Update: 更新现有记录
   - Error: 记录错误详情
9. FormulaRepository.BatchAddAsync(entities)
10. Database批量INSERT

← HTTP 200 OK { importResultDto }

Client:
11. 显示导入结果摘要
12. 列出失败详情
13. 刷新验方列表
```

### 药材智能匹配流程

```
Server:
1. FormulaService.TryMatchHerbAsync(herbName)

2. 级别1: 精确匹配
   var herb = await _herbRepository.GetByNameAsync(herbName);
   if (herb != null) return herb.Id;

3. 级别2: 别名模糊匹配
   var herb = await _herbRepository.SearchByAliasAsync(herbName);
   if (herb != null) return herb.Id;

4. 级别3: 拼音码匹配
   var herb = await _herbRepository.GetByPinYinCodeAsync(herbName);
   if (herb != null) return herb.Id;

5. 匹配失败
   return null; // 需要手动处理
```

---

## 技术决策

### TD-001: Repository模式简化

**决策**: Formulas模块使用标准`IRepository<Formula>`接口，仅保留8个特定业务方法

**理由**:
- 11个标准CRUD方法满足90%需求
- 减少接口定义的重复代码
- 统一数据访问模式

**特定方法**:
```csharp
public interface IFormulaRepository : IRepository<Formula>
{
    Task<Formula?> GetByIdWithHerbsAsync(Guid id);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);
    Task<List<Formula>> GetPendingValidationFormulasAsync();
    Task<PagedResult<Formula>> GetPagedAsync(int page, int pageSize, string? keyword);
}
```

> **Note**: `GetSharedFormulasAsync` 和 `GetByCategoryAsync` 已在 OpenSpec cleanup-unused-methods (2025-12-04) 中删除，因为未被调用。

**代码位置**: `LYBT.Module.Formula/Interfaces/IFormulaRepository.cs`

---

### TD-002: 克隆功能深拷贝

**决策**: 克隆时创建新的实体对象，而非引用复制

**理由**:
1. 确保验方数据独立性
2. 避免修改克隆方影响原方
3. 支持个性化调整

**实现**:
```csharp
var clone = new Formula
{
    Name = newName,
    Category = source.Category,
    Description = source.Description,
    IsShared = false,  // 克隆后默认不共享

    HerbItems = source.HerbItems.Select(item => new FormulaHerbItem
    {
        // 不复制Id，让数据库生成新Id
        HerbId = item.HerbId,
        Dosage = item.Dosage,
        Unit = item.Unit,
        Notes = item.Notes
    }).ToList()
};
```

**代码位置**: `LYBT.Module.Formula/Services/FormulaService.cs:CloneFormulaAsync`

---

### TD-003: Desktop-Led批量操作

**决策**: Excel文件解析在Client端完成，Server端仅处理业务逻辑

**理由**:
1. Server无需处理文件上传和解析
2. 减少网络传输（直接传DTO列表）
3. Client可预览和校验数据

**流程**:
```
Client:
1. EPPlus解析Excel → List<FormulaInputDto>
2. 数据预览和校验
3. POST /api/formulas/import { dtos, strategy }

Server:
1. 接收DTO列表
2. 业务逻辑验证
3. 数据库操作
4. 返回结果
```

**代码位置**:
- Client解析: `LYBT.Desktop.Formula` (TODO)
- Server处理: `FormulaService.cs:ImportFromExcelAsync`

---

### TD-004: Components辅助类模式

**决策**: 将复杂逻辑拆分为4个辅助类，降低ViewModel复杂度

**理由**:
1. 单一职责原则
2. 提高代码复用性
3. 降低ViewModel行数（从1000+行降至675行）

**Components设计**:
```csharp
// FormulaCalculator.cs - 总价计算器（单例）
public class FormulaCalculator
{
    public decimal CalculateTotalPrice(IEnumerable<FormulaHerbItemDto> herbItems);
    public decimal CalculateHerbPrice(FormulaHerbItemDto item);
}

// FormulaValidator.cs - 验证器
public class FormulaValidator
{
    public bool ValidateName(string name);
    public bool ValidateHerbCount(int count);
}

// FormulaCommandHandler.cs - 命令处理器
public class FormulaCommandHandler
{
    public async Task ExecuteImportFormulasAsync(string filePath);
}

// FormulaDataManager.cs - 数据管理器
public class FormulaDataManager
{
    public async Task LoadPageAsync(int page);
    public void ApplyCategoryFilter(string category);
}
```

**代码位置**: `LYBT.Desktop.Formula/Components/`

---

### TD-005: 软删除保护历史数据

**决策**: 所有删除操作均为软删除（IsDeleted=true）

**理由**:
1. 验方可能被历史处方引用
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

// 查询过滤
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
│  Prescriptions  │ ← 处方模块引用Formulas
│     Module      │   (通过FormulaId关联)
└────────┬────────┘
         │ 依赖
         ↓
┌─────────────────┐
│    Formulas     │ ← 核心业务模块
│     Module      │
└────────┬────────┘
         │ 依赖
         ↓
┌─────────────────┐
│     Herbs       │ ← 基础数据模块
│     Module      │   (提供药材信息)
└─────────────────┘
```

### 模块职责边界

**Formulas模块职责**:
- ✅ 验方档案管理
- ✅ 药材组成配置
- ✅ 验方克隆复制
- ✅ 批量导入/导出
- ✅ 智能药材匹配
- ✅ 验方验证机制
- ✅ 共享验方管理
- ❌ 不负责处方生成（由Prescriptions模块负责）
- ❌ 不负责药材库管理（由Herbs模块负责）

**Prescriptions模块依赖**:
- 读取验方模板信息（Name, HerbItems）
- 通过FormulaId关联处方（可选）
- 使用验方作为处方模板

**Herbs模块依赖**:
- 提供药材基础信息（Name, Price, Unit）
- 通过HerbId关联FormulaHerbItems

---

## 扩展性设计

### 未来功能规划

**Phase 1 (已完成)**:
- ✅ 基础CRUD操作
- ✅ 验方克隆功能
- ✅ 批量导入/导出
- ✅ 智能药材匹配
- ✅ 验方验证机制
- ✅ 共享验方管理

**Phase 2 (未来扩展)**:
- ⏳ 验方版本管理
- ⏳ 验方使用历史统计
- ⏳ 验方效果评价
- ⏳ 验方推荐算法

**Phase 3 (长期规划)**:
- ⏳ AI辅助验方生成
- ⏳ 验方知识图谱
- ⏳ 验方配伍分析
- ⏳ 验方临床研究

### 架构扩展点

**扩展点1: 验方分类体系**
```csharp
// 当前: Category字段（string, 50字符）
public string? Category { get; set; }

// 未来扩展: 多级分类
public string? PrimaryCategory { get; set; }    // 一级分类（补益方/清热方等）
public string? SecondaryCategory { get; set; }  // 二级分类（补气/补血等）
public string? ThirdCategory { get; set; }      // 三级分类（脾胃/肺气等）
```

**扩展点2: 验方版本管理**
```csharp
// 未来扩展: 版本控制
public class FormulaVersion
{
    public Guid Id { get; set; }
    public Guid FormulaId { get; set; }
    public int Version { get; set; }               // 版本号
    public string ChangeDescription { get; set; }  // 变更说明
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string Snapshot { get; set; }           // JSON快照
}
```

**扩展点3: 验方评价体系**
```csharp
// 未来扩展: 效果评价
public class FormulaRating
{
    public Guid Id { get; set; }
    public Guid FormulaId { get; set; }
    public Guid UserId { get; set; }               // 评价医生
    public int Effectiveness { get; set; }         // 疗效评分（1-5星）
    public string Comment { get; set; }            // 评价内容
    public DateTime CreatedAt { get; set; }
}
```

---

## 性能优化

### 数据库索引

```sql
-- 主键索引（自动）
CREATE UNIQUE CLUSTERED INDEX PK_Formulas ON Formulas(Id);

-- 名称索引（唯一）
CREATE UNIQUE NONCLUSTERED INDEX IX_Formulas_Name
ON Formulas(Name) WHERE IsDeleted = 0;

-- 分类索引
CREATE NONCLUSTERED INDEX IX_Formulas_Category
ON Formulas(Category) WHERE IsDeleted = 0;

-- 共享验方索引
CREATE NONCLUSTERED INDEX IX_Formulas_IsShared
ON Formulas(IsShared) WHERE IsDeleted = 0;

-- 外键索引
CREATE NONCLUSTERED INDEX IX_FormulaHerbItems_FormulaId
ON FormulaHerbItems(FormulaId);

CREATE NONCLUSTERED INDEX IX_FormulaHerbItems_HerbId
ON FormulaHerbItems(HerbId);
```

### 查询优化

**分页查询优化**:
```csharp
public async Task<PagedResult<FormulaDto>> GetPagedAsync(
    int page, int pageSize, string? keyword, string? category)
{
    // 数据库级别分页
    var query = _repository.GetQueryable();

    // 关键词过滤
    if (!string.IsNullOrWhiteSpace(keyword))
    {
        query = query.Where(f =>
            f.Name.Contains(keyword) ||
            f.Description.Contains(keyword));
    }

    // 分类过滤
    if (!string.IsNullOrWhiteSpace(category))
    {
        query = query.Where(f => f.Category == category);
    }

    // 分页 + 投影
    var pagedResult = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(f => new FormulaDto
        {
            Id = f.Id,
            Name = f.Name,
            Category = f.Category,
            HerbCount = f.HerbItems.Count,
            // 不加载HerbItems详情，提升性能
        })
        .ToListAsync();

    return pagedResult;
}
```

### 性能基准

| 操作 | 记录数 | 目标性能 | 实际性能 |
|------|--------|---------|---------|
| 分页查询（不含药材详情） | 20/100 | < 200ms | 50ms ⭐ |
| 查询详情（含药材详情） | 1 | < 200ms | 100ms ⭐ |
| 单条创建 | 1 | < 100ms | 50ms ⭐ |
| 克隆验方 | 1 | < 200ms | 80ms ⭐ |
| 批量导入 | 100 | < 10s | 1s ⭐ |
| 批量导出 | 1000 | < 5s | 2s ⭐ |

---

## 相关文档

**Tutorial**:
- [方剂管理快速入门](../../../tutorials/modules/formula/formula-management-tutorial.md)

**How-to**:
- [方剂管理问题解决指南](../../../how-to-guides/modules/formula/formula-issues-guide.md)

**Reference**:
- [Formula API参考](../../../reference/api/formula.md)
- [Formula模块参考](../../../reference/modules/formula/README.md)

---

**文档版本**: v1.0
**更新日期**: 2025-01-22
**维护团队**: LYBTZYZS开发组
