namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 加密服务接口 - UltraThink重构安全架构
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// 加密数据
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <returns>密文</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// 解密数据
        /// </summary>
        /// <param name="cipherText">密文</param>
        /// <returns>明文</returns>
        string Decrypt(string cipherText);

        /// <summary>
        /// 计算哈希值
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>哈希值</returns>
        string Hash(string input);

        /// <summary>
        /// 数字签名
        /// </summary>
        /// <param name="data">待签名数据</param>
        /// <param name="key">签名密钥</param>
        /// <returns>签名</returns>
        string Sign(string data, string key);

        /// <summary>
        /// 验证签名
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="signature">签名</param>
        /// <param name="key">验证密钥</param>
        /// <returns>验证结果</returns>
        bool VerifySignature(string data, string signature, string key);

        /// <summary>
        /// 生成安全密钥
        /// </summary>
        /// <returns>Base64编码的密钥</returns>
        string GenerateSecureKey();

        /// <summary>
        /// 加密连接字符串
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>加密后的连接字符串</returns>
        string EncryptConnectionString(string connectionString);

        /// <summary>
        /// 解密连接字符串
        /// </summary>
        /// <param name="encryptedConnectionString">加密的连接字符串</param>
        /// <returns>解密后的连接字符串</returns>
        string DecryptConnectionString(string encryptedConnectionString);
    }
}