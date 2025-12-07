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
        /// - 医生只能编辑自己创建的、未完成(Draft/Active)的医案
        /// - 医生不能编辑他人医案
        /// - 医生不能编辑已完成(Completed)的医案
        /// </summary>
        public bool CanEdit(Guid userId, UserRole role, MedicalCase medicalCase)
        {
            if (medicalCase == null)
            {
                _logger.LogWarning("权限检查: 医案为null");
                return false;
            }

            // 管理员可以编辑所有医案
            if (IsAdmin(role))
            {
                _logger.LogDebug("权限检查: 管理员({Role})可以编辑医案 {MedicalCaseId}",
                    role, medicalCase.Id);
                return true;
            }

            // 医生权限检查
            if (role == UserRole.Doctor)
            {
                // 检查是否是创建者
                var isOwner = medicalCase.DoctorId == userId;
                if (!isOwner)
                {
                    _logger.LogDebug("权限检查: 医生 {UserId} 不是医案 {MedicalCaseId} 的创建者",
                        userId, medicalCase.Id);
                    return false;
                }

                // 检查医案状态
                var isEditable = medicalCase.CaseStatus == MedicalCaseStatus.Draft
                              || medicalCase.CaseStatus == MedicalCaseStatus.Active;

                if (!isEditable)
                {
                    _logger.LogDebug("权限检查: 医生 {UserId} 无法编辑已完成的医案 {MedicalCaseId}, 状态: {Status}",
                        userId, medicalCase.Id, medicalCase.CaseStatus);
                }

                return isEditable;
            }

            // 其他角色默认无权编辑
            _logger.LogDebug("权限检查: 角色 {Role} 无编辑权限", role);
            return false;
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
                _logger.LogWarning("权限检查: 未登录用户尝试创建医案");
                return false;
            }

            // optimize-api-permissions: Admin/SuperAdmin不能创建医案，只有Doctor可以
            // 医案必须由接诊医生创建，管理员只能查看和编辑
            if (IsAdmin(role))
            {
                _logger.LogDebug("权限检查: 管理员({Role})不能创建医案", role);
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
        /// 已完成(Completed)医案修改时必须提供原因
        /// </summary>
        public bool RequiresEditReason(MedicalCase medicalCase)
        {
            if (medicalCase == null) return false;

            // 已完成医案修改需要原因
            return medicalCase.CaseStatus == MedicalCaseStatus.Completed;
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
                var isOwner = medicalCase.DoctorId == userId;
                if (!isOwner)
                    return "您不是该医案的创建者，无权编辑";

                if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
                    return "该医案已完成，医生无法编辑已完成的医案";
            }

            return "权限不足";
        }

        #endregion
    }
}
