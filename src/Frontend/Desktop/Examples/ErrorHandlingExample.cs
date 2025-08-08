using System;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Exceptions;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Resources;

namespace LYBT.WPF.Client.Examples
{
    /// <summary>
    /// 错误处理服务使用示例
    /// </summary>
    public class ErrorHandlingExample
    {
        private readonly IErrorHandlingService _errorHandlingService;

        public ErrorHandlingExample(IErrorHandlingService errorHandlingService)
        {
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
        }

        /// <summary>
        /// 示例1：基本错误处理
        /// </summary>
        public async Task BasicErrorHandlingExample()
        {
            try
            {
                // 模拟可能失败的操作
                await SimulateOperation();
            }
            catch (Exception ex)
            {
                // 使用错误处理服务处理异常
                var context = new ErrorContext
                {
                    OperationName = "基本操作示例",
                    ModuleName = "Examples",
                    ViewName = "ErrorHandlingExample"
                };

                var handledError = await _errorHandlingService.HandleExceptionAsync(ex, context);
                await _errorHandlingService.ShowErrorAsync(handledError);
            }
        }

        /// <summary>
        /// 示例2：使用安全执行方法
        /// </summary>
        public async Task SafeExecutionExample()
        {
            var context = new ErrorContext
            {
                OperationName = "安全执行示例",
                ModuleName = "Examples",
                ViewName = "ErrorHandlingExample"
            };

            // 安全执行无返回值操作
            var success = await _errorHandlingService.ExecuteSafelyAsync(
                async () => await SimulateOperation(),
                context,
                showErrorDialog: true
            );

            if (success)
            {
                // 操作成功
                Console.WriteLine("操作成功完成");
            }

            // 安全执行有返回值操作
            var result = await _errorHandlingService.ExecuteSafelyAsync(
                async () => await SimulateDataOperation(),
                context,
                showErrorDialog: true
            );

            if (result != null)
            {
                // 使用结果
                Console.WriteLine($"获取到数据: {result}");
            }
        }

        /// <summary>
        /// 示例3：抛出业务异常
        /// </summary>
        public async Task BusinessExceptionExample()
        {
            try
            {
                // 业务验证失败
                if (DateTime.Now.Hour < 9 || DateTime.Now.Hour > 17)
                {
                    throw new BusinessException(
                        "当前时间不在营业时间内，无法执行此操作",
                        "BUSINESS_HOURS_VALIDATION",
                        ErrorSeverity.Warning
                    );
                }

                // 继续业务逻辑...
            }
            catch (BusinessException ex)
            {
                var context = new ErrorContext
                {
                    OperationName = "营业时间验证",
                    ModuleName = "Business",
                    ViewName = "TimeValidation"
                };
                context.AddData("CurrentHour", DateTime.Now.Hour);
                context.AddData("BusinessHours", "9:00-17:00");

                await _errorHandlingService.HandleExceptionAsync(ex, context);
            }
        }

        /// <summary>
        /// 示例4：抛出验证异常
        /// </summary>
        public async Task ValidationExceptionExample()
        {
            try
            {
                var patientName = "";
                var patientAge = -1;

                var validationException = new ValidationException("患者信息验证失败");

                if (string.IsNullOrEmpty(patientName))
                {
                    validationException.AddError("姓名", ErrorMessages.Validation.RequiredFieldEmpty);
                }

                if (patientAge < 0 || patientAge > 150)
                {
                    validationException.AddError("年龄", ErrorMessages.Validation.InvalidRange);
                }

                if (validationException.ValidationErrors.Count > 0)
                {
                    throw validationException;
                }
            }
            catch (ValidationException ex)
            {
                var context = new ErrorContext
                {
                    OperationName = "患者信息验证",
                    ModuleName = "Patients",
                    ViewName = "PatientRegistration"
                };
                context.AddData("ValidationErrorCount", ex.ValidationErrors.Count);

                await _errorHandlingService.HandleExceptionAsync(ex, context);
            }
        }

        /// <summary>
        /// 示例5：抛出网络异常
        /// </summary>
        public async Task NetworkExceptionExample()
        {
            try
            {
                // 模拟网络请求失败
                throw new NetworkException(
                    ErrorMessages.Network.ConnectionFailed,
                    System.Net.HttpStatusCode.ServiceUnavailable,
                    canRetry: true,
                    severity: ErrorSeverity.Error
                );
            }
            catch (NetworkException ex)
            {
                var context = new ErrorContext
                {
                    OperationName = "API请求",
                    ModuleName = "Network",
                    ViewName = "ApiClient"
                };
                context.AddData("StatusCode", ex.StatusCode);
                context.AddData("CanRetry", ex.CanRetry);

                await _errorHandlingService.HandleExceptionAsync(ex, context);
            }
        }

        /// <summary>
        /// 示例6：抛出认证异常
        /// </summary>
        public async Task AuthenticationExceptionExample()
        {
            try
            {
                throw AuthenticationException.TokenExpired();
            }
            catch (AuthenticationException ex)
            {
                var context = new ErrorContext
                {
                    OperationName = "用户认证",
                    ModuleName = "Authentication",
                    ViewName = "LoginService"
                };
                context.AddData("ErrorType", ex.ErrorType);
                context.AddData("RequiresReLogin", ex.RequiresReLogin);

                await _errorHandlingService.HandleExceptionAsync(ex, context);
            }
        }

        /// <summary>
        /// 示例7：监听错误事件
        /// </summary>
        public void ErrorEventHandlingExample()
        {
            // 监听普通错误
            _errorHandlingService.ErrorOccurred += (sender, handledError) =>
            {
                Console.WriteLine($"检测到错误: {handledError.UserMessage}");
                
                // 可以在这里记录错误统计、发送遥测数据等
                LogErrorStatistics(handledError);
            };

            // 监听严重错误
            _errorHandlingService.CriticalErrorOccurred += (sender, handledError) =>
            {
                Console.WriteLine($"检测到严重错误: {handledError.UserMessage}");
                
                // 严重错误处理：发送紧急通知、保存状态等
                HandleCriticalError(handledError);
            };
        }

        #region 模拟方法

        private async Task SimulateOperation()
        {
            await Task.Delay(100);
            
            // 随机失败
            var random = new Random();
            if (random.Next(1, 3) == 1)
            {
                throw new InvalidOperationException("模拟操作失败");
            }
        }

        private async Task<string> SimulateDataOperation()
        {
            await Task.Delay(100);
            
            // 随机失败
            var random = new Random();
            if (random.Next(1, 3) == 1)
            {
                throw new InvalidOperationException("模拟数据获取失败");
            }

            return "模拟数据";
        }

        private void LogErrorStatistics(HandledError handledError)
        {
            // 错误统计逻辑
            Console.WriteLine($"记录错误统计 - 类型: {handledError.Category}, 严重程度: {handledError.Severity}");
        }

        private void HandleCriticalError(HandledError handledError)
        {
            // 严重错误处理逻辑
            Console.WriteLine($"处理严重错误 - ID: {handledError.Id}");
            
            // 可能的操作：
            // - 保存当前状态
            // - 发送紧急通知
            // - 准备应用程序重启
            // - 收集诊断信息
        }

        #endregion
    }
}