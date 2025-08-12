using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.CQRS.Behaviors
{
    /// <summary>
    /// 验证行为管道 - UltraThink重构架构
    /// 自动验证CQRS请求的业务规则和数据完整性
    /// </summary>
    /// <typeparam name="TRequest">请求类型</typeparam>
    /// <typeparam name="TResponse">响应类型</typeparam>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;
        // private readonly IEnumerable<IValidator<TRequest>> _validators; // 如果使用FluentValidation

        public ValidationBehavior(ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // _validators = validators ?? Enumerable.Empty<IValidator<TRequest>>();
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            // 执行基础验证
            var validationErrors = ValidateRequest(request);
            
            if (validationErrors.Any())
            {
                _logger.LogWarning("验证失败 {RequestName}: {ValidationErrors}", 
                    requestName, string.Join(", ", validationErrors));

                throw new ValidationException($"验证失败: {string.Join(", ", validationErrors)}");
            }

            // 如果使用FluentValidation
            /*
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
                
                var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();
                
                if (failures.Count != 0)
                {
                    _logger.LogWarning("FluentValidation验证失败 {RequestName}: {Failures}", 
                        requestName, failures.Select(f => f.ErrorMessage));
                    
                    throw new ValidationException(failures);
                }
            }
            */

            _logger.LogDebug("验证通过 {RequestName}", requestName);
            return await next();
        }

        /// <summary>
        /// 基础验证规则
        /// </summary>
        private List<string> ValidateRequest(TRequest request)
        {
            var errors = new List<string>();
            var requestType = typeof(TRequest);

            try
            {
                // 通用验证规则
                if (request == null)
                {
                    errors.Add("请求不能为空");
                    return errors;
                }

                // 根据请求类型进行特定验证
                ValidateByRequestType(request, errors);

                return errors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行验证时发生错误: {RequestType}", requestType.Name);
                errors.Add("验证过程中发生内部错误");
                return errors;
            }
        }

        /// <summary>
        /// 根据请求类型执行特定验证
        /// </summary>
        private void ValidateByRequestType(TRequest request, List<string> errors)
        {
            var requestType = request.GetType();
            var requestName = requestType.Name;

            // 用户相关验证
            if (requestName.StartsWith("CreateUser"))
            {
                ValidateCreateUserCommand(request, errors);
            }
            else if (requestName.StartsWith("UpdateUser"))
            {
                ValidateUpdateUserCommand(request, errors);
            }
            else if (requestName.Contains("GetUsers") && requestName.Contains("Paged"))
            {
                ValidatePagedQuery(request, errors);
            }

            // 可以添加更多业务实体的验证规则...
        }

        /// <summary>
        /// 验证创建用户命令
        /// </summary>
        private void ValidateCreateUserCommand(TRequest request, List<string> errors)
        {
            try
            {
                var userName = GetPropertyValue<string>(request, "UserName");
                var realName = GetPropertyValue<string>(request, "RealName");
                var email = GetPropertyValue<string>(request, "Email");
                var passwordHash = GetPropertyValue<string>(request, "PasswordHash");

                if (string.IsNullOrEmpty(userName))
                    errors.Add("用户名不能为空");
                else if (userName.Length < 3 || userName.Length > 50)
                    errors.Add("用户名长度必须在3-50个字符之间");

                if (string.IsNullOrEmpty(realName))
                    errors.Add("真实姓名不能为空");
                else if (realName.Length > 100)
                    errors.Add("真实姓名不能超过100个字符");

                if (string.IsNullOrEmpty(email))
                    errors.Add("邮箱不能为空");
                else if (!IsValidEmail(email))
                    errors.Add("邮箱格式不正确");

                if (string.IsNullOrEmpty(passwordHash))
                    errors.Add("密码哈希不能为空");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证创建用户命令时发生错误");
                errors.Add("创建用户命令验证失败");
            }
        }

        /// <summary>
        /// 验证更新用户命令
        /// </summary>
        private void ValidateUpdateUserCommand(TRequest request, List<string> errors)
        {
            try
            {
                var id = GetPropertyValue<Guid>(request, "Id");
                
                if (id == Guid.Empty)
                    errors.Add("用户ID不能为空");

                var email = GetPropertyValue<string>(request, "Email");
                if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
                    errors.Add("邮箱格式不正确");

                var realName = GetPropertyValue<string>(request, "RealName");
                if (!string.IsNullOrEmpty(realName) && realName.Length > 100)
                    errors.Add("真实姓名不能超过100个字符");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证更新用户命令时发生错误");
                errors.Add("更新用户命令验证失败");
            }
        }

        /// <summary>
        /// 验证分页查询
        /// </summary>
        private void ValidatePagedQuery(TRequest request, List<string> errors)
        {
            try
            {
                var pageIndex = GetPropertyValue<int>(request, "PageIndex");
                var pageSize = GetPropertyValue<int>(request, "PageSize");

                if (pageIndex < 0)
                    errors.Add("页码不能小于0");

                if (pageSize <= 0)
                    errors.Add("页大小必须大于0");
                else if (pageSize > 100)
                    errors.Add("页大小不能超过100");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证分页查询时发生错误");
                errors.Add("分页查询验证失败");
            }
        }

        /// <summary>
        /// 通过反射获取属性值
        /// </summary>
        private T GetPropertyValue<T>(object obj, string propertyName)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                if (property == null)
                    return default(T);

                var value = property.GetValue(obj);
                if (value == null)
                    return default(T);

                return (T)value;
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// 简单的邮箱格式验证
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 验证异常
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message)
        {
        }
    }
}