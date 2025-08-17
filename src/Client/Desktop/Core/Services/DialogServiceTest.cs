using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Models.Common;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 对话框服务测试类
    /// 用于验证新的自定义对话框系统功能
    /// </summary>
    public static class DialogServiceTest
    {
        /// <summary>
        /// 测试基础对话框功能
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        public static async Task TestBasicDialogsAsync(ICustomDialogService dialogService)
        {
            try
            {
                // 测试信息对话框
                await dialogService.ShowInformationAsync("这是一个信息对话框测试", "信息测试");

                // 测试确认对话框
                var confirmed = await dialogService.ShowConfirmationAsync("确认执行这个操作吗？", "确认测试");
                await dialogService.ShowInformationAsync($"用户选择: {(confirmed ? "确认" : "取消")}", "结果");

                // 测试输入对话框
                var input = await dialogService.ShowInputAsync("请输入您的姓名:", "输入测试", "默认名称");
                if (input != null)
                {
                    await dialogService.ShowSuccessAsync($"您输入的姓名是: {input}", "输入结果");
                }
                else
                {
                    await dialogService.ShowWarningAsync("用户取消了输入", "取消");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowErrorAsync($"测试过程中发生错误: {ex.Message}", "测试错误");
            }
        }

        /// <summary>
        /// 测试中药材选择对话框
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        public static async Task TestHerbSelectionDialogAsync(ICustomDialogService dialogService)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["Title"] = "选择处方中药材",
                    ["DefaultQuantity"] = 15
                };

                var result = await dialogService.ShowDialogAsync("HerbSelectionDialog", parameters);

                if (result.Result == true && result.Parameters.ContainsKey("SelectedHerb"))
                {
                    var selectedHerb = result.Parameters["SelectedHerb"];
                    var quantity = result.Parameters.ContainsKey("Quantity") ? result.Parameters["Quantity"] : "未知";
                    var unit = result.Parameters.ContainsKey("Unit") ? result.Parameters["Unit"] : "未知";

                    await dialogService.ShowSuccessAsync(
                        $"选择的中药材: {selectedHerb}\n用量: {quantity} {unit}", 
                        "选择结果");
                }
                else
                {
                    await dialogService.ShowInformationAsync("用户取消了选择", "取消选择");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowErrorAsync($"测试中药材选择对话框时发生错误: {ex.Message}", "测试错误");
            }
        }

        /// <summary>
        /// 测试验方选择对话框
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        public static async Task TestFormulaSelectionDialogAsync(ICustomDialogService dialogService)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["Title"] = "选择处方验方",
                    ["SearchKeyword"] = "感冒"
                };

                var result = await dialogService.ShowDialogAsync("FormulaSelectionDialog", parameters);

                if (result.Result == true && result.Parameters.ContainsKey("SelectedFormula"))
                {
                    var selectedFormula = result.Parameters["SelectedFormula"];
                    var formulaName = result.Parameters.ContainsKey("FormulaName") ? result.Parameters["FormulaName"] : "未知";
                    var composition = result.Parameters.ContainsKey("Composition") ? result.Parameters["Composition"] : "未知";

                    await dialogService.ShowSuccessAsync(
                        $"选择的验方: {formulaName}\n组成: {composition}", 
                        "选择结果");
                }
                else
                {
                    await dialogService.ShowInformationAsync("用户取消了选择", "取消选择");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowErrorAsync($"测试验方选择对话框时发生错误: {ex.Message}", "测试错误");
            }
        }

        /// <summary>
        /// 测试对话框注册功能
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        public static async Task TestDialogRegistrationAsync(ICustomDialogService dialogService)
        {
            try
            {
                var testDialogs = new[]
                {
                    "InputDialog",
                    "HerbSelectionDialog",
                    "FormulaSelectionDialog",
                    "ConfirmationDialog",
                    "NonExistentDialog"
                };

                foreach (var dialogName in testDialogs)
                {
                    var isRegistered = dialogService.IsDialogRegistered(dialogName);
                    var status = isRegistered ? "✅ 已注册" : "❌ 未注册";
                    System.Diagnostics.Debug.WriteLine($"对话框 '{dialogName}': {status}");
                }

                await dialogService.ShowInformationAsync("对话框注册状态检查完成，请查看输出窗口", "注册测试");
            }
            catch (Exception ex)
            {
                await dialogService.ShowErrorAsync($"测试对话框注册时发生错误: {ex.Message}", "测试错误");
            }
        }

        /// <summary>
        /// 快速验证对话框系统（用于系统启动时检查）
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        public static async Task<bool> QuickSystemValidationAsync(ICustomDialogService dialogService)
        {
            try
            {
                // 1. 检查服务可用性
                if (dialogService == null) return false;

                // 2. 检查核心对话框注册
                var coreDialogs = new[] { "InputDialog", "HerbSelectionDialog", "FormulaSelectionDialog" };
                foreach (var dialog in coreDialogs)
                {
                    if (!dialogService.IsDialogRegistered(dialog))
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ 核心对话框未注册: {dialog}");
                        return false;
                    }
                }

                System.Diagnostics.Debug.WriteLine("✅ 对话框系统快速验证通过");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 对话框系统验证失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行完整的对话框系统测试
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        public static async Task RunCompleteTestAsync(ICustomDialogService dialogService)
        {
            try
            {
                await dialogService.ShowInformationAsync("开始自定义对话框系统测试", "测试开始");

                // 1. 测试基础对话框
                var testBasic = await dialogService.ShowConfirmationAsync("测试基础对话框功能?", "测试选择");
                if (testBasic)
                {
                    await TestBasicDialogsAsync(dialogService);
                }

                // 2. 测试中药材选择对话框
                var testHerb = await dialogService.ShowConfirmationAsync("测试中药材选择对话框?", "测试选择");
                if (testHerb)
                {
                    await TestHerbSelectionDialogAsync(dialogService);
                }

                // 3. 测试验方选择对话框
                var testFormula = await dialogService.ShowConfirmationAsync("测试验方选择对话框?", "测试选择");
                if (testFormula)
                {
                    await TestFormulaSelectionDialogAsync(dialogService);
                }

                // 4. 测试对话框注册
                var testRegistration = await dialogService.ShowConfirmationAsync("测试对话框注册功能?", "测试选择");
                if (testRegistration)
                {
                    await TestDialogRegistrationAsync(dialogService);
                }

                await dialogService.ShowSuccessAsync("所有测试完成!", "测试完成");
            }
            catch (Exception ex)
            {
                await dialogService.ShowErrorAsync($"测试过程中发生错误: {ex.Message}", "测试失败");
            }
        }
    }
}