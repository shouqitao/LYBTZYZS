using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 处方服务 - UltraThink双层架构纯委托模式
    /// </summary>
    public class PrescriptionService : IPrescriptionService
    {
        private readonly PrescriptionQueryService _queryService;
        private readonly PrescriptionBusinessService _businessService;

        public PrescriptionService(
            PrescriptionQueryService queryService,
            PrescriptionBusinessService businessService)
        {
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        }

        #region Query Operations

        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
            => await _queryService.GetByIdAsync(id);

        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
            => await _queryService.GetPagedAsync(query);

        public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
            => await _queryService.GetByPatientIdAsync(patientId);

        public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
            => await _queryService.GetByMedicalCaseIdAsync(medicalCaseId);

        public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
            => await _queryService.SearchAsync(keyword);

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

        #endregion

        #region Business Operations

        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            await Task.CompletedTask;
            return ServiceResult<PrescriptionDto>.Failure("CreateAsync方法需要在BusinessService中实现");
        }

        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto dto)
        {
            await Task.CompletedTask;
            return ServiceResult<PrescriptionDto>.Failure("UpdateAsync方法需要在BusinessService中实现");
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            await Task.CompletedTask;
            return ServiceResult<bool>.Failure("DeleteAsync方法需要在BusinessService中实现");
        }

        public async Task<ServiceResult<PrescriptionValidationResult>> ValidateAsync(PrescriptionCreateDto dto)
        {
            await Task.CompletedTask;
            var result = new PrescriptionValidationResult
            {
                IsValid = !string.IsNullOrWhiteSpace(dto.Diagnosis) && dto.PatientId != Guid.Empty,
                Errors = []
            };

            if (string.IsNullOrWhiteSpace(dto.Diagnosis))
                result.Errors.Add("处方诊断不能为空");

            if (dto.PatientId == Guid.Empty)
                result.Errors.Add("患者ID不能为空");

            result.IsValid = result.Errors.Count == 0;
            return ServiceResult<PrescriptionValidationResult>.Success(result);
        }

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

        public async Task<PrescriptionDto?> CreateFromTemplateAsync(Guid templateId, Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {
            var result = await _businessService.CreateFromTemplateAsync(templateId, patientId, doctorId, operatorId, operatorName);
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
                return false;

            var result = await _businessService.CancelAsync(guid, operatorId, operatorName);
            return result.IsSuccess && result.Data;
        }

        #endregion
    }
}