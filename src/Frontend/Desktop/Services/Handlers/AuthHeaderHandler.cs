using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Services.Handlers {
    /// <summary>
    /// HTTP请求认证头处理器
    /// </summary>
    public class AuthHeaderHandler : DelegatingHandler {
        private readonly ITokenManager _tokenManager;

        public AuthHeaderHandler(ITokenManager tokenManager) {
            _tokenManager = tokenManager;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            var token = _tokenManager.GetToken();

            if (!string.IsNullOrEmpty(token)) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}