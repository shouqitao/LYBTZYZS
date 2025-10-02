using Microsoft.IdentityModel.Tokens;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// JWT密钥管理服务接口
    /// 负责密钥的生成、存储、轮换和获取
    /// </summary>
    public interface ISecurityKeyService
    {
        /// <summary>
        /// 获取当前活跃的签名密钥
        /// </summary>
        Task<SecurityKey> GetCurrentSigningKeyAsync();

        /// <summary>
        /// 获取所有有效的验证密钥（包括当前和历史密钥）
        /// 用于验证不同时期签发的Token
        /// </summary>
        Task<IEnumerable<SecurityKey>> GetValidationKeysAsync();

        /// <summary>
        /// 轮换密钥 - 生成新密钥并将当前密钥加入历史
        /// </summary>
        Task RotateKeyAsync();

        /// <summary>
        /// 获取密钥版本信息
        /// </summary>
        Task<string> GetCurrentKeyVersionAsync();

        /// <summary>
        /// 验证密钥是否即将过期
        /// </summary>
        Task<bool> IsKeyRotationRequiredAsync();
    }
}
