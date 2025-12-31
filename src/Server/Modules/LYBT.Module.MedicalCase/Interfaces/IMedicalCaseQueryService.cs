using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCases.Interfaces
{
    /// <summary>
    /// 病案查询服务接口 - 读操作
    /// Phase 3: 从IMedicalCaseService拆分，遵循CQRS原则
    /// 职责：GetById, GetList, Search等查询操作
    /// </summary>
    public interface IMedicalCaseQueryService
    {
        /// <summary>
        /// 根据ID获取病案详情
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <returns>病案实体（包含完整关联数据）</returns>
        Task<MedicalCase?> GetByIdAsync(Guid id);

        /// <summary>
        /// 查询病案列表（分页）
        /// OpenSpec: optimize-module-list-ui - 添加角色过滤支持
        /// </summary>
        /// <param name="status">病案状态（可选）</param>
        /// <param name="patientId">患者ID（可选）</param>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="currentDoctorId">当前用户ID（用于角色过滤）</param>
        /// <param name="isAdmin">是否为管理员</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<MedicalCase>> GetListAsync(
            MedicalCaseStatus? status,
            Guid? patientId,
            int page,
            int pageSize,
            Guid? currentDoctorId = null,
            bool isAdmin = false,
            string? keyword = null);

        /// <summary>
        /// 查询病案列表（分页，返回MedicalCaseListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        Task<PagedResult<MedicalCaseListDto>> GetListDtoAsync(
            MedicalCaseStatus? status,
            Guid? patientId,
            int page,
            int pageSize,
            Guid? currentDoctorId = null,
            bool isAdmin = false,
            string? keyword = null);

        /// <summary>
        /// 查询辨证记录列表
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <returns>辨证记录DTO列表</returns>
        Task<List<ConsultationDetailDto>> GetConsultationListAsync(Guid medicalCaseId);

        /// <summary>
        /// 查询处方列表
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <returns>处方DTO列表</returns>
        Task<List<PrescriptionDetailDto>> GetPrescriptionListAsync(Guid medicalCaseId);

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="doctorId">医生ID（为Guid.Empty时不筛选医生）</param>
        /// <returns>未完成的病案实体（包含关联数据），若无则返回null</returns>
        Task<MedicalCase?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId);

        /// <summary>
        /// 获取待看诊队列（Status = Active的医案患者列表）
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <returns>待诊队列列表</returns>
        Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId);

        /// <summary>
        /// 获取所有待看诊队列（管理员专用）
        /// </summary>
        /// <returns>待诊队列列表</returns>

        /// <summary>
        /// 跨医案搜索（支持多条件组合查询）
        /// OpenSpec: consolidate-medicalcase-queries (LIFECYCLE-015)
        /// </summary>
        /// <param name="patientName">患者姓名关键字（模糊匹配）</param>
        /// <param name="diagnosisKeyword">诊断关键字（搜索TcmDiagnosis）</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>分页结果（含嵌套Consultation/Prescription）</returns>
        Task<PagedResult<MedicalCaseDetailDto>> SearchMedicalCasesAsync(
            string? patientName = null,
            string? diagnosisKeyword = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20);

        /// <summary>
        /// 获取患者最近医案列表
        /// OpenSpec: consolidate-medicalcase-queries (LIFECYCLE-016)
        /// 用于处方编辑器历史处方参考
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="count">返回数量（默认5）</param>
        /// <returns>最近医案列表（按创建时间倒序，含完整Prescription数据）</returns>
        Task<List<MedicalCaseDetailDto>> GetPatientRecentMedicalCasesAsync(Guid patientId, int count = 5);

        Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync();

        /// <summary>
        /// 统一查询接口
        /// OpenSpec: optimize-medicalcase-api - 整合多个查询端点为统一接口
        /// 根据QueryType分发到不同查询逻辑
        /// </summary>
        /// <param name="query">查询参数</param>
        /// <returns>分页查询结果</returns>
        Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query);
    }
}
