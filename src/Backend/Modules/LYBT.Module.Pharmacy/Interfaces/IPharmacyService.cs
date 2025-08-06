using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Pharmacy;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Pharmacy.Interfaces {

    /// <summary>
    /// 药房业务服务接口（增强版）
    /// </summary>
    public interface IPharmacyService {

        /// <summary>
        /// 根据ID获取药房单详情
        /// </summary>
        Task<PharmacyDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取药房单列表
        /// </summary>
        Task<List<PharmacyDto>> GetListAsync();

        /// <summary>
        /// 分页获取药房单列表
        /// </summary>
        Task<PaginatedResult<PharmacyDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole);

        /// <summary>
        /// 新增药房单
        /// </summary>
        Task<PharmacyDto?> AddAsync(PharmacyCreateDto pharmacyCreateDto);

        /// <summary>
        /// 编辑药房单
        /// </summary>
        Task<bool> UpdateAsync(PharmacyEditDto pharmacyEditDto);

        /// <summary>
        /// 删除药房单
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 获取待抓药处方列表
        /// </summary>
        Task<List<PharmacyDto>> GetWaitingListAsync();

        /// <summary>
        /// 将指定处方标记为已抓药
        /// </summary>
        Task<bool> MarkAsPreparedAsync(Guid id);

        /// <summary>
        /// 获取待配药列表
        /// </summary>
        Task<List<PharmacyQueueDto>> GetPendingListAsync();

        /// <summary>
        /// 开始配药
        /// </summary>
        Task<bool> StartDispensingAsync(Guid id);

        /// <summary>
        /// 完成配药
        /// </summary>
        Task<bool> CompleteDispensingAsync(Guid id);

        /// <summary>
        /// 取消配药
        /// </summary>
        Task<bool> CancelDispensingAsync(Guid id, string reason);

        /// <summary>
        /// 根据医疗案例ID获取配药记录
        /// </summary>
        Task<PharmacyDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据处方ID获取配药记录
        /// </summary>
        Task<PharmacyDetailDto?> GetByPrescriptionIdAsync(Guid prescriptionId);

        /// <summary>
        /// 根据患者ID获取配药历史
        /// </summary>
        Task<List<PharmacyDto>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取今日配药记录
        /// </summary>
        Task<List<PharmacyDto>> GetTodayRecordsAsync();

        /// <summary>
        /// 发药确认
        /// </summary>
        Task<bool> ConfirmDispenseAsync(Guid id, string receiverName, string receiverPhone);

        /// <summary>
        /// 药品库存检查
        /// </summary>
        Task<StockCheckResultDto> CheckStockAsync(Guid prescriptionId);

        /// <summary>
        /// 获取配药统计
        /// </summary>
        Task<PharmacyStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate);

        // ==================== 现场取药增强功能 ====================

        /// <summary>
        /// 从处方创建药房单
        /// </summary>
        Task<PharmacyDto?> CreateFromPrescriptionAsync(Guid prescriptionId, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量配药（对多个处方同时处理）
        /// </summary>
        Task<bool> BatchDispenseAsync(List<Guid> pharmacyIds, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取今日待取药统计
        /// </summary>
        Task<PharmacyTodayStatDto> GetTodayStatisticsAsync();

        /// <summary>
        /// 获取药材配置明细（供配药师使用）
        /// </summary>
        Task<List<HerbDispenseDetailDto>> GetHerbDispenseDetailsAsync(Guid pharmacyId);

        /// <summary>
        /// 提交配药结果（包含实际配置量）
        /// </summary>
        Task<bool> SubmitDispenseResultAsync(Guid pharmacyId, List<HerbDispenseResultDto> results, Guid operatorId, string operatorName);
    }
}