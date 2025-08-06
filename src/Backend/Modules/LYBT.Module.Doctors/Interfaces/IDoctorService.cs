using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Doctors;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Doctors.Interfaces {

    /// <summary>
    /// 医生业务服务接口（简化版 - 仅基础功能）
    /// </summary>
    public interface IDoctorService {

        /// <summary>
        /// 根据ID获取医生详情
        /// </summary>
        Task<DoctorDetailDto?> GetByIdAsync(Guid id, UserRole currentUserRole);

        /// <summary>
        /// 根据用户ID获取医生详情
        /// </summary>
        Task<DoctorDetailDto?> GetByUserIdAsync(Guid userId, UserRole currentUserRole);

        /// <summary>
        /// 获取所有医生列表
        /// </summary>
        Task<List<DoctorDto>> GetAllAsync(UserRole currentUserRole);

        /// <summary>
        /// 搜索医生
        /// </summary>
        Task<List<DoctorDto>> SearchAsync(string keyword, UserRole currentUserRole);

        /// <summary>
        /// 分页获取医生列表
        /// </summary>
        Task<PaginatedResult<DoctorDto>> GetPagedAsync(DoctorQueryDto query, UserRole currentUserRole);

        /// <summary>
        /// 新增医生
        /// </summary>
        Task<DoctorDetailDto?> CreateAsync(DoctorCreateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 更新医生信息
        /// </summary>
        Task<DoctorDetailDto?> UpdateAsync(Guid id, DoctorUpdateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 删除医生（软删除）
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 设置医生状态
        /// </summary>
        Task<bool> SetStatusAsync(Guid id, DoctorStatus status, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取可用医生列表（用于挂号选择）
        /// </summary>
        Task<List<DoctorDto>> GetAvailableDoctorsAsync();

        // ==================== 休息时间管理 ====================

        /// <summary>
        /// 设置医生休息状态（某天不出诊）
        /// </summary>
        Task<bool> SetDoctorRestAsync(Guid doctorId, DateTime date, bool isRest, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取医生休息记录
        /// </summary>
        Task<List<DoctorRestRecordDto>> GetDoctorRestRecordsAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 检查医生是否在某天休息
        /// </summary>
        Task<bool> IsDoctorRestingAsync(Guid doctorId, DateTime date);

        /// <summary>
        /// 获取某天出诊的医生列表
        /// </summary>
        Task<List<DoctorDto>> GetAvailableDoctorsByDateAsync(DateTime date);

        // ==================== 简化的信息管理 ====================

        /// <summary>
        /// 更新医生基本信息（包含专长）
        /// </summary>
        Task<bool> UpdateDoctorInfoAsync(Guid doctorId, DoctorInfoUpdateDto info, Guid operatorId, string operatorName);
    }
}