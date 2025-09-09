using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 医案业务服务接口 - UltraThink双层架构简化版
/// 职责：基础业务操作
/// </summary>
public interface IMedicalCaseBusinessService
{

    #region 基础医案业务操作

    /// <summary>
    /// 创建医案
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto);

    /// <summary>
    /// 更新医案
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseDetailDto dto);

    /// <summary>
    /// 删除医案
    /// </summary>
    Task<ServiceResult<bool>> Delete(Guid id);

    /// <summary>
    /// 开始医案
    /// </summary>
    Task<ServiceResult<bool>> StartAsync(Guid id);

    /// <summary>
    /// 完成医案
    /// </summary>
    Task<ServiceResult<bool>> CompleteAsync(Guid id);

    /// <summary>
    /// 取消医案
    /// </summary>
    Task<ServiceResult<bool>> CancelAsync(Guid id);

    /// <summary>
    /// 暂停医案
    /// </summary>
    Task<ServiceResult<bool>> SuspendAsync(Guid id, string reason);

    /// <summary>
    /// 恢复医案
    /// </summary>
    Task<ServiceResult<bool>> ResumeAsync(Guid id);

    /// <summary>
    /// 归档医案
    /// </summary>
    Task<ServiceResult<bool>> ArchiveAsync(Guid id, string archiveReason);

    /// <summary>
    /// 搜索医案
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword);

    #endregion 基础医案业务操作
}
