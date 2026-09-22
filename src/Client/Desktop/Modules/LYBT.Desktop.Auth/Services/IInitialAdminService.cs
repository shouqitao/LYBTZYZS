using LYBT.Desktop.Contracts.Results;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// 初始管理员账号服务（B-07 初始化向导 Step 4）。
/// </summary>
/// <remarks>
/// DP10 门面：VM 不得注入 <c>IApiClient*</c> 子接口，本服务封装
/// <c>IApiClientIdentity</c> 的用户查询/创建端点，供初始化向导消费。
/// 走当前连接模式的 API（本地模式 → 嵌入式 LocalWebAPI；远程模式 → 远程 WebAPI），
/// 因此需要调用方已完成模式切换（向导在 Step 2 结束时切换）。
/// </remarks>
public interface IInitialAdminService
{
    /// <summary>用户名是否已存在</summary>
    Task<CommandResult<bool>> ExistsAsync(string userName, CancellationToken ct = default);

    /// <summary>创建管理员账号（角色 Admin）</summary>
    Task<CommandResult<bool>> CreateAdminAsync(
        string userName,
        string realName,
        string password,
        CancellationToken ct = default);
}
