namespace LYBT.SharedKernel.Contracts;

/// <summary>
/// 跨模块认证服务接口。供其他模块验证token和获取当前用户信息。
/// 实现位于LYBT.Module.Auth。
/// </summary>
public interface ICrossModuleAuthService
{
    /// <summary>
    /// 验证JWT token有效性。
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>有效返回true</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// 从token中提取用户ID。
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>用户ID，无效token返回null</returns>
    Guid? GetUserIdFromToken(string token);
}


