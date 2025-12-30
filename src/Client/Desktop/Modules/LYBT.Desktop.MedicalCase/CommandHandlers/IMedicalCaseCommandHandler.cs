using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.MedicalCase.CommandHandlers;

/// <summary>
/// 医案CommandHandler接口
/// OpenSpec: unify-desktop-architecture
/// 实现ICommandHandlerBase标准接口，提供统一的CRUD操作
/// 注：UI层聚合操作使用MedicalCaseWorkspaceCoordinator
/// </summary>
public interface IMedicalCaseCommandHandler : ICommandHandlerBase<MedicalCaseListDto, MedicalCaseDetailDto, MedicalCaseInputDto>
{
    /// <summary>
    /// 获取患者的医案列表
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>医案列表</returns>
    Task<CommandResult<List<MedicalCaseListDto>>> GetByPatientAsync(Guid patientId);

    /// <summary>
    /// 获取医生的医案列表
    /// </summary>
    /// <param name="userId">医生用户ID</param>
    /// <returns>医案列表</returns>
    Task<CommandResult<List<MedicalCaseListDto>>> GetByDoctorAsync(Guid userId);

    /// <summary>
    /// 获取待处理医案列表
    /// </summary>
    /// <returns>待处理医案列表</returns>
    Task<CommandResult<List<PendingMedicalCaseDto>>> GetPendingAsync();

    /// <summary>
    /// 完成医案
    /// </summary>
    /// <param name="id">医案ID</param>
    /// <returns>操作结果</returns>
    Task<CommandResult<MedicalCaseDetailDto>> CompleteAsync(Guid id);

    /// <summary>
    /// 取消医案
    /// </summary>
    /// <param name="id">医案ID</param>
    /// <returns>操作结果</returns>
    Task<CommandResult<bool>> CancelAsync(Guid id);

    /// <summary>
    /// 保存为草稿
    /// </summary>
    /// <param name="input">医案输入数据</param>
    /// <returns>保存的医案</returns>
    Task<CommandResult<MedicalCaseDetailDto>> SaveDraftAsync(MedicalCaseInputDto input);
}
