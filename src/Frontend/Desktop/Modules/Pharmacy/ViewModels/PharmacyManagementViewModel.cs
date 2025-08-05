using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Base;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Pharmacy;
using LYBT.WPF.Client.Modules.Pharmacy.Dialogs;
using Prism.Commands;
using Prism.Dialogs;

namespace LYBT.WPF.Client.Modules.Pharmacy.ViewModels
{
    /// <summary>
    /// 药房管理视图模型
    /// </summary>
    public class PharmacyManagementViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly IPharmacyService _pharmacyService;

        #region 属性

        private ObservableCollection<PrescriptionInfo> _prescriptionList;
        public ObservableCollection<PrescriptionInfo> PrescriptionList
        {
            get => _prescriptionList;
            set => SetProperty(ref _prescriptionList, value);
        }

        private PrescriptionInfo _selectedPrescription;
        public PrescriptionInfo SelectedPrescription
        {
            get => _selectedPrescription;
            set => SetProperty(ref _selectedPrescription, value);
        }

        private ObservableCollection<StockInfo> _stockList;
        public ObservableCollection<StockInfo> StockList
        {
            get => _stockList;
            set => SetProperty(ref _stockList, value);
        }

        private string _searchKeyword;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private string _selectedStatus = "全部状态";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                SetProperty(ref _selectedStatus, value);
                _ = SearchPrescriptions();
            }
        }

        private int _pendingCount;
        public int PendingCount
        {
            get => _pendingCount;
            set => SetProperty(ref _pendingCount, value);
        }

        private int _todayDispensedCount;
        public int TodayDispensedCount
        {
            get => _todayDispensedCount;
            set => SetProperty(ref _todayDispensedCount, value);
        }

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<PrescriptionInfo> StartDispensingCommand { get; }
        public DelegateCommand<PrescriptionInfo> CompleteDispensingCommand { get; }
        public DelegateCommand<PrescriptionInfo> DispenseDrugCommand { get; }
        public DelegateCommand<PrescriptionInfo> ViewDetailCommand { get; }
        public DelegateCommand ShowStockAlertCommand { get; }
        public DelegateCommand StockInCommand { get; }
        public DelegateCommand StockOutCommand { get; }
        public DelegateCommand InventoryCommand { get; }

        #endregion

        public PharmacyManagementViewModel(
            IDialogService dialogService,
            IPharmacyService pharmacyService)
        {
            _dialogService = dialogService;
            _pharmacyService = pharmacyService;

            PrescriptionList = new ObservableCollection<PrescriptionInfo>();
            StockList = new ObservableCollection<StockInfo>();

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SearchPrescriptions());
            RefreshCommand = new DelegateCommand(async () => await LoadPrescriptions());
            StartDispensingCommand = new DelegateCommand<PrescriptionInfo>(StartDispensing);
            CompleteDispensingCommand = new DelegateCommand<PrescriptionInfo>(CompleteDispensing);
            DispenseDrugCommand = new DelegateCommand<PrescriptionInfo>(DispenseDrug);
            ViewDetailCommand = new DelegateCommand<PrescriptionInfo>(ViewDetail);
            ShowStockAlertCommand = new DelegateCommand(ShowStockAlert);
            StockInCommand = new DelegateCommand(StockIn);
            StockOutCommand = new DelegateCommand(StockOut);
            InventoryCommand = new DelegateCommand(Inventory);

            // 加载数据
            _ = LoadPrescriptions();
            _ = LoadStockData();
            _ = LoadStatistics();
        }

        private async Task LoadPrescriptions()
        {
            try
            {
                SetBusy(true);

                var prescriptions = await _pharmacyService.GetTodayPrescriptionsAsync();
                PrescriptionList.Clear();

                foreach (var prescription in prescriptions)
                {
                    PrescriptionList.Add(new PrescriptionInfo
                    {
                        Id = prescription.Id,
                        PrescriptionNumber = prescription.PrescriptionNumber,
                        PatientName = prescription.PatientName,
                        Gender = prescription.Gender,
                        Age = prescription.Age,
                        DoctorName = prescription.DoctorName,
                        HerbCount = prescription.HerbCount,
                        TotalAmount = prescription.TotalAmount,
                        Status = prescription.Status,
                        StatusText = GetStatusText(prescription.Status),
                        StatusColor = GetStatusColor(prescription.Status),
                        CreateTime = prescription.CreateTime,
                        CanStartDispensing = prescription.Status == "待配药",
                        CanCompleteDispensing = prescription.Status == "配药中",
                        CanDispense = prescription.Status == "已配药"
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError($"加载处方列表失败：{ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task SearchPrescriptions()
        {
            try
            {
                SetBusy(true);

                var searchDto = new PrescriptionSearchDto
                {
                    Keyword = SearchKeyword,
                    Status = _selectedStatus == "全部状态" ? null : _selectedStatus
                };

                var prescriptions = await _pharmacyService.SearchPrescriptionsAsync(searchDto);
                PrescriptionList.Clear();

                foreach (var prescription in prescriptions)
                {
                    PrescriptionList.Add(new PrescriptionInfo
                    {
                        Id = prescription.Id,
                        PrescriptionNumber = prescription.PrescriptionNumber,
                        PatientName = prescription.PatientName,
                        Gender = prescription.Gender,
                        Age = prescription.Age,
                        DoctorName = prescription.DoctorName,
                        HerbCount = prescription.HerbCount,
                        TotalAmount = prescription.TotalAmount,
                        Status = prescription.Status,
                        StatusText = GetStatusText(prescription.Status),
                        StatusColor = GetStatusColor(prescription.Status),
                        CreateTime = prescription.CreateTime,
                        CanStartDispensing = prescription.Status == "待配药",
                        CanCompleteDispensing = prescription.Status == "配药中",
                        CanDispense = prescription.Status == "已配药"
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError($"搜索处方失败：{ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task LoadStockData()
        {
            try
            {
                var stocks = await _pharmacyService.GetStockListAsync();
                StockList.Clear();

                foreach (var stock in stocks)
                {
                    StockList.Add(new StockInfo
                    {
                        HerbId = stock.HerbId,
                        HerbName = stock.HerbName,
                        Specification = stock.Specification,
                        Unit = stock.Unit,
                        CurrentStock = stock.CurrentStock,
                        SafeStock = stock.SafeStock,
                        UnitPrice = stock.UnitPrice,
                        StockValue = stock.CurrentStock * stock.UnitPrice,
                        LastStockInDate = stock.LastStockInDate,
                        IsLowStock = stock.CurrentStock <= stock.SafeStock,
                        StockStatusText = GetStockStatusText(stock.CurrentStock, stock.SafeStock),
                        StockStatusColor = GetStockStatusColor(stock.CurrentStock, stock.SafeStock)
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError($"加载库存数据失败：{ex.Message}");
            }
        }

        private async Task LoadStatistics()
        {
            try
            {
                var statistics = await _pharmacyService.GetTodayStatisticsAsync();
                PendingCount = statistics.PendingCount;
                TodayDispensedCount = statistics.DispensedCount;
            }
            catch (Exception ex)
            {
                ShowError($"加载统计数据失败：{ex.Message}");
            }
        }

        private void StartDispensing(PrescriptionInfo prescription)
        {
            if (prescription == null) return;

            var parameters = new DialogParameters
            {
                { "Prescription", prescription }
            };

            _dialogService.ShowDialog("DispensingDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    prescription.Status = "配药中";
                    prescription.StatusText = GetStatusText(prescription.Status);
                    prescription.StatusColor = GetStatusColor(prescription.Status);
                    prescription.CanStartDispensing = false;
                    prescription.CanCompleteDispensing = true;
                    
                    _ = LoadStatistics();
                }
            });
        }

        private async void CompleteDispensing(PrescriptionInfo prescription)
        {
            if (prescription == null) return;

            if (!ShowConfirm($"确定完成"{prescription.PatientName}"的配药吗？"))
                return;

            try
            {
                SetBusy(true);

                var success = await _pharmacyService.CompleteDispensingAsync(prescription.Id);
                if (success)
                {
                    prescription.Status = "已配药";
                    prescription.StatusText = GetStatusText(prescription.Status);
                    prescription.StatusColor = GetStatusColor(prescription.Status);
                    prescription.CanCompleteDispensing = false;
                    prescription.CanDispense = true;

                    ShowInfo("配药完成");
                    _ = LoadStatistics();
                }
                else
                {
                    ShowError("配药完成失败");
                }
            }
            catch (Exception ex)
            {
                ShowError($"配药完成失败：{ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void DispenseDrug(PrescriptionInfo prescription)
        {
            if (prescription == null) return;

            if (!ShowConfirm($"确定发药给"{prescription.PatientName}"吗？"))
                return;

            try
            {
                SetBusy(true);

                var success = await _pharmacyService.DispenseDrugAsync(prescription.Id);
                if (success)
                {
                    prescription.Status = "已发药";
                    prescription.StatusText = GetStatusText(prescription.Status);
                    prescription.StatusColor = GetStatusColor(prescription.Status);
                    prescription.CanDispense = false;

                    ShowInfo("发药完成");
                    _ = LoadStatistics();
                }
                else
                {
                    ShowError("发药失败");
                }
            }
            catch (Exception ex)
            {
                ShowError($"发药失败：{ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ViewDetail(PrescriptionInfo prescription)
        {
            if (prescription == null) return;

            var parameters = new DialogParameters
            {
                { "PrescriptionId", prescription.Id }
            };

            _dialogService.ShowDialog("PrescriptionDetailDialog", parameters);
        }

        private void ShowStockAlert()
        {
            var lowStocks = StockList.Where(s => s.IsLowStock).ToList();
            if (lowStocks.Count == 0)
            {
                ShowInfo("目前没有库存预警");
                return;
            }

            var parameters = new DialogParameters
            {
                { "LowStocks", lowStocks }
            };

            _dialogService.ShowDialog("StockAlertDialog", parameters);
        }

        private void StockIn()
        {
            _dialogService.ShowDialog("StockInDialog", result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    _ = LoadStockData();
                }
            });
        }

        private void StockOut()
        {
            _dialogService.ShowDialog("StockOutDialog", result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    _ = LoadStockData();
                }
            });
        }

        private void Inventory()
        {
            _dialogService.ShowDialog("InventoryDialog", result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    _ = LoadStockData();
                }
            });
        }

        private string GetStatusText(string status)
        {
            return status switch
            {
                "待配药" => "待配药",
                "配药中" => "配药中",
                "已配药" => "已配药",
                "已发药" => "已发药",
                "已取消" => "已取消",
                _ => status
            };
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "待配药" => "#FF9800",
                "配药中" => "#2196F3",
                "已配药" => "#4CAF50",
                "已发药" => "#9E9E9E",
                "已取消" => "#F44336",
                _ => "#9E9E9E"
            };
        }

        private string GetStockStatusText(decimal currentStock, decimal safeStock)
        {
            if (currentStock <= 0)
                return "缺货";
            else if (currentStock <= safeStock)
                return "预警";
            else
                return "正常";
        }

        private string GetStockStatusColor(decimal currentStock, decimal safeStock)
        {
            if (currentStock <= 0)
                return "#F44336";
            else if (currentStock <= safeStock)
                return "#FF9800";
            else
                return "#4CAF50";
        }
    }
}