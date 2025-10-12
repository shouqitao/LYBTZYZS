using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Repositories
{
    /// <summary>
    /// 患者数据仓储实现 - Phase 2模块化架构
    /// Issue #1114 - 支持CreateDto和UpdateDto
    /// </summary>
    public class PatientRepository : BaseApiRepository<PatientDto>, IPatientRepository
    {
        public PatientRepository(
            IApiService apiService,
            ILogger<PatientRepository> logger)
            : base(apiService, logger, "api/v1/patients")
        {
        }

        public override Task<List<PatientDto>> GetAllAsync()
        {
            return base.GetAllAsync();
        }

        public override Task<PatientDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        /// <summary>
        /// 创建新患者（使用CreateDto）
        /// </summary>
        public async Task<PatientDto> CreateAsync(PatientCreateDto patient)
        {
            if (patient == null)
                throw new ArgumentNullException(nameof(patient));

            return (await _apiService.PostAsync<PatientCreateDto, PatientDto>(_endpoint, patient))!;
        }

        /// <summary>
        /// 更新患者信息（使用UpdateDto）
        /// </summary>
        public async Task<PatientDto> UpdateAsync(PatientUpdateDto patient)
        {
            if (patient?.Id == null || patient.Id == Guid.Empty)
            {
                _logger.LogError("Cannot update patient with null or invalid id");
                throw new ArgumentException("Patient ID is required", nameof(patient));
            }

            return (await _apiService.PutAsync<PatientUpdateDto, PatientDto>($"{_endpoint}/{patient.Id}", patient))!;
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public override Task<List<PatientDto>> SearchAsync(string keyword)
        {
            return base.SearchAsync(keyword);
        }

        /// <summary>
        /// 分页查询患者列表（服务端分页）- P0性能修复
        /// </summary>
        public override Task<PagedResult<PatientDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return base.GetPagedAsync(page, pageSize, keyword);
        }
    }
}
