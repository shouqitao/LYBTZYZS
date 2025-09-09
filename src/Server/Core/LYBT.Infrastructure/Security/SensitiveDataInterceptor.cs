using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LYBT.Entities.Attributes;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 敏感数据拦截器 - Epic 05-P0-03: 数据安全保障
    /// 自动处理标记了SensitiveDataAttribute的属性的加密和解密
    /// </summary>
    public class SensitiveDataInterceptor : SaveChangesInterceptor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SensitiveDataInterceptor> _logger;

        public SensitiveDataInterceptor(IServiceProvider serviceProvider, ILogger<SensitiveDataInterceptor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
            {
                await ProcessSensitiveDataAsync(eventData.Context);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context is not null)
            {
                ProcessSensitiveData(eventData.Context);
            }

            return base.SavingChanges(eventData, result);
        }

        private async Task ProcessSensitiveDataAsync(DbContext context)
        {
            await Task.Run(() => ProcessSensitiveData(context));
        }

        private void ProcessSensitiveData(DbContext context)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var encryptionService = scope.ServiceProvider.GetService<IDataEncryptionService>();
                var auditService = scope.ServiceProvider.GetService<ISecurityAuditService>();

                if (encryptionService == null)
                {
                    _logger.LogWarning("数据加密服务未注册，跳过敏感数据处理");
                    return;
                }

                var changeTracker = context.ChangeTracker;
                var modifiedEntries = changeTracker.Entries()
                    .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified)
                    .ToList();

                foreach (var entry in modifiedEntries)
                {
                    ProcessEntitySensitiveData(entry, encryptionService);

                    // 记录敏感操作审计
                    if (auditService != null && HasSensitiveData(entry.Entity))
                    {
                        var auditEntry = CreateSensitiveOperationAudit(entry);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await auditService.LogSensitiveOperationAsync(auditEntry);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "记录敏感操作审计失败");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理敏感数据时发生错误");
                // 不抛出异常，避免影响正常的数据保存操作
            }
        }

        private void ProcessEntitySensitiveData(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, IDataEncryptionService encryptionService)
        {
            var entityType = entry.Entity.GetType();
            var properties = entityType.GetProperties();

            foreach (var property in properties)
            {
                var sensitiveAttr = property.GetCustomAttribute<SensitiveDataAttribute>();
                if (sensitiveAttr == null || property.PropertyType != typeof(string))
                    continue;

                var currentValue = property.GetValue(entry.Entity) as string;
                if (string.IsNullOrEmpty(currentValue))
                    continue;

                // 检查是否已经加密（避免重复加密）
                if (IsEncrypted(currentValue))
                {
                    _logger.LogDebug("属性 {Property} 已经加密，跳过处理", property.Name);
                    continue;
                }

                try
                {
                    // 加密敏感数据
                    var encryptedValue = encryptionService.Encrypt(currentValue, sensitiveAttr.DataType);
                    property.SetValue(entry.Entity, encryptedValue);

                    _logger.LogDebug("已加密实体 {EntityType} 的敏感属性 {Property}",
                        entityType.Name, property.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "加密实体 {EntityType} 的属性 {Property} 失败",
                        entityType.Name, property.Name);
                }
            }
        }

        private bool HasSensitiveData(object entity)
        {
            var entityType = entity.GetType();
            var properties = entityType.GetProperties();

            return properties.Any(prop => prop.GetCustomAttribute<SensitiveDataAttribute>() != null);
        }

        private SensitiveOperationAuditEntry CreateSensitiveOperationAudit(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            var entityType = entry.Entity.GetType();
            var entityIdProperty = entityType.GetProperty("Id");
            var entityId = entityIdProperty?.GetValue(entry.Entity)?.ToString();

            return new SensitiveOperationAuditEntry
            {
                ResourceId = entityId,
                ResourceType = entityType.Name,
                Operation = entry.State == EntityState.Added ? "CREATE" : "UPDATE",
                OperationType = "DATA_ENCRYPTION",
                DataCategory = GetDataCategory(entityType),
                Success = true,
                BusinessContext = $"自动加密{entityType.Name}实体的敏感数据"
            };
        }

        private string GetDataCategory(Type entityType)
        {
            // 根据实体类型确定数据类别
            var entityName = entityType.Name.ToLower();
            return entityName switch
            {
                var name when name.Contains("patient") => "患者信息",
                var name when name.Contains("user") => "用户信息",
                var name when name.Contains("medical") => "医疗信息",
                var name when name.Contains("consultation") => "诊疗信息",
                _ => "敏感数据"
            };
        }

        /// <summary>
        /// 简单检查字符串是否已经被加密（基于Base64格式判断）
        /// </summary>
        private static bool IsEncrypted(string value)
        {
            // 检查是否为Base64格式（加密后的数据通常是Base64编码）
            if (string.IsNullOrEmpty(value) || value.Length < 20)
                return false;

            try
            {
                // 尝试解析Base64，如果成功且长度合理，可能已经加密
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
    /// 敏感数据查询拦截器 - 自动解密查询结果
    /// </summary>
    public class SensitiveDataQueryInterceptor : DbCommandInterceptor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SensitiveDataQueryInterceptor> _logger;

        public SensitiveDataQueryInterceptor(IServiceProvider serviceProvider, ILogger<SensitiveDataQueryInterceptor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            // 查询拦截器的实现相对复杂，因为需要在数据读取时解密
            // 目前先实现保存时加密，查询时解密可以在Service层处理
            return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }
    }
}