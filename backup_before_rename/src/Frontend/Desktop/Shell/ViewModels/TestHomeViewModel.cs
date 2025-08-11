using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Prism.Events;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Shell.ViewModels
{
    /// <summary>
    /// 测试用HomeViewModel - 逐步添加依赖以找出问题
    /// </summary>
    public class TestHomeViewModel : BindableBase
    {
        private string _title = "测试主页 - DI调试";
        private string _debugInfo = "开始测试依赖注入...";

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string DebugInfo
        {
            get => _debugInfo;
            set => SetProperty(ref _debugInfo, value);
        }

        // 逐步添加HomeViewModel的所有依赖
        public TestHomeViewModel(
            IRegionManager regionManager,
            IAuthenticationService authService,
            IUserSessionManager userSessionManager,
            IMedicalCaseService medicalCaseService,
            ICommonDialogService dialogService,
            IEventAggregator eventAggregator,
            ILogger<TestHomeViewModel> logger)
        {
            AddDebugInfo("=== 开始DI测试 ===");
            
            if (regionManager != null)
                AddDebugInfo("✅ IRegionManager 注入成功");
            else
                AddDebugInfo("❌ IRegionManager 为 null");
                
            if (authService != null)
                AddDebugInfo("✅ IAuthenticationService 注入成功");
            else
                AddDebugInfo("❌ IAuthenticationService 为 null");
                
            if (userSessionManager != null)
                AddDebugInfo("✅ IUserSessionManager 注入成功");
            else
                AddDebugInfo("❌ IUserSessionManager 为 null");
                
            if (medicalCaseService != null)
                AddDebugInfo("✅ IMedicalCaseService 注入成功");
            else
                AddDebugInfo("❌ IMedicalCaseService 为 null");
                
            if (dialogService != null)
                AddDebugInfo("✅ ICommonDialogService 注入成功");
            else
                AddDebugInfo("❌ ICommonDialogService 为 null");
                
            if (eventAggregator != null)
                AddDebugInfo("✅ IEventAggregator 注入成功");
            else
                AddDebugInfo("❌ IEventAggregator 为 null");
            
            if (logger != null)
                AddDebugInfo("✅ ILogger<TestHomeViewModel> 注入成功");
            else
                AddDebugInfo("❌ ILogger<TestHomeViewModel> 为 null");
                
            AddDebugInfo("=== DI测试完成 ===");
            AddDebugInfo("如果所有服务都显示✅，说明DI配置正确");
        }

        private void AddDebugInfo(string info)
        {
            DebugInfo += $"\n{DateTime.Now:HH:mm:ss} - {info}";
        }
    }
}