using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.TreatmentRoom;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.TreatmentRoom.Interfaces {

    /// <summary>
    /// 治疗室业务服务接口（增强版）
    /// </summary>
    public interface ITreatmentRoomService {

        /// <summary>
        /// 根据ID获取治疗室详情
        /// </summary>
        Task<TreatmentRoomDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取治疗室列表
        /// </summary>
        Task<List<TreatmentRoomDto>> GetListAsync();

        /// <summary>
        /// 分页获取治疗室列表
        /// </summary>
        Task<PaginatedResult<TreatmentRoomDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole);

        /// <summary>
        /// 新增治疗室单
        /// </summary>
        Task<bool> AddAsync(TreatmentRoomCreateDto treatmentRoomCreateDto);

        /// <summary>
        /// 编辑治疗室单
        /// </summary>
        Task<bool> UpdateAsync(TreatmentRoomEditDto treatmentRoomEditDto);

        /// <summary>
        /// 删除治疗室单
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据状态获取治疗室单
        /// </summary>
        Task<List<TreatmentRoomDto>> GetByStatusAsync(string status);

        /// <summary>
        /// 获取待治疗列表
        /// </summary>
        Task<List<TreatmentRoomQueueDto>> GetPendingListAsync();

        /// <summary>
        /// 开始治疗
        /// </summary>
        Task<bool> StartTreatmentAsync(Guid id, Guid therapistId);

        /// <summary>
        /// 完成治疗
        /// </summary>
        Task<bool> CompleteTreatmentAsync(Guid id, string treatmentNote);

        /// <summary>
        /// 取消治疗
        /// </summary>
        Task<bool> CancelTreatmentAsync(Guid id, string reason);

        /// <summary>
        /// 根据医疗案例ID获取治疗记录
        /// </summary>
        Task<List<TreatmentRoomDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据患者ID获取治疗历史
        /// </summary>
        Task<List<TreatmentRoomDto>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据治疗师ID获取治疗记录
        /// </summary>
        Task<List<TreatmentRoomDto>> GetByTherapistIdAsync(Guid therapistId);

        /// <summary>
        /// 获取今日治疗记录
        /// </summary>
        Task<List<TreatmentRoomDto>> GetTodayTreatmentsAsync();

        /// <summary>
        /// 更新治疗进度
        /// </summary>
        Task<bool> UpdateProgressAsync(Guid id, int completedSessions, string progressNote);

        /// <summary>
        /// 获取治疗室使用情况
        /// </summary>
        Task<List<TreatmentRoomUsageDto>> GetRoomUsageAsync();

        /// <summary>
        /// 分配治疗室
        /// </summary>
        Task<bool> AssignRoomAsync(Guid id, Guid roomId);

        /// <summary>
        /// 获取治疗统计
        /// </summary>
        Task<TreatmentRoomStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 批量安排治疗
        /// </summary>
        Task<bool> BatchScheduleTreatmentsAsync(List<Guid> physiotherapyItemIds);

        /// <summary>
        /// 获取可用治疗室列表
        /// </summary>
        Task<List<AvailableRoomDto>> GetAvailableRoomsAsync();
    }
}