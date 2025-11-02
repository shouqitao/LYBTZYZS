using FluentValidation.Results;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Components
{
    /// <summary>
    /// 用户验证器
    /// Issue #1779: Users模块组件化改造
    ///
    /// 职责:
    /// - 用户数据验证
    /// - 集成FluentValidation
    /// - 命令可执行条件验证
    /// </summary>
    public class UserValidator : IComponentValidator
    {
        #region 字段

        private readonly IValidationService _validationService;
        private readonly UserDataManager _dataManager;
        private readonly ILogger<UserValidator> _logger;

        #endregion

        #region 构造函数

        public UserValidator(
            IValidationService validationService,
            UserDataManager dataManager,
            ILogger<UserValidator> logger)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region IComponentValidator实现

        /// <summary>
        /// 异步验证用户数据
        /// </summary>
        public async Task<ValidationResult> ValidateAsync()
        {
            try
            {
                _logger.LogDebug("开始验证用户数据");

                var errors = new List<ValidationFailure>();

                // 验证用户基本信息
                if (_dataManager.Current == null)
                {
                    errors.Add(new ValidationFailure("User", "用户数据不能为空"));
                    return new ValidationResult(errors);
                }

                // 使用 FluentValidation 验证
                var userResult = await _validationService.ValidateAsync(_dataManager.Current);
                if (!userResult.IsValid)
                {
                    errors.AddRange(userResult.Errors);
                }

                var result = new ValidationResult(errors);
                _logger.LogDebug("用户验证完成: {IsValid}, 错误数: {ErrorCount}", result.IsValid, result.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户验证过程发生错误");
                return new ValidationResult(new[]
                {
                    new ValidationFailure("Validation", $"验证过程发生错误: {ex.Message}")
                });
            }
        }

        /// <summary>
        /// 同步验证用户数据
        /// </summary>
        public virtual bool IsValid(out string errorMessage)
        {
            try
            {
                _logger.LogDebug("开始同步验证用户数据");

                // 验证用户基本信息
                if (_dataManager.Current == null)
                {
                    errorMessage = "用户数据不能为空";
                    return false;
                }

                // 使用 ValidationService 同步验证
                if (!_validationService.IsValid(_dataManager.Current, out var validationError))
                {
                    errorMessage = validationError;
                    return false;
                }

                errorMessage = string.Empty;
                _logger.LogDebug("用户同步验证通过");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户同步验证过程发生错误");
                errorMessage = $"验证过程发生错误: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 验证特定属性
        /// </summary>
        public async Task<ValidationResult> ValidatePropertyAsync(string propertyName)
        {
            try
            {
                _logger.LogDebug("开始验证属性: {PropertyName}", propertyName);

                if (_dataManager.Current == null)
                {
                    return new ValidationResult(new[]
                    {
                        new ValidationFailure(propertyName, "用户数据不能为空")
                    });
                }

                // 执行完整验证
                var fullResult = await ValidateAsync();

                // 过滤出特定属性的错误
                var propertyErrors = fullResult.Errors
                    .Where(e => e.PropertyName == propertyName)
                    .ToList();

                return new ValidationResult(propertyErrors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证属性失败: {PropertyName}", propertyName);
                return new ValidationResult(new[]
                {
                    new ValidationFailure(propertyName, $"验证过程发生错误: {ex.Message}")
                });
            }
        }

        /// <summary>
        /// 清除验证错误
        /// </summary>
        public void ClearErrors()
        {
            _logger.LogDebug("清除验证错误");
            // 验证错误由ValidationResult管理,无需额外操作
        }

        #endregion

        #region 专用验证方法

        /// <summary>
        /// 验证是否可以编辑用户
        /// </summary>
        public virtual bool CanEditUser(out string errorMessage)
        {
            if (_dataManager.Current == null)
            {
                errorMessage = "用户数据不能为空";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 验证是否可以重置密码
        /// </summary>
        public virtual bool CanResetPassword(out string errorMessage)
        {
            if (_dataManager.Current == null)
            {
                errorMessage = "用户数据不能为空";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 验证是否可以切换状态
        /// </summary>
        public virtual bool CanToggleStatus(out string errorMessage)
        {
            if (_dataManager.Current == null)
            {
                errorMessage = "用户数据不能为空";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 通用的命令可执行验证（用于所有需要User数据的命令）
        /// </summary>
        public bool CanExecuteCommand(bool checkLoading = false, object? loadingContext = null)
        {
            // 基本检查：用户数据是否存在
            if (_dataManager.Current == null)
                return false;

            // 可选：检查是否正在加载中
            // 这个逻辑由ViewModel层管理，这里预留接口
            if (checkLoading && loadingContext != null)
            {
                // 未来可以添加加载状态检查逻辑
            }

            return true;
        }

        #endregion
    }
}
