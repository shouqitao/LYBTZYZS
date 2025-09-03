using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者服务 - UltraThink简化架构纯委托模式
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly PatientQueryService _queryService;
        private readonly PatientBusinessService _businessService;

        public PatientService(
            PatientQueryService queryService,
            PatientBusinessService businessService)
        {
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        }

        #region Core Operations - 委托给BusinessService

        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
            => await _businessService.GetByIdAsync(id);

        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
            => await _businessService.CreateAsync(dto);

        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
            => await _businessService.UpdateAsync(id, dto);

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
            => await _businessService.DeleteAsync(id);

        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName)
        {
            var result = await _businessService.DeleteAsync(id);
            return result.IsSuccess && result.Data;
        }

        public async Task<bool> SetStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName)
        {
            var result = await _businessService.UpdateStatusAsync(id, isActive);
            return result.IsSuccess && result.Data;
        }

        #endregion

        #region Query Operations

        public async Task<List<PatientDto>> GetAllAsync()
        {
            var result = await _queryService.GetAllActiveAsync();
            return result.IsSuccess ? (result.Data ?? []) : [];
        }

        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query)
            => await _queryService.GetPagedAsync(query);

        public async Task<List<PatientDto>> GetActivePatientsAsync()
        {
            var result = await _queryService.GetAllActiveAsync();
            return result.IsSuccess ? (result.Data ?? []) : [];
        }

        public async Task<PatientDto?> GetByPhoneNumberAsync(string phoneNumber)
        {
            var result = await _queryService.GetByPhoneNumberAsync(phoneNumber);
            return result.IsSuccess ? result.Data : null;
        }

        public async Task<PatientDto?> GetByIDNumberAsync(string idNumber)
        {
            var result = await _queryService.GetByIdNumberAsync(idNumber);
            return result.IsSuccess ? result.Data : null;
        }

        public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
            => await _queryService.SearchAsync(keyword);

        public async Task<PagedResult<PatientDto>> AdvancedSearchAsync(PatientAdvancedSearchDto query)
        {
            var result = await _queryService.AdvancedSearchAsync(query);
            return result.IsSuccess ? (result.Data ?? new PagedResult<PatientDto>
            {
                TotalCount = 0,
                Items = [],
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            }) : new PagedResult<PatientDto>
            {
                TotalCount = 0,
                Items = [],
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        public async Task<List<PatientDto>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber)
        {
            var result = await _queryService.CheckDuplicatePatientsAsync(idNumber, phoneNumber);
            return result.IsSuccess ? (result.Data ?? []) : [];
        }

        #endregion

        #region Business Operations

        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            var result = await _businessService.EnableAsync(id);
            return result.IsSuccess 
                ? ServiceResult.Success() 
                : ServiceResult.Failure(result.ErrorMessage ?? "启用患者失败");
        }

        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            var result = await _businessService.DisableAsync(id);
            return result.IsSuccess 
                ? ServiceResult.Success() 
                : ServiceResult.Failure(result.ErrorMessage ?? "禁用患者失败");
        }

        public async Task<ServiceResult<object>> ImportPatientsAsync(List<PatientCreateDto> patients)
            => await _businessService.ImportPatientsAsync(patients);

        public async Task<ServiceResult<byte[]>> ExportPatientsAsync(PagedQueryBaseDto query)
            => await _businessService.ExportPatientsAsync(query);

        public async Task<ServiceResult<object>> ValidatePatientAsync(PatientCreateDto dto)
            => await _businessService.ValidatePatientAsync(dto);

        public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
        {
            await Task.CompletedTask;
            return _businessService.GenerateImportTemplate();
        }

        #endregion

        #region Shared Interface

        public async Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard)
            => await _queryService.GetByIdNumberAsync(idCard);

        public async Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone)
        {
            var result = await _queryService.GetByPhoneNumberAsync(phone);
            if (result.IsSuccess && result.Data != null)
            {
                return ServiceResult<List<PatientDto>>.Success([result.Data]);
            }
            return ServiceResult<List<PatientDto>>.Success([]);
        }

        #endregion
    }
}