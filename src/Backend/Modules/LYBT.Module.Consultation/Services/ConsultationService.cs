using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Models.Consultation;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 看诊服务实现（替代DiagnosisTreatmentService）
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly IConsultationRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationService> _logger;

        public ConsultationService(
            IConsultationRepository repository,
            IMapper mapper,
            ILogger<ConsultationService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 获取看诊列表
        /// </summary>
        public async Task<List<ConsultationDto>> GetListAsync()
        {
            try
            {
                var models = await _repository.GetListAsync();
                return _mapper.Map<List<ConsultationDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊列表失败");
                throw;
            }
        }

        /// <summary>
        /// 分页获取看诊列表
        /// </summary>
        public async Task<PaginatedResult<ConsultationDto>> GetPagedAsync(PaginationRequest request)
        {
            try
            {
                var models = await _repository.GetListAsync();
                var dtos = _mapper.Map<List<ConsultationDto>>(models);

                // 搜索过滤
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    dtos = dtos.Where(x =>
                        x.PatientName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        x.DoctorName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        x.Diagnosis.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // 排序
                dtos = request.SortBy?.ToLower() switch
                {
                    "patientname" => request.SortDesc ? dtos.OrderByDescending(x => x.PatientName).ToList() : dtos.OrderBy(x => x.PatientName).ToList(),
                    "doctorname" => request.SortDesc ? dtos.OrderByDescending(x => x.DoctorName).ToList() : dtos.OrderBy(x => x.DoctorName).ToList(),
                    "consultationtime" => request.SortDesc ? dtos.OrderByDescending(x => x.ConsultationTime).ToList() : dtos.OrderBy(x => x.ConsultationTime).ToList(),
                    _ => dtos.OrderByDescending(x => x.ConsultationTime).ToList()
                };

                // 分页
                var total = dtos.Count;
                var items = dtos
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new PaginatedResult<ConsultationDto>
                {
                    Items = items,
                    TotalCount = total,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页获取看诊列表失败");
                throw;
            }
        }

        /// <summary>
        /// 获取看诊详情
        /// </summary>
        public async Task<ConsultationDetailDto?> GetByIdAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                return model == null ? null : _mapper.Map<ConsultationDetailDto>(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊详情失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建看诊记录
        /// </summary>
        public async Task<ConsultationDetailDto> CreateAsync(ConsultationCreateDto dto)
        {
            try
            {
                var model = _mapper.Map<ConsultationModel>(dto);
                model.Id = Guid.NewGuid();
                model.ConsultationTime = DateTime.Now;
                model.CreateTime = DateTime.Now;
                model.IsActive = true;

                var created = await _repository.CreateAsync(model);
                
                // 更新医疗案例状态为看诊中
                // TODO: 调用MedicalCaseService更新状态

                return _mapper.Map<ConsultationDetailDto>(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建看诊记录失败");
                throw;
            }
        }

        /// <summary>
        /// 更新看诊记录
        /// </summary>
        public async Task<bool> UpdateAsync(Guid id, ConsultationUpdateDto dto)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("看诊记录不存在，ID: {Id}", id);
                    return false;
                }

                // 更新字段
                if (!string.IsNullOrWhiteSpace(dto.ChiefComplaint))
                    model.ChiefComplaint = dto.ChiefComplaint;
                if (!string.IsNullOrWhiteSpace(dto.PresentIllness))
                    model.PresentIllness = dto.PresentIllness;
                if (!string.IsNullOrWhiteSpace(dto.PastHistory))
                    model.PastHistory = dto.PastHistory;
                if (!string.IsNullOrWhiteSpace(dto.AllergyHistory))
                    model.AllergyHistory = dto.AllergyHistory;
                if (!string.IsNullOrWhiteSpace(dto.PhysicalExamination))
                    model.PhysicalExamination = dto.PhysicalExamination;
                if (!string.IsNullOrWhiteSpace(dto.TongueInspection))
                    model.TongueInspection = dto.TongueInspection;
                if (!string.IsNullOrWhiteSpace(dto.PulseCondition))
                    model.PulseCondition = dto.PulseCondition;
                if (!string.IsNullOrWhiteSpace(dto.TCMDiagnosis))
                    model.TCMDiagnosis = dto.TCMDiagnosis;
                if (!string.IsNullOrWhiteSpace(dto.WesternDiagnosis))
                    model.WesternDiagnosis = dto.WesternDiagnosis;
                if (!string.IsNullOrWhiteSpace(dto.Diagnosis))
                    model.Diagnosis = dto.Diagnosis;
                if (!string.IsNullOrWhiteSpace(dto.TreatmentPrinciple))
                    model.TreatmentPrinciple = dto.TreatmentPrinciple;
                if (!string.IsNullOrWhiteSpace(dto.MedicalAdvice))
                    model.MedicalAdvice = dto.MedicalAdvice;

                model.UpdateTime = DateTime.Now;

                return await _repository.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新看诊记录失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 删除看诊记录（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("看诊记录不存在，ID: {Id}", id);
                    return false;
                }

                model.IsActive = false;
                model.UpdateTime = DateTime.Now;

                return await _repository.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除看诊记录失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取看诊记录
        /// </summary>
        public async Task<ConsultationDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var model = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
                return model == null ? null : _mapper.Map<ConsultationDetailDto>(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医疗案例ID获取看诊记录失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 根据患者ID获取看诊历史
        /// </summary>
        public async Task<List<ConsultationDto>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var models = await _repository.GetByPatientIdAsync(patientId);
                return _mapper.Map<List<ConsultationDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取看诊历史失败，PatientId: {PatientId}", patientId);
                throw;
            }
        }

        /// <summary>
        /// 根据医生ID获取看诊记录
        /// </summary>
        public async Task<List<ConsultationDto>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                var models = await _repository.GetByDoctorIdAsync(doctorId);
                return _mapper.Map<List<ConsultationDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医生ID获取看诊记录失败，DoctorId: {DoctorId}", doctorId);
                throw;
            }
        }

        /// <summary>
        /// 获取今日看诊列表
        /// </summary>
        public async Task<List<ConsultationDto>> GetTodayConsultationsAsync()
        {
            try
            {
                var today = DateTime.Today;
                var models = await _repository.GetByDateRangeAsync(today, today.AddDays(1));
                return _mapper.Map<List<ConsultationDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取今日看诊列表失败");
                throw;
            }
        }

        /// <summary>
        /// 完成看诊
        /// </summary>
        public async Task<bool> CompleteConsultationAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("看诊记录不存在，ID: {Id}", id);
                    return false;
                }

                model.UpdateTime = DateTime.Now;
                var result = await _repository.UpdateAsync(model);

                // TODO: 更新医疗案例状态为待付费

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成看诊失败，ID: {Id}", id);
                throw;
            }
        }
    }
}