namespace LYBT.Desktop.Core.Models.Consultation
{
    /// <summary>
    /// 中医四诊信息
    /// </summary>
    public class TCMDiagnosisInfo
    {
        /// <summary>
        /// 望诊 - 观察病人的神色、形态、舌象等
        /// </summary>
        public string? Inspection { get; set; }

        /// <summary>
        /// 闻诊 - 听声息、嗅气味
        /// </summary>
        public string? Auscultation { get; set; }

        /// <summary>
        /// 问诊 - 询问症状、病史、生活习惯等
        /// </summary>
        public string? Inquiry { get; set; }

        /// <summary>
        /// 切诊 - 摸脉象、按压诊查
        /// </summary>
        public string? Palpation { get; set; }

        /// <summary>
        /// 辨证 - 中医证型
        /// </summary>
        public string? Syndrome { get; set; }

        /// <summary>
        /// 治法 - 治疗原则和方法
        /// </summary>
        public string? Treatment { get; set; }

        /// <summary>
        /// 脉象描述
        /// </summary>
        public string? PulseDescription { get; set; }

        /// <summary>
        /// 舌象描述
        /// </summary>
        public string? TongueDescription { get; set; }

        /// <summary>
        /// 其他观察
        /// </summary>
        public string? OtherObservations { get; set; }
    }
}