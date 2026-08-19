using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LYBT.Desktop.Patients.Services
{
    /// <summary>
    /// 患者Service - 业务逻辑处理
    /// 负责处理患者相关的业务操作
    /// D4: Remote* 前缀统一（对齐 UserService/RegistrationService/HerbService；
    ///     契约层 IPatientService 无前缀为接口惯例，实现类 Remote 标识 HTTP 数据服务）
    /// </summary>
    public class PatientService : CrudServiceBase<PatientListDto, PatientDetailDto, PatientInputDto>, IPatientService
    {
        private readonly IPatientRepository _patientRepository;

        public PatientService(
            IPatientRepository patientRepository,
            ILogger<PatientService> logger)
            : base(logger, "Patient")
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
        }

        #region Core 实现

        protected override async Task<PatientDetailDto> CreateCoreAsync(PatientInputDto input, CancellationToken ct)
            => await _patientRepository.CreateAsync(input, ct);

        protected override async Task<PatientDetailDto> UpdateCoreAsync(PatientInputDto input, CancellationToken ct)
            => await _patientRepository.UpdateAsync(input, ct);

        protected override async Task DeleteCoreAsync(Guid id, CancellationToken ct)
            => await _patientRepository.DeleteAsync(id, ct);

        protected override async Task<PatientDetailDto?> GetByIdCoreAsync(Guid id, CancellationToken ct)
            => await _patientRepository.GetByIdAsync(id, ct);

        protected override async Task<PagedResult<PatientListDto>> GetPagedCoreAsync(int page, int pageSize, string? keyword, CancellationToken ct)
            => await _patientRepository.GetPagedAsync(page, pageSize, keyword, ct);

        protected override async Task<List<PatientListDto>> SearchCoreAsync(string keyword, CancellationToken ct)
            => await _patientRepository.SearchAsync(keyword, ct);

        protected override Task<PatientDetailDto?> ToggleStatusCoreAsync(Guid id, CancellationToken ct)
            => throw new NotSupportedException("患者模块不支持状态切换");

        #endregion

        #region 批量导入/导出

        /// <summary>
        /// 批量导入患者数据
        /// </summary>
        public async Task<CommandResult<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default)
        {
            return await ExecuteAsync<PatientBatchImportResultDto>("Patient.BatchImport", async () =>
            {
                var result = await _patientRepository.BatchImportAsync(request, ct);
                if (result == null)
                    return CommandResult<PatientBatchImportResultDto>.Failed("批量导入操作失败");
                return CommandResult<PatientBatchImportResultDto>.Succeeded(result);
            });
        }

        /// <summary>
        /// 下载患者导入模板
        /// </summary>
        public async Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync<byte[]>("Patient.ExportTemplate", async () =>
            {
                var data = await _patientRepository.ExportTemplateAsync(ct);
                if (data == null)
                    return CommandResult<byte[]>.Failed("导出模板操作失败");
                return CommandResult<byte[]>.Succeeded(data);
            });
        }

        /// <summary>
        /// 导出患者数据到Excel
        /// </summary>
        public async Task<CommandResult<byte[]>> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default)
        {
            return await ExecuteAsync<byte[]>("Patient.ExportPatients", async () =>
            {
                var data = await _patientRepository.ExportPatientsAsync(keyword, ct);
                if (data == null)
                    return CommandResult<byte[]>.Failed("导出患者数据操作失败");
                return CommandResult<byte[]>.Succeeded(data);
            });
        }

        #endregion
    }
}
