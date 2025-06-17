using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System.Windows;
using LYBT.UI.WPF.Views;
using LYBT.UI.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Refit;
using System.Net.Http;

namespace LYBT.UI.WPF {
    /// <summary>
    /// Prism application bootstrap
    /// </summary>
    public partial class App : PrismApplication {
        protected override Window CreateShell() {
            var login = Container.Resolve<LoginWindow>();
            var result = login.ShowDialog();
            if (result != true) {
                Shutdown();
                return null!;
            }
            return Container.Resolve<ShellView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            containerRegistry.RegisterForNavigation<LoginView>(nameof(LoginView));

            // share a single TokenService instance between Prism and the HTTP client
            var tokenService = new TokenService();
            containerRegistry.RegisterInstance(tokenService);
            containerRegistry.Register<LoginWindow>();

            var services = new ServiceCollection();
            services.AddSingleton(tokenService);
            services.AddTransient<AuthHttpMessageHandler>();
            services.AddRefitClient<IAuthApi>(new RefitSettings {
                ContentSerializer = new SystemTextJsonContentSerializer()
            })
                .ConfigureHttpClient(c => c.BaseAddress = new System.Uri("http://localhost:5297/"))
                .AddHttpMessageHandler<AuthHttpMessageHandler>()
                .AddPolicyHandler(Policy<HttpResponseMessage>.Handle<HttpRequestException>()
                    .WaitAndRetryAsync(3, _ => System.TimeSpan.FromSeconds(1)));

            var provider = services.BuildServiceProvider();
            containerRegistry.RegisterInstance(provider.GetRequiredService<IAuthApi>());
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog) {
            // register modules here
        }
    }
}