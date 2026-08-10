using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Users.Mappers;
using LYBT.Desktop.Users.Models.Items;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户编辑子视图模型 - 对象DP模式
    ///
    /// 封装用户编辑状态，提供初始化、验证、数据提取等功能
    /// 由 UserMasterDetailViewModel 组合使用
    /// D2: 改用 Mapperly UserMapper，消除手写字段映射
    /// D3: 基类对齐 Patients/Catalog——ObservableObject → EditorViewModelBase&lt;UserEditContext&gt;
    ///     （IsDirty/生命周期/上下文订阅复用；保留缓存联动：Reset 后失效用户缓存）
    /// </summary>
    public partial class UserEditorViewModel : EditorViewModelBase<UserEditContext>
    {
        private readonly IDesktopCacheManager _cacheManager;
        private readonly UserMapper _mapper;

        private UserEditContext _user = UserEditContext.CreateNew();

        /// <summary>用户编辑上下文 (XAML 绑定目标)</summary>
        public UserEditContext User
        {
            get => _user;
            set => SetProperty(ref _user, value);
        }

        public UserEditorViewModel(IDesktopCacheManager cacheManager, UserMapper mapper)
        {
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        protected override UserEditContext Context
        {
            get => User;
            set => User = value;
        }

        protected override UserEditContext CreateNewContext() => UserEditContext.CreateNew();

        /// <summary>
        /// 从服务器DTO初始化（编辑模式）
        /// D2: 改用 Mapperly ToEditContext（保留 PinYinCode 回退行为）
        /// </summary>
        public void InitializeFromDto(UserDetailDto dto)
        {
            User = _mapper.ToEditContext(dto);
            IsDirty = false;
            SubscribeContext();
        }

        /// <summary>
        /// 初始化空白实例（新建模式）
        /// </summary>
        public override void InitializeForNewCase()
        {
            base.InitializeForNewCase();
        }

        /// <summary>
        /// 获取编辑后的用户数据，用于保存到服务器
        /// D2: 改用 Mapperly ToInputDto（保留 Trim 行为）
        /// </summary>
        public UserInputDto GetUserInput()
        {
            return _mapper.ToInputDto(User);
        }

        /// <summary>
        /// 重置编辑状态（保留缓存联动：重置后失效用户缓存）
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _cacheManager.InvalidateUserCaches();
        }
    }
}
