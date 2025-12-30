using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.CommandHandlers;

/// <summary>
/// 方剂CommandHandler接口
/// OpenSpec: unify-desktop-architecture (Phase 2.4)
/// 封装IFormulaRepository，提供统一的CRUD操作和错误处理
/// </summary>
public interface IFormulaCommandHandler : ICommandHandlerBase<FormulaListDto, FormulaDetailDto, FormulaInputDto>
{
    /// <summary>
    /// 按名称搜索方剂
    /// </summary>
    /// <param name="name">方剂名称关键字</param>
    /// <returns>匹配的方剂列表</returns>
    Task<CommandResult<List<FormulaListDto>>> SearchByNameAsync(string name);

    /// <summary>
    /// 按拼音搜索方剂
    /// </summary>
    /// <param name="pinyin">拼音关键字</param>
    /// <returns>匹配的方剂列表</returns>
    Task<CommandResult<List<FormulaListDto>>> SearchByPinyinAsync(string pinyin);

    /// <summary>
    /// 获取方剂包含的药材列表
    /// </summary>
    /// <param name="id">方剂ID</param>
    /// <returns>药材项列表</returns>
    Task<CommandResult<List<FormulaHerbItemDto>>> GetHerbItemsAsync(Guid id);

    /// <summary>
    /// 复制方剂（用于基于现有方剂创建新方剂）
    /// </summary>
    /// <param name="id">源方剂ID</param>
    /// <param name="newName">新方剂名称</param>
    /// <returns>新创建的方剂</returns>
    Task<CommandResult<FormulaDetailDto>> CopyAsync(Guid id, string newName);
}
