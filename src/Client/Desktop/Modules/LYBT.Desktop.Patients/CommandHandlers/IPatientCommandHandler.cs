using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.CommandHandlers;

/// <summary>
/// 患者CommandHandler接口
/// OpenSpec: unify-desktop-architecture (Phase 2.1)
/// 封装IPatientRepository，提供统一的CRUD操作和错误处理
/// </summary>
public interface IPatientCommandHandler : ICommandHandlerBase<PatientListDto, PatientDetailDto, PatientInputDto>
{
    /// <summary>
    /// 按姓名搜索患者
    /// </summary>
    /// <param name="name">姓名关键字</param>
    /// <returns>匹配的患者列表</returns>
    Task<CommandResult<List<PatientListDto>>> SearchByNameAsync(string name);

    /// <summary>
    /// 按电话搜索患者
    /// </summary>
    /// <param name="phone">电话号码</param>
    /// <returns>匹配的患者列表</returns>
    Task<CommandResult<List<PatientListDto>>> SearchByPhoneAsync(string phone);

    /// <summary>
    /// 检查患者是否有关联的医案
    /// </summary>
    /// <param name="id">患者ID</param>
    /// <returns>是否有关联医案</returns>
    Task<CommandResult<bool>> HasMedicalCasesAsync(Guid id);
}
