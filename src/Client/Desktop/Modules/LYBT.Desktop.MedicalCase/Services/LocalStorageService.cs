using System.IO;
using System.Text.Json;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// 本地存储服务实现（Issue #1502 - 自动保存草稿功能）
    /// 负责将医案流程草稿保存到本地文件系统
    /// </summary>
    public class LocalStorageService : ILocalStorageService
    {
        private readonly ILogger<LocalStorageService> _logger;
        private readonly string _draftFilePath;

        public LocalStorageService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger<LocalStorageService>() ?? throw new ArgumentNullException(nameof(loggerFactory));

            // 确定草稿文件路径：AppData/Local/LYBT/Drafts/medical-case-draft.json
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var draftsFolder = Path.Combine(appDataPath, "LYBT", "Drafts");

            // 确保目录存在
            if (!Directory.Exists(draftsFolder))
            {
                Directory.CreateDirectory(draftsFolder);
                _logger.LogInformation("创建草稿目录：{DraftsFolder}", draftsFolder);
            }

            _draftFilePath = Path.Combine(draftsFolder, "medical-case-draft.json");
            _logger.LogInformation("LocalStorageService已初始化，草稿文件路径：{DraftFilePath}", _draftFilePath);
        }

        /// <summary>
        /// 保存草稿到本地存储
        /// </summary>
        public async Task SaveDraftAsync(FlowDraftState state)
        {
            try
            {
                _logger.LogInformation("开始保存草稿，CurrentStep: {CurrentStep}, MedicalCaseId: {MedicalCaseId}",
                    state.CurrentStep, state.MedicalCaseId);

                // 序列化为JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(state, options);

                // 写入文件
                await File.WriteAllTextAsync(_draftFilePath, json);

                _logger.LogInformation("草稿保存成功，文件大小：{FileSize} bytes", json.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存草稿失败");
                throw;
            }
        }

        /// <summary>
        /// 从本地存储加载草稿
        /// </summary>
        public async Task<FlowDraftState?> LoadDraftAsync()
        {
            try
            {
                if (!File.Exists(_draftFilePath))
                {
                    _logger.LogInformation("草稿文件不存在：{DraftFilePath}", _draftFilePath);
                    return null;
                }

                // 读取文件
                var json = await File.ReadAllTextAsync(_draftFilePath);

                // 反序列化
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var state = JsonSerializer.Deserialize<FlowDraftState>(json, options);

                if (state != null)
                {
                    _logger.LogInformation("草稿加载成功，SavedAt: {SavedAt}, CurrentStep: {CurrentStep}",
                        state.SavedAt, state.CurrentStep);
                }
                else
                {
                    _logger.LogWarning("草稿文件内容为空或无效");
                }

                return state;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载草稿失败");
                return null;
            }
        }

        /// <summary>
        /// 清除本地草稿
        /// </summary>
        public async Task ClearDraftAsync()
        {
            try
            {
                if (File.Exists(_draftFilePath))
                {
                    await Task.Run(() => File.Delete(_draftFilePath));
                    _logger.LogInformation("草稿文件已删除：{DraftFilePath}", _draftFilePath);
                }
                else
                {
                    _logger.LogInformation("草稿文件不存在，无需删除");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除草稿文件失败");
                throw;
            }
        }
    }
}
