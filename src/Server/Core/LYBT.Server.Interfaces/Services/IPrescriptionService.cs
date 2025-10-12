using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Shared.Interfaces.Services
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

        /// <summary>
        /// 创建新处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto);

        /// <summary>
        /// 更新处方信息
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionUpdateDto dto);

        /// <summary>
        /// 删除处方（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        /// <summary>
        /// 根据病例ID获取处方列表
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 生成处方编号 (Issue #1163)
        /// 格式：RX + YYYYMMDD + 4位序号
        /// </summary>
        Task<ServiceResult<string>> GeneratePrescriptionNoAsync();

        /// <summary>
        /// 克隆处方 - 复制处方并创建新实例 (Issue #1167)
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> CloneAsync(Guid prescriptionId);

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
    }
}
