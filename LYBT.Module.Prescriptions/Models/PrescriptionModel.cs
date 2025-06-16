using LYBT.Module.Prescriptions.Enums;

namespace LYBT.Module.Prescriptions.Models {

    /// <summary>
    /// 处方数据模型
    /// </summary>
    public class PrescriptionModel {
        public string PrescriptionId { get; set; }        // 处方ID
        public string PatientId { get; set; }             // 病人ID
        public string DoctorId { get; set; }              // 医生ID
        public DateTime CreateTime { get; set; }          // 开方时间
        public string Diagnosis { get; set; }             // 诊断信息
        public string Remark { get; set; }                // 备注
        public PrescriptionStatus Status { get; set; }    // 处方状态

        // 药材列表
        public List<PrescriptionHerbModel> Herbs { get; set; }
    }
}