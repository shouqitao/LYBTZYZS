using System.IO;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Utilities.Excel;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.ViewModels.Handlers;

/// <summary>
/// 用户导入导出处理实现
/// OpenSpec: refactor-frontend-srp-patterns - Handler提取模式
/// </summary>
public class UserImportExportHandler : IUserImportExportHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ICommonDialogService _commonDialogService;
    private readonly IMasterDetailServices<UserListDto, UserDetailModel> _masterDetailServices;
    private readonly ILogger<UserImportExportHandler> _logger;

    public UserImportExportHandler(
        IUserRepository userRepository,
        ICommonDialogService commonDialogService,
        IMasterDetailServices<UserListDto, UserDetailModel> masterDetailServices,
        ILogger<UserImportExportHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _commonDialogService = commonDialogService ?? throw new ArgumentNullException(nameof(commonDialogService));
        _masterDetailServices = masterDetailServices ?? throw new ArgumentNullException(nameof(masterDetailServices));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> ImportAsync()
    {
        try
        {
            var filePath = await _commonDialogService.ShowOpenFileDialogAsync(
                filter: "Excel文件|*.xlsx",
                title: "选择用户导入文件");
            if (string.IsNullOrEmpty(filePath)) return false;

            using var fileStream = File.OpenRead(filePath);
            var users = await ExcelHelper.ParseAsync<UserInputDto>(fileStream, hasHeader: true);
            if (users == null || users.Count == 0)
            {
                await _commonDialogService.ShowErrorAsync("文件中没有有效的用户数据", "导入用户");
                return false;
            }

            var request = new UserBatchImportInputDto
            {
                Users = users,
                Strategy = DuplicateStrategy.Skip
            };
            var result = await _userRepository.BatchImportAsync(request);
            if (result == null)
            {
                await _commonDialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入用户");
                return false;
            }

            var message = $"导入完成！\n成功：{result.SuccessCount}条\n失败：{result.FailureCount}条\n跳过：{result.SkippedCount}条\n成功率：{result.SuccessRate:F1}%";
            if (result.FailureCount > 0)
            {
                message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：";
                foreach (var f in result.Failures.Take(3))
                    message += $"\n第{f.OriginalRowNumber}行 [{f.UserName}]：{f.FailureReason}";
            }
            await _commonDialogService.ShowInfoAsync(message, "导入结果");
            return result.SuccessCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入用户失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("导入用户失败", "操作失败");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task ExportAsync(string? searchText)
    {
        try
        {
            var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                filter: "Excel文件|*.xlsx",
                title: "导出用户数据",
                defaultFileName: $"用户数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            if (string.IsNullOrEmpty(filePath)) return;

            var allUsers = await _userRepository.SearchAsync(searchText ?? string.Empty);
            if (allUsers == null || allUsers.Count == 0)
            {
                await _commonDialogService.ShowErrorAsync("没有可导出的数据", "导出用户");
                return;
            }

            await ExcelHelper.ExportAsync(allUsers, filePath, "用户数据");
            await _commonDialogService.ShowInfoAsync($"成功导出{allUsers.Count}条用户数据到：\n{filePath}", "导出成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出用户失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("导出用户失败", "操作失败");
        }
    }

    /// <inheritdoc/>
    public async Task DownloadTemplateAsync()
    {
        try
        {
            var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                filter: "Excel文件|*.xlsx",
                title: "保存用户导入模板",
                defaultFileName: $"用户导入模板_{DateTime.Now:yyyyMMdd}.xlsx");
            if (string.IsNullOrEmpty(filePath)) return;

            var sampleData = new List<UserInputDto>
            {
                new() { UserName = "doctor001", RealName = "张医生", PhoneNumber = "13800138000", Email = "doctor001@example.com", Role = UserRole.Doctor },
                new() { UserName = "admin001", RealName = "李管理员", PhoneNumber = "13800138001", Email = "admin001@example.com", Role = UserRole.Admin }
            };

            await ExcelHelper.GenerateTemplateAsync(filePath, "用户导入模板", sampleData);
            await _commonDialogService.ShowInfoAsync(
                $"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入用户」功能导入。\n\n注意：\n1. 用户名必须唯一\n2. 角色可选值：Admin(管理员)、Doctor(医生)、Nurse(护士)\n3. 新创建用户默认为启用状态",
                "下载成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载模板失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("下载模板失败", "操作失败");
        }
    }
}
