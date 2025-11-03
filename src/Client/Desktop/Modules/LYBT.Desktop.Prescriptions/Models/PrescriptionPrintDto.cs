namespace LYBT.Desktop.Prescriptions.Models
{
    /// <summary>
    /// 处方打印数据模型
    /// Issue #1379: [PRINT-2] 实现标准处方模板
    /// </summary>
    public class PrescriptionPrintDto
    {
        // 诊所信息
        public string ClinicName { get; set; } = "中医门诊";
        public string? ClinicAddress { get; set; }
        public string? ClinicPhone { get; set; }

        // 患者信息
        public string PatientName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime ConsultationDate { get; set; } = DateTime.Now;

        // 四诊信息
        public string? Inspection { get; set; } // 望诊
        public string? AuscultationOlfaction { get; set; } // 闻诊
        public string? Inquiry { get; set; } // 问诊
        public string? Palpation { get; set; } // 切诊
        public string? TCMDiagnosis { get; set; } // 中医诊断
        public string? TreatmentPrinciple { get; set; } // 治疗原则

        // 处方内容
        public List<PrescriptionItemPrintDto> Items { get; set; } = new();
        public int DosageCount { get; set; } = 7; // 剂数
        public string Usage { get; set; } = "水煎服，日一剂，分早晚服"; // 用法

        // 费用信息
        public decimal SingleDosePrice { get; set; } // 单剂价格
        public decimal TotalPrice { get; set; } // 总价

        // 医生信息
        public string DoctorName { get; set; } = string.Empty;
        public DateTime PrescriptionDate { get; set; } = DateTime.Now;

        // 可选信息
        public string? PrescriptionNumber { get; set; } // 处方编号
        public string? Advice { get; set; } // 医嘱
        public string? FormulaSource { get; set; } // 验方来源
    }

    /// <summary>
    /// 处方药材打印数据模型
    /// </summary>
    public class PrescriptionItemPrintDto
    {
        public int SequenceNumber { get; set; } // 序号
        public string HerbName { get; set; } = string.Empty; // 药材名
        public decimal Quantity { get; set; } // 剂量
        public string Unit { get; set; } = "g"; // 单位
    }
}
