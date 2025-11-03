using LYBT.Desktop.Contracts.Api; // Issue #1606 Phase 3: 改用IPrescriptionApi
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Services
{
    /// <summary>
    /// 处方编辑器服务实现（Epic #1540 方案B）
    ///
    /// 架构角色：
    /// - 实现IPrescriptionEditorService接口（定义在Desktop.Contracts）
    /// - 被MedicalCase模块调用，提供处方编辑器辅助功能
    /// - 复用Prescriptions模块的Repository和业务逻辑
    ///
    /// 依赖倒置实现：
    /// - MedicalCase模块依赖IPrescriptionEditorService接口
    /// - 本服务实现接口，提供具体功能
    /// - 打破MedicalCase ↔ Prescriptions循环依赖
    ///
    /// 与Issue #1477协调：
    /// - 功能分层：辅助层功能（处方编辑器辅助工具）
    /// - 写入控制：提供草稿构建能力，最终写入由MedicalCase聚合根控制
    /// </summary>
    public class PrescriptionEditorService : IPrescriptionEditorService
    {
        #region 依赖注入

        private readonly IPrescriptionApi _prescriptionApi; // Issue #1606 Phase 3: 改用IPrescriptionApi（只读）
        private readonly IHerbRepository _herbRepository;
        private readonly ILogger<PrescriptionEditorService> _logger;

        // 缓存药材数据
        private List<HerbDto>? _cachedHerbs;

        #endregion

        #region 构造函数

        public PrescriptionEditorService(
            IPrescriptionApi prescriptionApi, // Issue #1606 Phase 3: 改用IPrescriptionApi
            IHerbRepository herbRepository,
            ILogger<PrescriptionEditorService> logger)
        {
            _prescriptionApi = prescriptionApi ?? throw new ArgumentNullException(nameof(prescriptionApi)); // Issue #1606 Phase 3
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 1. 药材数据管理

        /// <inheritdoc/>
        public async Task<IEnumerable<HerbDto>> LoadAllHerbsAsync()
        {
            try
            {
                _logger.LogInformation("加载所有药材数据");

                // 如果已缓存，直接返回
                if (_cachedHerbs != null)
                {
                    _logger.LogDebug("返回缓存的药材数据（{Count}条）", _cachedHerbs.Count);
                    return _cachedHerbs;
                }

                // 从Repository加载（使用SearchAsync获取所有药材）
                var herbs = await _herbRepository.SearchAsync("");
                if (herbs != null && herbs.Any())
                {
                    _cachedHerbs = herbs;
                    _logger.LogInformation("成功加载{Count}条药材数据", _cachedHerbs.Count);
                    return _cachedHerbs;
                }

                _logger.LogWarning("加载药材数据失败，返回空列表");
                return Enumerable.Empty<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载药材数据时发生异常");
                return Enumerable.Empty<HerbDto>();
            }
        }

        /// <inheritdoc/>
        public IEnumerable<HerbDto> FilterHerbs(string searchText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    return _cachedHerbs ?? Enumerable.Empty<HerbDto>();
                }

                if (_cachedHerbs == null || _cachedHerbs.Count == 0)
                {
                    _logger.LogWarning("药材数据未加载，过滤操作返回空结果");
                    return Enumerable.Empty<HerbDto>();
                }

                var searchLower = searchText.Trim().ToLower();

                // 支持药材名称和拼音码模糊匹配
                var filtered = _cachedHerbs.Where(h =>
                    h.Name.ToLower().Contains(searchLower) ||
                    (!string.IsNullOrEmpty(h.PinYinCode) && h.PinYinCode.ToLower().Contains(searchLower))
                ).ToList();

                _logger.LogDebug("过滤药材：搜索'{SearchText}'，匹配{Count}条", searchText, filtered.Count);
                return filtered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "过滤药材数据时发生异常");
                return Enumerable.Empty<HerbDto>();
            }
        }

        #endregion

        #region 2. 历史处方管理

        /// <inheritdoc/>
        public async Task<IEnumerable<PrescriptionSearchResultDto>> LoadRecentPrescriptionsAsync(Guid patientId, int limit = 10)
        {
            try
            {
                _logger.LogInformation("加载患者{PatientId}的最近{Limit}条处方记录", patientId, limit);

                // Issue #1606 Phase 3: 改用IPrescriptionApi（只读）
                var response = await _prescriptionApi.GetPatientRecentPrescriptionsAsync(patientId, limit);
                var prescriptions = response?.Data;
                if (prescriptions != null && prescriptions.Any())
                {
                    _logger.LogInformation("成功加载{Count}条历史处方", prescriptions.Count);
                    return prescriptions;
                }

                _logger.LogWarning("加载历史处方失败，返回空列表");
                return Enumerable.Empty<PrescriptionSearchResultDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载历史处方时发生异常");
                return Enumerable.Empty<PrescriptionSearchResultDto>();
            }
        }

        #endregion

        #region 3. 验方导入

        /// <inheritdoc/>
        public Task<IEnumerable<FormulaDto>> LoadFormulasAsync()
        {
            try
            {
                _logger.LogInformation("加载所有验方数据");

                // 注意：这里需要通过Repository加载验方数据
                // 由于当前没有IFormulaRepository注入，暂时返回空列表
                // TODO: 添加IFormulaRepository依赖注入
                _logger.LogWarning("IFormulaRepository未注入，返回空验方列表");
                return Task.FromResult(Enumerable.Empty<FormulaDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载验方数据时发生异常");
                return Task.FromResult(Enumerable.Empty<FormulaDto>());
            }
        }

        /// <inheritdoc/>
        public Task<PrescriptionDto> ImportFormulaAsync(Guid formulaId)
        {
            try
            {
                _logger.LogInformation("从验方{FormulaId}导入处方数据", formulaId);

                // TODO: 实现验方导入逻辑
                // 1. 从IFormulaRepository加载验方详情
                // 2. 将验方的FormulaItems转换为PrescriptionItemDto列表
                // 3. 构建PrescriptionDto对象
                _logger.LogWarning("验方导入功能尚未实现，返回空处方");

                return Task.FromResult(new PrescriptionDto
                {
                    Id = Guid.NewGuid(),
                    Items = new List<PrescriptionItemDto>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入验方时发生异常");
                throw;
            }
        }

        #endregion

        #region 4. 处方数据操作

        /// <inheritdoc/>
        public async Task<PrescriptionDto> BuildPrescriptionDraftAsync(PrescriptionCreateDto dto)
        {
            try
            {
                _logger.LogInformation("构建处方草稿");

                // 将PrescriptionCreateDto转换为PrescriptionDto
                var prescription = new PrescriptionDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = dto.PatientId,
                    UserId = dto.DoctorId,
                    MedicalCaseId = dto.ConsultationId ?? Guid.Empty,
                    DosageCount = dto.Quantity,
                    Usage = dto.Usage,
                    Advice = dto.Advice,
                    Remark = dto.Notes,
                    FormulaSource = dto.FormulaSource,
                    Discount = 1.0m,
                    Items = dto.Items.Select(item => new PrescriptionItemDto
                    {
                        Id = Guid.NewGuid(),
                        HerbId = item.HerbId,
                        HerbName = item.HerbName ?? string.Empty,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Dosage = item.Quantity,
                        Subtotal = item.Subtotal,
                        Usage = item.Usage,
                        Remark = item.Remark
                    }).ToList()
                };

                _logger.LogInformation("成功构建处方草稿，包含{Count}个处方项", prescription.Items.Count);
                return await Task.FromResult(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建处方草稿时发生异常");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ValidatePrescriptionAsync(PrescriptionDto prescription)
        {
            try
            {
                _logger.LogInformation("验证处方数据");

                // 基本验证规则
                if (prescription == null)
                {
                    _logger.LogWarning("处方数据为空");
                    return false;
                }

                if (prescription.Items == null || prescription.Items.Count == 0)
                {
                    _logger.LogWarning("处方项列表为空");
                    return false;
                }

                if (prescription.DosageCount <= 0)
                {
                    _logger.LogWarning("剂数无效：{DosageCount}", prescription.DosageCount);
                    return false;
                }

                // 检查药材重复
                var herbIds = prescription.Items.Select(i => i.HerbId).ToList();
                var duplicates = herbIds.GroupBy(id => id).Where(g => g.Count() > 1).ToList();
                if (duplicates.Any())
                {
                    _logger.LogWarning("发现重复药材：{Count}个", duplicates.Count);
                    return false;
                }

                _logger.LogInformation("处方数据验证通过");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方数据时发生异常");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<decimal> CalculateTotalAmountAsync(IEnumerable<PrescriptionItemDto> items, int dosageCount = 7, decimal discount = 1.0m)
        {
            try
            {
                if (items == null || !items.Any())
                {
                    return 0m;
                }

                // 计算单帖价格（所有药材的小计之和）
                var singleDosePrice = items.Sum(item => item.UnitPrice * item.Quantity);

                // 计算总价格（单帖价格 × 剂数 × 折扣）
                var totalPrice = singleDosePrice * dosageCount * discount;

                _logger.LogDebug("价格计算：单帖={SingleDose}，剂数={Dosage}，折扣={Discount}，总价={Total}",
                    singleDosePrice, dosageCount, discount, totalPrice);

                return await Task.FromResult(totalPrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算总金额时发生异常");
                return 0m;
            }
        }

        #endregion

        #region 5. 事件通知

        /// <inheritdoc/>
        public event EventHandler<PrescriptionChangedEventArgs>? PrescriptionChanged;

        /// <summary>
        /// 触发处方变更事件
        /// </summary>
        protected virtual void OnPrescriptionChanged(PrescriptionDto? prescription, PrescriptionChangeType changeType)
        {
            PrescriptionChanged?.Invoke(this, new PrescriptionChangedEventArgs
            {
                Prescription = prescription,
                ChangeType = changeType,
                ChangedAt = DateTime.Now
            });
        }

        #endregion
    }
}
