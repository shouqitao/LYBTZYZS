using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Common;
using LYBT.Domain.ValueObjects;
using LYBT.Domain.Exceptions;

namespace LYBT.Domain.Aggregates.PrescriptionAggregate
{
    /// <summary>
    /// 处方聚合根 - 中医处方核心领域模型
    /// 
    /// 职责：
    /// 1. 管理处方项和药材配伍
    /// 2. 计算剂量和费用
    /// 3. 验证处方合理性
    /// 4. 控制处方状态流转
    /// </summary>
    public class Prescription : AggregateRoot
    {
        #region 私有字段

        private Guid _patientId;
        private Guid _doctorId;
        private Guid _consultationId;
        private string _prescriptionNo;
        private PrescriptionType _type;
        private TCMSyndrome _syndrome;
        private TreatmentPrinciple _treatmentPrinciple;
        private readonly List<PrescriptionItem> _items;
        private PrescriptionStatus _status;
        private Money _totalAmount;
        private int _days;
        private int _dosesPerDay;
        private string _usage;
        private string _notes;
        private DateTime _prescribedDate;
        private DateTime? _dispensedDate;
        private DateTime? _cancelledDate;
        private string _cancellationReason;

        #endregion

        #region 属性

        public Guid PatientId => _patientId;
        public Guid DoctorId => _doctorId;
        public Guid ConsultationId => _consultationId;
        public string PrescriptionNo => _prescriptionNo;
        public PrescriptionType Type => _type;
        public TCMSyndrome Syndrome => _syndrome;
        public TreatmentPrinciple TreatmentPrinciple => _treatmentPrinciple;
        public IReadOnlyCollection<PrescriptionItem> Items => _items.AsReadOnly();
        public PrescriptionStatus Status => _status;
        public Money TotalAmount => _totalAmount;
        public int Days => _days;
        public int DosesPerDay => _dosesPerDay;
        public int TotalDoses => _days * _dosesPerDay;
        public string Usage => _usage;
        public string Notes => _notes;
        public DateTime PrescribedDate => _prescribedDate;
        public DateTime? DispensedDate => _dispensedDate;
        public DateTime? CancelledDate => _cancelledDate;
        public string CancellationReason => _cancellationReason;

        // 计算属性
        public bool CanModify => _status == PrescriptionStatus.Draft;
        public bool CanDispense => _status == PrescriptionStatus.Confirmed;
        public bool CanCancel => _status == PrescriptionStatus.Draft || _status == PrescriptionStatus.Confirmed;
        public int TotalHerbTypes => _items.Count;
        public decimal TotalWeight => _items.Sum(i => i.Dosage.Value * TotalDoses);

        #endregion

        #region 构造函数

        protected Prescription()
        {
            _items = new List<PrescriptionItem>();
        }

        public Prescription(
            Guid patientId,
            Guid doctorId,
            Guid consultationId,
            PrescriptionType type,
            TCMSyndrome syndrome,
            TreatmentPrinciple treatmentPrinciple,
            int days,
            int dosesPerDay,
            string usage) : this()
        {
            _patientId = patientId;
            _doctorId = doctorId;
            _consultationId = consultationId;
            _prescriptionNo = GeneratePrescriptionNo();
            _type = type ?? throw new PrescriptionDomainException("处方类型不能为空");
            _syndrome = syndrome ?? throw new PrescriptionDomainException("证型不能为空");
            _treatmentPrinciple = treatmentPrinciple ?? throw new PrescriptionDomainException("治法不能为空");
            
            SetDaysAndDoses(days, dosesPerDay);
            SetUsage(usage);
            
            _status = PrescriptionStatus.Draft;
            _prescribedDate = DateTime.Now;
            _totalAmount = Money.Zero;
        }

        #endregion

        #region 处方项管理

        /// <summary>
        /// 添加处方项（药材）
        /// </summary>
        public void AddItem(
            Guid herbId,
            string herbName,
            Dosage dosage,
            HerbRole role,
            ProcessingMethod processingMethod = null,
            string specialInstructions = null)
        {
            if (!CanModify)
                throw new PrescriptionDomainException($"处方状态为{_status}，不能修改");

            // 检查是否已存在
            if (_items.Any(i => i.HerbId == herbId))
                throw new PrescriptionDomainException($"药材{herbName}已存在于处方中");

            // 验证配伍禁忌
            ValidateHerbCombination(herbId, herbName);

            // 验证剂量合理性
            ValidateDosage(herbName, dosage);

            var item = new PrescriptionItem(
                Guid.NewGuid(),
                herbId,
                herbName,
                dosage,
                role,
                processingMethod,
                specialInstructions,
                _items.Count + 1);

            _items.Add(item);
            RecalculateTotalAmount();
        }

        /// <summary>
        /// 移除处方项
        /// </summary>
        public void RemoveItem(Guid itemId)
        {
            if (!CanModify)
                throw new PrescriptionDomainException($"处方状态为{_status}，不能修改");

            var item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                throw new PrescriptionDomainException($"处方项{itemId}不存在");

            _items.Remove(item);
            
            // 重新排序
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].UpdateSequence(i + 1);
            }

            RecalculateTotalAmount();
        }

        /// <summary>
        /// 更新处方项剂量
        /// </summary>
        public void UpdateItemDosage(Guid itemId, Dosage newDosage)
        {
            if (!CanModify)
                throw new PrescriptionDomainException($"处方状态为{_status}，不能修改");

            var item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                throw new PrescriptionDomainException($"处方项{itemId}不存在");

            ValidateDosage(item.HerbName, newDosage);
            item.UpdateDosage(newDosage);
            RecalculateTotalAmount();
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 确认处方
        /// </summary>
        public void Confirm()
        {
            if (_status != PrescriptionStatus.Draft)
                throw new PrescriptionDomainException($"只有草稿状态的处方才能确认");

            if (!_items.Any())
                throw new PrescriptionDomainException("处方至少需要包含一味药材");

            // 验证处方完整性
            ValidatePrescriptionCompleteness();

            _status = PrescriptionStatus.Confirmed;
        }

        /// <summary>
        /// 发药
        /// </summary>
        public void Dispense(string dispenserId)
        {
            if (!CanDispense)
                throw new PrescriptionDomainException($"处方状态为{_status}，不能发药");

            _status = PrescriptionStatus.Dispensed;
            _dispensedDate = DateTime.Now;
        }

        /// <summary>
        /// 完成
        /// </summary>
        public void Complete()
        {
            if (_status != PrescriptionStatus.Dispensed)
                throw new PrescriptionDomainException($"只有已发药的处方才能完成");

            _status = PrescriptionStatus.Completed;
        }

        /// <summary>
        /// 取消处方
        /// </summary>
        public void Cancel(string reason)
        {
            if (!CanCancel)
                throw new PrescriptionDomainException($"处方状态为{_status}，不能取消");

            if (string.IsNullOrWhiteSpace(reason))
                throw new PrescriptionDomainException("取消原因不能为空");

            _status = PrescriptionStatus.Cancelled;
            _cancelledDate = DateTime.Now;
            _cancellationReason = reason;
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 更新用法用量
        /// </summary>
        public void UpdateUsageAndDosage(int days, int dosesPerDay, string usage)
        {
            if (!CanModify)
                throw new PrescriptionDomainException($"处方状态为{_status}，不能修改");

            SetDaysAndDoses(days, dosesPerDay);
            SetUsage(usage);
            RecalculateTotalAmount();
        }

        /// <summary>
        /// 添加备注
        /// </summary>
        public void AddNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return;

            _notes = string.IsNullOrWhiteSpace(_notes) 
                ? notes 
                : $"{_notes}\n{notes}";
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证药材配伍
        /// </summary>
        private void ValidateHerbCombination(Guid herbId, string herbName)
        {
            // 十八反检查
            var incompatiblePairs = new Dictionary<string, string[]>
            {
                { "甘草", new[] { "甘遂", "大戟", "海藻", "芫花" } },
                { "乌头", new[] { "贝母", "瓜蒌", "半夏", "白蔹", "白及" } },
                { "藜芦", new[] { "人参", "沙参", "丹参", "玄参", "苦参", "细辛", "芍药" } }
            };

            foreach (var item in _items)
            {
                foreach (var pair in incompatiblePairs)
                {
                    if (item.HerbName.Contains(pair.Key) && pair.Value.Any(h => herbName.Contains(h)) ||
                        herbName.Contains(pair.Key) && pair.Value.Any(h => item.HerbName.Contains(h)))
                    {
                        throw new PrescriptionDomainException($"{herbName}与{item.HerbName}存在配伍禁忌（十八反）");
                    }
                }
            }
        }

        /// <summary>
        /// 验证剂量合理性
        /// </summary>
        private void ValidateDosage(string herbName, Dosage dosage)
        {
            // 常用药材剂量范围（示例）
            var dosageRanges = new Dictionary<string, (decimal min, decimal max)>
            {
                { "麻黄", (3, 10) },
                { "桂枝", (3, 15) },
                { "甘草", (2, 10) },
                { "人参", (3, 30) },
                { "附子", (3, 15) },
                { "细辛", (1, 3) },
                { "大黄", (3, 15) }
            };

            foreach (var range in dosageRanges)
            {
                if (herbName.Contains(range.Key))
                {
                    if (dosage.Value < range.Value.min || dosage.Value > range.Value.max)
                    {
                        throw new PrescriptionDomainException(
                            $"{herbName}剂量{dosage.Value}{dosage.Unit}超出常规范围({range.Value.min}-{range.Value.max}g)");
                    }
                }
            }
        }

        /// <summary>
        /// 验证处方完整性
        /// </summary>
        private void ValidatePrescriptionCompleteness()
        {
            // 必须有君药
            if (!_items.Any(i => i.Role == HerbRole.Monarch))
                throw new PrescriptionDomainException("处方缺少君药");

            // 处方药味数合理性
            if (_items.Count < 2)
                throw new PrescriptionDomainException("处方药味数过少");

            if (_items.Count > 30)
                throw new PrescriptionDomainException("处方药味数过多（超过30味）");

            // 总剂量合理性
            var totalDosagePerDose = _items.Sum(i => i.Dosage.Value);
            if (totalDosagePerDose > 500)
                throw new PrescriptionDomainException("单剂总量过大（超过500g）");
        }

        #endregion

        #region 私有方法

        private string GeneratePrescriptionNo()
        {
            return $"RX{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";
        }

        private void SetDaysAndDoses(int days, int dosesPerDay)
        {
            if (days <= 0 || days > 30)
                throw new PrescriptionDomainException("处方天数必须在1-30天之间");

            if (dosesPerDay <= 0 || dosesPerDay > 3)
                throw new PrescriptionDomainException("每日剂数必须在1-3剂之间");

            _days = days;
            _dosesPerDay = dosesPerDay;
        }

        private void SetUsage(string usage)
        {
            if (string.IsNullOrWhiteSpace(usage))
                usage = "水煎服，每日一剂，分两次温服";

            _usage = usage;
        }

        private void RecalculateTotalAmount()
        {
            if (!_items.Any())
            {
                _totalAmount = Money.Zero;
                return;
            }

            // 计算总金额（示例算法）
            decimal totalPrice = 0;
            foreach (var item in _items)
            {
                // 假设每克单价（实际应从药材信息获取）
                decimal pricePerGram = GetHerbPrice(item.HerbId);
                totalPrice += item.Dosage.Value * TotalDoses * pricePerGram;
            }

            _totalAmount = new Money(totalPrice, "CNY");
        }

        private decimal GetHerbPrice(Guid herbId)
        {
            // 实际应从药材服务获取价格
            // 这里返回示例价格
            return 0.5m;
        }

        #endregion
    }

    #region 实体

    /// <summary>
    /// 处方项实体
    /// </summary>
    public class PrescriptionItem : Entity
    {
        public Guid HerbId { get; private set; }
        public string HerbName { get; private set; }
        public Dosage Dosage { get; private set; }
        public HerbRole Role { get; private set; }
        public ProcessingMethod ProcessingMethod { get; private set; }
        public string SpecialInstructions { get; private set; }
        public int Sequence { get; private set; }

        protected PrescriptionItem() { }

        public PrescriptionItem(
            Guid id,
            Guid herbId,
            string herbName,
            Dosage dosage,
            HerbRole role,
            ProcessingMethod processingMethod,
            string specialInstructions,
            int sequence)
        {
            Id = id;
            HerbId = herbId;
            HerbName = herbName;
            Dosage = dosage;
            Role = role;
            ProcessingMethod = processingMethod;
            SpecialInstructions = specialInstructions;
            Sequence = sequence;
        }

        public void UpdateDosage(Dosage newDosage)
        {
            Dosage = newDosage;
        }

        public void UpdateSequence(int newSequence)
        {
            Sequence = newSequence;
        }
    }

    #endregion

    #region 值对象

    /// <summary>
    /// 处方类型
    /// </summary>
    public class PrescriptionType : Enumeration
    {
        public static PrescriptionType Decoction = new(1, "汤剂");
        public static PrescriptionType Powder = new(2, "散剂");
        public static PrescriptionType Pill = new(3, "丸剂");
        public static PrescriptionType Ointment = new(4, "膏剂");
        public static PrescriptionType Tincture = new(5, "酊剂");
        public static PrescriptionType Granule = new(6, "颗粒剂");

        public PrescriptionType(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 处方状态
    /// </summary>
    public class PrescriptionStatus : Enumeration
    {
        public static PrescriptionStatus Draft = new(1, "草稿");
        public static PrescriptionStatus Confirmed = new(2, "已确认");
        public static PrescriptionStatus Dispensed = new(3, "已发药");
        public static PrescriptionStatus Completed = new(4, "已完成");
        public static PrescriptionStatus Cancelled = new(5, "已取消");

        public PrescriptionStatus(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 药材角色
    /// </summary>
    public class HerbRole : Enumeration
    {
        public static HerbRole Monarch = new(1, "君药");
        public static HerbRole Minister = new(2, "臣药");
        public static HerbRole Assistant = new(3, "佐药");
        public static HerbRole Guide = new(4, "使药");

        public HerbRole(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 炮制方法
    /// </summary>
    public class ProcessingMethod : Enumeration
    {
        public static ProcessingMethod Raw = new(1, "生");
        public static ProcessingMethod Fried = new(2, "炒");
        public static ProcessingMethod HoneyFried = new(3, "蜜炙");
        public static ProcessingMethod WineFried = new(4, "酒炙");
        public static ProcessingMethod SaltFried = new(5, "盐炙");
        public static ProcessingMethod Charred = new(6, "炭");
        public static ProcessingMethod Steamed = new(7, "蒸");
        public static ProcessingMethod Boiled = new(8, "煮");

        public ProcessingMethod(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 中医证型
    /// </summary>
    public class TCMSyndrome : Enumeration
    {
        public static TCMSyndrome WindColdExterior = new(1, "风寒表证");
        public static TCMSyndrome WindHeatExterior = new(2, "风热表证");
        public static TCMSyndrome DampHeat = new(3, "湿热证");
        public static TCMSyndrome QiDeficiency = new(4, "气虚证");
        public static TCMSyndrome BloodDeficiency = new(5, "血虚证");
        public static TCMSyndrome YinDeficiency = new(6, "阴虚证");
        public static TCMSyndrome YangDeficiency = new(7, "阳虚证");
        public static TCMSyndrome QiStagnation = new(8, "气滞证");
        public static TCMSyndrome BloodStasis = new(9, "血瘀证");
        public static TCMSyndrome PhlegmDampness = new(10, "痰湿证");

        public TCMSyndrome(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 治法
    /// </summary>
    public class TreatmentPrinciple : Enumeration
    {
        public static TreatmentPrinciple DispelWindCold = new(1, "疏风散寒");
        public static TreatmentPrinciple ClearHeat = new(2, "清热解毒");
        public static TreatmentPrinciple TonifyQi = new(3, "补气");
        public static TreatmentPrinciple TonifyBlood = new(4, "补血");
        public static TreatmentPrinciple NourishYin = new(5, "滋阴");
        public static TreatmentPrinciple WarmYang = new(6, "温阳");
        public static TreatmentPrinciple RegulateQi = new(7, "理气");
        public static TreatmentPrinciple ActivateBlood = new(8, "活血化瘀");
        public static TreatmentPrinciple ResolveDampness = new(9, "化湿");
        public static TreatmentPrinciple ResolvePhlegm = new(10, "化痰");

        public TreatmentPrinciple(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 剂量值对象
    /// </summary>
    public class Dosage : ValueObject
    {
        public decimal Value { get; private set; }
        public string Unit { get; private set; }

        protected Dosage() { }

        public Dosage(decimal value, string unit = "g")
        {
            if (value <= 0)
                throw new PrescriptionDomainException("剂量必须大于0");

            if (value > 1000)
                throw new PrescriptionDomainException("剂量不能超过1000g");

            Value = value;
            Unit = unit;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
            yield return Unit;
        }

        public override string ToString()
        {
            return $"{Value}{Unit}";
        }
    }

    #endregion
}