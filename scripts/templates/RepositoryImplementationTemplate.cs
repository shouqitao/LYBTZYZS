using LYBT.Desktop.{{ModuleName}}.Interfaces;
using LYBT.Shared.ApiInterfaces;
using LYBT.Shared.Dtos.{{Entity}};
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.{{ModuleName}}.Repositories;

/// <summary>
/// {{EntityChinese}}数据访问实现
/// </summary>
public class {{Entity}}Repository : I{{Entity}}Repository
{
    private readonly I{{Entity}}Api _api;
    private readonly ILogger<{{Entity}}Repository> _logger;

    public {{Entity}}Repository(
        I{{Entity}}Api api,
        ILogger<{{Entity}}Repository> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<List<{{Entity}}Dto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("正在获取所有{{EntityChinese}}...");
            return await _api.GetAll{{Entity}}sAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有{{EntityChinese}}失败");
            throw; // 异常向上抛出，由 ViewModel 处理
        }
    }

    public async Task<{{Entity}}Dto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("正在获取{{EntityChinese}} {Id}...", id);
            return await _api.Get{{Entity}}ByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取{{EntityChinese}} {Id} 失败", id);
            throw;
        }
    }

    public async Task<{{Entity}}Dto> AddAsync(Create{{Entity}}Dto dto)
    {
        try
        {
            _logger.LogInformation("正在添加{{EntityChinese}}...");
            return await _api.Create{{Entity}}Async(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加{{EntityChinese}}失败");
            throw;
        }
    }

    public async Task UpdateAsync(int id, Update{{Entity}}Dto dto)
    {
        try
        {
            _logger.LogInformation("正在更新{{EntityChinese}} {Id}...", id);
            await _api.Update{{Entity}}Async(id, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新{{EntityChinese}} {Id} 失败", id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogInformation("正在删除{{EntityChinese}} {Id}...", id);
            await _api.Delete{{Entity}}Async(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除{{EntityChinese}} {Id} 失败", id);
            throw;
        }
    }
}
