using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Core.Models.Prescriptions {
    /// <summary>
    /// 处方信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class PrescriptionInfo : BasePrescriptionModel {
        /// <summary>患者姓名（前端显示字段）</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名（前端显示字段）</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>处方编号（前端业务字段）</summary>
        public string PrescriptionNumber { get; set; } = string.Empty;

        /// <summary>处方项目（药材明细）</summary>
        public List<PrescriptionItemInfo> Items { get; set; } = new();

        /// <summary>是否已支付（前端业务字段）</summary>
        public bool IsPaid { get; set; }

        /// <summary>是否已发药（前端业务字段）</summary>
        public bool IsDispensed { get; set; }

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }

        /// <summary>状态名称（前端显示字段）</summary>
        public string StatusName => GetStatusName();

        private string GetStatusName() {
            return Status switch {
                PrescriptionStatus.Draft => "草稿",
                PrescriptionStatus.Completed => "已完成",
                _ => "未知状态"
            };
        }
    }
}