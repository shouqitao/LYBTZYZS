using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    /// <summary>
    /// 全局API Token注入Handler
    /// </summary>
    public class TokenHandler : DelegatingHandler {
        public TokenHandler() { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            var token = TokenProvider.Token;
            if (!string.IsNullOrEmpty(token)) {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
