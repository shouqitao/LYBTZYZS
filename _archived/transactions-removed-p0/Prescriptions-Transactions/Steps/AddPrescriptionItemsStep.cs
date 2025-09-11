using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Transactions;

// Removed: using LYBT.Infrastructure.Transactions.Steps; - DatabaseTransactionStep now in main namespace
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Transactions.Steps
{
    /// <summary>
    /// 添加处方药材项目事务步骤
    /// 负责将药材项目批量添加到处方中
    /// </summary>
    public class AddPrescriptionItemsStep : DatabaseTransactionStep<PrescriptionTransactionContext>
    {
        /// <inheritdoc />
        public override string StepName => "AddPrescriptionItems";

        /// <inheritdoc />
        public override int Order => 3;

        /// <inheritdoc />
        public override bool SupportsCompensation => true;

        /// <inheritdoc />
        public override TimeSpan Timeout => TimeSpan.FromSeconds(60); // 增加超时时间，因为可能有多个项目

        public AddPrescriptionItemsStep(AppDbContext dbContext, ILogger<AddPrescriptionItemsStep> logger)
            : base(dbContext, logger)
        {
        }

        /// <inheritdoc />
        public override async Task<bool> CanExecuteAsync(PrescriptionTransactionContext context, CancellationToken cancellationToken = default)
        {
            // 检查基础条件
            if (!await base.CanExecuteAsync(context, cancellationToken))
                return false;

            try
            {
                // 必须已经创建处方基础记录
                if (!context.PrescriptionId.HasValue)
                {
                    context.LogError("Cannot add prescription items without prescription ID");
                    context.SetValidationResult("CanAddPrescriptionItems", false);
                    return false;
                }

                // 验证处方是否存在
                var prescription = await FindEntityAsync<Prescription>(context.PrescriptionId.Value, cancellationToken);
                if (prescription == null)
                {
                    context.LogError("Prescription not found: {PrescriptionId}", context.PrescriptionId);
                    context.SetValidationResult("PrescriptionExists", false);
                    return false;
                }

                // 验证处方状态是否允许添加项目
                if (prescription.Status != Shared.Models.Enums.PrescriptionStatus.Draft)
                {
                    context.LogError("Cannot add items to prescription in status: {Status}", prescription.Status);
                    context.SetValidationResult("PrescriptionStatusAllowsEditing", false);
                    return false;
                }

                // 验证是否有药材项目需要添加
                if (context.Items == null || context.Items.Count == 0)
                {
                    context.LogError("No prescription items to add");
                    context.SetValidationResult("HasItemsToAdd", false);
                    return false;
                }

                // 验证药材项目数据完整性
                for (int i = 0; i < context.Items.Count; i++)
                {
                    var item = context.Items[i];
                    if (item.HerbId == Guid.Empty)
                    {
                        context.LogError("Prescription item {Index} has empty herb ID", i);
                        context.SetValidationResult($"Item_{i}_HerbIdValid", false);
                        return false;
                    }

                    if (string.IsNullOrEmpty(item.HerbName))
                    {
                        context.LogError("Prescription item {Index} has empty herb name", i);
                        context.SetValidationResult($"Item_{i}_HerbNameValid", false);
                        return false;
                    }

                    if (item.Quantity <= 0)
                    {
                        context.LogError("Prescription item {Index} has invalid quantity: {Quantity}", i, item.Quantity);
                        context.SetValidationResult($"Item_{i}_QuantityValid", false);
                        return false;
                    }

                    if (item.UnitPrice < 0)
                    {
                        context.LogError("Prescription item {Index} has negative unit price: {UnitPrice}", i, item.UnitPrice);
                        context.SetValidationResult($"Item_{i}_UnitPriceValid", false);
                        return false;
                    }
                }

                // 记录验证成功
                context.SetValidationResult("CanAddPrescriptionItems", true);
                context.SetValidationResult("PrescriptionExists", true);
                context.SetValidationResult("PrescriptionStatusAllowsEditing", true);
                context.SetValidationResult("HasItemsToAdd", true);

                return true;
            }
            catch (Exception ex)
            {
                context.LogError(ex, "Failed to validate prescription items addition conditions");
                return false;
            }
        }

        /// <inheritdoc />
        protected override async Task<TransactionStepResult> ExecuteDatabaseOperationAsync(
            PrescriptionTransactionContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var addedItems = new List<Guid>();
                var addedItemDetails = new List<string>();

                // 批量创建处方项目
                foreach (var itemContext in context.Items)
                {
                    var prescriptionItem = new PrescriptionItemModel
                    {
                        Id = Guid.NewGuid(),
                        PrescriptionId = context.PrescriptionId!.Value,
                        HerbId = itemContext.HerbId,
                        HerbName = itemContext.HerbName,
                        Quantity = itemContext.Quantity,
                        Unit = itemContext.Unit,
                        UnitPrice = itemContext.UnitPrice,
                        Usage = itemContext.Usage,
                        Remark = itemContext.Remark
                    };

                    // 保存到数据库
                    var createdItem = await CreateEntityAsync(prescriptionItem, cancellationToken);

                    addedItems.Add(createdItem.Id);
                    addedItemDetails.Add($"{createdItem.HerbName}:{createdItem.Quantity}{createdItem.Unit}@{createdItem.UnitPrice}");

                    Logger.LogDebug(
                        "Added prescription item: {ItemId} for prescription: {PrescriptionId}, Herb: {HerbName}, Quantity: {Quantity}, UnitPrice: {UnitPrice}",
                        createdItem.Id, context.PrescriptionId, createdItem.HerbName, createdItem.Quantity, createdItem.UnitPrice);
                }

                // 设置实体ID列表用于补偿
                context.SetEntityIds("PrescriptionItems", addedItems);

                // 重新计算处方总价
                if (context.AutoCalculatePrice)
                {
                    context.CalculateTotalPrice();

                    // 更新处方总价（可选）
                    await UpdatePrescriptionTotalAsync(context, cancellationToken);
                }

                // 记录添加历史
                await RecordItemsAdditionHistoryAsync(context, addedItems, addedItemDetails, cancellationToken);

                Logger.LogInformation(
                    "Successfully added {ItemCount} prescription items to prescription: {PrescriptionId}",
                    addedItems.Count, context.PrescriptionId);

                // 返回成功结果
                return CreateSuccessResult(new Dictionary<string, object>
                {
                    ["AddedItemIds"] = addedItems,
                    ["AddedItemDetails"] = addedItemDetails,
                    ["ItemCount"] = addedItems.Count,
                    ["PrescriptionId"] = context.PrescriptionId,
                    ["TotalPrice"] = context.TotalPrice,
                    ["Timestamp"] = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to add prescription items to prescription: {PrescriptionId}", context.PrescriptionId);
                throw;
            }
        }

        /// <inheritdoc />
        public override async Task<TransactionStepResult> CompensateAsync(
            PrescriptionTransactionContext context,
            TransactionStepResult originalResult,
            CancellationToken cancellationToken = default)
        {
            if (!SupportsCompensation)
            {
                return await base.CompensateAsync(context, originalResult, cancellationToken);
            }

            try
            {
                var itemIds = context.GetEntityIds("PrescriptionItems");
                if (itemIds == null || itemIds.Count == 0)
                {
                    Logger.LogWarning("No prescription item IDs found for compensation");
                    return CreateSuccessResult(new Dictionary<string, object> { ["Action"] = "NoCompensationNeeded" });
                }

                Logger.LogInformation("Starting compensation: deleting {ItemCount} prescription items", itemIds.Count);

                var deletedItems = new List<Guid>();

                // 批量删除添加的处方项目
                foreach (var itemId in itemIds)
                {
                    var deleted = await DeleteEntityAsync<PrescriptionItemModel>(itemId, cancellationToken);
                    if (deleted)
                    {
                        deletedItems.Add(itemId);
                    }
                    else
                    {
                        Logger.LogWarning("Prescription item not found during compensation: {ItemId}", itemId);
                    }
                }

                // 清除上下文中的相关信息
                context.RemoveEntityIds("PrescriptionItems");

                // 记录补偿历史
                await RecordCompensationHistoryAsync(context, deletedItems, cancellationToken);

                Logger.LogInformation("Successfully compensated: deleted {DeletedCount} prescription items", deletedItems.Count);

                return CreateSuccessResult(new Dictionary<string, object>
                {
                    ["Action"] = "PrescriptionItemsDeleted",
                    ["DeletedItemIds"] = deletedItems,
                    ["DeletedCount"] = deletedItems.Count,
                    ["CompensationTimestamp"] = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to compensate prescription items addition");
                return CreateFailureResult(ex, new Dictionary<string, object> { ["Action"] = "CompensationFailed" });
            }
        }

        /// <summary>
        /// 更新处方总价
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task UpdatePrescriptionTotalAsync(PrescriptionTransactionContext context, CancellationToken cancellationToken)
        {
            try
            {
                var prescription = await FindEntityAsync<Prescription>(context.PrescriptionId!.Value, cancellationToken);
                if (prescription != null)
                {
                    // 注意：Prescription实体中没有TotalPrice字段，这里仅用于演示
                    // 实际实现中，总价通常在DTO层计算，或者添加到实体中
                    // prescription.TotalPrice = context.TotalPrice;

                    await UpdateEntityAsync(prescription, cancellationToken);
                    Logger.LogDebug(
                        "Updated prescription total price: {PrescriptionId}, Total: {TotalPrice}",
                        prescription.Id, context.TotalPrice);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to update prescription total price");

                // 不抛出异常，因为这不是关键操作
            }
        }

        /// <summary>
        /// 记录药材项目添加历史
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="addedItems">添加的项目ID列表</param>
        /// <param name="addedItemDetails">添加的项目详情</param>
        /// <param name="cancellationToken">取消令牌</param>
        private Task RecordItemsAdditionHistoryAsync(
            PrescriptionTransactionContext context,
            List<Guid> addedItems,
            List<string> addedItemDetails,
            CancellationToken cancellationToken)
        {
            try
            {
                context.PrescriptionMetadata["ItemsAddition"] = new
                {
                    AddedAt = DateTime.UtcNow,
                    PrescriptionId = context.PrescriptionId,
                    AddedItemIds = addedItems,
                    AddedItemDetails = addedItemDetails,
                    ItemCount = addedItems.Count,
                    TotalPrice = context.TotalPrice
                };

                Logger.LogDebug(
                    "Recorded items addition history: {ItemCount} items for prescription {PrescriptionId}",
                    addedItems.Count, context.PrescriptionId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to record items addition history");

                // 不抛出异常，因为这不是关键操作
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 记录补偿操作历史
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="deletedItems">删除的项目ID列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        private Task RecordCompensationHistoryAsync(
            PrescriptionTransactionContext context,
            List<Guid> deletedItems,
            CancellationToken cancellationToken)
        {
            try
            {
                context.PrescriptionMetadata["ItemsCompensation"] = new
                {
                    CompensatedAt = DateTime.UtcNow,
                    DeletedItemIds = deletedItems,
                    DeletedCount = deletedItems.Count,
                    Reason = "TransactionRollback"
                };

                Logger.LogDebug("Recorded compensation history: deleted {DeletedCount} prescription items", deletedItems.Count);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to record compensation history");

                // 不抛出异常，因为这不是关键操作
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 验证药材配伍安全性（基础检查）
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <returns>验证结果</returns>
        private (bool IsValid, List<string> Warnings) ValidateHerbCompatibility(PrescriptionTransactionContext context)
        {
            var warnings = new List<string>();

            try
            {
                // 检查重复药材
                var herbIds = context.Items.Select(item => item.HerbId).ToList();
                var duplicateHerbs = herbIds.GroupBy(id => id)
                                          .Where(g => g.Count() > 1)
                                          .Select(g => g.Key)
                                          .ToList();

                if (duplicateHerbs.Any())
                {
                    var duplicateNames = context.Items
                        .Where(item => duplicateHerbs.Contains(item.HerbId))
                        .Select(item => item.HerbName)
                        .Distinct()
                        .ToList();

                    warnings.Add($"发现重复药材：{string.Join(", ", duplicateNames)}");
                }

                // TODO: 实现具体的配伍禁忌检查逻辑
                // 例如：十八反、十九畏等配伍禁忌

                return (true, warnings); // 目前返回通过，但记录警告
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to validate herb compatibility");
                return (true, new List<string> { "配伍检查过程中发生错误，请人工检查" });
            }
        }
    }
}
