using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Services
{
    /// <summary>
    /// 用户数据管理器
    /// Issue #1779: Users模块组件化改造
    /// OpenSpec: standardize-module-structure - Components重命名为Services
    ///
    /// 职责:
    /// - 管理用户实体数据
    /// - 加载用户详情（GetByIdAsync）
    /// - 更新用户信息（UpdateAsync）
    /// - 变更检测
    /// </summary>
    public class UserDataManager : IDataManager<UserDto>
    {
        #region 字段

        private readonly LYBT.Desktop.Users.Interfaces.IUserRepository _userRepository;
        private readonly ILogger<UserDataManager> _logger;

        // 用户数据
        private UserDto? _originalUser;
        private UserDto? _currentUser;

        #endregion

        #region 属性

        /// <summary>
        /// 当前用户数据
        /// </summary>
        public virtual UserDto? Current => _currentUser;

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        public virtual bool HasChanges
        {
            get
            {
                if (_currentUser == null || _originalUser == null)
                    return false;

                return IsUserChanged();
            }
        }

        #endregion

        #region 构造函数

        public UserDataManager(
            LYBT.Desktop.Users.Interfaces.IUserRepository userRepository,
            ILogger<UserDataManager> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region IDataManager实现

        /// <summary>
        /// 初始化用户数据
        /// </summary>
        /// <param name="entityId">用户ID</param>
        public async Task InitializeAsync(Guid entityId)
        {
            try
            {
                _logger.LogInformation("开始加载用户数据: UserId={UserId}", entityId);

                _currentUser = await _userRepository.GetByIdAsync(entityId);

                if (_currentUser != null)
                {
                    _originalUser = CloneUser(_currentUser);
                    _logger.LogInformation("用户数据加载成功: UserName={UserName}", _currentUser.UserName);
                }
                else
                {
                    _logger.LogWarning("未找到用户数据: UserId={UserId}", entityId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载用户数据失败: UserId={UserId}", entityId);
                throw;
            }
        }

        /// <summary>
        /// 保存用户数据
        /// </summary>
        public virtual async Task<bool> SaveAsync()
        {
            if (_currentUser == null)
            {
                _logger.LogWarning("无法保存：当前用户数据为空");
                return false;
            }

            if (!HasChanges)
            {
                _logger.LogInformation("用户数据无变更，跳过保存");
                return true;
            }

            try
            {
                _logger.LogInformation("开始保存用户数据: UserId={UserId}", _currentUser.Id);

                // 创建更新DTO
                var updateDto = new UserInputDto
                {
                    Id = _currentUser.Id,
                    RealName = _currentUser.RealName,
                    Role = _currentUser.Role,
                    Status = _currentUser.Status,
                    PhoneNumber = _currentUser.PhoneNumber,
                    Email = _currentUser.Email
                };

                // 调用Repository更新
                var updated = await _userRepository.UpdateAsync(updateDto);

                if (updated != null)
                {
                    _currentUser = updated;
                    _originalUser = CloneUser(updated);

                    _logger.LogInformation("用户数据保存成功");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存用户数据失败: UserId={UserId}", _currentUser?.Id);
                return false;
            }
        }

        /// <summary>
        /// 删除用户数据
        /// </summary>
        public virtual async Task<bool> DeleteAsync()
        {
            if (_currentUser == null)
            {
                _logger.LogWarning("无法删除：当前用户数据为空");
                return false;
            }

            try
            {
                _logger.LogInformation("开始删除用户数据: UserId={UserId}", _currentUser.Id);

                var result = await _userRepository.DeleteAsync(_currentUser.Id);
                if (result)
                {
                    _currentUser = null;
                    _originalUser = null;

                    _logger.LogInformation("用户数据删除成功");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户数据失败: UserId={UserId}", _currentUser?.Id);
                return false;
            }
        }

        /// <summary>
        /// 重新加载用户数据
        /// </summary>
        public virtual async Task ReloadAsync()
        {
            if (_currentUser != null)
            {
                _logger.LogInformation("重新加载用户数据: UserId={UserId}", _currentUser.Id);
                await InitializeAsync(_currentUser.Id);
            }
        }

        #endregion

        #region 数据操作方法

        /// <summary>
        /// 更新用户数据
        /// </summary>
        public void UpdateUser(UserDto user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            _currentUser = user;
        }

        /// <summary>
        /// 切换用户状态
        /// </summary>
        public virtual async Task<bool> ToggleStatusAsync()
        {
            if (_currentUser == null)
            {
                _logger.LogWarning("无法切换状态：用户数据为空");
                return false;
            }

            try
            {
                var newStatus = _currentUser.Status == Shared.Models.Enums.CommonStatus.Enabled
                    ? Shared.Models.Enums.CommonStatus.Disabled
                    : Shared.Models.Enums.CommonStatus.Enabled;

                _logger.LogInformation("开始切换用户状态: UserId={UserId}, 当前状态={CurrentStatus}, 目标状态={NewStatus}",
                    _currentUser.Id, _currentUser.Status, newStatus);

                // 创建更新DTO
                var updateDto = new UserInputDto
                {
                    Id = _currentUser.Id,
                    RealName = _currentUser.RealName,
                    Role = _currentUser.Role,
                    Status = newStatus,
                    PhoneNumber = _currentUser.PhoneNumber,
                    Email = _currentUser.Email
                };

                // 调用API更新
                var updated = await _userRepository.UpdateAsync(updateDto);

                if (updated != null)
                {
                    _currentUser = updated;
                    _originalUser = CloneUser(updated);

                    _logger.LogInformation("用户状态切换成功: UserId={UserId}, 新状态={NewStatus}", _currentUser.Id, newStatus);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换用户状态失败: UserId={UserId}", _currentUser?.Id);
                return false;
            }
        }

        #endregion

        #region 私有方法 - 变更检测

        private bool IsUserChanged()
        {
            if (_currentUser == null || _originalUser == null)
                return false;

            return _currentUser.RealName != _originalUser.RealName ||
                   _currentUser.Role != _originalUser.Role ||
                   _currentUser.Status != _originalUser.Status ||
                   _currentUser.PhoneNumber != _originalUser.PhoneNumber ||
                   _currentUser.Email != _originalUser.Email;
        }

        #endregion

        #region 私有方法 - 深拷贝

        private UserDto CloneUser(UserDto source)
        {
            return new UserDto
            {
                Id = source.Id,
                UserName = source.UserName,
                RealName = source.RealName,
                Role = source.Role,
                Status = source.Status,
                PhoneNumber = source.PhoneNumber,
                Email = source.Email,
                PinYinCode = source.PinYinCode,
                LastLoginTime = source.LastLoginTime,
                FailedLoginCount = source.FailedLoginCount
            };
        }

        #endregion
    }
}
