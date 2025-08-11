using System.Threading.Tasks;
using Prism.Navigation.Regions;
using LYBT.Desktop.Workbench.Core;

namespace LYBT.Desktop.Workbench.Admin.Services
{
    /// <summary>
    /// 系统管理工作台导航器接口
    /// </summary>
    public interface ISystemWorkbenchNavigator : IWorkbenchNavigator
    {
        /// <summary>
        /// 导航到用户管理
        /// </summary>
        Task NavigateToUsersAsync();

        /// <summary>
        /// 导航到患者管理
        /// </summary>
        Task NavigateToPatientsAsync();

        /// <summary>
        /// 导航到药材管理
        /// </summary>
        Task NavigateToHerbsAsync();

        /// <summary>
        /// 导航到验方管理
        /// </summary>
        Task NavigateToFormulasAsync();

        /// <summary>
        /// 导航到处方管理
        /// </summary>
        Task NavigateToPrescriptionsAsync();

        /// <summary>
        /// 导航到报表统计
        /// </summary>
        Task NavigateToReportsAsync();

        /// <summary>
        /// 导航到系统设置
        /// </summary>
        Task NavigateToSettingsAsync();

        /// <summary>
        /// 导航到仪表板
        /// </summary>
        Task NavigateToDashboardAsync();
    }
}