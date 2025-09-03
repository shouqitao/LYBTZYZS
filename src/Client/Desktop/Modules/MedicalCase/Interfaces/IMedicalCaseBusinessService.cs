using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
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
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    
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
    
    #endregion
}