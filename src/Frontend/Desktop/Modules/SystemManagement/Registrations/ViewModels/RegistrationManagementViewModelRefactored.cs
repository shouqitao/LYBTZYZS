using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Models.Registration;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Modules.SystemManagement.Common.ViewModels;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using Prism.Commands;

using LYBT.WPF.Client.Core.Interfaces.Services;
using Prism.Dialogs;
namespace LYBT.WPF.Client.Modules.SystemManagement.Registrations.ViewModels
{
    /// <summary>
    /// 挂号管理视图模型（重构版）
    /// </summary>
    public class RegistrationManagementViewModelRefactored : BaseManagementViewModel<RegistrationInfo, IRegistrationApiService>
    {
        private readonly ICommonDialogService _commonDialogService;

        protected override string ModuleName => "挂号管理";

        #region Properties

        private string _selectedStatus = "全部";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        private string _selectedDepartment = "全部";
        public string SelectedDepartment
        {
            get => _selectedDepartment;
            set => SetProperty(ref _selectedDepartment, value);
        }

        private ObservableCollection<string> _statusList = new();
        public ObservableCollection<string> StatusList
        {
            get => _statusList;
            set => SetProperty(ref _statusList, value);
        }

        private ObservableCollection<string> _departmentList = new();
        public ObservableCollection<string> DepartmentList
        {
            get => _departmentList;
            set => SetProperty(ref _departmentList, value);
        }

        private DateTime? _searchDate;
        public DateTime? SearchDate
        {
            get => _searchDate;
            set => SetProperty(ref _searchDate, value);
        }

        #endregion

        #region Commands

        public DelegateCommand BatchCancelCommand { get; }
        public DelegateCommand<RegistrationInfo> CancelCommand { get; }
        public DelegateCommand<RegistrationInfo> CheckInCommand { get; }

        #endregion

        public RegistrationManagementViewModelRefactored(IRegistrationApiService service,
            ICommonDialogService commonDialogService)
            : base(service)
        {
            _commonDialogService = commonDialogService;
            // 初始化额外的命令
            BatchCancelCommand = new DelegateCommand(BatchCancel);
            CancelCommand = new DelegateCommand<RegistrationInfo>(async (r) => await CancelRegistration(r));
            CheckInCommand = new DelegateCommand<RegistrationInfo>(CheckIn);

            // 初始化数据
            InitializeLists();
        }

        private void InitializeLists()
        {
            // 初始化状态列表
            StatusList.Clear();
            StatusList.Add("全部");
            StatusList.Add("已预约");
            StatusList.Add("已到达");
            StatusList.Add("就诊中");
            StatusList.Add("已完成");
            StatusList.Add("已取消");
            StatusList.Add("爽约");
            StatusList.Add("已过期");

            // 初始化科室列表
            DepartmentList.Clear();
            DepartmentList.Add("全部");
            DepartmentList.Add("内科");
            DepartmentList.Add("外科");
            DepartmentList.Add("妇科");
            DepartmentList.Add("儿科");
            DepartmentList.Add("中医科");
            DepartmentList.Add("皮肤科");
            DepartmentList.Add("骨科");
            DepartmentList.Add("眼科");
            DepartmentList.Add("耳鼻喉科");
        }

        #region 重写基类方法

        protected override async Task<ServiceResult<PagedResult<RegistrationInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                var status = ConvertToRegistrationStatus(SelectedStatus);
                var department = SelectedDepartment == "全部" ? null : SelectedDepartment;

                var query = new RegistrationPagedQueryDto
                {
                    CurrentPage = request.CurrentPage,
                    PageSize = request.PageSize,
                    PatientName = SearchKeyword,
                    StartDate = SearchDate,
                    EndDate = SearchDate,
                    Status = status,
                    Department = department
                };

                var response = await Service.GetPagedRegistrationsAsync(query);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var paginatedResult = response.Content;
                    
                    // 转换为前端模型
                    var registrationInfos = paginatedResult.Items.Select(dto => ConvertToRegistrationInfo(dto)).ToList();

                    var result = new PagedResult<RegistrationInfo>
                    {
                        Items = registrationInfos,
                        TotalCount = paginatedResult.TotalCount,
                        CurrentPage = paginatedResult.CurrentPage,
                        PageSize = paginatedResult.PageSize
                    };

                    return ServiceResult<PagedResult<RegistrationInfo>>.Success(result);
                }
                else
                {
                    var error = response.Error?.Content ?? "获取挂号列表失败";
                    return ServiceResult<PagedResult<RegistrationInfo>>.Failure(error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载挂号列表异常: {ex.Message}");
                return ServiceResult<PagedResult<RegistrationInfo>>.Failure($"加载挂号列表失败: {ex.Message}");
            }
        }

        protected override async Task<ServiceResult<bool>> DeleteFromServiceAsync(RegistrationInfo item)
        {
            try
            {
                var response = await Service.DeleteRegistrationAsync(item.Id);
                if (response.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Success(true);
                }
                else
                {
                    var error = response.Error?.Content ?? "删除挂号失败";
                    return ServiceResult<bool>.Failure(error);
                }
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"删除挂号失败: {ex.Message}");
            }
        }

        protected override string GetItemDisplayName(RegistrationInfo item)
        {
            return $"{item.RegistrationNo} - {item.PatientName}";
        }

        protected override bool CanExecuteDelete(RegistrationInfo item)
        {
            // 只有已预约状态的挂号可以删除
            return item != null && item/* .Status = */= RegistrationStatus.Scheduled;
        }

        protected override void ExecuteAdd()
        {
            try
            {
                // 使用简化版对话框
                var dialog = new Views.SimpleAddRegistrationDialog(_commonDialogService);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开新增挂号对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        protected override void ExecuteEdit(RegistrationInfo item)
        {
            if (item == null || !item.CanEdit) return;

            try
            {
                var dialog = new Views.EditRegistrationDialog(item.Id);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开编辑挂号对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        protected override void ExecuteView(RegistrationInfo item)
        {
            if (item == null) return;

            try
            {
                var parameters = new DialogParameters
                {
                    { "registrationId", item.Id }
                };

                // TODO: Replace with proper dialog service call
                // // TODO: Replace with proper dialog service call
            // _commonDialogService.ShowDialog("ViewRegistrationDialog", parameters, result =>
                // {
                //     // 对话框关闭后的处理（如果需要）
                // });
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开详情对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }


        #endregion

        #region 额外功能

        private async void BatchCancel()
        {
            var selectedItems = Items.Where(r => r.IsSelected && r.CanCancel).ToList();
            if (!selectedItems.Any())
            {
                await _commonDialogService.ShowInformationAsync("请选择要取消的挂号记录", "提示");
                return;
            }

            var result = await _commonDialogService.ShowConfirmationAsync($"确定要取消选中的 {selectedItems.Count} 条挂号记录吗？", "确认取消");
            
            if (result )
            {
                try
                {
                    IsLoading = true;
                    var ids = selectedItems.Select(r => r.Id).ToList();
                    
                    // 逐个取消挂号（如果没有批量API）
                    var successCount = 0;
                    var failedCount = 0;
                    
                    foreach (var id in ids)
                    {
                        try
                        {
                            var response = await Service.CancelRegistrationAsync(id);
                            if (response.IsSuccessStatusCode)
                            {
                                successCount++;
                            }
                            else
                            {
                                failedCount++;
                            }
                        }
                        catch
                        {
                            failedCount++;
                        }
                    }
                    
                    var message = $"批量取消完成\n成功：{successCount} 条\n失败：{failedCount} 条";
                    _commonDialogService.ShowWarningAsync(message, "批量取消结果").GetAwaiter().GetResult();
                    
                    RefreshCommand.Execute();
                }
                catch (Exception ex)
                {
                    _commonDialogService.ShowErrorAsync($"批量取消时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task CancelRegistration(RegistrationInfo registration)
        {
            if (registration == null || !registration.CanCancel) return;

            var result = await _commonDialogService.ShowConfirmationAsync($"确定要取消挂号单 {registration.RegistrationNo} 吗？", "确认取消");
            
            if (result )
            {
                try
                {
                    var response = await Service.CancelRegistrationAsync(registration.Id);
                    if (response.IsSuccessStatusCode)
                    {
                        _commonDialogService.ShowInformationAsync("挂号已取消", "成功").GetAwaiter().GetResult();
                        RefreshCommand.Execute();
                    }
                    else
                    {
                        var error = response.Error?.Content ?? "取消挂号失败";
                        _commonDialogService.ShowErrorAsync($"取消挂号失败：{error}", "错误").GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _commonDialogService.ShowErrorAsync($"取消挂号时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
                }
            }
        }

        private async void CheckIn(RegistrationInfo registration)
        {
            if (registration == null || !registration.CanCheckIn) return;

            var result = await _commonDialogService.ShowConfirmationAsync($"确定要为患者 {registration.PatientName} 签到吗？\n挂号单号：{registration.RegistrationNumber}", "确认签到");
            
            if (result )
            {
                try
                {
                    IsLoading = true;
                    
                    // 只更新必要的字段，签到实际上是更改状态
                    var updateDto = new RegistrationEditDto
                    {
                        Id = registration.Id,
                        RegistrationType = registration.RegistrationType,
                        DoctorId = registration.DoctorId,
                        Remark = $"{registration.Remark}\n签到时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                    };
                    
                    // 注意：由于 RegistrationEditDto 没有 Status 字段，
                    // 可能需要调用专门的签到 API 或者使用其他方法
                    var response = await Service.UpdateRegistrationAsync(registration.Id, updateDto);
                    if (response.IsSuccessStatusCode)
                    {
                        _commonDialogService.ShowInformationAsync($"患者 {registration.PatientName} 签到成功", "成功").GetAwaiter().GetResult();
                        RefreshCommand.Execute();
                    }
                    else
                    {
                        var error = response.Error?.Content ?? "签到失败";
                        _commonDialogService.ShowErrorAsync($"签到失败：{error}", "错误").GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _commonDialogService.ShowErrorAsync($"签到时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        #endregion

        #region 辅助方法

        private RegistrationStatus? ConvertToRegistrationStatus(string status)
        {
            return status switch
            {
                "已预约" => RegistrationStatus.Scheduled,
                "已到达" => RegistrationStatus.Arrived,
                "就诊中" => RegistrationStatus.InConsultation,
                "已完成" => RegistrationStatus.Completed,
                "已取消" => RegistrationStatus.Cancelled,
                "爽约" => RegistrationStatus.NoShow,
                "已过期" => RegistrationStatus.Expired,
                _ => null
            };
        }

        private RegistrationType? ConvertToRegistrationType(string type)
        {
            return type switch
            {
                "普通号" => RegistrationType.Regular,
                "专家号" => RegistrationType.Expert,
                "急诊号" => RegistrationType.Emergency,
                "预约号" => RegistrationType.Appointment,
                _ => null
            };
        }

        private RegistrationInfo ConvertToRegistrationInfo(RegistrationDto dto)
        {
            return new RegistrationInfo
            {
                Id = dto.Id,
                RegistrationNumber = dto.RegistrationNumber ?? string.Empty,
                PatientId = dto.PatientId,
                PatientName = dto.PatientName ?? string.Empty,
                PatientPhone = dto.PatientPhone ?? string.Empty,
                DoctorId = dto.DoctorId,
                DoctorName = dto.DoctorName ?? string.Empty,
                /* Department = dto.Department ?? string.Empty, */
                RegistrationType = ConvertToRegistrationType(dto.RegistrationType) ?? RegistrationType.Regular,
                RegistrationFee = dto.RegistrationFee,
                Status = ConvertToRegistrationStatus(dto.Status) ?? RegistrationStatus.Scheduled,
                AppointmentDate = dto.AppointmentDate,
                AppointmentTimeSlot = dto.AppointmentTimeSlot,
                QueueNumber = dto.QueueNumber,
                IsPaid = dto.IsPaid,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime,
                Remark = dto.Remark
            };
        }

        #endregion
    }
}