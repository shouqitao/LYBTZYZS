using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace LYBT.UI.PrismWpf.Services
{
    /// <summary>
    /// 配置服务接口
    /// </summary>
    public interface IConfigurationService
    {
        T GetSection<T>(string sectionName) where T : class, new();
        string GetConnectionString(string name);
        IConfiguration Configuration { get; }
    }

    /// <summary>
    /// 配置服务实现
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        public IConfiguration Configuration { get; private set; }

        public ConfigurationService()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public T GetSection<T>(string sectionName) where T : class, new()
        {
            var section = new T();
            Configuration.GetSection(sectionName).Bind(section);
            return section;
        }

        public string GetConnectionString(string name)
        {
            return Configuration.GetConnectionString(name) ?? string.Empty;
        }
    }
}