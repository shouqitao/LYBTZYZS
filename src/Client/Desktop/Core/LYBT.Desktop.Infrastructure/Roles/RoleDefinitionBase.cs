using LYBT.Desktop.Contracts.Roles;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Roles
{
    /// <summary>
    /// 角色定义基类 - 提供共享的基础模块配置
    /// refactor-auth-role-system Phase 2.1.4
    /// </summary>
    public abstract class RoleDefinitionBase : IRoleDefinition
    {
        /// <summary>
        /// 所有角色共享的基础模块
        /// </summary>
        private static readonly string[] SharedBaseModules = new[]
        {
            "AuthModule",
            "UsersModule"
        };

        /// <inheritdoc/>
        public abstract UserRole Role { get; }

        /// <inheritdoc/>
        public abstract string DisplayName { get; }

        /// <inheritdoc/>
        public abstract string Description { get; }

        /// <inheritdoc/>
        public abstract string HomeViewName { get; }

        /// <inheritdoc/>
        public abstract IReadOnlyList<string> RequiredModules { get; }

        /// <inheritdoc/>
        public IReadOnlyList<string> BaseModules => SharedBaseModules;

        /// <inheritdoc/>
        public IEnumerable<string> GetAllModules()
        {
            return BaseModules.Concat(RequiredModules).Distinct();
        }
    }
}
