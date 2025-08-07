using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Interfaces {

    /// <summary>
    /// 患者服务接口（简化版）
    /// 只提供基础的患者档案维护功能
    /// </summary>
    public interface IPatientService {

        /// <summary>
        /// 新增患者
        /// </summary>
        Task<PatientDetailDto?> CreateAsync(PatientDetailDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 更新患者信息
        /// </summary>
        Task<PatientDetailDto?> UpdateAsync(Guid id, PatientDetailDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据ID获取患者信息
        /// </summary>
        Task<PatientDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有患者列表
        /// </summary>
        Task<List<PatientDetailDto>> GetAllAsync();

        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<PaginatedResult<PatientDetailDto>> GetPagedAsync(PatientPagedQueryDto query);

        /// <summary>
        /// 搜索患者（根据姓名、手机号、身份证号）
        /// </summary>
        Task<List<PatientDetailDto>> SearchAsync(string keyword);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 设置患者状态（启用/禁用）
        /// </summary>
        Task<bool> SetStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取可用患者列表（用于挂号选择）
        /// </summary>
        Task<List<PatientDetailDto>> GetActivePatientsAsync();

        /// <summary>
        /// 根据手机号查找患者
        /// </summary>
        Task<PatientDetailDto?> GetByPhoneNumberAsync(string phoneNumber);

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        Task<PatientDetailDto?> GetByIDNumberAsync(string idNumber);

        // ==================== 患者档案管理功能 ====================

        /// <summary>
        /// 获取患者就诊历史
        /// </summary>
        Task<PatientVisitHistoryDto> GetVisitHistoryAsync(Guid patientId);

        /// <summary>
        /// 更新患者过敏史
        /// </summary>
        Task<bool> UpdateAllergyHistoryAsync(Guid patientId, string allergyHistory, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量导入患者档案
        /// </summary>
        Task<PatientImportResultDto> ImportPatientsAsync(List<PatientImportDto> patients, Guid operatorId, string operatorName);

        /// <summary>
        /// 导出患者档案
        /// </summary>
        Task<List<PatientExportDto>> ExportPatientsAsync(PatientExportQueryDto query);

        /// <summary>
        /// 合并重复患者档案
        /// </summary>
        Task<bool> MergeDuplicatePatientsAsync(Guid primaryId, Guid duplicateId, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取患者标签
        /// </summary>
        Task<List<PatientTagDto>> GetPatientTagsAsync(Guid patientId);

        /// <summary>
        /// 设置患者标签
        /// </summary>
        Task<bool> SetPatientTagsAsync(Guid patientId, List<string> tags, Guid operatorId, string operatorName);

        // ==================== 患者查询和统计功能 ====================

        /// <summary>
        /// 高级搜索患者（支持多条件组合）
        /// </summary>
        Task<PaginatedResult<PatientDetailDto>> AdvancedSearchAsync(PatientAdvancedSearchDto query);

        /// <summary>
        /// 获取患者统计信息
        /// </summary>
        Task<PatientStatisticsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 获取患者年龄分布统计
        /// </summary>
        Task<List<AgeDistributionDto>> GetAgeDistributionAsync();

        /// <summary>
        /// 获取患者性别分布统计
        /// </summary>
        Task<GenderDistributionDto> GetGenderDistributionAsync();

        /// <summary>
        /// 获取新增患者趋势（按月统计）
        /// </summary>
        Task<List<PatientTrendDto>> GetNewPatientTrendAsync(int months = 12);

        /// <summary>
        /// 获取活跃患者列表（最近就诊）
        /// </summary>
        Task<List<PatientDetailDto>> GetRecentActivePatientsAsync(int days = 30);

        /// <summary>
        /// 获取流失患者列表（长期未就诊）
        /// </summary>
        Task<List<PatientDetailDto>> GetInactivePatientsAsync(int days = 180);

        /// <summary>
        /// 获取今日新增患者
        /// </summary>
        Task<List<PatientDetailDto>> GetTodayNewPatientsAsync();

        /// <summary>
        /// 检查患者是否重复（根据身份证号或手机号）
        /// </summary>
        Task<List<PatientDetailDto>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber);
    }
}