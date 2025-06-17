using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 自动在请求中附加 JWT Token
    /// </summary>
    public class AuthHttpMessageHandler : DelegatingHandler {
        private readonly TokenService _tokenService;
        public AuthHttpMessageHandler(TokenService tokenService) {
            _tokenService = tokenService;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            if (!string.IsNullOrEmpty(_tokenService.Token)) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenService.Token);
            }
            return base.SendAsync(request, cancellationToken);
        }
    }
}
