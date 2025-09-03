using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Interfaces;

/// <summary>
/// 处方业务服务接口 - UltraThink双层架构简化版
/// 职责：基础业务操作
/// </summary>
public interface IPrescriptionsBusinessService
{
    /// <summary>
    /// 创建处方
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto createDto);

    /// <summary>
    /// 更新处方
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto updateDto);

    /// <summary>
    /// 删除处方
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid prescriptionId);

    /// <summary>
    /// 启用处方
    /// </summary>
    Task<ServiceResult<bool>> EnableAsync(Guid prescriptionId);

    /// <summary>
    /// 禁用处方
    /// </summary>
    Task<ServiceResult<bool>> DisableAsync(Guid prescriptionId);
}