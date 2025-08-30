using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.WebAPI.Services
{
    /// <summary>
    /// 诊所配置服务
    /// </summary>
    public interface IClinicConfigService
    {
        ClinicConfig GetConfig();
        string GetClinicName();
        string GetPrescriptionPrefix();
        int GetDefaultDosage();
        PricingRules GetPricingRules();
        bool UpdateConfig(ClinicConfig config);
    }

    public class ClinicConfigService : IClinicConfigService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClinicConfigService> _logger;
        private readonly string _configPath;
        private ClinicConfig _cachedConfig;
        private DateTime _lastLoadTime;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public ClinicConfigService(IConfiguration configuration, ILogger<ClinicConfigService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clinic.config.json");
            LoadConfig();
        }

        public ClinicConfig GetConfig()
        {
            // 检查缓存是否过期
            if (_cachedConfig == null || DateTime.Now - _lastLoadTime > _cacheExpiration)
            {
                LoadConfig();
            }
            return _cachedConfig;
        }

        public string GetClinicName()
        {
            return GetConfig()?.ClinicSettings?.Basic?.Name ?? "中医诊所";
        }

        public string GetPrescriptionPrefix()
        {
            return GetConfig()?.ClinicSettings?.Prescription?.Prefix ?? "RX";
        }

        public int GetDefaultDosage()
        {
            return GetConfig()?.ClinicSettings?.Prescription?.DefaultDosage ?? 7;
        }

        public PricingRules GetPricingRules()
        {
            return GetConfig()?.ClinicSettings?.Pricing;
        }

        public bool UpdateConfig(ClinicConfig config)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                File.WriteAllText(_configPath, json);
                _cachedConfig = config;
                _lastLoadTime = DateTime.Now;
                
                _logger.LogInformation("诊所配置已更新");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新诊所配置失败");
                return false;
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _cachedConfig = JsonSerializer.Deserialize<ClinicConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }
                else
                {
                    // 从appsettings.json加载默认配置
                    _cachedConfig = new ClinicConfig();
                    _configuration.GetSection("ClinicSettings").Bind(_cachedConfig.ClinicSettings);
                }
                
                _lastLoadTime = DateTime.Now;
                _logger.LogInformation($"诊所配置已加载: {GetClinicName()}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载诊所配置失败");
                _cachedConfig = GetDefaultConfig();
            }
        }

        private ClinicConfig GetDefaultConfig()
        {
            return new ClinicConfig
            {
                ClinicSettings = new ClinicSettings
                {
                    Basic = new BasicInfo
                    {
                        Name = "中医诊所",
                        Address = "地址未设置",
                        Phone = "电话未设置"
                    },
                    Prescription = new PrescriptionSettings
                    {
                        Prefix = "RX",
                        DefaultDosage = 7,
                        DefaultUsage = "每日1剂，水煎服"
                    }
                }
            };
        }
    }

    // 配置模型类
    public class ClinicConfig
    {
        public ClinicSettings ClinicSettings { get; set; }
    }

    public class ClinicSettings
    {
        public BasicInfo Basic { get; set; }
        public PrescriptionSettings Prescription { get; set; }
        public PricingRules Pricing { get; set; }
        public SystemSettings System { get; set; }
        public PrintTemplate PrintTemplate { get; set; }
        public BusinessRules BusinessRules { get; set; }
        public Features Features { get; set; }
    }

    public class BasicInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Logo { get; set; }
        public string Slogan { get; set; }
    }

    public class PrescriptionSettings
    {
        public string Prefix { get; set; }
        public int DefaultDosage { get; set; }
        public string DefaultUsage { get; set; }
        public int PrintCopies { get; set; }
        public bool ShowPrice { get; set; }
        public bool ShowDiagnosis { get; set; }
        public bool RequireSignature { get; set; }
    }

    public class PricingRules
    {
        public string Currency { get; set; }
        public string CurrencySymbol { get; set; }
        public DiscountRules DefaultDiscountRules { get; set; }
    }

    public class DiscountRules
    {
        public DiscountRange Doctor { get; set; }
        public DiscountRange ChiefDoctor { get; set; }
        public DiscountRange Admin { get; set; }
    }

    public class DiscountRange
    {
        public decimal MinDiscount { get; set; }
        public decimal MaxDiscount { get; set; }
    }

    public class SystemSettings
    {
        public string SystemName { get; set; }
        public string Version { get; set; }
        public bool MultiClinicMode { get; set; }
        public bool DataIsolation { get; set; }
        public bool EnableAudit { get; set; }
    }

    public class PrintTemplate
    {
        public string PageSize { get; set; }
        public string Orientation { get; set; }
        public int MarginTop { get; set; }
        public int MarginBottom { get; set; }
        public int MarginLeft { get; set; }
        public int MarginRight { get; set; }
        public string FontFamily { get; set; }
        public int FontSize { get; set; }
        public int HeaderHeight { get; set; }
        public int FooterHeight { get; set; }
        public bool ShowLogo { get; set; }
        public bool ShowQRCode { get; set; }
    }

    public class BusinessRules
    {
        public bool AllowPrescriptionModification { get; set; }
        public int PrescriptionModificationTimeLimit { get; set; }
        public bool AllowCrossDoctorQuery { get; set; }
        public bool RequirePatientConsent { get; set; }
        public bool AutoGeneratePrescriptionNumber { get; set; }
        public bool TrackPrintHistory { get; set; }
    }

    public class Features
    {
        public bool EnableFormulaSharing { get; set; }
        public bool EnableHistoricalImport { get; set; }
        public bool EnablePriceCalculation { get; set; }
        public bool EnableStatistics { get; set; }
        public bool EnableBatchPrescription { get; set; }
        public bool EnableVoiceInput { get; set; }
        public bool EnableAIAssistant { get; set; }
    }
}