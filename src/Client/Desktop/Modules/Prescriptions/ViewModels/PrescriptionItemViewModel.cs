using System;
using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;
using LYBT.Desktop.Infrastructure.Interfaces;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方项目视图模型 - UltraThink架构实现
    /// </summary>
    public class PrescriptionItemViewModel : UnifiedViewModelBase
    {
        #region 属性

        private Guid _herbId;
        private string _herbName = string.Empty;
        private decimal _dosage;
        private string _unit = "g";
        private string? _notes;
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
        [Range(0.1, 999.9, ErrorMessage = "用量必须在0.1-999.9之间")]
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
        /// 备注
        /// </summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        /// <summary>
        /// 数量
        /// </summary>
        [Required(ErrorMessage = "数量不能为空")]
        [Range(0.1, 999.9, ErrorMessage = "数量必须在0.1-999.9之间")]
        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        /// <summary>
        /// 单价
        /// </summary>
        [Range(0, 99999.99, ErrorMessage = "单价必须在0-99999.99之间")]
        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetProperty(ref _unitPrice, value);
        }

        /// <summary>
        /// 备注（别名，与Notes保持兼容）
        /// </summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        #endregion

        #region 构造函数

        public PrescriptionItemViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 转换为处方项目DTO
        /// </summary>
        public PrescriptionItemDto ToDto()
        {
            return new PrescriptionItemDto
            {
                HerbId = HerbId,
                HerbName = HerbName,
                Dosage = Dosage,
                Unit = Unit,
                Notes = Notes
            };
        }

        /// <summary>
        /// 从DTO加载数据
        /// </summary>
        public void LoadFromDto(PrescriptionItemDto dto)
        {
            if (dto == null) return;

            HerbId = dto.HerbId;
            HerbName = dto.HerbName ?? string.Empty;
            Dosage = dto.Dosage;
            Unit = dto.Unit ?? "g";
            Notes = dto.Notes;
        }

        /// <summary>
        /// 从药材DTO加载数据
        /// </summary>
        public void LoadFromHerb(HerbDto herb, decimal dosage = 10m)
        {
            if (herb == null) return;

            HerbId = herb.Id;
            HerbName = herb.Name ?? string.Empty;
            Dosage = dosage;
            Unit = "g";
        }

        #endregion
    }
}