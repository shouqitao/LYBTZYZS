using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using HerbStatus = LYBT.Shared.Models.Enums.HerbStatus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using System.ComponentModel;
using System.Windows.Data;

namespace LYBT.WPF.Client.Modules.SystemManagement.Herbs.ViewModels
{
    /// <summary>
    /// 药材管理视图模型
    /// </summary>
    public class HerbManagementViewModel : BindableBase
    {
        private readonly IHerbService _herbService;
        private string _searchKeyword = string.Empty;
        private HerbInfo _selectedHerb;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private bool _isLoading = false;
        private int _lowStockCount = 0;

        public ObservableCollection<HerbInfo> Herbs { get; }
        public ICollectionView HerbsView { get; }

        // Commands
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand ImportHerbsCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<HerbInfo> EditHerbCommand { get; }
        public DelegateCommand<HerbInfo> DeleteHerbCommand { get; }
        public DelegateCommand<HerbInfo> ManageStockCommand { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }

        /// <summary>搜索关键词</summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>选中的药材</summary>
        public HerbInfo SelectedHerb
        {
            get => _selectedHerb;
            set => SetProperty(ref _selectedHerb, value);
        }

        /// <summary>当前页码</summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>页大小</summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>总记录数</summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>库存不足数量</summary>
        public int LowStockCount
        {
            get => _lowStockCount;
            set => SetProperty(ref _lowStockCount, value);
        }

        /// <summary>状态文本</summary>
        public string StatusText => $"共 {TotalCount} 种药材，第 {CurrentPage} 页，共 {TotalPages} 页";

        /// <summary>总页数</summary>
        public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        public HerbManagementViewModel(IHerbService herbService)
        {
            _herbService = herbService;
            Herbs = new ObservableCollection<HerbInfo>();
            HerbsView = CollectionViewSource.GetDefaultView(Herbs);

            // 初始化命令
            SearchCommand = new DelegateCommand(ExecuteSearch);
            AddHerbCommand = new DelegateCommand(ExecuteAddHerb);
            ImportHerbsCommand = new DelegateCommand(ExecuteImportHerbs);
            RefreshCommand = new DelegateCommand(ExecuteRefresh);
            EditHerbCommand = new DelegateCommand<HerbInfo>(ExecuteEditHerb);
            DeleteHerbCommand = new DelegateCommand<HerbInfo>(ExecuteDeleteHerb);
            ManageStockCommand = new DelegateCommand<HerbInfo>(ExecuteManageStock);
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage, CanExecuteFirstPage);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage, CanExecutePreviousPage);
            NextPageCommand = new DelegateCommand(ExecuteNextPage, CanExecuteNextPage);
            LastPageCommand = new DelegateCommand(ExecuteLastPage, CanExecuteLastPage);

            // 加载初始数据
            LoadHerbs();
        }

        private async void LoadHerbs()
        {
            IsLoading = true;
            try
            {
                // TODO: 调用API获取药材列表
                await Task.Delay(1000); // 模拟API调用

                // 模拟数据
                Herbs.Clear();
                var herbNames = new[]
                {
                    "人参", "当归", "黄芪", "川芎", "白术", "茯苓", "甘草", "熟地黄", "白芍", "枸杞子",
                    "党参", "麦冬", "五味子", "山药", "薏苡仁", "陈皮", "半夏", "生姜", "大枣", "桂枝",
                    "附子", "干姜", "肉桂", "细辛", "麻黄", "桔梗", "杏仁", "紫苏叶", "防风", "荆芥",
                    "连翘", "金银花", "蒲公英", "板蓝根", "大青叶", "鱼腥草", "败酱草", "白头翁", "黄连", "黄芩",
                    "栀子", "龙胆草", "苦参", "白鲜皮", "地骨皮", "牡丹皮", "赤芍", "紫草", "茜草", "三七"
                };

                var origins = new[] { "吉林", "甘肃", "四川", "河南", "安徽", "山东", "湖北", "云南", "贵州", "陕西" };
                var units = new[] { "克", "两", "斤", "袋", "盒" };
                var specs = new[] { "统", "选", "特级", "一级", "二级" };

                var random = new Random();
                for (int i = 0; i < herbNames.Length; i++)
                {
                    var stock = random.Next(0, 200);
                    Herbs.Add(new HerbInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = herbNames[i],
                        PinyinCode = GetPinyin(herbNames[i]),
                        Origin = origins[random.Next(origins.Length)],
                        Spec = specs[random.Next(specs.Length)],
                        Unit = units[random.Next(units.Length)],
                        Price = (decimal)(random.NextDouble() * 100 + 5),
                        Stock = stock,
                        BatchNo = $"20241{random.Next(10, 99):D2}",
                        ExpireDate = DateTime.Now.AddMonths(random.Next(6, 36)),
                        Effect = GetHerbEffect(herbNames[i]),
                        IsActive = true,
                        CreateTime = DateTime.Now.AddDays(-random.Next(1, 365)),
                        UpdateTime = DateTime.Now.AddDays(-random.Next(1, 30))
                    });
                }

                TotalCount = Herbs.Count;
                LowStockCount = Herbs.Count(h => h.Stock < 10);

                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(TotalPages));

                // 更新分页命令状态
                FirstPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
                LastPageCommand.RaiseCanExecuteChanged();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetPinyin(string herbName)
        {
            // 简单的拼音映射，实际项目中应使用专业的拼音转换库
            var pinyinMap = new Dictionary<string, string>
            {
                { "人参", "RS" }, { "当归", "DG" }, { "黄芪", "HQ" }, { "川芎", "CX" }, { "白术", "BS" },
                { "茯苓", "FL" }, { "甘草", "GC" }, { "熟地黄", "SDH" }, { "白芍", "BS" }, { "枸杞子", "GQZ" }
            };
            return pinyinMap.ContainsKey(herbName) ? pinyinMap[herbName] : herbName.Substring(0, 1);
        }

        private string GetHerbEffect(string herbName)
        {
            var effectMap = new Dictionary<string, string>
            {
                { "人参", "大补元气，复脉固脱，补脾益肺，生津养血，安神益智" },
                { "当归", "补血活血，调经止痛，润肠通便" },
                { "黄芪", "补气升阳，固表止汗，利水消肿，生津养血，行滞通痹，托毒排脓，敛疮生肌" },
                { "川芎", "活血行气，祛风止痛" },
                { "白术", "健脾益气，燥湿利水，止汗，安胎" }
            };
            return effectMap.ContainsKey(herbName) ? effectMap[herbName] : "待完善";
        }

        private void ExecuteSearch()
        {
            // TODO: 实现搜索逻辑
            LoadHerbs();
        }

        private void ExecuteAddHerb()
        {
            // TODO: 打开新增药材对话框
        }

        private void ExecuteImportHerbs()
        {
            // TODO: 打开导入药材对话框
        }

        private void ExecuteRefresh()
        {
            LoadHerbs();
        }

        private void ExecuteEditHerb(HerbInfo herb)
        {
            if (herb == null) return;
            // TODO: 打开编辑药材对话框
        }

        private void ExecuteDeleteHerb(HerbInfo herb)
        {
            if (herb == null) return;
            // TODO: 确认删除药材
        }

        private void ExecuteManageStock(HerbInfo herb)
        {
            if (herb == null) return;
            // TODO: 打开库存管理对话框
        }

        private void ExecuteFirstPage()
        {
            CurrentPage = 1;
            LoadHerbs();
        }

        private bool CanExecuteFirstPage()
        {
            return CurrentPage > 1;
        }

        private void ExecutePreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadHerbs();
            }
        }

        private bool CanExecutePreviousPage()
        {
            return CurrentPage > 1;
        }

        private void ExecuteNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadHerbs();
            }
        }

        private bool CanExecuteNextPage()
        {
            return CurrentPage < TotalPages;
        }

        private void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
            LoadHerbs();
        }

        private bool CanExecuteLastPage()
        {
            return CurrentPage < TotalPages;
        }

        /// <summary>
        /// 转换HerbDto到HerbInfo
        /// </summary>
        private HerbInfo ConvertToHerbInfo(HerbDto dto)
        {
            return new HerbInfo
            {
                Id = dto.Id,
                // Code = dto.Code, // BaseHerbModel中没有Code属性
                Name = dto.Name,
                PinyinCode = dto.PinyinCode,
                // WuBiCode = dto.WuBiCode, // HerbDto中没有WuBiCode属性
                // Alias = dto.Alias, // BaseHerbModel中没有Alias属性
                // Category = dto.Category, // BaseHerbModel中没有Category属性
                Origin = dto.Origin,
                Spec = dto.Spec,
                Unit = dto.Unit,
                Price = dto.Price,
                Stock = (int)dto.Stock, // 需要转换为int
                // MinStock = dto.MinStock, // BaseHerbModel中没有MinStock属性
                // MaxStock = dto.MaxStock, // BaseHerbModel中没有MaxStock属性
                BatchNo = "",  // 批次号需要从库存记录获取
                ExpireDate = DateTime.Now.AddYears(2),  // 过期日期需要从库存记录获取
                Effect = dto.Effect,
                // Properties = dto.Properties, // BaseHerbModel中没有Properties属性
                // Usage = dto.Usage, // BaseHerbModel中没有Usage属性
                // Contraindications = dto.Contraindications, // BaseHerbModel中没有Contraindications属性
                Status = (HerbStatus)dto.Status, // 需要转换为枚举
                IsActive = dto.IsActive,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime,
                Remark = dto.Remark
            };
        }
    }
}