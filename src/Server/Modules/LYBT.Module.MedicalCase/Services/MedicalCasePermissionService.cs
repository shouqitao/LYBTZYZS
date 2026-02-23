using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案权限服务实现 - OpenSpec: refactor-medicalcase-management
    /// LIFECYCLE-007: 医案编辑权限控制
    /// </summary>
    public class MedicalCasePermissionService : IMedicalCasePermissionService
    {
        private readonly ILogger<MedicalCasePermissionService> _logger;

        public MedicalCasePermissionService(ILogger<MedicalCasePermissionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 检查用户是否有权编辑指定医案
        /// LIFECYCLE-007 权限规则:
        /// - 管理员(Admin/SuperAdmin)可以编辑所有医案
        /// - 医生只能编辑自己创建的医案
        /// - Draft/Active状态：不受跨日限制，随时可编辑
        /// - Completed状态：当天可编辑，跨日后锁定
        /// - 已软删除(IsDeleted)：不可编辑（由 EF 全局过滤器排除）
        /// </summary>
        public bool CanEdit(Guid userId, UserRole role, MedicalCase medicalCase)
        {
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.CheckPermission → NullEntity");
                return false;
            }

            // 管理员可以编辑所有医案
            if (IsAdmin(role))
            {
                _logger.LogDebug("[SVC] MedicalCase.CheckPermission → AdminGranted - MedicalCaseId={MedicalCaseId} Role={Role}",
                    medicalCase.Id, role);
                return true;
            }

            // 医生权限检查
            if (role == UserRole.Doctor)
            {
                // 检查是否是创建者
                var isOwner = medicalCase.UserId == userId;
                if (!isOwner)
                {
                    _logger.LogDebug("[SVC] MedicalCase.CheckPermission → NotOwner - MedicalCaseId={MedicalCaseId} UserId={UserId}",
                        medicalCase.Id, userId);
                    return false;
                }

                // Draft/Active 状态：随时可编辑，不受跨日限制
                if (medicalCase.IsActive)
                {
                    _logger.LogDebug("[SVC] MedicalCase.CheckPermission → ActiveGranted - MedicalCaseId={MedicalCaseId} Status={Status}",
                        medicalCase.Id, medicalCase.CaseStatus);
                    return true;
                }

                // Completed 状态：当天可编辑，跨日后锁定
                if (medicalCase.IsCompleted)
                {
                    var completionDate = (medicalCase.CompletedAt ?? medicalCase.CreatedAt).Date;
                    var isToday = completionDate == DateTime.Today;

                    if (isToday)
                    {
                        _logger.LogDebug("[SVC] MedicalCase.CheckPermission → CompletedTodayGranted - MedicalCaseId={MedicalCaseId}",
                            medicalCase.Id);
                        return true;
                    }
                    else
                    {
                        _logger.LogDebug("[SVC] MedicalCase.CheckPermission → CompletedLocked - MedicalCaseId={MedicalCaseId} CompletionDate={CompletionDate}",
                            medicalCase.Id, completionDate);
                        return false;
                    }
                }

                // 其他状态：不可编辑（已软删除由 EF 全局过滤器排除）
                _logger.LogDebug("[SVC] MedicalCase.CheckPermission → StatusDenied - MedicalCaseId={MedicalCaseId} Status={Status}",
                    medicalCase.Id, medicalCase.CaseStatus);
                return false;
            }

            // 其他角色默认无权编辑
            _logger.LogDebug("[SVC] MedicalCase.CheckPermission → RoleDenied - Role={Role}", role);
            return false;
        }

        /// <summary>
        /// 检查用户是否有权编辑指定医案 (isAdmin 重载，供 Service 层使用)
        /// 将 bool isAdmin 转换为 UserRole 后委托给主方法
        /// </summary>
        public bool CanEdit(Guid userId, bool isAdmin, MedicalCase medicalCase)
        {
            var role = isAdmin ? UserRole.Admin : UserRole.Doctor;
            return CanEdit(userId, role, medicalCase);
        }

        /// <summary>
        /// 检查用户是否有权删除指定医案 (isAdmin 重载，供 Service 层使用)
        /// </summary>
        public bool CanDelete(Guid userId, bool isAdmin, MedicalCase medicalCase)
        {
            var role = isAdmin ? UserRole.Admin : UserRole.Doctor;
            return CanDelete(userId, role, medicalCase);
        }

        /// <summary>
        /// 检查用户是否有权创建医案
        /// optimize-api-permissions: 只有医生(Doctor)可以创建医案，管理员不能创建
        /// </summary>
        public bool CanCreate(Guid userId, UserRole role)
        {
            // 空Guid表示未登录
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("[SVC] MedicalCase.CheckPermission → NotAuthenticated");
                return false;
            }

            // optimize-api-permissions: Admin/SuperAdmin不能创建医案，只有Doctor可以
            // 医案必须由接诊医生创建，管理员只能查看和编辑
            if (IsAdmin(role))
            {
                _logger.LogDebug("[SVC] MedicalCase.CheckPermission → AdminCannotCreate - Role={Role}", role);
                return false;
            }

            return role == UserRole.Doctor;
        }

        /// <summary>
        /// 检查用户是否有权删除指定医案
        /// 删除权限与编辑权限相同
        /// </summary>
        public bool CanDelete(Guid userId, UserRole role, MedicalCase medicalCase)
        {
            // 删除权限与编辑权限相同
            return CanEdit(userId, role, medicalCase);
        }

        /// <summary>
        /// 检查是否需要提供修改原因
        /// S3: 扩展规则 -- IsLocked OR Completed 状态均需提供原因
        /// </summary>
        public bool RequiresEditReason(MedicalCase medicalCase)
        {
            if (medicalCase == null) return false;

            // S3: 已锁定 或 已完成状态 都需要提供修改原因
            return medicalCase.IsLocked || medicalCase.IsCompleted;
        }

        /// <summary>
        /// S3: 扩展版本 -- 同时检查是否为非本人编辑
        /// </summary>
        public bool RequiresEditReason(MedicalCase medicalCase, Guid currentUserId)
        {
            if (medicalCase == null) return false;

            // IsLocked OR 非本人 OR 已完成
            return medicalCase.IsLocked
                || medicalCase.UserId != currentUserId
                || medicalCase.IsCompleted;
        }

        /// <summary>
        /// 获取用户对医案的权限详情
        /// </summary>
        public MedicalCasePermissionDto GetPermissions(Guid userId, UserRole role, MedicalCase medicalCase)
        {
            var canEdit = CanEdit(userId, role, medicalCase);
            var canDelete = CanDelete(userId, role, medicalCase);
            var requiresReason = RequiresEditReason(medicalCase);

            string? denialReason = null;
            if (!canEdit)
            {
                denialReason = GetDenialReason(userId, role, medicalCase);
            }

            return new MedicalCasePermissionDto
            {
                CanEdit = canEdit,
                CanDelete = canDelete,
                RequiresEditReason = requiresReason,
                DenialReason = denialReason
            };
        }

        #region Private Methods

        /// <summary>
        /// 检查是否为管理员角色
        /// </summary>
        private static bool IsAdmin(UserRole role)
        {
            return role == UserRole.Admin || role == UserRole.SuperAdmin;
        }

        /// <summary>
        /// 获取权限拒绝原因
        /// </summary>
        private string GetDenialReason(Guid userId, UserRole role, MedicalCase medicalCase)
        {
            if (medicalCase == null)
                return "医案不存在";

            if (role == UserRole.Doctor)
            {
                var isOwner = medicalCase.UserId == userId;
                if (!isOwner)
                    return "您不是该医案的创建者，无权编辑";

                // Completed 状态 + 跨日 = 锁定
                if (medicalCase.IsCompleted && medicalCase.IsLocked)
                {
                    return "该医案已完成且已过当天编辑时间，医生无法编辑";
                }

                // 已软删除的医案由 EF 全局过滤器排除，正常不会到达此处
            }

            return "权限不足";
        }

        #endregion
    }
}
