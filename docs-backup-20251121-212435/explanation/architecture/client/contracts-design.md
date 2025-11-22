# Client端契约层架构设计文档

> **文档版本**：v1.0
> **最后更新**：2025-01-30
> **所属模块**：LYBT.Desktop.Contracts（契约层）
> **相关Issue**：Epic #1540（依赖倒置）、Issue #1606（聚合根模式）、Epic #1343（MVP架构标准化）

---

## 📋 目录

1. [模块概述](#1-模块概述)
2. [模块架构](#2-模块架构)
3. [API接口设计](#3-api接口设计)
4. [服务接口设计](#4-服务接口设计)
5. [Refit客户端配置](#5-refit客户端配置)
6. [依赖注入注册](#6-依赖注入注册)
7. [依赖倒置模式](#7-依赖倒置模式)
8. [核心设计原则](#8-核心设计原则)
9. [集成与使用](#9-集成与使用)
10. [测试策略](#10-测试策略)
11. [最佳实践](#11-最佳实践)
12. [总结](#12-总结)

---

## 1. 模块概述

### 1.1 模块定位

契约层（LYBT.Desktop.Contracts）是Client端的**接口定义层**，负责定义：
- **HTTP API客户端接口**：前后端通信契约（Refit接口）
- **跨模块服务接口**：模块间解耦的服务契约（依赖倒置）
- **统一响应格式**：ApiResponse<T>、PagedResult<T>等标准类型

**⚠️ 核心特征**：
- **零业务逻辑**：只定义接口，不包含实现
- **依赖最小化**：仅依赖Refit和LYBT.Shared.Models
- **契约一致性**：与Server端API契约100%对齐
- **依赖倒置**：高层模块依赖抽象接口，低层模块实现接口

### 1.2 模块职责

```
┌─────────────────────────────────────────────────────────┐
│          LYBT.Desktop.Contracts（契约层）                  │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  职责1: API客户端接口定义（8个）                          │
│    - IAuthApi: 认证API接口                              │
│    - IPatientApi: 患者API接口                           │
│    - IHerbApi: 药材API接口                              │
│    - IFormulaApi: 验方API接口                           │
│    - IUserApi: 用户API接口                              │
│    - IConsultationApi: 诊疗API接口                       │
│    - IPrescriptionApi: 处方API接口（Read-only）          │
│    - IMedicalCaseApi: 医案API接口（聚合根）              │
│                                                         │
│  职责2: 跨模块服务接口定义（1个）                          │
│    - IPrescriptionEditorService: 处方编辑器服务接口       │
│                                                         │
│  职责3: 共享响应格式                                      │
│    - ApiResponse<T>: 统一API响应格式                     │
│    - PagedResult<T>: 分页查询响应格式                    │
│    - HealthCheckResponse: 健康检查响应格式                │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 1.3 技术栈

| 技术组件 | 版本 | 用途 |
|---------|------|------|
| .NET | 8.0 | 运行时框架 |
| Refit | 7.x | HTTP客户端代码生成器 |
| LYBT.Shared.Models | 1.0 | 共享DTO模型（前后端契约一致性） |
| System.ComponentModel | .NET 8 | Description特性标注 |

### 1.4 项目依赖

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Refit" />
  </ItemGroup>

  <ItemGroup>
    <!-- 唯一的项目引用：共享模型层 -->
    <ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
  </ItemGroup>
</Project>
```

**依赖原则**：
- ✅ **允许依赖**：Refit、LYBT.Shared.Models
- ❌ **禁止依赖**：任何业务模块（Auth、Patients、Herbs等）
- ✅ **被依赖**：所有业务模块依赖契约层

---

## 2. 模块架构

### 2.1 整体架构图

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Client端分层架构                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │  呈现层（Presentation Layer）                                │       │
│  │  - Views (XAML)                                             │       │
│  │  - ViewModels (MVVM)                                        │       │
│  │  - Commands, Converters                                     │       │
│  └───────────────────────┬─────────────────────────────────────┘       │
│                          │ 依赖                                        │
│                          ▼                                             │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │  契约层（Contracts Layer）⭐ 本文档的焦点                     │       │
│  │  - API接口（IAuthApi, IPatientApi等8个）                     │       │
│  │  - 服务接口（IPrescriptionEditorService）                    │       │
│  │  - 共享响应格式（ApiResponse, PagedResult）                  │       │
│  └───────────────────────┬─────────────────────────────────────┘       │
│                          │ 依赖                                        │
│                          ▼                                             │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │  共享模型层（Shared.Models Layer）                            │       │
│  │  - DTO (Data Transfer Objects)                              │       │
│  │  - Contracts (前后端契约)                                    │       │
│  │  - Common (通用类型)                                         │       │
│  └─────────────────────────────────────────────────────────────┘       │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

通信流程：
  ViewModel → IXxxApi (Refit) → HTTP → Server端API → Service → Repository → EF Core → Database
```

### 2.2 目录结构

```
LYBT.Desktop.Contracts/
├── Api/                          # API客户端接口（8个）
│   ├── IAuthApi.cs              # 认证API接口
│   ├── IPatientApi.cs           # 患者API接口
│   ├── IHerbApi.cs              # 药材API接口
│   ├── IFormulaApi.cs           # 验方API接口
│   ├── IUserApi.cs              # 用户API接口
│   ├── IConsultationApi.cs      # 诊疗API接口
│   ├── IPrescriptionApi.cs      # 处方API接口（Read-only）
│   └── IMedicalCaseApi.cs       # 医案API接口（聚合根）
│
├── Services/                     # 跨模块服务接口（1个）
│   └── IPrescriptionEditorService.cs  # 处方编辑器服务接口（依赖倒置）
│
└── LYBT.Desktop.Contracts.csproj  # 项目文件
```

---

## 3. API接口设计

### 3.1 API接口统一标准

**所有API接口必须遵循以下标准**：

1. **Refit特性标注**：
   ```csharp
   [Refit.Get("/api/v1/{module}")]
   [Refit.Post("/api/v1/{module}")]
   [Refit.Put("/api/v1/{module}/{id}")]
   [Refit.Delete("/api/v1/{module}/{id}")]
   ```

2. **统一响应格式**：
   ```csharp
   Task<ApiResponse<T>>              // 单个实体响应
   Task<ApiResponse<PagedResult<T>>> // 分页查询响应
   Task<ApiResponse>                 // 无数据响应（如删除操作）
   ```

3. **JWT认证标注**：
   ```csharp
   [Refit.Headers("Authorization: Bearer")]  // 需要认证的端点
   ```

4. **参数标注**：
   ```csharp
   [Refit.Query] int page = 1              // 查询参数（URL参数）
   [Refit.Body] PatientCreateDto request   // 请求体（JSON）
   ```

5. **描述性注释**：
   ```csharp
   /// <summary>
   /// 获取患者列表（支持分页和查询）
   /// </summary>
   /// <param name="page">页码（从1开始）</param>
   /// <param name="pageSize">每页记录数（默认20）</param>
   /// <param name="keyword">搜索关键字（可选）</param>
   /// <returns>分页患者列表</returns>
   ```

### 3.2 API接口示例：IAuthApi（认证API）

```csharp
using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 身份认证API客户端接口 - UltraThink统一API客户端标准
    /// </summary>
    /// <remarks>
    /// <para>功能范围: JWT身份认证、会话管理、密码操作、健康检查</para>
    /// <para>技术特性: Refit类型安全REST客户端、统一ApiResponse响应格式</para>
    /// <para>安全特性: JWT Bearer Token认证、8小时过期、Remember Me 30天</para>
    /// <para>架构定位: 前端WPF客户端与后端Web API的统一接口契约</para>
    /// </remarks>
    [Description("身份认证API客户端 - JWT认证、会话管理、安全操作")]
    public interface IAuthApi
    {
        /// <summary>
        /// 用户登录认证
        /// </summary>
        /// <param name="loginRequest">登录请求信息 - 包含用户名、密码、记住我选项</param>
        /// <returns>登录响应 - 包含JWT令牌、用户信息、过期时间</returns>
        /// <remarks>
        /// <para>功能: 验证用户凭据，生成JWT访问令牌和刷新令牌</para>
        /// <para>令牌: 访问令牌8小时有效期，刷新令牌30天(Remember Me)或1天</para>
        /// <para>安全: PBKDF2密码哈希验证、失败次数限制、IP地址记录</para>
        /// </remarks>
        [Refit.Post("/api/v1/auth/login")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>> LoginAsync(
            [Refit.Body] LoginRequest loginRequest);

        /// <summary>
        /// 用户登出操作
        /// </summary>
        /// <param name="logoutRequest">登出请求信息</param>
        /// <returns>登出结果确认</returns>
        /// <remarks>
        /// <para>功能: 使当前JWT令牌失效，清理服务端会话状态</para>
        /// <para>操作: 令牌加入黑名单、清理缓存、记录登出日志</para>
        /// <para>安全: 防止令牌被恶意使用，确保会话完全终止</para>
        /// </remarks>
        [Refit.Post("/api/v1/auth/logout")]
        [Refit.Headers("Authorization: Bearer")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse> LogoutAsync(
            [Refit.Body] LogoutRequest logoutRequest);

        /// <summary>
        /// 修改系统管理员密码
        /// </summary>
        /// <param name="changeSysAdminPassword">密码修改请求</param>
        /// <returns>密码修改结果</returns>
        /// <remarks>
        /// <para>功能: 修改系统管理员密码</para>
        /// <para>验证: 新密码强度检查，至少6位</para>
        /// <para>权限: 仅管理员角色可访问</para>
        /// </remarks>
        [Refit.Post("/api/v1/auth/changeSysAdminPassword")]
        [Refit.Headers("Authorization: Bearer")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse> ChangeSysAdminPasswordAsync(
            [Refit.Body] ChangeSysAdminPassword changeSysAdminPassword);

        /// <summary>
        /// 验证Token (GET方法)
        /// </summary>
        /// <returns>验证结果包含token有效性和用户信息</returns>
        /// <remarks>
        /// <para>功能: 从Authorization header中获取Bearer Token进行验证</para>
        /// <para>返回: Token有效性、用户信息和过期时间</para>
        /// </remarks>
        [Refit.Get("/api/v1/auth/validate")]
        [Refit.Headers("Authorization: Bearer")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>> ValidateTokenFromHeaderAsync();

        /// <summary>
        /// 验证Token (POST方法)
        /// </summary>
        /// <param name="token">要验证的Token</param>
        /// <returns>验证结果</returns>
        /// <remarks>
        /// <para>功能: 验证指定的Token是否有效</para>
        /// <para>用途: 用于无法使用Header的场景</para>
        /// </remarks>
        [Refit.Post("/api/v1/auth/validate")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<bool>> ValidateTokenAsync(
            [Refit.Body] string token);

        /// <summary>
        /// API服务健康状态检查
        /// </summary>
        /// <returns>健康检查响应</returns>
        /// <remarks>
        /// <para>功能: 检查API服务的可用性和响应时间</para>
        /// <para>用途: 客户端连接测试、服务监控、网络诊断</para>
        /// <para>响应: 返回服务状态信息，包含状态和时间戳，无需认证</para>
        /// </remarks>
        [Refit.Get("/api/v1/health")]
        Task<LYBT.Shared.Models.Contracts.Common.HealthCheckResponse> HealthCheckAsync();
    }
}
```

**关键特性说明**：
- ✅ **Refit路由**：`/api/v1/auth/{action}`（统一版本前缀）
- ✅ **JWT认证**：需要认证的端点标注`[Refit.Headers("Authorization: Bearer")]`
- ✅ **健康检查**：`HealthCheckAsync()`无需认证，用于服务可用性检测
- ✅ **描述性注释**：包含功能、技术特性、安全特性的详细说明

### 3.3 API接口示例：IPrescriptionApi（Read-only）

```csharp
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 处方API客户端接口 - Read-Only（Issue #1606）
    /// 所有Write操作已迁移至MedicalCaseController聚合根
    /// </summary>
    public interface IPrescriptionApi
    {
        /// <summary>
        /// 获取处方列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/prescriptions")]
        Task<ApiResponse<PagedResult<PrescriptionDto>>> GetPrescriptionsAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取处方详情
        /// </summary>
        [Refit.Get("/api/v1/prescriptions/{id}")]
        Task<ApiResponse<PrescriptionDto>> GetPrescriptionByIdAsync(Guid id);

        /// <summary>
        /// 根据医案ID获取处方列表
        /// </summary>
        [Refit.Get("/api/v1/prescriptions/medicalcase/{medicalCaseId}")]
        Task<ApiResponse<List<PrescriptionDto>>> GetPrescriptionsByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 获取患者最近处方列表 (Issue #1371 ENTRY-13)
        /// </summary>
        [Refit.Get("/api/v1/prescriptions/patient/{patientId}/recent")]
        Task<ApiResponse<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(
            Guid patientId,
            [Refit.Query] int count = 5);

        // ========== Write方法已删除（Issue #1606 Phase 1）==========
        // CreatePrescriptionAsync 已删除，请使用 POST /api/v1/medicalcases/with-details
        // UpdatePrescriptionAsync 已删除，请使用 PUT /api/v1/medicalcases/{id}/prescription
        // DeletePrescriptionAsync 已删除，请使用 DELETE /api/v1/medicalcases/{id}（级联删除）
        // SoftDeletePrescriptionAsync 已删除，请使用 DELETE /api/v1/medicalcases/{id}/soft
        // ImportFormulaIntoPrescriptionAsync 已删除，请使用 POST /api/v1/medicalcases/{id}/prescription/import-formula/{formulaId}
    }
}
```

**关键特性说明**：
- ⚠️ **Read-only约束**：符合Issue #1606聚合根模式，所有Write操作迁移至IMedicalCaseApi
- ✅ **查询专注**：专注于处方数据的查询操作（按ID、按医案ID、按患者ID、最近处方）
- ✅ **代码注释**：明确标注已删除的Write方法，并指导使用聚合根API

### 3.4 API接口示例：IMedicalCaseApi（聚合根）

```csharp
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 医疗案例API客户端接口 - 聚合根模式（Issue #1606）
    /// 负责医案、诊疗、处方的完整生命周期管理
    /// </summary>
    public interface IMedicalCaseApi
    {
        // ========== 基础CRUD操作 ==========

        /// <summary>
        /// 获取医疗案例列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/medicalcases")]
        Task<ApiResponse<PagedResult<MedicalCaseDto>>> GetMedicalCasesAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取医疗案例详情
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/{id}")]
        Task<ApiResponse<MedicalCaseDto>> GetMedicalCaseByIdAsync(Guid id);

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/by-patient/{patientId}")]
        Task<ApiResponse<List<MedicalCaseDto>>> GetMedicalCasesByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取完整的医疗案例（包含所有关联数据）
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/{id}/with-details")]
        Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        [Refit.Post("/api/v1/medicalcases")]
        Task<ApiResponse<MedicalCaseDto>> CreateMedicalCaseAsync(
            [Refit.Body] MedicalCaseCreateDto request);

        /// <summary>
        /// 创建完整的医疗案例（包含诊疗和可选处方）
        /// </summary>
        [Refit.Post("/api/v1/medicalcases/with-details")]
        Task<ApiResponse<MedicalCaseDto>> CreateMedicalCaseWithDetailsAsync(
            [Refit.Body] MedicalCaseWithDetailsCreateDto request);

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}")]
        Task<ApiResponse<MedicalCaseDto>> UpdateMedicalCaseAsync(
            Guid id,
            [Refit.Body] MedicalCaseUpdateDto request);

        /// <summary>
        /// 删除医疗案例（物理删除）
        /// </summary>
        [Refit.Delete("/api/v1/medicalcases/{id}")]
        Task<ApiResponse<ApiResponse>> DeleteMedicalCaseAsync(Guid id);

        /// <summary>
        /// 软删除医疗案例（标记为删除）
        /// Issue #1606 Phase 3 - 修复PrescriptionEditorViewModel软删除调用
        /// </summary>
        [Refit.Delete("/api/v1/medicalcases/{id}/soft")]
        Task<ApiResponse<ApiResponse>> SoftDeleteMedicalCaseAsync(Guid id);

        // ========== 聚合根专用方法（Issue #1606）==========

        /// <summary>
        /// 更新医案的诊断信息（聚合根方法）
        /// Issue #1563 - 修复ConsultationFormViewModel违反聚合根模式
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{medicalCaseId}/consultation")]
        Task<ApiResponse<ConsultationDto>> UpdateConsultationAsync(
            Guid medicalCaseId,
            [Refit.Body] ConsultationUpdateDto request);

        /// <summary>
        /// 为已存在的医案创建处方（Issue #1608补充）
        /// </summary>
        [Refit.Post("/api/v1/medicalcases/{medicalCaseId}/prescription")]
        Task<ApiResponse<PrescriptionDto>> CreatePrescriptionAsync(
            Guid medicalCaseId,
            [Refit.Body] PrescriptionCreateDto request);

        /// <summary>
        /// 更新医案的处方（Issue #1608补充）
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{medicalCaseId}/prescription")]
        Task<ApiResponse<PrescriptionDto>> UpdatePrescriptionAsync(
            Guid medicalCaseId,
            [Refit.Body] PrescriptionUpdateDto request);

        /// <summary>
        /// 删除医案的处方（Issue #1608补充）
        /// </summary>
        [Refit.Delete("/api/v1/medicalcases/{medicalCaseId}/prescription")]
        Task<ApiResponse> DeletePrescriptionAsync(Guid medicalCaseId);

        /// <summary>
        /// 从配方导入处方
        /// Epic #1589 Phase 4 - 架构合规版本
        /// </summary>
        [Refit.Post("/api/v1/medicalcases/{medicalCaseId}/prescription/import-formula/{formulaId}")]
        Task<ApiResponse<PrescriptionDto>> ImportFormulaIntoPrescriptionAsync(
            Guid medicalCaseId,
            Guid formulaId);

        /// <summary>
        /// 清空处方内容（保留处方框架）
        /// Epic #1589 Phase 4 - 架构合规版本
        /// </summary>
        [Refit.Delete("/api/v1/medicalcases/{medicalCaseId}/prescription/clear")]
        Task<ApiResponse> ClearPrescriptionAsync(Guid medicalCaseId);

        // ========== 三步工作流辅助方法（Epic #1589）==========

        /// <summary>
        /// 完成辩证步骤（Step 1）
        /// Epic #1589 Phase 1 - 架构合规版本
        /// </summary>
        [Refit.Post("/api/v1/medicalcases/{medicalCaseId}/complete-step1")]
        Task<ApiResponse<ConsultationStepDto>> CompleteStep1Async(
            Guid medicalCaseId,
            [Refit.Body] CompleteStep1Request request);

        /// <summary>
        /// 重置诊疗步骤
        /// Epic #1589 Phase 2 - 架构合规版本
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{medicalCaseId}/reset-consultation-steps")]
        Task<ApiResponse> ResetConsultationStepsAsync(Guid medicalCaseId);

        /// <summary>
        /// 暂存病案（保存当前状态）
        /// Epic #1589 Phase 5 - 架构合规版本
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{medicalCaseId}/save-as-draft")]
        Task<ApiResponse<MedicalCaseDto>> SaveAsDraftAsync(
            Guid medicalCaseId,
            [Refit.Body] MedicalCaseUpdateDto request);

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/patient/{patientId}/unfinished")]
        Task<ApiResponse<MedicalCaseDto>> GetUnfinishedCaseByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 关闭病案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}/close")]
        Task<ApiResponse> CloseCaseAsync(Guid id);

        /// <summary>
        /// 标记是否开处方
        /// Task 3.4 (#1661): RadioBox变化时自动保存
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{medicalCaseId}/prescription-flag")]
        Task<ApiResponse<MedicalCaseDto>> SetPrescriptionFlagAsync(
            Guid medicalCaseId,
            [Refit.Body] SetPrescriptionFlagRequest request);
    }
}
```

**关键特性说明**：
- ⭐ **聚合根模式**：医案、诊疗、处方的Write操作统一由此接口管理
- ✅ **完整生命周期**：从创建、更新、删除到三步工作流、暂存、关闭
- ✅ **Issue标注**：每个方法标注关联的Issue编号，便于追溯需求和架构决策

### 3.5 标准CRUD接口模式

**所有业务模块的API接口遵循统一的CRUD模式**：

```csharp
public interface I{Module}Api
{
    // ========== Read操作 ==========
    /// <summary>
    /// 获取列表（支持分页和查询）
    /// </summary>
    [Refit.Get("/api/v1/{module}")]
    Task<ApiResponse<PagedResult<{Module}Dto>>> Get{Module}sAsync(
        [Refit.Query] int page = 1,
        [Refit.Query] int pageSize = 20,
        [Refit.Query] string? keyword = null);

    /// <summary>
    /// 获取详情
    /// </summary>
    [Refit.Get("/api/v1/{module}/{id}")]
    Task<ApiResponse<{Module}Dto>> Get{Module}ByIdAsync(Guid id);

    // ========== Write操作 ==========
    /// <summary>
    /// 创建
    /// </summary>
    [Refit.Post("/api/v1/{module}")]
    Task<ApiResponse<{Module}Dto>> Create{Module}Async(
        [Refit.Body] {Module}CreateDto request);

    /// <summary>
    /// 更新
    /// </summary>
    [Refit.Put("/api/v1/{module}/{id}")]
    Task<ApiResponse<{Module}Dto>> Update{Module}Async(
        Guid id,
        [Refit.Body] {Module}UpdateDto request);

    /// <summary>
    /// 删除（软删除或物理删除）
    /// </summary>
    [Refit.Delete("/api/v1/{module}/{id}")]
    Task<ApiResponse<ApiResponse>> Delete{Module}Async(Guid id);
}
```

**模式优势**：
- ✅ **一致性**：所有模块遵循相同的API设计模式
- ✅ **可预测性**：开发者可以轻松推断任何模块的API结构
- ✅ **可维护性**：统一模式降低维护成本
- ✅ **易学性**：新开发者快速上手

---

## 4. 服务接口设计

### 4.1 服务接口概述

**服务接口**用于跨模块通信，解决模块间的循环依赖问题（依赖倒置原则）。

**当前唯一的服务接口**：
- `IPrescriptionEditorService`：处方编辑器服务接口（Epic #1540 方案B - 包装模式）

### 4.2 IPrescriptionEditorService（依赖倒置示例）

```csharp
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 处方编辑器服务接口（Epic #1540 方案B - 包装模式）
    ///
    /// 设计目标：
    /// 1. 依赖倒置：MedicalCase模块依赖此接口，Prescriptions模块实现此接口
    /// 2. 解除循环依赖：MedicalCase ↔ Prescriptions的循环依赖通过接口解耦
    /// 3. 代码复用：包装PrescriptionViewModel的完整功能（969行）
    ///
    /// 架构定位（与Issue #1477协调）：
    /// - 功能分层：辅助层功能（处方编辑器辅助工具）
    /// - 查询层：LoadRecentPrescriptionsAsync、LoadAllHerbsAsync
    /// - 辅助层：ImportFormulaAsync、FilterHerbs、BuildPrescriptionDraftAsync
    /// - 写入控制：提供草稿构建能力，最终写入由MedicalCase聚合根控制
    ///
    /// 符合SOLID原则：
    /// - S: 单一职责（仅处方编辑器相关功能）
    /// - O: 开闭原则（接口稳定，实现可扩展）
    /// - L: 里氏替换原则（任何实现都可替换）
    /// - I: 接口隔离原则（接口方法专注于处方编辑）
    /// - D: 依赖倒置原则（高层依赖抽象，低层实现抽象）
    /// </summary>
    public interface IPrescriptionEditorService
    {
        #region 1. 药材数据管理

        /// <summary>
        /// 加载所有药材数据
        /// 用途：初始化处方编辑器，提供药材选择列表
        /// </summary>
        /// <returns>所有药材DTO列表</returns>
        Task<IEnumerable<HerbDto>> LoadAllHerbsAsync();

        /// <summary>
        /// 过滤药材（支持拼音码模糊匹配）
        /// 用途：ComboBox实时搜索，支持拼音码快速定位
        /// </summary>
        /// <param name="searchText">搜索文本（药材名称或拼音码）</param>
        /// <returns>匹配的药材列表</returns>
        IEnumerable<HerbDto> FilterHerbs(string searchText);

        #endregion

        #region 2. 历史处方管理

        /// <summary>
        /// 加载患者的最近处方记录
        /// 用途：处方复用，快速调取患者历史处方
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="limit">返回记录数限制（默认10条）</param>
        /// <returns>处方搜索结果列表</returns>
        Task<IEnumerable<PrescriptionSearchResultDto>> LoadRecentPrescriptionsAsync(
            Guid patientId,
            int limit = 10);

        #endregion

        #region 3. 验方导入

        /// <summary>
        /// 加载所有验方数据
        /// 用途：验方导入对话框，提供验方选择列表
        /// </summary>
        /// <returns>所有验方DTO列表</returns>
        Task<IEnumerable<FormulaDto>> LoadFormulasAsync();

        /// <summary>
        /// 从验方导入处方数据（草稿构建）
        /// 用途：将验方的药材组成转换为处方项目
        /// 注意：此方法构建草稿，最终写入由MedicalCase聚合根控制
        /// </summary>
        /// <param name="formulaId">验方ID</param>
        /// <returns>处方数据DTO（包含从验方转换的处方项目）</returns>
        Task<PrescriptionDto> ImportFormulaAsync(Guid formulaId);

        #endregion

        #region 4. 处方数据操作

        /// <summary>
        /// 构建处方草稿（Issue #1477协调：强调草稿构建而非直接写入）
        /// 用途：将处方编辑器的数据转换为处方DTO，供MedicalCase聚合根使用
        /// 注意：此方法仅构建草稿，不执行数据库写入，最终写入由MedicalCase控制
        /// </summary>
        /// <param name="dto">处方创建DTO</param>
        /// <returns>处方数据DTO（草稿）</returns>
        Task<PrescriptionDto> BuildPrescriptionDraftAsync(PrescriptionCreateDto dto);

        /// <summary>
        /// 验证处方数据完整性
        /// 用途：保存前的数据验证（药材重复检查、必填项检查）
        /// </summary>
        /// <param name="prescription">处方数据DTO</param>
        /// <returns>验证是否通过</returns>
        Task<bool> ValidatePrescriptionAsync(PrescriptionDto prescription);

        /// <summary>
        /// 计算处方总金额
        /// 用途：实时计算并显示处方总金额（单帖价格 × 剂数）
        /// </summary>
        /// <param name="items">处方项目列表</param>
        /// <param name="dosageCount">剂数</param>
        /// <param name="discount">折扣（默认1.0）</param>
        /// <returns>总金额</returns>
        Task<decimal> CalculateTotalAmountAsync(
            IEnumerable<PrescriptionItemDto> items,
            int dosageCount = 7,
            decimal discount = 1.0m);

        #endregion

        #region 5. 事件通知

        /// <summary>
        /// 处方数据变更事件
        /// 用途：通知订阅者处方数据已变更（如MedicalCaseFlowViewModel）
        /// </summary>
        event EventHandler<PrescriptionChangedEventArgs>? PrescriptionChanged;

        #endregion
    }

    /// <summary>
    /// 处方变更事件参数
    /// </summary>
    public class PrescriptionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 变更的处方数据
        /// </summary>
        public PrescriptionDto? Prescription { get; set; }

        /// <summary>
        /// 变更类型（Created, Updated, Deleted）
        /// </summary>
        public PrescriptionChangeType ChangeType { get; set; }

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 处方变更类型
    /// </summary>
    public enum PrescriptionChangeType
    {
        /// <summary>创建</summary>
        Created,

        /// <summary>更新</summary>
        Updated,

        /// <summary>删除</summary>
        Deleted
    }
}
```

### 4.3 依赖倒置模式图解

```
┌─────────────────────────────────────────────────────────────────┐
│                  依赖倒置模式（Epic #1540）                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  问题：MedicalCase模块需要调用Prescriptions模块的处方编辑功能     │
│  结果：MedicalCase → Prescriptions（循环依赖）                   │
│                                                                 │
│  ┌─────────────┐       依赖        ┌───────────────────┐        │
│  │ MedicalCase ├──────────────────>│ Prescriptions     │        │
│  │ Module      │<─────────────────┤ Module            │        │
│  └─────────────┘       依赖        └───────────────────┘        │
│         ❌ 循环依赖问题                                          │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  解决方案：通过IPrescriptionEditorService接口解耦                │
│                                                                 │
│  ┌─────────────┐       依赖        ┌──────────────────────┐    │
│  │ MedicalCase ├──────────────────>│ IPrescription        │    │
│  │ Module      │                   │ EditorService        │    │
│  └─────────────┘                   │ (Contracts)          │    │
│                                    └──────────────────────┘    │
│                                             ▲                   │
│                                             │ 实现               │
│                                    ┌────────┴──────────┐        │
│                                    │ Prescriptions     │        │
│                                    │ Module            │        │
│                                    └───────────────────┘        │
│         ✅ 依赖倒置，无循环依赖                                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**依赖倒置的核心价值**：
- ✅ **解除循环依赖**：MedicalCase和Prescriptions模块解耦
- ✅ **高层依赖抽象**：MedicalCase依赖IPrescriptionEditorService接口
- ✅ **低层实现抽象**：Prescriptions模块实现IPrescriptionEditorService接口
- ✅ **易于测试**：可以Mock IPrescriptionEditorService进行单元测试
- ✅ **易于扩展**：可以替换不同的实现（如Avalonia版本的实现）

---

## 5. Refit客户端配置

### 5.1 Refit简介

**Refit**是一个类型安全的REST客户端代码生成器，通过接口定义自动生成HTTP调用代码。

**核心优势**：
- ✅ **类型安全**：编译时检查API契约
- ✅ **代码简洁**：无需手写HttpClient调用代码
- ✅ **易于测试**：接口可以轻松Mock
- ✅ **错误处理**：统一的异常处理机制

### 5.2 Refit客户端注册

**在Shell项目的App.xaml.cs中注册Refit客户端**：

```csharp
using Refit;
using LYBT.Desktop.Contracts.Api;
using System;

namespace LYBT.Desktop.Shell
{
    public partial class App
    {
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // ========== API客户端注册 ==========
            // 配置Refit客户端（带JWT认证）
            var refitSettings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    })
            };

            // 注册API客户端（Singleton）
            containerRegistry.RegisterSingleton<IAuthApi>(() =>
            {
                var httpClient = new HttpClient(new AuthenticatedHttpClientHandler())
                {
                    BaseAddress = new Uri("https://localhost:5001")
                };
                return RestService.For<IAuthApi>(httpClient, refitSettings);
            });

            containerRegistry.RegisterSingleton<IPatientApi>(() =>
            {
                var httpClient = new HttpClient(new AuthenticatedHttpClientHandler())
                {
                    BaseAddress = new Uri("https://localhost:5001")
                };
                return RestService.For<IPatientApi>(httpClient, refitSettings);
            });

            containerRegistry.RegisterSingleton<IHerbApi>(() =>
            {
                var httpClient = new HttpClient(new AuthenticatedHttpClientHandler())
                {
                    BaseAddress = new Uri("https://localhost:5001")
                };
                return RestService.For<IHerbApi>(httpClient, refitSettings);
            });

            // ... 其他API客户端类似注册 ...
        }
    }

    /// <summary>
    /// HTTP客户端处理器（自动添加JWT Token到Authorization Header）
    /// </summary>
    public class AuthenticatedHttpClientHandler : HttpClientHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // 从本地存储获取JWT Token
            var token = SecureStorage.GetAsync("jwt_token").Result;

            if (!string.IsNullOrEmpty(token))
            {
                // 添加Authorization Header
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
```

**关键配置说明**：
- ✅ **BaseAddress**：API服务器地址（https://localhost:5001）
- ✅ **RefitSettings**：JSON序列化配置（忽略null值、大小写不敏感）
- ✅ **AuthenticatedHttpClientHandler**：自动添加JWT Token到Authorization Header
- ✅ **Singleton生命周期**：API客户端全局单例，提高性能

### 5.3 简化注册（循环注册）

```csharp
/// <summary>
/// 简化API客户端注册（批量注册）
/// </summary>
private void RegisterApiClients(IContainerRegistry containerRegistry)
{
    var refitSettings = new RefitSettings
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            })
    };

    // 定义所有API接口类型
    var apiTypes = new[]
    {
        typeof(IAuthApi),
        typeof(IPatientApi),
        typeof(IHerbApi),
        typeof(IFormulaApi),
        typeof(IUserApi),
        typeof(IConsultationApi),
        typeof(IPrescriptionApi),
        typeof(IMedicalCaseApi)
    };

    // 批量注册
    foreach (var apiType in apiTypes)
    {
        containerRegistry.RegisterSingleton(apiType, sp =>
        {
            var httpClient = new HttpClient(new AuthenticatedHttpClientHandler())
            {
                BaseAddress = new Uri("https://localhost:5001")
            };
            return RestService.For(apiType, httpClient, refitSettings);
        });
    }
}
```

---

## 6. 依赖注入注册

### 6.1 服务接口注册

**服务接口由实现模块注册**：

```csharp
// 在Prescriptions模块中注册实现
public class PrescriptionsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Epic #1540: 注册处方编辑器服务（方案B - 包装模式）
        // 实现依赖倒置：MedicalCase模块依赖IPrescriptionEditorService接口
        containerRegistry.RegisterSingleton<IPrescriptionEditorService, PrescriptionEditorService>();
    }
}
```

### 6.2 ViewModel中使用API客户端

```csharp
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.ViewModels
{
    public class PatientListViewModel : BindableBase
    {
        private readonly IPatientApi _patientApi;

        // 构造函数注入
        public PatientListViewModel(IPatientApi patientApi)
        {
            _patientApi = patientApi;
        }

        // 使用API客户端
        public async Task LoadPatientsAsync()
        {
            var response = await _patientApi.GetPatientsAsync(page: 1, pageSize: 20);

            if (response.Success)
            {
                Patients = new ObservableCollection<PatientDto>(response.Data.Items);
            }
            else
            {
                // 错误处理
                MessageBox.Show($"加载失败：{response.Message}");
            }
        }
    }
}
```

---

## 7. 依赖倒置模式

### 7.1 依赖倒置原则（DIP）

**Dependency Inversion Principle**：
- **高层模块**不应该依赖**低层模块**，两者都应该依赖**抽象**
- **抽象**不应该依赖**细节**，**细节**应该依赖**抽象**

### 7.2 IPrescriptionEditorService依赖倒置实例

**问题场景**：
- MedicalCase模块（高层）需要调用Prescriptions模块（低层）的处方编辑功能
- 直接依赖会导致循环依赖：MedicalCase → Prescriptions → MedicalCase

**解决方案（Epic #1540 方案B）**：
1. **定义接口**：在Contracts层定义`IPrescriptionEditorService`接口
2. **高层依赖接口**：MedicalCase模块依赖`IPrescriptionEditorService`接口
3. **低层实现接口**：Prescriptions模块实现`IPrescriptionEditorService`接口
4. **DI容器注入**：在Prescriptions模块注册实现

**依赖关系图**：

```
┌───────────────────────────────────────────────────────────────────┐
│                     依赖倒置模式（DIP）                            │
├───────────────────────────────────────────────────────────────────┤
│                                                                   │
│  传统依赖（❌ 循环依赖）：                                          │
│                                                                   │
│    MedicalCase Module (高层)                                     │
│         │                                                        │
│         │ 依赖                                                    │
│         ▼                                                        │
│    Prescriptions Module (低层)                                   │
│         │                                                        │
│         │ 依赖（如访问MedicalCaseService）                         │
│         ▼                                                        │
│    MedicalCase Module (高层)                                     │
│                                                                   │
│    结果：❌ 循环依赖，编译失败                                      │
│                                                                   │
├───────────────────────────────────────────────────────────────────┤
│                                                                   │
│  依赖倒置（✅ 解除循环依赖）：                                      │
│                                                                   │
│    MedicalCase Module (高层)                                     │
│         │                                                        │
│         │ 依赖（接口）                                             │
│         ▼                                                        │
│    IPrescriptionEditorService (抽象 - Contracts层)               │
│         ▲                                                        │
│         │ 实现                                                    │
│         │                                                        │
│    Prescriptions Module (低层)                                   │
│                                                                   │
│    结果：✅ 高层依赖抽象，低层实现抽象，无循环依赖                   │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘
```

### 7.3 其他依赖倒置场景

**未来可能的服务接口**：
- `IConsultationEditorService`：诊疗编辑器服务接口
- `IMedicalCaseFlowService`：病案流程服务接口
- `IReportGeneratorService`：报表生成器服务接口

**设计原则**：
- ✅ 接口定义在Contracts层
- ✅ 实现由具体模块提供
- ✅ 高层模块依赖接口
- ✅ 低层模块实现接口

---

## 8. 核心设计原则

### 8.1 接口隔离原则（ISP）

**Interface Segregation Principle**：
- 客户端不应该依赖它不需要的接口
- 将大接口拆分为多个小接口，每个接口专注于特定功能

**实践案例**：
- ✅ `IAuthApi`：只包含认证相关方法（Login, Logout, ChangePassword, Validate）
- ✅ `IPatientApi`：只包含患者CRUD方法
- ✅ `IPrescriptionEditorService`：只包含处方编辑器辅助方法

**反例（避免）**：
- ❌ `IUniversalApi`：包含所有模块的CRUD方法（过于庞大，违反ISP）

### 8.2 开闭原则（OCP）

**Open-Closed Principle**：
- 软件实体应该对扩展开放，对修改封闭

**实践案例**：
- ✅ **API接口稳定**：一旦发布，API接口不轻易修改
- ✅ **实现可扩展**：可以替换不同的实现（如Mock实现、测试实现）
- ✅ **新功能通过新方法添加**：而不是修改现有方法签名

**示例**：
```csharp
// ❌ 错误：修改现有方法签名
public interface IPatientApi
{
    // 原方法
    // Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id);

    // 修改后（破坏现有代码）
    Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id, bool includeHistory);
}

// ✅ 正确：添加新方法
public interface IPatientApi
{
    // 保留原方法
    Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id);

    // 新增方法（扩展功能）
    Task<ApiResponse<PatientDto>> GetPatientByIdWithHistoryAsync(Guid id);
}
```

### 8.3 依赖倒置原则（DIP）

**Dependency Inversion Principle**：
- 高层模块不应该依赖低层模块，两者都应该依赖抽象
- 抽象不应该依赖细节，细节应该依赖抽象

**实践案例**：
- ✅ `IPrescriptionEditorService`：MedicalCase模块依赖接口，Prescriptions模块实现接口
- ✅ API接口：ViewModel依赖IXxxApi接口，Refit提供实现

### 8.4 单一职责原则（SRP）

**Single Responsibility Principle**：
- 一个类或模块应该只有一个变化的原因

**实践案例**：
- ✅ **API接口按模块拆分**：IAuthApi（认证）、IPatientApi（患者）、IHerbApi（药材）
- ✅ **服务接口单一职责**：IPrescriptionEditorService只负责处方编辑器辅助功能
- ✅ **Contracts层零业务逻辑**：只定义接口，不包含实现

### 8.5 里氏替换原则（LSP）

**Liskov Substitution Principle**：
- 子类型必须能够替换其基类型

**实践案例**：
- ✅ **接口实现可替换**：任何实现IPrescriptionEditorService的类都可以替换
- ✅ **Mock测试**：可以使用Mock实现替换真实实现进行单元测试

**示例**：
```csharp
// 真实实现
public class PrescriptionEditorService : IPrescriptionEditorService
{
    // 真实的数据库查询和业务逻辑
}

// Mock实现（单元测试）
public class MockPrescriptionEditorService : IPrescriptionEditorService
{
    // 返回预定义的测试数据
}

// 使用（符合LSP）
IPrescriptionEditorService service = new PrescriptionEditorService(); // 生产环境
IPrescriptionEditorService service = new MockPrescriptionEditorService(); // 测试环境
```

### 8.6 契约一致性原则

**Contract Consistency Principle**（自定义原则）：
- 前后端API契约必须100%一致
- 使用共享DTO模型（LYBT.Shared.Models）确保一致性

**实践案例**：
- ✅ **共享DTO**：PatientDto、HerbDto等DTO在前后端共享
- ✅ **统一响应格式**：ApiResponse<T>、PagedResult<T>在前后端共享
- ✅ **Refit接口对齐**：IPatientApi接口与Server端PatientController完全对齐

**对齐检查清单**：
| 检查项 | 前端（Refit） | 后端（Controller） | 一致性 |
|-------|-------------|------------------|--------|
| 路由 | `/api/v1/patients` | `[Route("api/v1/patients")]` | ✅ |
| HTTP方法 | `[Refit.Get]` | `[HttpGet]` | ✅ |
| 请求DTO | `PatientCreateDto` | `PatientCreateDto` | ✅ |
| 响应DTO | `ApiResponse<PatientDto>` | `ApiResponse<PatientDto>` | ✅ |
| 参数类型 | `Guid id` | `Guid id` | ✅ |

---

## 9. 集成与使用

### 9.1 ViewModel中使用API客户端

**标准使用模式**：

```csharp
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Patients;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.Patients.ViewModels
{
    public class PatientListViewModel : BindableBase
    {
        private readonly IPatientApi _patientApi;
        private ObservableCollection<PatientDto> _patients;

        // ========== 构造函数（依赖注入）==========
        public PatientListViewModel(IPatientApi patientApi)
        {
            _patientApi = patientApi ?? throw new ArgumentNullException(nameof(patientApi));

            // 初始化Commands
            LoadPatientsCommand = new DelegateCommand(async () => await LoadPatientsAsync());
            CreatePatientCommand = new DelegateCommand<PatientCreateDto>(async (dto) => await CreatePatientAsync(dto));
        }

        // ========== Properties ==========
        public ObservableCollection<PatientDto> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        // ========== Commands ==========
        public DelegateCommand LoadPatientsCommand { get; }
        public DelegateCommand<PatientCreateDto> CreatePatientCommand { get; }

        // ========== Methods ==========

        /// <summary>
        /// 加载患者列表（分页查询）
        /// </summary>
        private async Task LoadPatientsAsync()
        {
            try
            {
                var response = await _patientApi.GetPatientsAsync(page: 1, pageSize: 20);

                if (response.Success)
                {
                    Patients = new ObservableCollection<PatientDto>(response.Data.Items);
                }
                else
                {
                    // 错误处理
                    MessageBox.Show($"加载失败：{response.Message}");
                }
            }
            catch (Exception ex)
            {
                // 异常处理
                MessageBox.Show($"加载异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 创建患者
        /// </summary>
        private async Task CreatePatientAsync(PatientCreateDto dto)
        {
            try
            {
                var response = await _patientApi.CreatePatientAsync(dto);

                if (response.Success)
                {
                    // 刷新列表
                    await LoadPatientsAsync();

                    MessageBox.Show("创建成功");
                }
                else
                {
                    // 错误处理
                    MessageBox.Show($"创建失败：{response.Message}");
                }
            }
            catch (Exception ex)
            {
                // 异常处理
                MessageBox.Show($"创建异常：{ex.Message}");
            }
        }
    }
}
```

### 9.2 服务接口使用

**MedicalCase模块中使用IPrescriptionEditorService**：

```csharp
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    public class MedicalCaseFlowViewModel : BindableBase
    {
        private readonly IPrescriptionEditorService _prescriptionEditorService;

        // 构造函数注入（依赖倒置）
        public MedicalCaseFlowViewModel(IPrescriptionEditorService prescriptionEditorService)
        {
            _prescriptionEditorService = prescriptionEditorService
                ?? throw new ArgumentNullException(nameof(prescriptionEditorService));

            // 订阅处方变更事件
            _prescriptionEditorService.PrescriptionChanged += OnPrescriptionChanged;
        }

        // 使用处方编辑器服务
        private async Task OpenPrescriptionEditorAsync()
        {
            // 加载药材数据
            var herbs = await _prescriptionEditorService.LoadAllHerbsAsync();

            // 加载历史处方
            var recentPrescriptions = await _prescriptionEditorService.LoadRecentPrescriptionsAsync(
                patientId: CurrentPatient.Id,
                limit: 5);

            // 构建处方草稿
            var draft = await _prescriptionEditorService.BuildPrescriptionDraftAsync(
                new PrescriptionCreateDto
                {
                    PatientId = CurrentPatient.Id,
                    DoctorId = CurrentDoctor.Id,
                    Items = new List<PrescriptionItemCreateDto>()
                });

            // 最终写入由MedicalCase聚合根控制
            await _medicalCaseApi.CreatePrescriptionAsync(CurrentMedicalCase.Id, draft);
        }

        // 处理处方变更事件
        private void OnPrescriptionChanged(object? sender, PrescriptionChangedEventArgs e)
        {
            if (e.ChangeType == PrescriptionChangeType.Created)
            {
                // 刷新UI
                MessageBox.Show($"处方已创建：{e.Prescription?.Id}");
            }
        }
    }
}
```

---

## 10. 测试策略

### 10.1 API接口Mock测试

**使用Moq框架Mock API接口**：

```csharp
using Moq;
using Xunit;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Patients.Tests
{
    public class PatientListViewModelTests
    {
        [Fact]
        public async Task LoadPatientsAsync_Success_ShouldPopulatePatients()
        {
            // Arrange
            var mockApi = new Mock<IPatientApi>();
            mockApi.Setup(api => api.GetPatientsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new ApiResponse<PagedResult<PatientDto>>
                {
                    Success = true,
                    Data = new PagedResult<PatientDto>
                    {
                        Items = new List<PatientDto>
                        {
                            new PatientDto { Id = Guid.NewGuid(), Name = "患者1" },
                            new PatientDto { Id = Guid.NewGuid(), Name = "患者2" }
                        },
                        TotalCount = 2,
                        CurrentPage = 1,
                        PageSize = 20
                    }
                });

            var viewModel = new PatientListViewModel(mockApi.Object);

            // Act
            await viewModel.LoadPatientsAsync();

            // Assert
            Assert.NotNull(viewModel.Patients);
            Assert.Equal(2, viewModel.Patients.Count);
            Assert.Equal("患者1", viewModel.Patients[0].Name);
        }

        [Fact]
        public async Task CreatePatientAsync_Failure_ShouldShowErrorMessage()
        {
            // Arrange
            var mockApi = new Mock<IPatientApi>();
            mockApi.Setup(api => api.CreatePatientAsync(It.IsAny<PatientCreateDto>()))
                .ReturnsAsync(new ApiResponse<PatientDto>
                {
                    Success = false,
                    Message = "创建失败：患者姓名重复"
                });

            var viewModel = new PatientListViewModel(mockApi.Object);

            // Act
            await viewModel.CreatePatientAsync(new PatientCreateDto { Name = "患者1" });

            // Assert
            // 验证错误消息显示（实际项目中需要Mock MessageBox）
        }
    }
}
```

### 10.2 服务接口Mock测试

**Mock IPrescriptionEditorService**：

```csharp
using Moq;
using Xunit;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Tests
{
    public class MedicalCaseFlowViewModelTests
    {
        [Fact]
        public async Task OpenPrescriptionEditor_ShouldLoadHerbsAndHistoryPrescriptions()
        {
            // Arrange
            var mockService = new Mock<IPrescriptionEditorService>();
            mockService.Setup(s => s.LoadAllHerbsAsync())
                .ReturnsAsync(new List<HerbDto>
                {
                    new HerbDto { Id = Guid.NewGuid(), Name = "黄芪" },
                    new HerbDto { Id = Guid.NewGuid(), Name = "党参" }
                });

            mockService.Setup(s => s.LoadRecentPrescriptionsAsync(It.IsAny<Guid>(), It.IsAny<int>()))
                .ReturnsAsync(new List<PrescriptionSearchResultDto>
                {
                    new PrescriptionSearchResultDto { Id = Guid.NewGuid(), PatientName = "患者1" }
                });

            var viewModel = new MedicalCaseFlowViewModel(mockService.Object);

            // Act
            await viewModel.OpenPrescriptionEditorAsync();

            // Assert
            mockService.Verify(s => s.LoadAllHerbsAsync(), Times.Once);
            mockService.Verify(s => s.LoadRecentPrescriptionsAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Once);
        }
    }
}
```

### 10.3 集成测试

**使用真实API服务器进行集成测试**：

```csharp
using Xunit;
using Refit;
using LYBT.Desktop.Contracts.Api;

namespace LYBT.Desktop.Integration.Tests
{
    public class PatientApiIntegrationTests
    {
        private readonly IPatientApi _patientApi;

        public PatientApiIntegrationTests()
        {
            // 配置真实API客户端
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:5001")
            };

            _patientApi = RestService.For<IPatientApi>(httpClient);
        }

        [Fact]
        public async Task GetPatientsAsync_ShouldReturnPagedResult()
        {
            // Act
            var response = await _patientApi.GetPatientsAsync(page: 1, pageSize: 20);

            // Assert
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.NotEmpty(response.Data.Items);
        }

        [Fact]
        public async Task CreateAndDeletePatient_ShouldSucceed()
        {
            // Arrange
            var createDto = new PatientCreateDto
            {
                Name = "测试患者",
                Gender = Gender.Male,
                Phone = "13800138000"
            };

            // Act - Create
            var createResponse = await _patientApi.CreatePatientAsync(createDto);
            Assert.True(createResponse.Success);

            var patientId = createResponse.Data.Id;

            // Act - Delete
            var deleteResponse = await _patientApi.DeletePatientAsync(patientId);
            Assert.True(deleteResponse.Success);
        }
    }
}
```

---

## 11. 最佳实践

### 11.1 API接口设计最佳实践

1. **✅ 使用统一的响应格式**：
   ```csharp
   Task<ApiResponse<T>>              // 单个实体
   Task<ApiResponse<PagedResult<T>>> // 分页查询
   Task<ApiResponse>                 // 无数据响应
   ```

2. **✅ 使用统一的路由前缀**：
   ```csharp
   [Refit.Get("/api/v1/{module}")]  // /api/v1/patients
   ```

3. **✅ 使用统一的参数标注**：
   ```csharp
   [Refit.Query] int page = 1      // 查询参数
   [Refit.Body] PatientCreateDto   // 请求体
   ```

4. **✅ 使用统一的JWT认证标注**：
   ```csharp
   [Refit.Headers("Authorization: Bearer")]
   ```

5. **✅ 使用详细的XML注释**：
   ```csharp
   /// <summary>
   /// 获取患者列表（支持分页和查询）
   /// </summary>
   /// <param name="page">页码（从1开始）</param>
   /// <param name="pageSize">每页记录数（默认20）</param>
   /// <param name="keyword">搜索关键字（可选）</param>
   /// <returns>分页患者列表</returns>
   ```

### 11.2 服务接口设计最佳实践

1. **✅ 接口单一职责**：
   - 每个接口只负责一个功能领域
   - 例如：IPrescriptionEditorService只负责处方编辑器辅助功能

2. **✅ 方法命名清晰**：
   - 使用动词+名词的命名方式
   - 例如：`LoadAllHerbsAsync()`、`BuildPrescriptionDraftAsync()`

3. **✅ 异步方法后缀Async**：
   - 所有异步方法必须以`Async`结尾
   - 例如：`LoadAllHerbsAsync()`、`ValidatePrescriptionAsync()`

4. **✅ 事件通知机制**：
   - 使用事件通知订阅者数据变更
   - 例如：`event EventHandler<PrescriptionChangedEventArgs>? PrescriptionChanged;`

5. **✅ 草稿构建而非直接写入**：
   - 辅助服务只提供草稿构建能力
   - 最终写入由聚合根控制
   - 例如：`BuildPrescriptionDraftAsync()`而非`SavePrescriptionAsync()`

### 11.3 依赖注入最佳实践

1. **✅ 使用构造函数注入**：
   ```csharp
   public PatientListViewModel(IPatientApi patientApi)
   {
       _patientApi = patientApi ?? throw new ArgumentNullException(nameof(patientApi));
   }
   ```

2. **✅ 验证依赖非空**：
   ```csharp
   _patientApi = patientApi ?? throw new ArgumentNullException(nameof(patientApi));
   ```

3. **✅ 使用Singleton生命周期（API客户端）**：
   ```csharp
   containerRegistry.RegisterSingleton<IPatientApi>(() => { ... });
   ```

4. **❌ 禁止Service Locator模式**：
   ```csharp
   // ❌ 错误
   var patientApi = Container.Resolve<IPatientApi>();

   // ✅ 正确
   public PatientListViewModel(IPatientApi patientApi) { ... }
   ```

### 11.4 错误处理最佳实践

1. **✅ 统一错误处理**：
   ```csharp
   try
   {
       var response = await _patientApi.GetPatientsAsync();

       if (response.Success)
       {
           // 成功处理
       }
       else
       {
           // 错误处理（业务错误）
           MessageBox.Show($"加载失败：{response.Message}");
       }
   }
   catch (Exception ex)
   {
       // 异常处理（系统异常）
       MessageBox.Show($"加载异常：{ex.Message}");
   }
   ```

2. **✅ 区分业务错误和系统异常**：
   - `response.Success == false`：业务错误（如验证失败、数据不存在）
   - `catch (Exception ex)`：系统异常（如网络错误、序列化失败）

3. **✅ 记录错误日志**：
   ```csharp
   catch (Exception ex)
   {
       _logger.LogError(ex, "加载患者列表失败");
       MessageBox.Show($"加载异常：{ex.Message}");
   }
   ```

### 11.5 性能优化最佳实践

1. **✅ 使用分页查询**：
   ```csharp
   var response = await _patientApi.GetPatientsAsync(page: 1, pageSize: 20);
   ```

2. **✅ 避免频繁API调用**：
   - 使用本地缓存（ObservableCollection）
   - 使用防抖（Debounce）策略

3. **✅ 使用Singleton生命周期（API客户端）**：
   - API客户端全局单例，避免重复创建HttpClient

4. **✅ 使用异步方法**：
   - 所有API调用必须使用async/await

---

## 12. 总结

### 12.1 核心优势

1. **✅ 接口定义清晰**：
   - 8个API接口（IAuthApi、IPatientApi等）
   - 1个服务接口（IPrescriptionEditorService）
   - 统一的响应格式（ApiResponse<T>、PagedResult<T>）

2. **✅ 依赖最小化**：
   - 仅依赖Refit和LYBT.Shared.Models
   - 零业务逻辑，零实现代码

3. **✅ 契约一致性**：
   - 前后端共享DTO模型
   - Refit接口与Server端Controller 100%对齐

4. **✅ 依赖倒置**：
   - 高层模块依赖抽象接口
   - 低层模块实现抽象接口
   - 解除循环依赖（MedicalCase ↔ Prescriptions）

5. **✅ 易于测试**：
   - 接口可以轻松Mock
   - 单元测试、集成测试清晰

### 12.2 关键技术

| 技术组件 | 版本 | 用途 |
|---------|------|------|
| .NET | 8.0 | 运行时框架 |
| Refit | 7.x | HTTP客户端代码生成器 |
| LYBT.Shared.Models | 1.0 | 共享DTO模型 |
| System.ComponentModel | .NET 8 | Description特性标注 |

### 12.3 维护规则

1. **✅ 接口一旦发布，不轻易修改**：
   - 遵循开闭原则（OCP）
   - 新功能通过新方法添加

2. **✅ 保持与Server端契约一致**：
   - 路由、HTTP方法、DTO必须对齐
   - 定期检查一致性

3. **✅ 使用详细的XML注释**：
   - 每个接口、方法都有详细注释
   - 包含功能、参数、返回值、备注

4. **✅ 遵循SOLID原则**：
   - 单一职责（SRP）
   - 开闭原则（OCP）
   - 里氏替换（LSP）
   - 接口隔离（ISP）
   - 依赖倒置（DIP）

### 12.4 相关文档

- 📖 **客户端架构指南**：`docs/explanation/architecture/client/README.md`
- 📖 **服务端架构指南**：`docs/explanation/architecture/server/README.md`
- 📖 **共享模型文档**：`docs/explanation/architecture/shared/README.md`
- 📖 **Refit官方文档**：https://github.com/reactiveui/refit
- 📖 **SOLID原则**：https://en.wikipedia.org/wiki/SOLID

---

**📌 文档状态**：✅ 已完成
**🔄 最后更新**：2025-01-30
**👤 维护者**：LYBT开发团队
**📧 联系方式**：通过GitHub Issues反馈问题