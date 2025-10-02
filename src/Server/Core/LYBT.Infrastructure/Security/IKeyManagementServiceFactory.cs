namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 密钥管理服务工厂接口
    /// 用于创建密钥管理服务实例，避免Service Locator反模式
    /// </summary>
    public interface IKeyManagementServiceFactory
    {
        /// <summary>
        /// 创建密钥管理服务实例
        /// </summary>
        /// <returns>密钥管理服务实例</returns>
        IKeyManagementService CreateKeyManagementService();
    }
}
