using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Validators.BusinessRules
{
    /// <summary>
    /// 业务规则验证上下文
    /// Phase 3 Task 3.4: 统一业务规则验证框架
    /// </summary>
    public class ValidationContext
    {
        /// <summary>
        /// 当前操作用户ID
        /// </summary>
        public Guid? CurrentUserId { get; set; }

        /// <summary>
        /// 当前用户角色
        /// </summary>
        public UserRole? CurrentUserRole { get; set; }

        /// <summary>
        /// 是否管理员
        /// </summary>
        public bool IsAdmin => CurrentUserRole.HasValue && CurrentUserRole.Value >= UserRole.Admin;

        /// <summary>
        /// 是否超级管理员
        /// </summary>
        public bool IsSuperAdmin => CurrentUserRole.HasValue && CurrentUserRole.Value >= UserRole.SuperAdmin;

        /// <summary>
        /// 操作类型
        /// </summary>
        public BusinessOperation OperationType { get; set; }

        /// <summary>
        /// 额外参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// 创建上下文的便捷方法
        /// </summary>
        public static ValidationContext Create(Guid? currentUserId, UserRole? currentUserRole, BusinessOperation operationType)
        {
            return new ValidationContext
            {
                CurrentUserId = currentUserId,
                CurrentUserRole = currentUserRole,
                OperationType = operationType
            };
        }

        /// <summary>
        /// 添加参数
        /// </summary>
        public ValidationContext WithParameter(string key, object value)
        {
            Parameters[key] = value;
            return this;
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        public T? GetParameter<T>(string key)
        {
            return Parameters.TryGetValue(key, out var value) && value is T ? (T)value : default(T);
        }

        /// <summary>
        /// 权限检查：当前用户是否可以管理目标角色
        /// </summary>
        public bool CanManageRole(UserRole targetRole)
        {
            if (!CurrentUserRole.HasValue) return false;

            var currentRole = CurrentUserRole.Value;

            // 超级管理员可以管理所有角色
            if (currentRole == UserRole.SuperAdmin) return true;

            // 管理员可以管理医生，但不能管理管理员和超级管理员
            if (currentRole == UserRole.Admin) return targetRole == UserRole.Doctor;

            // 医生只能管理自己的信息，不能管理其他用户
            return false;
        }

        /// <summary>
        /// 检查是否为所有者或管理员
        /// </summary>
        public bool IsOwnerOrAdmin(Guid? ownerId)
        {
            return IsAdmin || (CurrentUserId.HasValue && ownerId.HasValue && CurrentUserId.Value == ownerId.Value);
        }
    }

    /// <summary>
    /// 业务操作类型
    /// </summary>
    public enum BusinessOperation
    {
        /// <summary>
        /// 创建操作
        /// </summary>
        Create,

        /// <summary>
        /// 更新操作
        /// </summary>
        Update,

        /// <summary>
        /// 删除操作
        /// </summary>
        Delete,

        /// <summary>
        /// 查询操作
        /// </summary>
        Read,

        /// <summary>
        /// 状态切换操作
        /// </summary>
        ToggleStatus,

        /// <summary>
        /// 自定义操作
        /// </summary>
        Custom
    }
}