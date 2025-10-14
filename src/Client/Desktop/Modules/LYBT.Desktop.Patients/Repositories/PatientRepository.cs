using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Repositories
{
    /// <summary>
    /// 患者数据仓储实现 - RepositoryBase统一架构
    /// Project Standardization 3.0 - 迁移到统一RepositoryBase
    /// </summary>
    public class PatientRepository : RepositoryBase<PatientDto, PatientCreateDto, PatientUpdateDto, IPatientApi>, IPatientRepository
    {
        public PatientRepository(
            IPatientApi patientApi,
            ILogger<PatientRepository> logger)
            : base(patientApi, logger)
        {
        }

        /// <summary>
        /// 获取所有患者（通过分页获取第一页的大量数据）
        /// </summary>
        public async Task<List<PatientDto>> GetAllAsync()
        {
            try
            {
                var pagedResult = await GetPagedAsync(1, 10000);
                return pagedResult.Items ?? new List<PatientDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有患者失败");
                return new List<PatientDto>();
            }
        }

        #region RepositoryBase抽象方法实现

        protected override Task<Refit.ApiResponse<PatientDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetPatientByIdAsync(id);
        }

        protected override Task<Refit.ApiResponse<PagedResult<PatientDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetPatientsAsync(page, pageSize, keyword);
        }

        protected override Task<Refit.ApiResponse<PatientDto>> CallApiCreateAsync(PatientCreateDto dto)
        {
            return _api.CreatePatientAsync(dto);
        }

        protected override Task<Refit.ApiResponse<PatientDto>> CallApiUpdateAsync(Guid id, PatientUpdateDto dto)
        {
            return _api.UpdatePatientAsync(id, dto);
        }

        protected override Task<Refit.ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
        {
            return _api.DeletePatientAsync(id);
        }

        protected override Guid? GetIdFromUpdateDto(PatientUpdateDto dto)
        {
            return dto?.Id;
        }

        #endregion
    }
}