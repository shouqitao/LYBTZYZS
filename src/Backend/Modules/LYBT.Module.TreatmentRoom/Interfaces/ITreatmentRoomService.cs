using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.TreatmentRoom;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.TreatmentRoom.Interfaces {

    /// <summary>
    /// 理疗室管理服务接口（现场理疗模式）
    /// </summary>
    public interface ITreatmentRoomService {

        /// <summary>
        /// 根据ID获取治疗记录详情
        /// </summary>
        Task<TreatmentDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取治疗记录列表
        /// </summary>
        Task<List<TreatmentDto>> GetListAsync();

        /// <summary>
        /// 分页查询治疗记录
        /// </summary>
        Task<PaginatedResult<TreatmentDto>> GetPagedAsync(TreatmentQueryDto query, UserRole operatorRole);

        /// <summary>
        /// 创建治疗记录
        /// </summary>
        Task<TreatmentDetailDto?> CreateAsync(TreatmentCreateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 更新治疗记录
        /// </summary>
        Task<TreatmentDetailDto?> UpdateAsync(Guid id, TreatmentUpdateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 删除治疗记录
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据状态获取治疗记录
        /// </summary>
        Task<List<TreatmentDto>> GetByStatusAsync(string status);

        /// <summary>
        /// 获取待治疗队列
        /// </summary>
        Task<List<TreatmentQueueDto>> GetTreatmentQueueAsync();

        /// <summary>
        /// 开始治疗
        /// </summary>
        Task<bool> StartTreatmentAsync(Guid id, StartTreatmentDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 完成治疗
        /// </summary>
        Task<bool> CompleteTreatmentAsync(Guid id, CompleteTreatmentDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 取消治疗
        /// </summary>
        Task<bool> CancelTreatmentAsync(Guid id, string reason, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据患者ID获取治疗历史
        /// </summary>
        Task<List<TreatmentDto>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取今日治疗记录
        /// </summary>
        Task<List<TreatmentDto>> GetTodayTreatmentsAsync();

        /// <summary>
        /// 获取理疗室状态
        /// </summary>
        Task<List<TreatmentRoomStatusDto>> GetRoomStatusAsync();

        /// <summary>
        /// 获取治疗统计
        /// </summary>
        Task<TreatmentStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取今日统计
        /// </summary>
        Task<TodayTreatmentStatDto> GetTodayStatisticsAsync();

        // ==================== 现场理疗增强功能 ====================

        /// <summary>
        /// 从挂号创建治疗记录
        /// </summary>
        Task<TreatmentDetailDto?> CreateFromRegistrationAsync(Guid registrationId, string treatmentType, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量安排治疗
        /// </summary>
        Task<bool> BatchScheduleTreatmentsAsync(List<Guid> treatmentIds, Guid therapistId, string therapistName, Guid operatorId, string operatorName);

        /// <summary>
        /// 分配治疗室
        /// </summary>
        Task<bool> AssignRoomAsync(Guid treatmentId, int roomNumber, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取可用治疗师列表
        /// </summary>
        Task<List<TherapistDto>> GetAvailableTherapistsAsync();

        /// <summary>
        /// 治疗进度更新
        /// </summary>
        Task<bool> UpdateTreatmentProgressAsync(Guid treatmentId, string progressNotes, Guid operatorId, string operatorName);
    }

    /// <summary>
    /// 治疗师DTO（简化版）
    /// </summary>
    public class TherapistDto {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public int CurrentTreatmentCount { get; set; }
    }
}