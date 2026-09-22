using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// 初始管理员账号服务（B-07 初始化向导 Step 4）。
/// </summary>
/// <remarks>
/// <para>DP10 门面：VM 不得注入 <c>IApiClient*</c> 子接口，本服务封装 <see cref="IApiClientIdentity"/>
/// 的用户查询/创建端点。</para>
/// <para>走当前连接模式的 API（本地模式 → 嵌入式 LocalWebAPI；远程模式 → 远程 WebAPI），
/// 因此要求调用方已完成模式切换（向导在 Step 2 结束时切换）。</para>
/// <para>所有异常均转为 <see cref="CommandResult{T}.Failed(string)"/>，向导据 <c>Error</c> 展示原因。</para>
/// </remarks>
public class InitialAdminService : IInitialAdminService
{
    /// <summary>存在性探测的分页窗口（向导 Step 4 为用户名唯一场景，首页足够）</summary>
    private const int ExistsProbePageSize = 20;

    private readonly IApiClientIdentity _identity;
    private readonly ILogger<InitialAdminService> _logger;

    /// <summary>构造服务</summary>
    /// <param name="identity">认证与用户管理 API 子接口</param>
    /// <param name="logger">日志</param>
    public InitialAdminService(IApiClientIdentity identity, ILogger<InitialAdminService> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CommandResult<bool>> ExistsAsync(string userName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return CommandResult<bool>.Succeeded(false);

        try
        {
            var response = await _identity.GetUsersAsync(1, ExistsProbePageSize, userName, ct);
            if (response is null || !response.Success)
            {
                var error = response?.Message ?? "查询用户失败";
                _logger.LogWarning("[INIT-ADMIN] 用户名存在性探测失败：{Error}", error);
                return CommandResult<bool>.Failed(error);
            }

            // 关键词检索可能返回模糊匹配——只有用户名完全一致（忽略大小写）才算已存在
            var exists = response.Data?.Items?.Any(u =>
                string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase)) == true;

            return CommandResult<bool>.Succeeded(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[INIT-ADMIN] 用户名存在性探测异常：{UserName}", userName);
            return CommandResult<bool>.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<bool>> CreateAdminAsync(
        string userName,
        string realName,
        string password,
        CancellationToken ct = default)
    {
        try
        {
            var request = new UserInputDto
            {
                UserName = userName,
                RealName = realName,
                Password = password,
                ConfirmPassword = password,
                Role = UserRole.Admin
            };

            var response = await _identity.CreateUserAsync(request, ct);
            if (response is null)
            {
                _logger.LogWarning("[INIT-ADMIN] 创建管理员账号无响应：{UserName}", userName);
                return CommandResult<bool>.Failed("创建管理员账号失败：服务无响应");
            }

            if (!response.Success)
            {
                _logger.LogWarning("[INIT-ADMIN] 创建管理员账号失败：{UserName} - {Error}", userName, response.Message);
                return CommandResult<bool>.Failed(response.Message);
            }

            _logger.LogInformation("[INIT-ADMIN] 管理员账号已创建：{UserName}", userName);
            return CommandResult<bool>.Succeeded(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[INIT-ADMIN] 创建管理员账号异常：{UserName}", userName);
            return CommandResult<bool>.Failed(ex.Message);
        }
    }
}
