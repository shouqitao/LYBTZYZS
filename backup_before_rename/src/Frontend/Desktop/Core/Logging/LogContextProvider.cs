using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Core.Logging
{
    /// <summary>
    /// 日志上下文提供者 - 提供当前执行上下文信息
    /// </summary>
    public class LogContextProvider : ILogContextProvider
    {
        private readonly IAuthenticationService _authService;
        private readonly AsyncLocal<string> _correlationId = new();
        private readonly AsyncLocal<string> _sessionId = new();
        private readonly AsyncLocal<Dictionary<string, object>> _customProperties = new();
        
        private static readonly string ApplicationVersion = 
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        private static readonly string MachineName = Environment.MachineName;
        
        public LogContextProvider(IAuthenticationService authService)
        {
            _authService = authService;
            InitializeSession();
        }
        
        private void InitializeSession()
        {
            _sessionId.Value = Guid.NewGuid().ToString();
            _correlationId.Value = Guid.NewGuid().ToString();
            _customProperties.Value = new Dictionary<string, object>();
        }
        
        public LogContext GetCurrentContext()
        {
            var currentUser = _authService.GetCurrentUserAsync().Result;
            
            return new LogContext
            {
                CorrelationId = GetCorrelationId(),
                SessionId = GetSessionId(),
                UserId = currentUser?.Id.ToString(),
                UserName = currentUser?.Username,
                MachineName = MachineName,
                ApplicationVersion = ApplicationVersion,
                CustomProperties = new Dictionary<string, object>(_customProperties.Value ?? new Dictionary<string, object>())
            };
        }
        
        public string GetCorrelationId()
        {
            if (string.IsNullOrEmpty(_correlationId.Value))
            {
                _correlationId.Value = Guid.NewGuid().ToString();
            }
            return _correlationId.Value;
        }
        
        public string GetSessionId()
        {
            if (string.IsNullOrEmpty(_sessionId.Value))
            {
                _sessionId.Value = Guid.NewGuid().ToString();
            }
            return _sessionId.Value;
        }
        
        public string GetCurrentUserId()
        {
            var currentUser = _authService.GetCurrentUserAsync().Result;
            return currentUser?.Id.ToString();
        }
        
        public string GetCurrentUserName()
        {
            var currentUser = _authService.GetCurrentUserAsync().Result;
            return currentUser?.Username;
        }
        
        public string GetClientIpAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
                // 忽略错误
            }
            return "127.0.0.1";
        }
        
        public void SetContextProperty(string key, object value)
        {
            if (_customProperties.Value == null)
            {
                _customProperties.Value = new Dictionary<string, object>();
            }
            _customProperties.Value[key] = value;
        }
        
        public void ClearContextProperty(string key)
        {
            _customProperties.Value?.Remove(key);
        }
        
        /// <summary>
        /// 创建新的关联ID（用于新的操作链）
        /// </summary>
        public void NewCorrelationId()
        {
            _correlationId.Value = Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// 设置关联ID（用于继承外部关联ID）
        /// </summary>
        public void SetCorrelationId(string correlationId)
        {
            _correlationId.Value = correlationId;
        }
    }
}