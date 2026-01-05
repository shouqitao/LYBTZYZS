using System.ComponentModel;
using System.Reflection;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Printing.Models
{
    /// <summary>
    /// 处方打印数据模型 - 基于普通处方模板
    /// OpenSpec: create-printing-module (从MedicalCase迁移)
    /// OpenSpec: print-prescription-slip
    /// </summary>
    public class PrescriptionPrintModel
    {
        // ===== 诊所信息 =====
        /// <summary>诊所名称（标题）</summary>
        public string ClinicName { get; set; } = "中医门诊";
        /// <summary>科别</summary>
        public string Department { get; set; } = "中医科";

        // ===== 患者信息 =====
        /// <summary>姓名</summary>
        public string PatientName { get; set; } = string.Empty;
        /// <summary>性别</summary>
        public string Gender { get; set; } = string.Empty;
        /// <summary>年龄</summary>
        public int Age { get; set; }
        /// <summary>就诊时间</summary>
        public DateTime ConsultationDate { get; set; } = DateTime.Now;
        /// <summary>门诊号</summary>
        public string? OutpatientNumber { get; set; }
        /// <summary>患者电话</summary>
        public string? PatientPhone { get; set; }
        /// <summary>住址</summary>
        public string? PatientAddress { get; set; }

        // ===== 诊断信息 =====
        /// <summary>诊断（中医诊断）</summary>
        public string? TcmDiagnosis { get; set; }
        /// <summary>诊见（症状描述）</summary>
        public string? Symptoms { get; set; }

        // ===== 诊断核心字段 =====
        /// <summary>现病史</summary>
        public string? PresentIllness { get; set; }
        /// <summary>舌诊</summary>
        public string? TongueDiagnosis { get; set; }
        /// <summary>脉诊</summary>
        public string? PulseDiagnosis { get; set; }

        // ===== 处方内容 =====
        /// <summary>药材列表</summary>
        public List<PrescriptionItemPrintModel> Items { get; set; } = new();
        /// <summary>剂数</summary>
        public int DosageCount { get; set; } = 7;
        /// <summary>用法</summary>
        public string Usage { get; set; } = "水煎服，日1剂，1日2次";

        // ===== 费用信息 =====
        /// <summary>诊疗费</summary>
        public decimal ConsultationFee { get; set; }
        /// <summary>药费（单剂价格 x 剂数）</summary>
        public decimal MedicineFee { get; set; }
        /// <summary>治疗费</summary>
        public decimal TreatmentFee { get; set; }
        /// <summary>单剂价格</summary>
        public decimal SingleDosePrice { get; set; }
        /// <summary>总价（合计）</summary>
        public decimal TotalPrice { get; set; }

        // ===== 签名区 =====
        /// <summary>医师（开方医生）</summary>
        public string DoctorName { get; set; } = string.Empty;
        /// <summary>处方日期</summary>
        public DateTime PrescriptionDate { get; set; } = DateTime.Now;
        /// <summary>审核人</summary>
        public string? Reviewer { get; set; }
        /// <summary>调配人</summary>
        public string? Dispenser { get; set; }

        // ===== 可选信息 =====
        /// <summary>处方编号</summary>
        public string? PrescriptionNumber { get; set; }
        /// <summary>医嘱</summary>
        public string? Advice { get; set; }
        /// <summary>验方来源</summary>
        public string? FormulaSource { get; set; }

        // ===== 诊所地址/电话 =====
        public string? ClinicAddress { get; set; }
        public string? ClinicPhone { get; set; }
    }

    /// <summary>
    /// 处方药材打印数据模型
    /// OpenSpec: create-printing-module (从MedicalCase迁移)
    /// </summary>
    public class PrescriptionItemPrintModel
    {
        /// <summary>序号</summary>
        public int SequenceNumber { get; set; }
        /// <summary>药材名</summary>
        public string HerbName { get; set; } = string.Empty;
        /// <summary>剂量</summary>
        public decimal Dosage { get; set; }
        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;
        /// <summary>煎法</summary>
        public DecocteMethod DecocteMethod { get; set; } = DecocteMethod.Default;

        /// <summary>
        /// 打印显示文本 - 格式: "药材名 剂量单位(煎法)"
        /// 默认煎法不显示括号标注
        /// </summary>
        public string DisplayText
        {
            get
            {
                var baseText = $"{HerbName}{Dosage:0.##}{Unit}";
                if (DecocteMethod == DecocteMethod.Default)
                {
                    return baseText;
                }
                else
                {
                    var decocteMethodText = GetDecocteMethodDescription(DecocteMethod);
                    return $"{baseText}({decocteMethodText})";
                }
            }
        }

        /// <summary>
        /// 获取煎法的中文描述
        /// </summary>
        private static string GetDecocteMethodDescription(DecocteMethod method)
        {
            var field = method.GetType().GetField(method.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? method.ToString();
        }
    }
}
