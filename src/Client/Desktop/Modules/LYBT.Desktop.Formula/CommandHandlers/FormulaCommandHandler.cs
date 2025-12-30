using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.CommandHandlers;

/// <summary>
/// 方剂CommandHandler实现
/// OpenSpec: unify-desktop-architecture (Phase 2.6)
/// 封装IFormulaRepository，提供统一的CRUD操作和错误处理
/// </summary>
public class FormulaCommandHandler : IFormulaCommandHandler
{
    private readonly IFormulaRepository _repository;
    private readonly ILogger<FormulaCommandHandler> _logger;

    public FormulaCommandHandler(
        IFormulaRepository repository,
        ILogger<FormulaCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<FormulaListDto>>> GetListAsync(QueryParams? query = null)
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
            return CommandResult<List<FormulaListDto>>.Succeeded(result.Items.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取方剂列表失败");
            return CommandResult<List<FormulaListDto>>.Failed($"获取方剂列表失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<FormulaDetailDto>> GetDetailAsync(Guid id)
    {
        try
        {
            var result = await _repository.GetByIdAsync(id);
            if (result == null)
            {
                return CommandResult<FormulaDetailDto>.NotFound($"未找到ID为 {id} 的方剂");
            }
            return CommandResult<FormulaDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取方剂详情失败: {FormulaId}", id);
            return CommandResult<FormulaDetailDto>.Failed($"获取方剂详情失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<FormulaDetailDto>> SaveAsync(FormulaInputDto input)
    {
        try
        {
            FormulaDetailDto result;
            if (input.Id == Guid.Empty)
            {
                result = await _repository.CreateAsync(input);
                _logger.LogInformation("创建方剂成功: {FormulaId}", result.Id);
            }
            else
            {
                result = await _repository.UpdateAsync(input);
                _logger.LogInformation("更新方剂成功: {FormulaId}", result.Id);
            }
            return CommandResult<FormulaDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            var operation = input.Id == Guid.Empty ? "创建" : "更新";
            _logger.LogError(ex, "{Operation}方剂失败", operation);
            return CommandResult<FormulaDetailDto>.Failed($"{operation}方剂失败: {ex.Message}");
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
                _logger.LogInformation("删除方剂成功: {FormulaId}", id);
                return CommandResult<bool>.Succeeded(true);
            }
            return CommandResult<bool>.Failed("删除方剂失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除方剂失败: {FormulaId}", id);
            return CommandResult<bool>.Failed($"删除方剂失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<FormulaListDto>>> SearchByNameAsync(string name)
    {
        try
        {
            var result = await _repository.SearchAsync(name);
            return CommandResult<List<FormulaListDto>>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按名称搜索方剂失败: {Name}", name);
            return CommandResult<List<FormulaListDto>>.Failed($"搜索方剂失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<FormulaListDto>>> SearchByPinyinAsync(string pinyin)
    {
        try
        {
            // 拼音搜索复用通用搜索接口
            var result = await _repository.SearchAsync(pinyin);
            return CommandResult<List<FormulaListDto>>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按拼音搜索方剂失败: {Pinyin}", pinyin);
            return CommandResult<List<FormulaListDto>>.Failed($"搜索方剂失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<List<FormulaHerbItemDto>>> GetHerbItemsAsync(Guid id)
    {
        try
        {
            var formula = await _repository.GetByIdAsync(id);
            if (formula == null)
            {
                return CommandResult<List<FormulaHerbItemDto>>.NotFound($"未找到ID为 {id} 的方剂");
            }

            // FormulaDetailDto.Herbs 已经是 List<FormulaHerbItemDto> 类型
            return CommandResult<List<FormulaHerbItemDto>>.Succeeded(formula.Herbs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取方剂药材列表失败: {FormulaId}", id);
            return CommandResult<List<FormulaHerbItemDto>>.Failed($"获取药材列表失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<FormulaDetailDto>> CopyAsync(Guid id, string newName)
    {
        try
        {
            // 使用仓储的克隆方法
            var result = await _repository.CloneFormulaAsync(id);
            _logger.LogInformation("复制方剂成功: 源ID={SourceId}, 新ID={NewId}", id, result.Id);
            return CommandResult<FormulaDetailDto>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制方剂失败: {FormulaId}", id);
            return CommandResult<FormulaDetailDto>.Failed($"复制方剂失败: {ex.Message}");
        }
    }
}
