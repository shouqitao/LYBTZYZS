using LYBT.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration
{
    /// <summary>
    /// 配置扩展方法
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// 添加统一配置管理
        /// </summary>
        public static IServiceCollection AddUnifiedConfiguration(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // 注册配置管理器
            services.AddSingleton<IConfigurationManager, ConfigurationManager>();

            // 配置选项绑定和验证
            services.AddOptionsWithValidation<JwtOptions>(configuration);
            services.AddOptionsWithValidation<SecurityOptions>(configuration);
            services.AddOptionsWithValidation<CacheOptions>(configuration);
            services.AddOptionsWithValidation<DatabaseOptions>(configuration);

            return services;
        }

        /// <summary>
        /// 添加带验证的配置选项
        /// </summary>
        private static IServiceCollection AddOptionsWithValidation<T>(
            this IServiceCollection services,
            IConfiguration configuration) where T : class
        {
            // 获取节名称
            var sectionName = GetSectionName<T>();
            
            // 绑定配置
            services.Configure<T>(configuration.GetSection(sectionName));
            
            // 添加验证
            services.AddSingleton<IValidateOptions<T>>(serviceProvider =>
                new ValidateOptions<T>(sectionName, ValidateConfiguration));

            return services;
        }

        /// <summary>
        /// 获取配置节名称
        /// </summary>
        private static string GetSectionName<T>() where T : class
        {
            // 尝试从常量获取节名称
            var sectionNameField = typeof(T).GetField("SectionName", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            if (sectionNameField?.GetValue(null) is string sectionName)
            {
                return sectionName;
            }

            // 如果没有常量，使用类型名称（移除Options后缀）
            var typeName = typeof(T).Name;
            if (typeName.EndsWith("Options"))
            {
                typeName = typeName.Substring(0, typeName.Length - 7);
            }
            return typeName;
        }

        /// <summary>
        /// 验证配置对象
        /// </summary>
        private static ValidateOptionsResult ValidateConfiguration<T>(T options, string name) where T : class
        {
            var validationContext = new ValidationContext(options, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();
            
            if (!Validator.TryValidateObject(options, validationContext, validationResults, validateAllProperties: true))
            {
                var errors = validationResults.Select(r => $"{name}: {r.ErrorMessage}");
                return ValidateOptionsResult.Fail(errors);
            }

            return ValidateOptionsResult.Success;
        }

        /// <summary>
        /// 获取强类型配置
        /// </summary>
        public static T GetTypedConfiguration<T>(this IConfiguration configuration, string sectionName) 
            where T : class, new()
        {
            var section = configuration.GetSection(sectionName);
            var config = new T();
            section.Bind(config);
            return config;
        }

        /// <summary>
        /// 验证所有配置
        /// </summary>
        public static void ValidateAllConfigurations(this IServiceProvider serviceProvider)
        {
            var configurationManager = serviceProvider.GetRequiredService<IConfigurationManager>();
            var validationResult = configurationManager.ValidateConfiguration();
            
            if (validationResult != ValidationResult.Success)
            {
                throw new InvalidOperationException($"配置验证失败: {validationResult.ErrorMessage}");
            }
        }
    }

    /// <summary>
    /// 配置验证器
    /// </summary>
    internal class ValidateOptions<T> : IValidateOptions<T> where T : class
    {
        private readonly string _name;
        private readonly Func<T, string, ValidateOptionsResult> _validation;

        public ValidateOptions(string name, Func<T, string, ValidateOptionsResult> validation)
        {
            _name = name;
            _validation = validation;
        }

        public ValidateOptionsResult Validate(string? name, T options)
        {
            // 只验证匹配的选项名称
            if (name == null || name == _name)
            {
                return _validation(options, _name);
            }

            return ValidateOptionsResult.Skip;
        }
    }
}