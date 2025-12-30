using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.CommandHandlers;

/// <summary>
/// 药材CommandHandler接口
/// OpenSpec: unify-desktop-architecture (Phase 2.3)
/// 封装IHerbRepository，提供统一的CRUD操作和错误处理
/// </summary>
public interface IHerbCommandHandler : ICommandHandlerBase<HerbListDto, HerbDetailDto, HerbInputDto>
{
    /// <summary>
    /// 按名称搜索药材
    /// </summary>
    /// <param name="name">药材名称关键字</param>
    /// <returns>匹配的药材列表</returns>
    Task<CommandResult<List<HerbListDto>>> SearchByNameAsync(string name);

    /// <summary>
    /// 按拼音搜索药材
    /// </summary>
    /// <param name="pinyin">拼音关键字</param>
    /// <returns>匹配的药材列表</returns>
    Task<CommandResult<List<HerbListDto>>> SearchByPinyinAsync(string pinyin);

    /// <summary>
    /// 检查药材是否被处方引用
    /// </summary>
    /// <param name="id">药材ID</param>
    /// <returns>引用检查结果</returns>
    Task<CommandResult<HerbReferenceCheckDto>> CheckReferenceAsync(Guid id);

    /// <summary>
    /// 批量检查药材引用
    /// </summary>
    /// <param name="ids">药材ID列表</param>
    /// <returns>引用检查结果列表</returns>
    Task<CommandResult<List<HerbReferenceCheckDto>>> BatchCheckReferenceAsync(IEnumerable<Guid> ids);

    /// <summary>
    /// 切换药材状态（启用/禁用）
    /// </summary>
    /// <param name="id">药材ID</param>
    /// <returns>更新后的药材详情</returns>
    Task<CommandResult<HerbDetailDto>> ToggleStatusAsync(Guid id);

    /// <summary>
    /// 恢复已删除的药材
    /// </summary>
    /// <param name="id">药材ID</param>
    /// <returns>恢复后的药材详情</returns>
    Task<CommandResult<HerbDetailDto>> RestoreAsync(Guid id);

    /// <summary>
    /// 导出药材数据到Excel
    /// </summary>
    /// <param name="keyword">搜索关键词（可选）</param>
    /// <returns>Excel文件字节数组</returns>
    Task<CommandResult<byte[]>> ExportAsync(string? keyword = null);
}
