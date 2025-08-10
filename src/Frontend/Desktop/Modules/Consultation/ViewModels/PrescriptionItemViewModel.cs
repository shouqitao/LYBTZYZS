using System;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Models.Prescriptions;

namespace LYBT.WPF.Client.Modules.Consultation.ViewModels
{
    /// <summary>
    /// 处方项视图模型
    /// 封装单个处方药材项的显示和编辑逻辑
    /// </summary>
    public class PrescriptionItemViewModel : BindableBase
    {
        #region 私有字段

        private readonly PrescriptionItem _item;

        #endregion

        #region 构造函数

        public PrescriptionItemViewModel(PrescriptionItem item)
        {
            _item = item ?? new PrescriptionItem();
        }
        
        public PrescriptionItemViewModel() : this(new PrescriptionItem())
        {
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 药材ID
        /// </summary>
        public Guid HerbId 
        { 
            get => _item.HerbId; 
            set { _item.HerbId = value; RaisePropertyChanged(); }
        }
        
        /// <summary>
        /// 药材名称
        /// </summary>
        public string HerbName 
        { 
            get => _item.HerbName; 
            set { _item.HerbName = value; RaisePropertyChanged(); }
        }
        
        /// <summary>
        /// 剂量
        /// </summary>
        public decimal Quantity
        {
            get => _item.Quantity;
            set
            {
                _item.Quantity = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Subtotal));
                RaisePropertyChanged(nameof(DisplayText));
                RaisePropertyChanged(nameof(PriceText));
            }
        }
        
        /// <summary>
        /// 单位
        /// </summary>
        public string Unit 
        { 
            get => _item.Unit; 
            set { _item.Unit = value; RaisePropertyChanged(); }
        }
        
        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice 
        { 
            get => _item.UnitPrice; 
            set 
            { 
                _item.UnitPrice = value; 
                RaisePropertyChanged(); 
                RaisePropertyChanged(nameof(Subtotal));
                RaisePropertyChanged(nameof(PriceText));
            }
        }
        
        /// <summary>
        /// 小计金额
        /// </summary>
        public decimal Subtotal => _item.Subtotal;
        
        /// <summary>
        /// 来源（验方、手动添加等）
        /// </summary>
        public string? Source 
        { 
            get => _item.ImportSource; 
            set { _item.ImportSource = value; RaisePropertyChanged(); }
        }
        
        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark 
        { 
            get => _item.Remark; 
            set { _item.Remark = value; RaisePropertyChanged(); }
        }
        
        /// <summary>
        /// 显示文本（药材名 + 剂量）
        /// </summary>
        public string DisplayText => _item.DisplayText;
        
        /// <summary>
        /// 价格显示文本
        /// </summary>
        public string PriceText => _item.PriceText;

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取底层数据模型
        /// </summary>
        public PrescriptionItem GetModel() => _item;

        /// <summary>
        /// 更新药材信息
        /// </summary>
        public void UpdateHerbInfo(string herbName, decimal unitPrice, string unit)
        {
            HerbName = herbName;
            UnitPrice = unitPrice;
            Unit = unit;
        }

        /// <summary>
        /// 设置剂量
        /// </summary>
        public void SetQuantity(decimal quantity)
        {
            if (quantity > 0)
            {
                Quantity = quantity;
            }
        }

        /// <summary>
        /// 重置到初始状态
        /// </summary>
        public void Reset()
        {
            HerbId = Guid.Empty;
            HerbName = "";
            Quantity = 0;
            Unit = "g";
            UnitPrice = 0;
            Source = "";
            Remark = "";
        }

        /// <summary>
        /// 验证数据有效性
        /// </summary>
        public bool IsValid()
        {
            return HerbId != Guid.Empty 
                && !string.IsNullOrWhiteSpace(HerbName) 
                && Quantity > 0 
                && UnitPrice >= 0;
        }

        /// <summary>
        /// 复制当前项目
        /// </summary>
        public PrescriptionItemViewModel Clone()
        {
            return new PrescriptionItemViewModel(new PrescriptionItem
            {
                HerbId = HerbId,
                HerbName = HerbName,
                Quantity = Quantity,
                Unit = Unit,
                UnitPrice = UnitPrice,
                ImportSource = Source,
                Remark = Remark
            });
        }

        #endregion

        #region 重写方法

        public override string ToString()
        {
            return $"{HerbName} {Quantity}{Unit} @ {UnitPrice:F2}元/{Unit}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is PrescriptionItemViewModel other)
            {
                return HerbId == other.HerbId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HerbId.GetHashCode();
        }

        #endregion
    }
}