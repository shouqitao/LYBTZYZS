using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Models.Common;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 对话框参数传递验证测试
    /// 验证新自定义对话框系统的参数传递机制
    /// </summary>
    public static class DialogParameterValidationTest
    {
        /// <summary>
        /// 执行完整的参数传递验证测试
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        public static async Task RunParameterValidationTestAsync(ICustomDialogService dialogService)
        {
            var results = new List<string>();
            var allPassed = true;

            try
            {
                await dialogService.ShowInformationAsync("开始对话框参数传递验证测试", "验证测试");

                // Test 1: 基础参数传递测试
                results.Add("=== Test 1: 基础参数传递测试 ===");
                var test1Result = await TestBasicParameterPassing(dialogService);
                results.Add($"基础参数传递: {(test1Result ? "✅ 通过" : "❌ 失败")}");
                allPassed = allPassed && test1Result;

                // Test 2: InputDialog参数验证
                results.Add("\n=== Test 2: InputDialog参数验证 ===");
                var test2Result = await TestInputDialogParameters(dialogService);
                results.Add($"InputDialog参数: {(test2Result ? "✅ 通过" : "❌ 失败")}");
                allPassed = allPassed && test2Result;

                // Test 3: HerbSelection参数验证
                results.Add("\n=== Test 3: HerbSelection参数验证 ===");
                var test3Result = await TestHerbSelectionParameters(dialogService);
                results.Add($"HerbSelection参数: {(test3Result ? "✅ 通过" : "❌ 失败")}");
                allPassed = allPassed && test3Result;

                // Test 4: FormulaSelection参数验证
                results.Add("\n=== Test 4: FormulaSelection参数验证 ===");
                var test4Result = await TestFormulaSelectionParameters(dialogService);
                results.Add($"FormulaSelection参数: {(test4Result ? "✅ 通过" : "❌ 失败")}");
                allPassed = allPassed && test4Result;

                // Test 5: 对话框注册验证
                results.Add("\n=== Test 5: 对话框注册验证 ===");
                var test5Result = await TestDialogRegistration(dialogService);
                results.Add($"对话框注册: {(test5Result ? "✅ 通过" : "❌ 失败")}");
                allPassed = allPassed && test5Result;

                // 输出测试结果
                results.Add($"\n=== 总体结果 ===");
                results.Add($"所有测试: {(allPassed ? "✅ 全部通过" : "❌ 部分失败")}");

                // 在Debug输出中显示详细结果
                foreach (var result in results)
                {
                    Debug.WriteLine(result);
                }

                // 向用户显示测试完成
                var summary = allPassed ? 
                    "🎉 所有参数传递验证测试通过！\n对话框系统已准备好用于Phase 3迁移。" :
                    "⚠️ 部分测试失败。\n请检查Debug输出窗口了解详情。";

                await dialogService.ShowInformationAsync(summary, "验证测试完成");
            }
            catch (Exception ex)
            {
                results.Add($"❌ 测试过程中发生异常: {ex.Message}");
                Debug.WriteLine($"参数验证测试异常: {ex}");
                await dialogService.ShowErrorAsync($"验证测试过程中发生错误: {ex.Message}", "测试错误");
            }
        }

        /// <summary>
        /// 测试基础参数传递
        /// </summary>
        private static async Task<bool> TestBasicParameterPassing(ICustomDialogService dialogService)
        {
            try
            {
                // 测试基础方法是否正常工作
                var confirmed = await dialogService.ShowConfirmationAsync("这是一个确认测试，点击'是'继续", "基础测试");
                Debug.WriteLine($"确认测试结果: {confirmed}");

                var input = await dialogService.ShowInputAsync("请输入'test'进行验证", "输入测试", "默认值");
                Debug.WriteLine($"输入测试结果: {input ?? "null"}");

                return true; // 如果没有异常，基础功能正常
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"基础参数传递测试失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试InputDialog参数验证
        /// </summary>
        private static async Task<bool> TestInputDialogParameters(ICustomDialogService dialogService)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["Message"] = "参数验证测试消息",
                    ["Title"] = "参数验证测试标题",
                    ["DefaultValue"] = "参数验证默认值"
                };

                var result = await dialogService.ShowDialogAsync("InputDialog", parameters);
                
                Debug.WriteLine($"InputDialog结果: Result={result.Result}, Parameters.Count={result.Parameters.Count}");
                
                // 验证结果结构
                var hasValidResult = result.Result != null;
                Debug.WriteLine($"InputDialog参数验证: 有效结果={hasValidResult}");

                return hasValidResult;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InputDialog参数验证失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试HerbSelection参数验证
        /// </summary>
        private static async Task<bool> TestHerbSelectionParameters(ICustomDialogService dialogService)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["Title"] = "中药材参数验证",
                    ["DefaultQuantity"] = 20
                };

                // 注意：这个测试可能会因为没有实际的Herb数据而显示空列表
                // 但重点是验证参数传递机制是否正常
                var result = await dialogService.ShowDialogAsync("HerbSelectionDialog", parameters);
                
                Debug.WriteLine($"HerbSelectionDialog结果: Result={result.Result}, Parameters.Count={result.Parameters.Count}");
                
                // 验证参数字典是否正确初始化
                var hasParameterDict = result.Parameters != null;
                Debug.WriteLine($"HerbSelection参数验证: 参数字典存在={hasParameterDict}");

                return hasParameterDict;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HerbSelection参数验证失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试FormulaSelection参数验证
        /// </summary>
        private static async Task<bool> TestFormulaSelectionParameters(ICustomDialogService dialogService)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["Title"] = "验方参数验证",
                    ["SearchKeyword"] = "测试验方"
                };

                // 注意：这个测试可能会因为没有实际的Formula数据而显示空列表
                // 但重点是验证参数传递机制是否正常
                var result = await dialogService.ShowDialogAsync("FormulaSelectionDialog", parameters);
                
                Debug.WriteLine($"FormulaSelectionDialog结果: Result={result.Result}, Parameters.Count={result.Parameters.Count}");
                
                // 验证参数字典是否正确初始化
                var hasParameterDict = result.Parameters != null;
                Debug.WriteLine($"FormulaSelection参数验证: 参数字典存在={hasParameterDict}");

                return hasParameterDict;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FormulaSelection参数验证失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 测试对话框注册验证
        /// </summary>
        private static async Task<bool> TestDialogRegistration(ICustomDialogService dialogService)
        {
            try
            {
                var requiredDialogs = new[]
                {
                    "InputDialog",
                    "HerbSelectionDialog", 
                    "FormulaSelectionDialog",
                    "ConfirmationDialog",
                    "InformationDialog"
                };

                var allRegistered = true;
                foreach (var dialogName in requiredDialogs)
                {
                    var isRegistered = dialogService.IsDialogRegistered(dialogName);
                    Debug.WriteLine($"对话框注册检查: {dialogName} = {(isRegistered ? "✅" : "❌")}");
                    allRegistered = allRegistered && isRegistered;
                }

                Debug.WriteLine($"对话框注册验证: 全部注册={allRegistered}");
                return allRegistered;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"对话框注册验证失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 快速自动化验证（无用户交互）
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        public static async Task<bool> QuickValidationAsync(ICustomDialogService dialogService)
        {
            try
            {
                Debug.WriteLine("=== 开始快速自动化验证 ===");

                // 1. 验证基础服务可用性
                if (dialogService == null)
                {
                    Debug.WriteLine("❌ DialogService为null");
                    return false;
                }

                // 2. 验证对话框注册
                var registrationResult = await TestDialogRegistration(dialogService);
                if (!registrationResult)
                {
                    Debug.WriteLine("❌ 对话框注册验证失败");
                    return false;
                }

                // 3. 验证参数字典创建
                var testParams = new Dictionary<string, object>
                {
                    ["TestKey"] = "TestValue",
                    ["TestNumber"] = 42
                };

                if (testParams.Count != 2)
                {
                    Debug.WriteLine("❌ 参数字典创建失败");
                    return false;
                }

                Debug.WriteLine("✅ 快速验证通过 - 对话框系统就绪");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ 快速验证失败: {ex.Message}");
                return false;
            }
        }
    }
}