using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// AutoLoginToken 服务 - 负责 AutoLogin 的生成、验证、轮换
    /// </summary>
    public interface IAutoLoginService
    {
        /// <summary>
        /// 使用 AutoLoginToken 自动登录
        /// </summary>
        Task<Result<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 生成 AutoLoginToken 并存储到数据库
        /// </summary>
        /// <returns>生成的 Token 字符串</returns>
        string GenerateAutoLoginToken(
            Guid userId,
            string userName,
            string? deviceId,
            string? deviceName,
            string? clientIp,
            string? userAgent,
            string? familyId = null);

        /// <summary>
        /// 撤销 AutoLoginToken Family (重放攻击检测)
        /// </summary>
        Task RevokeAutoLoginTokenFamilyAsync(string familyId, string reason);
    }
}
