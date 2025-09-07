using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Services
{
    // 以下是占位服务实现，待后续完善
    // 注意：UserService, PatientService, ConsultationService, HerbService 已移至独立文件

    /// <summary>
    /// 导航服务占位实现
    /// </summary>
    public class NavigationService : INavigationService
    {

        public Task NavigateToAsync(string viewName)
        {
            return Task.CompletedTask;
        }

        public Task NavigateToAsync(string viewName, object parameters)
        {
            return Task.CompletedTask;
        }

        public Task GoBackAsync()
        {
            return Task.CompletedTask;
        }

        public bool CanGoBack => false;
    }
}
