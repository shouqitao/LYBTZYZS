using System.IO;
using LYBT.Desktop.Workbench.Admin.Services;
using LYBT.Desktop.Workbench.Admin.ViewModels;
using LYBT.Desktop.Workbench.Admin.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Workbench.Admin
{

    /// <summary>
    /// 系统管理工作台模块
    /// 为管理员提供统一的管理界面
    /// </summary>
    public class SystemWorkbenchModule : IModule
    {

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 注册ViewModel映射
            ViewModelLocationProvider.Register<SystemWorkbenchMainView, SystemWorkbenchMainViewModel>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册工作台导航器
            containerRegistry.RegisterSingleton<ISystemWorkbenchNavigator, SystemWorkbenchNavigator>();

            // 注册主视图（必须成功）- 明确指定导航名称
            containerRegistry.RegisterForNavigation<SystemWorkbenchMainView>("SystemWorkbenchMainView");

            System.Diagnostics.Debug.WriteLine("✅ SystemWorkbench主视图注册完成");

            var diagnosticPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LYBT_Navigation_Debug.txt");
            File.AppendAllText(diagnosticPath, "=== SystemWorkbench模块视图注册开始 ===" + Environment.NewLine);

            // 业务模块视图配置 - 修复为实际存在的View名称
            var viewRegistrations = new Dictionary<string, string>
            {
                ["UserManagementView"] = "LYBT.Desktop.Users.Views.UserManagementView, LYBT.Desktop.Users",
                ["PatientManagementView"] = "LYBT.Desktop.Patients.Views.PatientManagementView, LYBT.Desktop.Patients",
                ["MedicalCaseListView"] = "LYBT.Desktop.MedicalCase.Views.MedicalCaseListView, LYBT.Desktop.MedicalCase", // 修复：使用实际存在的View
                ["ConsultationMainView"] = "LYBT.Desktop.Consultation.Views.ConsultationMainView, LYBT.Desktop.Consultation", // 修复：使用实际存在的View
                ["HerbManagementView"] = "LYBT.Desktop.Herbs.Views.HerbManagementView, LYBT.Desktop.Herbs",
                ["FormulaManagementView"] = "LYBT.Desktop.Formula.Views.FormulaManagementView, LYBT.Desktop.Formula",
                ["PrescriptionManagementView"] = "LYBT.Desktop.Prescriptions.Views.PrescriptionManagementView, LYBT.Desktop.Prescriptions"
            };

            int successCount = 0;
            int failureCount = 0;

            // UltraThink v2.0: 显式注册业务模块视图用于SystemWorkbench导航
            // 这些视图由业务模块定义，但需要在SystemWorkbench中可导航
            foreach (var kvp in viewRegistrations)
            {
                try
                {
                    var viewType = Type.GetType(kvp.Value);
                    if (viewType != null)
                    {
                        containerRegistry.RegisterForNavigation(viewType, kvp.Key);
                        var successMsg = $"✅ 成功注册SystemWorkbench视图: {kvp.Key} -> {viewType.FullName}";
                        System.Diagnostics.Debug.WriteLine(successMsg);
                        File.AppendAllText(diagnosticPath, successMsg + Environment.NewLine);
                        successCount++;
                    }
                    else
                    {
                        var warningMsg = $"⚠️ 视图类型未找到: {kvp.Key} -> {kvp.Value}";
                        System.Diagnostics.Debug.WriteLine(warningMsg);
                        File.AppendAllText(diagnosticPath, warningMsg + Environment.NewLine);
                        failureCount++;
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"❌ SystemWorkbench视图注册异常 {kvp.Key}: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine(errorMsg);
                    File.AppendAllText(diagnosticPath, errorMsg + Environment.NewLine);
                    failureCount++;
                }
            }

            var summaryMsg = $"🎯 SystemWorkbench视图注册统计: 成功 {successCount}, 失败 {failureCount}";
            System.Diagnostics.Debug.WriteLine(summaryMsg);
            File.AppendAllText(diagnosticPath, summaryMsg + Environment.NewLine);
            File.AppendAllText(diagnosticPath, "=== SystemWorkbench模块视图注册结束 ===" + Environment.NewLine);

            // 即使有部分视图注册失败，也不影响工作台主视图的正常工作
            if (failureCount > 0)
            {
                System.Diagnostics.Debug.WriteLine("💡 提示: 部分业务模块视图注册失败，但工作台主视图仍可正常使用");
            }
        }
    }
}
