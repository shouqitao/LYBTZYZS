using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Models.Prescriptions
{
    /// <summary>
    /// 处方模板
    /// 用于保存常用处方配置，提高开方效率
    /// </summary>
    public class PrescriptionTemplate : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private string _name = string.Empty;
        private string _category = string.Empty;
        private string _diagnosis = string.Empty;
        private string _syndrome = string.Empty;
        private string _treatmentPrinciple = string.Empty;
        private string _usage = string.Empty;
        private int _dosageCount = 7;
        private string _remark = string.Empty;
        private List<PrescriptionTemplateItem> _items = new();
        private bool _isPublic = false;
        private bool _isActive = true;
        private Guid _creatorId = Guid.Empty;
        private string _creatorName = string.Empty;
        private DateTime _createTime = DateTime.Now;
        private DateTime? _updateTime;
        private int _usageCount = 0;

        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 模板分类（如：感冒类、脾胃类、妇科类等）
        /// </summary>
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        /// <summary>
        /// 适用诊断
        /// </summary>
        public string Diagnosis
        {
            get => _diagnosis;
            set => SetProperty(ref _diagnosis, value);
        }

        /// <summary>
        /// 适用证型
        /// </summary>
        public string Syndrome
        {
            get => _syndrome;
            set => SetProperty(ref _syndrome, value);
        }

        /// <summary>
        /// 治则治法
        /// </summary>
        public string TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set => SetProperty(ref _treatmentPrinciple, value);
        }

        /// <summary>
        /// 用法用量
        /// </summary>
        public string Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        /// <summary>
        /// 默认剂数
        /// </summary>
        public int DosageCount
        {
            get => _dosageCount;
            set => SetProperty(ref _dosageCount, value);
        }

        /// <summary>
        /// 备注说明
        /// </summary>
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        /// <summary>
        /// 模板药材项目
        /// </summary>
        public List<PrescriptionTemplateItem> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        /// <summary>
        /// 是否公开（全院共享）
        /// </summary>
        public bool IsPublic
        {
            get => _isPublic;
            set => SetProperty(ref _isPublic, value);
        }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// 创建人ID
        /// </summary>
        public Guid CreatorId
        {
            get => _creatorId;
            set => SetProperty(ref _creatorId, value);
        }

        /// <summary>
        /// 创建人姓名
        /// </summary>
        public string CreatorName
        {
            get => _creatorName;
            set => SetProperty(ref _creatorName, value);
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime
        {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime
        {
            get => _updateTime;
            set => SetProperty(ref _updateTime, value);
        }

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount
        {
            get => _usageCount;
            set => SetProperty(ref _usageCount, value);
        }

        /// <summary>
        /// 药材数量
        /// </summary>
        public int HerbCount => Items?.Count ?? 0;

        /// <summary>
        /// 总价预估
        /// </summary>
        public decimal EstimatedTotalPrice => CalculateTotalPrice();

        /// <summary>
        /// 显示名称（包含分类）
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(Category) ? Name : $"[{Category}] {Name}";

        /// <summary>
        /// 是否为个人模板
        /// </summary>
        public bool IsPersonal => !IsPublic;

        /// <summary>
        /// 计算总价
        /// </summary>
        private decimal CalculateTotalPrice()
        {
            decimal total = 0;
            if (Items != null)
            {
                foreach (var item in Items)
                {
                    total += item.Quantity * item.EstimatedPrice;
                }
            }
            return total * DosageCount;
        }

        /// <summary>
        /// 应用模板到处方
        /// </summary>
        public PrescriptionDto ApplyToNewPrescription(Guid patientId)
        {
            var prescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Indication = Diagnosis,
                DosageCount = DosageCount,
                Advice = Usage,
                Remark = $"应用模板：{Name}\n{Remark}",
                Status = CommonStatus.Disabled, // 草稿状态
                Items = new List<PrescriptionItemDto>()
            };

            // 复制药材项目
            foreach (var templateItem in Items)
            {
                prescription.Items.Add(new PrescriptionItemDto
                {
                    HerbId = templateItem.HerbId,
                    HerbName = templateItem.HerbName,
                    Quantity = templateItem.Quantity,
                    Unit = templateItem.Unit
                    // Price = templateItem.EstimatedPrice, // 属性不存在：PrescriptionItemDto.Price
                    // Subtotal = templateItem.Quantity * templateItem.EstimatedPrice
                });
            }

            // 增加使用次数
            UsageCount++;

            return prescription;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null!)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 处方模板项目
    /// </summary>
    public class PrescriptionTemplateItem : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private Guid _templateId = Guid.Empty;
        private Guid _herbId = Guid.Empty;
        private string _herbName = string.Empty;
        private decimal _quantity = 0m;
        private string _unit = "g";
        private decimal _estimatedPrice = 0m;
        private string _processMethod = string.Empty;
        private string _remark = string.Empty;
        private int _sortOrder = 0;

        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid TemplateId
        {
            get => _templateId;
            set => SetProperty(ref _templateId, value);
        }

        /// <summary>
        /// 药材ID
        /// </summary>
        public Guid HerbId
        {
            get => _herbId;
            set => SetProperty(ref _herbId, value);
        }

        /// <summary>
        /// 药材名称
        /// </summary>
        public string HerbName
        {
            get => _herbName;
            set => SetProperty(ref _herbName, value);
        }

        /// <summary>
        /// 数量
        /// </summary>
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(Subtotal));
                }
            }
        }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        /// <summary>
        /// 预估单价
        /// </summary>
        public decimal EstimatedPrice
        {
            get => _estimatedPrice;
            set
            {
                if (SetProperty(ref _estimatedPrice, value))
                {
                    OnPropertyChanged(nameof(Subtotal));
                }
            }
        }

        /// <summary>
        /// 炮制方法（如：炒、炙、煅等）
        /// </summary>
        public string ProcessMethod
        {
            get => _processMethod;
            set => SetProperty(ref _processMethod, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder
        {
            get => _sortOrder;
            set => SetProperty(ref _sortOrder, value);
        }

        /// <summary>
        /// 小计
        /// </summary>
        public decimal Subtotal => Quantity * EstimatedPrice;

        /// <summary>
        /// 显示文本
        /// </summary>
        public string DisplayText
        {
            get
            {
                var text = $"{HerbName} {Quantity}{Unit}";
                if (!string.IsNullOrEmpty(ProcessMethod))
                {
                    text = $"{HerbName}({ProcessMethod}) {Quantity}{Unit}";
                }
                return text;
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null!)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 模板分类
    /// </summary>
    public static class TemplateCategories
    {
        public static readonly string[] DefaultCategories = new[]
        {
            "感冒类",
            "咳嗽类",
            "脾胃类",
            "肝胆类",
            "心脑类",
            "肾系类",
            "妇科类",
            "儿科类",
            "皮肤类",
            "骨伤类",
            "肿瘤类",
            "其他"
        };
    }
}
