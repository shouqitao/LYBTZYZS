using System.Collections.Concurrent;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Roles
{
    /// <summary>
    /// 角色注册表实现 - 管理所有角色定义
    /// refactor-auth-role-system Phase 2.1.2
    /// </summary>
    public class RoleRegistry : IRoleRegistry
    {
        private readonly ConcurrentDictionary<UserRole, IRoleDefinition> _definitions = new();
        private readonly ILogger<RoleRegistry> _logger;

        /// <summary>
        /// 默认主页视图（当角色未注册时使用）
        /// </summary>
        private const string DefaultHomeView = "ClinicalHomeView";

        public RoleRegistry(ILogger<RoleRegistry> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void Register(IRoleDefinition roleDefinition)
        {
            if (roleDefinition == null)
                throw new ArgumentNullException(nameof(roleDefinition));

            if (_definitions.TryAdd(roleDefinition.Role, roleDefinition))
            {
                _logger.LogInformation(
                    "角色定义已注册: {Role} ({DisplayName}), 主页: {HomeView}, 模块数: {ModuleCount}",
                    roleDefinition.Role,
                    roleDefinition.DisplayName,
                    roleDefinition.HomeViewName,
                    roleDefinition.RequiredModules.Count);
            }
            else
            {
                _logger.LogWarning("角色 {Role} 已存在，跳过重复注册", roleDefinition.Role);
            }
        }

        /// <inheritdoc/>
        public IRoleDefinition? GetDefinition(UserRole role)
        {
            _definitions.TryGetValue(role, out var definition);
            return definition;
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<IRoleDefinition> GetAllDefinitions()
        {
            return _definitions.Values.ToList().AsReadOnly();
        }

        /// <inheritdoc/>
        public bool IsRegistered(UserRole role)
        {
            return _definitions.ContainsKey(role);
        }

        /// <inheritdoc/>
        public string GetHomeViewName(UserRole role)
        {
            if (_definitions.TryGetValue(role, out var definition))
            {
                return definition.HomeViewName;
            }

            _logger.LogWarning("角色 {Role} 未注册，使用默认主页: {DefaultView}", role, DefaultHomeView);
            return DefaultHomeView;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetModulesForRole(UserRole role)
        {
            if (_definitions.TryGetValue(role, out var definition))
            {
                return definition.GetAllModules();
            }

            _logger.LogWarning("角色 {Role} 未注册，返回空模块列表", role);
            return Enumerable.Empty<string>();
        }
    }
}
