using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Formula.Interfaces
{
    /// <summary>
    /// 验方管理服务接口
    /// </summary>
    public interface IFormulaService
    {
        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        Task<FormulaDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取验方列表
        /// </summary>
        Task<List<FormulaDto>> GetListAsync();

        /// <summary>
        /// 分页查询验方
        /// </summary>
        Task<PaginatedResult<FormulaDto>> GetPagedAsync(FormulaQueryDto query);

        /// <summary>
        /// 创建验方
        /// </summary>
        Task<FormulaDetailDto?> CreateAsync(FormulaCreateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 更新验方
        /// </summary>
        Task<FormulaDetailDto?> UpdateAsync(Guid id, FormulaUpdateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 删除验方
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据创建者ID获取验方列表
        /// </summary>
        Task<List<FormulaDto>> GetByCreatorIdAsync(Guid creatorId);

        /// <summary>
        /// 获取共享验方列表
        /// </summary>
        Task<List<FormulaDto>> GetSharedFormulasAsync();

        /// <summary>
        /// 获取个人验方列表
        /// </summary>
        Task<List<FormulaDto>> GetPersonalFormulasAsync(Guid doctorId);

        /// <summary>
        /// 搜索验方
        /// </summary>
        Task<List<FormulaDto>> SearchFormulasAsync(string keyword, int maxResults = 50);

        /// <summary>
        /// 获取验方统计
        /// </summary>
        Task<FormulaStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate, Guid? doctorId = null);

        // ==================== 验方高级功能 ====================

        /// <summary>
        /// 从处方创建验方
        /// </summary>
        Task<FormulaDetailDto?> CreateFromPrescriptionAsync(CreateFormulaFromPrescriptionDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 复制验方
        /// </summary>
        Task<FormulaDetailDto?> CopyFormulaAsync(Guid sourceFormulaId, string newName, Guid operatorId, string operatorName);

        /// <summary>
        /// 分享验方（设置为共享）
        /// </summary>
        Task<bool> ShareFormulaAsync(Guid formulaId, Guid operatorId, string operatorName);

        /// <summary>
        /// 取消分享验方
        /// </summary>
        Task<bool> UnshareFormulaAsync(Guid formulaId, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取验方推荐（基于症状或诊断）
        /// </summary>
        Task<List<FormulaRecommendationDto>> GetRecommendationsAsync(string symptoms, string diagnosis, Guid? doctorId = null);

        /// <summary>
        /// 获取常用验方（按使用频率）
        /// </summary>
        Task<List<FormulaDto>> GetFrequentlyUsedFormulasAsync(Guid? doctorId = null, int limit = 20);

        /// <summary>
        /// 验证验方合理性（配伍检查）
        /// </summary>
        Task<FormulaValidationResult> ValidateFormulaAsync(Guid formulaId);

        /// <summary>
        /// 获取验方使用记录
        /// </summary>
        Task<List<FormulaUsageRecordDto>> GetUsageRecordsAsync(Guid formulaId);
    }

    /// <summary>
    /// 验方验证结果
    /// </summary>
    public class FormulaValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }

    /// <summary>
    /// 验方使用记录DTO
    /// </summary>
    public class FormulaUsageRecordDto
    {
        public Guid Id { get; set; }
        public Guid FormulaId { get; set; }
        public Guid PrescriptionId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime UsageDate { get; set; }
        public string? Modifications { get; set; }
        public string? Feedback { get; set; }
    }
}