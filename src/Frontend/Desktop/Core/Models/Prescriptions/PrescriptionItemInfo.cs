using Prism.Mvvm;
using System;

namespace LYBT.WPF.Client.Core.Models.Prescriptions
{
    /// <summary>
    /// 处方药材项信息模型 - 前端专用，支持MVVM属性通知
    /// </summary>
    public class PrescriptionItemInfo : BindableBase
    {
        private Guid _id;
        /// <summary>处方项ID</summary>
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private Guid _prescriptionId;
        /// <summary>处方ID</summary>
        public Guid PrescriptionId
        {
            get => _prescriptionId;
            set => SetProperty(ref _prescriptionId, value);
        }

        private Guid _herbId;
        /// <summary>药材ID</summary>
        public Guid HerbId
        {
            get => _herbId;
            set => SetProperty(ref _herbId, value);
        }

        private string _herbName = string.Empty;
        /// <summary>药材名称</summary>
        public string HerbName
        {
            get => _herbName;
            set => SetProperty(ref _herbName, value);
        }

        private decimal _quantity;
        /// <summary>用量</summary>
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    RaisePropertyChanged(nameof(Amount)); // 用量变化时通知金额更新
                }
            }
        }

        private string _unit = "g";
        /// <summary>单位</summary>
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        private decimal _unitPrice = 0;
        /// <summary>单价</summary>
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    RaisePropertyChanged(nameof(Amount)); // 单价变化时通知金额更新
                }
            }
        }

        /// <summary>小计金额</summary>
        public decimal Amount => UnitPrice * Quantity;

        private string? _usage;
        /// <summary>用法说明</summary>
        public string? Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        private string? _remark;
        /// <summary>备注信息</summary>
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        private string? _origin;
        /// <summary>产地（前端显示字段）</summary>
        public string? Origin
        {
            get => _origin;
            set => SetProperty(ref _origin, value);
        }

        private string? _specification;
        /// <summary>规格（前端显示字段）</summary>
        public string? Specification
        {
            get => _specification;
            set => SetProperty(ref _specification, value);
        }

        private bool _isOutOfStock;
        /// <summary>是否缺货（前端状态字段）</summary>
        public bool IsOutOfStock
        {
            get => _isOutOfStock;
            set => SetProperty(ref _isOutOfStock, value);
        }

        private bool _isSelected;
        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}