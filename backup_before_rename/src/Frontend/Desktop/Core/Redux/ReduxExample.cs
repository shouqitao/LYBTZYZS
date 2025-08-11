using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Windows.Input;
using LYBT.Desktop.Core.Mvvm;
using LYBT.Desktop.Core.Redux.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Redux
{
    /// <summary>
    /// Redux使用示例 - 展示如何在凌隐宝堂系统中使用Redux
    /// </summary>
    public class ReduxExample
    {
        /// <summary>
        /// 配置Redux Store
        /// </summary>
        public static IStateStore<AppState> ConfigureStore(IServiceProvider services)
        {
            var logger = services.GetService<ILogger<StateStore<AppState>>>();
            
            // 创建中间件
            var middlewares = new List<IMiddleware<AppState>>
            {
                // 日志中间件
                new LoggingMiddleware<AppState>(
                    services.GetService<ILogger<LoggingMiddleware<AppState>>>(),
                    logPayload: true,
                    logState: false),
                
                // 异步Action中间件
                ConfigureAsyncMiddleware(services),
                
                // 防抖中间件
                ConfigureDebounceMiddleware(),
                
                // DevTools中间件
                new DevToolsMiddleware<AppState>(),
                
                // 验证中间件
                ConfigureValidationMiddleware(services)
            };

            // 创建Store
            var store = new StateStore<AppState>(
                AppState.Initial,
                new AppReducer(),
                middlewares,
                logger,
                maxHistorySize: 50);

            return store;
        }

        /// <summary>
        /// 配置异步中间件
        /// </summary>
        private static AsyncActionMiddleware<AppState> ConfigureAsyncMiddleware(IServiceProvider services)
        {
            var middleware = new AsyncActionMiddleware<AppState>(
                services.GetService<ILogger<AsyncActionMiddleware<AppState>>>());

            // 注册登录异步处理
            middleware.RegisterHandler("AUTH/LOGIN_REQUEST", async (store, action) =>
            {
                if (action is LoginRequestAction loginAction)
                {
                    try
                    {
                        // 模拟API调用
                        await Task.Delay(1000);
                        
                        // 构造响应
                        var response = new LoginResponse
                        {
                            User = new UserInfo
                            {
                                Id = Guid.NewGuid(),
                                UserName = loginAction.Payload.Username,
                                RealName = "张医生",
                                Role = "Doctor",
                                LastLoginTime = DateTimeOffset.Now
                            },
                            Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                            TokenExpiry = DateTimeOffset.Now.AddHours(8),
                            Permissions = ImmutableList.Create("patient.view", "prescription.create")
                        };

                        // 分发成功Action
                        store.Dispatch(new LoginSuccessAction(response));
                    }
                    catch (Exception ex)
                    {
                        // 分发失败Action
                        store.Dispatch(new LoginFailureAction(ex.Message));
                    }
                }
            });

            // 注册加载患者异步处理
            middleware.RegisterHandler("PATIENTS/LOAD", async (store, action) =>
            {
                await Task.Delay(500);
                
                var patients = ImmutableList.Create(
                    new PatientInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "李四",
                        Gender = "男",
                        Age = 45,
                        Phone = "13800138000",
                        LastVisit = DateTimeOffset.Now.AddDays(-7)
                    },
                    new PatientInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "王五",
                        Gender = "女",
                        Age = 32,
                        Phone = "13900139000",
                        LastVisit = DateTimeOffset.Now.AddDays(-14)
                    }
                );

                store.Dispatch(new LoadPatientsSuccessAction(patients));
            });

            return middleware;
        }

        /// <summary>
        /// 配置防抖中间件
        /// </summary>
        private static DebounceMiddleware<AppState> ConfigureDebounceMiddleware()
        {
            var middleware = new DebounceMiddleware<AppState>(TimeSpan.FromMilliseconds(300));
            
            // 搜索防抖
            middleware.ConfigureDebounce("PATIENTS/SEARCH", TimeSpan.FromMilliseconds(500));
            
            // 诊断更新防抖
            middleware.ConfigureDebounce("CONSULTATION/UPDATE_DIAGNOSIS", TimeSpan.FromSeconds(1));
            
            return middleware;
        }

        /// <summary>
        /// 配置验证中间件
        /// </summary>
        private static ValidationMiddleware<AppState> ConfigureValidationMiddleware(IServiceProvider services)
        {
            var middleware = new ValidationMiddleware<AppState>(
                services.GetService<ILogger<ValidationMiddleware<AppState>>>());

            // 添加登录验证器
            middleware.AddValidator(new LoginValidator());
            
            // 添加处方验证器
            middleware.AddValidator(new PrescriptionValidator());
            
            return middleware;
        }
    }

    /// <summary>
    /// 登录ViewModel示例 - 使用Redux管理状态
    /// </summary>
    public class LoginViewModelWithRedux : StateViewModel<AppState>
    {
        private string _username = string.Empty;
        private string _password = string.Empty;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        // 从Store中获取状态
        public bool IsLoading => State.Auth.IsLoading;
        public bool IsAuthenticated => State.Auth.IsAuthenticated;
        public string? ErrorMessage => State.Auth.Error;
        public string? CurrentUserName => State.Auth.CurrentUser?.RealName;

        // 命令
        public ICommand LoginCommand { get; }
        public ICommand LogoutCommand { get; }

        public LoginViewModelWithRedux(IStateStore<AppState> store) : base(store)
        {
            // 创建命令
            LoginCommand = new AsyncRelayCommand(ExecuteLogin, CanExecuteLogin);
            LogoutCommand = CreateDispatchCommand(() => new LogoutAction());
        }

        protected override void InitializeSelectors()
        {
            // 订阅认证状态变化
            Select(state => state.Auth.IsLoading, 
                isLoading => OnPropertyChanged(nameof(IsLoading)));
            
            Select(state => state.Auth.IsAuthenticated,
                isAuth => OnPropertyChanged(nameof(IsAuthenticated)));
            
            Select(state => state.Auth.Error,
                error => OnPropertyChanged(nameof(ErrorMessage)));
            
            Select(state => state.Auth.CurrentUser != null ? state.Auth.CurrentUser.RealName : null,
                name => OnPropertyChanged(nameof(CurrentUserName)));
        }

        private async Task ExecuteLogin()
        {
            // 分发登录请求Action
            Dispatch(new LoginRequestAction(new LoginRequest
            {
                Username = Username,
                Password = Password,
                RememberMe = true
            }));

            // 异步中间件会处理实际的登录逻辑
            await Task.CompletedTask;
        }

        private bool CanExecuteLogin()
        {
            return !IsLoading && 
                   !string.IsNullOrEmpty(Username) && 
                   !string.IsNullOrEmpty(Password);
        }
    }

    /// <summary>
    /// 患者列表ViewModel示例
    /// </summary>
    public class PatientListViewModelWithRedux : CollectionStateViewModel<AppState, PatientInfo>
    {
        private string _searchQuery = string.Empty;

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    // 分发搜索Action（会被防抖）
                    Dispatch(new SearchPatientsAction(value));
                }
            }
        }

        public bool IsLoading => State.Patients.IsLoading;
        public PatientInfo? SelectedPatient => State.Patients.CurrentPatient;

        public ICommand LoadPatientsCommand { get; }
        public ICommand SelectPatientCommand { get; }
        public ICommand StartConsultationCommand { get; }

        public PatientListViewModelWithRedux(IStateStore<AppState> store) : base(store)
        {
            LoadPatientsCommand = CreateDispatchCommand(() => new LoadPatientsAction());
            SelectPatientCommand = CreateDispatchCommand<Guid>(id => new SelectPatientAction(id));
            StartConsultationCommand = CreateDispatchCommand<Guid>(id => new StartConsultationAction(id));
        }

        protected override void InitializeSelectors()
        {
            // 订阅患者列表变化
            Select(state => state.Patients.PatientList,
                patients => UpdateCollection(patients));
            
            // 订阅加载状态
            Select(state => state.Patients.IsLoading,
                _ => OnPropertyChanged(nameof(IsLoading)));
            
            // 订阅选中患者
            Select(state => state.Patients.CurrentPatient,
                _ => OnPropertyChanged(nameof(SelectedPatient)));
        }

        protected override void OnStateChanged(AppState state)
        {
            base.OnStateChanged(state);
            
            // 根据搜索条件过滤患者
            if (!string.IsNullOrEmpty(state.Patients.SearchQuery))
            {
                var filtered = state.Patients.PatientList
                    .Where(p => p.Name.Contains(state.Patients.SearchQuery) ||
                               p.Phone.Contains(state.Patients.SearchQuery));
                UpdateCollection(filtered);
            }
        }
    }

    /// <summary>
    /// 登录验证器
    /// </summary>
    public class LoginValidator : IActionValidator
    {
        public ValidationResult Validate(IAction action)
        {
            if (action is LoginRequestAction loginAction)
            {
                if (string.IsNullOrEmpty(loginAction.Payload.Username))
                {
                    return ValidationResult.Failure("用户名不能为空");
                }
                
                if (string.IsNullOrEmpty(loginAction.Payload.Password))
                {
                    return ValidationResult.Failure("密码不能为空");
                }
                
                if (loginAction.Payload.Password.Length < 6)
                {
                    return ValidationResult.Failure("密码长度不能少于6位");
                }
            }
            
            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// 处方验证器
    /// </summary>
    public class PrescriptionValidator : IActionValidator
    {
        public ValidationResult Validate(IAction action)
        {
            if (action is SavePrescriptionAction prescriptionAction)
            {
                if (prescriptionAction.Payload.Herbs.IsEmpty)
                {
                    return ValidationResult.Failure("处方不能为空");
                }
                
                if (prescriptionAction.Payload.Doses <= 0)
                {
                    return ValidationResult.Failure("剂数必须大于0");
                }
                
                if (prescriptionAction.Payload.TotalPrice < 0)
                {
                    return ValidationResult.Failure("总价不能为负数");
                }
            }
            
            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// 时间旅行调试工具
    /// </summary>
    public class TimeTravelDebugger<TState> where TState : class, new()
    {
        private readonly IStateStore<TState> _store;
        private readonly DevToolsMiddleware<TState> _devTools;

        public TimeTravelDebugger(IStateStore<TState> store, DevToolsMiddleware<TState> devTools)
        {
            _store = store;
            _devTools = devTools;
        }

        /// <summary>
        /// 后退一步
        /// </summary>
        public void StepBack()
        {
            var history = _store.GetHistory();
            if (history.Count > 1)
            {
                _store.TimeTravelTo(history.Count - 2);
            }
        }

        /// <summary>
        /// 前进一步
        /// </summary>
        public void StepForward()
        {
            var history = _store.GetHistory();
            var currentIndex = history.Count - 1;
            if (currentIndex < history.Count - 1)
            {
                _store.TimeTravelTo(currentIndex + 1);
            }
        }

        /// <summary>
        /// 跳转到指定时间点
        /// </summary>
        public void JumpTo(int index)
        {
            _store.TimeTravelTo(index);
        }

        /// <summary>
        /// 导出日志
        /// </summary>
        public string ExportLog()
        {
            return _devTools.ExportLog();
        }
    }
}