using LYBT.Desktop.Services.Http;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Repositories
{
    /// <summary>
    /// 患者数据仓储实现 - API集成 - UltraThink架构
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

        public override Task<PatientDto> CreateAsync(PatientDto patient)
        {
            return base.CreateAsync(patient);
        }

        public Task<PatientDto> UpdateAsync(PatientDto patient)
        {
            if (patient?.Id == null)
            {
                _logger.LogError("Cannot update patient with null or invalid id");
                return Task.FromResult<PatientDto>(null!);
            }
            return base.UpdateAsync(patient.Id, patient);
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public override Task<List<PatientDto>> SearchAsync(string keyword)
        {
            return base.SearchAsync(keyword);
        }
    }
}
