using System.Reflection;
using LYBT.Entities.Attributes;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 敏感数据处理辅助工具 - Epic 05-P0-03: 数据安全保障
    /// 提供敏感数据的加密、解密和脱敏功能的便捷方法
    /// </summary>
    public static class SensitiveDataHelper
    {
        /// <summary>
        /// 解密实体中的敏感数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体对象</param>
        /// <param name="encryptionService">加密服务</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>解密后的实体</returns>
        public static T DecryptSensitiveData<T>(T entity, IDataEncryptionService encryptionService, ILogger? logger = null)
            where T : class
        {
            if (entity == null || encryptionService == null)
                return entity;

            try
            {
                var entityType = typeof(T);
                var properties = entityType.GetProperties()
                    .Where(p => p.GetCustomAttribute<SensitiveDataAttribute>() != null)
                    .Where(p => p.PropertyType == typeof(string))
                    .Where(p => p.CanRead && p.CanWrite);

                foreach (var property in properties)
                {
                    var sensitiveAttr = property.GetCustomAttribute<SensitiveDataAttribute>();
                    var encryptedValue = property.GetValue(entity) as string;

                    if (string.IsNullOrEmpty(encryptedValue) || sensitiveAttr == null)
                        continue;

                    try
                    {
                        var decryptedValue = encryptionService.Decrypt(encryptedValue, sensitiveAttr.DataType);
                        property.SetValue(entity, decryptedValue);

                        logger?.LogDebug(
                            "已解密实体 {EntityType} 的敏感属性 {Property}",
                            entityType.Name, property.Name);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "解密实体 {EntityType} 的属性 {Property} 失败，保持原值",
                            entityType.Name, property.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "解密实体 {EntityType} 的敏感数据时发生错误", typeof(T).Name);
            }

            return entity;
        }

        /// <summary>
        /// 批量解密实体列表中的敏感数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体列表</param>
        /// <param name="encryptionService">加密服务</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>解密后的实体列表</returns>
        public static IEnumerable<T> DecryptSensitiveData<T>(IEnumerable<T> entities, IDataEncryptionService encryptionService, ILogger? logger = null)
            where T : class
        {
            if (entities == null || encryptionService == null)
                return entities ?? new List<T>();

            return entities.Select(entity => DecryptSensitiveData(entity, encryptionService, logger)).ToList();
        }

        /// <summary>
        /// 脱敏实体中的敏感数据（用于日志记录和显示）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体对象</param>
        /// <param name="encryptionService">加密服务</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>脱敏后的实体</returns>
        public static T MaskSensitiveData<T>(T entity, IDataEncryptionService encryptionService, ILogger? logger = null)
            where T : class
        {
            if (entity == null || encryptionService == null)
                return entity;

            try
            {
                var entityType = typeof(T);
                var properties = entityType.GetProperties()
                    .Where(p => p.GetCustomAttribute<SensitiveDataAttribute>() != null)
                    .Where(p => p.PropertyType == typeof(string))
                    .Where(p => p.CanRead && p.CanWrite);

                foreach (var property in properties)
                {
                    var sensitiveAttr = property.GetCustomAttribute<SensitiveDataAttribute>();
                    if (sensitiveAttr == null || !sensitiveAttr.RequireLogMasking)
                        continue;

                    var originalValue = property.GetValue(entity) as string;
                    if (string.IsNullOrEmpty(originalValue))
                        continue;

                    try
                    {
                        // 先尝试解密（如果是加密数据）
                        string valueToMask = originalValue;
                        if (IsEncrypted(originalValue))
                        {
                            valueToMask = encryptionService.Decrypt(originalValue, sensitiveAttr.DataType);
                        }

                        var maskedValue = encryptionService.MaskData(valueToMask, sensitiveAttr.MaskingMode);
                        property.SetValue(entity, maskedValue);

                        logger?.LogDebug(
                            "已脱敏实体 {EntityType} 的敏感属性 {Property}",
                            entityType.Name, property.Name);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "脱敏实体 {EntityType} 的属性 {Property} 失败",
                            entityType.Name, property.Name);

                        // 如果脱敏失败，至少显示简单的掩码
                        property.SetValue(entity, "***");
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "脱敏实体 {EntityType} 的敏感数据时发生错误", typeof(T).Name);
            }

            return entity;
        }

        /// <summary>
        /// 检查实体是否包含敏感数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>是否包含敏感数据</returns>
        public static bool HasSensitiveData<T>() where T : class
        {
            var entityType = typeof(T);
            return entityType.GetProperties()
                .Any(p => p.GetCustomAttribute<SensitiveDataAttribute>() != null);
        }

        /// <summary>
        /// 获取实体中所有敏感数据属性
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>敏感数据属性列表</returns>
        public static IEnumerable<PropertyInfo> GetSensitiveDataProperties<T>() where T : class
        {
            var entityType = typeof(T);
            return entityType.GetProperties()
                .Where(p => p.GetCustomAttribute<SensitiveDataAttribute>() != null)
                .Where(p => p.PropertyType == typeof(string));
        }

        /// <summary>
        /// 简单检查字符串是否已经被加密（基于Base64格式判断）
        /// </summary>
        /// <param name="value">要检查的字符串</param>
        /// <returns>是否已加密</returns>
        private static bool IsEncrypted(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 20)
                return false;

            try
            {
                Convert.FromBase64String(value);
                return value.Length > 40; // 加密后的数据通常比较长
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Service层敏感数据处理扩展方法
    /// </summary>
    public static class ServiceSensitiveDataExtensions
    {
        /// <summary>
        /// 为Service结果解密敏感数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="result">Service结果</param>
        /// <param name="encryptionService">加密服务</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>解密后的结果</returns>
        public static LYBT.Shared.Models.Contracts.Common.ServiceResult<T> DecryptSensitiveData<T>(
            this LYBT.Shared.Models.Contracts.Common.ServiceResult<T> result,
            IDataEncryptionService encryptionService,
            ILogger? logger = null) where T : class
        {
            if (result?.IsSuccess == true && result.Data != null && encryptionService != null)
            {
                result.Data = SensitiveDataHelper.DecryptSensitiveData(result.Data, encryptionService, logger);
            }

            return result;
        }

        /// <summary>
        /// 为Service结果列表解密敏感数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="result">Service结果</param>
        /// <param name="encryptionService">加密服务</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>解密后的结果</returns>
        public static LYBT.Shared.Models.Contracts.Common.ServiceResult<IEnumerable<T>> DecryptSensitiveData<T>(
            this LYBT.Shared.Models.Contracts.Common.ServiceResult<IEnumerable<T>> result,
            IDataEncryptionService encryptionService,
            ILogger? logger = null) where T : class
        {
            if (result?.IsSuccess == true && result.Data != null && encryptionService != null)
            {
                result.Data = SensitiveDataHelper.DecryptSensitiveData(result.Data, encryptionService, logger);
            }

            return result;
        }

        /// <summary>
        /// 为分页结果解密敏感数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="result">分页结果</param>
        /// <param name="encryptionService">加密服务</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>解密后的分页结果</returns>
        public static LYBT.Shared.Models.Contracts.Common.PagedResult<T> DecryptSensitiveData<T>(
            this LYBT.Shared.Models.Contracts.Common.PagedResult<T> result,
            IDataEncryptionService encryptionService,
            ILogger? logger = null) where T : class
        {
            if (result?.Items != null && encryptionService != null)
            {
                result.Items = SensitiveDataHelper.DecryptSensitiveData(result.Items, encryptionService, logger).ToList();
            }

            return result;
        }
    }
}
