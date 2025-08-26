using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Shared.Models.Contracts.Prescriptions;
// UltraThink v2.0: 直接使用HerbDto，移除Info模型引用
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Interfaces.Services;
using Prism.Commands;
using Prism.Mvvm;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    /// <summary>
    /// 中药材选择对话框视图模型
    /// </summary>
    /// <summary>
    /// 中药材选择对话框ViewModel - UltraThink架构统一
    /// </summary>
    public class HerbSelectionDialogViewModel : DialogViewModelBase
    {
        private readonly IHerbService _herbService;
        private ObservableCollection<HerbDto> _availableHerbs = new();
        private HerbDto? _selectedHerb;
        private string _searchText = "";
        private decimal _quantity = 1;
        private string _unit = "g";

        /// <summary>
        /// 可选择的中药材列表
        /// </summary>
        public ObservableCollection<HerbDto> AvailableHerbs
        {
            get => _availableHerbs;
            set => SetProperty(ref _availableHerbs, value);
        }

        /// <summary>
        /// 选中的中药材
        /// </summary>
        public HerbDto? SelectedHerb
        {
            get => _selectedHerb;
            set 
            { 
                SetProperty(ref _selectedHerb, value);
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// 用量
        /// </summary>
        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
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
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; }

        /// <summary>
        /// 选中的中药材信息（用于返回结果）
        /// </summary>
        public PrescriptionHerbItemDto? Result { get; private set; }

        public HerbSelectionDialogViewModel(IHerbService herbService) : base()
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            Title = "选择中药材";
            
            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync());
            
            // 初始化加载中药材列表
            _ = LoadHerbsAsync();
        }

        /// <summary>
        /// 加载中药材列表
        /// </summary>
        private async Task LoadHerbsAsync()
        {
            try
            {
                IsLoading = true;
                var result = await _herbService.GetPagedAsync(new HerbPagedQueryDto { PageSize = 100 });
                if (result.IsSuccess && result.Data != null)
                {
                    AvailableHerbs = new ObservableCollection<HerbDto>(result.Data.Items);
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("加载中药材列表", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行搜索
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadHerbsAsync();
                return;
            }

            try
            {
                IsLoading = true;
                var result = await _herbService.GetPagedAsync(new HerbPagedQueryDto 
                { 
                    Name = SearchText,
                    PageSize = 100 
                });
                
                if (result.IsSuccess && result.Data != null)
                {
                    AvailableHerbs = new ObservableCollection<HerbDto>(result.Data.Items);
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("搜索中药材", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行确认逻辑
        /// </summary>
        protected override Task<bool> ExecuteConfirmAsync()
        {
            if (SelectedHerb == null)
                return Task.FromResult(false);

            Result = new PrescriptionHerbItemDto
            {
                HerbId = SelectedHerb.Id,
                HerbName = SelectedHerb.Name,
                Quantity = Quantity,
                Unit = Unit,
                Price = SelectedHerb.Price,
                Subtotal = SelectedHerb.Price * Quantity
            };

            return Task.FromResult(true);
        }

        /// <summary>
        /// 检查是否可以确认
        /// </summary>
        protected override bool CanConfirm()
        {
            return !IsLoading && SelectedHerb != null && Quantity > 0;
        }
    }
}