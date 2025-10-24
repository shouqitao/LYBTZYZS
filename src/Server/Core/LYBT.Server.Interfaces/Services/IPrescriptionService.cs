using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Server.Interfaces.Services
{
    /// <summary>
    /// 处方服务接口 - 简化版，包含基础CRUD和统计功能
    /// </summary>
    public interface IPrescriptionService
    {
        /// <summary>
        /// 分页查询处方（Issue #1163: 扩展支持日期范围筛选）
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="startDate">开始日期（可选）</param>
        /// <param name="endDate">结束日期（可选）</param>
        Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id);

        // ========== Write方法已移除（Issue #1601 Phase 1）==========
        // CreateAsync, UpdateAsync, DeleteAsync, PhysicalDeleteAsync 已移除
        // 所有写操作必须通过MedicalCase聚合根进行

        /// <summary>
        /// 根据病例ID获取处方列表
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 生成处方编号 (Issue #1163)
        /// 格式：RX + YYYYMMDD + 4位序号
        /// </summary>
        Task<ServiceResult<string>> GeneratePrescriptionNoAsync();

        // ========== Clone/Import方法已移除（Issue #1601 Phase 1）==========
        // CloneAsync, ClonePrescriptionAsync, ImportFormulaIntoPrescriptionAsync 已移除
        // 所有写操作必须通过MedicalCase聚合根进行

        /// <summary>
        /// 获取处方统计数据 (Issue #1163)
        /// 包含总数、今日数量和今日金额
        /// </summary>
        Task<ServiceResult<PrescriptionMainStatisticsDto>> GetStatisticsAsync();

        /// <summary>
        /// 获取日期范围统计 (Issue #1163)
        /// 包含数量、总金额和平均金额
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        Task<ServiceResult<PrescriptionRangeStatisticsDto>> GetRangeStatisticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 搜索处方 - 按患者姓名或症状/诊断关键字 (Issue #1372 ENTRY-14)
        /// </summary>
        /// <param name="patientName">患者姓名关键字（可空）</param>
        /// <param name="symptomKeyword">症状/诊断关键字（可空）</param>
        /// <returns>处方搜索结果列表</returns>
        Task<ServiceResult<List<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(
            string? patientName = null,
            string? symptomKeyword = null);

        /// <summary>
        /// 获取患者最近处方列表 (Issue #1371 ENTRY-13)
        /// 按日期倒序排列，包含诊断信息
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="count">返回数量（默认5条）</param>
        /// <returns>患者最近处方列表</returns>
        Task<ServiceResult<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(
            Guid patientId,
            int count = 5);
    }
}
