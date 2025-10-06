using LYBT.Desktop.Services.Exceptions;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// 患者服务实现 - UltraThink架构
    /// 实现Shared.Interfaces统一接口，返回ServiceResult包装
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly ILogger<PatientService> _logger;
        private readonly IPatientRepository _repository;
        private readonly IExceptionHandler _exceptionHandler;

        public PatientService(
            IPatientRepository repository,
            ILogger<PatientService> logger,
            IExceptionHandler exceptionHandler)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allPatients = await _repository.GetAllAsync();

                // 应用关键词搜索
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    allPatients = allPatients.Where(p =>
                        p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (p.IdNumber != null && p.IdNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // 分页
                var totalCount = allPatients.Count;
                var items = allPatients
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResult = new PagedResult<PatientDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<PatientDto>>.Success(pagedResult);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var patient = await _repository.GetByIdAsync(id);
                return ServiceResult<PatientDto>.Success(patient);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"创建患者: {dto.Name}");

                // 转换DTO
                var patient = new PatientDto
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Gender = dto.Gender,
                    BirthDate = dto.BirthDate,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    IdNumber = dto.IdNumber,
                    AllergyHistory = dto.AllergyHistory,
                    MaritalStatus = dto.MaritalStatus,
                    IdType = dto.IdType,
                    BloodType = dto.BloodType,
                    EmergencyContactName = dto.EmergencyContactName,
                    EmergencyContactPhone = dto.EmergencyContactPhone,
                    EmergencyContactRelation = dto.EmergencyContactRelation,
                    Status = dto.Status,
                    VisitCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var created = await _repository.CreateAsync(patient);
                return ServiceResult<PatientDto>.Success(created);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 先获取现有数据
                var existing = await _repository.GetByIdAsync(id);

                // 更新字段
                existing.Name = dto.Name;
                existing.Gender = dto.Gender;
                existing.BirthDate = dto.BirthDate;
                existing.PhoneNumber = dto.PhoneNumber;
                existing.Address = dto.Address;
                existing.IdNumber = dto.IdNumber;
                existing.AllergyHistory = dto.AllergyHistory;
                existing.MaritalStatus = dto.MaritalStatus;
                existing.IdType = dto.IdType;
                existing.BloodType = dto.BloodType;
                existing.EmergencyContactName = dto.EmergencyContactName;
                existing.EmergencyContactPhone = dto.EmergencyContactPhone;
                existing.EmergencyContactRelation = dto.EmergencyContactRelation;
                existing.Status = dto.Status;
                existing.UpdatedAt = DateTime.UtcNow;

                var updated = await _repository.UpdateAsync(existing);
                return ServiceResult<PatientDto>.Success(updated);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                await _repository.DeleteAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }

        public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var patients = await _repository.SearchAsync(keyword);
                return ServiceResult<List<PatientDto>>.Success(patients);
            }, nameof(SearchAsync));
        }
    }
}
