using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医疗案例规则 - 集中管理核心业务逻辑
    /// 简化版本，只保留最核心的业务规则
    /// </summary>
    public static class MedicalCaseRules
    {
        /// <summary>
        /// 核心规则1：患者同时只能有一个进行中或暂存的医案
        /// Issue #xxxx: 增加Draft（暂存/挂起）状态检查
        /// </summary>
        /// <param name="existingCases">患者现有的医案列表</param>
        /// <returns>是否可以创建新医案</returns>
        public static bool CanCreateNewCase(IEnumerable<MedicalCase> existingCases)
        {
            return !existingCases.Any(c => c.CaseStatus == MedicalCaseStatus.Active ||
                                            c.CaseStatus == MedicalCaseStatus.Draft);
        }

        /// <summary>
        /// 检查是否有Active状态的医案
        /// </summary>
        public static bool HasActiveCase(IEnumerable<MedicalCase> existingCases)
        {
            return existingCases.Any(c => c.CaseStatus == MedicalCaseStatus.Active);
        }

        /// <summary>
        /// 检查是否有Draft（暂存/挂起）状态的医案
        /// </summary>
        public static bool HasDraftCase(IEnumerable<MedicalCase> existingCases)
        {
            return existingCases.Any(c => c.CaseStatus == MedicalCaseStatus.Draft);
        }

        /// <summary>
        /// 核心规则2：基于状态的编辑权限
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-007)
        ///
        /// 权限规则:
        /// - 管理员(isAdmin=true)可以编辑所有医案
        /// - 医生只能编辑自己创建的医案
        /// - Draft/Active状态：不受跨日限制，随时可编辑
        /// - Completed状态：当天可编辑，跨日后锁定
        /// - Cancelled状态：不可编辑
        /// </summary>
        /// <param name="medicalCase">医案实体</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <returns>是否可以编辑</returns>
        public static bool CanEdit(MedicalCase medicalCase, Guid currentUserId, bool isAdmin = false)
        {
            // 管理员权限 - 可以编辑所有医案
            if (isAdmin) return true;

            // 非创建者无权编辑
            if (medicalCase.UserId != currentUserId) return false;

            // Draft/Active 状态：随时可编辑，不受跨日限制
            if (medicalCase.IsActive) return true;

            // Completed 状态：当天可编辑，跨日后锁定
            if (medicalCase.IsCompleted)
            {
                var completionDate = (medicalCase.CompletedAt ?? medicalCase.CreatedAt).Date;
                return completionDate == DateTime.Today;
            }

            // Cancelled 状态：不可编辑
            return false;
        }

        /// <summary>
        /// 核心规则3：删除权限检查
        /// </summary>
        /// <param name="medicalCase">医案实体</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <returns>是否可以删除</returns>
        public static bool CanDelete(MedicalCase medicalCase, Guid currentUserId, bool isAdmin = false)
        {
            // 删除规则与编辑相同：当天创建的可以删除
            return CanEdit(medicalCase, currentUserId, isAdmin);
        }

        /// <summary>
        /// 核心规则4：完成医案的前置条件
        /// </summary>
        /// <param name="medicalCase">医案实体</param>
        /// <returns>是否可以完成</returns>
        public static bool CanComplete(MedicalCase medicalCase)
        {
            // 简化逻辑：只有进行中的医案可以完成
            return medicalCase.CaseStatus == MedicalCaseStatus.Active;
        }

        /// <summary>
        /// 核心规则5：检查是否为当天本人创建的医案
        /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-011)
        /// 用于判断操作是否需要审计理由
        /// </summary>
        /// <param name="medicalCase">医案实体</param>
        /// <param name="operatorId">操作者ID</param>
        /// <returns>是否为当天本人创建</returns>
        public static bool IsSameDayByCreator(MedicalCase medicalCase, Guid operatorId)
        {
            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
            // 检查是否为创建者
            if (medicalCase.UserId != operatorId) return false;

            // 检查是否为当天创建
            var today = DateTime.Today;
            return medicalCase.CreatedAt.Date == today;
        }

        /// <summary>
        /// 业务规则验证结果
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;

            public static ValidationResult Success() => new() { IsValid = true };
            public static ValidationResult Failure(string message) => new() { IsValid = false, ErrorMessage = message };
        }

        /// <summary>
        /// 综合验证：创建新医案前的所有检查
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="existingCases">患者现有医案</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateNewCaseCreation(Guid patientId, IEnumerable<MedicalCase> existingCases)
        {
            if (!CanCreateNewCase(existingCases))
            {
                return ValidationResult.Failure("该患者已有进行中的医案，请先完成现有医案");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// 综合验证：更新医案前的所有检查
        /// </summary>
        /// <param name="medicalCase">医案实体</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateCaseUpdate(MedicalCase medicalCase, Guid currentUserId, bool isAdmin = false)
        {
            if (!CanEdit(medicalCase, currentUserId, isAdmin))
            {
                if (medicalCase.IsLocked)
                {
                    return ValidationResult.Failure("医案已锁定，无法修改");
                }
                else
                {
                    return ValidationResult.Failure("无权限修改此医案");
                }
            }

            return ValidationResult.Success();
        }
    }
}
