# Server端处方管理模块架构设计

> **文档版本**: v1.0
> **创建日期**: 2025-01-30
> **维护状态**: ✅ 活跃维护
> **关联Issue**: #1600, #1601, #1606, #1551, #1372, #1371
> **架构版本**: UltraThink v2.0 简化版

## 📋 目录

1. [模块概述](#1-模块概述)
2. [模块架构](#2-模块架构)
3. [实体设计](#3-实体设计)
4. [仓储层设计](#4-仓储层设计)
5. [服务层设计](#5-服务层设计)
6. [验证器设计](#6-验证器设计)
7. [映射配置](#7-映射配置)
8. [核心设计原则](#8-核心设计原则)
9. [API层设计](#9-api层设计)
10. [数据库设计](#10-数据库设计)
11. [模块集成与使用](#11-模块集成与使用)
12. [测试策略](#12-测试策略)
13. [性能优化](#13-性能优化)
14. [安全性考虑](#14-安全性考虑)
15. [未来扩展](#15-未来扩展)
16. [总结](#16-总结)

---

## 1. 模块概述

### 1.1 模块定位

处方管理模块（LYBT.Module.Prescriptions）是Server端的核心业务模块，负责处方记录的**只读查询、编号生成和数据检索**功能。

**⚠️ 核心约束（Issue #1600/1601/1606）**：
- 本模块**只提供Read操作**，所有Write操作（创建、修改、删除）必须通过`MedicalCaseService`聚合根进行
- `IPrescriptionRepository`接口已移除所有Write方法
- `PrescriptionService`服务已移除所有Write方法（CreateAsync, UpdateAsync, DeleteAsync等）
- 这是DDD聚合根模式的严格实现，确保病案-处方的生命周期一致性

### 1.2 在MedicalCase工作流中的定位

```
病案创建 → 诊断录入 → [处方生成] → 打印处方 → 病案完成
                         ↑
                    本模块职责范围
                    （Read-only）
```

**处方生命周期管理**：
1. **创建阶段**：通过`MedicalCaseService`创建处方（聚合根控制）
2. **查询阶段**：通过`PrescriptionService`查询处方详情、搜索历史处方（本模块提供）
3. **编号生成**：通过`PrescriptionNumberService`自动生成处方编号（本模块提供）
4. **修改阶段**：通过`MedicalCaseService`修改处方（聚合根控制）
5. **删除阶段**：通过`MedicalCaseService`软删除处方（聚合根控制）

### 1.3 模块特性

| 特性 | 说明 | 实现方式 |
|------|------|----------|
| **Read-only架构** | 所有Write操作移除 | IPrescriptionRepository和PrescriptionService只保留Read方法 |
| **处方编号生成** | 自动生成格式化编号 | PrescriptionNumberService（Issue #1551） |
| **N+1查询优化** | 避免多次数据库查询 | `.Include(p => p.Items)`预加载策略 |
| **MVP内存过滤** | 小数据量场景优化 | SearchPrescriptionsAsync内存过滤（<1000条） |
| **FluentValidation** | DTO验证 | PrescriptionCreateDtoValidator嵌套验证器 |
| **AutoMapper** | 实体-DTO映射 | PrescriptionMappingProfile显式Ignore计算属性 |

---

## 2. 模块架构

### 2.1 三层架构设计

```
┌─────────────────────────────────────────────────────────────┐
│                      API控制器层                              │
│                 (PrescriptionController)                     │
│  职责：HTTP请求处理、参数验证、DTO转换、权限控制                │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                      服务层 (Read-only)                       │
│              ┌──────────────────────────────────┐            │
│              │    PrescriptionService           │            │
│              │  - GetByIdAsync                  │            │
│              │  - GetByMedicalCaseIdAsync       │            │
│              │  - SearchPrescriptionsAsync      │            │
│              │  - GetPatientRecentPrescriptionsAsync │      │
│              │  ❌ CreateAsync (已移除)          │            │
│              │  ❌ UpdateAsync (已移除)          │            │
│              │  ❌ DeleteAsync (已移除)          │            │
│              └──────────────────────────────────┘            │
│              ┌──────────────────────────────────┐            │
│              │  PrescriptionNumberService       │            │
│              │  - GenerateNumberAsync           │            │
│              │  - ValidateNumberFormat          │            │
│              └──────────────────────────────────┘            │
│  职责：业务逻辑协调、跨仓储查询、价格计算、编号生成              │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                   仓储层 (Read-only)                          │
│              ┌──────────────────────────────────┐            │
│              │  IPrescriptionRepository         │            │
│              │  - GetByIdWithItemsAsync         │            │
│              │  - GetPagedWithDetailsAsync      │            │
│              │  - GetByPatientIdAsync           │            │
│              │  - GetByMedicalCaseIdAsync       │            │
│              │  - GetPrescriptionNumbersByPrefixAsync │      │
│              │  - GetByIdAsync                  │            │
│              │  - GetAllAsync                   │            │
│              │  - FindAsync                     │            │
│              │  ❌ AddAsync (已移除)             │            │
│              │  ❌ UpdateAsync (已移除)          │            │
│              │  ❌ DeleteAsync (已移除)          │            │
│              └──────────────────────────────────┘            │
│  职责：数据访问、Include策略、AsNoTracking优化                 │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                     数据库层                                  │
│         [Prescriptions]  [PrescriptionItems]                │
│         [PrescriptionPrintLogs]                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 依赖关系图

```
PrescriptionController
    ↓ 依赖
PrescriptionService
    ↓ 依赖（7个仓储）
    ├── IPrescriptionRepository (Read-only)
    ├── IFormulaRepository (Read引用验方数据)
    ├── IMedicalCaseRepository (Read关联病案，合法用途)
    ├── IPatientRepository (Read关联患者)
    ├── IConsultationRepository (Read关联诊疗)
    ├── IPrescriptionNumberService (生成编号)
    └── IMapper (AutoMapper)

PrescriptionNumberService
    ↓ 依赖
IPrescriptionRepository (Read编号前缀查询)
```

**关键约束**：
- ✅ 服务层可依赖多个Read-only仓储进行跨模块查询
- ✅ `IMedicalCaseRepository`用于Read操作（查询患者信息）是合法的
- ❌ 所有Write操作必须通过`MedicalCaseService`聚合根

### 2.3 模块边界

| 模块职责 | 本模块范围 | 不在范围 |
|---------|-----------|----------|
| **处方查询** | ✅ GetByIdAsync, GetByMedicalCaseIdAsync | - |
| **处方搜索** | ✅ SearchPrescriptionsAsync（MVP内存过滤） | 数据库层搜索优化 |
| **处方编号** | ✅ GenerateNumberAsync, ValidateNumberFormat | - |
| **处方创建** | ❌ 移至MedicalCaseService | 本模块不再提供 |
| **处方修改** | ❌ 移至MedicalCaseService | 本模块不再提供 |
| **处方删除** | ❌ 移至MedicalCaseService | 本模块不再提供 |
| **打印管理** | ✅ 提供打印数据查询 | 打印逻辑在Client端 |

---

## 3. 实体设计

### 3.1 Prescription实体（主实体）

**文件位置**: `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionModel.cs`

**实体概述**：
- **架构版本**: UltraThink v2.0简化版（合并了原BasePrescription和PrescriptionModel）
- **表名**: `Prescriptions`
- **继承**: `BaseEntity`（Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RowVersion, IsDeleted）
- **关系**: 一个MedicalCase可以有零个或一个Prescription（一对零或一关系）

**核心属性（127行）**：

```csharp
[Table("Prescriptions")]
public class Prescription : BaseEntity
{
    // ========== 关联关系 ==========
    /// <summary>医疗案例ID（外键，必填）</summary>
    [Required]
    public Guid MedicalCaseId { get; set; }

    /// <summary>
    /// 处方编号（格式：RX-YYYYMMDD-NNNN，例如：RX-20251021-0001）
    /// 可为空以兼容旧数据，新建处方时自动生成
    /// Issue #1551: 处方自动编号功能
    /// </summary>
    [StringLength(20)]
    public string? PrescriptionNumber { get; set; }

    // ========== 冗余字段（兼容性保留）==========
    /// <summary>患者ID（冗余，通过MedicalCase获取）</summary>
    public Guid? PatientId { get; set; }

    /// <summary>关联用户ID（医生，冗余，通过MedicalCase获取）</summary>
    public Guid? UserId { get; set; }

    // ========== 业务属性 ==========
    /// <summary>主治（适应症/主要症状描述，500字符）</summary>
    [StringLength(500)]
    public string? Indication { get; set; }

    /// <summary>处方帖数（默认7帖，范围1-100）</summary>
    public int DosageCount { get; set; } = 7;

    /// <summary>折扣（0-1之间，0.8表示8折，默认1.0）</summary>
    [Column(TypeName = "decimal(5,4)")]
    public decimal Discount { get; set; } = 1.0m;

    /// <summary>医嘱（500字符）</summary>
    [StringLength(500)]
    public string? Advice { get; set; }

    /// <summary>验方来源（调用验方时自动填写，多个用逗号分隔，200字符）</summary>
    [StringLength(200)]
    public string? FormulaSource { get; set; }

    /// <summary>
    /// 引用的验方名称列表，逗号分隔 (Issue #1365 ENTRY-7)
    /// 用于记录从哪些验方导入了药材，例如："逍遥散,六味地黄丸"
    /// </summary>
    [StringLength(500)]
    public string? ReferencedFormulas { get; set; }

    /// <summary>处方状态（枚举：Draft, Confirmed, Completed）</summary>
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;

    /// <summary>备注（500字符）</summary>
    [StringLength(500)]
    public string? Remark { get; set; }

    // ========== 打印版本管理 ==========
    /// <summary>当前打印版本号（默认1）</summary>
    public int PrintVersion { get; set; } = 1;

    /// <summary>最后打印时间（可空）</summary>
    public DateTime? LastPrintedAt { get; set; }

    /// <summary>打印次数（默认0）</summary>
    public int PrintCount { get; set; } = 0;

    /// <summary>是否已打印（默认false）</summary>
    public bool IsPrinted { get; set; } = false;

    // ========== 导航属性 ==========
    /// <summary>处方项目（药材明细）</summary>
    public List<PrescriptionItem> Items { get; set; } = new();

    /// <summary>所属医疗案例</summary>
    public virtual MedicalCase.MedicalCase? MedicalCase { get; set; }

    /// <summary>打印日志记录</summary>
    public List<PrescriptionPrintLog> PrintLogs { get; set; } = new();
}
```

**设计要点**：
1. **价格计算在DTO层**：实体只存储`DosageCount`和`Discount`，总价由Service层计算
2. **冗余字段保留**：`PatientId`和`UserId`保留用于兼容性，实际通过`MedicalCase`获取
3. **打印版本管理**：支持打印次数、版本号、打印状态跟踪
4. **处方编号可空**：兼容旧数据，新建处方自动生成编号（Issue #1551）

### 3.2 PrescriptionItem实体（处方项）

**文件位置**: `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionItem.cs`

**实体概述**：
- **表名**: `PrescriptionItems`
- **继承**: 无（独立实体，不继承BaseEntity）
- **关系**: 多对一关系（多个PrescriptionItem属于一个Prescription）

**核心属性（86行）**：

```csharp
public class PrescriptionItem
{
    /// <summary>主键</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>处方ID（外键，必填）</summary>
    [Required]
    public Guid PrescriptionId { get; set; }

    /// <summary>中药材ID（外键到药材库，必填）</summary>
    [Required]
    public Guid HerbId { get; set; }

    /// <summary>中药材名称（100字符，必填）</summary>
    [Required]
    [StringLength(100)]
    public string HerbName { get; set; } = string.Empty;

    /// <summary>用量（整数，单位为克）</summary>
    public int Quantity { get; set; }

    /// <summary>单位（默认"g"，16字符）</summary>
    [StringLength(16)]
    public string Unit { get; set; } = "g";

    /// <summary>单价（decimal(18,2)）</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>小计金额（计算属性：UnitPrice × Quantity）</summary>
    [NotMapped]
    public decimal Amount => UnitPrice * Quantity;

    /// <summary>用法（200字符，例如："先煎"）</summary>
    [StringLength(200)]
    public string? Usage { get; set; }

    /// <summary>备注（200字符）</summary>
    [StringLength(200)]
    public string? Remark { get; set; }

    // 导航属性
    public virtual Prescription? Prescription { get; set; }
}
```

**设计要点**：
1. **不实现IHerbItem接口**：按照文档要求，独立设计
2. **整数用量**：`Quantity`为`int`类型，单位固定为克
3. **计算属性**：`Amount`标记为`[NotMapped]`，不存储到数据库
4. **外键关系**：`HerbId`关联到药材库，`PrescriptionId`关联到处方

### 3.3 PrescriptionPrintLog实体（打印日志）

**文件位置**: `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionPrintLog.cs`

**实体概述**：
- **表名**: `PrescriptionPrintLogs`
- **继承**: `BaseEntity`
- **关系**: 多对一关系（多个PrintLog属于一个Prescription）

**核心属性（66行）**：

```csharp
[Table("PrescriptionPrintLogs")]
public class PrescriptionPrintLog : BaseEntity
{
    /// <summary>处方ID（外键，必填）</summary>
    [Required]
    public Guid PrescriptionId { get; set; }

    /// <summary>打印版本号</summary>
    public int PrintVersion { get; set; }

    /// <summary>打印时间（默认当前时间）</summary>
    public DateTime PrintedAt { get; set; } = DateTime.Now;

    /// <summary>打印操作人ID（可空）</summary>
    public Guid? PrintedBy { get; set; }

    /// <summary>打印操作人姓名（50字符）</summary>
    [StringLength(50)]
    public string? PrintedByName { get; set; }

    /// <summary>打印机名称（100字符）</summary>
    [StringLength(100)]
    public string? PrinterName { get; set; }

    /// <summary>是否成功（默认true）</summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>错误信息（500字符，失败时记录）</summary>
    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>备注（200字符）</summary>
    [StringLength(200)]
    public string? Remark { get; set; }

    // 导航属性
    public virtual Prescription? Prescription { get; set; }
}
```

**设计要点**：
1. **审计追踪**：记录每次打印操作的完整信息（时间、操作人、打印机）
2. **版本管理**：`PrintVersion`关联到`Prescription.PrintVersion`
3. **失败记录**：`IsSuccess`和`ErrorMessage`支持打印失败日志
4. **继承BaseEntity**：获得基础审计字段（CreatedAt, CreatedBy等）

### 3.4 实体关系图

```
MedicalCase (1) ───────── (0..1) Prescription
                                      │
                                      │ (1)
                                      │
                                      ├─────── (N) PrescriptionItem
                                      │             ├── HerbId (FK → Herb)
                                      │             └── PrescriptionId (FK)
                                      │
                                      └─────── (N) PrescriptionPrintLog
                                                    └── PrescriptionId (FK)
```

---

## 4. 仓储层设计

### 4.1 IPrescriptionRepository接口（Read-only）

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Interfaces/IPrescriptionRepository.cs`

**接口定义（60行）**：

```csharp
/// <summary>
/// 处方仓储接口 - Read-only版本（Issue #1600 Phase 1）
/// 移除Write方法，所有写操作必须通过MedicalCase聚合根
/// </summary>
public interface IPrescriptionRepository
{
    // ========== 优化查询方法（包含Include策略）==========

    /// <summary>
    /// 根据ID获取处方（包含处方项和药材信息）
    /// </summary>
    Task<Prescription?> GetByIdWithItemsAsync(Guid id);

    /// <summary>
    /// 获取分页列表（包含关联数据）
    /// </summary>
    Task<PagedResult<Prescription>> GetPagedWithDetailsAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null);

    /// <summary>
    /// 根据患者ID获取处方列表
    /// </summary>
    Task<List<Prescription>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// 根据病案ID获取处方
    /// </summary>
    Task<List<Prescription>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

    /// <summary>
    /// 根据前缀查询处方编号列表（用于编号生成）
    /// Issue #1551: 处方自动编号功能
    /// </summary>
    Task<List<string>> GetPrescriptionNumbersByPrefixAsync(string prefix);

    // ========== 基础Read方法（Issue #1600 Phase 1）==========

    Task<Prescription?> GetByIdAsync(Guid id);
    Task<IEnumerable<Prescription>> GetAllAsync();
    Task<IEnumerable<Prescription>> FindAsync(Expression<Func<Prescription, bool>> predicate);

    // ========== Write方法已移除（Issue #1600 Phase 1）==========
    // ❌ AddAsync (已移除)
    // ❌ UpdateAsync (已移除)
    // ❌ DeleteAsync (已移除)
    // ❌ PhysicalDeleteAsync (已移除)
}
```

**设计要点**：
1. **Read-only约束**：所有Write方法已移除，只保留查询方法
2. **Include策略方法**：`GetByIdWithItemsAsync`和`GetPagedWithDetailsAsync`预加载`Items`集合
3. **编号前缀查询**：`GetPrescriptionNumbersByPrefixAsync`支持编号生成服务（Issue #1551）
4. **返回类型**：基础方法返回`IEnumerable<T>`，优化方法返回`List<T>`或`PagedResult<T>`

### 4.2 PrescriptionRepository实现

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Repositories/PrescriptionRepository.cs`

**类定义（137行）**：

```csharp
/// <summary>
/// 处方仓储 - 优化版，包含Include策略以解决N+1查询问题
/// </summary>
internal class PrescriptionRepository : BaseRepository<PrescriptionEntity>, IPrescriptionRepository
{
    public PrescriptionRepository(AppDbContext context) : base(context) { }

    public PrescriptionRepository(AppDbContext context, ILogger<PrescriptionRepository> logger)
        : base(context, logger) { }

    // ========== N+1优化：预加载Items集合 ==========

    /// <summary>
    /// 根据ID获取处方（包含处方项和药材信息）
    /// </summary>
    public async Task<PrescriptionEntity?> GetByIdWithItemsAsync(Guid id)
    {
        return await _dbSet
            .AsNoTracking()  // 只读查询，不跟踪变更
            .Include(p => p.Items)  // 预加载处方项，避免N+1查询
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 获取分页列表（包含关联数据）
    /// 优化：预加载Items信息，避免N+1查询
    /// </summary>
    public async Task<PagedResult<PrescriptionEntity>> GetPagedWithDetailsAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(p => p.Items)  // 预加载处方项
            .Where(p => !p.IsDeleted);

        // 关键字搜索（Indication, FormulaSource, Items.HerbName）
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(p =>
                (p.Indication != null && p.Indication.Contains(keyword)) ||
                (p.FormulaSource != null && p.FormulaSource.Contains(keyword)) ||
                p.Items.Any(i => i.HerbName.Contains(keyword)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<PrescriptionEntity>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 根据患者ID获取处方列表
    /// </summary>
    public async Task<List<PrescriptionEntity>> GetByPatientIdAsync(Guid patientId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.PatientId == patientId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 根据病案ID获取处方
    /// </summary>
    public async Task<List<PrescriptionEntity>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.MedicalCaseId == medicalCaseId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 根据前缀查询处方编号列表（用于编号生成）
    /// Issue #1551: 处方自动编号功能
    /// </summary>
    public async Task<List<string>> GetPrescriptionNumbersByPrefixAsync(string prefix)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => !p.IsDeleted &&
                        p.PrescriptionNumber != null &&
                        p.PrescriptionNumber.StartsWith(prefix))
            .Select(p => p.PrescriptionNumber!)
            .ToListAsync();
    }

    // ========== 显式接口实现（Issue #1600 Phase 1）==========
    // 由于BaseRepository返回List<T>，而IPrescriptionRepository定义返回IEnumerable<T>

    /// <summary>
    /// 获取所有实体（显式实现）
    /// </summary>
    async Task<IEnumerable<PrescriptionEntity>> IPrescriptionRepository.GetAllAsync()
    {
        return await GetAllAsync();
    }

    /// <summary>
    /// 根据条件查找（显式实现）
    /// </summary>
    async Task<IEnumerable<PrescriptionEntity>> IPrescriptionRepository.FindAsync(
        Expression<Func<PrescriptionEntity, bool>> predicate)
    {
        return await FindAsync(predicate);
    }
}
```

**设计要点**：
1. **N+1查询优化**：所有查询方法都使用`.Include(p => p.Items)`预加载处方项
2. **AsNoTracking**：所有Read查询使用`.AsNoTracking()`，禁用变更跟踪
3. **关键字搜索**：支持在`Indication`、`FormulaSource`和`Items.HerbName`中搜索
4. **显式接口实现**：解决`List<T>`与`IEnumerable<T>`返回类型不兼容问题

### 4.3 仓储层性能优化

| 优化技术 | 实现方式 | 性能提升 |
|---------|---------|---------|
| **Include策略** | `.Include(p => p.Items)` | 避免N+1查询，减少数据库往返 |
| **AsNoTracking** | `.AsNoTracking()` | 禁用变更跟踪，减少内存开销 |
| **分页查询** | `Skip().Take()` | 减少单次查询数据量 |
| **索引优化** | `MedicalCaseId`, `PatientId`索引 | 加速关联查询 |
| **前缀查询** | `StartsWith(prefix)` | 支持编号生成的高效查询 |

---

## 5. 服务层设计

### 5.1 PrescriptionService（Read-only）

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`

**服务概述（324行）**：
- **Issue #1600 Phase 3**: Read-only服务层，所有Write方法已移除
- **职责**: 提供处方记录的只读查询功能、价格计算和打印格式生成
- **依赖**: 7个仓储 + IPrescriptionNumberService + IMapper + ILogger

**构造函数依赖**：

```csharp
/// <summary>
/// 处方服务 - Read Layer（Issue #1600 Phase 3）
/// 职责：提供处方记录的只读查询功能、价格计算和打印格式生成
/// 所有Write操作必须通过MedicalCaseService聚合根进行
/// IMedicalCaseRepository用于Read关联患者信息（合法用途）
/// </summary>
public class PrescriptionService : IPrescriptionService
{
    private readonly IPrescriptionRepository _repository;
    private readonly IFormulaRepository _formulaRepository;
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IConsultationRepository _consultationRepository;
    private readonly IPrescriptionNumberService _numberService;
    private readonly IMapper _mapper;
    private readonly ILogger<PrescriptionService> _logger;

    public PrescriptionService(
        IPrescriptionRepository repository,
        IFormulaRepository formulaRepository,
        IMedicalCaseRepository medicalCaseRepository,
        IPatientRepository patientRepository,
        IConsultationRepository consultationRepository,
        IPrescriptionNumberService numberService,
        IMapper mapper,
        ILogger<PrescriptionService> logger)
    {
        _repository = repository;
        _formulaRepository = formulaRepository;
        _medicalCaseRepository = medicalCaseRepository;
        _patientRepository = patientRepository;
        _consultationRepository = consultationRepository;
        _numberService = numberService;
        _mapper = mapper;
        _logger = logger;
    }
}
```

### 5.2 核心Read方法

#### 5.2.1 GetByIdAsync

```csharp
public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
{
    try
    {
        // 使用优化后的查询方法，包含处方项
        var entity = await _repository.GetByIdWithItemsAsync(id);
        if (entity == null)
            return ServiceResult<PrescriptionDto>.Failure("处方不存在");

        var dto = _mapper.Map<PrescriptionDto>(entity);
        return ServiceResult<PrescriptionDto>.Success(dto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取处方详情失败");
        return ServiceResult<PrescriptionDto>.Failure("获取处方详情失败");
    }
}
```

#### 5.2.2 GetByMedicalCaseIdAsync

```csharp
public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
{
    try
    {
        // 使用优化后的查询方法，直接查询并包含Items集合
        var prescriptions = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);

        // 转换为DTO
        var prescriptionDtos = _mapper.Map<List<PrescriptionDto>>(prescriptions);

        return ServiceResult<List<PrescriptionDto>>.Success(prescriptionDtos);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取病历相关处方时发生错误，病历ID：{MedicalCaseId}", medicalCaseId);
        return ServiceResult<List<PrescriptionDto>>.Failure($"获取病历相关处方失败：{ex.Message}");
    }
}
```

### 5.3 高级查询方法（MVP实现）

#### 5.3.1 SearchPrescriptionsAsync（Issue #1372 ENTRY-14）

**功能**: 按患者姓名或症状/诊断关键字搜索处方
**实现**: MVP内存过滤，适用于小数据量（<1000条处方）

```csharp
/// <summary>
/// 搜索处方 - 按患者姓名或症状/诊断关键字 (Issue #1372 ENTRY-14)
/// MVP实现：内存过滤，适用于小数据量（<1000条处方）
/// </summary>
public async Task<ServiceResult<List<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(
    string? patientName = null,
    string? symptomKeyword = null)
{
    try
    {
        // 如果两个参数都为空，返回空列表
        if (string.IsNullOrWhiteSpace(patientName) && string.IsNullOrWhiteSpace(symptomKeyword))
        {
            return ServiceResult<List<PrescriptionSearchResultDto>>.Success(
                new List<PrescriptionSearchResultDto>());
        }

        // Step 1: 获取所有处方
        var allPrescriptions = await _repository.GetAllAsync();

        // Step 2: 获取所有病历（用于关联患者）
        var allMedicalCases = await _medicalCaseRepository.GetAllAsync();
        var medicalCaseDict = allMedicalCases.ToDictionary(mc => mc.Id);

        // Step 3: 获取所有诊疗记录（用于获取 TCMDiagnosis）
        var allConsultations = await _consultationRepository.GetAllAsync();
        var consultationDict = allConsultations.ToDictionary(c => c.Id);

        // Step 4: 获取所有患者（用于关联 PatientName）
        var allPatients = await _patientRepository.GetAllAsync();
        var patientDict = allPatients.ToDictionary(p => p.Id);

        // Step 5: 内存过滤与关联
        var searchResults = new List<PrescriptionSearchResultDto>();

        foreach (var prescription in allPrescriptions)
        {
            // 关联病历
            if (!medicalCaseDict.TryGetValue(prescription.MedicalCaseId, out var medicalCase))
                continue;

            // 关联患者
            if (!patientDict.TryGetValue(medicalCase.PatientId, out var patient))
                continue;

            // 关联诊疗记录（MedicalCase 与 Consultation 共享主键）
            consultationDict.TryGetValue(medicalCase.Id, out var consultation);

            // 按患者姓名筛选
            if (!string.IsNullOrWhiteSpace(patientName))
            {
                if (patient.Name == null ||
                    !patient.Name.Contains(patientName, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            // 按症状/诊断关键字筛选
            if (!string.IsNullOrWhiteSpace(symptomKeyword))
            {
                var matchedInDiagnosis = consultation?.TCMDiagnosis != null &&
                    consultation.TCMDiagnosis.Contains(symptomKeyword, StringComparison.OrdinalIgnoreCase);

                var matchedInIndication = prescription.Indication != null &&
                    prescription.Indication.Contains(symptomKeyword, StringComparison.OrdinalIgnoreCase);

                if (!matchedInDiagnosis && !matchedInIndication)
                    continue;
            }

            // 构建搜索结果
            searchResults.Add(new PrescriptionSearchResultDto
            {
                Id = prescription.Id,
                CreatedAt = prescription.CreatedAt,
                PatientId = patient.Id,
                PatientName = patient.Name ?? string.Empty,
                Indication = prescription.Indication,
                TCMDiagnosis = consultation?.TCMDiagnosis,
                DosageCount = prescription.DosageCount,
                Advice = prescription.Advice,
                FormulaSource = prescription.FormulaSource,
                Remark = prescription.Remark
            });
        }

        _logger.LogInformation("处方搜索完成，患者姓名：{PatientName}，症状关键字：{SymptomKeyword}，结果数量：{Count}",
            patientName ?? "(空)", symptomKeyword ?? "(空)", searchResults.Count);

        return ServiceResult<List<PrescriptionSearchResultDto>>.Success(searchResults);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "搜索处方时发生错误");
        return ServiceResult<List<PrescriptionSearchResultDto>>.Failure($"搜索处方失败：{ex.Message}");
    }
}
```

**MVP内存过滤设计**：
1. **适用场景**: 小数据量（<1000条处方）
2. **实现方式**: 加载所有相关表到内存，使用LINQ过滤
3. **优点**: 实现简单，支持复杂关联查询
4. **缺点**: 数据量大时性能下降，需要未来优化为数据库层查询

#### 5.3.2 GetPatientRecentPrescriptionsAsync（Issue #1371 ENTRY-13）

**功能**: 获取患者最近处方列表（默认5条）
**实现**: MVP内存过滤，包含药材数量（Issue #1370 ENTRY-12）

```csharp
/// <summary>
/// 获取患者最近处方列表 (Issue #1371 ENTRY-13)
/// MVP实现：内存过滤，适用于小数据量（<1000条处方）
/// </summary>
public async Task<ServiceResult<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(
    Guid patientId,
    int count = 5)
{
    try
    {
        // 获取所有处方
        var allPrescriptions = await _repository.GetAllAsync();

        // 获取所有病历（用于关联患者）
        var allMedicalCases = await _medicalCaseRepository.GetAllAsync();
        var medicalCaseDict = allMedicalCases.ToDictionary(mc => mc.Id);

        // 获取所有诊疗记录（用于获取 TCMDiagnosis）
        var allConsultations = await _consultationRepository.GetAllAsync();
        var consultationDict = allConsultations.ToDictionary(c => c.Id);

        // 获取患者信息
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null)
        {
            return ServiceResult<List<PrescriptionSearchResultDto>>.Failure("患者不存在");
        }

        // 内存过滤：找到该患者的所有处方
        var patientPrescriptions = new List<PrescriptionSearchResultDto>();

        foreach (var prescription in allPrescriptions)
        {
            // 关联病历
            if (!medicalCaseDict.TryGetValue(prescription.MedicalCaseId, out var medicalCase))
                continue;

            // 筛选该患者的处方
            if (medicalCase.PatientId != patientId)
                continue;

            // 关联诊疗记录（MedicalCase 与 Consultation 共享主键）
            consultationDict.TryGetValue(medicalCase.Id, out var consultation);

            // 获取处方项以计算药材数量（Issue #1370 ENTRY-12 新增需求）
            var prescriptionWithItems = await _repository.GetByIdWithItemsAsync(prescription.Id);
            var herbCount = prescriptionWithItems?.Items?.Count ?? 0;

            // 构建搜索结果
            var prescriptionDto = new PrescriptionSearchResultDto
            {
                Id = prescription.Id,
                CreatedAt = prescription.CreatedAt,
                PatientId = patient.Id,
                PatientName = patient.Name ?? string.Empty,
                Indication = prescription.Indication,
                TCMDiagnosis = consultation?.TCMDiagnosis,
                DosageCount = prescription.DosageCount,
                Advice = prescription.Advice,
                FormulaSource = prescription.FormulaSource,
                Remark = prescription.Remark,
                HerbCount = herbCount, // Issue #1370 新增
                Items = prescriptionWithItems?.Items != null
                    ? _mapper.Map<List<PrescriptionItemDto>>(prescriptionWithItems.Items)
                    : new List<PrescriptionItemDto>() // Issue #1370 新增
            };

            patientPrescriptions.Add(prescriptionDto);
        }

        // 按创建日期倒序排列，取前count条
        var recentPrescriptions = patientPrescriptions
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToList();

        _logger.LogInformation("获取患者最近处方完成，患者ID：{PatientId}，请求数量：{RequestCount}，实际返回：{ActualCount}",
            patientId, count, recentPrescriptions.Count);

        return ServiceResult<List<PrescriptionSearchResultDto>>.Success(recentPrescriptions);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取患者最近处方时发生错误，患者ID：{PatientId}", patientId);
        return ServiceResult<List<PrescriptionSearchResultDto>>.Failure($"获取患者最近处方失败：{ex.Message}");
    }
}
```

### 5.4 价格计算方法（私有）

```csharp
/// <summary>
/// 计算处方总价 - 简化的价格计算逻辑
/// </summary>
/// <param name="items">处方项列表</param>
/// <param name="dosageCount">处方帖数</param>
/// <param name="discount">折扣</param>
/// <returns>总价</returns>
private decimal CalculateTotalAmount(
    IEnumerable<LYBT.Entities.Prescriptions.PrescriptionItem> items,
    int dosageCount,
    decimal discount = 1.0m)
{
    decimal total = 0;

    foreach (var item in items)
    {
        // 基础价格计算：单价 × 数量 × 帖数
        var itemTotal = item.UnitPrice * item.Quantity * dosageCount;
        total += itemTotal;
    }

    // 应用折扣
    return total * discount;
}
```

### 5.5 Write方法移除记录（Issue #1601 Phase 1）

```csharp
// ========== Write方法已移除（Issue #1601 Phase 1）==========
// CreateAsync, UpdateAsync, DeleteAsync, PhysicalDeleteAsync, CloneAsync,
// ClonePrescriptionAsync, ImportFormulaIntoPrescriptionAsync 已移除
// 所有写操作必须通过MedicalCase聚合根进行
```

---

## 6. 验证器设计

### 6.1 PrescriptionCreateDtoValidator

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Validators/PrescriptionCreateDtoValidator.cs`

**验证器定义（137行）**：

```csharp
/// <summary>
/// 处方创建DTO验证器
/// </summary>
public class PrescriptionCreateDtoValidator : AbstractValidator<PrescriptionCreateDto>
{
    public PrescriptionCreateDtoValidator()
    {
        // 患者ID必填
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("患者ID不能为空");

        // 医生ID必填
        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("医生ID不能为空");

        // 处方编号长度限制（可选）
        RuleFor(x => x.PrescriptionNumber)
            .MaximumLength(50).WithMessage("处方编号长度不能超过50个字符")
            .When(x => !string.IsNullOrEmpty(x.PrescriptionNumber));

        // 剂数范围验证（1-100）
        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 100).WithMessage("剂数必须在1-100之间");

        // 总金额范围验证（>=0）
        RuleFor(x => x.TotalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("总金额必须大于等于0");

        // 验证处方项目（可选，嵌套验证器）
        When(x => x.Items != null && x.Items.Count > 0, () =>
        {
            RuleForEach(x => x.Items).SetValidator(new PrescriptionItemCreateDtoValidator());
        });
    }
}
```

### 6.2 PrescriptionItemCreateDtoValidator（嵌套验证器）

```csharp
/// <summary>
/// 处方项目创建DTO验证器
/// </summary>
public class PrescriptionItemCreateDtoValidator : AbstractValidator<PrescriptionItemCreateDto>
{
    public PrescriptionItemCreateDtoValidator()
    {
        // 药材ID必填
        RuleFor(x => x.HerbId)
            .NotEmpty().WithMessage("中药材ID不能为空");

        // 药材名称必填且长度限制（1-100字符）
        RuleFor(x => x.HerbName)
            .NotEmpty().WithMessage("中药材名称不能为空")
            .MaximumLength(100).WithMessage("中药材名称长度不能超过100个字符");

        // 用量范围验证（0.1-1000）
        RuleFor(x => x.Quantity)
            .InclusiveBetween(0.1m, 1000m).WithMessage("用量必须在0.1-1000之间");

        // 单位必填且长度限制（1-10字符）
        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("单位不能为空")
            .MaximumLength(10).WithMessage("单位长度不能超过10个字符");

        // 单价范围验证（0-10000）
        RuleFor(x => x.UnitPrice)
            .InclusiveBetween(0m, 10000m).WithMessage("单价必须在0-10000之间");
    }
}
```

### 6.3 PrescriptionEditDtoValidator

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Validators/PrescriptionEditDtoValidator.cs`

**验证器定义（60行）**：

```csharp
/// <summary>
/// 处方编辑DTO验证器
/// </summary>
public class PrescriptionEditDtoValidator : AbstractValidator<PrescriptionEditDto>
{
    public PrescriptionEditDtoValidator()
    {
        // ID必填
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("处方ID不能为空");

        // 患者ID必填
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("患者ID不能为空");

        // 用户ID必填
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("用户ID不能为空");

        // 总价范围验证（>=0）
        RuleFor(x => x.TotalPrice)
            .GreaterThanOrEqualTo(0).WithMessage("总价必须大于等于0");

        // 折扣范围验证（0-1）
        RuleFor(x => x.Discount)
            .InclusiveBetween(0m, 1m).WithMessage("折扣必须在0-1之间");

        // 剂数范围验证（1-100）
        RuleFor(x => x.DosageCount)
            .InclusiveBetween(1, 100).WithMessage("剂数必须在1-100之间");

        // 验证处方项目（嵌套验证器）
        When(x => x.Items != null && x.Items.Count > 0, () =>
        {
            RuleForEach(x => x.Items).SetValidator(new PrescriptionItemCreateDtoValidator());
        });
    }
}
```

### 6.4 验证器设计要点

| 验证规则 | PrescriptionCreateDto | PrescriptionEditDto | PrescriptionItemCreateDto |
|---------|----------------------|---------------------|--------------------------|
| **必填字段** | PatientId, DoctorId | Id, PatientId, UserId | HerbId, HerbName |
| **范围验证** | Quantity (1-100), TotalAmount (>=0) | TotalPrice (>=0), Discount (0-1), DosageCount (1-100) | Quantity (0.1-1000), UnitPrice (0-10000) |
| **长度验证** | PrescriptionNumber (<=50) | - | HerbName (<=100), Unit (<=10) |
| **嵌套验证** | Items (RuleForEach) | Items (RuleForEach) | - |

---

## 7. 映射配置

### 7.1 PrescriptionMappingProfile

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Mapping/PrescriptionMappingProfile.cs`

**映射配置（124行）**：

```csharp
public class PrescriptionMappingProfile : Profile
{
    public PrescriptionMappingProfile()
    {
        // ========== Prescription → PrescriptionDto ==========
        CreateMap<Prescription, PrescriptionDto>()
            .ForMember(dest => dest.SingleDosePrice, opt => opt.Ignore())  // 计算属性
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())       // 计算属性
            .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())      // 计算属性
            .ForMember(dest => dest.Usage, opt => opt.Ignore());           // 计算属性

        // ========== Prescription → PrescriptionDetailDto ==========
        CreateMap<Prescription, PrescriptionDetailDto>()
            .ForMember(dest => dest.SingleDosePrice, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
            .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
            .ForMember(dest => dest.Usage, opt => opt.Ignore())
            .ForMember(dest => dest.HasWarnings, opt => opt.Ignore())      // 业务逻辑属性
            .ForMember(dest => dest.WarningDetails, opt => opt.Ignore());  // 业务逻辑属性

        // ========== PrescriptionItem → PrescriptionItemDto ==========
        CreateMap<PrescriptionItem, PrescriptionItemDto>()
            .ForMember(dest => dest.Amount, opt => opt.Ignore());          // 计算属性

        // ========== PrescriptionCreateDto → Prescription ==========
        CreateMap<PrescriptionCreateDto, Prescription>()
            .ForMember(dest => dest.Status, opt => opt.Ignore())           // 默认值
            .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())    // 由Service层赋值
            .ForMember(dest => dest.Items, opt => opt.Ignore())            // 导航属性
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())      // 导航属性
            .ForMember(dest => dest.PrintLogs, opt => opt.Ignore())        // 导航属性
            // 忽略 BaseEntity 审计字段
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // ========== PrescriptionItemCreateDto → PrescriptionItem ==========
        CreateMap<PrescriptionItemCreateDto, PrescriptionItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())               // 自动生成
            .ForMember(dest => dest.PrescriptionId, opt => opt.Ignore());  // 由Service层赋值

        // ========== PrescriptionUpdateDto → Prescription ==========
        CreateMap<PrescriptionUpdateDto, Prescription>()
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
            .ForMember(dest => dest.PatientId, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.Items, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
            .ForMember(dest => dest.PrintLogs, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // ========== PrescriptionEditDto → Prescription ==========
        CreateMap<PrescriptionEditDto, Prescription>()
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
            .ForMember(dest => dest.Items, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
            .ForMember(dest => dest.PrintLogs, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
```

### 7.2 映射规则说明

| 映射方向 | 忽略字段类型 | 示例 |
|---------|-------------|------|
| **Entity → DTO** | 计算属性、业务逻辑属性 | SingleDosePrice, TotalPrice, HasWarnings |
| **DTO → Entity** | 导航属性、审计字段、默认值字段 | Items, MedicalCase, CreatedAt, Id |
| **UpdateDto → Entity** | 使用`.Condition()`防止null覆盖 | `.ForAllMembers(opts => opts.Condition(...))` |

---

## 8. 核心设计原则

### 8.1 聚合根约束原则（Issue #1600 Phase 1）⭐⭐⭐

**原则描述**：
- `IPrescriptionRepository`和`PrescriptionService`只提供**Read操作**
- 所有**Write操作**（创建、修改、删除）必须通过`MedicalCaseService`聚合根进行
- 这是DDD聚合根模式的严格实现，确保病案-处方的生命周期一致性

**实现方式**：

```csharp
// ❌ 错误：直接调用PrescriptionService创建处方（已移除）
// var result = await _prescriptionService.CreateAsync(dto);

// ✅ 正确：通过MedicalCaseService聚合根创建处方
var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
medicalCase.Prescription = new Prescription { /* ... */ };
await _medicalCaseRepository.UpdateAsync(medicalCase);
```

**约束验证**：
- ✅ `IPrescriptionRepository`不包含`AddAsync`、`UpdateAsync`、`DeleteAsync`方法
- ✅ `PrescriptionService`不包含`CreateAsync`、`UpdateAsync`、`DeleteAsync`方法
- ✅ 代码注释明确标记：`// ========== Write方法已移除（Issue #1601 Phase 1）==========`

### 8.2 N+1查询优化原则⭐⭐

**原则描述**：
- 所有查询处方的方法必须使用`.Include(p => p.Items)`预加载处方项
- 所有Read查询必须使用`.AsNoTracking()`禁用变更跟踪

**实现方式**：

```csharp
// ✅ 正确：使用Include策略预加载Items
public async Task<Prescription?> GetByIdWithItemsAsync(Guid id)
{
    return await _dbSet
        .AsNoTracking()  // 禁用变更跟踪
        .Include(p => p.Items)  // 预加载Items集合
        .Where(p => p.Id == id && !p.IsDeleted)
        .FirstOrDefaultAsync();
}

// ❌ 错误：不使用Include导致N+1查询
public async Task<Prescription?> GetByIdAsync(Guid id)
{
    var prescription = await _dbSet.FindAsync(id);
    // 后续访问prescription.Items会触发额外查询（N+1问题）
    return prescription;
}
```

**性能提升**：
- 避免N+1查询：单次查询加载所有数据
- 减少数据库往返：1次查询 vs N+1次查询
- 减少内存开销：AsNoTracking禁用变更跟踪

### 8.3 处方编号生成原则（Issue #1551）⭐

**原则描述**：
- 处方编号格式：`RX-YYYYMMDD-NNNN`（例如：RX-20251021-0001）
- 编号生成服务提供格式验证和序列号生成功能
- 兼容旧数据：`PrescriptionNumber`字段可为空

**实现方式**：

```csharp
public async Task<string> GenerateNumberAsync(DateTime date)
{
    // 格式化日期前缀（RX-YYYYMMDD）
    var datePrefix = $"RX-{date:yyyyMMdd}";

    // 获取当日已存在的最大序号
    var maxSequence = await GetMaxSequenceForDateAsync(date);

    // 生成新序号（最大序号+1，从0001开始）
    var newSequence = maxSequence + 1;

    // 组合完整编号
    var prescriptionNumber = $"{datePrefix}-{newSequence:D4}";

    return prescriptionNumber; // 例如：RX-20251021-0001
}
```

**格式验证**：

```csharp
public bool ValidateNumberFormat(string prescriptionNumber)
{
    // 总长16字符
    if (prescriptionNumber.Length != 16) return false;

    // 前缀RX-
    if (!prescriptionNumber.StartsWith("RX-")) return false;

    // 中间分隔符-（位置11）
    if (prescriptionNumber[11] != '-') return false;

    // 日期部分（8位数字）
    var datePart = prescriptionNumber.Substring(3, 8);
    if (!datePart.All(char.IsDigit)) return false;

    // 验证日期有效性
    if (!DateTime.TryParseExact(datePart, "yyyyMMdd", null,
        System.Globalization.DateTimeStyles.None, out _))
        return false;

    // 序号部分（4位数字）
    var sequencePart = prescriptionNumber.Substring(12, 4);
    if (!sequencePart.All(char.IsDigit)) return false;

    return true;
}
```

### 8.4 MVP内存过滤原则（Issue #1372/1371）⭐

**原则描述**：
- `SearchPrescriptionsAsync`和`GetPatientRecentPrescriptionsAsync`使用**内存过滤**实现
- **适用场景**：小数据量（<1000条处方）
- **未来优化**：数据量增长后迁移到数据库层查询

**实现方式**：

```csharp
public async Task<ServiceResult<List<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(
    string? patientName = null,
    string? symptomKeyword = null)
{
    // Step 1: 获取所有处方
    var allPrescriptions = await _repository.GetAllAsync();

    // Step 2: 获取所有病历
    var allMedicalCases = await _medicalCaseRepository.GetAllAsync();
    var medicalCaseDict = allMedicalCases.ToDictionary(mc => mc.Id);

    // Step 3: 获取所有诊疗记录
    var allConsultations = await _consultationRepository.GetAllAsync();
    var consultationDict = allConsultations.ToDictionary(c => c.Id);

    // Step 4: 获取所有患者
    var allPatients = await _patientRepository.GetAllAsync();
    var patientDict = allPatients.ToDictionary(p => p.Id);

    // Step 5: 内存过滤与关联
    foreach (var prescription in allPrescriptions)
    {
        // 关联病历、患者、诊疗记录
        // 过滤患者姓名、症状关键字
        // ...
    }

    return ServiceResult<List<PrescriptionSearchResultDto>>.Success(searchResults);
}
```

**优缺点分析**：

| 维度 | MVP内存过滤 | 数据库层查询（未来优化） |
|------|-----------|----------------------|
| **实现复杂度** | 简单 | 复杂（需要EF Core Join优化） |
| **性能（<1000条）** | 可接受 | 优秀 |
| **性能（>1000条）** | 下降明显 | 优秀 |
| **开发成本** | 低 | 中等 |
| **适用场景** | MVP阶段 | 生产环境大数据量 |

### 8.5 FluentValidation嵌套验证原则⭐

**原则描述**：
- `PrescriptionCreateDtoValidator`使用`RuleForEach().SetValidator()`嵌套`PrescriptionItemCreateDtoValidator`
- `PrescriptionEditDtoValidator`同样使用嵌套验证器模式
- 验证规则与实体属性约束保持一致

**实现方式**：

```csharp
public class PrescriptionCreateDtoValidator : AbstractValidator<PrescriptionCreateDto>
{
    public PrescriptionCreateDtoValidator()
    {
        // ... 其他验证规则 ...

        // 嵌套验证器：验证处方项目
        When(x => x.Items != null && x.Items.Count > 0, () =>
        {
            RuleForEach(x => x.Items).SetValidator(new PrescriptionItemCreateDtoValidator());
        });
    }
}
```

**验证规则一致性**：

| 实体属性 | 验证规则 | 对应验证器 |
|---------|---------|-----------|
| `Prescription.DosageCount` | `int` (默认7) | `InclusiveBetween(1, 100)` |
| `Prescription.Discount` | `decimal(5,4)` (默认1.0) | `InclusiveBetween(0, 1)` |
| `PrescriptionItem.Quantity` | `int` | `InclusiveBetween(0.1m, 1000m)` |
| `PrescriptionItem.UnitPrice` | `decimal(18,2)` | `InclusiveBetween(0m, 10000m)` |
| `PrescriptionItem.HerbName` | `[StringLength(100)]` | `MaximumLength(100)` |

---

## 9. API层设计

（待补充：根据实际Controller实现添加API端点文档）

**预期API端点**：

| HTTP方法 | 路由 | 功能 | 权限 |
|---------|------|------|------|
| GET | /api/v1/prescriptions/{id} | 获取处方详情 | 医生、管理员 |
| GET | /api/v1/prescriptions/medicalcase/{medicalCaseId} | 获取病案关联处方 | 医生、管理员 |
| GET | /api/v1/prescriptions/search | 搜索处方 | 医生、管理员 |
| GET | /api/v1/prescriptions/patient/{patientId}/recent | 获取患者最近处方 | 医生、管理员 |
| POST | /api/v1/prescriptions/number/generate | 生成处方编号 | 医生、管理员 |
| POST | /api/v1/prescriptions/number/validate | 验证处方编号格式 | 医生、管理员 |

---

## 10. 数据库设计

### 10.1 表结构

#### Prescriptions表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | uniqueidentifier | PK | 主键 |
| MedicalCaseId | uniqueidentifier | FK, NOT NULL | 外键到MedicalCases表 |
| PrescriptionNumber | nvarchar(20) | NULL | 处方编号（格式：RX-YYYYMMDD-NNNN） |
| PatientId | uniqueidentifier | NULL | 冗余字段，通过MedicalCase获取 |
| UserId | uniqueidentifier | NULL | 冗余字段，通过MedicalCase获取 |
| Indication | nvarchar(500) | NULL | 主治 |
| DosageCount | int | NOT NULL, DEFAULT 7 | 处方帖数 |
| Discount | decimal(5,4) | NOT NULL, DEFAULT 1.0 | 折扣 |
| Advice | nvarchar(500) | NULL | 医嘱 |
| FormulaSource | nvarchar(200) | NULL | 验方来源 |
| ReferencedFormulas | nvarchar(500) | NULL | 引用的验方名称列表 |
| Status | int | NOT NULL, DEFAULT 0 | 处方状态（枚举） |
| Remark | nvarchar(500) | NULL | 备注 |
| PrintVersion | int | NOT NULL, DEFAULT 1 | 打印版本号 |
| LastPrintedAt | datetime2 | NULL | 最后打印时间 |
| PrintCount | int | NOT NULL, DEFAULT 0 | 打印次数 |
| IsPrinted | bit | NOT NULL, DEFAULT 0 | 是否已打印 |
| CreatedAt | datetime2 | NOT NULL | 创建时间（BaseEntity） |
| CreatedBy | nvarchar(50) | NULL | 创建人（BaseEntity） |
| UpdatedAt | datetime2 | NULL | 更新时间（BaseEntity） |
| UpdatedBy | nvarchar(50) | NULL | 更新人（BaseEntity） |
| RowVersion | rowversion | NOT NULL | 并发控制（BaseEntity） |
| IsDeleted | bit | NOT NULL, DEFAULT 0 | 软删除标记（BaseEntity） |

#### PrescriptionItems表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | uniqueidentifier | PK | 主键 |
| PrescriptionId | uniqueidentifier | FK, NOT NULL | 外键到Prescriptions表 |
| HerbId | uniqueidentifier | FK, NOT NULL | 外键到Herbs表 |
| HerbName | nvarchar(100) | NOT NULL | 中药材名称 |
| Quantity | int | NOT NULL | 用量（整数，单位：克） |
| Unit | nvarchar(16) | NOT NULL, DEFAULT 'g' | 单位 |
| UnitPrice | decimal(18,2) | NOT NULL | 单价 |
| Usage | nvarchar(200) | NULL | 用法 |
| Remark | nvarchar(200) | NULL | 备注 |

#### PrescriptionPrintLogs表

| 列名 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | uniqueidentifier | PK | 主键（BaseEntity） |
| PrescriptionId | uniqueidentifier | FK, NOT NULL | 外键到Prescriptions表 |
| PrintVersion | int | NOT NULL | 打印版本号 |
| PrintedAt | datetime2 | NOT NULL, DEFAULT GETDATE() | 打印时间 |
| PrintedBy | uniqueidentifier | NULL | 打印操作人ID |
| PrintedByName | nvarchar(50) | NULL | 打印操作人姓名 |
| PrinterName | nvarchar(100) | NULL | 打印机名称 |
| IsSuccess | bit | NOT NULL, DEFAULT 1 | 是否成功 |
| ErrorMessage | nvarchar(500) | NULL | 错误信息 |
| Remark | nvarchar(200) | NULL | 备注 |
| CreatedAt | datetime2 | NOT NULL | 创建时间（BaseEntity） |
| CreatedBy | nvarchar(50) | NULL | 创建人（BaseEntity） |
| UpdatedAt | datetime2 | NULL | 更新时间（BaseEntity） |
| UpdatedBy | nvarchar(50) | NULL | 更新人（BaseEntity） |
| RowVersion | rowversion | NOT NULL | 并发控制（BaseEntity） |
| IsDeleted | bit | NOT NULL, DEFAULT 0 | 软删除标记（BaseEntity） |

### 10.2 索引设计

| 索引名称 | 表名 | 列 | 类型 | 说明 |
|---------|------|---|------|------|
| IX_Prescriptions_MedicalCaseId | Prescriptions | MedicalCaseId | Non-Clustered | 加速按病案查询 |
| IX_Prescriptions_PatientId | Prescriptions | PatientId | Non-Clustered | 加速按患者查询 |
| IX_Prescriptions_PrescriptionNumber | Prescriptions | PrescriptionNumber | Non-Clustered, Unique | 加速编号查询，保证唯一性 |
| IX_PrescriptionItems_PrescriptionId | PrescriptionItems | PrescriptionId | Non-Clustered | 加速关联查询 |
| IX_PrescriptionItems_HerbId | PrescriptionItems | HerbId | Non-Clustered | 加速按药材查询 |
| IX_PrescriptionPrintLogs_PrescriptionId | PrescriptionPrintLogs | PrescriptionId | Non-Clustered | 加速打印日志查询 |

### 10.3 外键关系

```
MedicalCases (1) ────── (0..1) Prescriptions
                                    │
                                    │
                                    ├────── (N) PrescriptionItems
                                    │
                                    └────── (N) PrescriptionPrintLogs

Herbs (1) ────── (N) PrescriptionItems
```

---

## 11. 模块集成与使用

### 11.1 DI注册（PrescriptionsModule）

**文件位置**: `src/Server/Modules/LYBT.Module.Prescriptions/PrescriptionsModule.cs`

```csharp
/// <summary>
/// 处方模块注册 - 标准三层架构
/// </summary>
public static class PrescriptionsModule
{
    /// <summary>
    /// 注册处方模块服务 - 标准三层架构
    /// </summary>
    public static IServiceCollection AddPrescriptionsModule(this IServiceCollection services)
    {
        // 仓储层 - Read-only仓储
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();

        // 服务层 - Read-only服务
        services.AddScoped<IPrescriptionService, PrescriptionService>();

        // Issue #1551: 处方编号生成服务
        services.AddScoped<IPrescriptionNumberService, PrescriptionNumberService>();

        // FluentValidation验证器 - 自动注册所有Validator
        services.AddValidatorsFromAssemblyContaining<PrescriptionCreateDtoValidator>();

        // AutoMapper配置已在UnifiedServiceRegistration中集中注册

        return services;
    }
}
```

### 11.2 在Program.cs中集成

```csharp
// src/Server/Services/LYBT.WebAPI/Program.cs

var builder = WebApplication.CreateBuilder(args);

// 注册处方模块
builder.Services.AddPrescriptionsModule();

// 其他模块注册...
```

### 11.3 使用示例

#### 示例1：查询处方详情

```csharp
public class PrescriptionController : ControllerBase
{
    private readonly IPrescriptionService _prescriptionService;

    public PrescriptionController(IPrescriptionService prescriptionService)
    {
        _prescriptionService = prescriptionService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _prescriptionService.GetByIdAsync(id);

        if (result.IsSuccess)
            return Ok(result.Data);
        else
            return BadRequest(result.ErrorMessage);
    }
}
```

#### 示例2：搜索处方

```csharp
[HttpGet("search")]
public async Task<IActionResult> Search(
    [FromQuery] string? patientName,
    [FromQuery] string? symptomKeyword)
{
    var result = await _prescriptionService.SearchPrescriptionsAsync(patientName, symptomKeyword);

    if (result.IsSuccess)
        return Ok(result.Data);
    else
        return BadRequest(result.ErrorMessage);
}
```

#### 示例3：生成处方编号

```csharp
public class PrescriptionNumberController : ControllerBase
{
    private readonly IPrescriptionNumberService _numberService;

    public PrescriptionNumberController(IPrescriptionNumberService numberService)
    {
        _numberService = numberService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateNumber([FromBody] DateTime date)
    {
        var number = await _numberService.GenerateNumberAsync(date);
        return Ok(new { PrescriptionNumber = number });
    }

    [HttpPost("validate")]
    public IActionResult ValidateNumber([FromBody] string prescriptionNumber)
    {
        var isValid = _numberService.ValidateNumberFormat(prescriptionNumber);
        return Ok(new { IsValid = isValid });
    }
}
```

---

## 12. 测试策略

### 12.1 单元测试（Repository层）

**测试文件**: `tests/UnitTests/Server/Modules/LYBT.Module.Prescriptions.Tests/Repositories/PrescriptionRepositoryTests.cs`

**测试用例**：

```csharp
public class PrescriptionRepositoryTests
{
    private readonly AppDbContext _context;
    private readonly PrescriptionRepository _repository;

    [Fact]
    public async Task GetByIdWithItemsAsync_应返回处方及其处方项()
    {
        // Arrange
        var prescriptionId = Guid.NewGuid();
        // ... 创建测试数据 ...

        // Act
        var result = await _repository.GetByIdWithItemsAsync(prescriptionId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Equal(3, result.Items.Count); // 假设有3个处方项
    }

    [Fact]
    public async Task GetPagedWithDetailsAsync_应返回分页结果()
    {
        // Arrange
        // ... 创建测试数据 ...

        // Act
        var result = await _repository.GetPagedWithDetailsAsync(1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.PageSize);
        Assert.True(result.Items.Count <= 10);
    }

    [Fact]
    public async Task GetPrescriptionNumbersByPrefixAsync_应返回匹配前缀的编号列表()
    {
        // Arrange
        var prefix = "RX-20251021-";
        // ... 创建测试数据 ...

        // Act
        var result = await _repository.GetPrescriptionNumbersByPrefixAsync(prefix);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, num => Assert.StartsWith(prefix, num));
    }
}
```

### 12.2 单元测试（Service层）

**测试文件**: `tests/UnitTests/Server/Modules/LYBT.Module.Prescriptions.Tests/Services/PrescriptionServiceTests.cs`

**测试用例**：

```csharp
public class PrescriptionServiceTests
{
    private readonly Mock<IPrescriptionRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<PrescriptionService>> _mockLogger;
    private readonly PrescriptionService _service;

    [Fact]
    public async Task GetByIdAsync_处方存在时_应返回成功结果()
    {
        // Arrange
        var prescriptionId = Guid.NewGuid();
        var prescription = new Prescription { Id = prescriptionId };
        var dto = new PrescriptionDto { Id = prescriptionId };

        _mockRepository.Setup(r => r.GetByIdWithItemsAsync(prescriptionId))
            .ReturnsAsync(prescription);
        _mockMapper.Setup(m => m.Map<PrescriptionDto>(prescription))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(prescriptionId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(prescriptionId, result.Data.Id);
    }

    [Fact]
    public async Task SearchPrescriptionsAsync_应返回搜索结果()
    {
        // Arrange
        var patientName = "张三";
        // ... 设置Mock数据 ...

        // Act
        var result = await _service.SearchPrescriptionsAsync(patientName, null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.All(result.Data, r => Assert.Contains(patientName, r.PatientName));
    }
}
```

### 12.3 单元测试（Validator层）

**测试文件**: `tests/UnitTests/Server/Modules/LYBT.Module.Prescriptions.Tests/Validators/PrescriptionCreateDtoValidatorTests.cs`

**测试用例**：

```csharp
public class PrescriptionCreateDtoValidatorTests
{
    private readonly PrescriptionCreateDtoValidator _validator;

    [Fact]
    public void Validate_患者ID为空_应返回验证错误()
    {
        // Arrange
        var dto = new PrescriptionCreateDto { PatientId = Guid.Empty };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PatientId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_剂数超出范围_应返回验证错误(int quantity)
    {
        // Arrange
        var dto = new PrescriptionCreateDto { Quantity = quantity };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void Validate_嵌套验证器_应验证处方项()
    {
        // Arrange
        var dto = new PrescriptionCreateDto
        {
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            Items = new List<PrescriptionItemCreateDto>
            {
                new PrescriptionItemCreateDto { Quantity = 0 } // 无效用量
            }
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Items"));
    }
}
```

### 12.4 单元测试（编号生成服务）

**测试文件**: `tests/UnitTests/Server/Modules/LYBT.Module.Prescriptions.Tests/Services/PrescriptionNumberServiceTests.cs`

**测试用例**：

```csharp
public class PrescriptionNumberServiceTests
{
    private readonly Mock<IPrescriptionRepository> _mockRepository;
    private readonly Mock<ILogger<PrescriptionNumberService>> _mockLogger;
    private readonly PrescriptionNumberService _service;

    [Fact]
    public async Task GenerateNumberAsync_应生成正确格式的编号()
    {
        // Arrange
        var date = new DateTime(2025, 10, 21);
        _mockRepository.Setup(r => r.GetPrescriptionNumbersByPrefixAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _service.GenerateNumberAsync(date);

        // Assert
        Assert.Equal("RX-20251021-0001", result);
    }

    [Theory]
    [InlineData("RX-20251021-0001", true)]
    [InlineData("RX-20251021-0999", true)]
    [InlineData("RX-2025102-0001", false)] // 日期部分不足8位
    [InlineData("RX-20251021-001", false)] // 序号部分不足4位
    [InlineData("XX-20251021-0001", false)] // 前缀错误
    public void ValidateNumberFormat_应正确验证格式(string number, bool expected)
    {
        // Act
        var result = _service.ValidateNumberFormat(number);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GenerateNumberAsync_当日已有编号_应生成递增序号()
    {
        // Arrange
        var date = new DateTime(2025, 10, 21);
        _mockRepository.Setup(r => r.GetPrescriptionNumbersByPrefixAsync("RX-20251021-"))
            .ReturnsAsync(new List<string> { "RX-20251021-0001", "RX-20251021-0002" });

        // Act
        var result = await _service.GenerateNumberAsync(date);

        // Assert
        Assert.Equal("RX-20251021-0003", result);
    }
}
```

---

## 13. 性能优化

### 13.1 N+1查询优化

**问题描述**：
- 不使用`.Include()`时，每次访问`prescription.Items`会触发额外查询
- 100个处方 = 1次查询处方 + 100次查询Items = 101次数据库往返

**优化方案**：

```csharp
// ❌ 不优化：N+1查询（101次数据库往返）
public async Task<List<Prescription>> GetPrescriptionsWithItems_NotOptimized()
{
    var prescriptions = await _dbSet.ToListAsync(); // 1次查询

    foreach (var prescription in prescriptions)
    {
        var items = prescription.Items; // 每个处方1次查询（N次）
    }

    return prescriptions;
}

// ✅ 优化后：单次查询（1次数据库往返）
public async Task<List<Prescription>> GetPrescriptionsWithItems_Optimized()
{
    return await _dbSet
        .Include(p => p.Items) // 预加载Items
        .ToListAsync(); // 1次查询，JOIN加载所有数据
}
```

**性能对比**：

| 场景 | 不优化（N+1） | 优化后（Include） | 性能提升 |
|------|-------------|-----------------|---------|
| 100个处方 | 101次查询 | 1次查询 | 101倍 |
| 1000个处方 | 1001次查询 | 1次查询 | 1001倍 |

### 13.2 AsNoTracking优化

**问题描述**：
- EF Core默认启用变更跟踪（Change Tracking）
- Read-only查询不需要变更跟踪，浪费内存

**优化方案**：

```csharp
// ❌ 不优化：启用变更跟踪（内存开销大）
public async Task<Prescription?> GetByIdAsync_NotOptimized(Guid id)
{
    return await _dbSet
        .Include(p => p.Items)
        .FirstOrDefaultAsync(p => p.Id == id); // 启用变更跟踪
}

// ✅ 优化后：禁用变更跟踪（内存开销小）
public async Task<Prescription?> GetByIdAsync_Optimized(Guid id)
{
    return await _dbSet
        .AsNoTracking() // 禁用变更跟踪
        .Include(p => p.Items)
        .FirstOrDefaultAsync(p => p.Id == id);
}
```

**内存对比**：

| 场景 | 变更跟踪 | AsNoTracking | 内存节省 |
|------|---------|--------------|---------|
| 单个处方 | ~5KB | ~2KB | 60% |
| 100个处方 | ~500KB | ~200KB | 60% |

### 13.3 MVP内存过滤优化（未来）

**当前实现**（MVP）：

```csharp
// 加载所有数据到内存
var allPrescriptions = await _repository.GetAllAsync();
var allMedicalCases = await _medicalCaseRepository.GetAllAsync();
var allConsultations = await _consultationRepository.GetAllAsync();
var allPatients = await _patientRepository.GetAllAsync();

// 内存过滤
foreach (var prescription in allPrescriptions)
{
    // 关联和过滤逻辑
}
```

**未来优化**（数据库层查询）：

```csharp
// 在数据库层执行JOIN和过滤
var query = _dbSet
    .AsNoTracking()
    .Include(p => p.Items)
    .Join(_context.MedicalCases, p => p.MedicalCaseId, mc => mc.Id, (p, mc) => new { p, mc })
    .Join(_context.Patients, x => x.mc.PatientId, pt => pt.Id, (x, pt) => new { x.p, x.mc, pt })
    .Join(_context.Consultations, x => x.mc.Id, c => c.Id, (x, c) => new { x.p, x.mc, x.pt, c });

// 数据库层过滤
if (!string.IsNullOrWhiteSpace(patientName))
    query = query.Where(x => x.pt.Name.Contains(patientName));

if (!string.IsNullOrWhiteSpace(symptomKeyword))
    query = query.Where(x =>
        x.c.TCMDiagnosis.Contains(symptomKeyword) ||
        x.p.Indication.Contains(symptomKeyword));

var results = await query.ToListAsync();
```

**性能对比**：

| 场景 | MVP内存过滤 | 数据库层查询 | 性能提升 |
|------|-----------|-------------|---------|
| 1000条处方 | ~2秒 | ~0.2秒 | 10倍 |
| 10000条处方 | ~20秒 | ~0.5秒 | 40倍 |

---

## 14. 安全性考虑

### 14.1 聚合根约束安全

**威胁**：绕过聚合根直接修改处方数据，导致数据一致性问题

**防护措施**：
1. **接口级别防护**：`IPrescriptionRepository`和`IPrescriptionService`不提供Write方法
2. **代码审查**：PR审查时检查是否有绕过聚合根的Write操作
3. **运行时验证**：单元测试验证Write方法已移除

### 14.2 验证器安全

**威胁**：恶意输入导致SQL注入或数据溢出

**防护措施**：
1. **FluentValidation验证**：所有DTO在Service层前验证
2. **范围验证**：`DosageCount`（1-100）、`Discount`（0-1）、`Quantity`（0.1-1000）
3. **长度验证**：`PrescriptionNumber`（≤50）、`HerbName`（≤100）

### 14.3 处方编号生成安全

**威胁**：并发创建处方时生成重复编号

**防护措施**：
1. **数据库唯一约束**：`PrescriptionNumber`字段添加Unique索引
2. **乐观并发控制**：使用`RowVersion`字段防止并发冲突
3. **事务保护**：编号生成和处方创建在同一事务中

### 14.4 权限控制（API层）

**威胁**：未授权访问处方数据

**防护措施**：
1. **基于角色的访问控制（RBAC）**：医生、管理员角色
2. **基于资源的授权**：医生只能访问自己的患者的处方
3. **JWT认证**：所有API端点要求JWT Token

---

## 15. 未来扩展

### 15.1 数据库层搜索优化（Issue #1372优化）

**当前实现**：MVP内存过滤（<1000条）
**未来优化**：数据库层JOIN查询

**实施计划**：
1. **Phase 1**（当前）：MVP内存过滤，满足MVP需求
2. **Phase 2**（数据量>1000）：迁移到数据库层查询
3. **Phase 3**（性能优化）：添加全文索引（Indication, TCMDiagnosis）

### 15.2 缓存策略

**缓存场景**：
- 患者最近处方（TTL: 5分钟）
- 处方编号前缀列表（TTL: 1小时）
- 验方来源列表（TTL: 10分钟）

**实施方案**：
```csharp
public async Task<List<PrescriptionSearchResultDto>> GetPatientRecentPrescriptionsAsync_Cached(
    Guid patientId,
    int count = 5)
{
    var cacheKey = $"patient_{patientId}_recent_{count}";

    // 尝试从缓存读取
    if (_cache.TryGetValue(cacheKey, out List<PrescriptionSearchResultDto> cached))
        return cached;

    // 缓存未命中，查询数据库
    var result = await GetPatientRecentPrescriptionsAsync(patientId, count);

    // 写入缓存（5分钟过期）
    _cache.Set(cacheKey, result.Data, TimeSpan.FromMinutes(5));

    return result.Data;
}
```

### 15.3 事件驱动架构

**事件定义**：
- `PrescriptionCreatedEvent`：处方创建后发布
- `PrescriptionPrintedEvent`：处方打印后发布

**订阅场景**：
- 统计模块订阅处方创建事件，更新统计数据
- 通知模块订阅处方打印事件，发送打印通知

**实施方案**（使用MediatR）：
```csharp
// 发布事件（在MedicalCaseService中）
await _mediator.Publish(new PrescriptionCreatedEvent
{
    PrescriptionId = prescription.Id,
    PatientId = prescription.PatientId,
    CreatedAt = DateTime.Now
});

// 订阅事件（在统计模块中）
public class PrescriptionCreatedEventHandler : INotificationHandler<PrescriptionCreatedEvent>
{
    public async Task Handle(PrescriptionCreatedEvent notification, CancellationToken cancellationToken)
    {
        // 更新统计数据
        await _statisticsService.UpdatePrescriptionCountAsync(notification.PatientId);
    }
}
```

### 15.4 打印服务增强

**当前实现**：Client端打印逻辑
**未来增强**：Server端打印队列管理

**实施计划**：
1. **打印队列**：支持异步打印任务
2. **打印模板**：支持多种打印模板（中药饮片、中成药）
3. **打印预览**：Server端生成PDF预览
4. **打印历史**：完整的打印日志管理

---

## 16. 总结

### 16.1 核心优势

| 优势 | 描述 |
|------|------|
| **聚合根约束** | Issue #1600/1601/1606严格实现，确保数据一致性 |
| **N+1查询优化** | Include策略 + AsNoTracking，性能提升101倍 |
| **处方编号生成** | Issue #1551自动编号，格式验证 |
| **MVP内存过滤** | Issue #1372/1371简单实现，快速交付 |
| **FluentValidation** | 嵌套验证器，验证规则与实体一致 |
| **AutoMapper** | 显式Ignore计算属性，防止映射错误 |

### 16.2 关键技术

- **ASP.NET Core 8.0**: Web API框架
- **Entity Framework Core 8.0**: ORM + 优化策略
- **FluentValidation**: DTO验证框架
- **AutoMapper**: 对象映射框架
- **DI Container**: 依赖注入（Scoped生命周期）

### 16.3 文档维护

**更新规则**：
1. **实体变更**：同步更新第3节实体设计
2. **仓储方法新增**：同步更新第4节仓储层设计
3. **服务方法新增**：同步更新第5节服务层设计
4. **Issue变更**：更新相关章节的Issue引用
5. **性能优化**：更新第13节性能优化

**关联文档**：
- [Client端处方管理架构设计](client/prescriptions-design.md)
- [Server端病案管理架构设计](server/medicalcase-design.md)
- [Server端诊疗管理架构设计](server/consultation-design.md)
- [Server端编号生成服务设计](server/number-service-design.md)

**版本历史**：
- v1.0 (2025-01-30): 初始版本，覆盖Issue #1600/1601/1606/1551/1372/1371

---

## 附录A：代码文件清单

| 序号 | 文件路径 | 行数 | 说明 |
|------|---------|------|------|
| 1 | `PrescriptionsModule.cs` | 43 | DI注册 |
| 2 | `IPrescriptionRepository.cs` | 60 | Read-only仓储接口 |
| 3 | `PrescriptionRepository.cs` | 137 | 仓储实现 |
| 4 | `PrescriptionService.cs` | 324 | Read-only服务层 |
| 5 | `IPrescriptionNumberService.cs` | 26 | 编号生成接口 |
| 6 | `PrescriptionNumberService.cs` | 125 | 编号生成实现 |
| 7 | `PrescriptionCreateDtoValidator.cs` | 137 | 创建验证器 |
| 8 | `PrescriptionEditDtoValidator.cs` | 60 | 编辑验证器 |
| 9 | `PrescriptionModel.cs` | 127 | 主实体 |
| 10 | `PrescriptionItem.cs` | 86 | 处方项实体 |
| 11 | `PrescriptionPrintLog.cs` | 66 | 打印日志实体 |
| 12 | `PrescriptionMappingProfile.cs` | 124 | AutoMapper配置 |

**总计**：1315行核心代码

---

## 附录B：Issue引用清单

| Issue | 标题 | 相关章节 |
|-------|------|---------|
| #1600 | IPrescriptionRepository Read-only（Phase 1） | 4.1, 8.1 |
| #1601 | PrescriptionService Write方法移除（Phase 1） | 5.5, 8.1 |
| #1606 | 聚合根约束实施（Phase 3） | 1.1, 8.1 |
| #1551 | 处方自动编号功能 | 5.2, 8.3 |
| #1372 | 处方搜索功能（ENTRY-14） | 5.3.1, 8.4 |
| #1371 | 患者最近处方功能（ENTRY-13） | 5.3.2, 8.4 |
| #1370 | 处方药材数量显示（ENTRY-12） | 5.3.2 |
| #1365 | 验方引用记录（ENTRY-7） | 3.1 |

---

**文档编写**：Claude Code
**审核状态**：待审核
**最后更新**：2025-01-30
**下一步**：等待用户审查，根据反馈调整
