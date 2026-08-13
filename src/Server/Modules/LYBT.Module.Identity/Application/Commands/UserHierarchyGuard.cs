using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Identity.Application.Commands;

/// <summary>
/// 用户层级管理校验（UPDATEUSER-HIERARCHY-FIX 2026-08-13: Update/Delete/ToggleStatus/BatchDelete
/// 原用 bool IsAdmin 粗粒度——Admin 可管理 Admin 违反 USER-D05 一级管一级；统一抽离对齐 Create/Restore）：
/// - sysadmin（SuperAdmin）→ 可管理 Admin 及以下
/// - Admin → 仅可管理 Doctor/Receptionist
/// - 不可自管（改自己角色/删/禁自己）
/// - 目标角色越级（Admin 想把 Doctor 升 Admin）→ 拒绝
/// - sysadmin 账号不可被任何操作（除 sysadmin 本人不可自管）
/// </summary>
internal static class UserHierarchyGuard
{
    /// <summary>
    /// 校验操作者是否有权管理目标用户（返回 null = 允许，否则返回失败 Result）。
    /// </summary>
    public static Result<T>? Validate<T>(
        Guid operatorId,
        Guid targetUserId,
        UserRole operatorRole,
        bool targetIsSysAdmin,
        UserRole targetRole,
        string selfOperationMessage = "不能操作自己的账号"
    )
    {
        // 操作者必须是 Admin/SuperAdmin（对齐 CreateUser USER-D04）
        if (operatorRole is not (UserRole.Admin or UserRole.SuperAdmin))
            return Result<T>.Failure(ErrorCode.Unauthorized, "无权管理用户");

        // 不可自管
        if (operatorId == targetUserId)
            return Result<T>.Failure(ErrorCode.Forbidden, selfOperationMessage);

        // sysadmin 账号不可被管理
        if (targetIsSysAdmin)
            return Result<T>.Failure(ErrorCode.Forbidden, "系统管理员账号不可被操作");

        // 一级管一级：Admin 仅可管理 Doctor/Receptionist
        if (
            operatorRole == UserRole.Admin
            && targetRole != UserRole.Doctor
            && targetRole != UserRole.Receptionist
        )
            return Result<T>.Failure(ErrorCode.Forbidden, "仅超级管理员可管理管理员账号");

        return null;
    }
}
