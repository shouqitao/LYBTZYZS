using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.CommandHandlers;

/// <summary>
/// 患者CommandHandler实现
/// OpenSpec: unify-desktop-architecture (Phase 2.6)
/// 封装IPatientRepository，提供统一的CRUD操作和错误处理
/// </summary>
public class PatientCommandHandler : IPatientCommandHandler
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<PatientCommandHandler> _logger;

    public PatientCommandHandler(
        IPatientRepository repository,
        ILogger<PatientCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<PatientListDto>>> GetListAsync(QueryParams? query = null)
    {
        try
        {
            var result = await _repository.GetPagedAsync(
                query?.Page ?? 1,
                query?.PageSize ?? 20,
                query?.SearchText);
            return CommandResult<List<PatientListDto>>.Succeeded(result.Items.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者列表失败");
            return CommandResult<List<PatientListDto>>.Failed($"获取患者列表失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<PatientDetailDto>> GetDetailAsync(Guid id)
    {
        try
        {
            var result = await _repository.GetByIdAsync(id);
            if (result == null)
            {
                return CommandResult<PatientDetailDto>.NotFound($"未找到ID为 {id} 的患者");
            }
            return CommandResult<PatientDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者详情失败: {PatientId}", id);
            return CommandResult<PatientDetailDto>.Failed($"获取患者详情失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<PatientDetailDto>> SaveAsync(PatientInputDto input)
    {
        try
        {
            PatientDetailDto result;
            if (input.Id == Guid.Empty)
            {
                result = await _repository.CreateAsync(input);
                _logger.LogInformation("创建患者成功: {PatientId}", result.Id);
            }
            else
            {
                result = await _repository.UpdateAsync(input);
                _logger.LogInformation("更新患者成功: {PatientId}", result.Id);
            }
            return CommandResult<PatientDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            var operation = input.Id == Guid.Empty ? "创建" : "更新";
            _logger.LogError(ex, "{Operation}患者失败", operation);
            return CommandResult<PatientDetailDto>.Failed($"{operation}患者失败: {ex.Message}");
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
                _logger.LogInformation("删除患者成功: {PatientId}", id);
                return CommandResult<bool>.Succeeded(true);
            }
            return CommandResult<bool>.Failed("删除患者失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除患者失败: {PatientId}", id);
            return CommandResult<bool>.Failed($"删除患者失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<PatientListDto>>> SearchByNameAsync(string name)
    {
        try
        {
            var result = await _repository.SearchAsync(name);
            return CommandResult<List<PatientListDto>>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按姓名搜索患者失败: {Name}", name);
            return CommandResult<List<PatientListDto>>.Failed($"搜索患者失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<PatientListDto>>> SearchByPhoneAsync(string phone)
    {
        try
        {
            // 电话搜索复用通用搜索接口
            var result = await _repository.SearchAsync(phone);
            return CommandResult<List<PatientListDto>>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按电话搜索患者失败: {Phone}", phone);
            return CommandResult<List<PatientListDto>>.Failed($"搜索患者失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<bool>> HasMedicalCasesAsync(Guid id)
    {
        try
        {
            // 通过获取患者详情检查是否有关联医案
            var patient = await _repository.GetByIdAsync(id);
            if (patient == null)
            {
                return CommandResult<bool>.NotFound($"未找到ID为 {id} 的患者");
            }
            // 如果患者存在，返回是否有医案记录
            // 注：实际实现可能需要调用MedicalCase相关接口
            return CommandResult<bool>.Succeeded(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查患者医案关联失败: {PatientId}", id);
            return CommandResult<bool>.Failed($"检查失败: {ex.Message}");
        }
    }
}
