using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Contracts.Services;
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
    /// </summary>
    public partial class UserEditorViewModel : ObservableObject
    {
        private readonly IDesktopCacheManager _cacheManager;
        private readonly UserMapper _mapper;

        /// <summary>用户编辑上下文</summary>
        [ObservableProperty]
        private UserEditContext _user = UserEditContext.CreateNew();

        /// <summary>是否已修改</summary>
        [ObservableProperty]
        private bool _isDirty;

        public UserEditorViewModel(IDesktopCacheManager cacheManager, UserMapper mapper)
        {
            _cacheManager = cacheManager;
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// 从服务器DTO初始化（编辑模式）
        /// D2: 改用 Mapperly ToEditContext（保留 PinYinCode 回退行为）
        /// </summary>
        public void InitializeFromDto(UserDetailDto dto)
        {
            User = _mapper.ToEditContext(dto);
            IsDirty = false;
        }

        /// <summary>
        /// 初始化空白实例（新建模式）
        /// </summary>
        public void InitializeForNewCase()
        {
            User = UserEditContext.CreateNew();
            IsDirty = false;
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
        /// 验证编辑数据
        /// </summary>
        public bool Validate()
        {
            return User.ValidateAll();
        }

        /// <summary>
        /// 重置编辑状态
        /// </summary>
        public void Reset()
        {
            User = UserEditContext.CreateNew();
            IsDirty = false;
            _cacheManager.InvalidateUserCaches();
        }
    }
}
