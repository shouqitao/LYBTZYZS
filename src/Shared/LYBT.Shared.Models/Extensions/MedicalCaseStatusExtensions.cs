using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Extensions
{

    /// <summary>
    /// 医案状态扩展方法 - Record-Only模式兼容性映射
    /// </summary>
    public static class MedicalCaseStatusExtensions
    {

        /// <summary>
        /// 将旧状态映射到新的简化状态（Active/Closed）
        /// </summary>
        /// <param name="status">原始状态</param>
        /// <returns>映射后的简化状态</returns>
        public static MedicalCaseStatus ToSimplifiedStatus(this MedicalCaseStatus status)
        {
            return status switch
            {
                // 活跃状态映射
#pragma warning disable CS0618 // Type or member is obsolete
                MedicalCaseStatus.Registered => MedicalCaseStatus.Active,
                MedicalCaseStatus.InConsultation => MedicalCaseStatus.Active,
                MedicalCaseStatus.Suspended => MedicalCaseStatus.Active,

                // 关闭状态映射
                MedicalCaseStatus.Completed => MedicalCaseStatus.Closed,
                MedicalCaseStatus.Cancelled => MedicalCaseStatus.Closed,
                MedicalCaseStatus.Archived => MedicalCaseStatus.Closed,
#pragma warning restore CS0618 // Type or member is obsolete

                // 已经是简化状态的直接返回
                MedicalCaseStatus.Active => MedicalCaseStatus.Active,
                MedicalCaseStatus.Closed => MedicalCaseStatus.Closed,

                // 默认映射到活跃状态
                _ => MedicalCaseStatus.Active
            };
        }

        /// <summary>
        /// 检查状态是否为活跃状态（包含兼容映射）
        /// </summary>
        /// <param name="status">状态</param>
        /// <returns>是否为活跃状态</returns>
        public static bool IsActive(this MedicalCaseStatus status)
        {
            return status.ToSimplifiedStatus() == MedicalCaseStatus.Active;
        }

        /// <summary>
        /// 检查状态是否为关闭状态（包含兼容映射）
        /// </summary>
        /// <param name="status">状态</param>
        /// <returns>是否为关闭状态</returns>
        public static bool IsClosed(this MedicalCaseStatus status)
        {
            return status.ToSimplifiedStatus() == MedicalCaseStatus.Closed;
        }

        /// <summary>
        /// 获取状态的显示名称（兼容旧状态）
        /// </summary>
        /// <param name="status">状态</param>
        /// <returns>显示名称</returns>
        public static string GetDisplayName(this MedicalCaseStatus status)
        {
            return status switch
            {
                MedicalCaseStatus.Active => "活跃",
                MedicalCaseStatus.Closed => "已关闭",
#pragma warning disable CS0618 // Type or member is obsolete
                MedicalCaseStatus.Registered => "挂号完成（活跃）",
                MedicalCaseStatus.InConsultation => "看诊中（活跃）",
                MedicalCaseStatus.Suspended => "暂停（活跃）",
                MedicalCaseStatus.Completed => "已完成（关闭）",
                MedicalCaseStatus.Cancelled => "已取消（关闭）",
                MedicalCaseStatus.Archived => "已归档（关闭）",
#pragma warning restore CS0618 // Type or member is obsolete
                _ => "未知状态"
            };
        }
    }
}
