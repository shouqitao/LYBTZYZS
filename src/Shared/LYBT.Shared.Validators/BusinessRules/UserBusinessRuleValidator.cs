using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Validators.BusinessRules
{
    /// <summary>
    /// 用户业务规则验证器
    /// Phase 3 Task 3.4: 统一业务规则验证框架
    /// 整合UserService中的权限检查逻辑
    /// 注意：Shared层不能直接引用Entity，主要通过OperationValidator验证输入DTO
    /// </summary>
    public class UserBusinessRuleValidator : BaseBusinessOperationValidator<UserInputDto>
    {
        public override string ValidatorName => "UserBusinessRuleValidator";

        public override string Description => "用户业务规则验证器，处理权限控制、角色管理、用户状态等业务规则";

        public UserBusinessRuleValidator(ILogger<UserBusinessRuleValidator> logger) : base(logger) { }

        #region 操作验证

        /// <summary>
        /// 验证用户输入DTO的业务规则
        /// </summary>
        public override Task<ValidationResult> ValidateAsync(UserInputDto input, ValidationContext? context = null)
        {
            if (input == null)
            {
                return Task.FromResult(Failure("用户输入数据不能为空"));
            }

            var results = new List<ValidationResult>
            {
                ValidateUsernameAsync(input),
                ValidateRoleAsync(input, context),
                ValidatePasswordAsync(input, context)
            };

            foreach (var result in results)
            {
                if (!result.IsValid)
                {
                    return Task.FromResult(result);
                }
            }

            return Task.FromResult(Success());
        }

        /// <summary>
        /// 验证用户名规则
        /// </summary>
        private ValidationResult ValidateUsernameAsync(UserInputDto input)
        {
            if (string.IsNullOrWhiteSpace(input.UserName))
            {
                return Failure("用户名不能为空");
            }

            // 检查系统保留用户名
            var reservedUsernames = new[] { "admin", "administrator", "root", "system", "superadmin", "sysadmin" };
            if (reservedUsernames.Any(reserved =>
                string.Equals(input.UserName, reserved, StringComparison.OrdinalIgnoreCase)))
            {
                return Failure($"用户名 '{input.UserName}' 为系统保留用户名，不可使用");
            }

            return Success();
        }

        /// <summary>
        /// 验证角色权限
        /// </summary>
        private ValidationResult ValidateRoleAsync(UserInputDto input, ValidationContext? context)
        {
            // 如果没有上下文，跳过权限验证
            if (context == null)
            {
                return Success();
            }

            // 权限检查：不能创建比自己权限高的角色
            if (input.Role.HasValue && !context.CanManageRole(input.Role.Value))
            {
                var roleName = GetRoleDisplayName(input.Role.Value);
                return Failure($"您没有权限创建{roleName}账户");
            }

            return Success();
        }

        /// <summary>
        /// 验证密码规则
        /// </summary>
        private ValidationResult ValidatePasswordAsync(UserInputDto input, ValidationContext? context)
        {
            // 如果是更新操作且没有提供密码，则跳过密码验证
            if (context?.OperationType == BusinessOperation.Update && string.IsNullOrEmpty(input.Password))
            {
                return Success();
            }

            // 密码规则验证（如果有提供密码）
            if (!string.IsNullOrEmpty(input.Password))
            {
                if (input.Password.Length < 6)
                {
                    return Failure("密码长度不能少于6位");
                }

                // 可以添加更多密码复杂度规则
                // 例如：必须包含数字、字母、特殊字符等
            }

            return Success();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取角色显示名称
        /// </summary>
        private static string GetRoleDisplayName(UserRole role)
        {
            return role switch
            {
                UserRole.SuperAdmin => "超级管理员",
                UserRole.Admin => "管理员",
                UserRole.Doctor => "医生",
                _ => "未知角色"
            };
        }

        #endregion
    }
}