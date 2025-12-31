using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Models;
using LYBT.Shared.Components;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Infrastructure.Controls.HerbItem
{
    /// <summary>
    /// 药材项控件内部ViewModel
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    public class HerbItemControlViewModel : BindableBase, IHerbItemEditable
    {
        #region Fields

        private Guid _herbId;
        private string _herbName = string.Empty;
        private int _dosage;
        private string _unit = "g";
        private decimal _unitPrice;
        private DecocteMethod _decocteMethod = DecocteMethod.Default;
        private ObservableCollection<HerbListDto>? _allHerbs;
        private ObservableCollection<HerbListDto> _filteredHerbs = new();
        private HerbListDto? _selectedHerb;
        private string _validationMessage = string.Empty;
        private bool _isDosageValid = true;

        #endregion

        #region Events

        /// <summary>
        /// 药材项变更事件
        /// </summary>
        public event EventHandler<HerbItemChangedEventArgs>? ItemChanged;

        #endregion

        #region IHerbItem Properties

        public Guid HerbId
        {
            get => _herbId;
            set
            {
                if (SetProperty(ref _herbId, value))
                {
                    RaisePropertyChanged(nameof(IsEmpty));
                    RaisePropertyChanged(nameof(IsValid));
                }
            }
        }

        public string HerbName
        {
            get => _herbName;
            set
            {
                if (SetProperty(ref _herbName, value))
                {
                    // 触发药材过滤
                    FilterHerbs();
                }
            }
        }

        public int Dosage
        {
            get => _dosage;
            set
            {
                if (SetProperty(ref _dosage, value))
                {
                    ValidateDosage();
                    RaisePropertyChanged(nameof(IsValid));
                    OnItemChanged(HerbItemChangeType.DosageChanged);
                }
            }
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetProperty(ref _unitPrice, value);
        }

        #endregion

        #region Extended Properties

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
        /// 剂量是否有效(1-500g范围内)
        /// </summary>
        public bool IsDosageValid
        {
            get => _isDosageValid;
            private set => SetProperty(ref _isDosageValid, value);
        }

        /// <summary>
        /// 校验消息
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            private set => SetProperty(ref _validationMessage, value);
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

        #region IHerbItemEditable Properties

        public ObservableCollection<HerbListDto>? AllHerbs
        {
            get => _allHerbs;
            set => SetProperty(ref _allHerbs, value);
        }

        public ObservableCollection<HerbListDto> FilteredHerbs
        {
            get => _filteredHerbs;
            private set => SetProperty(ref _filteredHerbs, value);
        }

        public HerbListDto? SelectedHerb
        {
            get => _selectedHerb;
            set
            {
                if (SetProperty(ref _selectedHerb, value) && value != null)
                {
                    // 自动填充药材属性
                    HerbId = value.Id;
                    _herbName = value.Name; // 直接设置避免触发FilterHerbs
                    RaisePropertyChanged(nameof(HerbName));
                    Unit = value.Unit;
                    UnitPrice = value.Price;

                    OnItemChanged(HerbItemChangeType.HerbSelected);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 从DTO加载数据
        /// </summary>
        public void LoadFromDto(HerbItemDto dto)
        {
            HerbId = dto.HerbId;
            _herbName = dto.HerbName;
            RaisePropertyChanged(nameof(HerbName));
            Dosage = dto.Dosage;
            Unit = dto.Unit;
            UnitPrice = dto.UnitPrice;
            DecocteMethod = dto.DecocteMethod;
        }

        /// <summary>
        /// 导出为DTO
        /// </summary>
        public HerbItemDto ToDto()
        {
            return new HerbItemDto
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
            _herbName = string.Empty;
            RaisePropertyChanged(nameof(HerbName));
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
