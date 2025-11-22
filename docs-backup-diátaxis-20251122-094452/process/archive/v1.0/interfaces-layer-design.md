---
**⚠️ 归档文档（v1.0）**

本文档描述的**中心化接口层架构**（LYBT.Server.Interfaces）已于 2025-10-31 废弃。

- **归档原因**：Issue #1729 - 迁移到模块化接口设计
- **新架构**：服务接口现分散在各模块的 `Interfaces/` 文件夹
- **参考文档**：`docs/explanation/architecture/server/README.md`

本文档保留作为历史参考，描述的架构**不再适用**。
---

# Server端接口层架构设计（已归档）

> **版本**: v1.0（已归档）
> **最后更新**: 2025-10-29
> **归档日期**: 2025-10-31
> **维护负责**: Server端架构组

---

## 📋 目录

1. [接口层定位与职责](#1-接口层定位与职责)
2. [服务接口设计体系](#2-服务接口设计体系)
3. [Repository接口模式](#3-repository接口模式)
4. [接口命名规范与约定](#4-接口命名规范与约定)
5. [ServiceResult统一返回封装](#5-serviceresult统一返回封装)
6. [依赖注入注册模式](#6-依赖注入注册模式)
7. [接口扩展策略](#7-接口扩展策略)
8. [最佳实践与反模式](#8-最佳实践与反模式)
9. [设计原则与约束](#9-设计原则与约束)
10. [测试支持（Mock接口）](#10-测试支持mock接口)
11. [参考资料](#11-参考资料)
12. [更新历史](#12-更新历史)

---

## 1. 接口层定位与职责

### 1.1 架构位置

```
LYBT.Server.Interfaces (Server端核心库)
   ↓ 定义契约
LYBT.Module.* (业务模块)  →  LYBT.WebAPI (API Controller)
   ↓ 实现接口                  ↓ 依赖接口
依赖注入容器 (IServiceCollection)
   ↓ 映射关系
运行时绑定 (Controller → 具体Service实现)
```

**核心职责**：
- ✅ **定义契约**：为8个业务模块提供统一的服务接口定义
- ✅ **依赖倒置**：高层模块(Controller)依赖接口而非具体实现(DIP)
- ✅ **解耦设计**：业务逻辑与具体实现完全解耦
- ✅ **测试支持**：通过Mock接口实现单元测试隔离
- ❌ **不包含实现**：仅定义方法签名,不包含任何业务逻辑

### 1.2 依赖关系图

```
┌─────────────────────────────────────────────────────┐
│         LYBT.Server.Interfaces (接口层)              │
│  ┌────────────────────┐  ┌────────────────────┐    │
│  │  Services/         │  │  (无Repository)    │    │
│  │  - IAuthService    │  │  Repository接口在  │    │
│  │  - IPatientService │  │  各模块中定义      │    │
│  │  - IHerbService    │  │                    │    │
│  │  - I...Service     │  │                    │    │
│  └────────────────────┘  └────────────────────┘    │
│           ↓ 仅依赖                                   │
│  ┌────────────────────┐                             │
│  │ LYBT.Shared.Models │ (DTO模型)                   │
│  └────────────────────┘                             │
└─────────────────────────────────────────────────────┘
              ↑ 被依赖
┌─────────────┴──────────────┐
│                             │
│  LYBT.Module.*             │  LYBT.WebAPI
│  (Service实现)             │  (Controller层)
│                             │
│  services.AddScoped<       │  public HerbsController(
│    IHerbService,           │    IHerbService service)
│    HerbService>();         │  { }
└────────────────────────────┘
```

**依赖约束**：
- ✅ 接口层仅依赖 `LYBT.Shared.Models` (DTO模型)
- ✅ 无任何外部NuGet包依赖(保持纯净)
- ✅ 不依赖Infrastructure、Entities、具体模块
- ⚠️ **关键约束**：接口层不能依赖任何具体实现

---

## 2. 服务接口设计体系

### 2.1 8大核心服务接口

| 服务接口 | 方法数 | 核心职责 | 复杂度 |
|---------|-------|---------|-------|
| **IAuthService** | 8 | 认证、授权、Token管理、会话管理 | ⭐⭐⭐ 高 |
| **IPatientService** | 8 | 患者信息CRUD、搜索、Excel导入导出 | ⭐⭐ 中 |
| **IHerbService** | 10 | 中药材CRUD、分类筛选、批量操作、Excel导入导出 | ⭐⭐ 中 |
| **IMedicalCaseService** | 19 | 病案CRUD、聚合根管理、复杂查询、三步工作流 | ⭐⭐⭐⭐ 极高 |
| **IConsultationService** | N/A | 诊断记录管理(通过MedicalCase聚合根管理) | ⭐⭐ 中 |
| **IPrescriptionService** | N/A | 处方管理(通过MedicalCase聚合根管理) | ⭐⭐ 中 |
| **IFormulaService** | N/A | 验方管理CRUD、方剂组成 | ⭐⭐ 中 |
| **IUserService** | N/A | 用户管理CRUD、权限管理 | ⭐⭐ 中 |

**说明**：
- N/A = 待补充完整接口定义文档
- IMedicalCaseService是最复杂的接口(19个方法),实现聚合根模式

### 2.2 IAuthService接口详解(认证服务)

**核心能力**：用户认证、JWT令牌管理、会话控制

```csharp
public interface IAuthService
{
    // ========== 核心认证流程 ==========

    /// <summary>
    /// 用户登录验证(返回JWT Token)
    /// </summary>
    Task<ServiceResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 用户登出(撤销Token)
    /// </summary>
    Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request);

    // ========== Token生命周期管理 ==========

    /// <summary>
    /// 验证用户凭证(用户名+密码)
    /// </summary>
    Task<ServiceResult<string>> VerifyCredentialsAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新Token(使用RefreshToken获取新AccessToken)
    /// </summary>
    Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 验证Token有效性(用于API鉴权)
    /// </summary>
    Task<ServiceResult<bool>> ValidateTokenAsync(string token);

    /// <summary>
    /// 撤销RefreshToken(强制登出)
    /// </summary>
    Task<ServiceResult<bool>> RevokeTokenAsync(RevokeTokenRequest request);

    // ========== 会话管理 ==========

    /// <summary>
    /// 获取用户会话信息(从Token解析)
    /// </summary>
    Task<ServiceResult<object>> GetSessionInfoAsync(string token);

    // ========== 超级管理员特权 ==========

    /// <summary>
    /// 修改sysadmin密码(系统初始化场景)
    /// </summary>
    Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(
        ChangeSysAdminPassword request);
}
```

**设计亮点**：
1. **双轨认证**：支持JWT令牌(无状态)和会话管理(有状态)
2. **Token刷新机制**：AccessToken短期(15分钟) + RefreshToken长期(7天)
3. **CancellationToken支持**：允许客户端取消长时间认证操作
4. **安全约束**：SuperAdmin密码修改需要特殊验证

### 2.3 IPatientService接口详解(患者服务)

**核心能力**：患者信息CRUD、搜索、批量导入导出

```csharp
public interface IPatientService
{
    // ========== 基础CRUD ==========

    /// <summary>
    /// 分页查询患者(支持关键字搜索)
    /// </summary>
    Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null);

    /// <summary>
    /// 根据ID获取患者详情
    /// </summary>
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 创建新患者
    /// </summary>
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);

    /// <summary>
    /// 更新患者信息
    /// </summary>
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);

    /// <summary>
    /// 删除患者(软删除)
    /// </summary>
    Task<ServiceResult> DeleteAsync(Guid id);

    // ========== 搜索与查询 ==========

    /// <summary>
    /// 搜索患者(按姓名、电话、身份证号)
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);

    // ========== 批量操作(Issue #1165) ==========

    /// <summary>
    /// 从Excel文件导入患者数据
    /// </summary>
    /// <param name="stream">Excel文件流</param>
    /// <param name="fileName">文件名(可选,用于日志记录)</param>
    /// <returns>导入结果,包含成功、失败数量和详细错误信息</returns>
    Task<ServiceResult<ImportResultDto<PatientDto>>> ImportFromExcelAsync(
        Stream stream,
        string? fileName = null);

    /// <summary>
    /// 生成患者导入模板(包含示例数据)
    /// </summary>
    /// <returns>包含示例数据的Excel模板流</returns>
    MemoryStream GenerateImportTemplate();
}
```

**设计模式**：
1. **CRUD标准化**：所有实体服务遵循统一的CRUD方法签名
2. **关键字搜索**：`GetPagedAsync`的`keyword`参数支持多字段模糊匹配
3. **Excel导入导出**：`ImportFromExcelAsync`返回详细的成功/失败清单
4. **同步方法例外**：`GenerateImportTemplate()`是同步方法(生成模板不涉及I/O)

### 2.4 IHerbService接口详解(中药材服务)

**核心能力**：中药材CRUD、分类筛选、拼音检索、批量操作

```csharp
public interface IHerbService
{
    // ========== 基础CRUD ==========

    /// <summary>
    /// 分页查询药材(Issue #1164: 扩展支持分类筛选)
    /// </summary>
    /// <param name="category">分类筛选(可选)</param>
    Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        string? category = null);

    Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto);
    Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);

    // ========== 批量操作(Issue #1169) ==========

    /// <summary>
    /// 批量删除药材(软删除)
    /// </summary>
    /// <param name="ids">药材ID列表</param>
    Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);

    // ========== 搜索与查询 ==========

    /// <summary>
    /// 搜索药材 - 支持多条件搜索(名称、拼音、功效)
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword);

    // ========== 批量导入导出(Issue #1166) ==========

    Task<ServiceResult<ImportResultDto<HerbDto>>> ImportFromExcelAsync(
        Stream stream,
        string? fileName = null);

    /// <summary>
    /// 导出药材数据到Excel(支持分类过滤)
    /// </summary>
    Task<MemoryStream> ExportAsync(string? category = null);

    MemoryStream GenerateImportTemplate();
}
```

**设计亮点**：
1. **分类筛选**：`GetPagedAsync`扩展`category`参数(Issue #1164)
2. **批量删除**：返回`BatchOperationResultDto`包含成功/失败统计
3. **拼音检索**：`SearchAsync`支持拼音首字母搜索(如"dg"匹配"当归")
4. **导出过滤**：`ExportAsync`支持按分类导出(可选参数)

### 2.5 IMedicalCaseService接口详解(病案服务 - 最复杂)

**核心能力**：病案聚合根管理、复杂查询、三步工作流辅助

#### 2.5.1 基础CRUD(6个方法)

```csharp
// ========== 基础CRUD ==========

Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(
    int page = 1,
    int pageSize = 20,
    string? keyword = null);

Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id);
Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto);
Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto);
Task<ServiceResult> DeleteAsync(Guid id);

/// <summary>
/// 批量删除医疗案例(软删除)(Issue #1169)
/// </summary>
Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);
```

#### 2.5.2 聚合根管理方法(7个方法)

```csharp
// ========== 聚合根统一管理(MedicalCase聚合Consultation + Prescription) ==========

/// <summary>
/// 创建完整的医疗案例(包含诊疗记录和可选的处方)
/// 作为聚合根统一管理整个诊疗流程
/// </summary>
Task<ServiceResult<MedicalCaseDto>> CreateWithDetailsAsync(
    MedicalCaseCreateDto caseDto,
    ConsultationCreateDto consultationDto,
    PrescriptionCreateDto? prescriptionDto = null);

/// <summary>
/// 根据ID获取完整的医疗案例(包含所有关联数据)
/// </summary>
Task<ServiceResult<MedicalCaseDetailDto>> GetByIdWithDetailsAsync(Guid id);

/// <summary>
/// 更新病案的诊断信息 (Issue #1477 架构纠正v2)
/// </summary>
Task<ServiceResult<ConsultationDto>> UpdateConsultationAsync(
    Guid medicalCaseId,
    ConsultationUpdateDto dto);

/// <summary>
/// 更新病案的处方信息 (Issue #1477 架构纠正v2)
/// </summary>
Task<ServiceResult<PrescriptionDto>> UpdatePrescriptionAsync(
    Guid medicalCaseId,
    PrescriptionUpdateDto dto);

/// <summary>
/// 为已存在的医案创建处方(Issue #1608补充)
/// 前置条件:MedicalCase和Consultation已存在
/// </summary>
Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(
    Guid medicalCaseId,
    PrescriptionCreateDto dto);

/// <summary>
/// 删除医案的处方(Issue #1608补充)
/// 根据A2决策:支持单独删除Prescription,保留MedicalCase和Consultation
/// </summary>
Task<ServiceResult> DeletePrescriptionAsync(Guid medicalCaseId);
```

**架构亮点**：
- ✅ **聚合根模式**：MedicalCase作为聚合根统一管理Consultation和Prescription
- ✅ **级联操作**：`CreateWithDetailsAsync`一次性创建完整病案(事务保证)
- ✅ **细粒度控制**：支持单独更新/删除子实体(通过聚合根协调)

#### 2.5.3 复杂查询方法(3个方法)

```csharp
// ========== 复杂查询 ==========

/// <summary>
/// 根据患者ID获取医疗案例列表
/// </summary>
Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId);

/// <summary>
/// 获取待看诊医案列表(Status=Active)
/// Epic #1583 - Phase 5
/// </summary>
Task<ServiceResult<List<PendingMedicalCaseDto>>> GetPendingCasesAsync();

/// <summary>
/// 查询病案列表(支持多条件组合查询)
/// Issue #1592 - Phase 3
/// </summary>
/// <param name="patientName">患者姓名关键字(模糊匹配)</param>
/// <param name="startDate">开始日期(过滤CreatedAt)</param>
/// <param name="endDate">结束日期(过滤CreatedAt)</param>
/// <param name="diagnosisKeyword">诊断关键字(搜索TCMDiagnosis)</param>
Task<ServiceResult<List<MedicalCaseDto>>> QueryAsync(
    string? patientName = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    string? diagnosisKeyword = null);
```

**查询优化**：
- ✅ `GetPendingCasesAsync`：专用于"待看诊"列表(性能优化)
- ✅ `QueryAsync`：支持4个维度的组合查询(灵活性)

#### 2.5.4 三步工作流辅助方法(4个方法)

```csharp
// ========== Epic #1589 - 三步工作流辅助方法(Issue #1600 Phase 3) ==========

/// <summary>
/// 完成辩证步骤(Step 1)
/// Epic #1589 Phase 1 - 架构合规版本
/// 通过MedicalCase聚合根更新Consultation.Step1CompletedAt
/// </summary>
/// <param name="medicalCaseId">医案ID</param>
/// <param name="request">Step1请求参数(是否开处方)</param>
/// <returns>Step1完成状态</returns>
Task<ServiceResult<ConsultationStepDto>> CompleteStep1Async(
    Guid medicalCaseId,
    CompleteStep1Request request);

/// <summary>
/// 重置诊疗步骤(清除所有Step完成时间)
/// Epic #1589 - 辅助功能
/// </summary>
Task<ServiceResult> ResetConsultationStepsAsync(Guid medicalCaseId);

/// <summary>
/// 清空处方内容(保留处方实体框架)
/// Epic #1589 - 辅助功能
/// </summary>
Task<ServiceResult> ClearPrescriptionAsync(Guid medicalCaseId);

/// <summary>
/// 从验方导入到处方(将Formula内容复制到Prescription)
/// Epic #1589 - 辅助功能(TODO: 待实现)
/// </summary>
Task<ServiceResult<PrescriptionDto>> ImportFormulaIntoPrescriptionAsync(
    Guid medicalCaseId,
    Guid formulaId);
```

**工作流设计**：
- **Step 1**: 辩证论治(完成诊断,决定是否开处方)
- **Step 2**: 处方开具(选择药材、设置剂量)
- **Step 3**: 打印交付(生成处方单、完成诊疗)

**辅助功能**：
- ✅ `ResetConsultationStepsAsync`：允许医生重新诊断
- ✅ `ClearPrescriptionAsync`：清空处方但保留实体(避免重新创建)
- ✅ `ImportFormulaIntoPrescriptionAsync`：快速导入验方模板

### 2.6 服务接口方法命名规范

| 操作类型 | 命名模式 | 示例 | 说明 |
|---------|---------|------|------|
| **查询单个** | `GetByIdAsync` | `GetByIdAsync(Guid id)` | 根据ID获取单个实体 |
| **查询列表** | `GetByXxxAsync` | `GetByPatientIdAsync(Guid patientId)` | 根据特定条件获取列表 |
| **分页查询** | `GetPagedAsync` | `GetPagedAsync(int page, int pageSize)` | 分页查询,返回`PagedResult<T>` |
| **搜索** | `SearchAsync` | `SearchAsync(string keyword)` | 多字段模糊搜索 |
| **复杂查询** | `QueryAsync` | `QueryAsync(...)` | 多条件组合查询 |
| **创建** | `CreateAsync` | `CreateAsync(PatientCreateDto dto)` | 创建新实体 |
| **更新** | `UpdateAsync` | `UpdateAsync(Guid id, PatientUpdateDto dto)` | 更新实体 |
| **删除** | `DeleteAsync` | `DeleteAsync(Guid id)` | 软删除 |
| **批量删除** | `BatchDeleteAsync` | `BatchDeleteAsync(List<Guid> ids)` | 批量软删除 |
| **导入** | `ImportFromExcelAsync` | `ImportFromExcelAsync(Stream stream)` | Excel导入 |
| **导出** | `ExportAsync` | `ExportAsync(string? filter)` | Excel导出 |
| **生成模板** | `GenerateImportTemplate` | `GenerateImportTemplate()` | 生成Excel模板(同步方法) |
| **聚合根操作** | `XxxWithDetailsAsync` | `CreateWithDetailsAsync(...)` | 聚合根级联操作 |
| **业务流程** | `CompleteXxxAsync` | `CompleteStep1Async(...)` | 业务流程推进 |

**强制约束**：
- ✅ 所有涉及I/O的方法必须以`Async`结尾
- ✅ 同步方法仅限纯计算类操作(如`GenerateImportTemplate`)
- ✅ 参数顺序：`(ID, DTO, Filter, Pagination, CancellationToken)`
- ❌ 禁止在接口方法中使用`out`/`ref`参数(破坏可测试性)

---

## 3. Repository接口模式

### 3.1 Repository接口架构

```
IBaseRepository<TEntity> (Infrastructure层)
   ↓ 继承
IRepository<T> (Infrastructure层 - 通用仓储)
   ↓ 继承
IHerbRepository (Module.Herbs层 - 模块仓储)
   ↓ 扩展方法
   - GetByNameAsync(string name)
   - GetByNameOrPinyinAsync(string searchTerm)
```

**设计说明**：
- ✅ **IBaseRepository**：定义20个通用CRUD方法(所有实体共享)
- ✅ **IRepository<T>**：泛型仓储接口(可选的业务扩展层)
- ✅ **IXxxRepository**：模块专用仓储接口(扩展特定查询方法)
- ⚠️ **Repository接口不在Server.Interfaces中**：各模块自行定义Repository接口

### 3.2 IBaseRepository接口详解(20个核心方法)

```csharp
public interface IBaseRepository<TEntity> where TEntity : BaseEntity
{
    // ========== 查询操作(8个方法) ==========

    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    Task<TEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// 根据ID获取实体(包含关联数据)
    /// </summary>
    Task<TEntity?> GetByIdWithIncludesAsync(Guid id,
        params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// 获取所有实体(⚠️ 慎用,数据量大时性能问题)
    /// </summary>
    Task<List<TEntity>> GetAllAsync();

    /// <summary>
    /// 根据条件查询
    /// </summary>
    Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// 分页查询
    /// </summary>
    Task<(List<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool descending = true);

    /// <summary>
    /// 检查是否存在
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// 获取数量
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);

    // ========== 创建操作(2个方法) ==========

    Task<TEntity> AddAsync(TEntity entity);
    Task<List<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities);

    // ========== 更新操作(2个方法) ==========

    Task<TEntity> UpdateAsync(TEntity entity);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities);

    // ========== 删除操作(3个方法) ==========

    /// <summary>
    /// 软删除实体(设置IsDeleted=true)
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// 批量软删除实体
    /// </summary>
    Task<int> DeleteRangeAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// 物理删除实体(⚠️ 谨慎使用,数据不可恢复)
    /// </summary>
    Task<bool> HardDeleteAsync(Guid id);

    // ========== 高级查询(3个方法) ==========

    /// <summary>
    /// 获取可查询对象(用于复杂LINQ查询)
    /// </summary>
    IQueryable<TEntity> GetQueryable();

    /// <summary>
    /// 获取不跟踪的查询对象(只读查询,性能优化)
    /// </summary>
    IQueryable<TEntity> GetNoTrackingQueryable();

    /// <summary>
    /// 执行SQL查询(⚠️ 慎用,存在SQL注入风险)
    /// </summary>
    Task<List<TEntity>> FromSqlRawAsync(string sql, params object[] parameters);

    // ========== 事务操作(3个方法) ==========

    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync(IDbContextTransaction transaction);
    Task RollbackTransactionAsync(IDbContextTransaction transaction);
}
```

**方法分组说明**：
1. **查询操作(8个)**：覆盖90%的查询场景(单个、列表、分页、条件、计数)
2. **创建操作(2个)**：单个创建 + 批量创建
3. **更新操作(2个)**：单个更新 + 批量更新
4. **删除操作(3个)**：软删除(默认) + 批量软删除 + 物理删除(危险)
5. **高级查询(3个)**：IQueryable(复杂LINQ) + NoTracking(只读) + SQL原生查询(最后手段)
6. **事务操作(3个)**：显式事务控制(Begin/Commit/Rollback)

### 3.3 模块仓储接口扩展示例(IHerbRepository)

```csharp
/// <summary>
/// 中药材仓储接口(扩展2个业务查询方法)
/// </summary>
public interface IHerbRepository : IRepository<Herb>
{
    /// <summary>
    /// 根据名称获取药材(精确匹配)
    /// </summary>
    Task<Herb?> GetByNameAsync(string name);

    /// <summary>
    /// 按名称或拼音码查询药材 (Issue #1351)
    /// 优先精确匹配名称,其次模糊匹配拼音码
    /// </summary>
    /// <param name="searchTerm">搜索词(药材名称或拼音码)</param>
    Task<Herb?> GetByNameOrPinyinAsync(string searchTerm);
}
```

**扩展原则**：
- ✅ 仅扩展**业务特定的查询方法**(如拼音检索、名称查询)
- ✅ 扩展方法命名遵循`GetByXxxAsync`模式
- ❌ 禁止在Repository接口中扩展业务逻辑方法(应在Service层)
- ❌ 禁止扩展CRUD方法(已在IBaseRepository中定义)

### 3.4 Repository接口 vs Service接口

| 对比维度 | Repository接口 | Service接口 |
|---------|---------------|-------------|
| **定位** | 数据访问层(Infrastructure) | 业务逻辑层(Application) |
| **职责** | 数据库CRUD操作 | 业务规则验证、流程编排 |
| **返回类型** | `Task<TEntity>` | `Task<ServiceResult<TDto>>` |
| **依赖方向** | 被Service依赖 | 被Controller依赖 |
| **扩展策略** | 扩展查询方法 | 扩展业务方法 |
| **事务管理** | 支持显式事务 | 通过UnitOfWork管理事务 |
| **测试策略** | Mock Repository | Mock Service |

**实例对比**：

```csharp
// ❌ 错误示例：Repository接口包含业务逻辑
public interface IPatientRepository
{
    Task<bool> ValidatePatientEligibilityAsync(Guid patientId); // 业务逻辑
}

// ✅ 正确示例：Service接口包含业务逻辑
public interface IPatientService
{
    Task<ServiceResult<bool>> ValidatePatientEligibilityAsync(Guid patientId);
}

// ✅ 正确示例：Repository接口仅包含数据查询
public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByIdCardAsync(string idCard); // 数据查询
}
```

---

## 4. 接口命名规范与约定

### 4.1 接口命名规范

| 接口类型 | 命名模式 | 示例 | 说明 |
|---------|---------|------|------|
| **服务接口** | `I{Entity}Service` | `IPatientService` | 实体名称 + Service |
| **仓储接口** | `I{Entity}Repository` | `IPatientRepository` | 实体名称 + Repository |
| **基础仓储** | `IBaseRepository<T>` | `IBaseRepository<Patient>` | 泛型基础仓储 |
| **认证服务** | `IAuthService` | `IAuthService` | 特殊命名(非实体服务) |
| **聚合根服务** | `I{AggregateRoot}Service` | `IMedicalCaseService` | 聚合根名称 + Service |

**强制约束**：
- ✅ 所有接口必须以`I`开头(C#接口命名约定)
- ✅ 接口名称使用PascalCase命名
- ✅ 接口文件名与接口名称一致(如`IPatientService.cs`)
- ❌ 禁止在接口名称中使用缩写(如`IPatSvc` ❌,应为`IPatientService` ✅)

### 4.2 方法参数命名约定

| 参数类型 | 命名模式 | 示例 | 说明 |
|---------|---------|------|------|
| **ID参数** | `id` | `Guid id` | 主键ID(小写) |
| **DTO参数** | `dto` | `PatientCreateDto dto` | DTO对象(小写) |
| **关键字** | `keyword` | `string keyword` | 搜索关键字 |
| **分页参数** | `page`, `pageSize` | `int page = 1, int pageSize = 20` | 分页参数(小写) |
| **过滤条件** | `{entity}Id` | `Guid patientId` | 外键ID(camelCase) |
| **可选参数** | `{name}?` | `string? category = null` | 可空类型 + 默认值 |
| **取消令牌** | `cancellationToken` | `CancellationToken cancellationToken = default` | 最后一个参数 |

**参数顺序约定**：
```csharp
// ✅ 推荐的参数顺序
Task<ServiceResult<PatientDto>> GetByIdAsync(
    Guid id,                              // 1. 必需的ID参数
    CancellationToken cancellationToken   // 2. 可选的CancellationToken(放最后)
);

Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(
    int page = 1,                         // 1. 分页参数(有默认值)
    int pageSize = 20,                    // 2. 分页参数(有默认值)
    string? keyword = null,               // 3. 可选的过滤参数
    CancellationToken cancellationToken = default  // 4. 取消令牌(最后)
);
```

### 4.3 返回类型约定

| 操作类型 | 返回类型 | 示例 | 说明 |
|---------|---------|------|------|
| **单个实体** | `Task<ServiceResult<TDto>>` | `Task<ServiceResult<PatientDto>>` | 包装在ServiceResult中 |
| **实体列表** | `Task<ServiceResult<List<TDto>>>` | `Task<ServiceResult<List<PatientDto>>>` | 包装List |
| **分页结果** | `Task<ServiceResult<PagedResult<TDto>>>` | `Task<ServiceResult<PagedResult<PatientDto>>>` | 包装PagedResult |
| **无返回值** | `Task<ServiceResult>` | `Task<ServiceResult>` | 仅返回成功/失败 |
| **布尔结果** | `Task<ServiceResult<bool>>` | `Task<ServiceResult<bool>>` | 返回bool |
| **文件流** | `Task<MemoryStream>` | `Task<MemoryStream>` | Excel导出等(不包装) |
| **同步方法** | `MemoryStream` | `MemoryStream` | 生成模板等(不包装) |

**特殊情况**：
- ✅ `MemoryStream`不包装在`ServiceResult`中(文件流不需要错误封装)
- ✅ 同步方法`GenerateImportTemplate()`直接返回`MemoryStream`
- ❌ 禁止返回`null`(使用`ServiceResult.Fail()`表示失败)

---

## 5. ServiceResult统一返回封装

### 5.1 ServiceResult设计目标

**为什么需要ServiceResult**：
1. **统一错误处理**：避免在Controller层到处`try-catch`
2. **明确成功/失败**：通过`IsSuccess`属性一目了然
3. **错误信息传递**：通过`ErrorMessage`和`Errors`传递详细错误
4. **业务逻辑与HTTP解耦**：Service层不依赖HTTP状态码

### 5.2 ServiceResult结构定义

```csharp
/// <summary>
/// 服务结果封装(无数据)
/// </summary>
public class ServiceResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 详细错误列表(用于验证错误等)
    /// </summary>
    public List<string>? Errors { get; set; }

    // ========== 工厂方法 ==========

    public static ServiceResult Success()
        => new() { IsSuccess = true };

    public static ServiceResult Fail(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };

    public static ServiceResult Fail(List<string> errors)
        => new() { IsSuccess = false, Errors = errors, ErrorMessage = string.Join(", ", errors) };
}

/// <summary>
/// 服务结果封装(带数据)
/// </summary>
public class ServiceResult<T> : ServiceResult
{
    /// <summary>
    /// 返回数据
    /// </summary>
    public T? Data { get; set; }

    // ========== 工厂方法 ==========

    public static ServiceResult<T> Success(T data)
        => new() { IsSuccess = true, Data = data };

    public new static ServiceResult<T> Fail(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };

    public new static ServiceResult<T> Fail(List<string> errors)
        => new() { IsSuccess = false, Errors = errors, ErrorMessage = string.Join(", ", errors) };
}
```

### 5.3 ServiceResult使用模式

#### 5.3.1 Service层使用(返回ServiceResult)

```csharp
public class PatientService : IPatientService
{
    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var patient = await _repository.GetByIdAsync(id);

            // ❌ 错误示例：返回null
            // if (patient == null) return null;

            // ✅ 正确示例：使用ServiceResult.Fail()
            if (patient == null)
                return ServiceResult<PatientDto>.Fail("患者不存在");

            var dto = _mapper.Map<PatientDto>(patient);
            return ServiceResult<PatientDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者失败: {Id}", id);
            return ServiceResult<PatientDto>.Fail($"获取患者失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
    {
        try
        {
            // 业务规则验证
            var validationResult = await ValidatePatientAsync(dto);
            if (!validationResult.IsSuccess)
                return ServiceResult<PatientDto>.Fail(validationResult.Errors);

            // 检查重复
            var exists = await _repository.ExistsAsync(p => p.IdCard == dto.IdCard);
            if (exists)
                return ServiceResult<PatientDto>.Fail("身份证号已存在");

            // 创建实体
            var patient = _mapper.Map<Patient>(dto);
            var created = await _repository.AddAsync(patient);
            var patientDto = _mapper.Map<PatientDto>(created);

            return ServiceResult<PatientDto>.Success(patientDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者失败");
            return ServiceResult<PatientDto>.Fail($"创建患者失败: {ex.Message}");
        }
    }
}
```

#### 5.3.2 Controller层使用(解包ServiceResult)

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _patientService.GetByIdAsync(id);

        // ✅ 推荐方式：统一处理ServiceResult
        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientCreateDto dto)
    {
        var result = await _patientService.CreateAsync(dto);

        if (!result.IsSuccess)
        {
            // 返回详细错误(如验证错误)
            if (result.Errors != null && result.Errors.Any())
                return BadRequest(new { errors = result.Errors });

            return BadRequest(new { error = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }
}
```

### 5.4 ServiceResult vs 异常抛出

| 场景 | ServiceResult | 异常抛出(throw) | 推荐方案 |
|------|--------------|----------------|---------|
| **业务规则验证失败** | ✅ 返回`ServiceResult.Fail("规则错误")` | ❌ 不应抛异常 | ServiceResult |
| **数据不存在** | ✅ 返回`ServiceResult.Fail("不存在")` | ❌ 不应抛异常 | ServiceResult |
| **参数验证失败** | ✅ 返回`ServiceResult.Fail(errors)` | ✅ 或抛出`ValidationException` | ServiceResult(推荐) |
| **数据库异常** | ❌ | ✅ 抛出异常,全局捕获 | 异常抛出 |
| **网络异常** | ❌ | ✅ 抛出异常,全局捕获 | 异常抛出 |
| **未预期的系统错误** | ❌ | ✅ 抛出异常,全局捕获 | 异常抛出 |

**核心原则**：
- ✅ **预期的业务错误**：使用`ServiceResult.Fail()`(如数据不存在、业务规则不满足)
- ✅ **系统级错误**：抛出异常,由全局异常处理器捕获(如数据库连接失败、内存溢出)

---

## 6. 依赖注入注册模式

### 6.1 服务接口注册(在模块中)

**推荐模式**：每个模块提供扩展方法统一注册

```csharp
// LYBT.Module.Patients/PatientsModule.cs
public static class PatientsModuleExtensions
{
    public static IServiceCollection AddPatientsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册服务接口
        services.AddScoped<IPatientService, PatientService>();

        // 注册仓储接口
        services.AddScoped<IPatientRepository, PatientRepository>();

        // 注册验证器
        services.AddValidatorsFromAssemblyContaining<PatientCreateDtoValidator>();

        // 注册AutoMapper配置
        services.AddAutoMapper(typeof(PatientMappingProfile));

        return services;
    }
}
```

### 6.2 统一注册(在Startup.cs中)

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // ========== 注册所有模块 ==========

        services.AddAuthModule(Configuration);
        services.AddPatientsModule(Configuration);
        services.AddHerbsModule(Configuration);
        services.AddMedicalCaseModule(Configuration);
        services.AddConsultationModule(Configuration);
        services.AddPrescriptionModule(Configuration);
        services.AddFormulaModule(Configuration);
        services.AddUsersModule(Configuration);

        // ========== 注册基础设施 ==========

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

        // ========== 注册通用服务 ==========

        services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}
```

### 6.3 生命周期选择

| 生命周期 | 适用场景 | 示例 | 说明 |
|---------|---------|------|------|
| **Scoped** | 服务、仓储、DbContext | `IPatientService`, `IPatientRepository`, `AppDbContext` | 每次HTTP请求创建一次 |
| **Singleton** | 无状态服务、配置 | `IConfiguration`, `ILogger<T>` | 应用生命周期单例 |
| **Transient** | 轻量级无状态服务 | `IMapper` | 每次注入创建新实例 |

**推荐约定**：
- ✅ **所有Service接口注册为Scoped**(与DbContext生命周期一致)
- ✅ **所有Repository接口注册为Scoped**(避免跨请求数据污染)
- ❌ 禁止将DbContext注册为Singleton(会导致并发问题)

---

## 7. 接口扩展策略

### 7.1 何时扩展接口

| 场景 | 是否扩展 | 推荐方案 | 说明 |
|------|---------|---------|------|
| **新增CRUD方法** | ❌ | 复用`IBaseRepository`已有方法 | 通用CRUD已覆盖 |
| **新增业务查询** | ✅ | 在IXxxService中扩展 | 如`GetPendingCasesAsync` |
| **新增特殊查询** | ✅ | 在IXxxRepository中扩展 | 如`GetByNameOrPinyinAsync` |
| **新增聚合根操作** | ✅ | 在IAggregateRootService中扩展 | 如`CreateWithDetailsAsync` |
| **新增辅助工具方法** | ❌ | 放在Utilities项目中 | 如`StringHelper.ToPinyin` |

### 7.2 接口扩展示例(正确方式)

#### 示例1：扩展业务查询方法

```csharp
// ✅ 正确示例：在Service接口中扩展业务查询
public interface IMedicalCaseService
{
    // 基础CRUD(已有)
    Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id);

    // ✅ 扩展：业务特定查询
    Task<ServiceResult<List<PendingMedicalCaseDto>>> GetPendingCasesAsync();
}
```

#### 示例2：扩展特殊数据查询

```csharp
// ✅ 正确示例：在Repository接口中扩展特殊查询
public interface IHerbRepository : IRepository<Herb>
{
    // ✅ 扩展：拼音检索(数据层特殊查询)
    Task<Herb?> GetByNameOrPinyinAsync(string searchTerm);
}
```

### 7.3 接口扩展反模式(错误方式)

```csharp
// ❌ 错误示例1：在Repository中扩展业务逻辑
public interface IPatientRepository
{
    Task<bool> ValidatePatientEligibilityAsync(Guid patientId); // 业务逻辑
}

// ❌ 错误示例2：在Service中扩展工具方法
public interface IPatientService
{
    string GenerateRandomPassword(); // 应该在Utilities中
}

// ❌ 错误示例3：破坏接口隔离原则
public interface IMedicalCaseService
{
    // 将Consultation和Prescription的所有方法都混入MedicalCaseService
    Task<ConsultationDto> CreateConsultationAsync(...); // 应该通过聚合根管理
    Task<PrescriptionDto> CreatePrescriptionAsync(...); // 应该通过聚合根管理
}
```

---

## 8. 最佳实践与反模式

### 8.1 接口设计最佳实践

#### 实践1：接口方法保持简洁

```csharp
// ✅ 推荐：方法职责单一
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
}

// ❌ 反模式：方法参数过多(>5个)
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> CreatePatientAsync(
        string name,
        string phone,
        string idCard,
        DateTime birthDate,
        string gender,
        string address,
        string notes); // 7个参数,应使用DTO
}
```

#### 实践2：使用DTO封装复杂参数

```csharp
// ✅ 推荐：使用DTO封装
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
}

// ✅ DTO定义(在Shared.Models中)
public class PatientCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdCard { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Notes { get; set; }
}
```

#### 实践3：返回类型一致性

```csharp
// ✅ 推荐：所有Service方法返回ServiceResult
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
}

// ❌ 反模式：返回类型不一致
public interface IPatientService
{
    Task<PatientDto?> GetByIdAsync(Guid id); // 返回null表示失败
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
    Task<bool> DeleteAsync(Guid id); // 返回bool表示成功/失败
}
```

#### 实践4：CancellationToken支持长时间操作

```csharp
// ✅ 推荐：长时间操作支持CancellationToken
public interface IAuthService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}

// ✅ 短时间操作可省略CancellationToken
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id); // 查询ID通常很快
}
```

#### 实践5：使用明确的方法命名

```csharp
// ✅ 推荐：方法名清晰表达意图
public interface IMedicalCaseService
{
    Task<ServiceResult<List<PendingMedicalCaseDto>>> GetPendingCasesAsync();
    Task<ServiceResult<ConsultationStepDto>> CompleteStep1Async(Guid medicalCaseId, CompleteStep1Request request);
}

// ❌ 反模式：方法名模糊
public interface IMedicalCaseService
{
    Task<ServiceResult<List<MedicalCaseDto>>> GetListAsync(); // 什么列表?
    Task<ServiceResult> DoStep1Async(Guid id, object data); // 做什么?
}
```

### 8.2 常见反模式与纠正

#### 反模式1：接口依赖具体实现

```csharp
// ❌ 错误：接口方法参数依赖具体类型
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> CreateAsync(Patient entity); // Patient是Entity
}

// ✅ 正确：接口方法参数使用DTO
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto); // DTO
}
```

#### 反模式2：接口方法抛出特定异常

```csharp
// ❌ 错误：接口定义中承诺抛出特定异常
/// <summary>
/// 创建患者
/// </summary>
/// <exception cref="DuplicateIdCardException">身份证号重复时抛出</exception>
Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);

// ✅ 正确：通过ServiceResult返回错误
Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
// 实现中：return ServiceResult<PatientDto>.Fail("身份证号重复");
```

#### 反模式3：接口方法使用out/ref参数

```csharp
// ❌ 错误：使用out参数(破坏可测试性)
Task<bool> TryGetPatientAsync(Guid id, out PatientDto patient);

// ✅ 正确：使用返回值
Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
```

#### 反模式4：接口方法包含业务逻辑默认实现

```csharp
// ❌ 错误：接口方法有默认实现(C# 8.0+特性,不推荐用于业务逻辑)
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        // 默认实现业务逻辑(违反接口隔离原则)
        return Task.FromResult(ServiceResult<PatientDto>.Fail("未实现"));
    }
}

// ✅ 正确：接口只定义契约,实现在Service类中
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
}

public class PatientService : IPatientService
{
    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        // 具体实现
    }
}
```

#### 反模式5：接口方法参数过度泛化

```csharp
// ❌ 错误：使用object类型参数
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> CreateAsync(object data);
}

// ✅ 正确：使用强类型DTO
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
}
```

---

## 9. 设计原则与约束

### 9.1 SOLID原则在接口设计中的体现

#### 9.1.1 单一职责原则(SRP)

```csharp
// ✅ 正确：每个接口职责单一
public interface IPatientService
{
    // 仅负责患者管理
}

public interface IMedicalCaseService
{
    // 仅负责病案管理
}

// ❌ 错误：接口职责混乱
public interface IPatientAndMedicalCaseService
{
    // 患者 + 病案混在一起
    Task<ServiceResult<PatientDto>> CreatePatientAsync(...);
    Task<ServiceResult<MedicalCaseDto>> CreateMedicalCaseAsync(...);
}
```

#### 9.1.2 接口隔离原则(ISP)

```csharp
// ✅ 正确：接口细粒度拆分
public interface IReadOnlyPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<List<PatientDto>>> GetAllAsync();
}

public interface IPatientService : IReadOnlyPatientService
{
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
}

// ❌ 错误：接口过于臃肿
public interface IPatientService
{
    // 50个方法...查询、创建、更新、删除、统计、报表、导入导出等全部混在一起
}
```

#### 9.1.3 依赖倒置原则(DIP)

```csharp
// ✅ 正确：Controller依赖接口(高层依赖抽象)
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService; // 依赖接口

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }
}

// ❌ 错误：Controller依赖具体实现(高层依赖细节)
public class PatientsController : ControllerBase
{
    private readonly PatientService _patientService; // 依赖具体类

    public PatientsController(PatientService patientService)
    {
        _patientService = patientService;
    }
}
```

### 9.2 接口设计约束清单

| 约束类型 | 约束规则 | 违反后果 |
|---------|---------|---------|
| **依赖约束** | 接口层仅依赖Shared.Models(DTO) | 循环依赖、编译失败 |
| **命名约束** | 接口以`I`开头,使用PascalCase | 不符合C#约定 |
| **返回类型约束** | 所有Service方法返回`Task<ServiceResult<T>>` | 错误处理不统一 |
| **参数约束** | 复杂参数使用DTO封装,禁止>5个参数 | 接口难以维护 |
| **异步约束** | 所有涉及I/O的方法必须异步(Async) | 性能问题 |
| **生命周期约束** | Service和Repository注册为Scoped | 并发问题、数据污染 |
| **接口职责约束** | 每个接口职责单一(SRP) | 接口臃肿、难以测试 |
| **版本兼容约束** | 接口方法签名变更需要新版本 | 破坏现有调用方 |

### 9.3 接口版本演进策略

#### 策略1：添加新方法(兼容性变更)

```csharp
// v1.0 接口
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
}

// v1.1 接口(添加新方法,不影响现有调用)
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword); // 新增
}
```

#### 策略2：修改方法签名(破坏性变更,需新版本)

```csharp
// v1.0 接口
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
}

// v2.0 接口(修改方法签名,破坏性变更)
public interface IPatientService_V2
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id, bool includeDetails); // 新增参数
}
```

#### 策略3：接口继承(渐进式扩展)

```csharp
// 基础接口
public interface IPatientService
{
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
}

// 扩展接口(继承基础接口)
public interface IExtendedPatientService : IPatientService
{
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);
}
```

---

## 10. 测试支持(Mock接口)

### 10.1 单元测试中Mock接口

**使用NSubstitute进行Mock**：

```csharp
using NSubstitute;
using Xunit;

public class PatientsControllerTests
{
    [Fact]
    public async Task GetById_Should_Return_Patient()
    {
        // Arrange - Mock IPatientService
        var mockPatientService = Substitute.For<IPatientService>();
        mockPatientService.GetByIdAsync(Arg.Any<Guid>())
            .Returns(ServiceResult<PatientDto>.Success(new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = "张三"
            }));

        var controller = new PatientsController(mockPatientService);

        // Act
        var result = await controller.GetById(Guid.NewGuid());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var patientDto = Assert.IsType<PatientDto>(okResult.Value);
        Assert.Equal("张三", patientDto.Name);
    }

    [Fact]
    public async Task GetById_Should_Return_BadRequest_When_NotFound()
    {
        // Arrange
        var mockPatientService = Substitute.For<IPatientService>();
        mockPatientService.GetByIdAsync(Arg.Any<Guid>())
            .Returns(ServiceResult<PatientDto>.Fail("患者不存在"));

        var controller = new PatientsController(mockPatientService);

        // Act
        var result = await controller.GetById(Guid.NewGuid());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
```

### 10.2 集成测试中使用真实实现

```csharp
public class PatientServiceIntegrationTests : IClassFixture<DbContextFixture>
{
    private readonly AppDbContext _context;
    private readonly IPatientService _patientService;

    public PatientServiceIntegrationTests(DbContextFixture fixture)
    {
        _context = fixture.Context;

        // 使用真实的Repository和Service实现
        var repository = new PatientRepository(_context);
        var mapper = new MapperConfiguration(cfg =>
            cfg.AddProfile<PatientMappingProfile>()).CreateMapper();
        var logger = Substitute.For<ILogger<PatientService>>();

        _patientService = new PatientService(repository, mapper, logger);
    }

    [Fact]
    public async Task CreateAsync_Should_Persist_To_Database()
    {
        // Arrange
        var dto = new PatientCreateDto
        {
            Name = "李四",
            Phone = "13800138000",
            IdCard = "110101199001011234"
        };

        // Act
        var result = await _patientService.CreateAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        // 验证数据库持久化
        var savedPatient = await _context.Patients.FindAsync(result.Data.Id);
        Assert.NotNull(savedPatient);
        Assert.Equal("李四", savedPatient.Name);
    }
}
```

### 10.3 测试接口方法覆盖率清单

| 测试类型 | 覆盖内容 | 工具 | 覆盖率目标 |
|---------|---------|------|-----------|
| **单元测试** | Service接口方法(Mock Repository) | xUnit + NSubstitute | >80% |
| **集成测试** | Service接口方法(真实Repository + InMemoryDb) | xUnit + EF Core InMemory | >60% |
| **API测试** | Controller端点(真实Service + 真实DB) | xUnit + WebApplicationFactory | >50% |

---

## 11. 参考资料

### 11.1 内部文档

- **DTO设计标准**: `docs/explanation/architecture/shared/dto-design-standard.md`
- **Server端三层架构**: `docs/explanation/architecture/server/README.md`
- **服务层设计**: `docs/explanation/architecture/server/services-layer-design.md` *(待创建)*
- **Repository模式**: `docs/explanation/architecture/server/repository-pattern.md` *(待创建)*
- **依赖注入指南**: `docs/how-to-guides/server/interfaces-usage.md` *(待创建)*

### 11.2 外部参考

- **依赖倒置原则(DIP)**: [Microsoft Docs - Dependency Inversion Principle](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles#dependency-inversion)
- **接口隔离原则(ISP)**: [Microsoft Docs - Interface Segregation Principle](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles#interface-segregation)
- **Repository模式**: [Microsoft Docs - Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design#the-repository-pattern)
- **NSubstitute文档**: https://nsubstitute.github.io/

### 11.3 相关源文件

| 文件路径 | 说明 |
|---------|------|
| `src/Server/Core/LYBT.Server.Interfaces/Services/IAuthService.cs` | 认证服务接口(8方法) |
| `src/Server/Core/LYBT.Server.Interfaces/Services/IPatientService.cs` | 患者服务接口(8方法) |
| `src/Server/Core/LYBT.Server.Interfaces/Services/IHerbService.cs` | 中药材服务接口(10方法) |
| `src/Server/Core/LYBT.Server.Interfaces/Services/IMedicalCaseService.cs` | 病案服务接口(19方法) |
| `src/Server/Core/LYBT.Infrastructure/Repositories/IBaseRepository.cs` | 基础仓储接口(20方法) |
| `src/Server/Modules/LYBT.Module.Herbs/Interfaces/IHerbRepository.cs` | 中药材仓储接口(2扩展方法) |
| `src/Shared/LYBT.Shared.Models/Common/ServiceResult.cs` | ServiceResult封装类 |

---

## 12. 更新历史

| 版本 | 日期 | 修改内容 | 负责人 |
|-----|------|---------|--------|
| v1.0 | 2025-10-29 | 初始版本,完整架构设计文档 | Server端架构组 |

---

**文档所有权**: Server端开发组
**审阅人**: 架构组
**批准人**: 技术负责人
