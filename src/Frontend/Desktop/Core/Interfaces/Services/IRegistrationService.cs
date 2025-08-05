using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.WPF.Client.Core.Models.Registration;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 挂号服务接口
    /// </summary>
    public interface IRegistrationService
    {
        /// <summary>
        /// 分页查询挂号记录
        /// </summary>
        Task<Models.Common.PagedResult<RegistrationInfo>> SearchRegistrationsAsync(RegistrationPagedQueryDto query);

        /// <summary>
        /// 获取挂号列表
        /// </summary>
        Task<List<RegistrationInfo>> GetRegistrationsAsync();

        /// <summary>
        /// 获取挂号详情
        /// </summary>
        Task<RegistrationInfo?> GetByIdAsync(Guid id);

        /// <summary>
        /// 新增挂号
        /// </summary>
        Task<ServiceResult> CreateRegistrationAsync(RegistrationCreateDto dto);

        /// <summary>
        /// 编辑挂号
        /// </summary>
        Task<ServiceResult> UpdateRegistrationAsync(RegistrationEditDto dto);

        /// <summary>
        /// 删除挂号
        /// </summary>
        Task<ServiceResult> DeleteRegistrationAsync(Guid id);

        /// <summary>
        /// 取消挂号
        /// </summary>
        Task<ServiceResult> CancelRegistrationAsync(Guid id);


        /// <summary>
        /// 获取医生可预约时间段
        /// </summary>
        Task<List<TimeSlotInfo>> GetAvailableSlotsAsync(Guid doctorId, DateTime date);

        /// <summary>
        /// 分页获取挂号记录
        /// </summary>
        Task<Models.Common.PagedResult<RegistrationInfo>> GetPagedAsync(int page, int pageSize, string? searchKeyword = null, DateTime? startDate = null, DateTime? endDate = null, string? status = null, string? registrationType = null);

        /// <summary>
        /// 创建挂号
        /// </summary>
        Task<ServiceResult> CreateAsync(RegistrationCreateDto dto);

        /// <summary>
        /// 更新挂号
        /// </summary>
        Task<ServiceResult> UpdateAsync(RegistrationEditDto dto);

        /// <summary>
        /// 取消挂号
        /// </summary>
        Task<ServiceResult> CancelAsync(Guid id);

        /// <summary>
        /// 批量取消挂号
        /// </summary>
        Task<ServiceResult> BatchCancelAsync(List<Guid> ids);
    }
}