namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 当前用户提供者接口 - 用于 LocalDbContext 审计字段填充
/// OpenSpec: implement-local-mode
/// </summary>
public interface ICurrentUserProvider
{
    /// <summary>
    /// 当前用户ID
    /// </summary>
    Guid? CurrentUserId { get; }
}
