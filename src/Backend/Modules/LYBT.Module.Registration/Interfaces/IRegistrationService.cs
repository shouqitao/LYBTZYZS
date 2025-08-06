using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Registration.Interfaces {

    /// <summary>
    /// 挂号业务服务接口（现场挂号模式）
    /// </summary>
    public interface IRegistrationService {

        /// <summary>
        /// 获取挂号详情
        /// </summary>
        Task<RegistrationDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取挂号列表
        /// </summary>
        Task<List<RegistrationDto>> GetListAsync();

        /// <summary>
        /// 分页查询挂号列表
        /// </summary>
        Task<PaginatedResult<RegistrationDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole);

        /// <summary>
        /// 新增挂号
        /// </summary>
        Task<RegistrationDto?> AddAsync(RegistrationCreateDto dto);

        /// <summary>
        /// 编辑挂号
        /// </summary>
        Task<bool> UpdateAsync(RegistrationEditDto dto);

        /// <summary>
        /// 删除挂号（物理删除，不推荐）
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 取消挂号，更新状态为已取消
        /// </summary>
        Task<bool> CancelAsync(Guid id);

        // ==================== 现场挂号特有功能 ====================

        /// <summary>
        /// 获取今日挂号列表
        /// </summary>
        Task<List<RegistrationDto>> GetTodayRegistrationsAsync(Guid? doctorId = null);

        /// <summary>
        /// 获取医生今日挂号统计
        /// </summary>
        Task<DoctorRegistrationStatDto> GetDoctorTodayStatAsync(Guid doctorId);

        /// <summary>
        /// 开始就诊
        /// </summary>
        Task<bool> StartConsultationAsync(Guid registrationId, Guid operatorId, string operatorName);

        /// <summary>
        /// 完成就诊
        /// </summary>
        Task<bool> CompleteConsultationAsync(Guid registrationId, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取当前正在就诊的挂号
        /// </summary>
        Task<RegistrationDto?> GetCurrentConsultationAsync(Guid doctorId);

        /// <summary>
        /// 获取下一个等待就诊的挂号
        /// </summary>
        Task<RegistrationDto?> GetNextWaitingAsync(Guid doctorId);
    }
}