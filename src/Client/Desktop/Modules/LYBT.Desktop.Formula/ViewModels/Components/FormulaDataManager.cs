using System.Collections.ObjectModel;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.ViewModels.Components
{
    /// <summary>
    /// 配方数据管理器 - 组件化架构实现
    /// Issue #1153: 负责配方数据的加载、刷新和状态管理
    /// </summary>
    public class FormulaDataManager
    {
        private readonly IFormulaRepository _repository;
        private readonly IHerbRepository _herbRepository;
        private readonly ILogger<FormulaDataManager> _logger;

        public FormulaDataManager(
            IFormulaRepository repository,
            IHerbRepository herbRepository,
            ILogger<FormulaDataManager> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 数据加载

        /// <summary>
        /// 加载配方详情
        /// </summary>
        public async Task<(bool success, FormulaDto? formula, string? errorMessage)> LoadFormulaAsync(Guid formulaId)
        {
            if (formulaId == Guid.Empty)
            {
                return (false, null, "配方ID无效");
            }

            try
            {
                _logger.LogInformation("加载配方详情: {FormulaId}", formulaId);

                var formula = await _repository.GetByIdAsync(formulaId);

                if (formula == null)
                {
                    return (false, null, "未找到指定的配方");
                }

                return (true, formula, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载配方详情时发生异常: {FormulaId}", formulaId);
                return (false, null, "加载配方详情时发生系统错误，请稍后重试");
            }
        }

        /// <summary>
        /// 刷新配方数据
        /// </summary>
        public async Task<(bool success, FormulaDto? formula, string? errorMessage)> RefreshFormulaAsync(Guid formulaId)
        {
            try
            {
                _logger.LogInformation("刷新配方数据: {FormulaId}", formulaId);

                return await LoadFormulaAsync(formulaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新配方数据时发生异常: {FormulaId}", formulaId);
                return (false, null, "刷新配方数据时发生系统错误");
            }
        }

        #endregion

        #region 药材集合管理

        /// <summary>
        /// 加载药材列表到集合
        /// </summary>
        public void LoadHerbItems(
            ObservableCollection<FormulaHerbItemDto> targetCollection,
            IEnumerable<FormulaHerbItemDto>? sourceItems)
        {
            if (targetCollection == null)
            {
                throw new ArgumentNullException(nameof(targetCollection));
            }

            try
            {
                targetCollection.Clear();

                if (sourceItems != null)
                {
                    foreach (var item in sourceItems)
                    {
                        targetCollection.Add(item);
                    }
                }

                _logger.LogDebug("已加载 {Count} 个药材项", targetCollection.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载药材列表时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 添加药材项
        /// </summary>
        public void AddHerbItem(
            ObservableCollection<FormulaHerbItemDto> collection,
            FormulaHerbItemDto item)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            try
            {
                // 设置排序顺序
                item.SortOrder = collection.Count;
                collection.Add(item);

                _logger.LogDebug("添加药材项: {HerbName}", item.HerbName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加药材项时发生异常: {HerbName}", item.HerbName);
                throw;
            }
        }

        /// <summary>
        /// 移除药材项
        /// </summary>
        public bool RemoveHerbItem(
            ObservableCollection<FormulaHerbItemDto> collection,
            FormulaHerbItemDto item)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            try
            {
                var result = collection.Remove(item);

                if (result)
                {
                    // 重新排序
                    ReorderHerbItems(collection);
                    _logger.LogDebug("移除药材项: {HerbName}", item.HerbName);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除药材项时发生异常: {HerbName}", item.HerbName);
                throw;
            }
        }

        /// <summary>
        /// 清空药材列表
        /// </summary>
        public void ClearHerbItems(ObservableCollection<FormulaHerbItemDto> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            try
            {
                var count = collection.Count;
                collection.Clear();
                _logger.LogDebug("已清空 {Count} 个药材项", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空药材列表时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 重新排序药材项
        /// </summary>
        public void ReorderHerbItems(ObservableCollection<FormulaHerbItemDto> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            try
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    collection[i].SortOrder = i;
                }

                _logger.LogDebug("重新排序 {Count} 个药材项", collection.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新排序药材项时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 移动药材项位置
        /// </summary>
        public bool MoveHerbItem(
            ObservableCollection<FormulaHerbItemDto> collection,
            int oldIndex,
            int newIndex)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (oldIndex < 0 || oldIndex >= collection.Count ||
                newIndex < 0 || newIndex >= collection.Count)
            {
                return false;
            }

            try
            {
                collection.Move(oldIndex, newIndex);
                ReorderHerbItems(collection);

                _logger.LogDebug("移动药材项: {OldIndex} → {NewIndex}", oldIndex, newIndex);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移动药材项时发生异常: {OldIndex} → {NewIndex}", oldIndex, newIndex);
                throw;
            }
        }

        #endregion

        #region 数据状态管理

        /// <summary>
        /// 检查配方是否已加载
        /// </summary>
        public bool IsFormulaLoaded(FormulaDto? formula)
        {
            return formula != null && formula.Id != Guid.Empty;
        }

        /// <summary>
        /// 检查配方是否有药材
        /// </summary>
        public bool HasHerbItems(FormulaDto? formula)
        {
            return formula?.Herbs != null && formula.Herbs.Any();
        }

        /// <summary>
        /// 获取药材数量
        /// </summary>
        public int GetHerbItemCount(IEnumerable<FormulaHerbItemDto>? items)
        {
            return items?.Count() ?? 0;
        }

        /// <summary>
        /// 获取配方总价格
        /// </summary>
        public decimal CalculateTotalPrice(IEnumerable<FormulaHerbItemDto>? items)
        {
            if (items == null || !items.Any())
            {
                return 0m;
            }

            try
            {
                return items.Sum(h => h.Price * h.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算配方总价格时发生异常");
                return 0m;
            }
        }

        /// <summary>
        /// 创建配方数据快照（用于取消编辑时恢复）
        /// </summary>
        public FormulaDataSnapshot CreateSnapshot(FormulaDto formula)
        {
            if (formula == null)
            {
                throw new ArgumentNullException(nameof(formula));
            }

            return new FormulaDataSnapshot
            {
                Name = formula.Name,
                Effect = formula.Effect,
                Usage = formula.Usage,
                Property = formula.Property,
                Remark = formula.Remark,
                IsShared = formula.IsShared,
                Category = formula.Category,
                HerbItems = formula.Herbs?.Select(h => new FormulaHerbItemDto
                {
                    Id = h.Id,
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Quantity = h.Quantity,
                    Preparation = h.Preparation,
                    Usage = h.Usage,
                    SortOrder = h.SortOrder,
                    Price = h.Price
                }).ToList() ?? new List<FormulaHerbItemDto>()
            };
        }

        /// <summary>
        /// 从快照恢复数据
        /// </summary>
        public void RestoreFromSnapshot(
            FormulaDataSnapshot snapshot,
            Action<string> setName,
            Action<string> setEffect,
            Action<string> setUsage,
            Action<string> setProperty,
            Action<string> setRemark,
            Action<bool> setIsShared,
            Action<string> setCategory,
            ObservableCollection<FormulaHerbItemDto> herbCollection)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            try
            {
                setName?.Invoke(snapshot.Name ?? string.Empty);
                setEffect?.Invoke(snapshot.Effect ?? string.Empty);
                setUsage?.Invoke(snapshot.Usage ?? string.Empty);
                setProperty?.Invoke(snapshot.Property ?? string.Empty);
                setRemark?.Invoke(snapshot.Remark ?? string.Empty);
                setIsShared?.Invoke(snapshot.IsShared);
                setCategory?.Invoke(snapshot.Category ?? string.Empty);

                LoadHerbItems(herbCollection, snapshot.HerbItems);

                _logger.LogDebug("已从快照恢复数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从快照恢复数据时发生异常");
                throw;
            }
        }

        #endregion

        #region 8列模型转换（Issue #2073）

        /// <summary>
        /// 将FormulaItemRow集合转换为FormulaHerbItemDto列表
        /// </summary>
        /// <param name="rows">FormulaItemRow集合</param>
        /// <returns>FormulaHerbItemDto列表，自动重新设置SortOrder</returns>
        public List<FormulaHerbItemDto> ConvertRowsToHerbItems(
            ObservableCollection<LYBT.Desktop.Formula.Models.FormulaItemRow> rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            try
            {
                var herbItems = new List<FormulaHerbItemDto>();

                // 遍历每一行，调用ToHerbItems()获取药材项
                foreach (var row in rows)
                {
                    herbItems.AddRange(row.ToHerbItems());
                }

                // 重新设置SortOrder（0, 1, 2, 3...）
                for (int i = 0; i < herbItems.Count; i++)
                {
                    herbItems[i].SortOrder = i;
                }

                _logger.LogDebug("已将 {RowCount} 行转换为 {HerbCount} 个药材项", rows.Count, herbItems.Count);
                return herbItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "转换FormulaItemRow到HerbItems时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 将FormulaHerbItemDto列表转换为FormulaItemRow集合（异步方法，需要加载HerbDto）
        /// </summary>
        /// <param name="herbItems">FormulaHerbItemDto列表</param>
        /// <returns>FormulaItemRow集合，每行包含4个药材</returns>
        public async Task<ObservableCollection<LYBT.Desktop.Formula.Models.FormulaItemRow>> ConvertHerbItemsToRowsAsync(
            List<FormulaHerbItemDto>? herbItems)
        {
            var rows = new ObservableCollection<LYBT.Desktop.Formula.Models.FormulaItemRow>();

            if (herbItems == null || !herbItems.Any())
            {
                return rows;
            }

            try
            {
                // 按SortOrder排序
                var sortedItems = herbItems.OrderBy(h => h.SortOrder).ToList();

                // 4个药材一组转换为FormulaItemRow
                for (int i = 0; i < sortedItems.Count; i += 4)
                {
                    var row = new LYBT.Desktop.Formula.Models.FormulaItemRow();

                    // 第1个药材
                    if (i < sortedItems.Count)
                    {
                        var item1 = sortedItems[i];
                        row.Herb1 = item1.HerbId.HasValue
                            ? await _herbRepository.GetByIdAsync(item1.HerbId.Value)
                            : null;
                        row.Quantity1 = item1.Quantity;
                    }

                    // 第2个药材
                    if (i + 1 < sortedItems.Count)
                    {
                        var item2 = sortedItems[i + 1];
                        row.Herb2 = item2.HerbId.HasValue
                            ? await _herbRepository.GetByIdAsync(item2.HerbId.Value)
                            : null;
                        row.Quantity2 = item2.Quantity;
                    }

                    // 第3个药材
                    if (i + 2 < sortedItems.Count)
                    {
                        var item3 = sortedItems[i + 2];
                        row.Herb3 = item3.HerbId.HasValue
                            ? await _herbRepository.GetByIdAsync(item3.HerbId.Value)
                            : null;
                        row.Quantity3 = item3.Quantity;
                    }

                    // 第4个药材
                    if (i + 3 < sortedItems.Count)
                    {
                        var item4 = sortedItems[i + 3];
                        row.Herb4 = item4.HerbId.HasValue
                            ? await _herbRepository.GetByIdAsync(item4.HerbId.Value)
                            : null;
                        row.Quantity4 = item4.Quantity;
                    }

                    rows.Add(row);
                }

                _logger.LogDebug("已将 {HerbCount} 个药材项转换为 {RowCount} 行", sortedItems.Count, rows.Count);
                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "转换HerbItems到FormulaItemRow时发生异常");
                throw;
            }
        }

        #endregion

        #region 配方复制（Issue #2082）

        /// <summary>
        /// 创建验方副本（用于复制功能）
        /// </summary>
        /// <param name="sourceFormula">源验方</param>
        /// <param name="currentUserName">当前用户名（作为CreatedBy）</param>
        /// <returns>新验方副本（新Id，CreatedBy为当前用户）</returns>
        public FormulaDto CreateFormulaCopy(FormulaDto sourceFormula, string currentUserName)
        {
            if (sourceFormula == null)
            {
                throw new ArgumentNullException(nameof(sourceFormula));
            }

            if (string.IsNullOrWhiteSpace(currentUserName))
            {
                throw new ArgumentNullException(nameof(currentUserName));
            }

            try
            {
                _logger.LogInformation("创建验方副本: {SourceFormulaId}, CurrentUser: {UserName}", sourceFormula.Id, currentUserName);

                // 1. 复制验方基础信息（Name保持相同 - 用户需求）
                var copiedFormula = new FormulaDto
                {
                    Id = Guid.Empty, // 新Id（保存时由Repository生成）
                    Name = sourceFormula.Name, // Name保持相同（需求要求）
                    PinYinCode = sourceFormula.PinYinCode, // 拼音码
                    Effect = sourceFormula.Effect, // 功效
                    Usage = sourceFormula.Usage, // 用法
                    Property = sourceFormula.Property, // 性味归经
                    Remark = sourceFormula.Remark, // 备注
                    IsShared = sourceFormula.IsShared, // 是否共享
                    Status = sourceFormula.Status, // 状态
                    ValidationStatus = sourceFormula.ValidationStatus, // 验证状态
                    Source = sourceFormula.Source, // 来源
                    Description = sourceFormula.Description, // 描述
                    Indications = sourceFormula.Indications, // 主治
                    Contraindications = sourceFormula.Contraindications, // 禁忌症
                    CreatedAt = DateTime.Now, // 设置为当前时间（注意：currentUserName参数预留，实际创建者由Repository层处理）
                    Herbs = new List<FormulaHerbItemDto>()
                };

                // 2. 深拷贝药材列表
                if (sourceFormula.Herbs != null)
                {
                    foreach (var herb in sourceFormula.Herbs)
                    {
                        copiedFormula.Herbs.Add(new FormulaHerbItemDto
                        {
                            Id = Guid.Empty, // 新Guid（保存时由Repository生成）
                            HerbId = herb.HerbId, // 药材ID
                            HerbName = herb.HerbName, // 药材名称
                            OriginalHerbName = herb.OriginalHerbName, // 原始药材名称
                            IsValidated = herb.IsValidated, // 是否已验证
                            Quantity = herb.Quantity, // 用量
                            Unit = herb.Unit, // 单位
                            Preparation = herb.Preparation, // 炮制方法
                            ProcessingMethod = herb.ProcessingMethod, // 加工方法
                            Usage = herb.Usage, // 用法
                            SpecialInstructions = herb.SpecialInstructions, // 特殊说明
                            SortOrder = herb.SortOrder // 排序
                            // 注意：不复制Price字段（副本创建时价格需重新计算）
                        });
                    }
                }

                _logger.LogInformation("验方副本创建成功，包含 {HerbCount} 个药材", copiedFormula.Herbs.Count);
                return copiedFormula;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建验方副本时发生异常");
                throw;
            }
        }

        #endregion
    }

    /// <summary>
    /// 配方数据快照 - 用于编辑取消时恢复原始数据
    /// </summary>
    public class FormulaDataSnapshot
    {
        public string? Name { get; set; }
        public string? Effect { get; set; }
        public string? Usage { get; set; }
        public string? Property { get; set; }
        public string? Remark { get; set; }
        public bool IsShared { get; set; }
        public string? Category { get; set; }
        public List<FormulaHerbItemDto> HerbItems { get; set; } = new();
    }
}
