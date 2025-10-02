namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 密钥管理服务接口
    /// </summary>
    /// <summary>
    /// 密钥管理服务接口
    /// </summary>
    public interface IKeyManagementService
    {
        /// <summary>
        /// 检查是否需要旋转密钥
        /// </summary>
        /// <returns>是否需要旋转</returns>
        Task<bool> ShouldRotateKeyAsync();

        /// <summary>
        /// 旋转JWT密钥并返回新密钥
        /// </summary>
        /// <returns>新的JWT密钥</returns>
        Task<string> RotateJwtSecretAsync();

        /// <summary>
        /// 记录密钥旋转
        /// </summary>
        /// <param name="newSecret">新密钥</param>
        /// <param name="rotationTime">旋转时间</param>
        Task RecordRotationAsync(string newSecret, DateTime rotationTime);
    }
}
