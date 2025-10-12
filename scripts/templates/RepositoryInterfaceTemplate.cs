using LYBT.Shared.Dtos.{{Entity}};

namespace LYBT.Desktop.{{ModuleName}}.Interfaces;

/// <summary>
/// {{EntityChinese}}数据访问接口
/// </summary>
public interface I{{Entity}}Repository
{
    /// <summary>
    /// 获取所有{{EntityChinese}}
    /// </summary>
    Task<List<{{Entity}}Dto>> GetAllAsync();

    /// <summary>
    /// 根据ID获取{{EntityChinese}}
    /// </summary>
    Task<{{Entity}}Dto?> GetByIdAsync(int id);

    /// <summary>
    /// 添加{{EntityChinese}}
    /// </summary>
    Task<{{Entity}}Dto> AddAsync(Create{{Entity}}Dto dto);

    /// <summary>
    /// 更新{{EntityChinese}}
    /// </summary>
    Task UpdateAsync(int id, Update{{Entity}}Dto dto);

    /// <summary>
    /// 删除{{EntityChinese}}
    /// </summary>
    Task DeleteAsync(int id);
}
