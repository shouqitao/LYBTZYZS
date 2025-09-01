using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Interfaces;

/// <summary>
/// 看诊业务服务接口 - UltraThink三层架构业务层
/// 职责：业务流程编排、CRUD操作、验证规则、事务管理
/// </summary>
public interface IConsultationBusinessService
{
    #region 事件定义

    /// <summary>
    /// 看诊状态变更事件
    /// </summary>
    event EventHandler<ConsultationStatusChangedEventArgs>? ConsultationStatusChanged;

    /// <summary>
    /// 看诊操作事件
    /// </summary>
    event EventHandler<ConsultationOperationEventArgs>? ConsultationOperation;

    /// <summary>
    /// 诊断更新事件
    /// </summary>
    event EventHandler<DiagnosisUpdatedEventArgs>? DiagnosisUpdated;

    /// <summary>
    /// 四诊记录事件
    /// </summary>
    event EventHandler<FourDiagnosisRecordedEventArgs>? FourDiagnosisRecorded;

    #endregion

    #region 基础CRUD操作

    /// <summary>
    /// 开始看诊
    /// </summary>
    Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto startDto);

    /// <summary>
    /// 更新看诊
    /// </summary>
    Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto updateDto);

    /// <summary>
    /// 删除看诊
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id);

    /// <summary>
    /// 获取看诊详情
    /// </summary>
    Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 获取分页看诊列表
    /// </summary>
    Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query);

    #endregion

    #region 业务流程方法

    /// <summary>
    /// 完成看诊
    /// </summary>
    Task<ServiceResult<bool>> CompleteConsultationAsync(Guid id, ConsultationCompleteDto completeDto);

    /// <summary>
    /// 取消看诊
    /// </summary>
    Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason);

    /// <summary>
    /// 保存完整四诊记录
    /// </summary>
    Task<ServiceResult<bool>> SaveCompleteFourDiagnosisAsync(Guid consultationId, CompleteFourDiagnosisDto fourDiagnosisData);

    /// <summary>
    /// 批量更新看诊状态
    /// </summary>
    Task<ServiceResult<ConsultationBatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> consultationIds, ConsultationStatus status);

    #endregion

    #region 搜索方法

    /// <summary>
    /// 搜索看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword, int limit = 100);

    #endregion
}