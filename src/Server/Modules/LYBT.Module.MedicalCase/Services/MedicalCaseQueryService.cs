using AutoMapper;
using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Services;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 病案查询服务实现 - 读操作
    /// Phase 3: 从MedicalCaseService拆分，遵循CQRS原则
    /// 职责：GetById, GetList, Search等查询操作
    /// </summary>
    public class MedicalCaseQueryService : BaseService<MedicalCase>, IMedicalCaseQueryService
    {
        private readonly IMedicalCaseRepository _repository;

        public MedicalCaseQueryService(
            IMedicalCaseRepository repository,
            IMapper mapper,
            ILogger<MedicalCaseQueryService> logger)
            : base(logger, mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// 根据ID获取病案详情（包含完整关联数据）
        /// Epic #1612: 使用GetDetailQuery预加载Consultation和Prescription
        /// </summary>
        public async Task<MedicalCase?> GetByIdAsync(Guid id)
        {
            try
            {
                var result = await _repository.GetByIdWithDetailsAsync(id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取病案详情失败，MedicalCaseId: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 查询病案列表（分页）
        /// Epic #1612: 支持按状态、患者ID过滤
        /// </summary>
        public async Task<PagedResult<MedicalCase>> GetListAsync(
            MedicalCaseStatus? status,
            Guid? patientId,
            int page,
            int pageSize)
        {
            try
            {
                // TODO: Repository需要扩展支持status和patientId过滤的分页方法
                // 当前使用GetPagedWithDetailsAsync作为临时实现
                var result = await _repository.GetPagedWithDetailsAsync(page, pageSize);

                // 临时过滤逻辑（后续应在Repository层实现）
                var filteredItems = result.Items.AsQueryable();

                if (status.HasValue)
                {
                    filteredItems = filteredItems.Where(m => m.CaseStatus == status.Value);
                }

                if (patientId.HasValue)
                {
                    filteredItems = filteredItems.Where(m => m.PatientId == patientId.Value);
                }

                return new PagedResult<MedicalCase>
                {
                    Items = filteredItems.ToList(),
                    TotalCount = filteredItems.Count(),
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询病案列表失败");
                throw;
            }
        }

        /// <summary>
        /// 查询辨证记录列表
        /// Epic #1612: 返回病案的所有历史辨证记录
        /// </summary>
        public async Task<List<ConsultationDto>> GetConsultationListAsync(Guid medicalCaseId)
        {
            try
            {
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase?.Consultation == null)
                {
                    return new List<ConsultationDto>();
                }

                // 当前架构下只有一条Consultation（共享主键），直接映射
                var dto = _mapper.Map<ConsultationDto>(medicalCase.Consultation);
                return new List<ConsultationDto> { dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询辨证记录失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 查询处方列表
        /// Epic #1612: 返回病案的所有历史处方记录
        /// </summary>
        public async Task<List<PrescriptionDto>> GetPrescriptionListAsync(Guid medicalCaseId)
        {
            try
            {
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase?.Prescription == null)
                {
                    return new List<PrescriptionDto>();
                }

                // 当前架构下只有一条Prescription（一诊一方），直接映射
                var dto = _mapper.Map<PrescriptionDto>(medicalCase.Prescription);
                return new List<PrescriptionDto> { dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询处方列表失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// Epic #2210 Task 3.1.2: 添加doctorId参数
        /// </summary>
        public async Task<MedicalCase?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId)
        {
            try
            {
                _logger.LogInformation("查询患者未完成医案，PatientId: {PatientId}, DoctorId: {DoctorId}",
                    patientId, doctorId);

                // Epic #2210 Task 3.1.2: 直接传递doctorId到Repository，无额外业务逻辑
                var result = await _repository.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId);

                if (result != null)
                {
                    _logger.LogInformation("找到未完成医案，MedicalCaseId: {MedicalCaseId}, CaseStatus: {CaseStatus}, DoctorId: {DoctorId}",
                        result.Id, result.CaseStatus, result.DoctorId);
                }
                else
                {
                    _logger.LogInformation("未找到患者的未完成医案，PatientId: {PatientId}, DoctorId: {DoctorId}",
                        patientId, doctorId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询患者未完成医案失败，PatientId: {PatientId}, DoctorId: {DoctorId}",
                    patientId, doctorId);
                throw;
            }
        }

        /// <summary>
        /// 获取待看诊队列（Status = Active的医案患者列表）
        /// Epic #2210 Phase 3: P0 Bug修复 - 实现缺失的Service方法
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId)
        {
            try
            {
                _logger.LogInformation("获取待看诊队列，DoctorId: {DoctorId}", doctorId);

                // Epic #2210: 直接委托给Repository，传递doctorId进行数据隔离
                var result = await _repository.GetPendingCasesAsync(doctorId);

                _logger.LogInformation("待看诊队列查询完成，DoctorId: {DoctorId}, Count: {Count}",
                    doctorId, result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取待看诊队列失败，DoctorId: {DoctorId}", doctorId);
                throw;
            }
        }

        /// <summary>
        /// 获取所有待看诊队列（管理员专用）
        /// 业务规则：返回所有Active状态医案的患者信息，不限定医生
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync()
        {
            try
            {
                _logger.LogInformation("获取所有待看诊队列（管理员）");

                var result = await _repository.GetAllPendingCasesAsync();

                _logger.LogInformation("待看诊队列查询完成（管理员），Count: {Count}", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有待看诊队列失败（管理员）");
                throw;
            }
        }

    }
}
