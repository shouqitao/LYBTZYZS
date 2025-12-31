using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Controls.HerbItem;
using LYBT.Desktop.Infrastructure.Models;
using LYBT.Shared.Models.Contracts.Herbs;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Infrastructure.Controls.HerbList
{
    /// <summary>
    /// 药材列表控件内部ViewModel
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    public class HerbListControlViewModel : BindableBase
    {
        #region Fields

        private ObservableCollection<HerbListDto>? _allHerbs;
        private DuplicateDosageStrategy _duplicateStrategy = DuplicateDosageStrategy.Max;

        #endregion

        #region Events

        /// <summary>
        /// 列表变更事件
        /// </summary>
        public event EventHandler<HerbListChangedEventArgs>? ListChanged;

        #endregion

        #region Properties

        /// <summary>
        /// 药材项集合
        /// </summary>
        public ObservableCollection<HerbItemControlViewModel> Items { get; } = new();

        /// <summary>
        /// 药材库数据
        /// </summary>
        public ObservableCollection<HerbListDto>? AllHerbs
        {
            get => _allHerbs;
            set
            {
                if (SetProperty(ref _allHerbs, value))
                {
                    // 更新所有子项的药材库引用
                    foreach (var item in Items)
                    {
                        item.AllHerbs = value;
                    }
                }
            }
        }

        /// <summary>
        /// 重复剂量取值策略
        /// </summary>
        public DuplicateDosageStrategy DuplicateStrategy
        {
            get => _duplicateStrategy;
            set => SetProperty(ref _duplicateStrategy, value);
        }

        /// <summary>
        /// 有效药材项数量(排除空行)
        /// </summary>
        public int ValidItemCount => Items.Count(i => !i.IsEmpty);

        /// <summary>
        /// 是否有重复药材
        /// </summary>
        public bool HasDuplicates => CheckForDuplicates();

        /// <summary>
        /// 是否全部有效
        /// </summary>
        public bool IsValid => Items.All(i => i.IsEmpty || i.IsValid);

        #endregion

        #region Commands

        public DelegateCommand ClearAllCommand { get; }

        #endregion

        #region Constructor

        public HerbListControlViewModel()
        {
            ClearAllCommand = new DelegateCommand(ExecuteClearAll, CanExecuteClearAll);

            // 初始化时添加一个空槽位
            EnsureSingleEmptySlot();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 从DTO列表加载数据
        /// </summary>
        public void LoadFromDto(IEnumerable<HerbItemDto> items)
        {
            Items.Clear();

            foreach (var dto in items.Where(i => !i.IsEmpty))
            {
                var vm = CreateItemViewModel();
                vm.LoadFromDto(dto);
                Items.Add(vm);
            }

            EnsureSingleEmptySlot();
            OnListChanged(HerbListChangeType.Loaded);
        }

        /// <summary>
        /// 导出为DTO列表
        /// </summary>
        public IReadOnlyList<HerbItemDto> ToDto()
        {
            return Items
                .Where(i => !i.IsEmpty)
                .Select(i => i.ToDto())
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// 批量添加药材(处理重复)
        /// </summary>
        /// <param name="herbs">要添加的药材列表</param>
        /// <param name="onDuplicateFound">发现重复时的回调(返回true则合并，false则跳过)</param>
        public async Task AddHerbsAsync(
            IEnumerable<HerbItemDto> herbs,
            Func<HerbItemDto, HerbItemDto, Task<bool>>? onDuplicateFound = null)
        {
            foreach (var herb in herbs.Where(h => !h.IsEmpty))
            {
                var existingIndex = FindHerbIndex(herb.HerbId);
                if (existingIndex >= 0)
                {
                    // 发现重复
                    var existing = Items[existingIndex];
                    var shouldMerge = onDuplicateFound != null
                        ? await onDuplicateFound(existing.ToDto(), herb)
                        : true;

                    if (shouldMerge)
                    {
                        // 合并剂量
                        var mergedDosage = DuplicateStrategy.CalculateMergedDosage(
                            existing.Dosage, herb.Dosage);
                        existing.Dosage = mergedDosage;
                    }
                }
                else
                {
                    // 添加新药材
                    AddItem(herb);
                }
            }

            EnsureSingleEmptySlot();
            OnListChanged(HerbListChangeType.BatchImported);
        }

        /// <summary>
        /// 同步版本的批量添加(无重复确认)
        /// </summary>
        public void AddHerbs(IEnumerable<HerbItemDto> herbs)
        {
            foreach (var herb in herbs.Where(h => !h.IsEmpty))
            {
                var existingIndex = FindHerbIndex(herb.HerbId);
                if (existingIndex >= 0)
                {
                    // 合并剂量
                    var existing = Items[existingIndex];
                    var mergedDosage = DuplicateStrategy.CalculateMergedDosage(
                        existing.Dosage, herb.Dosage);
                    existing.Dosage = mergedDosage;
                }
                else
                {
                    AddItem(herb);
                }
            }

            EnsureSingleEmptySlot();
            OnListChanged(HerbListChangeType.BatchImported);
        }

        /// <summary>
        /// 清空所有药材
        /// </summary>
        public void Clear()
        {
            Items.Clear();
            EnsureSingleEmptySlot();
            OnListChanged(HerbListChangeType.Cleared);
        }

        /// <summary>
        /// 删除指定索引的药材
        /// </summary>
        public void DeleteAt(int index)
        {
            if (index < 0 || index >= Items.Count)
                return;

            var item = Items[index];
            Items.RemoveAt(index);

            EnsureSingleEmptySlot();
            Compact();
            OnListChanged(HerbListChangeType.ItemRemoved, item.ToDto(), index);
        }

        /// <summary>
        /// 移动药材位置
        /// </summary>
        public void MoveItem(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= Items.Count ||
                newIndex < 0 || newIndex >= Items.Count ||
                oldIndex == newIndex)
                return;

            var item = Items[oldIndex];
            Items.RemoveAt(oldIndex);
            Items.Insert(newIndex, item);

            OnListChanged(HerbListChangeType.ItemMoved, item.ToDto(), newIndex);
        }

        /// <summary>
        /// 执行校验
        /// </summary>
        public bool Validate()
        {
            foreach (var item in Items.Where(i => !i.IsEmpty))
            {
                item.Validate();
            }

            RaisePropertyChanged(nameof(IsValid));
            return IsValid;
        }

        /// <summary>
        /// 检查是否可以添加指定药材(重复检测)
        /// </summary>
        public bool CanAddHerb(Guid herbId)
        {
            return FindHerbIndex(herbId) < 0;
        }

        /// <summary>
        /// 请求添加新的空槽位(Enter键触发)
        /// </summary>
        public void RequestNewSlot()
        {
            EnsureSingleEmptySlot();
        }

        /// <summary>
        /// 获取指定索引后的第一个空槽位索引
        /// </summary>
        public int GetNextEmptySlotIndex(int afterIndex)
        {
            for (int i = afterIndex + 1; i < Items.Count; i++)
            {
                if (Items[i].IsEmpty)
                    return i;
            }
            return -1;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 创建新的子ViewModel
        /// </summary>
        private HerbItemControlViewModel CreateItemViewModel()
        {
            var vm = new HerbItemControlViewModel
            {
                AllHerbs = AllHerbs
            };

            vm.ItemChanged += OnItemChanged;

            return vm;
        }

        /// <summary>
        /// 添加单个药材
        /// </summary>
        private void AddItem(HerbItemDto dto)
        {
            // 从药材库同步最新信息
            // OpenSpec: herb-editor-control-refactoring - 导入时使用药材库最新数据
            // 解决问题：
            // 1. 经验方无价格、历史处方价格可能过时
            // 2. 药材名称可能被修改（如"红枣"→"大枣"）
            // 3. 单位等信息可能变化
            if (dto.HerbId != Guid.Empty && AllHerbs != null)
            {
                var herbInfo = AllHerbs.FirstOrDefault(h => h.Id == dto.HerbId);
                if (herbInfo != null)
                {
                    // 同步最新信息，保留原始剂量和煎法
                    dto.HerbName = herbInfo.Name;
                    dto.Unit = herbInfo.Unit;
                    dto.UnitPrice = herbInfo.Price;
                }
            }

            // 找到最后一个非空项的位置
            var insertIndex = Items.Count;
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (!Items[i].IsEmpty)
                {
                    insertIndex = i + 1;
                    break;
                }
                else
                {
                    insertIndex = i;
                }
            }

            var vm = CreateItemViewModel();
            vm.LoadFromDto(dto);
            Items.Insert(insertIndex, vm);
        }

        /// <summary>
        /// 确保只有一个空槽位
        /// </summary>
        private void EnsureSingleEmptySlot()
        {
            // 移除多余的空槽位
            var emptySlots = Items.Where(i => i.IsEmpty).ToList();
            while (emptySlots.Count > 1)
            {
                var toRemove = emptySlots[emptySlots.Count - 1];
                Items.Remove(toRemove);
                emptySlots.RemoveAt(emptySlots.Count - 1);
            }

            // 确保至少有一个空槽位
            if (!Items.Any(i => i.IsEmpty))
            {
                Items.Add(CreateItemViewModel());
            }

            RaisePropertyChanged(nameof(ValidItemCount));
        }

        /// <summary>
        /// 紧凑列表(将空槽位移到末尾)
        /// </summary>
        private void Compact()
        {
            // 收集所有非空项
            var nonEmptyItems = Items.Where(i => !i.IsEmpty).ToList();

            // 清空并重新添加
            Items.Clear();
            foreach (var item in nonEmptyItems)
            {
                Items.Add(item);
            }

            EnsureSingleEmptySlot();
        }

        /// <summary>
        /// 查找药材在列表中的索引
        /// </summary>
        private int FindHerbIndex(Guid herbId)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].HerbId == herbId)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 检查是否有重复药材
        /// </summary>
        private bool CheckForDuplicates()
        {
            var herbIds = Items
                .Where(i => !i.IsEmpty)
                .Select(i => i.HerbId)
                .ToList();

            return herbIds.Count != herbIds.Distinct().Count();
        }

        /// <summary>
        /// 子项变更事件处理
        /// </summary>
        private void OnItemChanged(object? sender, HerbItemChangedEventArgs e)
        {
            // 当某项从空变为非空时，确保末尾有空槽位
            if (e.ChangeType == HerbItemChangeType.HerbSelected)
            {
                EnsureSingleEmptySlot();
            }

            RaisePropertyChanged(nameof(ValidItemCount));
            RaisePropertyChanged(nameof(HasDuplicates));
            RaisePropertyChanged(nameof(IsValid));

            OnListChanged(HerbListChangeType.ItemModified, e.Item, e.Index);
        }

        /// <summary>
        /// 触发ListChanged事件
        /// </summary>
        private void OnListChanged(HerbListChangeType changeType, HerbItemDto? item = null, int index = -1)
        {
            RaisePropertyChanged(nameof(ValidItemCount));
            ListChanged?.Invoke(this, new HerbListChangedEventArgs(changeType, ValidItemCount, item, index));
        }

        private void ExecuteClearAll()
        {
            Clear();
        }

        private bool CanExecuteClearAll()
        {
            return ValidItemCount > 0;
        }

        #endregion
    }
}
