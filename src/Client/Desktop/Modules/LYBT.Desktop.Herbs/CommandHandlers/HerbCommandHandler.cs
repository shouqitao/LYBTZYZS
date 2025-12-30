using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.CommandHandlers;

/// <summary>
/// 药材CommandHandler实现
/// OpenSpec: unify-desktop-architecture (Phase 2.6)
/// 封装IHerbRepository，提供统一的CRUD操作和错误处理
/// </summary>
public class HerbCommandHandler : IHerbCommandHandler
{
    private readonly IHerbRepository _repository;
    private readonly ILogger<HerbCommandHandler> _logger;

    public HerbCommandHandler(
        IHerbRepository repository,
        ILogger<HerbCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<HerbListDto>>> GetListAsync(QueryParams? query = null)
    {
        try
        {
            // 从Filters中提取category参数
            string? category = null;
            if (query?.Filters?.TryGetValue("category", out var categoryValue) == true)
            {
                category = categoryValue?.ToString();
            }

            var result = await _repository.GetPagedAsync(
                query?.Page ?? 1,
                query?.PageSize ?? 20,
                query?.SearchText,
                category);
            return CommandResult<List<HerbListDto>>.Succeeded(result.Items.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取药材列表失败");
            return CommandResult<List<HerbListDto>>.Failed($"获取药材列表失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<HerbDetailDto>> GetDetailAsync(Guid id)
    {
        try
        {
            var result = await _repository.GetByIdAsync(id);
            if (result == null)
            {
                return CommandResult<HerbDetailDto>.NotFound($"未找到ID为 {id} 的药材");
            }
            return CommandResult<HerbDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取药材详情失败: {HerbId}", id);
            return CommandResult<HerbDetailDto>.Failed($"获取药材详情失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<HerbDetailDto>> SaveAsync(HerbInputDto input)
    {
        try
        {
            HerbDetailDto result;
            if (input.Id == Guid.Empty)
            {
                result = await _repository.CreateAsync(input);
                _logger.LogInformation("创建药材成功: {HerbId}", result.Id);
            }
            else
            {
                result = await _repository.UpdateAsync(input);
                _logger.LogInformation("更新药材成功: {HerbId}", result.Id);
            }
            return CommandResult<HerbDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            var operation = input.Id == Guid.Empty ? "创建" : "更新";
            _logger.LogError(ex, "{Operation}药材失败", operation);
            return CommandResult<HerbDetailDto>.Failed($"{operation}药材失败: {ex.Message}");
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
                _logger.LogInformation("删除药材成功: {HerbId}", id);
                return CommandResult<bool>.Succeeded(true);
            }
            return CommandResult<bool>.Failed("删除药材失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除药材失败: {HerbId}", id);
            return CommandResult<bool>.Failed($"删除药材失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<HerbListDto>>> SearchByNameAsync(string name)
    {
        try
        {
            var result = await _repository.SearchAsync(name);
            return CommandResult<List<HerbListDto>>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按名称搜索药材失败: {Name}", name);
            return CommandResult<List<HerbListDto>>.Failed($"搜索药材失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<HerbListDto>>> SearchByPinyinAsync(string pinyin)
    {
        try
        {
            // 拼音搜索复用通用搜索接口
            var result = await _repository.SearchAsync(pinyin);
            return CommandResult<List<HerbListDto>>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按拼音搜索药材失败: {Pinyin}", pinyin);
            return CommandResult<List<HerbListDto>>.Failed($"搜索药材失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<HerbReferenceCheckDto>> CheckReferenceAsync(Guid id)
    {
        try
        {
            // 检查药材是否被处方引用
            // 注：实际实现可能需要调用专门的引用检查API
            var herb = await _repository.GetByIdAsync(id);
            if (herb == null)
            {
                return CommandResult<HerbReferenceCheckDto>.NotFound($"未找到ID为 {id} 的药材");
            }

            // 返回默认检查结果（实际应调用后端API）
            var checkResult = new HerbReferenceCheckDto
            {
                HerbId = id,
                HerbName = herb.Name,
                HasReferences = false,
                ReferenceCount = 0
            };
            return CommandResult<HerbReferenceCheckDto>.Succeeded(checkResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查药材引用失败: {HerbId}", id);
            return CommandResult<HerbReferenceCheckDto>.Failed($"检查引用失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<HerbReferenceCheckDto>>> BatchCheckReferenceAsync(IEnumerable<Guid> ids)
    {
        try
        {
            var results = new List<HerbReferenceCheckDto>();
            foreach (var id in ids)
            {
                var checkResult = await CheckReferenceAsync(id);
                if (checkResult.Success && checkResult.Data != null)
                {
                    results.Add(checkResult.Data);
                }
            }
            return CommandResult<List<HerbReferenceCheckDto>>.Succeeded(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量检查药材引用失败");
            return CommandResult<List<HerbReferenceCheckDto>>.Failed($"批量检查引用失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<HerbDetailDto>> ToggleStatusAsync(Guid id)
    {
        try
        {
            var result = await _repository.ToggleStatusAsync(id);
            if (result == null)
            {
                return CommandResult<HerbDetailDto>.Failed("切换药材状态失败");
            }
            _logger.LogInformation("切换药材状态成功: {HerbId} -> {Status}", id, result.Status);
            return CommandResult<HerbDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换药材状态失败: {HerbId}", id);
            return CommandResult<HerbDetailDto>.Failed($"切换状态失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<HerbDetailDto>> RestoreAsync(Guid id)
    {
        try
        {
            var result = await _repository.RestoreAsync(id);
            if (result == null)
            {
                return CommandResult<HerbDetailDto>.Failed("恢复药材失败");
            }
            _logger.LogInformation("恢复药材成功: {HerbId}", id);
            return CommandResult<HerbDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复药材失败: {HerbId}", id);
            return CommandResult<HerbDetailDto>.Failed($"恢复药材失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<byte[]>> ExportAsync(string? keyword = null)
    {
        try
        {
            var result = await _repository.ExportHerbsAsync(keyword);
            if (result == null || result.Length == 0)
            {
                return CommandResult<byte[]>.Failed("导出药材数据失败");
            }
            _logger.LogInformation("导出药材成功，关键词: {Keyword}", keyword);
            return CommandResult<byte[]>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出药材失败");
            return CommandResult<byte[]>.Failed($"导出药材失败: {ex.Message}");
        }
    }
}
