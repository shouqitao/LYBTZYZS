using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Admin.Prescriptions.Services
{
    /// <summary>
    /// 处方模板服务
    /// 管理处方模板的创建、读取、更新和删除
    /// </summary>
    public class PrescriptionTemplateService : IPrescriptionTemplateService
    {
        private readonly ILogger<PrescriptionTemplateService> _logger;
        private readonly IUserSessionManager _userSessionManager;
        private readonly string _templatesFilePath;
        private List<PrescriptionTemplate> _templates;
        private readonly object _lock = new object();

        public PrescriptionTemplateService(
            ILogger<PrescriptionTemplateService> logger,
            IUserSessionManager userSessionManager)
        {
            _logger = logger;
            _userSessionManager = userSessionManager;
            
            // 设置模板文件路径
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "LYBT", "PrescriptionTemplates");
            Directory.CreateDirectory(appFolder);
            _templatesFilePath = Path.Combine(appFolder, "templates.json");
            
            _templates = new List<PrescriptionTemplate>();
            
            // 加载模板
            _ = LoadTemplatesAsync();
        }

        #region 模板管理

        /// <summary>
        /// 获取所有可用模板
        /// </summary>
        public async Task<IEnumerable<PrescriptionTemplate>> GetAvailableTemplatesAsync()
        {
            try
            {
                await EnsureTemplatesLoadedAsync();
                
                var currentUserId = _userSessionManager.CurrentUser?.Id ?? Guid.Empty;
                
                // 返回公开模板和当前用户的私有模板
                return _templates.Where(t => t.IsActive && 
                    (t.IsPublic || t.CreatorId == currentUserId))
                    .OrderBy(t => t.Category)
                    .ThenBy(t => t.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用模板失败");
                return new List<PrescriptionTemplate>();
            }
        }

        /// <summary>
        /// 按分类获取模板
        /// </summary>
        public async Task<IEnumerable<PrescriptionTemplate>> GetTemplatesByCategoryAsync(string category)
        {
            try
            {
                var templates = await GetAvailableTemplatesAsync();
                return templates.Where(t => t.Category == category).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取分类模板失败 - 分类: {category}");
                return new List<PrescriptionTemplate>();
            }
        }

        /// <summary>
        /// 获取个人模板
        /// </summary>
        public async Task<IEnumerable<PrescriptionTemplate>> GetPersonalTemplatesAsync()
        {
            try
            {
                await EnsureTemplatesLoadedAsync();
                
                var currentUserId = _userSessionManager.CurrentUser?.Id ?? Guid.Empty;
                return _templates.Where(t => t.CreatorId == currentUserId && !t.IsPublic)
                    .OrderBy(t => t.CreateTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取个人模板失败");
                return new List<PrescriptionTemplate>();
            }
        }

        /// <summary>
        /// 获取常用模板（按使用次数排序）
        /// </summary>
        public async Task<IEnumerable<PrescriptionTemplate>> GetFrequentlyUsedTemplatesAsync(int topCount = 10)
        {
            try
            {
                var templates = await GetAvailableTemplatesAsync();
                return templates.OrderByDescending(t => t.UsageCount)
                    .Take(topCount)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取常用模板失败");
                return new List<PrescriptionTemplate>();
            }
        }

        /// <summary>
        /// 搜索模板
        /// </summary>
        public async Task<IEnumerable<PrescriptionTemplate>> SearchTemplatesAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return await GetAvailableTemplatesAsync();
                }

                var templates = await GetAvailableTemplatesAsync();
                keyword = keyword.ToLower();
                
                return templates.Where(t =>
                    t.Name.ToLower().Contains(keyword) ||
                    t.Diagnosis.ToLower().Contains(keyword) ||
                    t.Syndrome.ToLower().Contains(keyword) ||
                    t.TreatmentPrinciple.ToLower().Contains(keyword) ||
                    t.Items.Any(i => i.HerbName.ToLower().Contains(keyword)))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"搜索模板失败 - 关键词: {keyword}");
                return new List<PrescriptionTemplate>();
            }
        }

        #endregion

        #region CRUD操作

        /// <summary>
        /// 创建模板
        /// </summary>
        public async Task<bool> CreateTemplateAsync(PrescriptionTemplate template)
        {
            try
            {
                if (template == null)
                {
                    _logger.LogWarning("创建模板失败：模板为空");
                    return false;
                }

                // 设置创建信息
                template.Id = Guid.NewGuid();
                template.CreatorId = _userSessionManager.CurrentUser?.Id ?? Guid.Empty;
                template.CreatorName = _userSessionManager.CurrentUser?.DisplayName ?? "系统";
                template.CreateTime = DateTime.Now;
                template.UsageCount = 0;

                // 验证模板
                if (!ValidateTemplate(template))
                {
                    return false;
                }

                lock (_lock)
                {
                    _templates.Add(template);
                }

                await SaveTemplatesAsync();
                
                _logger.LogInformation($"创建模板成功 - ID: {template.Id}, 名称: {template.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建模板失败");
                return false;
            }
        }

        /// <summary>
        /// 更新模板
        /// </summary>
        public async Task<bool> UpdateTemplateAsync(PrescriptionTemplate template)
        {
            try
            {
                if (template == null)
                {
                    _logger.LogWarning("更新模板失败：模板为空");
                    return false;
                }

                lock (_lock)
                {
                    var existingTemplate = _templates.FirstOrDefault(t => t.Id == template.Id);
                    if (existingTemplate == null)
                    {
                        _logger.LogWarning($"更新模板失败：模板不存在 - ID: {template.Id}");
                        return false;
                    }

                    // 检查权限
                    var currentUserId = _userSessionManager.CurrentUser?.Id ?? Guid.Empty;
                    if (!existingTemplate.IsPublic && existingTemplate.CreatorId != currentUserId)
                    {
                        _logger.LogWarning($"更新模板失败：无权限 - ID: {template.Id}");
                        return false;
                    }

                    // 验证模板
                    if (!ValidateTemplate(template))
                    {
                        return false;
                    }

                    // 更新模板
                    template.UpdateTime = DateTime.Now;
                    var index = _templates.IndexOf(existingTemplate);
                    _templates[index] = template;
                }

                await SaveTemplatesAsync();
                
                _logger.LogInformation($"更新模板成功 - ID: {template.Id}, 名称: {template.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新模板失败");
                return false;
            }
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        public async Task<bool> DeleteTemplateAsync(Guid templateId)
        {
            try
            {
                lock (_lock)
                {
                    var template = _templates.FirstOrDefault(t => t.Id == templateId);
                    if (template == null)
                    {
                        _logger.LogWarning($"删除模板失败：模板不存在 - ID: {templateId}");
                        return false;
                    }

                    // 检查权限
                    var currentUserId = _userSessionManager.CurrentUser?.Id ?? Guid.Empty;
                    if (!template.IsPublic && template.CreatorId != currentUserId)
                    {
                        _logger.LogWarning($"删除模板失败：无权限 - ID: {templateId}");
                        return false;
                    }

                    _templates.Remove(template);
                }

                await SaveTemplatesAsync();
                
                _logger.LogInformation($"删除模板成功 - ID: {templateId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除模板失败 - ID: {templateId}");
                return false;
            }
        }

        /// <summary>
        /// 根据ID获取模板
        /// </summary>
        public async Task<PrescriptionTemplate?> GetTemplateByIdAsync(Guid templateId)
        {
            try
            {
                await EnsureTemplatesLoadedAsync();
                return _templates.FirstOrDefault(t => t.Id == templateId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取模板失败 - ID: {templateId}");
                return null;
            }
        }

        #endregion

        #region 模板应用

        /// <summary>
        /// 应用模板创建处方
        /// </summary>
        public async Task<PrescriptionInfo?> ApplyTemplateAsync(Guid templateId, Guid patientId)
        {
            try
            {
                var template = await GetTemplateByIdAsync(templateId);
                if (template == null)
                {
                    _logger.LogWarning($"应用模板失败：模板不存在 - ID: {templateId}");
                    return null;
                }

                // 应用模板
                var prescription = template.ApplyToNewPrescription(patientId);
                
                // 保存使用次数
                await SaveTemplatesAsync();
                
                _logger.LogInformation($"应用模板成功 - 模板: {template.Name}, 患者: {patientId}");
                return prescription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"应用模板失败 - ID: {templateId}");
                return null;
            }
        }

        /// <summary>
        /// 从处方创建模板
        /// </summary>
        public async Task<bool> CreateTemplateFromPrescriptionAsync(
            PrescriptionInfo prescription,
            string templateName,
            string category,
            bool isPublic)
        {
            try
            {
                if (prescription == null)
                {
                    _logger.LogWarning("从处方创建模板失败：处方为空");
                    return false;
                }

                var template = new PrescriptionTemplate
                {
                    Name = templateName,
                    Category = category,
                    Diagnosis = prescription.Diagnosis ?? string.Empty,
                    Usage = prescription.Usage ?? string.Empty,
                    DosageCount = prescription.DosageCount,
                    Remark = prescription.Remark ?? string.Empty,
                    IsPublic = isPublic,
                    IsActive = true,
                    Items = new List<PrescriptionTemplateItem>()
                };

                // 复制药材项目
                if (prescription.Items != null)
                {
                    int sortOrder = 1;
                    foreach (var item in prescription.Items)
                    {
                        template.Items.Add(new PrescriptionTemplateItem
                        {
                            HerbId = item.HerbId,
                            HerbName = item.HerbName,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            EstimatedPrice = item.Price,
                            SortOrder = sortOrder++
                        });
                    }
                }

                return await CreateTemplateAsync(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从处方创建模板失败");
                return false;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 确保模板已加载
        /// </summary>
        private async Task EnsureTemplatesLoadedAsync()
        {
            if (_templates == null || !_templates.Any())
            {
                await LoadTemplatesAsync();
            }
        }

        /// <summary>
        /// 加载模板
        /// </summary>
        private async Task LoadTemplatesAsync()
        {
            try
            {
                if (File.Exists(_templatesFilePath))
                {
                    var json = await File.ReadAllTextAsync(_templatesFilePath);
                    _templates = JsonSerializer.Deserialize<List<PrescriptionTemplate>>(json) ?? new List<PrescriptionTemplate>();
                    _logger.LogInformation($"加载模板成功 - 数量: {_templates.Count}");
                }
                else
                {
                    // 创建默认模板
                    await CreateDefaultTemplatesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载模板失败");
                _templates = new List<PrescriptionTemplate>();
            }
        }

        /// <summary>
        /// 保存模板
        /// </summary>
        private async Task SaveTemplatesAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_templates, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(_templatesFilePath, json);
                _logger.LogInformation("保存模板成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存模板失败");
            }
        }

        /// <summary>
        /// 创建默认模板
        /// </summary>
        private async Task CreateDefaultTemplatesAsync()
        {
            _templates = new List<PrescriptionTemplate>();

            // 添加一些经典方剂作为默认模板
            var defaultTemplates = new[]
            {
                CreateClassicTemplate("麻黄汤", "感冒类", "外感风寒表实证", "发汗解表，宣肺平喘",
                    new[] { ("麻黄", 9m), ("桂枝", 6m), ("杏仁", 9m), ("甘草", 3m) }),
                
                CreateClassicTemplate("小柴胡汤", "肝胆类", "少阳证", "和解少阳",
                    new[] { ("柴胡", 12m), ("黄芩", 9m), ("半夏", 9m), ("人参", 6m), 
                           ("甘草", 6m), ("生姜", 9m), ("大枣", 4m) }),
                
                CreateClassicTemplate("四君子汤", "脾胃类", "脾胃气虚证", "益气健脾",
                    new[] { ("人参", 10m), ("白术", 10m), ("茯苓", 10m), ("甘草", 6m) }),
                
                CreateClassicTemplate("六味地黄丸", "肾系类", "肾阴虚证", "滋阴补肾",
                    new[] { ("熟地黄", 24m), ("山药", 12m), ("山茱萸", 12m), 
                           ("茯苓", 9m), ("泽泻", 9m), ("牡丹皮", 9m) })
            };

            foreach (var template in defaultTemplates)
            {
                _templates.Add(template);
            }

            await SaveTemplatesAsync();
            _logger.LogInformation($"创建默认模板成功 - 数量: {_templates.Count}");
        }

        /// <summary>
        /// 创建经典方剂模板
        /// </summary>
        private PrescriptionTemplate CreateClassicTemplate(
            string name, 
            string category, 
            string syndrome,
            string principle,
            (string name, decimal quantity)[] herbs)
        {
            var template = new PrescriptionTemplate
            {
                Id = Guid.NewGuid(),
                Name = name,
                Category = category,
                Syndrome = syndrome,
                TreatmentPrinciple = principle,
                Usage = "水煎服，每日一剂，分两次服用",
                DosageCount = 7,
                IsPublic = true,
                IsActive = true,
                CreatorId = Guid.Empty,
                CreatorName = "系统",
                CreateTime = DateTime.Now,
                Items = new List<PrescriptionTemplateItem>()
            };

            int sortOrder = 1;
            foreach (var (herbName, quantity) in herbs)
            {
                template.Items.Add(new PrescriptionTemplateItem
                {
                    Id = Guid.NewGuid(),
                    TemplateId = template.Id,
                    HerbId = Guid.NewGuid(), // 实际使用时需要关联真实药材ID
                    HerbName = herbName,
                    Quantity = quantity,
                    Unit = "g",
                    EstimatedPrice = 5.0m, // 默认估价
                    SortOrder = sortOrder++
                });
            }

            return template;
        }

        /// <summary>
        /// 验证模板
        /// </summary>
        private bool ValidateTemplate(PrescriptionTemplate template)
        {
            if (string.IsNullOrWhiteSpace(template.Name))
            {
                _logger.LogWarning("模板验证失败：名称为空");
                return false;
            }

            if (template.Items == null || !template.Items.Any())
            {
                _logger.LogWarning("模板验证失败：药材项目为空");
                return false;
            }

            if (template.DosageCount <= 0)
            {
                _logger.LogWarning("模板验证失败：剂数无效");
                return false;
            }

            return true;
        }

        #endregion
    }

    #region 接口定义

    /// <summary>
    /// 处方模板服务接口
    /// </summary>
    public interface IPrescriptionTemplateService
    {
        // 模板查询
        Task<IEnumerable<PrescriptionTemplate>> GetAvailableTemplatesAsync();
        Task<IEnumerable<PrescriptionTemplate>> GetTemplatesByCategoryAsync(string category);
        Task<IEnumerable<PrescriptionTemplate>> GetPersonalTemplatesAsync();
        Task<IEnumerable<PrescriptionTemplate>> GetFrequentlyUsedTemplatesAsync(int topCount = 10);
        Task<IEnumerable<PrescriptionTemplate>> SearchTemplatesAsync(string keyword);
        Task<PrescriptionTemplate?> GetTemplateByIdAsync(Guid templateId);

        // CRUD操作
        Task<bool> CreateTemplateAsync(PrescriptionTemplate template);
        Task<bool> UpdateTemplateAsync(PrescriptionTemplate template);
        Task<bool> DeleteTemplateAsync(Guid templateId);

        // 模板应用
        Task<PrescriptionInfo?> ApplyTemplateAsync(Guid templateId, Guid patientId);
        Task<bool> CreateTemplateFromPrescriptionAsync(
            PrescriptionInfo prescription,
            string templateName,
            string category,
            bool isPublic);
    }

    #endregion
}