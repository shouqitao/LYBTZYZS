using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LYBT.Domain.Common;

namespace LYBT.Domain.Aggregates.PrescriptionAggregate.ValueObjects
{
    /// <summary>
    /// 处方编号值对象 - UltraThink重构DDD架构
    /// </summary>
    public class PrescriptionNumber : SingleValueObject<string>
    {
        private static readonly Regex ValidPrescriptionNumberRegex = new(@"^RX\d{14}\d{3}$", RegexOptions.Compiled);

        private PrescriptionNumber(string value) : base(value) { }

        public static PrescriptionNumber Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("处方编号不能为空", nameof(value));

            value = value.Trim().ToUpper();

            if (!ValidPrescriptionNumberRegex.IsMatch(value))
                throw new ArgumentException($"处方编号格式不正确: '{value}'", nameof(value));

            return new PrescriptionNumber(value);
        }

        /// <summary>
        /// 生成新的处方编号
        /// </summary>
        public static PrescriptionNumber Generate()
        {
            // 格式：RX + 年月日时分秒(14位) + 随机数(3位)
            var now = DateTime.Now;
            var random = new Random().Next(100, 999);
            var prescriptionNumber = $"RX{now:yyyyMMddHHmmss}{random}";
            return new PrescriptionNumber(prescriptionNumber);
        }
    }

    /// <summary>
    /// 处方类型枚举值对象
    /// </summary>
    public class PrescriptionType : Enumeration<PrescriptionType>
    {
        public static readonly PrescriptionType TcmDecoction = new(1, nameof(TcmDecoction), "中药汤剂", "传统中药汤剂");
        public static readonly PrescriptionType TcmPowder = new(2, nameof(TcmPowder), "中药散剂", "中药研磨成粉末");
        public static readonly PrescriptionType TcmPill = new(3, nameof(TcmPill), "中药丸剂", "中药制成丸状");
        public static readonly PrescriptionType TcmOintment = new(4, nameof(TcmOintment), "中药膏剂", "外用中药膏剂");
        public static readonly PrescriptionType Custom = new(5, nameof(Custom), "自定义", "其他自定义类型");

        public string DisplayName { get; }
        public string Description { get; }

        private PrescriptionType(int value, string name, string displayName, string description) : base(value, name)
        {
            DisplayName = displayName;
            Description = description;
        }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// 处方状态枚举值对象
    /// </summary>
    public class PrescriptionStatus : Enumeration<PrescriptionStatus>
    {
        public static readonly PrescriptionStatus Draft = new(1, nameof(Draft), "草稿", "处方草稿状态");
        public static readonly PrescriptionStatus Confirmed = new(2, nameof(Confirmed), "已确认", "处方已确认");
        public static readonly PrescriptionStatus Dispensing = new(3, nameof(Dispensing), "配药中", "正在配药");
        public static readonly PrescriptionStatus Dispensed = new(4, nameof(Dispensed), "已配药", "药品已配发");
        public static readonly PrescriptionStatus Cancelled = new(5, nameof(Cancelled), "已取消", "处方已取消");

        public string DisplayName { get; }
        public string Description { get; }

        private PrescriptionStatus(int value, string name, string displayName, string description) : base(value, name)
        {
            DisplayName = displayName;
            Description = description;
        }

        public override string ToString() => DisplayName;

        /// <summary>
        /// 检查是否可以从当前状态转换到目标状态
        /// </summary>
        public bool CanTransitionTo(PrescriptionStatus targetStatus)
        {
            return this switch
            {
                _ when Equals(Draft) => targetStatus.Equals(Confirmed) || targetStatus.Equals(Cancelled),
                _ when Equals(Confirmed) => targetStatus.Equals(Dispensing) || targetStatus.Equals(Cancelled),
                _ when Equals(Dispensing) => targetStatus.Equals(Dispensed) || targetStatus.Equals(Cancelled),
                _ when Equals(Dispensed) => false, // 已配药的处方不能再转换状态
                _ when Equals(Cancelled) => false, // 已取消的处方不能再转换状态
                _ => false
            };
        }
    }

    /// <summary>
    /// 用药方式值对象
    /// </summary>
    public class UsageInstructions : ValueObject
    {
        public string Method { get; } // 服用方法：煎服、冲服、含化等
        public string Frequency { get; } // 服用频次：一日三次、饭前服用等
        public string Duration { get; } // 疗程：7天、2周等
        public string SpecialInstructions { get; } // 特殊说明

        private UsageInstructions(string method, string frequency, string duration, string specialInstructions)
        {
            Method = method;
            Frequency = frequency;
            Duration = duration;
            SpecialInstructions = specialInstructions;
        }

        public static UsageInstructions Create(string method, string frequency, string duration = null, string specialInstructions = null)
        {
            if (string.IsNullOrWhiteSpace(method))
                throw new ArgumentException("用药方法不能为空", nameof(method));

            if (string.IsNullOrWhiteSpace(frequency))
                throw new ArgumentException("用药频次不能为空", nameof(frequency));

            return new UsageInstructions(
                method.Trim(),
                frequency.Trim(),
                duration?.Trim(),
                specialInstructions?.Trim());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Method;
            yield return Frequency;
            yield return Duration ?? "";
            yield return SpecialInstructions ?? "";
        }

        public override string ToString()
        {
            var parts = new List<string> { Method, Frequency };
            if (!string.IsNullOrEmpty(Duration)) parts.Add($"疗程: {Duration}");
            if (!string.IsNullOrEmpty(SpecialInstructions)) parts.Add($"注意: {SpecialInstructions}");
            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// 药品剂量值对象
    /// </summary>
    public class Dosage : ValueObject
    {
        public decimal Amount { get; } // 剂量数量
        public string Unit { get; } // 单位：克、毫升、片等

        private Dosage(decimal amount, string unit)
        {
            Amount = amount;
            Unit = unit;
        }

        public static Dosage Create(decimal amount, string unit)
        {
            if (amount <= 0)
                throw new ArgumentException("剂量必须大于0", nameof(amount));

            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("剂量单位不能为空", nameof(unit));

            // 常见单位验证
            var validUnits = new[] { "g", "克", "mg", "毫克", "ml", "毫升", "片", "粒", "包", "袋" };
            if (!validUnits.Contains(unit.Trim(), StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"不支持的剂量单位: '{unit}'", nameof(unit));

            return new Dosage(amount, unit.Trim());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Amount;
            yield return Unit;
        }

        public override string ToString() => $"{Amount}{Unit}";
    }

    /// <summary>
    /// 处方明细项值对象
    /// </summary>
    public class PrescriptionItem : ValueObject
    {
        public Guid HerbId { get; } // 药材ID
        public string HerbName { get; } // 药材名称
        public Dosage Dosage { get; } // 单次剂量
        public decimal Quantity { get; } // 总数量
        public decimal UnitPrice { get; } // 单价
        public string Specification { get; } // 规格说明
        public string Remarks { get; } // 备注

        private PrescriptionItem(
            Guid herbId,
            string herbName,
            Dosage dosage,
            decimal quantity,
            decimal unitPrice,
            string specification,
            string remarks)
        {
            HerbId = herbId;
            HerbName = herbName;
            Dosage = dosage;
            Quantity = quantity;
            UnitPrice = unitPrice;
            Specification = specification;
            Remarks = remarks;
        }

        public static PrescriptionItem Create(
            Guid herbId,
            string herbName,
            decimal dosageAmount,
            string dosageUnit,
            decimal quantity,
            decimal unitPrice,
            string specification = null,
            string remarks = null)
        {
            if (herbId == Guid.Empty)
                throw new ArgumentException("药材ID不能为空", nameof(herbId));

            if (string.IsNullOrWhiteSpace(herbName))
                throw new ArgumentException("药材名称不能为空", nameof(herbName));

            if (quantity <= 0)
                throw new ArgumentException("数量必须大于0", nameof(quantity));

            if (unitPrice < 0)
                throw new ArgumentException("单价不能为负数", nameof(unitPrice));

            var dosage = Dosage.Create(dosageAmount, dosageUnit);

            return new PrescriptionItem(
                herbId,
                herbName.Trim(),
                dosage,
                quantity,
                unitPrice,
                specification?.Trim(),
                remarks?.Trim());
        }

        /// <summary>
        /// 计算明细项的总金额
        /// </summary>
        public decimal GetTotalAmount() => Quantity * UnitPrice;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return HerbId;
            yield return HerbName;
            yield return Dosage;
            yield return Quantity;
            yield return UnitPrice;
            yield return Specification ?? "";
            yield return Remarks ?? "";
        }

        public override string ToString() => $"{HerbName} {Dosage} × {Quantity} = ¥{GetTotalAmount():F2}";
    }

    /// <summary>
    /// 处方总金额值对象
    /// </summary>
    public class PrescriptionAmount : SingleValueObject<decimal>
    {
        private PrescriptionAmount(decimal value) : base(value) { }

        public static PrescriptionAmount Create(decimal value)
        {
            if (value < 0)
                throw new ArgumentException("处方总金额不能为负数", nameof(value));

            return new PrescriptionAmount(Math.Round(value, 2)); // 保留两位小数
        }

        public static PrescriptionAmount Zero => new(0);

        /// <summary>
        /// 从处方明细项列表计算总金额
        /// </summary>
        public static PrescriptionAmount CalculateFrom(IEnumerable<PrescriptionItem> items)
        {
            if (items == null) return Zero;

            var totalAmount = items.Sum(item => item.GetTotalAmount());
            return Create(totalAmount);
        }

        public override string ToString() => $"¥{Value:F2}";
    }

    /// <summary>
    /// 症状诊断值对象
    /// </summary>
    public class SymptomDiagnosis : ValueObject
    {
        public string ChiefComplaint { get; } // 主诉
        public string CurrentSymptoms { get; } // 现病史
        public string TcmSyndrome { get; } // 中医证候
        public string TcmDiagnosis { get; } // 中医诊断
        public string TreatmentPrinciple { get; } // 治法

        private SymptomDiagnosis(
            string chiefComplaint,
            string currentSymptoms,
            string tcmSyndrome,
            string tcmDiagnosis,
            string treatmentPrinciple)
        {
            ChiefComplaint = chiefComplaint;
            CurrentSymptoms = currentSymptoms;
            TcmSyndrome = tcmSyndrome;
            TcmDiagnosis = tcmDiagnosis;
            TreatmentPrinciple = treatmentPrinciple;
        }

        public static SymptomDiagnosis Create(
            string chiefComplaint,
            string currentSymptoms = null,
            string tcmSyndrome = null,
            string tcmDiagnosis = null,
            string treatmentPrinciple = null)
        {
            if (string.IsNullOrWhiteSpace(chiefComplaint))
                throw new ArgumentException("主诉不能为空", nameof(chiefComplaint));

            return new SymptomDiagnosis(
                chiefComplaint.Trim(),
                currentSymptoms?.Trim(),
                tcmSyndrome?.Trim(),
                tcmDiagnosis?.Trim(),
                treatmentPrinciple?.Trim());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return ChiefComplaint;
            yield return CurrentSymptoms ?? "";
            yield return TcmSyndrome ?? "";
            yield return TcmDiagnosis ?? "";
            yield return TreatmentPrinciple ?? "";
        }

        public override string ToString()
        {
            var parts = new List<string> { $"主诉: {ChiefComplaint}" };
            if (!string.IsNullOrEmpty(TcmDiagnosis)) parts.Add($"中医诊断: {TcmDiagnosis}");
            if (!string.IsNullOrEmpty(TcmSyndrome)) parts.Add($"证候: {TcmSyndrome}");
            if (!string.IsNullOrEmpty(TreatmentPrinciple)) parts.Add($"治法: {TreatmentPrinciple}");
            return string.Join("; ", parts);
        }
    }
}