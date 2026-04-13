using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Users.Models.Items;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Utilities.Text;

namespace LYBT.Desktop.Users.ViewModels
{
    /// <summary>
    /// 用户编辑子视图模型 - 对象DP模式
    /// OpenSpec: frontend-architecture-unification
    ///
    /// 封装用户编辑状态，提供初始化、验证、数据提取等功能
    /// 由 UserMasterDetailViewModel 组合使用
    /// </summary>
    public partial class UserEditorViewModel : ObservableObject
    {
        private readonly IDesktopCacheManager _cacheManager;

        /// <summary>用户编辑上下文</summary>
        [ObservableProperty]
        private UserEditContext _user = UserEditContext.CreateNew();

        /// <summary>是否已修改</summary>
        [ObservableProperty]
        private bool _isDirty;

        public UserEditorViewModel(IDesktopCacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        /// <summary>
        /// 从服务器DTO初始化（编辑模式）
        /// </summary>
        public void InitializeFromDto(UserDetailDto dto)
        {
            User = new UserEditContext
            {
                Id = dto.Id,
                UserName = dto.UserName,
                RealName = dto.RealName,
                PinYinCode = dto.PinYinCode ?? PinYinHelper.GetPinYinCode(dto.RealName),
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                Role = dto.Role,
                Status = dto.Status,
                LastLoginTime = dto.LastLoginTime,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                Remark = dto.Remark
            };
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
        /// </summary>
        public UserInputDto GetUserInput()
        {
            return new UserInputDto
            {
                Id = User.Id == Guid.Empty ? null : User.Id,
                UserName = User.UserName.Trim(),
                RealName = User.RealName?.Trim() ?? string.Empty,
                PinYinCode = User.PinYinCode?.Trim(),
                PhoneNumber = User.PhoneNumber?.Trim(),
                Email = User.Email?.Trim(),
                Role = User.Role,
                Remark = User.Remark?.Trim()
            };
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
