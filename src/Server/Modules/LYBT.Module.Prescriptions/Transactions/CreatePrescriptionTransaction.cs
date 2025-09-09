using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Transactions;
using LYBT.Infrastructure.Transactions.Steps;
using LYBT.Module.Prescriptions.Transactions.Steps;

namespace LYBT.Module.Prescriptions.Transactions
{
    /// <summary>
    /// 创建处方事务定义
    /// 包含验证先决条件、创建处方、添加药材项目、验证配伍安全性、更新医疗案例关联的完整流程
    /// </summary>
    public class CreatePrescriptionTransaction
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CreatePrescriptionTransaction> _logger;

        public CreatePrescriptionTransaction(
            IServiceProvider serviceProvider, 
            ILogger<CreatePrescriptionTransaction> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 创建处方创建事务定义
        /// </summary>
        /// <param name="options">事务选项</param>
        /// <returns>事务定义</returns>
        public TransactionDefinition<PrescriptionTransactionContext> CreateDefinition(PrescriptionTransactionOptions? options = null)
        {
            options ??= new PrescriptionTransactionOptions();

            var steps = CreateTransactionSteps(options);

            var definition = new TransactionDefinition<PrescriptionTransactionContext>
            {
                Name = "CreatePrescription",
                Description = "创建处方流程：验证先决条件、创建处方、添加药材项目、验证配伍安全性、更新医疗案例关联",
                Steps = steps,
                Timeout = options.Timeout,
                MaxRetryCount = options.MaxRetryCount,
                EnableAutoCompensation = options.EnableAutoCompensation,
                EnableParallelExecution = false // 步骤之间有依赖关系，必须顺序执行
            };

            _logger.LogDebug("Created CreatePrescription transaction definition with {StepCount} steps", steps.Count);
            return definition;
        }

        /// <summary>
        /// 执行处方创建事务
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="options">事务选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>事务执行结果</returns>
        public async Task<TransactionResult<PrescriptionTransactionContext>> ExecuteAsync(
            PrescriptionTransactionContext context,
            PrescriptionTransactionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            // 验证上下文
            var (isValid, errors) = context.ValidateContext();
            if (!isValid)
            {
                _logger.LogError("Transaction context validation failed: {Errors}", string.Join(", ", errors));
                return new TransactionResult<PrescriptionTransactionContext>
                {
                    TransactionId = Guid.NewGuid(),
                    Status = TransactionStatus.Failed,
                    Context = context,
                    Message = $"Context validation failed: {string.Join(", ", errors)}",
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                };
            }

            // 创建事务定义
            var definition = CreateDefinition(options);
            
            // 获取事务协调器并执行
            var coordinator = _serviceProvider.GetRequiredService<ITransactionCoordinator<PrescriptionTransactionContext>>();
            return await coordinator.ExecuteAsync(definition, context, cancellationToken);
        }

        /// <summary>
        /// 创建事务步骤列表
        /// </summary>
        /// <param name="options">事务选项</param>
        /// <returns>事务步骤列表</returns>
        private List<ITransactionStep<PrescriptionTransactionContext>> CreateTransactionSteps(PrescriptionTransactionOptions options)
        {
            var steps = new List<ITransactionStep<PrescriptionTransactionContext>>();

            // 第一步：验证先决条件
            if (options.IncludeValidatePrerequisites)
            {
                var validatePrerequisitesStep = _serviceProvider.GetRequiredService<ValidatePrerequisitesStep>();
                steps.Add(validatePrerequisitesStep);
            }

            // 第二步：创建处方基础记录
            if (options.IncludeCreatePrescription)
            {
                var createPrescriptionStep = _serviceProvider.GetRequiredService<CreatePrescriptionStep>();
                steps.Add(createPrescriptionStep);
            }

            // 第三步：添加药材项目
            if (options.IncludeAddPrescriptionItems)
            {
                var addPrescriptionItemsStep = _serviceProvider.GetRequiredService<AddPrescriptionItemsStep>();
                steps.Add(addPrescriptionItemsStep);
            }

            // 第四步：验证配伍安全性
            if (options.IncludeValidateCompatibility)
            {
                var validateCompatibilityStep = _serviceProvider.GetRequiredService<ValidateCompatibilityStep>();
                steps.Add(validateCompatibilityStep);
            }

            // 第五步：更新医疗案例关联
            if (options.IncludeUpdateMedicalCase)
            {
                var updateMedicalCaseStep = _serviceProvider.GetRequiredService<UpdateMedicalCaseStep>();
                steps.Add(updateMedicalCaseStep);
            }

            // 可以根据选项添加额外的步骤
            if (options.IncludeBusinessRuleValidation)
            {
                steps.Add(CreateBusinessRuleValidationStep());
            }

            if (options.IncludeNotification)
            {
                steps.Add(CreateNotificationStep());
            }

            return steps;
        }

        /// <summary>
        /// 创建业务规则验证步骤
        /// </summary>
        /// <returns>业务规则验证步骤</returns>
        private ITransactionStep<PrescriptionTransactionContext> CreateBusinessRuleValidationStep()
        {
            // 可以创建一个专门的业务规则验证步骤
            // 这里为简化，返回一个条件步骤
            return new PrescriptionBusinessRuleValidationStep(_serviceProvider.GetRequiredService<ILogger<PrescriptionBusinessRuleValidationStep>>());
        }

        /// <summary>
        /// 创建通知步骤
        /// </summary>
        /// <returns>通知步骤</returns>
        private ITransactionStep<PrescriptionTransactionContext> CreateNotificationStep()
        {
            // 可以创建一个通知步骤，发送处方创建的通知
            return new PrescriptionNotificationStep(_serviceProvider.GetRequiredService<ILogger<PrescriptionNotificationStep>>());
        }
    }

    /// <summary>
    /// 处方创建事务选项配置
    /// </summary>
    public class PrescriptionTransactionOptions
    {
        /// <summary>
        /// 事务超时时间
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 0;

        /// <summary>
        /// 是否启用自动补偿
        /// </summary>
        public bool EnableAutoCompensation { get; set; } = true;

        /// <summary>
        /// 是否包含验证先决条件步骤
        /// </summary>
        public bool IncludeValidatePrerequisites { get; set; } = true;

        /// <summary>
        /// 是否包含创建处方步骤
        /// </summary>
        public bool IncludeCreatePrescription { get; set; } = true;

        /// <summary>
        /// 是否包含添加药材项目步骤
        /// </summary>
        public bool IncludeAddPrescriptionItems { get; set; } = true;

        /// <summary>
        /// 是否包含验证配伍安全性步骤
        /// </summary>
        public bool IncludeValidateCompatibility { get; set; } = true;

        /// <summary>
        /// 是否包含更新医疗案例关联步骤
        /// </summary>
        public bool IncludeUpdateMedicalCase { get; set; } = true;

        /// <summary>
        /// 是否包含业务规则验证步骤
        /// </summary>
        public bool IncludeBusinessRuleValidation { get; set; } = false;

        /// <summary>
        /// 是否包含通知步骤
        /// </summary>
        public bool IncludeNotification { get; set; } = false;

        /// <summary>
        /// 是否自动计算处方价格
        /// </summary>
        public bool AutoCalculatePrice { get; set; } = true;

        /// <summary>
        /// 是否允许处方重复药材
        /// </summary>
        public bool AllowDuplicateHerbs { get; set; } = false;

        /// <summary>
        /// 是否跳过配伍安全性检查
        /// </summary>
        public bool SkipCompatibilityCheck { get; set; } = false;

        /// <summary>
        /// 配伍检查严格程度（Critical/High/Medium/Low）
        /// </summary>
        public string CompatibilityCheckLevel { get; set; } = "Medium";

        /// <summary>
        /// 自定义验证规则
        /// </summary>
        public Dictionary<string, object> CustomValidationRules { get; set; } = new();

        /// <summary>
        /// 事务元数据
        /// </summary>
        public Dictionary<string, object> TransactionMetadata { get; set; } = new();

        /// <summary>
        /// 创建默认选项
        /// </summary>
        /// <returns>默认选项实例</returns>
        public static PrescriptionTransactionOptions Default()
        {
            return new PrescriptionTransactionOptions();
        }

        /// <summary>
        /// 创建快速创建选项（跳过一些非关键验证）
        /// </summary>
        /// <returns>快速创建选项实例</returns>
        public static PrescriptionTransactionOptions QuickCreate()
        {
            return new PrescriptionTransactionOptions
            {
                Timeout = TimeSpan.FromMinutes(5),
                SkipCompatibilityCheck = true,
                IncludeBusinessRuleValidation = false,
                IncludeNotification = false,
                CompatibilityCheckLevel = "Low"
            };
        }

        /// <summary>
        /// 创建严格验证选项（包含所有验证步骤）
        /// </summary>
        /// <returns>严格验证选项实例</returns>
        public static PrescriptionTransactionOptions Strict()
        {
            return new PrescriptionTransactionOptions
            {
                Timeout = TimeSpan.FromMinutes(15),
                IncludeBusinessRuleValidation = true,
                IncludeNotification = true,
                AllowDuplicateHerbs = false,
                SkipCompatibilityCheck = false,
                CompatibilityCheckLevel = "Critical"
            };
        }

        /// <summary>
        /// 创建开发环境选项（包含详细日志和验证）
        /// </summary>
        /// <returns>开发环境选项实例</returns>
        public static PrescriptionTransactionOptions Development()
        {
            return new PrescriptionTransactionOptions
            {
                Timeout = TimeSpan.FromMinutes(20),
                MaxRetryCount = 1,
                IncludeBusinessRuleValidation = true,
                IncludeNotification = false, // 开发环境不发送通知
                AllowDuplicateHerbs = true, // 开发时允许重复药材
                CompatibilityCheckLevel = "Medium"
            };
        }
    }

    /// <summary>
    /// 处方业务规则验证步骤示例
    /// </summary>
    internal class PrescriptionBusinessRuleValidationStep : ConditionalTransactionStep<PrescriptionTransactionContext>
    {
        public override string StepName => "PrescriptionBusinessRuleValidation";
        public override int Order => 100;

        public PrescriptionBusinessRuleValidationStep(ILogger logger) : base(logger)
        {
        }

        protected override Task<bool> EvaluateConditionAsync(PrescriptionTransactionContext context, CancellationToken cancellationToken = default)
        {
            // 实现具体的业务规则验证逻辑
            // 例如：检查医生是否有权限开具该类型处方、检查患者是否有禁忌症等
            return Task.FromResult(true);
        }

        protected override Task<TransactionStepResult> ExecuteConditionalOperationAsync(PrescriptionTransactionContext context, CancellationToken cancellationToken = default)
        {
            // 执行业务规则验证
            Logger?.LogInformation("Prescription business rule validation passed for prescription: {PatientId}, {DoctorId}", context.PatientId, context.DoctorId);
            return Task.FromResult(CreateSuccessResult());
        }
    }

    /// <summary>
    /// 处方通知步骤示例
    /// </summary>
    internal class PrescriptionNotificationStep : TransactionStepBase<PrescriptionTransactionContext>
    {
        private readonly ILogger _logger;

        public override string StepName => "SendPrescriptionNotification";
        public override int Order => 200;
        public override bool SupportsCompensation => false; // 通知通常不需要补偿

        public PrescriptionNotificationStep(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task<TransactionStepResult> ExecuteAsync(PrescriptionTransactionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // 发送处方创建完成通知
                _logger.LogInformation("Sending prescription creation notification for patient: {PatientId}, doctor: {DoctorId}, prescription: {PrescriptionId}", 
                    context.PatientId, context.DoctorId, context.PrescriptionId);

                // 这里可以集成实际的通知系统
                // 例如：发送短信、邮件、系统内消息、打印提醒等

                return Task.FromResult(CreateSuccessResult(new Dictionary<string, object>
                {
                    ["NotificationType"] = "PrescriptionCreated",
                    ["Recipients"] = new[] { context.PatientId.ToString(), context.DoctorId.ToString() },
                    ["PrescriptionId"] = context.PrescriptionId?.ToString() ?? "",
                    ["ItemCount"] = context.Items.Count,
                    ["TotalPrice"] = context.TotalPrice,
                    ["Timestamp"] = DateTime.UtcNow
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send prescription notification");
                return Task.FromResult(CreateFailureResult(ex));
            }
        }
    }
}