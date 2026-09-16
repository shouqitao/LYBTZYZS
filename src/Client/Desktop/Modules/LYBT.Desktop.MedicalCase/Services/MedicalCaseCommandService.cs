using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案命令服务 - 写操作 + 变更检测
/// 从 MedicalCaseService 拆分，实现 IMedicalCaseCommandService
/// 基于 Model 编辑会话（MedicalCaseEditContext）工作：HasChanges 委托会话 IsDirty，
/// SaveAsync 从会话 Commit 后的 CurrentModel 经 Mapper 生成 InputDto 提交仓库。
/// </summary>
internal class MedicalCaseCommandService : IMedicalCaseCommandService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly ISessionManager? _sessionManager;
    private readonly ILogger<MedicalCaseCommandService> _logger;
    private readonly MedicalCaseEditContext _context;
    private readonly MedicalCaseDetailModelMapper _mapper;

    public MedicalCaseCommandService(
        IMedicalCaseRepository repository,
        MedicalCaseEditSession session,
        ILogger<MedicalCaseCommandService> logger,
        MedicalCaseDetailModelMapper mapper,
        ISessionManager? sessionManager = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        ArgumentNullException.ThrowIfNull(session);
        _context = session.Context;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _sessionManager = sessionManager;
    }

    /// <summary>
    /// 当前医案数据（DTO 门面）。
    /// 会话基于 Model 编辑，DTO 门面由聚合代理 MedicalCaseService 持有（Current/Cached*），
    /// 命令服务不再维护 DTO 快照，返回 null。
    /// </summary>
    public MedicalCaseDetailDto? Current => null;

    public bool HasChanges => _context.IsDirty;

    public virtual async Task<CommandResult<bool>> SaveAsync(CancellationToken ct = default)
    {
        var model = _context.CurrentModel;
        if (model == null)
        {
            _logger.LogWarning("[CMD] MedicalCase.Save → NoSession");
            return CommandResult<bool>.Failed("无编辑会话");
        }
        if (!HasChanges)
        {
            _logger.LogDebug("[CMD] MedicalCase.Save → NoChanges - MedicalCaseId={MedicalCaseId}", model.Id);
            return CommandResult<bool>.Succeeded(true);
        }

        try
        {
            _logger.LogInformation("[CMD] MedicalCase.Save started - MedicalCaseId={MedicalCaseId}", model.Id);
            _context.Commit();
            var inputDto = _mapper.ToInputDto(model);
            var updated = await _repository.SaveAsync(model.Id, inputDto);
            if (updated != null)
            {
                _context.BeginEdit(_mapper.ToItem(updated));
            }
            _logger.LogInformation("[CMD] MedicalCase.Save completed - MedicalCaseId={MedicalCaseId}", model.Id);
            return CommandResult<bool>.Succeeded(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CMD] MedicalCase.Save failed - MedicalCaseId={MedicalCaseId}", model.Id);
            return CommandResult<bool>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
        }
    }

    public virtual async Task<CommandResult<bool>> DeleteAsync(CancellationToken ct = default)
    {
        var model = _context.CurrentModel;
        if (model == null)
        {
            _logger.LogWarning("[CMD] MedicalCase.Delete → NoSession");
            return CommandResult<bool>.Failed("无编辑会话");
        }
        try
        {
            _logger.LogInformation("[CMD] MedicalCase.Delete started - MedicalCaseId={MedicalCaseId}", model.Id);
            await _repository.DeleteAsync(model.Id);
            _logger.LogInformation("[CMD] MedicalCase.Delete completed - MedicalCaseId={MedicalCaseId}", model.Id);
            _context.Clear();
            return CommandResult<bool>.Succeeded(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CMD] MedicalCase.Delete failed - MedicalCaseId={MedicalCaseId}", model.Id);
            return CommandResult<bool>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除", ex));
        }
    }

    public virtual async Task<CommandResult<Guid>> CreateMedicalCaseAsync(Guid patientId, Guid? registrationId = null, CancellationToken ct = default)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("患者ID不能为空", nameof(patientId));

        try
        {
            _logger.LogInformation("[CMD] MedicalCase.CreateNew started - PatientId={PatientId} RegistrationId={RegistrationId}", patientId, registrationId);

            if (_sessionManager == null)
            {
                _logger.LogWarning("[CMD] MedicalCase.CreateNew → NullSessionManager");
                return CommandResult<Guid>.Failed("会话管理器未初始化，无法创建医案");
            }
            if (_sessionManager.CurrentUser == null)
            {
                _logger.LogWarning("[CMD] MedicalCase.CreateNew → NullCurrentUser");
                return CommandResult<Guid>.Failed("用户信息丢失，无法创建医案");
            }

            var createDto = new MedicalCaseInputDto
            {
                Id = null,
                PatientId = patientId,
                UserId = _sessionManager.CurrentUser.Id,
                RegistrationId = registrationId
            };
            var createdDto = await _repository.CreateAsync(createDto);
            if (createdDto == null)
            {
                _logger.LogWarning("[CMD] MedicalCase.CreateNew → NullResult");
                return CommandResult<Guid>.Failed("创建医案失败：服务返回空结果");
            }

            _logger.LogInformation("[CMD] MedicalCase.CreateNew completed - MedicalCaseId={MedicalCaseId}", createdDto.Id);
            return CommandResult<Guid>.Succeeded(createdDto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CMD] MedicalCase.CreateNew failed - PatientId={PatientId}", patientId);
            return CommandResult<Guid>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建医案", ex));
        }
    }
}
