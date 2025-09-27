using Microsoft.IdentityModel.Tokens;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 密钥管理服务接口 - 支持密钥轮换和多密钥验证
    /// </summary>
    public interface ISecurityKeyService
    {
        /// <summary>
        /// 获取当前用于签名的密钥
        /// </summary>
        Task<SecurityKey> GetCurrentKeyAsync();

        /// <summary>
        /// 获取所有有效密钥（用于验证）
        /// </summary>
        Task<IEnumerable<SecurityKey>> GetAllKeysAsync();

        /// <summary>
        /// 执行密钥轮换
        /// </summary>
        Task RotateKeyAsync();

        /// <summary>
        /// 获取当前密钥版本标识
        /// </summary>
        Task<string> GetCurrentKeyIdAsync();

        /// <summary>
        /// 根据密钥ID获取特定密钥
        /// </summary>
        Task<SecurityKey?> GetKeyByIdAsync(string keyId);
    }
}