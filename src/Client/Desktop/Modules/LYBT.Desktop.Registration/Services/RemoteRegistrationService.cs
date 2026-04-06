using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LYBT.Desktop.Registration.Services;

/// <summary>
/// 挂号Remote Service实现
/// PRD: registration.md US-REG-001~006
/// </summary>
public class RemoteRegistrationService : IRegistrationService
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ILogger<RemoteRegistrationService> _logger;

    public RemoteRegistrationService(
        IRegistrationRepository registrationRepository,
        ILogger<RemoteRegistrationService> logger)
    {
        _registrationRepository = registrationRepository ?? throw new ArgumentNullException(nameof(registrationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 创建挂号 (前台模式)
    /// US-REG-001: Source=Receptionist, Status=Waiting
    /// </summary>
    public async Task<CommandResult<RegistrationDetailDto>> CreateAsync(RegistrationInputDto request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[SVC] Registration.Create started - PatientId={PatientId}", request.PatientId);

            var registration = await _registrationRepository.CreateAsync(request);
            _logger.LogInformation("[SVC] Registration.Create completed - RegistrationId={RegistrationId}", registration.Id);
            return CommandResult<RegistrationDetailDto>.Succeeded(registration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Registration.Create failed - PatientId={PatientId}", request.PatientId);
            return CommandResult<RegistrationDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建挂号", ex));
        }
    }

    /// <summary>
    /// 获取挂号详情
    /// </summary>
    public async Task<CommandResult<RegistrationDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[SVC] Registration.GetById - RegistrationId={RegistrationId}", id);

            var registration = await _registrationRepository.GetByIdAsync(id);
            if (registration == null)
                return CommandResult<RegistrationDetailDto>.NotFound("挂号记录不存在");

            return CommandResult<RegistrationDetailDto>.Succeeded(registration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Registration.GetById failed - RegistrationId={RegistrationId}", id);
            return CommandResult<RegistrationDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取挂号详情", ex));
        }
    }

    /// <summary>
    /// 分页查询挂号记录
    /// </summary>
    public async Task<CommandResult<PagedResult<RegistrationListDto>>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[SVC] Registration.GetPaged - Page={Page}, PageSize={PageSize}, Keyword={Keyword}",
                page, pageSize, keyword);

            var result = await _registrationRepository.GetPagedAsync(page, pageSize, keyword);
            return CommandResult<PagedResult<RegistrationListDto>>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Registration.GetPaged failed - Page={Page}, Keyword={Keyword}", page, keyword);
            return CommandResult<PagedResult<RegistrationListDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("分页查询挂号", ex));
        }
    }

    /// <summary>
    /// 获取等待队列
    /// US-REG-003: Waiting 状态，按挂号时间升序?
    /// </summary>
    public async Task<CommandResult<List<RegistrationListDto>>> GetQueueAsync(
        Guid? doctorId = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("[SVC] Registration.GetQueue - DoctorId={DoctorId}", doctorId);

            var queue = await _registrationRepository.GetWaitingQueueAsync(doctorId);
            return CommandResult<List<RegistrationListDto>>.Succeeded(queue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Registration.GetQueue failed - DoctorId={DoctorId}", doctorId);
            return CommandResult<List<RegistrationListDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取等待队列", ex));
        }
    }

    /// <summary>
    /// 接诊: Registration -> InProgress
    /// US-REG-003 验收标准第4条?�?
    /// </summary>
    public async Task<CommandResult<Guid>> StartVisitAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[SVC] Registration.StartVisit started - RegistrationId={RegistrationId}", id);

            var medicalCaseId = await _registrationRepository.StartVisitAsync(id);
            if (medicalCaseId.HasValue)
            {
                _logger.LogInformation("[SVC] Registration.StartVisit completed - RegistrationId={RegistrationId}, MedicalCaseId={MedicalCaseId}",
                    id, medicalCaseId.Value);
                return CommandResult<Guid>.Succeeded(medicalCaseId.Value);
            }
            else
            {
                return CommandResult<Guid>.Failed("接诊失败，请稍后重试");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Registration.StartVisit failed - RegistrationId={RegistrationId}", id);
            return CommandResult<Guid>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("接诊", ex));
        }
    }

    /// <summary>
    /// 取消挂号
    /// US-REG-004: �?Waiting 状态可取消
    /// </summary>
    public async Task<CommandResult> CancelAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[SVC] Registration.Cancel started - RegistrationId={RegistrationId}", id);

            var success = await _registrationRepository.CancelAsync(id);
            if (success)
            {
                _logger.LogInformation("[SVC] Registration.Cancel completed - RegistrationId={RegistrationId}", id);
                return CommandResult.Succeeded();
            }
            else
            {
                return CommandResult.Failed("取消挂号失败，可能存在关联的活跃医案");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] Registration.Cancel failed - RegistrationId={RegistrationId}", id);
            return CommandResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("取消挂号", ex));
        }
    }
}
