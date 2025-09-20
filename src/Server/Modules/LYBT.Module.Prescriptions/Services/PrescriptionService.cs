using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Services
{

    /// <summary>
    /// 处方服务 - UltraThink双层架构纯委托模式
    /// </summary>
    public class PrescriptionService(
        IPrescriptionQueryService queryService,
        IPrescriptionBusinessService businessService) : IPrescriptionService
    {
        private readonly IPrescriptionQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        private readonly IPrescriptionBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

        #region Query Operations

        /// <inheritdoc/>
        public Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
            => _queryService.GetByIdAsync(id);

        /// <inheritdoc/>
        public Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
            => _queryService.GetPagedAsync(query);

        /// <inheritdoc/>
        public Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
            => _queryService.GetByPatientIdAsync(patientId);

        /// <inheritdoc/>
        public Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
            => _queryService.GetByMedicalCaseIdAsync(medicalCaseId);

        /// <inheritdoc/>
        public Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
            => _queryService.SearchAsync(keyword);

        public async Task<List<PrescriptionDto>> GetAllAsync()
        {
            var result = await _queryService.GetAllAsync();
            return result.IsSuccess ? (result.Data ?? []) : [];
        }

        public async Task<List<PrescriptionDto>> GetDoctorTodayPrescriptionsAsync(Guid doctorId)
        {
            var result = await _queryService.GetDoctorTodayPrescriptionsAsync(doctorId);
            return result.IsSuccess ? (result.Data ?? []) : [];
        }

        #endregion Query Operations

        #region Business Operations

        /// <inheritdoc/>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
            => await _businessService.CreateAsync(dto);

        /// <inheritdoc/>
        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto dto)
            => await _businessService.UpdateAsync(id, dto);

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
            => await _businessService.DeleteAsync(id);

        /// <inheritdoc/>
        public Task<ServiceResult<PrescriptionValidationResult>> ValidateAsync(PrescriptionCreateDto dto)
        {
            var result = new PrescriptionValidationResult
            {
                IsValid = !string.IsNullOrWhiteSpace(dto.Diagnosis) && dto.PatientId != Guid.Empty,
                Errors = []
            };

            if (string.IsNullOrWhiteSpace(dto.Diagnosis))
            {
                result.Errors.Add("处方诊断不能为空");
            }

            if (dto.PatientId == Guid.Empty)
            {
                result.Errors.Add("患者ID不能为空");
            }

            result.IsValid = result.Errors.Count == 0;
            return Task.FromResult(ServiceResult<PrescriptionValidationResult>.Success(result));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName)
        {
            var operatorId = Guid.Empty;
            var operatorName = "System";
            return await _businessService.CopyAsync(id, newName, operatorId, operatorName);
        }

        public async Task<PrescriptionDto?> CopyLastPrescriptionAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {
            var result = await _businessService.CopyLastPrescriptionAsync(patientId, doctorId, operatorId, operatorName);
            return result.IsSuccess ? result.Data : null;
        }


        public async Task<bool> QuickSaveAsync(Guid prescriptionId, QuickPrescriptionDto dto, Guid operatorId, string operatorName)
        {
            var result = await _businessService.QuickSaveAsync(prescriptionId, dto, operatorId, operatorName);
            return result.IsSuccess && result.Data;
        }

        public async Task<bool> CancelAsync(string id, Guid operatorId, string operatorName)
        {
            if (!Guid.TryParse(id, out var guid))
            {
                return false;
            }

            var result = await _businessService.CancelAsync(guid, operatorId, operatorName);
            return result.IsSuccess && result.Data;
        }

        #endregion Business Operations
    }
}
