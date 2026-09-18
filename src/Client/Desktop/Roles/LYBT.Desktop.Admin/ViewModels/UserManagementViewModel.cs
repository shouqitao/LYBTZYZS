using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Enums;
using Prism.Regions;

namespace LYBT.Desktop.Admin.ViewModels;

/// <summary>
/// 用户管理薄包装导航 ViewModel
/// 消费 DefaultRoleFilter 导航参数，并向嵌入的 UserMasterDetailControl 广播
/// （设计：参数消费在 VM.OnNavigatedTo，View 仅做 UI 转发）
/// </summary>
public class UserManagementViewModel : NavigableViewModelBase
{
    /// <summary>导航参数键：DefaultRoleFilter</summary>
    public const string DefaultRoleFilterKey = "DefaultRoleFilter";

    private UserRole? _defaultRoleFilter;

    /// <summary>目标角色筛选（导航参数消费结果）</summary>
    public UserRole? DefaultRoleFilter
    {
        get => _defaultRoleFilter;
        private set
        {
            if (SetProperty(ref _defaultRoleFilter, value) && value.HasValue)
                DefaultRoleFilterApplied?.Invoke(value.Value);
        }
    }

    /// <summary>角色筛选就绪事件（View 订阅后转发给 UserMasterDetailControl）</summary>
    public event Action<UserRole>? DefaultRoleFilterApplied;

    public UserManagementViewModel(IViewModelServices services)
        : base(services)
    {
        PageTitle = "用户管理";
    }

    protected override void OnNavigatedToCore(NavigationContext context)
    {
        if (context.Parameters.TryGetValue(DefaultRoleFilterKey, out UserRole role))
        {
            DefaultRoleFilter = role;
            return;
        }

        // NavigationParameters 可能装箱为 object/int
        if (context.Parameters.ContainsKey(DefaultRoleFilterKey))
        {
            var raw = context.Parameters[DefaultRoleFilterKey];
            if (raw is UserRole boxed)
                DefaultRoleFilter = boxed;
            else if (raw is not null && Enum.TryParse(raw.ToString(), out UserRole parsed))
                DefaultRoleFilter = parsed;
        }
    }
}
