using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;
using LYBT.Client.Desktop.Core.Mapping;
using LYBT.Module.Auth.Mapping;
using LYBT.Module.Consultation.Mapping;
using LYBT.Module.Formula.Mapping;
using LYBT.Module.Herbs.Mapping;
using LYBT.Module.MedicalCase.Mapping;
using LYBT.Module.Patients.Mapping;
using LYBT.Module.Prescriptions.Mapping;
using LYBT.Module.Users.Mapping;

namespace LYBT.Tests.Common
{
    /// <summary>
    /// AutoMapper测试配置类
    /// 统一管理所有测试中的AutoMapper配置
    /// </summary>
    public static class AutoMapperTestConfiguration
    {
        private static MapperConfiguration _configuration;
        private static IMapper _mapper;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取配置好的Mapper实例
        /// </summary>
        public static IMapper GetMapper()
        {
            if (_mapper == null)
            {
                lock (_lock)
                {
                    if (_mapper == null)
                    {
                        Initialize();
                    }
                }
            }
            return _mapper;
        }

        /// <summary>
        /// 获取MapperConfiguration
        /// </summary>
        public static MapperConfiguration GetConfiguration()
        {
            if (_configuration == null)
            {
                lock (_lock)
                {
                    if (_configuration == null)
                    {
                        Initialize();
                    }
                }
            }
            return _configuration;
        }

        /// <summary>
        /// 初始化AutoMapper配置
        /// </summary>
        private static void Initialize()
        {
            _configuration = new MapperConfiguration(cfg =>
            {
                // 显式注册所有已知的Profile
                RegisterKnownProfiles(cfg);
                
                // 尝试从程序集中扫描其他Profile
                ScanAssembliesForProfiles(cfg);
            });

            // 验证配置
            try
            {
                _configuration.AssertConfigurationIsValid();
            }
            catch (Exception ex)
            {
                // 记录但不中断测试
                Console.WriteLine($"[AutoMapper Warning] Configuration validation failed: {ex.Message}");
            }

            _mapper = new Mapper(_configuration);
        }

        /// <summary>
        /// 显式注册已知的Profile
        /// </summary>
        private static void RegisterKnownProfiles(IMapperConfigurationExpression cfg)
        {
            // Server端Profile
            cfg.AddProfile<AuthMappingProfile>();
            cfg.AddProfile<ConsultationMappingProfile>();
            cfg.AddProfile<FormulaMappingProfile>();
            cfg.AddProfile<HerbMappingProfile>();
            cfg.AddProfile<MedicalCaseMappingProfile>();
            cfg.AddProfile<PatientMappingProfile>();
            cfg.AddProfile<PrescriptionMappingProfile>();
            cfg.AddProfile<UserMappingProfile>();

            // Client端Profile（如果需要测试客户端映射）
            try
            {
                // 客户端Desktop Core
                cfg.AddProfile<LYBT.Client.Desktop.Core.Mapping.MappingProfile>();
                
                // 客户端模块Profile
                cfg.AddProfile<LYBT.Client.Desktop.Modules.Auth.Mappings.MappingProfile>();
                cfg.AddProfile<LYBT.Client.Desktop.Modules.Herbs.Mappings.MappingProfile>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoMapper Warning] Failed to load client profiles: {ex.Message}");
            }
        }

        /// <summary>
        /// 扫描程序集以发现Profile
        /// </summary>
        private static void ScanAssembliesForProfiles(IMapperConfigurationExpression cfg)
        {
            try
            {
                // 获取所有已加载的程序集
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && 
                               !a.FullName.StartsWith("System") && 
                               !a.FullName.StartsWith("Microsoft") &&
                               a.FullName.Contains("LYBT"))
                    .ToList();

                // 查找所有Profile类型
                var profileTypes = new List<Type>();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var types = assembly.GetTypes()
                            .Where(t => t.IsClass && 
                                       !t.IsAbstract && 
                                       typeof(Profile).IsAssignableFrom(t))
                            .ToList();
                        
                        profileTypes.AddRange(types);
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // 忽略无法加载的类型
                        Console.WriteLine($"[AutoMapper Warning] Failed to scan assembly {assembly.FullName}: {ex.Message}");
                    }
                }

                // 注册找到的Profile
                foreach (var profileType in profileTypes.Distinct())
                {
                    try
                    {
                        // 避免重复注册
                        if (!cfg.AllConfiguredTypeMaps().Any(m => m.Profile?.GetType() == profileType))
                        {
                            var profile = Activator.CreateInstance(profileType) as Profile;
                            if (profile != null)
                            {
                                cfg.AddProfile(profile);
                                Console.WriteLine($"[AutoMapper Info] Registered profile: {profileType.FullName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AutoMapper Warning] Failed to register profile {profileType.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoMapper Warning] Assembly scanning failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 重置配置（仅用于测试）
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _configuration = null;
                _mapper = null;
            }
        }

        /// <summary>
        /// 创建一个隔离的Mapper实例（用于特殊测试场景）
        /// </summary>
        public static IMapper CreateIsolatedMapper(Action<IMapperConfigurationExpression> configure = null)
        {
            var config = new MapperConfiguration(cfg =>
            {
                // 注册基础Profile
                RegisterKnownProfiles(cfg);
                
                // 应用自定义配置
                configure?.Invoke(cfg);
            });

            return new Mapper(config);
        }
    }
}