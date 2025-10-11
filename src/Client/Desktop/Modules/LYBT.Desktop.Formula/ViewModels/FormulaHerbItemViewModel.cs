using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Components;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 配方药材项目视图模型
    /// Issue #1153: 实现IHerbItem接口以支持共享组件
    /// </summary>
    public class FormulaHerbItemViewModel : UnifiedViewModelBase, IHerbItem
    {
        #region 属性

        private Guid _herbId;
        private string _herbName = string.Empty;
        private decimal _dosage;
        private string _unit = "g";
        private decimal _quantity = 1;
        private decimal _unitPrice;
        private string? _remark;

        /// <summary>
        /// 药材ID
        /// </summary>
        [Required(ErrorMessage = "药材不能为空")]
        public Guid HerbId
        {
            get => _herbId;
            set => SetProperty(ref _herbId, value);
        }

        /// <summary>
        /// 药材名称
        /// </summary>
        [Required(ErrorMessage = "药材名称不能为空")]
        [StringLength(100, ErrorMessage = "药材名称长度不能超过100个字符")]
        public string HerbName
        {
            get => _herbName;
            set => SetProperty(ref _herbName, value);
        }

        /// <summary>
        /// 用量
        /// </summary>
        [Required(ErrorMessage = "用量不能为空")]
        [Range(0.1, 500, ErrorMessage = "用量必须在0.1到500之间")]
        public decimal Dosage
        {
            get => _dosage;
            set => SetProperty(ref _dosage, value);
        }

        /// <summary>
        /// 单位
        /// </summary>
        [Required(ErrorMessage = "单位不能为空")]
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        /// <summary>
        /// 数量（克重）
        /// </summary>
        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetProperty(ref _unitPrice, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        #endregion

        #region 构造函数

        public FormulaHerbItemViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager sessionManager,
            IUserNotificationService userNotificationService)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
        }

        #endregion
    }
}