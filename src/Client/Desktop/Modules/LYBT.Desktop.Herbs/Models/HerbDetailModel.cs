using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.Validation;
using LYBT.Shared.Utilities.Text;

namespace LYBT.Desktop.Herbs.Models
{
    /// <summary>
    /// 药材详情模型 - Master-Detail模式使用
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 用于在Detail区域展示和编辑药材信息
    /// </summary>
    public class HerbDetailModel : ValidatableModelBase
    {
        private Guid _id;
        private string _name = string.Empty;
        private string _pinYinCode = string.Empty;
        private string? _category;
        private string? _properties;
        private string? _origin;
        private string? _spec;
        private string _unit = "克";
        private decimal _price;
        private decimal? _costPrice;
        private string? _effect;
        private string? _usage;
        private string? _remark;
        private CommonStatus _status = CommonStatus.Enabled;
        private DateTime? _createdAt;
        private DateTime? _updatedAt;

        /// <summary>药材ID</summary>
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>是否为新建</summary>
        public bool IsNew => Id == Guid.Empty;

        /// <summary>药材名称</summary>
        [Required(ErrorMessage = "药材名称不能为空")]
        [StringLength(ValidationConstants.NameMaxLength,
            ErrorMessage = "药材名称长度不能超过100个字符")]
        public string Name
        {
            get => _name;
            set
            {
                if (SetPropertyAndValidate(ref _name, value))
                {
                    // 自动生成拼音码
                    PinYinCode = PinYinHelper.GetPinYinCode(value);
                }
            }
        }

        /// <summary>拼音码（自动生成）</summary>
        public string PinYinCode
        {
            get => _pinYinCode;
            set => SetProperty(ref _pinYinCode, value);
        }

        /// <summary>分类</summary>
        [StringLength(ValidationConstants.NameMaxLength, ErrorMessage = "分类长度不能超过100个字符")]
        public string? Category
        {
            get => _category;
            set => SetPropertyAndValidate(ref _category, value);
        }

        /// <summary>性味</summary>
        public string? Properties
        {
            get => _properties;
            set => SetProperty(ref _properties, value);
        }

        /// <summary>产地</summary>
        [StringLength(ValidationConstants.AddressMaxLength, ErrorMessage = "产地长度不能超过200个字符")]
        public string? Origin
        {
            get => _origin;
            set => SetPropertyAndValidate(ref _origin, value);
        }

        /// <summary>规格</summary>
        [StringLength(ValidationConstants.NameMaxLength, ErrorMessage = "规格长度不能超过100个字符")]
        public string? Spec
        {
            get => _spec;
            set => SetPropertyAndValidate(ref _spec, value);
        }

        /// <summary>单位</summary>
        [Required(ErrorMessage = "单位不能为空")]
        public string Unit
        {
            get => _unit;
            set => SetPropertyAndValidate(ref _unit, value);
        }

        /// <summary>零售价</summary>
        [Required(ErrorMessage = "零售价不能为空")]
        [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "零售价必须在0.01-100000之间")]
        public decimal Price
        {
            get => _price;
            set => SetPropertyAndValidate(ref _price, value);
        }

        /// <summary>成本价</summary>
        [Range(typeof(decimal), "0", "100000", ErrorMessage = "成本价必须在0-100000之间")]
        public decimal? CostPrice
        {
            get => _costPrice;
            set => SetPropertyAndValidate(ref _costPrice, value);
        }

        /// <summary>功效</summary>
        [StringLength(ValidationConstants.LongRemarkMaxLength,
            ErrorMessage = "功效描述长度不能超过2000个字符")]
        public string? Effect
        {
            get => _effect;
            set => SetPropertyAndValidate(ref _effect, value);
        }

        /// <summary>用法用量</summary>
        [StringLength(ValidationConstants.UsageMaxLength,
            ErrorMessage = "用法用量长度不能超过200个字符")]
        public string? Usage
        {
            get => _usage;
            set => SetPropertyAndValidate(ref _usage, value);
        }

        /// <summary>备注</summary>
        [StringLength(ValidationConstants.RemarkMaxLength,
            ErrorMessage = "备注长度不能超过1000个字符")]
        public string? Remark
        {
            get => _remark;
            set => SetPropertyAndValidate(ref _remark, value);
        }

        /// <summary>状态</summary>
        public CommonStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>创建时间</summary>
        public DateTime? CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        /// <summary>更新时间</summary>
        public DateTime? UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        /// <summary>创建空模型</summary>
        public static HerbDetailModel CreateNew()
        {
            return new HerbDetailModel
            {
                Id = Guid.Empty,
                Name = string.Empty,
                PinYinCode = string.Empty,
                Unit = "克",
                Price = 0,
                Status = CommonStatus.Enabled
            };
        }

        /// <summary>克隆模型</summary>
        public HerbDetailModel Clone()
        {
            var clone = new HerbDetailModel
            {
                Id = Id,
                Category = Category,
                Properties = Properties,
                Origin = Origin,
                Spec = Spec,
                Unit = Unit,
                Price = Price,
                CostPrice = CostPrice,
                Effect = Effect,
                Usage = Usage,
                Remark = Remark,
                Status = Status,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
            // 直接赋值名称和拼音码，避免设置Name时触发自动生成
            clone._name = Name;
            clone._pinYinCode = PinYinCode;
            return clone;
        }
    }
}
