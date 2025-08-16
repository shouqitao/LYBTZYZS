using LYBT.Shared.Models.Contracts.Common;
using System;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 配置管理器接口
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// 获取配置值
        /// </summary>
        T GetValue<T>(string key);
        
        /// <summary>
        /// 获取配置值（带默认值）
        /// </summary>
        T GetValue<T>(string key, T defaultValue);
        
        /// <summary>
        /// 设置配置值
        /// </summary>
        void SetValue<T>(string key, T value);
        
        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        bool Contains(string key);
        
        /// <summary>
        /// 获取连接字符串
        /// </summary>
        string GetConnectionString(string name);
        
        /// <summary>
        /// 重新加载配置
        /// </summary>
        void Reload();
        
        /// <summary>
        /// 保存配置
        /// </summary>
        void Save();
    }
}