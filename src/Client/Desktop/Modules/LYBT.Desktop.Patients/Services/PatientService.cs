using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Services
{
    /// <summary>
    /// 患者Service - 业务逻辑处理
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// OpenSpec: refactor-frontend-srp-patterns - 迁移到Services目录
    /// OpenSpec: cleanup-patient-dead-code - 清理未使用的事件和Command
    /// 负责处理患者相关的业务操作
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            IPatientRepository patientRepository,
            ILogger<PatientService> logger)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 患者CRUD操作

        /// <summary>
        /// 创建患者
        /// OpenSpec: enhance-dataflow-logging - LOG-018 统一[SVC]前缀
        /// </summary>
        public async Task<CommandResult<PatientDetailDto>> CreatePatientAsync(PatientInputDto inputDto)
        {
            try
            {
                _logger.LogInformation("[SVC] Patient.Create started - Name={PatientName}", inputDto.Name);

                var patient = await _patientRepository.CreateAsync(inputDto);
                _logger.LogInformation("[SVC] Patient.Create completed - PatientId={PatientId}", patient.Id);
                return CommandResult<PatientDetailDto>.Succeeded(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.Create failed - Name={PatientName}", inputDto.Name);
                return CommandResult<PatientDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建患者", ex));
            }
        }

        /// <summary>
        /// 更新患者
        /// </summary>
        public async Task<CommandResult<PatientDetailDto>> UpdatePatientAsync(PatientInputDto inputDto)
        {
            try
            {
                _logger.LogInformation("[SVC] Patient.Update started - PatientId={PatientId}", inputDto.Id);

                var patient = await _patientRepository.UpdateAsync(inputDto);
                _logger.LogInformation("[SVC] Patient.Update completed - PatientId={PatientId}", patient.Id);
                return CommandResult<PatientDetailDto>.Succeeded(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.Update failed - PatientId={PatientId}", inputDto.Id);
                return CommandResult<PatientDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("更新患者", ex));
            }
        }

        /// <summary>
        /// 删除患者
        /// </summary>
        public async Task<CommandResult<bool>> DeletePatientAsync(Guid patientId)
        {
            try
            {
                _logger.LogInformation("[SVC] Patient.Delete started - PatientId={PatientId}", patientId);

                await _patientRepository.DeleteAsync(patientId);
                _logger.LogInformation("[SVC] Patient.Delete completed - PatientId={PatientId}", patientId);
                return CommandResult<bool>.Succeeded(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.Delete failed - PatientId={PatientId}", patientId);
                return CommandResult<bool>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除患者", ex));
            }
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量删除患者
        /// OpenSpec: optimize-batch-operations Phase 2 - 使用单次批量API调用
        /// </summary>
        public async Task<CommandResult<BatchOperationResultDto>> BatchDeletePatientsAsync(IEnumerable<Guid> patientIds)
        {
            try
            {
                var ids = patientIds?.ToList() ?? new List<Guid>();
                _logger.LogInformation("[SVC] Patient.BatchDelete started - Count={Count}", ids.Count);

                if (!ids.Any())
                {
                    return CommandResult<BatchOperationResultDto>.Failed("没有选择要删除的患者");
                }

                // OpenSpec: optimize-batch-operations - 使用单次批量API调用替代N+1模式
                var result = await _patientRepository.BatchDeleteAsync(ids);
                if (result == null)
                {
                    _logger.LogWarning("[SVC] Patient.BatchDelete failed");
                    return CommandResult<BatchOperationResultDto>.Failed("批量删除患者失败");
                }

                if (result.FailureCount == 0)
                {
                    _logger.LogInformation("[SVC] Patient.BatchDelete completed - Success={SuccessCount}", result.SuccessCount);
                }
                else
                {
                    _logger.LogWarning("[SVC] Patient.BatchDelete partial - Success={SuccessCount} Failure={FailureCount}",
                        result.SuccessCount, result.FailureCount);
                }

                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.BatchDelete failed");
                return CommandResult<BatchOperationResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量删除患者", ex));
            }
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 搜索患者
        /// </summary>
        public async Task<CommandResult<IEnumerable<PatientListDto>>> SearchPatientsAsync(string keyword)
        {
            try
            {
                _logger.LogDebug("[SVC] Patient.Search started - Keyword={Keyword}", keyword);

                var patients = await _patientRepository.SearchAsync(keyword);
                _logger.LogDebug("[SVC] Patient.Search completed - Count={Count}", patients.Count);
                return CommandResult<IEnumerable<PatientListDto>>.Succeeded(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.Search failed - Keyword={Keyword}", keyword);
                return CommandResult<IEnumerable<PatientListDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("搜索患者", ex));
            }
        }

        /// <summary>
        /// 分页查询患者
        /// </summary>
        public async Task<CommandResult<PagedResult<PatientListDto>>> GetPatientsPagedAsync(int page, int pageSize, string? keyword = null)
        {
            try
            {
                _logger.LogDebug("[SVC] Patient.GetPaged started - Page={Page} PageSize={PageSize}", page, pageSize);

                var result = await _patientRepository.GetPagedAsync(page, pageSize, keyword);
                _logger.LogDebug("[SVC] Patient.GetPaged completed - Count={Count}", result.Items.Count);
                return CommandResult<PagedResult<PatientListDto>>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.GetPaged failed");
                return CommandResult<PagedResult<PatientListDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("查询患者列表", ex));
            }
        }

        /// <summary>
        /// 根据ID获取患者（Issue #1788: 支持单个患者查询）
        /// </summary>
        public async Task<CommandResult<PatientDetailDto>> GetByIdAsync(Guid patientId)
        {
            try
            {
                _logger.LogDebug("[SVC] Patient.GetById started - PatientId={PatientId}", patientId);

                var patient = await _patientRepository.GetByIdAsync(patientId);

                if (patient == null)
                {
                    _logger.LogWarning("[SVC] Patient.GetById → NotFound - PatientId={PatientId}", patientId);
                    return CommandResult<PatientDetailDto>.Failed("患者不存在");
                }

                _logger.LogDebug("[SVC] Patient.GetById completed - Name={PatientName}", patient.Name);
                return CommandResult<PatientDetailDto>.Succeeded(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] Patient.GetById failed - PatientId={PatientId}", patientId);
                return CommandResult<PatientDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("查询患者", ex));
            }
        }

        #endregion
    }

    // OpenSpec: cleanup-patient-dead-code - 本地CommandResult<T>已迁移到LYBT.Desktop.Contracts.CommandHandlers
}
