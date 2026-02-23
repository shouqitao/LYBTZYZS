using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Components;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Controls.HerbItem
{
    /// <summary>
    /// 药材项控件内部ViewModel
    /// OpenSpec: herb-editor-control-refactoring
    /// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
    /// OpenSpec: cross-module-decoupling - 迁移到Infrastructure，解耦模块间编译依赖
    /// </summary>
    public partial class HerbItemControlViewModel : ObservableObject, IHerbItemEditable
    {
        #region Fields

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEmpty))]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        private Guid _herbId;

        [ObservableProperty]
        private string _herbName = string.Empty;

        [ObservableProperty]
        private string _unit = "g";

        [ObservableProperty]
        private decimal _unitPrice;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        private bool _isDosageValid = true;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<HerbListDto>? _allHerbs;

        [ObservableProperty]
        private ObservableCollection<HerbListDto> _filteredHerbs = new();

        [ObservableProperty]
        private HerbListDto? _selectedHerb;

        private int _dosage;
        private DecocteMethod _decocteMethod = DecocteMethod.Default;

        #endregion

        #region Events

        /// <summary>
        /// 药材项变更事件
        /// </summary>
        public event EventHandler<HerbItemChangedEventArgs>? ItemChanged;

        #endregion

        #region IHerbItem Properties

        public int Dosage
        {
            get => _dosage;
            set
            {
                if (SetProperty(ref _dosage, value))
                {
                    ValidateDosage();
                    OnPropertyChanged(nameof(IsValid));
                    OnItemChanged(HerbItemChangeType.DosageChanged);
                }
            }
        }

        /// <summary>
        /// 煎法
        /// </summary>
        public DecocteMethod DecocteMethod
        {
            get => _decocteMethod;
            set
            {
                if (SetProperty(ref _decocteMethod, value))
                {
                    OnItemChanged(HerbItemChangeType.DecocteMethodChanged);
                }
            }
        }

        /// <summary>
        /// 是否为空行(未选择药材)
        /// </summary>
        public bool IsEmpty => HerbId == Guid.Empty;

        /// <summary>
        /// 是否为有效药材项
        /// </summary>
        public bool IsValid => HerbId != Guid.Empty && Dosage > 0 && IsDosageValid;

        #endregion

        #region 属性变更回调

        /// <summary>
        /// 药材名称变更时触发药材过滤
        /// </summary>
        partial void OnHerbNameChanged(string value)
        {
            FilterHerbs();
        }

        /// <summary>
        /// 选中药材变更时自动填充属性
        /// </summary>
        partial void OnSelectedHerbChanged(HerbListDto? value)
        {
            if (value != null)
            {
                // 自动填充药材属性
                HerbId = value.Id;
                // 直接设置字段避免触发FilterHerbs
#pragma warning disable MVVMTK0034
                _herbName = value.Name;
#pragma warning restore MVVMTK0034
                OnPropertyChanged(nameof(HerbName));
                Unit = value.Unit;
                UnitPrice = value.Price;

                OnItemChanged(HerbItemChangeType.HerbSelected);
            }
        }

        /// <summary>
        /// AllHerbs变更时更新所有子项的药材库引用
        /// </summary>
        partial void OnAllHerbsChanged(ObservableCollection<HerbListDto>? value)
        {
            // 可以在这里添加额外逻辑
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 从DTO加载数据
        /// </summary>
        public void LoadFromDto(PrescriptionItemDto dto)
        {
            HerbId = dto.HerbId;
#pragma warning disable MVVMTK0034
            _herbName = dto.HerbName;
#pragma warning restore MVVMTK0034
            OnPropertyChanged(nameof(HerbName));
            Dosage = dto.Dosage;
            Unit = dto.Unit;
            UnitPrice = dto.UnitPrice;
            DecocteMethod = dto.DecocteMethod;
        }

        /// <summary>
        /// 导出为DTO
        /// </summary>
        public PrescriptionItemDto ToDto()
        {
            return new PrescriptionItemDto
            {
                HerbId = HerbId,
                HerbName = HerbName,
                Dosage = Dosage,
                Unit = Unit,
                UnitPrice = UnitPrice,
                DecocteMethod = DecocteMethod
            };
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        public void Clear()
        {
            HerbId = Guid.Empty;
#pragma warning disable MVVMTK0034
            _herbName = string.Empty;
#pragma warning restore MVVMTK0034
            OnPropertyChanged(nameof(HerbName));
            Dosage = 0;
            Unit = "g";
            UnitPrice = 0;
            DecocteMethod = DecocteMethod.Default;
            SelectedHerb = null;
            FilteredHerbs.Clear();
            ValidationMessage = string.Empty;
            IsDosageValid = true;

            OnItemChanged(HerbItemChangeType.Cleared);
        }

        /// <summary>
        /// 执行校验
        /// </summary>
        public bool Validate()
        {
            ValidateDosage();
            return IsValid;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 过滤药材列表(拼音码匹配)
        /// </summary>
        private void FilterHerbs()
        {
            if (AllHerbs == null || string.IsNullOrWhiteSpace(HerbName))
            {
                FilteredHerbs.Clear();
                return;
            }

            var keyword = HerbName.Trim().ToLower();

            // 过滤匹配的药材
            var filtered = AllHerbs
                .Where(h =>
                    h.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(h.PinYinCode) && h.PinYinCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .Take(20) // 限制建议数量
                .ToList();

            FilteredHerbs.Clear();
            foreach (var herb in filtered)
            {
                FilteredHerbs.Add(herb);
            }
        }

        /// <summary>
        /// 校验剂量
        /// </summary>
        private void ValidateDosage()
        {
            if (IsEmpty)
            {
                // 空行不校验
                IsDosageValid = true;
                ValidationMessage = string.Empty;
                return;
            }

            if (Dosage <= 0)
            {
                IsDosageValid = false;
                ValidationMessage = "剂量必须大于0";
            }
            else if (Dosage > 500)
            {
                IsDosageValid = false;
                ValidationMessage = "剂量不能超过500g";
            }
            else
            {
                IsDosageValid = true;
                ValidationMessage = string.Empty;
            }
        }

        /// <summary>
        /// 触发ItemChanged事件
        /// </summary>
        private void OnItemChanged(HerbItemChangeType changeType)
        {
            ItemChanged?.Invoke(this, new HerbItemChangedEventArgs(changeType, ToDto()));
        }

        #endregion
    }
}
