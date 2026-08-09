// ---------------------------------------------------------------------------
// IApiClient — Unified API Client Abstraction
// ---------------------------------------------------------------------------
// This interface aggregates all domain-specific API sub-interfaces.
// Two implementations exist:
//   - RefitApiClient (Remote mode): uses Refit-generated HTTP clients
//   - HttpClientApiClient (LocalWebAPI mode): uses IHttpClientFactory
//
// The active implementation is determined by the connection URL at runtime.
// Mode switching (Remote ↔ Local) is handled internally by SwitchingApiClient.
// ---------------------------------------------------------------------------

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// 聚合所有领域专用 API 子接口的统一 API 客户端。
/// 取代对单个 Refit 接口（IHerbApi、IPatientApi 等）
/// 以及 HttpXxxRepository 原生 HttpClient 用法的直接依赖。
/// </summary>
public interface IApiClient
{
    /// <summary>认证与用户管理端点（登录、登出、刷新、校验、CRUD、密码、个人资料）。</summary>
    IApiClientIdentity Identity { get; }

    /// <summary>患者管理端点（CRUD、导入/导出、批量操作）。</summary>
    IApiClientPatients Patients { get; }

    /// <summary>药材管理端点（CRUD、导入/导出、批量操作）。</summary>
    IApiClientHerbs Herbs { get; }

    /// <summary>验方管理端点（CRUD、克隆、导入/导出、批量操作）。</summary>
    IApiClientFormulas Formulas { get; }

    /// <summary>医案端点（CRUD、状态流转、处方）。</summary>
    IApiClientMedicalCases MedicalCases { get; }

    /// <summary>挂号端点（CRUD、队列、就诊管理）。</summary>
    IApiClientRegistrations Registrations { get; }

    /// <summary>报表端点（日收入、就诊、药材用量）。</summary>
    IApiClientReports Reports { get; }

    /// <summary>服务器部署端点（上传、重启）。</summary>
    IApiClientDeploy Deploy { get; }

    /// <summary>诊断/日志端点（状态、启用/禁用、级别）。</summary>
    IApiClientDiagnostics Diagnostics { get; }

    /// <summary>系统配置端点（服务器配置，仅远程模式有意义）。</summary>
    IApiClientConfiguration Configuration { get; }
}
