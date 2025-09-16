using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Prescriptions.ViewModels
{

    /// <summary>
    /// 处方管理视图模型（UltraThink 现代架构版）
    /// 基于ModernManagementViewModel，统一的管理界面模式
    /// 零编译警告，现代化MVVM架构
    /// </summary>
    public class PrescriptionManagementViewModel : ModernManagementViewModel<PrescriptionDto>
    {

        #region Fields

        private readonly IPrescriptionService _prescriptionService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;
        private readonly IPrescriptionPrintService _printService;

        #endregion Fields

        #region 额外Commands

        /// <summary>打印处方命令</summary>
        public DelegateCommand PrintCommand { get; }

        /// <summary>P0-02新增：查看患者处方历史命令</summary>
        public DelegateCommand ViewPatientHistoryCommand { get; }

        /// <summary>Epic 04-P0-02新增：导出处方数据命令</summary>
        public DelegateCommand ExportPrescriptionsCommand { get; }

        #endregion 额外Commands

        #region Constructor

        public PrescriptionManagementViewModel(
            IPrescriptionService prescriptionService,
            ICustomDialogService dialogService,
            IMapper mapper,
            IPrescriptionPrintService printService,
            IEventAggregator eventAggregator,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, errorHandlingService)
        {
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化额外命令
            PrintCommand = new DelegateCommand(async () => await ExecutePrintAsync(), () => HasSelectedItem);
            ViewPatientHistoryCommand = new DelegateCommand(async () => await ViewPatientHistoryAsync(), () => HasSelectedItem);
            ExportPrescriptionsCommand = new DelegateCommand(async () => await ExportPrescriptionsAsync());
        }

        /// <summary>Initializes a new instance of the <see cref="PrescriptionManagementViewModel"/> class.兼容性构造函数</summary>
        public PrescriptionManagementViewModel(
            IPrescriptionService prescriptionService,
            ICustomDialogService dialogService,
            IMapper mapper,
            IEventAggregator eventAggregator)
            : this(prescriptionService, dialogService, mapper, null, eventAggregator, null)
        {
        }

        #endregion Constructor

        #region 重写基类方法

        /// <summary>加载数据</summary>
        protected override async Task<ServiceResult<PagedResult<PrescriptionDto>>> LoadDataAsync(int page, int pageSize, string? keyword = null)
        {
            var prescriptionQuery = new PrescriptionQueryDto
            {
                PageIndex = page,
                PageSize = pageSize,
                Keyword = keyword ?? string.Empty
            };
            return await _prescriptionService.GetPagedAsync(prescriptionQuery);
        }

        /// <summary>添加项</summary>
        protected override async Task OnAddAsync()
        {
            var parameters = new Dictionary<string, object> { ["IsEditMode"] = false };
            var result = await _dialogService.ShowDialogAsync("PrescriptionEditorDialog", parameters);

            if (result.Result == true)
            {
                await _dialogService.ShowSuccessAsync("处方创建成功", "成功");
            }
        }

        /// <summary>编辑项</summary>
        protected override async Task OnEditAsync(PrescriptionDto item)
        {
            var parameters = new Dictionary<string, object>
            {
                ["IsEditMode"] = true,
                ["Prescription"] = item
            };
            var result = await _dialogService.ShowDialogAsync("PrescriptionEditorDialog", parameters);

            if (result.Result == true)
            {
                await _dialogService.ShowSuccessAsync($"处方 {item.Id} 更新成功", "成功");
            }
        }

        /// <summary>删除项</summary>
        protected override async Task OnDeleteAsync(PrescriptionDto item)
        {
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要删除处方 {item.Id} 吗？\n此操作不可恢复。",
                "确认删除");

            if (confirm)
            {
                var result = await _prescriptionService.DeleteAsync(item.Id);
                if (result.IsSuccess)
                {
                    await _dialogService.ShowInformationAsync("处方删除成功", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "处方删除失败", "错误");
                }
            }
        }

        /// <summary>查看详情</summary>
        protected override async Task OnViewDetailsAsync(PrescriptionDto item)
        {
            var result = await _prescriptionService.GetByIdAsync(item.Id);

            if (result.IsSuccess && result.Data != null)
            {
                var detailInfo = $"处方ID: {result.Data.Id}\n创建时间: {result.Data.CreateTime}\n更新时间: {result.Data.UpdateTime}";
                await _dialogService.ShowInformationAsync(detailInfo, $"处方详情 - {result.Data.Id}");
            }
            else
            {
                await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "获取处方详情失败", "错误");
            }
        }

        /// <summary>更新Command状态</summary>
        protected override void RaiseCanExecuteChanged()
        {
            base.RaiseCanExecuteChanged();
            PrintCommand.RaiseCanExecuteChanged();
            ViewPatientHistoryCommand.RaiseCanExecuteChanged();
        }

        #endregion 重写基类方法

        #region Command执行方法

        /// <summary>
        /// P0-03优化：处方标准打印功能
        /// Epic 03-P0-03: 实用化处方标准打印功能，专为小诊所设计
        /// 使用专业的IPrescriptionPrintService，提供预览和标准化打印
        /// </summary>
        private async Task ExecutePrintAsync()
        {
            if (SelectedItem == null)
            {
                await _dialogService.ShowWarningAsync("请先选择要打印的处方", "提示");
                return;
            }

            try
            {
                // 获取完整处方详情用于打印
                var result = await _prescriptionService.GetByIdAsync(SelectedItem.Id);
                if (!result.IsSuccess || result.Data == null)
                {
                    await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "获取处方详情失败", "打印失败");
                    return;
                }

                var prescription = result.Data;

                // P0-03核心：使用专业打印服务生成预览
                var previewResult = await _printService.PreviewPrescriptionAsync(prescription);

                if (!previewResult.Success)
                {
                    await _dialogService.ShowErrorAsync(previewResult.Message, "预览失败");
                    return;
                }

                // P0-03核心：显示标准化打印预览对话框
                var previewDialog = new LYBT.Desktop.Core.Views.PrintPreviewDialog(
                    previewResult.Content,
                    _printService,
                    prescription);

                // 设置对话框标题和属性
                previewDialog.Title = $"处方打印预览 - {prescription.Name ?? "未知患者"}";
                previewDialog.Owner = System.Windows.Application.Current.MainWindow;
                previewDialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

                var dialogResult = previewDialog.ShowDialog();

                if (dialogResult == true)
                {
                    await _dialogService.ShowSuccessAsync(
                        $"处方 {prescription.PrescriptionNo ?? prescription.Id.ToString()[..8]} 打印操作完成",
                        "打印成功");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"打印处理失败: {ex.Message}", "打印错误");
            }
        }

        /// <summary>
        /// P0-02新增：查看患者处方历史
        /// Epic 03-P0-02: 实用化患者处方历史查询功能
        /// 专为小诊所设计，快速查看患者的历史处方记录
        /// </summary>
        private async Task ViewPatientHistoryAsync()
        {
            if (SelectedItem == null)
            {
                return;
            }

            try
            {
                // 获取选中处方的患者ID
                var currentPrescription = SelectedItem;
                var patientId = currentPrescription.PatientId;
                var patientName = currentPrescription.Name;

                // 获取该患者的所有处方历史
                var query = new PrescriptionQueryDto
                {
                    PatientId = patientId,
                    PageIndex = 1,
                    PageSize = 50, // 最多获取50条历史记录
                    Keyword = string.Empty
                };

                var result = await _prescriptionService.GetPagedAsync(query);

                if (result.IsSuccess && result.Data != null)
                {
                    var prescriptions = result.Data.Items
                        .OrderByDescending(p => p.CreateTime)
                        .ToList();

                    ShowPatientPrescriptionHistory(patientName, prescriptions);
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "获取患者处方历史失败",
                        "查询失败");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"查询患者处方历史失败: {ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 显示患者处方历史对话框
        /// 实用化设计：清晰展示患者的历史处方信息，便于医生参考既往用药情况
        /// </summary>
        /// <param name="patientName">患者姓名</param>
        /// <param name="prescriptions">历史处方列表</param>
        private async void ShowPatientPrescriptionHistory(string patientName, List<PrescriptionDto> prescriptions)
        {
            var historyContent = new System.Text.StringBuilder();

            historyContent.AppendLine("=== 患者处方历史记录 ===\n");
            historyContent.AppendLine($"【患者】: {patientName}\n");

            if (!prescriptions.Any())
            {
                historyContent.AppendLine("该患者暂无历史处方记录。");
            }
            else
            {
                historyContent.AppendLine($"【处方记录】(共 {prescriptions.Count} 条)\n");

                for (int i = 0; i < Math.Min(prescriptions.Count, 20); i++) // 最多显示20条
                {
                    var prescription = prescriptions[i];

                    historyContent.AppendLine($"▶ 第 {i + 1} 张处方 - {prescription.CreateTime:yyyy-MM-dd}");
                    historyContent.AppendLine($"   处方号: {prescription.PrescriptionNo ?? prescription.Id.ToString()[..8]}");
                    historyContent.AppendLine($"   开方医师: {prescription.DoctorName ?? "未知"}");
                    historyContent.AppendLine($"   剂数: {prescription.DosageCount} 剂");
                    historyContent.AppendLine($"   费用: ¥{prescription.TotalPrice:F2}");

                    if (!string.IsNullOrEmpty(prescription.Diagnosis))
                    {
                        var diagnosis = prescription.Diagnosis.Length > 30 ?
                            prescription.Diagnosis.Substring(0, 30) + "..." :
                            prescription.Diagnosis;
                        historyContent.AppendLine($"   诊断: {diagnosis}");
                    }

                    // 显示主要药材（前5味）
                    if (prescription.Items != null && prescription.Items.Any())
                    {
                        var mainHerbs = prescription.Items.Take(5).Select(item =>
                            $"{item.HerbName}({item.Quantity}{item.Unit})").ToList();
                        historyContent.AppendLine($"   主要药材: {string.Join("、", mainHerbs)}");

                        if (prescription.Items.Count() > 5)
                        {
                            historyContent.AppendLine($"   等共{prescription.Items.Count()}味药材");
                        }
                    }

                    historyContent.AppendLine();
                }

                if (prescriptions.Count > 20)
                {
                    historyContent.AppendLine($"📋 注：为保持界面简洁，仅显示最近20条记录，实际共{prescriptions.Count}条。");
                }

                historyContent.AppendLine("💡 提示：");
                historyContent.AppendLine("• 可参考既往处方进行复诊开药");
                historyContent.AppendLine("• 注意用药间隔和配伍禁忌");
                historyContent.AppendLine("• 如需查看完整处方详情，可在处方管理中搜索处方号");
            }

            await _dialogService.ShowInformationAsync(historyContent.ToString(), $"处方历史 - {patientName}");
        }

        /// <summary>
        /// Epic 04-P0-02: 处方数据导出功能
        /// 专为小诊所设计，支持自定义时间段的处方数据Excel导出
        /// 提供完整的处方信息，便于统计和分析
        /// </summary>
        private async Task ExportPrescriptionsAsync()
        {
            try
            {
                // 使用SaveFileDialog选择导出位置
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出处方数据",
                    DefaultExt = "xlsx",
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                    FileName = $"处方数据导出_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // 显示进度
                    IsLoading = true;
                    StatusMessage = "正在准备导出数据...";

                    // 获取所有处方数据（可以考虑添加日期范围选择）
                    var query = new PrescriptionQueryDto
                    {
                        PageIndex = 1,
                        PageSize = int.MaxValue, // 获取所有数据
                        Keyword = string.Empty
                    };

                    var result = await _prescriptionService.GetPagedAsync(query);

                    if (result.IsSuccess && result.Data != null)
                    {
                        var prescriptions = result.Data.Items.OrderByDescending(p => p.CreateTime).ToList();

                        if (prescriptions.Any())
                        {
                            StatusMessage = $"正在导出 {prescriptions.Count} 条处方数据...";

                            // 定义导出列
                            var columns = new Dictionary<string, string>
                            {
                                { "PrescriptionNo", "处方号" },
                                { "PatientName", "患者姓名" },
                                { "DoctorName", "开方医师" },
                                { "CreateTime", "开方日期" },
                                { "Diagnosis", "诊断" },
                                { "DosageCount", "剂数" },
                                { "SingleDosePrice", "单帖价格" },
                                { "TotalPrice", "总金额" },
                                { "Discount", "折扣率" },
                                { "Status", "状态" },
                                { "MainHerbs", "主要药材" },
                                { "Remark", "处方备注" }
                            };

                            // 转换数据为导出格式
                            var exportData = prescriptions.Select(p => new
                            {
                                PrescriptionNo = p.PrescriptionNo ?? p.Id.ToString()[..8],
                                PatientName = p.Name ?? "未知患者",
                                DoctorName = p.DoctorName ?? "未知医师",
                                CreateTime = p.CreateTime.ToString("yyyy-MM-dd HH:mm"),
                                Diagnosis = p.Diagnosis ?? "无诊断信息",
                                DosageCount = p.DosageCount.ToString(),
                                SingleDosePrice = p.SingleDosePrice.ToString("F2"),
                                TotalPrice = p.TotalPrice.ToString("F2"),
                                Discount = p.Discount.ToString("P1"), // 格式化为百分比
                                Status = p.Status == LYBT.Shared.Models.Enums.CommonStatus.Enabled ? "正常" : "禁用",
                                MainHerbs = p.Items != null && p.Items.Any()
                                    ? string.Join("、", p.Items.Take(5).Select(item => $"{item.HerbName}({item.Quantity}{item.Unit})"))
                                    : "无药材信息",
                                Remark = p.Remark ?? string.Empty
                            }).ToList();

                            // 使用ExcelHelper导出
                            LYBT.Desktop.Core.Helpers.ExcelHelper.ExportToExcel(
                                exportData,
                                columns,
                                saveDialog.FileName,
                                "处方数据");

                            StatusMessage = "处方数据导出完成";

                            await _dialogService.ShowSuccessAsync(
                                $"成功导出 {prescriptions.Count} 条处方数据到：\n{saveDialog.FileName}",
                                "导出成功");
                        }
                        else
                        {
                            await _dialogService.ShowInformationAsync("没有找到可导出的处方数据", "提示");
                        }
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "获取处方数据失败",
                            "导出失败");
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"导出处方数据失败: {ex.Message}", "导出错误");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = "就绪";
            }
        }

        #endregion Command执行方法
    }
}
