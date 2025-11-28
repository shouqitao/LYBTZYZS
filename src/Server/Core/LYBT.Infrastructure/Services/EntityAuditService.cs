using System.Reflection;
using System.Text.Json;
using LYBT.Entities.Common;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Services
{
    /// <summary>
    /// 通用审计服务实现
    /// OpenSpec: add-global-audit-system
    /// 提供对任意BaseEntity子类的变更审计能力
    /// </summary>
    /// <typeparam name="TEntity">业务实体类型</typeparam>
    public class EntityAuditService<TEntity> : IAuditService<TEntity> where TEntity : BaseEntity
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<EntityAuditService<TEntity>> _logger;
        private readonly string _entityTypeName;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // 排除的属性（BaseEntity的系统字段和导航属性）
        private static readonly HashSet<string> _excludedProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "Id", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy", "RowVersion", "IsDeleted"
        };

        public EntityAuditService(
            AppDbContext dbContext,
            ILogger<EntityAuditService<TEntity>> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _entityTypeName = typeof(TEntity).Name;
        }

        /// <inheritdoc/>
        public async Task LogCreateAsync(
            TEntity entity,
            Guid operatorId,
            string operatorName,
            UserRole role)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                var newValues = ExtractEntityValues(entity);

                var auditLog = new EntityAuditLog
                {
                    EntityType = _entityTypeName,
                    EntityId = entity.Id,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    OperatorRole = role,
                    OperationType = AuditOperationType.Create,
                    ChangedFields = JsonSerializer.Serialize(newValues.Keys, _jsonOptions),
                    OldValues = null,
                    NewValues = JsonSerializer.Serialize(newValues, _jsonOptions),
                    Reason = null,
                    CreatedAt = DateTime.Now
                };

                _dbContext.EntityAuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "审计日志: Create {EntityType} {EntityId}, 操作者: {OperatorName}({OperatorRole})",
                    _entityTypeName, entity.Id, operatorName, role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "记录审计日志失败: {EntityType} {EntityId}, 操作者: {OperatorId}",
                    _entityTypeName, entity.Id, operatorId);
                // 审计日志失败不应影响主业务流程
            }
        }

        /// <inheritdoc/>
        public async Task LogUpdateAsync(
            TEntity? before,
            TEntity after,
            Guid operatorId,
            string operatorName,
            UserRole role,
            string? reason = null)
        {
            if (after == null)
                throw new ArgumentNullException(nameof(after));

            try
            {
                var (changedFields, oldValues, newValues) = DetectChanges(before, after);

                // 如果没有变更，不记录日志
                if (changedFields == null || changedFields.Count == 0)
                {
                    _logger.LogDebug(
                        "无变更，跳过审计日志: {EntityType} {EntityId}",
                        _entityTypeName, after.Id);
                    return;
                }

                var auditLog = new EntityAuditLog
                {
                    EntityType = _entityTypeName,
                    EntityId = after.Id,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    OperatorRole = role,
                    OperationType = AuditOperationType.Update,
                    ChangedFields = JsonSerializer.Serialize(changedFields, _jsonOptions),
                    OldValues = JsonSerializer.Serialize(oldValues, _jsonOptions),
                    NewValues = JsonSerializer.Serialize(newValues, _jsonOptions),
                    Reason = reason,
                    CreatedAt = DateTime.Now
                };

                _dbContext.EntityAuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "审计日志: Update {EntityType} {EntityId}, 操作者: {OperatorName}({OperatorRole}), 变更字段: {ChangedFields}",
                    _entityTypeName, after.Id, operatorName, role, string.Join(", ", changedFields));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "记录审计日志失败: {EntityType} {EntityId}, 操作者: {OperatorId}",
                    _entityTypeName, after.Id, operatorId);
            }
        }

        /// <inheritdoc/>
        public async Task LogDeleteAsync(
            TEntity entity,
            Guid operatorId,
            string operatorName,
            UserRole role,
            string? reason = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                var oldValues = ExtractEntityValues(entity);

                var auditLog = new EntityAuditLog
                {
                    EntityType = _entityTypeName,
                    EntityId = entity.Id,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    OperatorRole = role,
                    OperationType = AuditOperationType.SoftDelete,
                    ChangedFields = JsonSerializer.Serialize(new[] { "IsDeleted" }, _jsonOptions),
                    OldValues = JsonSerializer.Serialize(oldValues, _jsonOptions),
                    NewValues = null,
                    Reason = reason,
                    CreatedAt = DateTime.Now
                };

                _dbContext.EntityAuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "审计日志: Delete {EntityType} {EntityId}, 操作者: {OperatorName}({OperatorRole}), 原因: {Reason}",
                    _entityTypeName, entity.Id, operatorName, role, reason ?? "未指定");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "记录审计日志失败: {EntityType} {EntityId}, 操作者: {OperatorId}",
                    _entityTypeName, entity.Id, operatorId);
            }
        }

        /// <inheritdoc/>
        public async Task<(List<EntityAuditLog> Logs, int TotalCount)> GetLogsAsync(
            Guid entityId,
            int page = 1,
            int pageSize = 20)
        {
            var query = _dbContext.EntityAuditLogs
                .Where(l => l.EntityType == _entityTypeName && l.EntityId == entityId)
                .OrderByDescending(l => l.CreatedAt);

            var totalCount = await query.CountAsync();

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }

        #region Private Methods

        /// <summary>
        /// 提取实体的业务字段值
        /// </summary>
        private Dictionary<string, object?> ExtractEntityValues(TEntity entity)
        {
            var values = new Dictionary<string, object?>();
            var properties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // 排除系统字段、导航属性和复杂类型
                if (_excludedProperties.Contains(prop.Name))
                    continue;

                // 排除导航属性（集合类型或其他实体类型）
                if (IsNavigationProperty(prop))
                    continue;

                try
                {
                    var value = prop.GetValue(entity);
                    // 枚举类型转为字符串
                    if (value != null && prop.PropertyType.IsEnum)
                    {
                        value = value.ToString();
                    }
                    values[prop.Name] = value;
                }
                catch
                {
                    // 忽略无法获取的属性
                }
            }

            return values;
        }

        /// <summary>
        /// 检测两个实体之间的变更
        /// </summary>
        private (List<string>? ChangedFields, Dictionary<string, object?>? OldValues, Dictionary<string, object?>? NewValues)
            DetectChanges(TEntity? before, TEntity after)
        {
            if (before == null)
            {
                // 无before实体时，记录所有字段为新增
                var newValues = ExtractEntityValues(after);
                return (newValues.Keys.ToList(), null, newValues);
            }

            var changedFields = new List<string>();
            var oldValues = new Dictionary<string, object?>();
            var newValuesDict = new Dictionary<string, object?>();

            var properties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (_excludedProperties.Contains(prop.Name))
                    continue;

                if (IsNavigationProperty(prop))
                    continue;

                try
                {
                    var oldValue = prop.GetValue(before);
                    var newValue = prop.GetValue(after);

                    // 枚举类型转为字符串进行比较
                    if (prop.PropertyType.IsEnum)
                    {
                        oldValue = oldValue?.ToString();
                        newValue = newValue?.ToString();
                    }

                    if (!Equals(oldValue, newValue))
                    {
                        changedFields.Add(prop.Name);
                        oldValues[prop.Name] = oldValue;
                        newValuesDict[prop.Name] = newValue;
                    }
                }
                catch
                {
                    // 忽略无法比较的属性
                }
            }

            if (changedFields.Count == 0)
                return (null, null, null);

            return (changedFields, oldValues, newValuesDict);
        }

        /// <summary>
        /// 判断是否为导航属性
        /// </summary>
        private static bool IsNavigationProperty(PropertyInfo prop)
        {
            var type = prop.PropertyType;

            // 排除集合类型
            if (type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                 type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                 type.GetGenericTypeDefinition() == typeof(List<>)))
            {
                return true;
            }

            // 排除其他实体类型（继承自BaseEntity的类型）
            if (type.IsClass && type != typeof(string) && typeof(BaseEntity).IsAssignableFrom(type))
            {
                return true;
            }

            // 排除virtual属性（通常是导航属性）
            var getter = prop.GetGetMethod();
            if (getter != null && getter.IsVirtual && !getter.IsFinal)
            {
                // 但要保留基本类型的virtual属性
                if (!type.IsPrimitive && type != typeof(string) && type != typeof(DateTime) &&
                    type != typeof(DateTime?) && type != typeof(Guid) && type != typeof(Guid?) &&
                    type != typeof(decimal) && type != typeof(decimal?))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
