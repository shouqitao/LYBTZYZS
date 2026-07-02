namespace LYBT.Module.Users.Domain.Services;

/// <summary>
/// 用户编号生成器接口。用于生成用户工号等唯一标识。
/// </summary>
public interface IUserNumberGenerator
{
    /// <summary>
    /// 生成下一个用户工号。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户工号</returns>
    Task<string> GenerateNextAsync(CancellationToken cancellationToken = default);
}


