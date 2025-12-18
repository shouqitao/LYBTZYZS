using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 医案权限详情DTO
    /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-007)
    /// 用于前后端传递用户对医案的权限信息
    /// </summary>
    public class MedicalCasePermissionDto
    {
        /// <summary>是否可编辑</summary>
        [DisplayName("可编辑")]
        public bool CanEdit { get; set; }

        /// <summary>是否可删除</summary>
        [DisplayName("可删除")]
        public bool CanDelete { get; set; }

        /// <summary>是否需要修改原因（编辑已完成医案时需要）</summary>
        [DisplayName("需要修改原因")]
        public bool RequiresEditReason { get; set; }

        /// <summary>是否只读模式</summary>
        [DisplayName("只读")]
        public bool IsReadOnly => !CanEdit;

        /// <summary>权限拒绝原因（如果无权限）</summary>
        [DisplayName("拒绝原因")]
        public string? DenialReason { get; set; }
    }
}
