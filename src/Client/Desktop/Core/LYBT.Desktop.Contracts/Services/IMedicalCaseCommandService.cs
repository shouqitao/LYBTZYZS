using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 医案命令服务接口 - 跨模块共享
/// OpenSpec: refactor-frontend-srp-patterns (ADR-1) - SRP职责分离，命令职责
/// 负责医案的创建、保存、删除等写操作
/// </summary>
public interface IMedicalCaseCommandService
{
    /// <summary>
    /// 当前病案数据
    /// </summary>
    MedicalCaseDetailDto? Current { get; }

    /// <summary>
    /// 是否有未保存的变更
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// 保存变更
    /// </summary>
    /// <returns>是否保存成功</returns>
    Task<bool> SaveAsync();

    /// <summary>
    /// 删除当前病案
    /// </summary>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync();

    /// <summary>
    /// 创建新医案
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>(是否成功, 医案ID, 错误信息)</returns>
    Task<(bool success, Guid medicalCaseId, string? errorMessage)> CreateMedicalCaseAsync(Guid patientId);
}
