using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.Validation;

namespace LYBT.Desktop.Formula.Models
{
    /// <summary>
    /// 验方详情模型 - Master-Detail模式使用
    /// OpenSpec: refactor-master-detail-layout
    /// OpenSpec: ui-validation-framework - 添加验证支持
    ///
    /// 用于在Detail区域展示和编辑验方信息
    /// </summary>
    public class FormulaDetailModel : ValidatableModelBase
    {
        private Guid _id;
        private string _name = string.Empty;
        private string? _effect;
        private string? _usage;
        private string? _property;
        private string? _remark;
        private bool _isShared;
        private string? _category;
        private CommonStatus _status = CommonStatus.Enabled;
        private DateTime? _createdAt;
        private DateTime? _updatedAt;
        private Guid? _createdBy;
        private ObservableCollection<FormulaHerbItemDto> _herbs = new();

        /// <summary>验方ID</summary>
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>是否为新建</summary>
        public bool IsNew => Id == Guid.Empty;

        /// <summary>验方名称</summary>
        [Required(ErrorMessage = "验方名称不能为空")]
        [StringLength(ValidationConstants.NameMaxLength, ErrorMessage = "验方名称长度不能超过100个字符")]
        public string Name
        {
            get => _name;
            set => SetPropertyAndValidate(ref _name, value);
        }

        /// <summary>功效</summary>
        public string? Effect
        {
            get => _effect;
            set => SetProperty(ref _effect, value);
        }

        /// <summary>用法用量</summary>
        public string? Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        /// <summary>性味</summary>
        public string? Property
        {
            get => _property;
            set => SetProperty(ref _property, value);
        }

        /// <summary>备注</summary>
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        /// <summary>是否共享</summary>
        public bool IsShared
        {
            get => _isShared;
            set => SetProperty(ref _isShared, value);
        }

        /// <summary>分类</summary>
        public string? Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
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

        /// <summary>创建者ID</summary>
        public Guid? CreatedBy
        {
            get => _createdBy;
            set => SetProperty(ref _createdBy, value);
        }

        /// <summary>药材列表</summary>
        public ObservableCollection<FormulaHerbItemDto> Herbs
        {
            get => _herbs;
            set => SetProperty(ref _herbs, value);
        }

        /// <summary>药材数量</summary>
        public int HerbCount => Herbs.Count(h => h.HerbId != null && h.HerbId != Guid.Empty);

        /// <summary>创建空模型</summary>
        public static FormulaDetailModel CreateNew()
        {
            return new FormulaDetailModel
            {
                Id = Guid.Empty,
                Name = string.Empty,
                IsShared = false,
                Status = CommonStatus.Enabled,
                Herbs = new ObservableCollection<FormulaHerbItemDto>()
            };
        }

        /// <summary>从DTO创建模型</summary>
        /// <remarks>已废弃：请使用IMappingService&lt;FormulaDetailDto, FormulaInputDto, FormulaDetailModel&gt;.ToItem()</remarks>
        [Obsolete("请使用FormulaDetailModelMappingService.ToItem()替代。OpenSpec: adopt-mapperly-unified-mapping")]
        public static FormulaDetailModel FromDto(FormulaDetailDto dto)
        {
            var model = new FormulaDetailModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Effect = dto.Effect,
                Usage = dto.Usage,
                Property = dto.Property,
                Remark = dto.Remark,
                IsShared = dto.IsShared,
                Category = dto.Category,
                Status = dto.Status,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                CreatedBy = dto.CreatedBy
            };

            if (dto.Herbs != null)
            {
                foreach (var herb in dto.Herbs)
                {
                    model.Herbs.Add(new FormulaHerbItemDto
                    {
                        HerbId = herb.HerbId,
                        HerbName = herb.HerbName,
                        Dosage = herb.Dosage,
                        Unit = herb.Unit,
                        ProcessingMethod = herb.ProcessingMethod,
                        DecocteMethod = herb.DecocteMethod
                    });
                }
            }

            return model;
        }

        /// <summary>转换为DTO</summary>
        /// <remarks>已废弃：请使用IMappingService&lt;FormulaDetailDto, FormulaInputDto, FormulaDetailModel&gt;.ToDto()</remarks>
        [Obsolete("请使用FormulaDetailModelMappingService.ToDto()替代。OpenSpec: adopt-mapperly-unified-mapping")]
        public FormulaDetailDto ToDto()
        {
            return new FormulaDetailDto
            {
                Id = Id,
                Name = Name,
                Effect = Effect,
                Usage = Usage,
                Property = Property,
                Remark = Remark,
                IsShared = IsShared,
                Category = Category ?? string.Empty,
                Status = Status,
                CreatedAt = CreatedAt ?? DateTime.Now,
                UpdatedAt = UpdatedAt,
                CreatedBy = CreatedBy,
                Herbs = Herbs.ToList()
            };
        }

        /// <summary>克隆模型</summary>
        public FormulaDetailModel Clone()
        {
            var clone = new FormulaDetailModel
            {
                Id = Id,
                Name = Name,
                Effect = Effect,
                Usage = Usage,
                Property = Property,
                Remark = Remark,
                IsShared = IsShared,
                Category = Category,
                Status = Status,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                CreatedBy = CreatedBy
            };

            foreach (var herb in Herbs)
            {
                clone.Herbs.Add(new FormulaHerbItemDto
                {
                    HerbId = herb.HerbId,
                    HerbName = herb.HerbName,
                    Dosage = herb.Dosage,
                    Unit = herb.Unit,
                    ProcessingMethod = herb.ProcessingMethod,
                    DecocteMethod = herb.DecocteMethod
                });
            }

            return clone;
        }
    }
}
