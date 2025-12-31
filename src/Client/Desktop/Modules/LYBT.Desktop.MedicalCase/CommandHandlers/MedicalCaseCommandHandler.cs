using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.CommandHandlers;

/// <summary>
/// 医案CommandHandler实现
/// OpenSpec: unify-desktop-architecture
/// 实现ICommandHandlerBase标准接口，提供统一的CRUD操作
/// 注：UI层聚合操作使用MedicalCaseWorkspaceCoordinator
/// </summary>
public class MedicalCaseCommandHandler : IMedicalCaseCommandHandler
{
    private readonly IMedicalCaseRepository _repository;
    private readonly ILogger<MedicalCaseCommandHandler> _logger;

    public MedicalCaseCommandHandler(
        IMedicalCaseRepository repository,
        ILogger<MedicalCaseCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<MedicalCaseListDto>>> GetListAsync(QueryParams? query = null)
    {
        try
        {
            var result = await _repository.GetPagedAsync(
                query?.Page ?? 1,
                query?.PageSize ?? 20,
                query?.SearchText);
            return CommandResult<List<MedicalCaseListDto>>.Succeeded(result.Items.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取医案列表失败");
            return CommandResult<List<MedicalCaseListDto>>.Failed($"获取医案列表失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<MedicalCaseDetailDto>> GetDetailAsync(Guid id)
    {
        try
        {
            var result = await _repository.GetByIdAsync(id);
            if (result == null)
            {
                return CommandResult<MedicalCaseDetailDto>.NotFound($"未找到ID为 {id} 的医案");
            }
            return CommandResult<MedicalCaseDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取医案详情失败: {MedicalCaseId}", id);
            return CommandResult<MedicalCaseDetailDto>.Failed($"获取医案详情失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<MedicalCaseDetailDto>> SaveAsync(MedicalCaseInputDto input)
    {
        try
        {
            MedicalCaseDetailDto result;
            if (input.Id == Guid.Empty)
            {
                result = await _repository.CreateAsync(input);
                _logger.LogInformation("创建医案成功: {MedicalCaseId}", result.Id);
            }
            else
            {
                result = await _repository.UpdateAsync(input);
                _logger.LogInformation("更新医案成功: {MedicalCaseId}", result.Id);
            }
            return CommandResult<MedicalCaseDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            var operation = input.Id == Guid.Empty ? "创建" : "更新";
            _logger.LogError(ex, "{Operation}医案失败", operation);
            return CommandResult<MedicalCaseDetailDto>.Failed($"{operation}医案失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<bool>> DeleteAsync(Guid id)
    {
        try
        {
            var success = await _repository.DeleteAsync(id);
            if (success)
            {
                _logger.LogInformation("删除医案成功: {MedicalCaseId}", id);
                return CommandResult<bool>.Succeeded(true);
            }
            return CommandResult<bool>.Failed("删除医案失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除医案失败: {MedicalCaseId}", id);
            return CommandResult<bool>.Failed($"删除医案失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<MedicalCaseListDto>>> GetByPatientAsync(Guid patientId)
    {
        try
        {
            var details = await _repository.GetByPatientIdAsync(patientId);
            // 将DetailDto转换为ListDto（提取列表展示所需字段）
            var listDtos = details.Select(d => new MedicalCaseListDto
            {
                Id = d.Id,
                PatientId = d.PatientId,
                PatientName = d.PatientName,
                UserId = d.UserId,
                DoctorName = d.DoctorName,
                CaseStatus = d.CaseStatus,
                CreatedAt = d.CreatedAt
            }).ToList();
            return CommandResult<List<MedicalCaseListDto>>.Succeeded(listDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者医案列表失败: {PatientId}", patientId);
            return CommandResult<List<MedicalCaseListDto>>.Failed($"获取患者医案列表失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<MedicalCaseListDto>>> GetByDoctorAsync(Guid userId)
    {
        try
        {
            // 使用分页查询获取医生的医案
            var result = await _repository.GetPagedAsync(1, 100, null);
            // 在客户端过滤（实际应该在API层面支持UserId过滤）
            var doctorCases = result.Items.Where(c => c.UserId == userId).ToList();
            return CommandResult<List<MedicalCaseListDto>>.Succeeded(doctorCases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取医生医案列表失败: {UserId}", userId);
            return CommandResult<List<MedicalCaseListDto>>.Failed($"获取医生医案列表失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<PendingMedicalCaseDto>>> GetPendingAsync()
    {
        try
        {
            // 使用分页查询获取待处理医案
            var result = await _repository.GetPagedAsync(1, 50, null);
            // 转换为PendingMedicalCaseDto（过滤未完成的医案）
            var pendingCases = result.Items
                .Where(c => c.CaseStatus != Shared.Models.Enums.MedicalCaseStatus.Completed)
                .Select(c => new PendingMedicalCaseDto
                {
                    PatientId = c.PatientId,
                    PatientName = c.PatientName ?? string.Empty,
                    MedicalCaseId = c.Id,
                    CreatedAt = c.CreatedAt
                }).ToList();
            return CommandResult<List<PendingMedicalCaseDto>>.Succeeded(pendingCases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取待处理医案列表失败");
            return CommandResult<List<PendingMedicalCaseDto>>.Failed($"获取待处理医案列表失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<MedicalCaseDetailDto>> CompleteAsync(Guid id)
    {
        try
        {
            // OpenSpec: optimize-medicalcase-api - CloseCaseAsync直接返回完整医案详情
            var result = await _repository.CloseCaseAsync(id);
            if (result == null)
            {
                return CommandResult<MedicalCaseDetailDto>.Failed("完成医案失败");
            }
            _logger.LogInformation("完成医案成功: {MedicalCaseId}", id);
            return CommandResult<MedicalCaseDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成医案失败: {MedicalCaseId}", id);
            return CommandResult<MedicalCaseDetailDto>.Failed($"完成医案失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<bool>> CancelAsync(Guid id)
    {
        try
        {
            // 取消医案使用删除操作（软删除）
            var success = await _repository.DeleteAsync(id);
            if (success)
            {
                _logger.LogInformation("取消医案成功: {MedicalCaseId}", id);
                return CommandResult<bool>.Succeeded(true);
            }
            return CommandResult<bool>.Failed("取消医案失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消医案失败: {MedicalCaseId}", id);
            return CommandResult<bool>.Failed($"取消医案失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<MedicalCaseDetailDto>> SaveDraftAsync(MedicalCaseInputDto input)
    {
        try
        {
            // 注：草稿状态由Repository/Service层处理
            MedicalCaseDetailDto result;
            if (input.Id == null || input.Id == Guid.Empty)
            {
                result = await _repository.CreateAsync(input);
                _logger.LogInformation("保存医案草稿成功: {MedicalCaseId}", result.Id);
            }
            else
            {
                result = await _repository.UpdateAsync(input);
                _logger.LogInformation("更新医案草稿成功: {MedicalCaseId}", result.Id);
            }
            return CommandResult<MedicalCaseDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存医案草稿失败");
            return CommandResult<MedicalCaseDetailDto>.Failed($"保存草稿失败: {ex.Message}");
        }
    }
}
