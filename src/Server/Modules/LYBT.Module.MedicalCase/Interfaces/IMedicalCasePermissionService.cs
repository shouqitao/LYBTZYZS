using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCases.Interfaces
{
    /// <summary>
    /// 医案权限服务接口 - OpenSpec: refactor-medicalcase-management
    /// LIFECYCLE-007: 医案编辑权限控制
    ///
    /// 权限矩阵:
    /// ┌──────────────┬─────────┬──────────┬───────────┬─────────────────────┐
    /// │ 角色         │ Draft   │ Active   │ Completed │ 说明                │
    /// ├──────────────┼─────────┼──────────┼───────────┼─────────────────────┤
    /// │ Doctor(自己) │ ✓       │ ✓        │ ✗         │ 只能编辑自己未完成的 │
    /// │ Doctor(他人) │ ✗       │ ✗        │ ✗         │ 不能修改他人医案     │
    /// │ Admin        │ ✓       │ ✓        │ ✓         │ 可编辑所有医案       │
    /// └──────────────┴─────────┴──────────┴───────────┴─────────────────────┘
    /// </summary>
    public interface IMedicalCasePermissionService
    {
        /// <summary>
        /// 检查用户是否有权编辑指定医案
        /// </summary>
        /// <param name="userId">当前用户ID</param>
        /// <param name="role">用户角色</param>
        /// <param name="medicalCase">医案实体</param>
        /// <returns>是否有编辑权限</returns>
        bool CanEdit(Guid userId, UserRole role, MedicalCase medicalCase);

        /// <summary>
        /// 检查用户是否有权编辑指定医案 (isAdmin 重载，供 Service 层使用)
        /// </summary>
        bool CanEdit(Guid userId, bool isAdmin, MedicalCase medicalCase);

        /// <summary>
        /// 检查用户是否有权创建医案
        /// </summary>
        /// <param name="userId">当前用户ID</param>
        /// <param name="role">用户角色</param>
        /// <returns>是否有创建权限</returns>
        bool CanCreate(Guid userId, UserRole role);

        /// <summary>
        /// 检查用户是否有权删除指定医案
        /// </summary>
        /// <param name="userId">当前用户ID</param>
        /// <param name="role">用户角色</param>
        /// <param name="medicalCase">医案实体</param>
        /// <returns>是否有删除权限</returns>
        bool CanDelete(Guid userId, UserRole role, MedicalCase medicalCase);

        /// <summary>
        /// 检查用户是否有权删除指定医案 (isAdmin 重载，供 Service 层使用)
        /// </summary>
        bool CanDelete(Guid userId, bool isAdmin, MedicalCase medicalCase);

        /// <summary>
        /// 检查是否需要提供修改原因
        /// (已完成医案修改时必须提供原因)
        /// </summary>
        /// <param name="medicalCase">医案实体</param>
        /// <returns>是否需要修改原因</returns>
        bool RequiresEditReason(MedicalCase medicalCase);

        /// <summary>
        /// S3: 检查是否需要提供修改原因（扩展版，考虑当前操作者）
        /// IsLocked OR 非本人 OR 已完成
        /// </summary>
        bool RequiresEditReason(MedicalCase medicalCase, Guid currentUserId);

        /// <summary>
        /// 获取用户对医案的权限详情
        /// </summary>
        /// <param name="userId">当前用户ID</param>
        /// <param name="role">用户角色</param>
        /// <param name="medicalCase">医案实体</param>
        /// <returns>权限详情DTO</returns>
        MedicalCasePermissionDto GetPermissions(Guid userId, UserRole role, MedicalCase medicalCase);
    }

    /// <summary>
    /// 编辑权限检查响应
    /// </summary>
    public class CanEditResponse
    {
        /// <summary>
        /// 是否可编辑
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// 不可编辑时的原因说明
        /// </summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 删除权限检查响应
    /// </summary>
    public class CanDeleteResponse
    {
        /// <summary>
        /// 是否可删除
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// 不可删除时的原因说明
        /// </summary>
        public string? Reason { get; set; }
    }
}
